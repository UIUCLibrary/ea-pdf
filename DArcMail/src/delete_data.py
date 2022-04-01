#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import sys
import os
import re
import db_access as dba
import stop_watch
import stat
import dm_defaults as dmd

class DeleteData ():

  ######################################################################
  def __init__ (self, cnx, account_name, account_directory, account_id):
    self.cnx               = cnx
    self.account_name      = account_name
    self.account_directory = account_directory
    self.account_id        = account_id
    self.remove_external   = True

  ######################################################################
  def delete_subdirs(self, curdir):
    m = re.match("^[0-9a-f]{2}$", os.path.basename(curdir))
    if m and os.path.isdir(curdir):
      files = os.listdir(curdir)
      for f in files:
        child = os.path.join(curdir, f)
        if os.path.isdir(child):
          self.delete_subdirs(child)
      if len(os.listdir(curdir)) == 0:
        try:
          os.chmod(curdir, 0o666)
          os.rmdir(curdir)
        except:
          print("Unexpected error while attempting to rmdir " + \
            curdir, sys.exc_info()[0])

  ######################################################################
  def delete_one_external_content (self, stored_fn, folder_directory):
    file_name = os.path.join(folder_directory, stored_fn)
    if not os.path.exists(file_name):
      print("external file ", file_name, " does not exist")
      return
    try:
      os.chmod(file_name, 0o666)
      os.remove(file_name)
    except:
      print("Unexpected error:", sys.exc_info()[0])

  ######################################################################
  def delete_external_content (self, dbf):
    if self.remove_external:
      folder_directory = os.path.abspath(
          os.path.join(self.account_directory,
	      os.path.dirname(dbf.folder_name)))
      exc_list = dba.get_folder_external_content(self.cnx, dbf.folder_id)
      for (ext_content_id, stored_fn) in exc_list:
        if not stored_fn:
          print("no stored_file_name for external_content.id=", ext_content_id)
        else:
          self.delete_one_external_content(stored_fn, folder_directory)
      for d in os.listdir(folder_directory):
        self.delete_subdirs(os.path.join(folder_directory, d))
    dba.delete_folder_external_content(self.cnx, dbf.folder_id)

  ######################################################################
  def delete_internal_content (self, dbf):
    dba.delete_folder_internal_content(self.cnx, dbf.folder_id)

  ######################################################################
  def delete_content (self, dbf):
    self.delete_external_content(dbf)
    self.delete_internal_content(dbf)

  ######################################################################d
  def delete_part_headers (self, folder_id):
    dba.delete_folder_part_headers(self.cnx, folder_id)

  ######################################################################d
  def delete_message_headers (self, folder_id):
    dba.delete_folder_message_headers(self.cnx, folder_id)
    dba.delete_folder_message_address(self.cnx, folder_id)

  ######################################################################d
  def delete_headers (self, folder_id):
    self.delete_part_headers(folder_id)
    self.delete_message_headers(folder_id)

  ######################################################################
  def delete_parts (self, folder_id):
    dba.delete_folder_parts(self.cnx, folder_id)

  ######################################################################
  def delete_messages (self, folder_id):
    dba.delete_folder_replies(self.cnx, folder_id)
    dba.delete_folder_messages(self.cnx, folder_id)

  ######################################################################
  def delete_one_folder (self, dbf):
    self.delete_headers(dbf.folder_id)
    self.delete_parts(dbf.folder_id)
    self.delete_messages(dbf.folder_id)
    self.delete_content(dbf)
    dba.delete_folder(self.cnx, dbf.folder_id)

  ####################################################################
  def delete_addresses_and_names (self):
    # delete from address_name where name_id is not used in message_address
    query = "delete from address_name where " + \
      "address_name.address_id in (select a.id " + \
      "from address a LEFT JOIN message_address ma on " + \
      "a.id=ma.address_id where ma.address_id is null);"
    cursor = self.cnx.cursor()
    cursor.execute(query)
    cursor.close()

    # delete from address_name where name_id is not used in message_address
    query = "delete from address_name where " + \
      "address_name.name_id in (select n.id " + \
      "from name n LEFT JOIN message_address ma on " + \
      "n.id=ma.name_id where ma.name_id is null);"
    cursor = self.cnx.cursor()
    cursor.execute(query)
    cursor.close()

    # delete from address where id is not used in message_address
    query = "delete from address where address.id in (select a.id " + \
        "from address a LEFT JOIN message_address ma on " + \
        "a.id=ma.address_id where ma.address_id is null);"
    cursor = self.cnx.cursor()
    cursor.execute(query)
    cursor.close()

    # delete from name where id is not used in message_address
    query = "delete from name where name.id in (select n.id " + \
        "from name n LEFT JOIN message_address ma on " + \
        "n.id=ma.name_id where ma.name_id is null);"
    cursor = self.cnx.cursor()
    cursor.execute(query)
    cursor.close()

######################################################################
def main():
  pass

######################################################################
if __name__ == "__main__":
  main()

