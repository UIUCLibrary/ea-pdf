#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import threading
import time
import sys
import os
import re
import wx
import  wx.lib.scrolledpanel as scrolled
import db_access as dba
import dm_common as dmc
import dm_wx
from wx import DirPickerCtrl
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT
import dm_defaults as dmd
import mbox2db
import info_window
import folder_data as fld
import sqlite_schema

## Issue with wxpython and Mac OS X 10.11:
## A wx.MessageDialog created in an event-triggered thread can be
## caused to disappear ONLY if either (a) a new wx.MessageDiaglog
## is created in the same event-triggered thread (which then becomes
## the zombie), OR (b) the event-triggered thread completes.
## We have to disallow this zombie MessgeDialog in the case where
## a user is prompted to create an account that does not yet exist
## before loading. So we will let the account-creation thread finish
## and require the user to hit the "load data" button again. Sigh.

####################################################################
## LoadPanel
####################################################################
class LoadPanel (scrolled.ScrolledPanel):

  variable_names = [
    "account",
    "account_dir",
    "mbox_select",
    "yes_ext_rb",
    "no_ext_rb"
  ]

  name2default   = {
    "account"         : "",
    "account_dir"     : "",
    "mbox_select"   : 0,
    "yes_ext_rb"      : False,
    "no_ext_rb"       : True
  }

  name2component = {}

  account         = None
  account_dir     = None
  mbox_select     = 0
  
  external_attachment = True
  account_id      = None
  cnx             = None

  fd_list         = None
  log_name        = "dm_load.log.txt"
  external_subdir_levels = 1
  logf                   = None
    
  ####################################################################
  def __init__ (self, parent):

    wx.ScrolledWindow.__init__ (self, parent=parent)
    self.parent = parent

    normal_font_size = self.GetFont().GetPointSize()  # get the current size
    bigger_font_size = normal_font_size + 3
    
    sz = wx.BoxSizer(orient=wx.VERTICAL)
    sz.Add((FRAME_WIDTH-30, 10))

    t1 = wx.StaticText(self, label="Load DArcMail Data")
    t1.SetFont(wx.Font(bigger_font_size, wx.SWISS, wx.NORMAL, wx.BOLD))
    sz.Add(t1, 0, wx.ALIGN_CENTER, 5)
    sz.Add((FRAME_WIDTH-30, 15))

    sz.Add(self.make_form(), 0, wx.ALL, 5)
    sz.Add((FRAME_WIDTH-30, 15))
    
    sz.Add(dm_wx.ActionButtons(self, "Load Data"), 0,
           wx.ALIGN_CENTER|wx.ALL, 5)
    
    self.SetSizer(sz)
    self.SetupScrolling()
    self.ResetVariables()
    self.name2component["reset_button"].Bind(wx.EVT_BUTTON, self.ExecuteReset)
    self.name2component["go_button"].Bind(wx.EVT_BUTTON, self.ValidateVariablesAndGo)
    self.name2component["account_dir"].Bind(wx.EVT_DIRPICKER_CHANGED,
                               self.AccountDirChanged)
    self.Bind(wx.EVT_SHOW, self.ExecuteReset)
    
  ####################################################################
  def ResetVariables (self):
    (aid, aname, adir) = (None, None, None)
    if self.cnx:
      info = dba.loaded_account_info(self.cnx)
      if len(info) > 1:
        print("ERROR: database file contains more than one account")
        sys.exit(0)
      elif len(info) == 1:
        (aid, aname, adir) = info[0]
    for v in self.variable_names:
      if v == "account_dir":
        if adir:
          self.name2component[v].SetPath(adir)
        else:
          self.name2component[v].SetPath(self.name2default[v])
      elif v == "mbox_select":
        self.name2component[v].SetSelection(self.name2default[v])
      elif v == "account":
        if aname:
          self.name2component[v].SetValue(aname)
        else:
          self.name2component[v].SetValue(self.name2default[v])
      else:
        self.name2component[v].SetValue(self.name2default[v])
    self.make_mbox_select()
    if aid:
      self.name2component["account"].Disable()
      self.name2component["account_dir"].Disable()
    else:
      self.name2component["account"].Enable()
      self.name2component["account_dir"].Enable()
    self.Layout()
    
  ####################################################################
  def ExecuteReset (self, event):
    self.ResetVariables()
    self.GetParent().SetFocus()

  ####################################################################
  def ExecuteExit (self, event):
    # self is a
    #   wx.Panel within a
    #       wx.Notebook within a
    #             wx.Frame
    self.GetParent().GetParent().Close()

  ####################################################################
  def AccountDirChanged (self, event):
    self.make_mbox_select()
    self.Layout()
    
  ####################################################################
  def LogVariables (self):
    self.logf.write("########## SETTINGS ##########\n")
    self.logf.write("account: " + self.account + "\n")
    self.logf.write("account directory: " + self.account_dir + "\n")
    if self.external_attachment:
      self.logf.write("store attachments externally" + "\n")
    if self.folder_name:
      self.logf.write("do only one mbox: " + self.folder_name + ".mbox\n")
    self.logf.write("\n")

  ####################################################################
  def ValidateVariablesAndGo (self, event):
    ready = True

    # normalize the account_dir before entering into the db
    temp_dir = self.name2component["account_dir"].GetPath()
    temp_dir = os.path.normcase(os.path.abspath(temp_dir))
    self.name2component["account_dir"].SetPath(temp_dir)
    self.name2component["account_dir"].Refresh()

    msg = dm_wx.ValidateDirectory(self,
        self.name2component["account_dir"].GetPath())
    if msg != "":
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False
      # no point in continuing
      return
    
    msg = dm_wx.ValidateAccount(self, self.name2component["account"].GetValue())
    if msg != "":
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False
      # no point in continuing
      return

    # An account name was specified; it may or may not exist.
    # An account directory was specified and is externally valid;
    #   it may or may not be recorded in the db.
    # Now check to see that either:
    #   (a) neither the account name nor directory appears in the database; or
    #   (b) they both appear in the database and are associated with each other

    self.account_id = dba.get_account_id(self.cnx, self.account, make=False)
    acc_info = dba.lookup_account_directory (self.cnx, self.account_dir)
    acc_names = []
    for (id, name, dir) in acc_info:
      acc_names.append(name)

    if len(acc_names) > 1:
      msg = "database is corrupt;\n" + self.account_dir + \
        " is associated with different accounts: " + \
        ", ".join(acc_names)
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False
    elif self.account_id == None and len(acc_names) > 0:
      msg = self.account_dir + " is associated with a different account: " + \
          ", ".join(acc_names)
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False
    elif self.account_id and (self.account not in acc_names):
      msg = "Account " + self.account + " has already been " + \
          "assigned a different directory. " + \
          "If you want to assign directory " + self.account_dir + " " + \
          "You must first " + \
          "delete the existing account and all its data"
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False

    if ready and self.account_id == None:
      msg = "No account " + self.account + "; do you want to create it?"
      md = wx.MessageDialog(parent=self, message=msg, caption="Info",
          style=wx.CANCEL|wx.OK|wx.ICON_EXCLAMATION)
      md.SetOKLabel("Create Account?")
      retcode = md.ShowModal()
      if retcode == wx.ID_OK:
        self.account_id = dba.get_account_id(self.cnx, self.account, make=True)
        ## We won't commit here; it will happen after the first message is committed
        ## or else, not at all. This will prevent having committed an account
        ## name that has no associated directory
        if self.account_id == None:
          msg = "Cannot create account " + self.account
          md = wx.MessageDialog(parent=self, message=msg, caption="Error",
              style=wx.OK|wx.ICON_EXCLAMATION)
          retcode = md.ShowModal()
          ready = False
          return
        else:
          dba.update_account_directory(self.cnx, self.account_id, self.account_dir)
      else:
        # no sense proceeding if missing account will not be created
        ready = False
        return

    # at this point, self.account_id is valid

    self.external_attachment = self.name2component["yes_ext_rb"].GetValue()
    
    self.mbox_select = \
        self.name2component["mbox_select"].GetCurrentSelection()
    # copy self.fd_list first
    if self.mbox_select > 0:
      load_list = [ self.fd_list[self.mbox_select-1] ]
      self.folder_name = load_list[0].folder_name
    else:
      load_list = [ fd for fd in self.fd_list ]
      self.folder_name = None
      
    for fd in load_list:
      folder_id = dba.lookup_folder_name(self.cnx, self.account, fd.folder_name)
      if folder_id:
        msg = "Folder " + fd.folder_name + \
            " is already in the database; you must delete the account " + \
            "and all its existing folders before this folder can be loaded."
        md = wx.MessageDialog(parent=self, message=msg, caption="Error",
           style=wx.OK|wx.ICON_EXCLAMATION)
        retcode = md.ShowModal()
        ready = False
        break

    if ready:
      self.logf = open(os.path.join(self.account_dir, self.log_name),
          "w", errors="replace")
      self.LogVariables()

      self.load_data(load_list)
      
      self.ResetVariables()
      self.SetFocus()
      ## not sure why this return statement is needed,
      ## but the call to load_data is sometimes repeated ...
      return
    
  ######################################################################
  def load_one_folder (self, mbdb, fd):
    folder_id = dba.get_folder_id(self.cnx, self.account_id,
      fd.folder_name, make=True)
    fd.set_folder_id(folder_id)
    disable_all = wx.WindowDisabler()
    busy = wx.BusyInfo("Loading mbox data for account " + \
        str(self.account) + ",\nfolder " + fd.folder_name)
    ## need the call to Yield to get the busy box painted
    wx.GetApp().Yield()
    mbdb.do_mbox_folder(fd)
    del busy
    del disable_all

  ######################################################################
  def load_data (self, load_list):
    self.name2component["go_button"].Disable()
#    sqlite_schema.run_drop_index(self.cnx)
    mbdb = mbox2db.Mbox2Db(
        self.cnx,
        self.logf,
        self.account,
        self.account_dir,
        self.account_id,
        self.external_attachment,
        self.external_subdir_levels)
    # self.fd_list has already been set properly (either one folder or all)
    for fd in load_list:
      self.load_one_folder(mbdb, fd)

#    sqlite_schema.run_create_index(self.cnx)
    mbdb.write_log()
    del mbdb
    
    self.acp.set_account(self.account_id, self.account,
                                 self.account_dir)
    self.acp.OnPageSelect()
    
    self.logf.close()
    self.logf = open(os.path.join(self.account_dir, self.log_name))
    message = self.logf.read()
    self.logf.close()

    self.ResetVariables()
    self.name2component["go_button"].Enable()
    summary_info = info_window.InfoWindow(self, "Load Summary", message)
    summary_info.Show(True)
    summary_info.SetFocus()
  
  ####################################################################
  def make_mbox_select (self):
    account_directory = self.name2component["account_dir"].GetPath()
    account_name = self.name2component["account"].GetValue()
    self.fd_list = []
    choices = []
    if account_directory:
      fld.GetFolders(account_name, account_directory, self.fd_list,
                            choices)
    ms = self.name2component["mbox_select"]
    ms.Clear()
    ms.Append("ALL FOLDERS")
    for c in choices:
      ms.Append(c)
    ms.SetSelection(0)

  ######################################################################
  def make_form (self):

    box1 = wx.StaticBoxSizer(wx.StaticBox(self), wx.VERTICAL)

    ##
    ## account name and directory
    ##

    t1 = wx.StaticText(self, label="Account Name:")
    self.name2component["account"] = txc = \
        wx.TextCtrl(self, name="account", size=(200, -1))
    box1.Add(t1, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add(txc, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add((FRAME_WIDTH-30, 15))

    t1 = wx.StaticText(self, label="Account Directory:")
    self.name2component["account_dir"] = dpc = \
        DirPickerCtrl(self, size=(500,-1),
        style=wx.DIRP_DEFAULT_STYLE,
#        style=wx.DIRP_DIR_MUST_EXIST,
#        style=wx.DIRP_USE_TEXTCTRL|wx.DIRP_DIR_MUST_EXIST,
        name="account_dir")
    box1.Add(t1, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add(dpc, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add((FRAME_WIDTH-30, 15))

    fsbox = self.fsbox = wx.BoxSizer(wx.HORIZONTAL)
    fsbox.Add(wx.StaticText(self, label="Select one or all folders:"),
        0, wx.ALIGN_LEFT|wx.ALL, 5)
    self.name2component["mbox_select"] = fs = \
        wx.ComboBox(self, style=wx.CB_DROPDOWN,
        choices=["[ALL FOLDERS"], name="mbox_select")
    fsbox.Add(fs, 0, wx.ALIGN_LEFT|wx.LEFT|wx.BOTTOM, 5)
    box1.Add(fsbox, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add((FRAME_WIDTH-30, 15))
    
    ##
    ## external attachments
    ##
    
    t1 = wx.StaticText(self, label="Store attachments externally?")
    self.name2component['yes_ext_rb'] = yes_ext_rb = \
        wx.RadioButton(self, label=' Yes', name='yes_ext_rb',
        style=wx.RB_GROUP)
    self.name2component['no_ext_rb'] = no_ext_rb = \
        wx.RadioButton(self, label=' No ', name='no_ext_rb')
    rb_sizer = wx.BoxSizer(wx.HORIZONTAL)
    rb_sizer.Add(yes_ext_rb, 0, wx.RIGHT|wx.LEFT, 10)
    rb_sizer.Add(no_ext_rb, 0, wx.RIGHT|wx.LEFT, 10)
    box1.Add(t1, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add(rb_sizer, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add((FRAME_WIDTH-30, 15))
    
    return box1
