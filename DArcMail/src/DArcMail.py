#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import sys
import os
import wx
sys.path.append(os.path.join(os.path.dirname(os.path.realpath(__file__)),
    "lib"))
if "." not in sys.path:
  sys.path.append(".")
import dm_frame
import dm_defaults as dmd

######################################################################
class DArcMail(wx.App):
  def OnInit(self):
    frame = dm_frame.DMFrame(None, "DArcMail " + dmd.VERSION)
    self.SetTopWindow(frame)
    frame.Show(True)
    return True

######################################################################
#app = DArcMail(redirect=True)
app = DArcMail(redirect=False)
app.MainLoop()

