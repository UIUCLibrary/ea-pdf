#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import os
import wx
import wx.html as wxhtml
import base64
import quopri
import db_access as dba
import dm_common as dmc
import dm_wx
from dm_wx import FRAME_WIDTH, FRAME_HEIGHT
import view_content
import message_list
import message_search
import info_window

viewable_types = ["text/plain", "text/html", "message/rfc822"]

####################################################################
## MessageInfo
####################################################################
class MessageInfo (wx.Panel):

  ####################################################################
  def __init__ (self, browse, results_notebook, cnx, data, search_term=None):
    wx.Panel.__init__ (self, parent=results_notebook)
    self.browse = browse
    self.results_notebook = results_notebook
    self.results = results_notebook.GetParent()
    self.cnx  = cnx
    self.msg = data
    self.search_term = search_term
    self.normal_font_size = self.GetFont().GetPointSize()
    self.bigger_font_size = self.normal_font_size + 3

    self.msg_attrs = self.make_msg_attrs()
    self.bplc      = self.body_part_list()

    sizer = wx.BoxSizer(wx.VERTICAL)
    sizer.Add((FRAME_WIDTH, 10))
    sizer.Add(self.msg_attrs, 1, wx.LEFT, 5)
    sizer.Add((FRAME_WIDTH, 10))
    aname = wx.StaticText(self, label="Message Content")
    aname.SetFont(wx.Font(self.normal_font_size, wx.SWISS, wx.NORMAL, wx.BOLD))
    sizer.Add(aname, 0, wx.ALIGN_CENTER)
    sizer.Add((FRAME_WIDTH, 10))
    sizer.Add(self.bplc, 1, wx.LEFT, 5)
    self.SetSizer(sizer)

    self.results.page_id = self.results.page_id + 1
    results_notebook.AddPage(self, str(self.results.page_id), select=True)
    self.browse.switch_to_results()

  ####################################################################
  def OnLinkClick (self, event):
    info = event.GetLinkInfo()
    href = info.GetHref()
    if href == "get_reply_to":
      self.InReplyClick(event)
    elif href == "get_reply":
      self.HasReplyClick(event)

  ####################################################################
  def OnBodyPartClick (self, event):
    r = event.Index
    action  = self.bplc.GetItem(r, 0).GetText()
    if action == "-":
      self.bplc.Select(r, False)
      return
    content_data = self.msg.body_info[r]
    part_id      = content_data.sequence_id
    if action == "show path":
      acc_dir = dba.get_account_directory(self.cnx, self.msg.account_id)
      path = os.path.abspath(
          os.path.join(acc_dir, os.path.dirname(self.msg.folder_name),
	      content_data.stored_file_name))
      msg = "External file path for message id = " + \
          str(self.msg.message_id) + \
          ", part id = " + str(part_id)  + "\n\n" + path
      path_window = info_window.InfoWindow(self, "External File Path", msg)
      path_window.Show(True)
      path_window.SetFocus()
    elif action == "download":
      self.download_content(content_data)
    elif action == "view":
      view_content.ViewContent(self.browse, self.results_notebook,
          self.cnx, content_data, self.search_term)

  ####################################################################
  def download_content (self, content_data):
    # content_data is a DbContent
    ctype = content_data.content_type
    trenc = content_data.transfer_encoding
    isatt = content_data.is_attachment
    ofn   = content_data.original_file_name
    content_id = content_data.content_id
    fd = wx.FileDialog(self, message="Select name for downloaded file",
        style=wx.FD_SAVE|wx.FD_OVERWRITE_PROMPT)
    if ofn:
      fd.SetFilename(ofn)
    retcode = fd.ShowModal()
    if retcode == wx.ID_CANCEL:
      return
    output = open(fd.GetPath(), "wb")
    input  = None
    if content_data.is_internal:
      cnt   = dba.get_internal_content_text(self.cnx, content_id)
      if trenc == "base64":
        cnt = base64.b64decode(cnt)
      elif trenc == "quoted-printable":
        cnt = quopri.decodestring(cnt)
      output.write(cnt)
    else:
      acc_dir = dba.get_account_directory(self.cnx, self.msg.account_id)
      path = os.path.join(acc_dir, os.path.dirname(self.msg.folder_name),
          content_data.stored_file_name)
      input = open(path)
      if not content_data.xml_wrapped:
        if trenc == "base64":
          base64.decode(input, output)
        elif trenc == "quoted-printable":
          quopri.decode(input, output)
        else:
          output.write(input.read())
      else:
        output.write(input.read())
      input.close()
    output.close()

  ####################################################################
  def InReplyClick (self, event):
    MessageInfo(self.browse, self.results_notebook,
        self.cnx, dba.DbMessage(self.cnx, self.msg.reply_to_id))
    self.browse.switch_to_results()

  ####################################################################
  def HasReplyClick (self, event):
    if len(self.msg.has_reply_id) == 1:
      MessageInfo(self.browse, self.results_notebook,
          self.cnx, dba.DbMessage(self.cnx, self.msg.has_reply_id[0]))
      self.browse.switch_to_results()
    else:
      message_info = dba.search_message_for_ids(self.cnx, self.msg.has_reply_id)
      self.results.page_id = self.results.page_id + 1
      page_name = str(self.results.page_id)
      search_params = message_search.SearchParams(replies=self.msg.global_id)
      message_list.MessageList(self.browse, self.results_notebook,
          page_name, self.cnx, message_info, search_params)
      self.browse.switch_to_results()

  ####################################################################
  def body_part_list (self):
    acc_dir = dba.get_account_directory(self.cnx, self.msg.account_id)
    fld_dir = os.path.join(acc_dir, os.path.dirname(self.msg.folder_name))
    bplc = wx.ListView(self,
        style=wx.LC_REPORT|wx.LC_SINGLE_SEL)
    bplc.InsertColumn(0, "Action", width=75)
    bplc.InsertColumn(1, "Storage", width=60)
    bplc.InsertColumn(2, "Content-Type",  width=100)
    bplc.InsertColumn(3, "Length", width=100)
    bplc.InsertColumn(4, "Original Name", width=250)
    bplc.InsertColumn(5, "Part Id",  width=40)
    self.Bind(wx.EVT_LIST_ITEM_SELECTED, self.OnBodyPartClick, bplc)

    for b in self.msg.body_info:
      storage = "internal" if b.is_internal else "external"
      ofn     = b.original_file_name if b.original_file_name else "-"
      action = "-"
      if storage == "external":
        if os.path.exists(os.path.join(fld_dir, b.stored_file_name)):
          if b.content_type in viewable_types:
            action = "view"
          else:
            action = "download"
        else:
          action = "show path"
      else:
        if b.content_type in viewable_types:
          action = "view"
        else:
          action = "download"
      bplc.Append((action, storage, b.content_type,
          "{:,}".format(b.content_length),
          ofn, b.sequence_id))
    return bplc

  ####################################################################
  def make_msg_attrs (self):
    attributes = [
      ("Id", str(self.msg.message_id)),
      ("Account", dmc.escape_html(self.msg.account_name)),
      ("Folder", dmc.escape_html(self.msg.folder_name)),
      ("Global&nbsp;Id", dmc.escape_html(self.msg.global_id)),
      ("Date", self.msg.date_line),
      ("Subject", dmc.escape_html(self.msg.subject_line)),
      ("From", dmc.escape_html(self.msg.from_line)),
      ("To", dmc.escape_html(self.msg.to_line))
    ]
    if self.msg.cc_line:
      attributes.append(("Cc", dmc.escape_html(self.msg.cc_line)))
    if self.msg.bcc_line:
      attributes.append(("Bcc", dmc.escape_html(self.msg.bcc_line)))
    if self.msg.reply_to_global:
      name = "In-Reply-To"
      value = dmc.escape_html(self.msg.reply_to_global)
      if self.msg.reply_to_id:
        value = '<a href="get_reply_to">' + value + "</a>"
      attributes.append((name, value))
    nr = len(self.msg.has_reply_global)
    if nr > 0:
      name = "Has&nbsp;Reply"
      value = "Get Reply"
      if nr > 1:
        name = "Has&nbsp;Replies"
        value = "Get Replies"
      value = '<a href="get_reply">' + value + "</a>"
      attributes.append((name, value))

    s = '<table border="0" cellpadding="1" cellspacing="1">'
    for (name, value) in attributes:
      if name and value:
        s = s + "<tr><td><b>" + name + "</b></td><td>" + value + "</td></tr>"
      elif name:
        s = s + '<tr><td colspan="2" align="left">' + name + "</td></tr>"
      elif value:
        s = s + '<tr><td colspan="2" align="center">' + name + "</td></tr>"

    s = s + "</table>"
    h = wxhtml.HtmlWindow(self, size=(FRAME_WIDTH, 240))
    h.SetPage(s)
    self.Bind(wxhtml.EVT_HTML_LINK_CLICKED, self.OnLinkClick, h)
    return h


