 #!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import wx
import db_access as dba
import dm_common as dmc
import message_search
import address_search
import results_panel
import account

####################################################################
## BrowsePanel
####################################################################
class BrowsePanel (wx.Panel):
  cnx                  = None

  ####################################################################
  def __init__ (self, parent):

    wx.Panel.__init__ (self, parent=parent)

    normal_font_size = self.GetFont().GetPointSize()  # get the current size
    bigger_font_size = normal_font_size + 3

    self.nb = wx.Notebook(self)
    self.acp = account.Accounts(self.nb)
    self.msp = message_search.MessageSearch(self.nb)
    self.asp = address_search.AddressSearch(self.nb)
    self.rsp = results_panel.ResultsPanel(self.nb)
    self.nb.AddPage(self.acp, "Account")       # 0
    self.nb.AddPage(self.msp, "Message")       # 1
    self.nb.AddPage(self.asp, "Address/Name")  # 2
    self.nb.AddPage(self.rsp, "Results")       # 3
    for p in [self.acp, self.msp, self.asp, self.rsp]:
      p.browse           = self
      p.browse_notebook  = self.nb
      p.results          = self.rsp
      p.results_notebook = self.rsp.nb
    sizer = wx.BoxSizer(orient=wx.VERTICAL)
    sizer.Add(self.nb, 1, wx.EXPAND)
    self.SetSizer(sizer)

  ####################################################################
  def OnPageChange (self, event):
    if self.nb.GetSelection() == 0:
      self.acp.OnPageSelect()
    
  ####################################################################
  def switch_to_results (self):
    self.nb.SetSelection(3)
    self.rsp.SetFocus()

  ####################################################################
  def switch_to_account_search (self):
    self.nb.SetSelection(0)
