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
import db_access as dba
import dm_common as dmc
import dm_wx
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT
import delete_data
import stop_watch
import info_window
import folder_data as fld

####################################################################
## DeletePanel
####################################################################
class DeletePanel (scrolled.ScrolledPanel):
  name2component = {}
  name2default   = {
    "folder_select"    : 0
  }
  log_name        = "dm_delete.log.txt"

  ####################################################################
  def __init__ (self, parent):

    wx.ScrolledWindow.__init__ (self, parent=parent)

    self.name2component  = {}
    self.account         = ""
    self.account_dir     = ""
    self.account_id      = None
    self.logf            = None
    self.cnx             = None
    self.parent          = parent
    self.browse          = parent.bp
    
    normal_font_size = self.GetFont().GetPointSize()  # get the current size
    bigger_font_size = normal_font_size + 3

    sizer = wx.BoxSizer(orient=wx.VERTICAL)
    self.SetSizer(sizer)
    self.SetupScrolling()
    
    sizer.Add((FRAME_WIDTH-30, 10))

    t1 = wx.StaticText(self, label="Delete DArcMail Account")
    t1.SetFont(wx.Font(bigger_font_size, wx.SWISS, wx.NORMAL, wx.BOLD))
    sizer.Add(t1, 0, wx.ALIGN_CENTER, 5)
    
    sizer.Add((FRAME_WIDTH-30, 15))
    sizer.Add(self.make_form(), 0, wx.ALIGN_LEFT|wx.ALL, 5)
    
#    sizer.Add((FRAME_WIDTH-30, 15))

    ##
    ## action buttons
    ##
    
    self.name2component["go_button"] = gb = \
        wx.Button(self, label="Delete")
    self.name2component["go_button"].Bind(wx.EVT_BUTTON,
        self.ValidateVariablesAndGo)
    sizer.Add(gb, 0, wx.ALIGN_CENTER, 5)

    self.Bind(wx.EVT_SHOW, self.NewDelete)
    self.Layout()

  ####################################################################
  def NewDelete (self, event):
    if not self.IsShown():
      return
    if not self.cnx:
      self.cnx = self.browse.cnx
    self.make_account_header()
    self.make_folder_select()
    self.Layout()

  ####################################################################
  def get_folders (self):
    fids   = [0]
    fnames = ["[ALL FOLDERS]"]
    mboxes = [""]
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
  def make_account_header (self):
    if self.acp.account_is_set():
      (self.account_id, self.account, self.account_dir) = \
          self.acp.get_account()
      self.name2component["static_account"]. \
          SetLabel(self.account)
      self.name2component["static_account_dir"]. \
          SetLabel(self.account_dir)

  ####################################################################
  def ExecuteExit (self, event):
    # self is a
    #   wx.Panel within a
    #       wx.Notebook within a
    #             wx.Frame
    self.GetParent().GetParent().Close()

  ####################################################################
  def LogVariables(self):
    self.logf.write("########## SETTINGS ##########\n")
    self.logf.write("account: " + self.account + "\n")
    self.logf.write("account directory: " + self.account_dir + "\n")
    self.logf.write("\n")

  ####################################################################
  def ValidateVariablesAndGo(self, event):
    ready = True
    fs = self.name2component["folder_select"]
    idx = fs.GetSelection()
    fname = fs.GetString(idx)
    if ready:
      self.logf = open(os.path.join(self.account_dir, self.log_name),
          "w", errors="replace")
      self.LogVariables()
      self.delete_data(idx, fname)
      self.SetFocus()

  ######################################################################
  def delete_data (self, idx, fname):
    sw = stop_watch.StopWatch()

    dd = delete_data.DeleteData(self.cnx, self.account,
                                self.account_dir, self.account_id)
    folders = dba.get_folders_for_account(self.cnx, self.account_id)

    disable_all = wx.WindowDisabler()
    busy = wx.BusyInfo("Deleting data for account " + \
        str(self.account))
    ## need the call to Yield to get the busy box painted
    wx.GetApp().Yield()
    for dbf in folders:
      if idx == 0 or dbf.folder_name == fname:
        dd.delete_one_folder(dbf)
        self.cnx.commit()
        self.logf.write("deleted folder " + dbf.folder_name + "\n")
    folders = dba.get_folders_for_account(self.cnx, self.account_id)
    if len(folders) == 0:
      dd.delete_addresses_and_names()
      dba.delete_account(self.cnx, self.account_id)
      self.acp.set_account(None, None, None)
    self.cnx.commit()
    self.acp.OnPageSelect()
    del busy
    del disable_all

    del dd

    self.logf.write("\n" + "########## ELAPSED TIME ##########\n")
    self.logf.write(sw.elapsed_time() + "\n")

    self.logf.close()
    self.logf = open(os.path.join(self.account_dir, self.log_name))
    message = self.logf.read()
    self.logf.close()
    summary_info = info_window.InfoWindow(self, "Delete Summary", message)
    summary_info.Show(True)
    summary_info.SetFocus()

    self.account         = None
    self.account_dir     = None
    self.account_id      = None

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
  def NewDelete (self, event):
    if not self.IsShown():
      return
    if not self.cnx:
      self.cnx = self.browse.cnx
    (account_id, account, account_dir) = \
        self.acp.get_account()
    if self.account_id != account_id:
      self.make_account_header()
      self.make_folder_select()
      self.name2component["folder_select"]. \
          SetSelection(self.name2default["folder_select"])
    self.Layout()

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
    
    return box1
