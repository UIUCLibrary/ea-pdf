#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import time
import sys
import os
import re
import mailbox
import email
import wx
import  wx.lib.scrolledpanel as scrolled
from wx import DirPickerCtrl
import db_access as dba
import dm_common as dmc
import dm_defaults as dmd
import dm_wx
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT, FRAME_XPOS, FRAME_YPOS
import message_group
import stop_watch
import info_window
import message_log
import hashlib

####################################################################
## ExportPanel
####################################################################
class ExportPanel (scrolled.ScrolledPanel):
  name2component = {}
  name2default   = {
    "folder_select"    : 0,
    "include_rb"       : True,
    "export_directory" : dmd.DEFAULT_EXPORT_DIRECTORY_PREFIX
  }
 
  ####################################################################
  def __init__ (self, parent):

    wx.ScrolledWindow.__init__ (self, parent=parent)

    self.account_id        = None
    self.account           = ""
    self.account_dir       = ""
    self.export_directory  = ""
    self.logf              = None
    self.cnx               = None
    self.log_name          = "dm_export.log.txt"
    self.browse            = parent.bp
    
    normal_font_size = self.GetFont().GetPointSize()  # get the current size
    bigger_font_size = normal_font_size + 3

    self.sizer = sizer = wx.BoxSizer(orient=wx.VERTICAL)
    self.SetSizer(sizer)
    self.SetupScrolling()

    sizer.Add((FRAME_WIDTH-30, 10))

    t1 = wx.StaticText(self, label="Export Mbox File")
    t1.SetFont(wx.Font(bigger_font_size, wx.SWISS, wx.NORMAL, wx.BOLD))
    sizer.Add(t1, 0, wx.ALIGN_CENTER, 5)

    sizer.Add((FRAME_WIDTH-30, 15))
    sizer.Add(self.make_form(), 0, wx.ALIGN_LEFT|wx.ALL, 5)
    
    ##
    ## action buttons
    ##
    sizer.Add(dm_wx.ActionButtons(self, "Export Mbox"), 0,
              wx.ALIGN_CENTER, 5)
    sizer = wx.BoxSizer(orient=wx.HORIZONTAL)
    sizer.Add((20,FRAME_HEIGHT))
    sizer.Add(sizer, 1, wx.EXPAND)

    self.name2component["reset_button"].Bind(wx.EVT_BUTTON,
        self.ExecuteReset)
    self.name2component["go_button"].Bind(wx.EVT_BUTTON,
        self.ValidateVariablesAndGo)
    self.Bind(wx.EVT_SHOW, self.NewExport)
    self.Layout()
    
  ####################################################################
  def get_folders (self):
    fids   = []
    fnames = []
    mboxes = []
    query = "select f.id, f.folder_name " + \
        "from folder f where f.account_id=" + \
        str(self.account_id) + " " + \
        "order by f.folder_name"
    cursor = self.cnx.cursor()
    cursor.execute(query)
    for (fid, fname) in cursor:
      fids.append(fid)
      fnames.append(fname)
      fp = os.path.abspath(os.path.join(self.account_dir, fname))
      mbox = fp + ".mbox"
      mboxes.append(mbox)
    cursor.close
    return (fids, fnames, mboxes)

  ####################################################################
  def NewExport (self, event):
    if not self.IsShown():
      return
    if not self.cnx:
      self.cnx = self.browse.cnx
    (account_id, account, account_dir) = \
        self.acp.get_account()
    if self.account_id != account_id:
      self.make_account_header()
      self.make_folder_select()
      self.name2component["include_rb"]. \
          SetValue(self.name2default["include_rb"])
      self.name2component["export_directory"]. \
          SetPath(self.name2default["export_directory"])
      self.name2component["folder_select"]. \
          SetSelection(self.name2default["folder_select"])
    self.Layout()

  ####################################################################
  def make_account_header (self):
    if self.acp.account_is_set():
      (self.account_id, self.account, self.account_dir) = \
          self.acp.get_account()
      self.name2component["static_account"]. \
          SetLabel(self.account)
      self.name2component["static_account_dir"]. \
          SetLabel(self.account_dir)
      
  ####################################################################
  def make_folder_select (self, evt=None):
    (self.fids, self.fnames, self.mboxes) = self.get_folders()
    fs = self.name2component["folder_select"]
    fs.Clear()
    fs.Append("[ALL FOLDERS]")
    for fname in self.fnames:
      fs.Append(fname)
    fs.SetSelection(self.name2default["folder_select"])
    self.Layout()
    
  ####################################################################
  def ExecuteReset (self, event=None):
    self.name2component["export_directory"].SetPath(
        self.name2default["export_directory"])
    self.name2component["include_rb"].SetValue(
        self.name2default["include_rb"])
    self.make_folder_select()
    self.Layout()
#    self.GetParent().SetFocus()

  ####################################################################
  def LogVariables(self):
    self.logf.write("########## SETTINGS ##########" + "\n")
    self.logf.write("account: " + self.account + "\n")

  ######################################################################
  def ValidateFolders (self, account_dir, mboxes):
    for mbox in mboxes:
      if not os.path.exists(mbox):
        return "mbox file " + mbox + " does not exist"
      elif not os.path.isfile(mbox):
        return "mbox file " + mbox + " is not a file"
    return ""

  ######################################################################
  def CheckForExistingExports (self, account_dir, exp_dir, mboxes):
    for mbox in mboxes:
      print("mbox = " + mbox + " " +  mbox[len(account_dir)+1:])
      ex_mbox = os.path.join(exp_dir, mbox[len(account_dir)+1:])
      if os.path.exists(ex_mbox):
        return ex_mbox + " already exists; cannot be overwritten"
    return ""

  ######################################################################
  def ValidateExportDirectory (self, dir):
    if dir:
      self.export_directory = d = os.path.abspath(dir.strip())
      if not os.path.exists(d):
        return "export directory " + d + \
              " does not exist"
      elif self.export_directory == self.account_dir:
        return "export directory must be different from account directory"
      elif not os.path.isdir(d):
        return d + " is not a directory"
      else:
        return ""
    else:
      return "null export directory"

  ######################################################################
  def ValidateVariablesAndGo (self, event):
    ready = True

    exp_dir = self.name2component["export_directory"].GetPath()
    print(exp_dir)
    msg = self.ValidateExportDirectory(exp_dir)
    if msg != "":
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False
      return

    idx = self.name2component["folder_select"].GetSelection()
    # use copies of fids, fnames, mboxes in case user
    # retries without resetting
    if idx > 0:
      fids   = [self.fids[idx-1]]
      fnames = [self.fnames[idx-1]]
      mboxes = [self.mboxes[idx-1]]
    else:
      fids = self.fids
      fnames = self.fnames
      mboxes = self.mboxes

    msg = self.ValidateFolders(self.account_dir, mboxes)
    if msg != "":
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False
      return
    
    msg = self.CheckForExistingExports(self.account_dir,
                                       exp_dir, mboxes)
    if msg != "":
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False
      return
    if ready:
      include = self.name2component["include_rb"].GetValue()
      self.name2component["go_button"].Disable()
      self.export_data(self.account_id, self.export_directory,
                       fids, fnames, mboxes, include)
      self.name2component["go_button"].Enable()
        
  ######################################################################
  def get_exportable_glids (self, account_id, fid, include):
    mg = message_group.message_group_for_account(account_id)
    glids = set()
    query = "select m.id, m.global_message_id, m.date_time " + \
      "from message m where m.folder_id=" + str(fid) + " " +\
      "order by m.date_time"
    cursor = self.cnx.cursor()
    cursor.execute(query)
    for (mid, glid, dt) in cursor:
      if include and mid in mg:
        glids.add(glid)
      elif not include and mid not in mg:
        glids.add(glid)
    cursor.close
    return glids

  ######################################################################
  def export_data (self, account_id, export_directory,
      fids, fnames, mboxes, include):
    self.logf = open(os.path.join(export_directory, self.log_name),
        "w", errors="replace")
    self.LogVariables()
    self.sw = stop_watch.StopWatch()
    for i in range(0,len(fids)):
      glids = self.get_exportable_glids(account_id, fids[i], include)
      if len(glids) > 0:
        busy = wx.BusyInfo("Exporting from folder " + fnames[i])
        self.export_folder(export_directory, fids[i], fnames[i],
                           mboxes[i], glids)
        busy = None
      else:
        self.logf.write("Folder " + fnames[i] + \
                        " has no selected messages " + "\n")
    self.logf.write("\n" + "########## ELAPSED TIME ##########" + "\n")
    self.logf.write(self.sw.elapsed_time() + "\n")
    self.logf.close()
    self.logf = open(os.path.join(self.export_directory, self.log_name))
    message = self.logf.read()
    self.logf.close()
    summary_info = info_window.InfoWindow(self, "Export Summary", message)
    summary_info.Show(True)
    summary_info.SetFocus()

  ######################################################################
  def message_summary_row (self, em, sha1_digest):
    merrors = []
    mlrow = message_log.MessageLogRow()
    for tag in ["From", "To", "Subject", "Date", "Message-ID"]:
      if em[tag]:
        if tag   == "From":
          mlrow.add_from(em[tag])
        elif tag == "To":
          mlrow.add_to(em[tag])
        elif tag == "Subject":
          mlrow.add_subject(em[tag])
        elif tag == "Date":
          mlrow.add_date(em[tag])
        elif tag == "Message-ID":
          mlrow.add_messageid(em[tag])
      else:
        merrors.append(tag + " header is missing")
    mlrow.add_hash(sha1_digest)
    mlrow.add_errors(len(merrors))
    if len(merrors) > 0:
      mlrow.add_firstmessage(merrors[0])
    return mlrow

  ######################################################################
  def export_folder (self, export_directory, fid, fname, mbox, glids):
    exp_fdir = os.path.join(export_directory,
        os.path.normcase(os.path.dirname(fname)))
    if os.path.dirname(fname):
      if not os.path.exists(exp_fdir):
        try:
          os.makedirs(exp_fdir)
        except Exception as e:
          self.logf.write("Failed to create directory " + \
                          exp_fdir + "\n")
          self.logf.write(str(e) + "\n")
          return
    mlfn = os.path.join(export_directory, fname + ".csv")
    mlog = open(mlfn, "w", errors="replace")
    mlf = message_log.MessageLog(mlog)
    exf = open(os.path.join(export_directory, fname + ".mbox"),
        "w", errors="replace")
    mb = mailbox.mbox(mbox, create=False)
    exported_msgs = 0
    for i in range(len(mb)):
      mes = mb.get_message(i)
      (mes_string, errors) = dmc.get_message_as_string(mes)
      if mes_string is None:
        for er in errors:
          self.logf.write(er + "\n")
      else:
        (eol, sha1_digest) = dmc.message_hash(mes_string)
        em = email.message_from_string(mes_string)
        glid = em.get("Message-ID")
        if glid in glids:
          # "From "-line minus "From " and minus newline
          fl =  mes.get_from()
          exf.write("From " + fl + os.linesep)
          exf.write(mes_string)
          exf.write(os.linesep)
          exported_msgs += 1
          mlf.writerow(self.message_summary_row(em, sha1_digest))
    exf.close()
    mlog.close()
    self.logf.write("Folder " + fname + ": " + \
        str(exported_msgs) + " messages exported" + "\n")

  ####################################################################
  def LogVariables (self):
    self.logf.write("########## SETTINGS ##########" + "\n")
    self.logf.write("account: " + self.account + "\n")
    self.logf.write("account directory: " + self.account_dir + "\n")
    self.logf.write("export directory: " + self.export_directory + "\n")
    idx = self.name2component["folder_select"].GetSelection()
    self.logf.write("folder(s): " + self.fnames[idx-1] + "\n")
    include = self.name2component["include_rb"].GetValue()
    if include:
      self.logf.write("Export INCLUDES selected messages" + "\n")
    else:
      self.logf.write("Export EXCLUDES selected messages" + "\n")
    self.logf.write("\n" + "########## SUMMARY ##########" + "\n")

  ######################################################################
  def make_form (self):
    
    box1 = wx.StaticBoxSizer(wx.StaticBox(self), wx.VERTICAL)

    ##
    ## account name and directory
    ##

    account_grid = wx.FlexGridSizer(cols=2)
    self.name2component["static_account"] = grid_account = \
        wx.StaticText(self,
        label=self.account, name="static_account",
        style=wx.SIMPLE_BORDER)
    self.name2component["static_account_dir"] = \
        grid_account_dir = wx.StaticText(self,
        label=self.account_dir,
	name="static_account_dir", style=wx.SIMPLE_BORDER)
    t1 = wx.StaticText(self, label="Account:")
    account_grid.Add(t1, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    account_grid.Add(grid_account, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    t1 = wx.StaticText(self, label="Account Directory:")
    account_grid.Add(t1, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    account_grid.Add(grid_account_dir, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add(account_grid, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add((FRAME_WIDTH-30, 15))

    ##
    ## folders
    ##

    fsbox = self.fsbox = wx.BoxSizer(wx.HORIZONTAL)
    fsbox.Add(wx.StaticText(self, label="Select one or all folders:"),
        0, wx.ALIGN_LEFT|wx.ALL, 5)
    self.name2component["folder_select"] = fs = \
        wx.ComboBox(self, style=wx.CB_DROPDOWN,
#        choices=["[ALL FOLDERS"], name="folder_select")
        name="folder_select")
    fsbox.Add(fs, 0, wx.ALIGN_LEFT|wx.LEFT|wx.BOTTOM, 5)
    box1.Add(fsbox, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    box1.Add((FRAME_WIDTH-30, 15))

    ##
    ## export directory
    ##

    box1.Add(wx.StaticText(self, label="Export Directory:"),
        0, wx.ALIGN_LEFT|wx.LEFT, 5)
    self.name2component["export_directory"] = dpc = \
        DirPickerCtrl(self, size=(500,-1),
        style=wx.DIRP_DEFAULT_STYLE,
#        style=wx.DIRP_DIR_MUST_EXIST,
#        style=wx.DIRP_USE_TEXTCTRL|wx.DIRP_DIR_MUST_EXIST,
        name="export_directory")
    box1.Add(dpc, 0, wx.ALIGN_LEFT|wx.TOP|wx.LEFT, 5)
    box1.Add((FRAME_WIDTH-30, 15))

    ##
    ## include or exclude
    ##
    
    box1.Add(wx.StaticText(self, label="Messages to be exported:"),
        0, wx.ALIGN_LEFT|wx.ALL, 5)
    hbox = wx.BoxSizer(wx.HORIZONTAL)
    self.name2component["include_rb"] = self.include_rb = \
        wx.RadioButton(self, name="include_rb", \
	label="INCLUDE selected messages",
        style=wx.RB_GROUP)
    self.name2component["exclude_rb"] = self.exclude_rb = \
        wx.RadioButton(self, name="exclude_rb", \
	label="EXCLUDE selected messages")
    hbox = wx.BoxSizer(wx.HORIZONTAL)
    hbox.Add((30, -1))
    hbox.Add(self.include_rb, 0, wx.ALIGN_LEFT|wx.RIGHT, 5)
    hbox.Add(self.exclude_rb, 0, wx.ALIGN_LEFT|wx.LEFT, 5)
    box1.Add(hbox, 0, wx.ALIGN_LEFT|wx.LEFT|wx.TOP, 5)
    box1.Add((FRAME_WIDTH-30, 15))

    return box1
