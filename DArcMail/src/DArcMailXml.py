#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import time
import sys
import os
import wx
sys.path.append(os.path.join(os.path.dirname(os.path.realpath(__file__)),
    "lib"))
if "." not in sys.path:
  sys.path.append(".")
import dmxml_frame
import dm_defaults as dmd

######################################################################
class DArcMailXml(wx.App):
  def OnInit(self):
    frame = dmxml_frame.DMXmlFrame(None, "DArcMailXml " + dmd.VERSION)
    self.SetTopWindow(frame)
    frame.Show(True)
    return True

######################################################################
#app = DArcMailXml(redirect=True)
app = DArcMailXml(redirect=False)
app.MainLoop()

