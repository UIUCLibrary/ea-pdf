#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import sys
if "." not in sys.path:
  sys.path.append(".")
import os
import re
import argparse
import dm_common as dmc
import mbox2xml
import folder_data as fld

LOG_NAME  = "dm_xml.log.txt"
EOL       = os.linesep

fd_list   = []

######################################################################
def LogVariables (logf):
  logf.write("########## SETTINGS ##########\n")
  logf.write("account: " + account_name + "\n")
  logf.write("account directory: " + account_directory + "\n")
  if folder_name:
    logf.write("do only one folder: " + folder_name + "\n")

######################################################################
def CheckAccountDirectory (account_name, account_directory, fd_list):
  if not os.path.exists(account_directory):
    print("account directory " + account_directory + " does not exist")
    return False
  if not os.path.isdir(account_directory):
    print(account_directory +  " is not a directory")
    return False
  fld.FindMboxFiles(account_name, account_directory, fd_list,
      account_directory)
  return True

########################################################################
def check_for_dup_folder_names (fd_list):
  fld_names = {}
  for fd in fd_list:
    if fd.folder_name not in fld_names.keys():
      fld_names[fd.folder_name] = 1
    else:
      fld_names[fd.folder_name] = fld_names[fd.folder_name] + 1
  dups = []
  for folder_name in fld_names.keys():
    if fld_names[folder_name] > 1:
      dups.append(folder_name)
  return dups

######################################################################
def CheckFolderList (fd_list, folder):
  if len(fd_list) == 0:
    print("There are no .mbox files located under the account directory")
    return False
  dups = check_for_dup_folder_names(fd_list)
  if len(dups) > 0:
    msg = "Folder names must be unique in an account. " + \
        "These folder names are used more than one time in " + \
        "the account directory:\n" + "\n".join(dups)
    print(msg)
    return False
  elif folder and not fld.FindFolderName(fd_list, folder):
    print("File " + folder + ".mbox cannot be found under the account directory")
    return False
  return True

######################################################################
def GetArgs():

  global account_directory
  global account_name
  global folder_name
  global folder_path

  account_directory = None
  account_name      = None
  folder_path       = None
  folder_name       = None
  
  parser = argparse.ArgumentParser(description="Convert mbox to XML.")
  parser.add_argument("--account", "-a", dest="account_name",
                      required=True, help="email account name")
  parser.add_argument("--directory", "-d", dest="account_directory",
                      required=True,
                      help="directory to hold all files for this account")
  parser.add_argument("--folder", "-f", dest="folder_name",
      help="folder name (generate XML only for this one folder)")
  
  args      = parser.parse_args()
  argdict   = vars(args)
  account_name      = argdict["account_name"].strip()
  account_directory = os.path.normpath(
      os.path.abspath(argdict["account_directory"].strip()))
  folder_name = argdict["folder_name"]
  if folder_name:
    folder_name = folder_name.strip()

  if not CheckAccountDirectory(account_name, account_directory, fd_list):
    sys.exit(0)
  if not CheckFolderList(fd_list, folder_name):
    sys.exit(0)

######################################################################
def Convert ():  
  logf = open(os.path.join(account_directory, LOG_NAME),
      "w", errors="replace")
  LogVariables(logf)
  mx = mbox2xml.Mbox2Xml(
      logf,
      account_name,
      dmc.CHUNK_BYTES)
  for fd in fd_list:
    if folder_name and folder_name != fd.folder_name:
      continue
    mx.walk_folder(fd)
  mx.write_log()
  del mx
  logf.close()

######################################################################
GetArgs()
Convert()
