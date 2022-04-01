#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import wx
import wx.html as wxhtml
import db_access as dba
import dm_common as dmc
import dm_wx
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT
import message_list
import message_search

####################################################################
## AddressInfo
####################################################################
class AddressInfo (wx.Panel):
  def __init__ (self, browse, results_notebook, cnx, data):
    wx.Panel.__init__ (self, parent=results_notebook)
    self.browse = browse
    self.acp = self.browse.acp
    self.results_notebook = results_notebook
    self.results = results_notebook.GetParent()
    self.cnx  = cnx
    self.addr = data
    self.normal_font_size = self.GetFont().GetPointSize()
    self.bigger_font_size = self.normal_font_size + 3

    self.addr_attrs = self.make_addr_attrs()

    sizer = wx.BoxSizer(wx.VERTICAL)
    sizer.Add((FRAME_WIDTH, 10))

    sizer.Add(self.addr_attrs, 1, wx.LEFT, 5)
    self.SetSizer(sizer)

    self.results.page_id = self.results.page_id + 1
    results_notebook.AddPage(self, str(self.results.page_id), select=True)
    self.browse.switch_to_results()

  ####################################################################
  def OnLinkClick (self, event):
    info = event.GetLinkInfo()
    tag = None
    href = info.GetHref()
    search_params = message_search.SearchParams()
    if href   == "from_msgs":
      tag = "From"
      search_params = message_search.SearchParams(from_line=self.addr.address)
    elif href == "to_msgs":
      tag = "To"
      search_params = message_search.SearchParams(to_line=self.addr.address)
    elif href == "cc_msgs":
      tag = "Cc"
      search_params = message_search.SearchParams(cc_line=self.addr.address)
    elif href == "bcc_msgs":
      tag = "Bcc"
      search_params = message_search.SearchParams(bcc_line=self.addr.address)
    if tag:
      message_info = \
          dba.search_message_for_address(self.cnx, self.addr.account_id,
                                         self.addr.address_id,
                                         tag)
      self.results.page_id = self.results.page_id + 1
      page_name = str(self.results.page_id)
      message_list.MessageList(self.browse, self.acp, self.results_notebook,
          self.cnx, message_info, search_params)
      self.browse.switch_to_results()

  ####################################################################
  def make_addr_attrs (self):
    attributes = [
      ("Account&nbsp;name", dmc.escape_html(self.addr.account_name)),
      ("Email&nbsp;address", dmc.escape_html(self.addr.address)),
      ("Email&nbsp;address&nbsp;Id", str(self.addr.address_id)),

        ("Email&nbsp;name", dmc.escape_html(self.addr.email_name))
        if self.addr.name_id else
        ("Email&nbsp;names", dmc.escape_html("; ".join(self.addr.email_names))),

      ("'From'&nbsp;messages",
        '<a href="from_msgs">' + "{:,}".format(self.addr.from_msgs) + "</a>"
        if self.addr.from_msgs else
        "{:,}".format(self.addr.from_msgs)
      ),

      ("'To'&nbsp;messages",
        '<a href="to_msgs">' + "{:,}".format(self.addr.to_msgs) + "</a>"
        if self.addr.to_msgs else
        "{:,}".format(self.addr.to_msgs)
      ),

      ("'Cc'&nbsp;messages",
        '<a href="cc_msgs">' + "{:,}".format(self.addr.cc_msgs) + "</a>"
        if self.addr.cc_msgs else
        "{:,}".format(self.addr.cc_msgs)
      ),

      ("'Bcc'&nbsp;messages",
        '<a href="bcc_msgs">' + "{:,}".format(self.addr.bcc_msgs) + "</a>"
        if self.addr.bcc_msgs else
        "{:,}".format(self.addr.bcc_msgs)
      )
    ]
    s = '<table border="0" cellpadding="1" cellspacing="1">'
    for (name, value) in attributes:
      s = s + "<tr><td><b>" + name + "</b></td><td>" + value + "</td></tr>"
    s = s + "</table>"
    h = wxhtml.HtmlWindow(self, size=(FRAME_WIDTH-50, 240))
    h.SetPage(s)
    self.Bind(wxhtml.EVT_HTML_LINK_CLICKED, self.OnLinkClick, h)
    return h

    
