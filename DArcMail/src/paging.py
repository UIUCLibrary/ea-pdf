#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import wx
import dm_wx

######################################################################
class PagingInfo ():
  def __init__ (self, total_rows):
    self.current_page = 1
    self.current_low  = 1
    self.total_rows   = total_rows
    self.total_pages = int(self.total_rows / dm_wx.MAX_LIST_SIZE)
    if self.total_rows % dm_wx.MAX_LIST_SIZE > 0:
      self.total_pages = self.total_pages + 1
    self.current_high = self.current_low + dm_wx.MAX_DISPLAYED_PAGE_LINKS - 1
    if self.current_high > self.total_pages:
      self.current_high = self.total_pages
    self.current_lo_i = (self.current_page-1)*dm_wx.MAX_LIST_SIZE
    self.current_hi_i = self.current_lo_i + dm_wx.MAX_LIST_SIZE
    if self.current_hi_i > self.total_rows:
      self.current_hi_i = self.total_rows

  ####################################################################
  def page_links_left (self, parent, page_button_handler):
    new_current_high = self.current_low - 1
    new_current_low  = new_current_high  - (dm_wx.MAX_DISPLAYED_PAGE_LINKS-1)
    if new_current_low < 1:
      new_current_low = 1
    self.current_high = new_current_high
    self.current_low  = new_current_low
    self.current_page = self.current_high
    self.current_lo_i = (self.current_page-1)*dm_wx.MAX_LIST_SIZE
    self.current_hi_i = self.current_lo_i + dm_wx.MAX_LIST_SIZE
    if self.current_hi_i > self.total_rows:
      self.current_hi_i = self.total_rows
    return self.page_links(self.current_page, parent, page_button_handler)

  ####################################################################
  def page_links_right (self, parent, page_button_handler):
    new_current_low  = self.current_high + 1
    new_current_high = new_current_low + (dm_wx.MAX_DISPLAYED_PAGE_LINKS-1)
    if new_current_high > self.total_pages:
      new_current_high = self.total_pages
    self.current_low = new_current_low
    self.current_high = new_current_high
    self.current_page = self.current_low
    self.current_lo_i = (self.current_page-1)*dm_wx.MAX_LIST_SIZE
    self.current_hi_i = self.current_lo_i + dm_wx.MAX_LIST_SIZE
    if self.current_hi_i > self.total_rows:
      self.current_hi_i = self.total_rows
    return self.page_links(self.current_page, parent, page_button_handler)

  ####################################################################
  def page_links (self, current_page, parent, page_button_handler):
    self.current_page = current_page
    self.current_lo_i = (self.current_page-1)*dm_wx.MAX_LIST_SIZE
    self.current_hi_i = self.current_lo_i + dm_wx.MAX_LIST_SIZE
    if self.current_hi_i > self.total_rows:
      self.current_hi_i = self.total_rows
    page_sizer = wx.BoxSizer(orient=wx.HORIZONTAL)
    page_sizer.Add((30,0))
    page_sizer.Add(wx.StaticText(parent,
        label='page (of ' + str(self.total_pages) + '):', name='page_label'),
        0, wx.ALL, 5)
    if self.current_low > 1:
      b = wx.Button(parent, label='<<', name='page_left', size=(40,-1))
      page_sizer.Add(b, 0, wx.ALIGN_LEFT|wx.ALL, 5)
      b.Bind(wx.EVT_BUTTON, page_button_handler)
    for p in range(self.current_low, self.current_high+1):
      w = 30 if p < 10 else 40 if p < 100 else 50
      b = wx.ToggleButton(parent, label=str(p),
          name='page_' + str(p), size=(w,-1))
      page_sizer.Add(b, 0, wx.ALIGN_LEFT|wx.ALL, 5)
      b.Bind(wx.EVT_TOGGLEBUTTON, page_button_handler)
      if p == self.current_page:
        b.SetValue(True)
        b.Refresh()
      else:
        b.SetValue(False)
        b.Refresh()
    if self.current_high < self.total_pages:
      b = wx.Button(parent, label='>>', name='page_right', size=(40,-1))
      page_sizer.Add(b, 0, wx.ALIGN_LEFT|wx.ALL, 5)
      b.Bind(wx.EVT_BUTTON, page_button_handler)
    return page_sizer
