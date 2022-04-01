#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import re
import wx
import  wx.lib.scrolledpanel as scrolled
import db_access as dba
import dm_common as dmc
import dm_wx
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT
import address_list

####################################################################
## AddressSearch
####################################################################
class AddressSearch (scrolled.ScrolledPanel):

  variable_names = [
    "address",
  ]
  name2default   = {
    "address"         : ""
  }
  name2component = {}
  cnx              = None
  browse           = None
  browse_notebook  = None
  results          = None
  results_notebook = None
  address          = None
 
  ####################################################################
  def __init__ (self, parent):

    wx.ScrolledWindow.__init__ (self, parent=parent)

    normal_font_size = self.GetFont().GetPointSize()  # get the current size
    bigger_font_size = normal_font_size + 3

    box = wx.StaticBoxSizer(wx.StaticBox(self), wx.VERTICAL)
    grid = wx.FlexGridSizer(cols=2)
    grid.Add(wx.StaticText(self, label="Search String"),
        0, wx.ALIGN_RIGHT|wx.TOP, 5)
    self.name2component["address"] = txc = \
          wx.TextCtrl(self, name="address", size=(200, -1))
    grid.Add(txc, 0, wx.ALIGN_LEFT|wx.LEFT|wx.TOP|wx.BOTTOM, 5)
    box.Add(grid, 0, wx.ALIGN_LEFT|wx.LEFT, 5)

    sz = wx.BoxSizer(orient=wx.VERTICAL)
    sz.Add((FRAME_WIDTH, 15))
    sz.Add(box, 0, wx.ALIGN_CENTER)
    sz.Add((FRAME_WIDTH, 15))
    sz.Add(dm_wx.ActionButtons(self, "Search for Address/Name"), 0, wx.ALIGN_CENTER, 5)

    self.SetSizer(sz)
    self.SetupScrolling()

    self.ResetVariables()

    self.name2component["reset_button"].Bind(wx.EVT_BUTTON, self.ExecuteReset)
    self.name2component["go_button"].Bind(wx.EVT_BUTTON, self.ValidateVariablesAndGo)

  ####################################################################
  def ResetVariables (self):
    for v in self.variable_names:
      self.name2component[v].SetValue(self.name2default[v])

  ####################################################################
  def ExecuteReset (self, event):
    self.ResetVariables()
    self.GetParent().SetFocus()

  ####################################################################
  def ValidateVariablesAndGo(self, event):
    ready = True
    if not self.acp.account_is_set():
      md = wx.MessageDialog(parent=self, message="Before searching for " + \
          "addresses or messages, you must load account data",
          caption="Default account not set",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
      ready = False
      self.browse.switch_to_account_search()
      return
    self.address = self.name2component["address"].GetValue().strip()
    if ready:
      self.search_address()

  ######################################################################
  def search_address (self):
    address_info = dba.search_address_name(self.cnx,
        self.acp.account_id, self.address)
    if len(address_info) == 0:
      md = wx.MessageDialog(parent=self,
          message="No address/name matching search string",
          caption="No data",
          style=wx.OK|wx.ICON_EXCLAMATION)
      retcode = md.ShowModal()
    else:
      self.results.page_id = self.results.page_id + 1
      address_list.AddressList(self.browse, self.acp,
                               self.results_notebook,
                               self.cnx, address_info)
      self.browse.switch_to_results()


