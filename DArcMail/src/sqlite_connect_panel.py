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
import dm_defaults as dmd
import wx.lib.newevent
import sqlite_schema

####################################################################
## LogonPanel
####################################################################
class LogonPanel (scrolled.ScrolledPanel):
  variable_names = [
    'database'
  ]
  name2default   = {
    'database'        : dmd.DEFAULT_DATABASE
  }
  name2component = {}

  cnx             = None
  database        = None
  
  ####################################################################
  def __init__ (self, parent):

    wx.ScrolledWindow.__init__ (self, parent=parent)

    normal_font_size = self.GetFont().GetPointSize()  # get the current size
    bigger_font_size = normal_font_size + 3

    sz = wx.BoxSizer(orient=wx.VERTICAL)
    sz.Add((FRAME_WIDTH, 10))

    t1 = wx.StaticText(self, label='Connect to DArcMail Database')
    t1.SetFont(wx.Font(bigger_font_size, wx.SWISS, wx.NORMAL, wx.BOLD))
    sz.Add(t1, 0, wx.ALIGN_CENTER, 5)

    sz.Add((FRAME_WIDTH, 15))

    t1 = wx.StaticText(self, label='Sqlite3 database file:')
    sz.Add(t1, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    self.name2component['database'] = fpc = \
        wx.FilePickerCtrl(self, size=(500,-1),
        style=wx.FLP_USE_TEXTCTRL, name='database')
    sz.Add(fpc, 0, wx.ALIGN_LEFT|wx.ALL, 5)
    sz.Add((FRAME_WIDTH, 10))

    sz.Add(dm_wx.ActionButtons(self, 'Connect'), 0, wx.ALIGN_CENTER, 5)
    sizer = wx.BoxSizer(orient=wx.HORIZONTAL)
    sizer.Add((20,FRAME_HEIGHT))
    sizer.Add(sz, 1, wx.EXPAND)
    self.SetSizer(sizer)
    self.SetupScrolling()

    self.ResetVariables()

    self.name2component['reset_button'].Bind(wx.EVT_BUTTON, self.ExecuteReset)
    self.name2component['go_button'].Bind(wx.EVT_BUTTON, self.ValidateVariablesAndGo)

    self.LoginEvent, self.EVT_LOGIN = wx.lib.newevent.NewEvent()

  ####################################################################
  def ResetVariables (self):
    for v in self.variable_names:
      if v == 'database':
        self.name2component[v].SetPath(self.name2default[v])

  ####################################################################
  def ExecuteReset (self, event):
    self.ResetVariables()
    self.GetParent().SetFocus()

  ####################################################################
  def ExecuteExit (self, event):
    # self is a
    #    LogonPanel within a
    #        wx.Frame

    self.GetParent().Close()

  ####################################################################
  def ValidateVariablesAndGo(self, event):
    ready = True
    db_path = self.name2component['database'].GetPath()
    if not db_path:
      msg = "You must specify the full path of an Sqlite3 database " + \
          "file;\nif it does not exist, it will be created."
      md = wx.MessageDialog(parent=self, message=msg, caption="Error",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      return
    if not os.path.exists(db_path):
      try:
        self.cnx = dba.sqlite_connect(db_path)
        sqlite_schema.run_create_table(self.cnx)
        sqlite_schema.run_load_tags(self.cnx)
        sqlite_schema.run_create_index(self.cnx)
      except Exception as e:
        ready = False
        print(e)
        sys.exit(0)
    else:
      try:
        self.cnx = dba.sqlite_connect(db_path)
      except Exception as e:
        ready = False
        print(e)
        sys.exit(0)
    ## loading & deleting are much slower with synchronous and journaling active
    ## The typical use case for an sqlite db in DArcMail should require neither
    ## synchronous nor journaling -- can always reload again from .mbox
    cursor = self.cnx.cursor()
    cursor.execute("pragma synchronous = OFF;")
    cursor.close()
    cursor = self.cnx.cursor()
    cursor.execute("pragma journal_mode = OFF;")
    cursor.close()
    if ready:
      evt = self.LoginEvent(evt_type='logged in')
      frame = self.GetParent()
      wx.PostEvent(frame, evt)
