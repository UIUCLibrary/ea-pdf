#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import re
import wx
import wx.lib.scrolledpanel as scrolled
import wx.html as wxhtml
import db_access as dba
import dm_common as dmc
import dm_wx
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT
import paging
import message_group

GRID_GAP=(5,5)

####################################################################
## Accounts
####################################################################
class Accounts (wx.Panel):

  account_id = None
  acount_name = None
  account_directory = None
  
  ####################################################################
  def __init__ (self, browse):
    wx.Panel.__init__ (self, browse)
    self.browse = browse
    self.cnx  = None
    self.sizer = wx.BoxSizer(wx.VERTICAL)
    self.null_account_info()    
    self.SetSizer(self.sizer)
    self.Layout()
    self.normal_font_size = self.GetFont().GetPointSize()
    self.bigger_font_size = self.normal_font_size + 3
    # Apparently, EVT_SHOW is NOT triggered when hitting a
    # tab in a notbook... So can't use the method of updating
    # the folder_select components used in delete_panel and export_panel.
    
  ####################################################################
  def account_is_set (self):
    return self.account_id is not None
  
  ####################################################################
  def get_account (self):
    return (self.account_id, self.account_name,
            self.account_directory)
  
  ####################################################################
  def set_account (self, aid, aname, adir):
    self.account_id = aid
    self.account_name = aname
    self.account_directory = adir
    self.browse.rsp.delete_obsolete_pages (self.account_id)
    self.browse.msp.OnPageSelect()
    
  ####################################################################
  def OnPageSelect (self):
    self.sizer.Clear(delete_windows=True)
    for child in self.sizer.GetChildren():
      self.sizer.Remove(child)
    if self.account_is_set():
      (aid, aname, adir) = self.get_account()
      self.account_info(dba.DbAccount(self.cnx, aid))
    else:
      self.null_account_info()
    self.SetSizer(self.sizer)
    self.Layout()
    
####################################################################
  def null_account_info (self):
    t1 = wx.StaticText(self, label="No accounts in database")      
    self.sizer.Add((FRAME_WIDTH-30, 10))
    self.sizer.Add(t1, 1, wx.LEFT|wx.TOP, 10)
    
####################################################################
  def account_info (self, acc):
    self.sizer.Add((FRAME_WIDTH, 5))
    self.sizer.Add(self.make_acc_attrs(acc), 1, wx.EXPAND|wx.LEFT, 5)
    self.sizer.Add((FRAME_WIDTH, 5))
    aname = wx.StaticText(self, label="Folders")
    aname.SetFont(wx.Font(self.normal_font_size, wx.SWISS, wx.NORMAL, wx.BOLD))
    self.sizer.Add(aname, 0, wx.ALIGN_CENTER)
    self.sizer.Add(self.make_folder_list(acc), 1, wx.EXPAND|wx.LEFT, 5)
    self.sizer.Add((FRAME_WIDTH, 5))

  ####################################################################
  def make_acc_attrs (self, acc):
    attributes = [
      ("Account&nbsp;name", acc.account_name),
      ("Id", str(acc.account_id)),
      ("Account&nbsp;directory", acc.account_directory),
      ("Message&nbsp;count", "{:,}".format(acc.message_count)),
      ("First&nbsp;message&nbsp;date", str(acc.start_date)),
      ("Last&nbsp;message&nbsp;date", str(acc.end_date)),
      ("'From'&nbsp;addresses", "{:,}".format(acc.from_count)),
      ("'To'&nbsp;addresses", "{:,}".format(acc.to_count)),
      ("'Cc'&nbsp;addresses", "{:,}".format(acc.cc_count)),
      ("'Bcc'&nbsp;addresses", "{:,}".format(acc.bcc_count)),
      ("External&nbsp;content&nbsp;files", "{:,}".format(acc.external_files))
    ]
    s = '<table border="0" cellpadding="1" cellspacing="1">'
    for (name, value) in attributes:
      s = s + "<tr><td><b>" + name + "</b></td><td>" + value + "</td></tr>"
    s = s + "</table>"
    h = wxhtml.HtmlWindow(self, size=(FRAME_WIDTH-50, 240))
    h.SetPage(s)
    return h

  ####################################################################
  def make_folder_list (self, acc):
    flc = wx.ListView(self,
        size=(FRAME_WIDTH-50, 240),
        style=wx.LC_REPORT|wx.LC_SINGLE_SEL)
    flc.InsertColumn(0, "Id",  width=60)
    flc.InsertColumn(1, "Name", width=350)
    flc.InsertColumn(2, "Messages", width=60)
    flc.InsertColumn(3, "Start Date", width=90)
    flc.InsertColumn(4, "End Date", width=90)
    for fld in acc.folders:
      flc.Append((fld.folder_id, fld.folder_name,
                  "{:,}".format(fld.message_count),
                  fld.start_date, fld.end_date))
    return flc
