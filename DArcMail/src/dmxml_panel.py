#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import time
import sys
import os
import re
import wx
import  wx.lib.scrolledpanel as scrolled
from wx import DirPickerCtrl
import dm_common as dmc
import dm_wx
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT
import dm_defaults as dmd
import mbox2xml
import info_window
import folder_data as fld

####################################################################
## DMXmlPanel
####################################################################
class DMXmlPanel (scrolled.ScrolledPanel):

  variable_names = [
    "account",
    "account_dir",
    "mbox",
    "mbox_select"     # index of selection
  ]

  name2default   = {
    "account"         : "",
    "account_dir"     : dmd.DEFAULT_ACCOUNT_DIRECTORY_PREFIX,
    "mbox"            : "",
    "mbox_select"   : 0    # index of selection
  }

  log_name               = "dm_xml.log.txt"
  
  ####################################################################
  def __init__ (self, parent):

    wx.ScrolledWindow.__init__ (self, parent=parent)

    self.name2component = {}
    self.account          = None
    self.account_dir      = None
    self.mbox_select      = None    # index of selection
    self.logf             = None
    self.folder_name      = None
        
    normal_font_size = self.GetFont().GetPointSize()  # get the current size
    bigger_font_size = normal_font_size + 3

    sz = wx.BoxSizer(orient=wx.VERTICAL)
    sz.Add((FRAME_WIDTH, 10))

    t1 = wx.StaticText(self, label="Convert .mbox Files to XML")
    t1.SetFont(wx.Font(bigger_font_size, wx.SWISS, wx.NORMAL, wx.BOLD))
    sz.Add(t1, 0, wx.ALIGN_CENTER, 5)

    sz.Add((FRAME_WIDTH-30, 15))
    sz.Add(self.make_form(), 0, wx.ALIGN_LEFT|wx.ALL, 5)
    
    sz.Add(dm_wx.ActionButtons(self, "Convert to XML"), 0, wx.ALIGN_CENTER, 5)
    
    sizer = wx.BoxSizer(orient=wx.HORIZONTAL)
    sizer.Add((20,FRAME_HEIGHT))
    sizer.Add(sz, 1, wx.EXPAND)
    self.SetSizer(sizer)
    self.SetupScrolling()

    self.name2component["reset_button"].Bind(wx.EVT_BUTTON, self.ExecuteReset)
    self.name2component["go_button"].Bind(wx.EVT_BUTTON,
                                          self.ValidateVariablesAndGo)
    self.name2component["account_dir"].Bind(wx.EVT_DIRPICKER_CHANGED,
                               self.AccountDirChanged)
    self.ResetVariables()
    
  ####################################################################
  def ResetVariables (self):
    self.name2component["account_dir"]. \
        SetPath(self.name2default["account_dir"])
    ms = self.name2component["mbox_select"]
    ms.Clear()
    ms.Append("[ALL MBOXES]")
    ms.SetSelection(self.name2default["mbox_select"])
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
  def LogVariables (self):
    self.logf.write("########## SETTINGS ##########\n")
    self.logf.write("account: " + self.account + "\n")
    self.logf.write("account directory: " + self.account_dir + "\n")
    self.logf.write("external content: all attachments\n")
    if self.folder_name:
      self.logf.write("do only one folder: " + self.folder_name + "\n")

  ####################################################################
  def ValidateVariablesAndGo(self, event):
    ready = True

    self.account = self.name2component["account"].GetValue()
    print(self.account)
    if self.account:
      self.account = self.account.strip()
    if not self.account:
      msg = "You must specify a name for the account"
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False
      # no sense checking further if the account does not exist
      return

    self.account_dir = self.name2component["account_dir"].GetPath()
    msg = dm_wx.ValidateDirectory(self, self.account_dir)
    if msg != "":
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False
      # no point in continuing
      return

    self.mbox_select = \
        self.name2component["mbox_select"].GetCurrentSelection()
      
    if ready:
      self.logf = open(os.path.join(self.account_dir, self.log_name),
          "w", errors="replace")
      self.LogVariables()

      self.name2component["go_button"].Disable()
      self.convert_data()
      self.name2component["go_button"].Enable()
      self.ResetVariables()
      self.SetFocus()

  ######################################################################
  def convert_data (self):
    mx = mbox2xml.Mbox2Xml(
        self.logf,
        self.account,
        dmc.CHUNK_BYTES)
    wait = wx.BusyCursor()
    if self.mbox_select > 0:
      self.fd_list = [ self.fd_list[self.mbox_select-1] ]
      self.folder_name = self.fd_list[0].folder_name
    else:
      self.folder_name = None
    for fd in self.fd_list:
      mx.walk_folder(fd)
    mx.write_log()

    del mx    
    del wait

    self.logf.close()
    self.logf = open(os.path.join(self.account_dir, self.log_name))
    message = self.logf.read()
    self.logf.close()
    summary_info = info_window.InfoWindow(self,
        "Convert mbox to XML Summary", message)
    summary_info.Show(True)
    summary_info.SetFocus()

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
#        style=wx.DIRP_USE_TEXTCTRL|wx.DIRP_DIR_MUST_EXIST,
#       style=wx.DIRP_DIR_MUST_EXIST,
        name="account_dir")
    box1.Add(t1, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add(dpc, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add((FRAME_WIDTH-30, 15))

    ##
    ## mboxes
    ##

    msbox = self.msbox = wx.BoxSizer(wx.HORIZONTAL)
    msbox.Add(wx.StaticText(self, label="Select one or all mboxes:"),
        0, wx.ALIGN_LEFT|wx.ALL, 5)
    self.name2component["mbox_select"] = mbox_select = \
        wx.ComboBox(self, style=wx.CB_DROPDOWN,
        choices=["[ALL MBOXES"], name="mbox_select")
    msbox.Add(mbox_select, 0, wx.ALIGN_LEFT|wx.LEFT|wx.BOTTOM, 5)
    box1.Add(msbox, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add((FRAME_WIDTH-30, 15))

    return box1

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
    ms.Append("ALL MBOXES")
    for c in choices:
      ms.Append(c)
    ms.SetSelection(0)

  ####################################################################
  def AccountDirChanged (self, event):
    self.make_mbox_select()
    self.Layout()
