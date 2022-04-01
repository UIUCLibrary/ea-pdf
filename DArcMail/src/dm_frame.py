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
import dm_defaults as dmd
import dm_wx
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT, FRAME_XPOS, FRAME_YPOS
import load_panel
import export_panel
import delete_panel
import browse_panel
import sqlite_connect_panel

######################################################################
class DMFrame(wx.Frame):

  ####################################################################
  def __init__(self, parent, title):
    wx.Frame.__init__(self, parent, -1, title,
        pos=(FRAME_XPOS, FRAME_YPOS), size=(FRAME_WIDTH, FRAME_HEIGHT))

    self.qb = wx.Button(self, label="Quit", name="quit_button")
    self.qb.Bind(wx.EVT_BUTTON, self.ExecuteExit)

    self.gp = sqlite_connect_panel.LogonPanel(self)

    self.Bind(self.gp.EVT_LOGIN, self.login_handler)

    self.lb = wx.ToggleButton(self, label="Load")
    self.db = wx.ToggleButton(self, label="Delete")
    self.xb = wx.ToggleButton(self, label="Export")
    self.bb = wx.ToggleButton(self, label="Browse")

    self.lp = load_panel.LoadPanel(self)
    self.bp = browse_panel.BrowsePanel(self)
    self.xp = export_panel.ExportPanel(self)
    self.dp = delete_panel.DeletePanel(self)

    self.bs_and_ps = [
      (self.lb, self.lp),
      (self.db, self.dp),
      (self.xb, self.xp),
      (self.bb, self.bp)
    ]

    self.buttons = wx.BoxSizer(wx.HORIZONTAL)    
    self.buttons.Add(self.qb, 0, wx.ALL, 3)
    for (b, p) in self.bs_and_ps:
      self.Bind(wx.EVT_TOGGLEBUTTON, self.OnToggle, b)
      self.buttons.Add(b, 0, wx.ALL, 3)

    self.top_sizer = wx.BoxSizer(orient=wx.VERTICAL)
    self.top_sizer.Add(self.buttons, 0, wx.LEFT|wx.TOP, 3)
    self.top_sizer.Add(self.gp, 1, wx.EXPAND)
    for (b, p) in self.bs_and_ps:
      self.top_sizer.Add(p, 1, wx.EXPAND)
      b.Hide()
      p.Hide()
    self.gp.Show()
    self.SetSizer(self.top_sizer)
    self.Layout()

  ####################################################################
  def set_account (self):
    accounts = dba.loaded_account_info(self.gp.cnx)
    if len(accounts) == 1:
      (aid, aname, adir) = accounts[0]
      self.bp.acp.set_account(aid, aname, adir)
      
  ####################################################################
  def login_handler (self, event):
    for p in [self.lp, self.dp, self.bp, self.xp,
        self.bp.acp, self.bp.msp, self.bp.asp, self.bp.rsp]:
      p.cnx = self.gp.cnx
      # give all pages access to accounts.
      if p != self.bp.acp:
        p.acp = self.bp.acp
    self.set_account()    
    # have to force the data load on accounts page here
    self.bp.acp.OnPageSelect()
    self.gp.Hide()
    for (b, p) in self.bs_and_ps:
      b.Show()
    self.lp.Show()
    self.lb.SetValue(True)
    self.Layout()
    self.SetFocus()

  ####################################################################
  def OnToggle (self, event):
    b0 = event.GetEventObject()
    if (b0 == self.xb or b0 == self.db) and \
         not self.bp.acp.account_is_set():
      md = wx.MessageDialog(parent=self,
          message="Database file does not have an account " + \
          "to delete or export.",
          caption="No account loaded",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      b0.SetValue(False)
    else:
      for (b, p) in self.bs_and_ps:
        if b != b0:
          b.SetValue(False)
          p.Hide()
        else:
          p.Show()
      b0.SetValue(True)
      self.Layout()

  ####################################################################
  def ExecuteExit (self, event):
    self.Close()

######################################################################
