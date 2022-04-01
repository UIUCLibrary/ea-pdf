#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import wx
import wx.lib.scrolledpanel as scrolled
import dm_common as dmc
import dm_wx
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT, FRAME_XPOS, FRAME_YPOS

######################################################################
class InfoPanel(scrolled.ScrolledPanel):

  ####################################################################
  def __init__ (self, parent, information):
    wx.ScrolledWindow.__init__ (self, parent=parent)

    txt = wx.TextCtrl(self, style=wx.TE_READONLY|wx.TE_MULTILINE)
    txt.SetValue(information)

    scr_sizer = wx.BoxSizer(wx.VERTICAL)
    scr_sizer.Add(txt, 1, wx.EXPAND)
    self.SetupScrolling()
    self.SetSizer(scr_sizer)

######################################################################
class InfoWindow(wx.Frame):

  ####################################################################
  def __init__ (self, parent, title, information):
    wx.Frame.__init__(self, parent, -1, title,
        pos=(FRAME_XPOS+100, FRAME_YPOS+100), size=(500, 350))

    self.parent = parent

    self.qb = wx.Button(self, label="Close", name="quit_button")
    self.qb.Bind(wx.EVT_BUTTON, self.ExecuteExit)

    scrp = InfoPanel(parent=self, information=information)

    sizer = wx.BoxSizer(wx.VERTICAL)
    sizer.Add(self.qb, 0, wx.CENTER|wx.TOP|wx.BOTTOM, 10)
    sizer.Add(scrp, 1, wx.EXPAND|wx.ALL, 5)
    self.SetSizer(sizer)

  ####################################################################
  def ExecuteExit (self, event):
    self.parent.SetFocus()
    self.Close()
