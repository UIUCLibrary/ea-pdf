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
import dm_common as dmc
import dm_wx
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT, FRAME_XPOS, FRAME_YPOS
import dmxml_panel

######################################################################
class DMXmlFrame(wx.Frame):

  ####################################################################
  def __init__(self, parent, title):
    wx.Frame.__init__(self, parent, -1, title,
        pos=(FRAME_XPOS, FRAME_YPOS), size=(FRAME_WIDTH, FRAME_HEIGHT))

    self.qb = wx.Button(self, label="Quit", name="quit_button")
    self.qb.Bind(wx.EVT_BUTTON, self.ExecuteExit)

    self.xlp = dmxml_panel.DMXmlPanel(self)

    self.buttons = wx.BoxSizer(wx.HORIZONTAL)
    
    self.buttons.Add(self.qb, 0, wx.ALL, 3)

    self.top_sizer = wx.BoxSizer(orient=wx.VERTICAL)
    self.top_sizer.Add(self.buttons, 0, wx.LEFT|wx.TOP, 3)
    self.top_sizer.Add(self.xlp, 1, wx.EXPAND)
    self.xlp.Show()
    self.SetSizer(self.top_sizer)
    self.Layout()

  ####################################################################
  def ExecuteExit (self, event):
    self.Close()
