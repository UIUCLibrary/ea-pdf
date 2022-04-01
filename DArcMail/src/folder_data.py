#!/usr/bin/env python3

import os
import re
import dm_common as dmc

######################################################################
class FolderData ():

  ## ON CREATION, account_dir and mbox_path are input in OS-DEPENDENT FORM
  ## ALL PATHS SAVED IN THE CLASS ARE IN OS-COMMON FORM (i.e., no backslashes)

  ## mbox_path:           the full path of the mbox, including suffix
  ## relative_folder_dir: the path of the directory that contains
  ##    the mbox, relative to the account_dir [NO preceding './']
  ## folder_name:         name of the folder as entered into the db
  ##    == <relative_folder_dir>/<simple_mbox_name>

  ####################################################################
  def __init__ (self, account_name, account_dir, mbox_path):
    self.folder_id = None
    self.account_name = account_name
    self.folder_size = os.path.getsize(mbox_path)
    self.folder_dir = \
      dmc.os_common_path(os.path.dirname(mbox_path))
    self.relative_folder_dir = \
      dmc.os_common_path(os.path.dirname(mbox_path)[len(account_dir)+1:])
    self.mbox_path   = dmc.os_common_path(mbox_path)
    self.account_dir = dmc.os_common_path(account_dir)
    self.folder_name = dmc.os_common_path(re.sub("\.mbox$", "", \
        mbox_path, re.I)[len(account_dir)+1:])
 
  ####################################################################
  def set_folder_id (self, fid):
    self.folder_id = fid

####################################################################
def FindFolderName (fd_list, fname):
  for fd in fd_list:
    if fd.folder_name == fname:
      return fd
  return None

####################################################################
def GetFolders (account_name, account_directory, fd_list, choices):
  if len(fd_list) > 0:
    print("GetFolders: fd_list must be zero-length")
    sys.exit(0)
  if len(choices) > 0:
    print("GetFolders: choices must be zero-length")
    sys.exit(0)
  temp_list = []
  FindMboxFiles(account_name, account_directory, temp_list, parent="")
  MakeFolderChoices(account_directory, temp_list, fd_list, choices)
  
####################################################################
def MakeFolderChoices (account_directory, temp_list, fd_list, choices):
  lookup = {}
  start = len(account_directory)+1
  for f in temp_list:
    folder_path = f.mbox_path[start:]
    choices.append(folder_path)
    lookup[folder_path] = f
  choices.sort()
  for c in choices:
    fd_list.append(lookup[c])
  #choices = ["ALL FOLDERS"] + choices
  
####################################################################
def FindMboxFiles (account_name, account_directory, fd_list, parent=""):
  account_directory = os.path.abspath(account_directory)
  if not parent:
    parent = account_directory
  else:
    parent = os.path.abspath(parent)
  for f in os.listdir(parent):
    child = os.path.join(parent, f)
    if os.path.isdir(child):
      FindMboxFiles(account_name, account_directory, fd_list, child)
    else:
      m = re.match(".*\.mbox$", f)
      if m:
        fd = FolderData(account_name, account_directory, child)
        fd_list.append(fd)

######################################################################$
if __name__ == "__main__":
  import sys
  account_dir = os.path.abspath(sys.argv[1])
  start = len(account_dir)+1
  fd_list = []
  FindMboxFiles(None, sys.argv[1], fd_list, None)
  for f in fd_list:
    print(f.mbox_path[start:])
