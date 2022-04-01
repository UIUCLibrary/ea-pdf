#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import time
import os
import re
import wx
from wx import DirPickerCtrl
import db_access as dba
import dm_common as dmc
import dm_defaults as dmd
import xml_common

FRAME_WIDTH  = 650
FRAME_HEIGHT = 700
FRAME_XPOS   = 250
FRAME_YPOS   = 150

LIST_RESULT_SIZE = (FRAME_WIDTH-50, FRAME_HEIGHT-150)

button_names = [
    "reset_button",
    "go_button",
    "default_button"
]

MAX_LIST_SIZE            = 30
MAX_DISPLAYED_PAGE_LINKS = 6

####################################################################
def ValidateDirectory (p, account_dir):
  if account_dir:
    p.account_dir = dmc.os_common_path(os.path.abspath(account_dir.strip()))
    if not os.path.exists(p.account_dir):
      return "account directory " + p.account_dir + \
            " does not exist"
    elif not os.path.isdir(p.account_dir):
      return p.dir + " is not a directory"
    else:
      return ""
  else:
    return "null account directory"

####################################################################
def ValidateAccount (p, account):
  p.account = account.strip();
  if p.account == "":
    return "You must specify the name of the email account"
  return ""

####################################################################
def ValidateLimits (p, folder_flag, folder):
  p.folder_cb = folder_flag
  p.folder    = folder.strip()  
  if p.folder_cb:
    if p.folder == "":
      return "If limiting to a folder, you must give the folder name"
    else:
      pass
  return ""

####################################################################
def OpenSqlite3Connection (p, db):
  p.database = db.strip()
  if p.cnx != None:
    p.cnx.close()
    p.cnx = None
  try:
    p.cnx = dba.sqlite_connect(db)
  except:
    p.cnx = None
    return "cannot connect to sqlite3 database" + p.database 
  return "" 

######################################################################
def ActionButtons (p, go_label):
  bx = wx.BoxSizer(orient=wx.HORIZONTAL)
  p.name2component["reset_button"] = rb = \
      wx.Button(p, label="Reset")
  p.name2component["go_button"] = gb = \
      wx.Button(p, label=go_label)
  bx.Add(rb, 1, wx.ALL, 5)
  bx.Add(gb, 1, wx.ALL, 5)
  return bx

