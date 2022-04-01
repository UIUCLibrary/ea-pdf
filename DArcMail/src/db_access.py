#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import sys
import os
import sqlite3
import re
import dm_common as dmc
import dm_defaults as dmd
import message_group

CHARACTER_SET = dmc.CHARACTER_SET
CLIENT_CHARACTER_SET = dmc.CHARACTER_SET

######################################################################
def last_insert_id (cnx):
  query = "select last_insert_rowid()"    
  cursor = cnx.cursor()
  cursor.execute(query)
  last_id = None
  for row in cursor:
    last_id = row[0]
  cursor.close()
  return last_id

######################################################################
def sqlite_connect (sqlite_db):
  cnx = sqlite3.connect(sqlite_db)
  return cnx

######################################################################
## get's for class constructors
######################################################################

######################################################################
def get_uses (cnx, account_id, address_id, name_id, tag):
  n = 0
  if name_id:
    query = "select count(distinct ma.message_id) from " + \
       "message_address ma, message m, folder f, tag t where " + \
        "t.original_name='" + tag + "' and " + \
        "ma.tag_id=t.id and " + \
        "ma.address_id=" + str(address_id) + " and " + \
        "ma.name_id=" + str(name_id) + " and " + \
        "ma.message_id=m.id and " + \
        "m.folder_id=f.id and " + \
        "f.account_id=" + str(account_id)
  else:
    query = "select count(distinct ma.message_id) from " + \
       "message_address ma, message m, folder f, tag t where " + \
        "t.original_name='" + tag + "' and " + \
        "ma.tag_id=t.id and " + \
        "ma.address_id=" + str(address_id) + " and " + \
        "ma.message_id=m.id and " + \
        "m.folder_id=f.id and " + \
        "f.account_id=" + str(account_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
      n = row[0]
  cursor.close()
  return n

######################################################################
def get_folder_names_for_account (cnx, account_id):
  folder_names = set()
  query = "select folder_name from folder where " + \
      "account_id=" + str(account_id) + " order by folder_name;"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    folder_names.add(row[0])
  cursor.close()
  return folder_names

######################################################################
def get_folder_info (cnx, folder_id):
  (folder_name, account_id, account_name) = (None, None, None)
  query = "select f.folder_name, a.id, a.account_name " + \
      "from account a, folder f where " + \
      "f.account_id=a.id and f.id=" + str(folder_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    (folder_name, account_id, account_name) = row
  cursor.close()
  return (folder_name, account_id, account_name)

######################################################################
def get_account_name (cnx, account_id):
  query = "select a.account_name from account a where a.id=" + \
      str(account_id)
  account_name = None
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    account_name = row[0]
  cursor.close()
  return account_name

####################################################################
def get_message_counts_for_account (cnx, account_id):
  """
  query = "select count(m.id),date(min(m.date_time))," + \
        "date(max(m.date_time)) " + \
        "from message m,folder f where m.folder_id=f.id and " + \
        "f.account_id=" + str(account_id) + ";"
  """
  
  message_count = 0
  query = "select sum(f.n_messages) from folder f where " + \
          "f.account_id=" + str(account_id) + ";"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    message_count = row[0]
  cursor.close()
  
  start_date    = ""
  end_date      = ""
  query = "select date(min(f.date_from)), date(max(f.date_to)) " + \
          "from folder f where f.account_id=" + str(account_id) + ";"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    (start_date, end_date) = row
  cursor.close()
  
  return (message_count, start_date, end_date)

####################################################################
def get_message_counts_for_folder (cnx, folder_id):
  """
  query = "select count(distinct m.id),date(min(m.date_time))," + \
        "date(max(m.date_time)) " + \
        "from message m,folder f where m.folder_id=" + str(folder_id)
  """
  query = "select n_messages, date(date_from), date(date_to) " + \
          "from folder where id=" + str(folder_id) + ";"
  message_count = 0
  start_date    = ""
  end_date      = ""
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    (message_count, start_date, end_date) = row
  cursor.close()
  return (message_count, start_date, end_date)

####################################################################
def get_address_counts_for_account (cnx, account_id):
  from_count = 0
  to_count   = 0
  cc_count   = 0
  bcc_count  = 0
  ## faster to query each tag separately rather than using SQL "in (...)"
  for tag in ("From", "To", "Cc", "Bcc"):
    query = "select t.original_name,count(distinct ma.address_id) " + \
        "from tag t,message_address ma,message m,folder f " + \
        "where t.original_name = '" + tag + "' and " + \
        "ma.tag_id=t.id and " + \
        "ma.message_id=m.id and ma.address_id is not null and " + \
        "m.folder_id=f.id and f.account_id=" + str(account_id) + " " + \
        "group by t.original_name "
    cursor = cnx.cursor()
    cursor.execute(query)
    for (tag, count) in cursor:
      if tag == "From":
        from_count = count
      elif tag == "To":
        to_count = count
      elif tag == "Cc":
        cc_count = count
      elif tag == "Bcc":
        bcc_count = count
    cursor.close()
  return (from_count, to_count, cc_count, bcc_count)

####################################################################
def get_address_counts_for_folder (cnx, folder_id):
  query = "select t.original_name,count(distinct ma.address_id) " + \
      "from tag t,message_address ma,message m,folder f " + \
      "where t.original_name in ('From','To','Cc','Bcc') and " + \
      "ma.tag_id=t.id and " + \
      "ma.message_id=m.id and ma.address_id is not null and " + \
      "m.folder_id=" + str(folder_id) + " " + \
      "group by t.original_name " + \
      "order by field (t.original_name, 'From', 'To', 'Cc', 'Bcc')"
  from_count = 0
  to_count   = 0
  cc_count   = 0
  bcc_count  = 0
  cursor = cnx.cursor()
  cursor.execute(query)
  for (tag, count) in cursor:
    if tag == "From":
      from_count = count
    elif tag == "To":
      to_count = count
    elif tag == "Cc":
      cc_count = count
    elif tag == "Bcc":
      bcc_count = count
  cursor.close()
  return (from_count, to_count, cc_count, bcc_count)

######################################################################
## general stuff
#####################################################################

######################################################################
def escape_sql (s):
  s0 = s
  s1 = re.sub("'", "''", s0)
  return s1

######################################################################
def original_tag_dictionary (cnx):
  tag2id = {}
  query = "select id,original_name from tag"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    (tag_id, name) = row
    if (name != ""):
      tag2id[name] = tag_id
  cursor.close()
  return tag2id

######################################################################
def xml_tag_dictionary (cnx):
  tag2id = {}
  query = "select id,xml_name from tag"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    (tag_id, name) = row
    if (name != ""):
      tag2id[name] = tag_id
  cursor.close()
  return tag2id

######################################################################
## delete
######################################################################
def  delete_account (cnx, account_id):
  query = "delete from account where id=" + str(account_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()

######################################################################
def delete_folder_internal_content (cnx, folder_id):
  query = "delete from internal_content where folder_id=" + str(folder_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()

######################################################################
def delete_folder (cnx, folder_id):
  query = "delete from folder where id=" + str(folder_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()

######################################################################
def delete_folder_parts (cnx, folder_id):
  query = "delete from part " + \
    "where message_id in (select m.id from message m " + \
    "where m.folder_id=" + str(folder_id) + ")"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()

######################################################################
def delete_folder_messages (cnx, folder_id):
  query = "delete from message " + \
      "where folder_id=" + str(folder_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()

######################################################################
def delete_folder_replies (cnx, folder_id):
  query = "delete from replyto " + \
      "where replying_id in (select m.id from message m " + \
      "where m.folder_id=" + str(folder_id) + ")"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()

######################################################################
def delete_folder_part_headers (cnx, folder_id):
  ## not sure if there are limits on length of an IN expression...
  ## but can't seem to delete with aliases, inner joins or with clauses
  query = "delete from part_header " + \
    "where part_id in (select p.id from part p, message m " + \
    "where p.message_id=m.id and m.folder_id=" + str(folder_id) + ")"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()

######################################################################
def delete_folder_message_headers (cnx, folder_id):
  query = "delete from message_header " + \
    "where message_id in (select m.id from message m " + \
    "where m.folder_id=" + str(folder_id) + ")"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()

######################################################################
def delete_folder_message_address (cnx, folder_id):
  query = "delete from message_address " + \
    "where message_id in (select m.id from message m " + \
    "where m.folder_id=" + str(folder_id) + ")"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()

######################################################################
def delete_folder_external_content (cnx, folder_id):
  query = "delete from external_content where folder_id=" + str(folder_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()

######################################################################
def get_folder_external_content(cnx, folder_id):
  query = "select id, stored_file_name from external_content where " + \
      "folder_id=" + str(folder_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  exc_list = []
  for row in cursor:
    exc_list.append(row)
  cursor.close()
  return exc_list

######################################################################
def get_stored_file_name (cnx, ext_content_id):
  query = "select stored_file_name from external_content where " + \
      "id=" + str(ext_content_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  stored_file_name = None
  for row in cursor:
    stored_file_name = row[0]
  cursor.close()
  return stored_file_name

######################################################################
def existing_external_content (cnx, sha1, content_length,
      original_file_name, folder_id):
  query = "select id from external_content " + \
    "where content_length=" + str(content_length) + " " + \
    "and content_sha1='" + sha1 + "' " + \
    "and folder_id=" + str(folder_id) + " "
  if original_file_name:
    query = query + "and original_file_name='" + \
        escape_sql(original_file_name) + "'"
  else:
    query = query + "and original_file_name is null"

  cursor = cnx.cursor()
  cursor.execute(query)
  external_content_id = None
  for row in cursor:
    if external_content_id:
      print("multiple external_content_ids match id=", \
          external_content_id)
    external_content_id = row[0]
  cursor.close()
  return external_content_id

######################################################################
def existing_internal_content (cnx, sha1, content_length,
    original_file_name, folder_id):
  query = "select id from internal_content " + \
    "where content_length=" + str(content_length) + " " + \
    "and content_sha1='" + sha1 + "' " + \
    "and folder_id=" + str(folder_id) + " "
  if original_file_name:
    query = query + "and original_file_name='" + escape_sql(original_file_name) + "'"
  else:
    query = query + "and original_file_name is null"

  cursor = cnx.cursor()
  cursor.execute(query)
  internal_content_id = None
  for row in cursor:
    if internal_content_id:
      print("multiple internal_content_ids match id=", internal_content_id)
    internal_content_id = row[0]
  cursor.close()
  return internal_content_id

######################################################################
## insert and update
######################################################################
def insert_tag (cnx, tag):
  query = "insert into tag(original_name,xml_name) values " + \
    "('" + tag + "','')"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    pass
  cursor.close()
  new_tag_id = last_insert_id(cnx)
  return new_tag_id

######################################################################
def insert_message (cnx, folder_id, global_message_id, eol, sha1_hash,
      date_time, zoffset):
  query = "insert into message(folder_id,global_message_id,eol,sha1_hash," + \
    "date_time, zoffset) values (" + \
    str(folder_id) + "," + \
    "'" + escape_sql(str(global_message_id)) + "'," + \
    ("'" + eol + "'" if eol else "null") + "," + \
    "'" + sha1_hash +  "'," + \
    "'" + date_time +  "'," + \
    "'" + zoffset +  "'" + \
    ");"

#  print(query)
#  sys.exit(0)

  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

  message_id = last_insert_id(cnx)
  return message_id

######################################################################
def insert_replyto(cnx, message_id, repliedto_global_id):
  query = "insert into replyto(replying_id,repliedto_id) values" + \
      "(" + str(message_id) + ",'" + escape_sql(repliedto_global_id) + "')"
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

######################################################################
def set_is_attachment (cnx, part_id, is_attachment):
  query = "update part set is_attachment=" + \
      ("1" if is_attachment else "0") + \
      " where id=" + str(part_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

######################################################################
def insert_part (cnx, message_id, part_sequence_id, parent_sequence_id,
    is_multipart):
  query = "insert into part(message_id,sequence_id,parent_sequence_id," + \
      "is_multipart) values (" + \
    str(message_id) + "," + \
    str(part_sequence_id) + "," + \
    str(parent_sequence_id) + "," + \
    ("1" if is_multipart else "0") + \
    ");"
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

  part_id = last_insert_id(cnx)
  return part_id

######################################################################
def add_internal_content (cnx, part_id, internal_content_id,
    content_length, content_sha1):
  query = "update part set internal_content_id=" + \
      str(internal_content_id) + "," + \
      "content_length=" + str(content_length) + "," + \
      "content_sha1='" + content_sha1 + "' " + \
      "where id=" + str(part_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

######################################################################
def add_external_content (cnx, part_id, external_content_id,
    content_length, content_sha1):
  query = "update part set external_content_id=" + \
      str(external_content_id) + "," + \
      "content_length=" + str(content_length) + "," + \
      "content_sha1='" + content_sha1 + "' " + \
      "where id=" + str(part_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

######################################################################
def insert_internal_content (cnx, content, original_file_name, folder_id,
    content_length, content_sha1):
  query = "insert into internal_content(" + \
    "content_text,original_file_name,folder_id," + \
    "content_length,content_sha1) " + \
    "values (" + \
    "'" + escape_sql(content) + "'," + \
    ("'" + escape_sql(original_file_name) + \
        "'" if original_file_name else "null") + "," + \
    str(folder_id) + "," + \
    str(content_length) + "," + \
    "'" + content_sha1 + "'" + \
    ");"
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

  internal_content_id = last_insert_id(cnx)
  return internal_content_id

######################################################################
def insert_external_content (cnx, original_file_name,
    stored_file_name, folder_id, content_length, content_sha1, xml_wrapped):
  query = "insert into external_content(original_file_name," + \
    "stored_file_name,folder_id,content_length,content_sha1,xml_wrapped) " + \
    "values (" + \
    ("'" + escape_sql(original_file_name) + \
        "'" if original_file_name else "null") + "," + \
    "'" + escape_sql(stored_file_name) + "'," + \
    str(folder_id) + "," + \
    str(content_length) + "," + \
    "'" + content_sha1 + "'," + \
    ("1" if xml_wrapped else "0") + \
    ");"
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

  external_content_id = last_insert_id(cnx)
  return external_content_id

######################################################################
def insert_message_header (cnx, message_id, tag_id, value):
  query = "insert into message_header(message_id,tag_id,header_value) " + \
    "values (" + \
    str(message_id) + "," + \
    str(tag_id) + "," + \
    "'" + escape_sql(value) + "'" + \
    ");"
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

######################################################################
def insert_part_header (cnx, part_id, tag_id, value):
  query = "insert into part_header(part_id,tag_id,header_value) " + \
    'values (' + \
    str(part_id) + "," + \
    str(tag_id) + "," + \
    "'" + escape_sql(value) + "'" + \
    ");"
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

######################################################################
def insert_name (cnx, name):
  query = "insert into name(name) values" + \
      "('" + escape_sql(name) + "')" 
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

  name_id = last_insert_id(cnx)
  return name_id

######################################################################
def insert_message_address(cnx, message_id, tag_id, address_id, name_id):
  query = "insert into message_address(" + \
      "message_id,tag_id,address_id,name_id) " + \
      "values(" + str(message_id) + "," + str(tag_id) + "," + \
      (str(address_id) if address_id else "null") + "," + \
      (str(name_id) if name_id else "null") + ")"
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

######################################################################
def insert_address (cnx, address):
  query = "insert into address(address) values" + \
      "('" + escape_sql(address) + "')" 
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

  address_id = last_insert_id(cnx)
  return address_id

######################################################################
def insert_address_name (cnx, address_id, name_id):
  query = "select address_id, name_id from address_name where address_id=" + \
    str(address_id) + " and name_id=" + str(name_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  found = False  
  for row in cursor:
    found = True
  cursor.close()
  if not found:
    query = "insert into address_name(address_id,name_id) values" + \
        "(" + str(address_id) + "," + str(name_id) + ")"
    cursor = cnx.cursor()
    cursor.execute(query)
    cursor.close()

######################################################################
def insert_account (cnx, account_name):
  query = "insert into account(account_name) values" + \
      "('" + escape_sql(account_name) + "')" 
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

  account_id = last_insert_id(cnx)
  return account_id

######################################################################
def update_account_directory (cnx, account_id, account_directory):
  query = "update account set account_directory='" + \
      escape_sql(account_directory) + "' where id=" + str(account_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

######################################################################
## gets
######################################################################

######################################################################
def get_transfer_encoding (cnx, part_id):
  te = ""
  query = "select ph.header_value from part_header ph, tag t where " + \
      "t.xml_name='TransferEncoding' and ph.tag_id=t.id and " + \
      "ph.part_id=" + str(part_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    te = row[0]
  cursor.close()
  return te

######################################################################
def get_message_digest (cnx, message_id):
  hash = None
  query = "select sha1_hash from message where id=" + str(message_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    hash = row[0]
  cursor.close()
  return hash
  
######################################################################
def get_last_header_seq (cnx, part_id):
  seq = 0
  query = "select max(sequence_id) from header where part_id =" + \
      str(part_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    seq = row[0]
  cursor.close()
  return seq

######################################################################
def is_multipart (cnx, part_id):
  is_mp = None
  query = "select is_multipart from part where id=" + str(part_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    is_mp = row[0]
  cursor.close()
  return is_mp

######################################################################
def get_top_part (cnx, message_id):
  part_id = None
  query = "select id from part where message_id=" + \
    str(message_id) + " " + \
    "and sequence_id=1"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    part_id = row[0]
  cursor.close()
  return part_id

######################################################################
def get_internal_content_text (cnx, content_id):
  query = "select content_text from internal_content " + \
      "where id=" + str(content_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    content_text = row[0]
  cursor.close()
  return content_text

######################################################################
def get_part_content (cnx, part_id):
  query = "select internal_content_id, external_content_id from part " + \
      "where id=" + str(part_id)
  internal_content_id = None
  external_content_id = None
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    (internal_content_id, external_content_id) = row
  cursor.close()

  original_file_name = None
  stored_file_name   = None
  content_text       = None
  if internal_content_id:
    query = "select content_text from internal_content " + \
        "where id=" + str(internal_content_id)
    cursor = cnx.cursor()
    cursor.execute(query)
    for row in cursor:
      content_text = row[0]
    cursor.close()
  elif external_content_id:
    query = "select original_file_name,stored_file_name from external_content " + \
        "where id=" + str(external_content_id)
    cursor = cnx.cursor()
    cursor.execute(query)
    for row in cursor:
      (original_file_name, stored_file_name) = row
    cursor.close()
  else:
#    print("failed to retrieve content for part = ", part_id)
    return (None, None, None, None, None)

  return (internal_content_id, external_content_id, original_file_name,
      stored_file_name, content_text)

######################################################################
def get_external_content_info (cnx, content_id):
  content_info = (None, None, None, None)
  query = "select folder_id,xml_wrapped,original_file_name," + \
      "stored_file_name,content_sha1 " + \
      "from external_content where id=" + str(content_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    content_info = row
  cursor.close()
  return content_info

######################################################################
def get_folders_for_account (cnx, account_id):
  folder_list = []
  folder_ids  = []
  query = "select f.id from folder f where " + \
    "f.account_id=" + str(account_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    folder_ids.append(row[0])
  cursor.close()
  for folder_id in folder_ids:
    folder_list.append(DbFolder(cnx, folder_id))
  return folder_list

######################################################################
def get_folder_names (cnx, account_id):
  folder_list = []
  query = "select id, folder_name from folder where account_id=" + \
      str(account_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    folder_list.append(row)
  cursor.close()
  return folder_list

######################################################################
def get_messages (cnx, folder_id):
  message_list = []
  query = "select id from message where folder_id=" + str(folder_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    message_list.append(row[0])
  cursor.close()
  return message_list

######################################################################
def get_message (cnx, message_id):
  folder_id         = None
  global_message_id = None
  eol               = None
  sha1_hash         = None

  query = \
    "select " + \
    "folder_id," + \
    "global_message_id," + \
    "eol," + \
    "sha1_hash " + \
    "from message where id=" + str(message_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    (folder_id, global_message_id, eol, sha1_hash) = row
  cursor.close()
  return (folder_id, global_message_id, eol, sha1_hash)

######################################################################
def get_message_headers (cnx, message_id):
  headers = {}
  query = "select t.original_name,t.xml_name,h.header_value from " + \
      "tag t,message_header h where h.message_id=" + \
      str(message_id) + " and " + \
      "h.tag_id=t.id"
  cursor = cnx.cursor()
  cursor.execute(query)
  for (original_name, xml_name, value) in cursor:
    name = xml_name
    if xml_name == "":
      name = original_name
    if name in headers.keys():
      print("multiple values for tag ", name, "; message_id=", message_id)
    else:
      headers[name] = value
  cursor.close()
  return headers

######################################################################
def get_part_headers (cnx, part_id):
  headers = {}
  query = "select t.original_name,t.xml_name,h.header_value from " + \
      "tag t,part_header h where h.part_id=" + str(part_id) + " and " + \
      "h.tag_id=t.id"
  cursor = cnx.cursor()
  cursor.execute(query)
  for (original_name, xml_name, value) in cursor:
    name = xml_name
    if xml_name == "":
      name = original_name
    if name in headers.keys():
      print("multiple values for tag ", name, "; part_id=", part_id)
    else:
      headers[name] = value
  cursor.close()
  return headers

######################################################################
def get_name_id (cnx, name, make=False):
  name_id = None
  query = "select n.id from name n where n.name='" + \
      escape_sql(name) + "'"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    if name_id:
      print("multiple names with name ", name, "; using first one")
    else:
      name_id = row[0]
  cursor.close()
  if not name_id:
    if not make:
      sys.stderr.write("No name with name " + name + "\n")
      return None
    else:
      return insert_name(cnx, name)
  return name_id


######################################################################
def get_address_id (cnx, address, make=False):
  address_id = None
  query = "select a.id from address a where a.address='" + \
      escape_sql(address) + "'"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    if address_id:
      print("multiple addresses with address ", address, "; using first one")
    else:
      address_id = row[0]
  cursor.close()
  if not address_id:
    if not make:
      sys.stderr.write("No address with address " + address + "\n")
      return None
    else:
      return insert_address(cnx, address)
  return address_id

######################################################################
def get_account_id (cnx, account_name, make=False):
  account_id = None
  query = "select a.id from account a where a.account_name='" + \
      escape_sql(account_name) + "'"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    if account_id:
      print("multiple acounts with name ", account, "; using first one")
    else:
      account_id = row[0]
  cursor.close()
  if not account_id:
    if not make:
#      sys.stderr.write('No account with name ' + account_name + '\n')
      return None
    else:
      return insert_account(cnx, account_name)
  return account_id

######################################################################
def get_account_list (cnx):
  account_names = []
  query = "select account_name from account order by account_name;"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    account_names.append(row[0])
  cursor.close()
  if len(account_names) == 0:
    account_names.append("None")
  return account_names

######################################################################
def get_account_directory (cnx, account_id):
  account_directory = None
  query = "select account_directory from account where id=" + str(account_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    account_directory = row[0]
  cursor.close()
  return account_directory

######################################################################
def loaded_account_info (cnx):
  account_id = None
  account_name = None
  account_directory = None
  info = []
  query = "select id, account_name, account_directory " + \
          "from account order by id;"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    info.append(row)
  cursor.close()
  return info

######################################################################
def insert_folder (cnx, account_id, folder_name):
  folder_id = None
  query = "insert into folder(account_id,folder_name) values" + \
      "(" + str(account_id) + ",'" + escape_sql(folder_name) + "')"
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

  folder_id = last_insert_id(cnx)
  return folder_id

######################################################################
def update_folder_info (cnx, folder_id, n_messages, date_from,
                        date_to):
  query = "update folder set n_messages=" + str(n_messages) + "," + \
      "date_from='" + date_from + "'," + \
      "date_to='" + date_to + "' " + \
      "where id=" + str(folder_id) + ";"
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

######################################################################
def get_folder_of_message (cnx, message_id):
  folder = None
  query = "select folder_id from message m where m.id=" + str(message_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    folder_id = row[0]
  cursor.close()
  return folder_id
  
######################################################################
def get_folder_id (cnx, account_id, folder_name, make=False):
  folder_id = None
  query = "select f.id from folder f where f.folder_name='" + \
      escape_sql(folder_name) + "' and account_id=" + str(account_id)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    if folder_id:
      print("multiple folders with name ", folder_name, "; using first one")
    else:
      folder_id = row[0]
  cursor.close()
  if not folder_id:
    if not make:
      return None
    else:
      return insert_folder(cnx, account_id, folder_name)
  return folder_id

######################################################################
## search
######################################################################
def search_message_for_ids (cnx, message_ids):
  message_info = []
  query = "select m.id, m.date_time, date(m.date_time), mh.header_value " + \
      "from message m, message_header mh, tag t where " + \
      "t.id=mh.tag_id and " + \
      "m.id=mh.message_id and " + \
      "t.original_name='Subject' and " + \
      "m.id in (" + ','.join(map(str,message_ids)) + ") " + \
      "order by m.date_time"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    mid   = row[0]
    mdate = row[2]
    msubj = row[3]
    message_info.append((mid, mdate, msubj))
  cursor.close()
  return message_info

######################################################################
def search_message_for_address (cnx, account_id, address_id,
                                address_type):
  message_info = []
  query = "select m.id, m.date_time, date(m.date_time), " + \
      "mh.header_value " + \
      "from message m, folder f, message_header mh, tag t, " + \
      "message_address ma where " + \
      "m.folder_id=f.id and " + \
      "f.account_id=" + str(account_id) + " and " + \
      "t.id=mh.tag_id and " + \
      "m.id=mh.message_id and " + \
      "t.original_name='Subject' and " + \
      "ma.address_id=" + str(address_id) + " and " + \
      "ma.message_id=m.id and " + \
      "ma.tag_id=(select id from tag where original_name='" + address_type + "') "
  query = query + "order by m.date_time, m.id"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    mid   = row[0]
    mdate = row[2]
    msubj = row[3]
    message_info.append((mid, mdate, msubj))
  cursor.close()
  return message_info

######################################################################
def search_message (cnx, account_id, search_params):
  global_id        = search_params.global_id
  date_from        = search_params.date_from
  date_to          = search_params.date_to
  folder           = search_params.folder
  from_line        = search_params.from_line
  to_line          = search_params.to_line
  cc_line          = search_params.cc_line
  subject          = search_params.subject
  attachment       = search_params.attachment
  body             = search_params.body
  body_search_type = search_params.body_search_type
  selected_status  = search_params.selected_status
  sort_order       = search_params.sort_order

  mg = message_group.message_group_for_account(account_id)
  message_info = []
  query = "select m.id, m.date_time, date(m.date_time), mh.header_value " + \
    "from message m, folder f, message_header mh, tag t where " + \
    "m.folder_id=f.id and " + \
    "f.account_id=" + str(account_id) + " and " + \
    "t.id=mh.tag_id and " + \
    "m.id=mh.message_id and " + \
    "t.original_name='Subject' "
  if date_from and date_to:
    query = query + " and " + \
    "date(m.date_time) between '" + date_from + "' " + \
    "and '" + date_to + "' "
  elif date_from:
    query = query + " and " + \
    "date(m.date_time) >= '" + date_from + "' "
  elif date_to:
    query = query + " and " + \
    "date(m.date_time) <= '" + date_to + "' "
  if global_id:
    query = query + " and " + \
    "m.global_message_id like'%" + global_id + "%' "
  if folder:
    query = query + " and " + \
    "f.folder_name like '%" + escape_sql(folder) + "%' "
  if subject:
    query = query + " and " + \
    "mh.header_value like '%" + escape_sql(subject) + "%' "
  if from_line:
    query = query + " and (" + \
      "m.id in (select distinct ma.message_id from " + \
      "message_address ma, tag ta, address ad where " + \
      "ta.id=ma.tag_id and ta.original_name='From' and " + \
      "ma.address_id=ad.id and ad.address like '%" + escape_sql(from_line) + "%' ) " + \
      "or " + \
      "m.id in (select distinct ma.message_id from " + \
      "message_address ma, tag ta, name na where " + \
      "ta.id=ma.tag_id and ta.original_name='From' and " + \
      "ma.name_id=na.id and na.name like '%" + escape_sql(from_line) + "%' ) " + \
    ")"
  if to_line:
    query = query + " and (" + \
      "m.id in (select distinct ma.message_id from " + \
      "message_address ma, tag ta, address ad where " + \
      "ta.id=ma.tag_id and ta.original_name='To' and " + \
      "ma.address_id=ad.id and ad.address like '%" + escape_sql(to_line) + "%' ) " + \
      "or " + \
      "m.id in (select distinct ma.message_id from " + \
      "message_address ma, tag ta, name na where " + \
      "ta.id=ma.tag_id and ta.original_name='To' and " + \
      "ma.name_id=na.id and na.name like '%" + escape_sql(to_line) + "%' ) " + \
   ")"
  if cc_line:
    query = query + " and (" + \
      "m.id in (select distinct ma.message_id from " + \
      "message_address ma, tag ta, address ad where " + \
      "ta.id=ma.tag_id and ta.original_name='Cc' and " + \
      "ma.address_id=ad.id and ad.address like '%" + escape_sql(cc_line) + "%' ) " + \
      "or " + \
      "m.id in (select distinct ma.message_id from " + \
      "message_address ma, tag ta, name na where " + \
      "ta.id=ma.tag_id and ta.original_name='Cc' and " + \
      "ma.name_id=na.id and na.name like '%" + escape_sql(cc_line) + "%' ) " + \
    ")"
  if body:
    if body_search_type == "both":
      body_search_type = "'text/plain',text/html'"
    elif body_search_type == "plain":
      body_search_type = "'text/plain'"
    elif body_search_type == "html":
      body_search_type = "'text/html'"
    query = query + " and m.id in (" + \
      "select p.message_id from part p, internal_content i, part_header ph, " + \
      "tag t where " + \
      "ph.part_id=p.id and ph.tag_id=t.id and t.original_name='Content-Type' and " + \
      "ph.header_value in (" + body_search_type + ") and " + \
      "p.internal_content_id = i.id and p.is_attachment = 0 and " + \
      "i.content_text like '%" + escape_sql(body) + "%' ) "
  if attachment:
    att = escape_sql(attachment)
    query = query + " and (" + \
      "m.id in (" + \
      "select p.message_id from part p, internal_content c where " + \
      "p.is_attachment=1 and p.internal_content_id=c.id and " + \
      "c.original_file_name like '%" + att + "%' ) " + \
      " or " + \
      "m.id in (" + \
      "select p.message_id from part p, external_content c where " + \
      "p.is_attachment=1 and p.external_content_id=c.id and " + \
      "c.original_file_name like '%" + att + "%' ) " + \
      ")"
  if sort_order == "oldest":
    query = query + "order by m.date_time, m.id"
  else:
    query = query + "order by m.date_time desc, m.id"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    mid   = row[0]
    mdate = row[2]
    msubj = row[3]
    if selected_status == 'selected' and mid not in mg:
      continue
    elif selected_status == 'unselected' and mid in mg:
      continue
    message_info.append((mid, mdate, msubj))
  cursor.close()
  return message_info

######################################################################
def search_account_name (cnx, account_name):
  account_info = []
  query = "select id, account_name from account " + \
      "where account_name like '%" + account_name + "%' " + \
      "order by account_name"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    account_info.append(row)
  cursor.close()
  return account_info

######################################################################
def lookup_folder_name (cnx, account_name, folder_name):
  fid = None
  query = "select f.id from folder f, account a where " + \
    "f.account_id=a.id and f.folder_name='" + \
    escape_sql(folder_name) + "' and " + \
    "a.account_name='" + escape_sql(account_name) + "'"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    fid = row[0]
  cursor.close()
  return fid

######################################################################
def lookup_account_directory (cnx, account_directory):
  account_info = []
  query = "select id, account_name, account_directory from account " + \
      "where account_directory in ('" + account_directory + "', '" + \
      dmc.os_common_path(account_directory) + "') " + \
      "order by account_name"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    account_info.append(row)
  cursor.close()
  return account_info

######################################################################
def get_address (cnx, aid):
  address = None
  query = "select a.address from address a where a.id=" + str(aid)
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    address = row[0]
  cursor.close()
  return address

######################################################################
def search_address_name (cnx, account_id, address_expr):
  # find all (aid, nid) pairs used in this account
  # find all (aid, address) used in this account and matching expr
  # find all (nid, name)    used in this account and matching expr

  aid2nid     = {}
  nid2aid     = {}

  ## all non-null (aid, nid) in message in folder in account
  ## (NOT required to match address_expr)
  query1 = \
      "select distinct ma.address_id, ma.name_id " + \
      "from message_address ma, message m, folder f where " + \
      "ma.address_id is not null and ma.name_id is not null and " + \
      "ma.message_id=m.id and m.folder_id=f.id and " + \
      "f.account_id=" + str(account_id)

  cursor = cnx.cursor()
  cursor.execute(query1)
  for (aid, nid) in cursor:
    if aid not in aid2nid.keys():
      aid2nid[aid] = set()
    aid2nid[aid].add(nid)
    if nid not in nid2aid.keys():
      nid2aid[nid] = set()
    nid2aid[nid].add(aid)
  cursor.close()

  aid2address = {}
  address2aid = {}

  ## all (aid, address) in message in folder in account
  ## where address matches address_expr
  query2 = \
      "select distinct ma.address_id, a.address " + \
      "from message m, folder f, message_address ma, address a where " + \
      "f.account_id=" + str(account_id) + " and " + \
      "m.folder_id=f.id and " + \
      "ma.message_id=m.id and " + \
      "ma.address_id=a.id and " + \
      "a.address like '%" + address_expr + "%'"

  cursor = cnx.cursor()
  cursor.execute(query2)
  for (aid, addr) in cursor:
    aid2address[aid] = addr
    address2aid[addr] = aid
  cursor.close()

  nid2name    = {}
  name2nid    = {}

  ## all (nid, name) in message in folder in account
  ## where name matches address_expr
  query3 = \
      "select distinct ma.name_id, n.name " + \
      "from message m, folder f, message_address ma, name n where " + \
      "f.account_id=" + str(account_id) + " and " + \
      "m.folder_id=f.id and " + \
      "ma.message_id=m.id and " + \
      "ma.name_id=n.id and " + \
      "n.name like '%" + address_expr + "%'"

  cursor = cnx.cursor()
  cursor.execute(query3)
  for (nid, name) in cursor:
    nid2name[nid] = name
    name2nid[name] = nid
  cursor.close()

  ## [This covers cases where a matching name appeared in a long
  ## list that had no paired addresses - such an address gets entered
  ## in the address table as as '-']
  ## for every name that matches the address_expr
  ## and was used on a message in the account
  ## and was paired with an address on some messsage
  ## in the account, 
  ## add this (aid, address) as matching
  for nid in nid2name.keys():
    if nid in nid2aid.keys():
      for aid in nid2aid[nid]:
        if aid not in aid2address.keys():
          addr = get_address(cnx, aid)
          if addr:
#            print("added address", aid, addr)
            aid2address[aid] = addr
            address2aid[addr] = aid

  address_info = []
  for addr in sorted(address2aid.keys()):
    aid = address2aid[addr]
    if aid in aid2nid.keys():
      # this aid was paired with a name,
      # so get ALL the names this aid is paired with
      temp = []
      for nid in aid2nid[aid]:
        if nid in nid2name.keys():
          temp.append(nid2name[nid])
        else:
          pass
      address_info.append((aid, addr, "; ".join(sorted(temp))))
    else:
      address_info.append((address2aid[addr], addr, ""))
  return address_info

######################################################################
#         stats support
######################################################################

######################################################################
def count_message (cnx, account):
  # message count
  count = 0
  query = "select count(m.id) from " + \
      "message m,folder f,account a where " + \
      "m.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    count = row[0]
  cursor.close()
  return count

######################################################################
def count_part (cnx, account):
  # part count
  count = 0
  query = "select count(p.id) from " + \
      "part p,message m,folder f,account a where " + \
      "p.message_id=m.id and " + \
      "m.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    count = row[0]
  cursor.close()
  return count

######################################################################
def count_unique_internal (cnx, account):
  # non-redundant internal_content count
  count = 0
  query = "select count(c.id) from " + \
      "internal_content c,folder f,account a where " + \
      "c.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    count = row[0]
  cursor.close()
  return count

######################################################################
def count_unique_external (cnx, account):
  # non-redundant external_content count
  count = 0
  query = "select count(c.id) from " + \
      "external_content c,folder f,account a where " + \
      "c.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    count = row[0]
  cursor.close()
  return count

######################################################################
def count_redundant_internal (cnx, account):
  # redundant internal_content count
  count = 0
  query = "select count(distinct p.id) from " + \
      "part p,message m,folder f,account a where " + \
      "p.internal_content_id is not null and " + \
      "p.message_id=m.id and " + \
      "m.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    count = row[0]
  cursor.close()
  return count

######################################################################
def count_redundant_external (cnx, account):
  # redundant external_content count
  count = 0
  query = "select count(distinct p.id) from " + \
      "part p,message m,folder f,account a where " + \
      "p.external_content_id is not null and " + \
      "p.message_id=m.id and " + \
      "m.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    count = row[0]
  cursor.close()
  return count

######################################################################
def count_attachment (cnx, account):
  # attachment count
  count = 0
  query = "select count(p.id) from " + \
      "part p,message m,folder f,account a where " + \
      "p.is_attachment=1 and " + \
      "p.message_id=m.id and " + \
      "m.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    count = row[0]
  cursor.close()
  return count

######################################################################
def count_multipart (cnx, account):
  # multipart count
  count = 0
  query = "select count(p.id) from " + \
      "part p,message m,folder f,account a where " + \
      "p.is_multipart=1  and " + \
      "p.message_id=m.id and " + \
      "m.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    count = row[0]
  cursor.close()
  return count

######################################################################
def count_grandchild_parts (cnx, account):
  # multipart count
  count = 0
  query = "select count(p.id) from " + \
      "part p,message m,folder f,account a where " + \
      "p.parent_sequence_id > 1 and " + \
      "p.message_id=m.id and " + \
      "m.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    count = row[0]
  cursor.close()
  return count

######################################################################
def redundant_content_size (cnx, account):
  # multipart count
  size = 0
  query = "select sum(p.content_length) from " + \
      "part p,message m,folder f,account a where " + \
      "p.message_id=m.id and " + \
      "m.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    size = row[0]
  cursor.close()
  return size

######################################################################
def unique_content_size (cnx, account):
  # multipart count
  size = 0
  # internal
  query = "select sum(i.content_length) from " + \
      "internal_content i,folder f,account a where " + \
      "i.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    if row[0]:
      size = row[0]
  cursor.close()
  # external
  query = "select sum(e.content_length) from " + \
      "external_content e,folder f,account a where " + \
      "e.folder_id=f.id and " + \
      "f.account_id=a.id and " + \
      "a.account_name='" + account + "';"
  cursor = cnx.cursor()
  cursor.execute(query)
  for row in cursor:
    if row[0]:
      size = size + row[0]
  cursor.close()
  return size

######################################################################
# Db classes
######################################################################
class DbAccount ():
  def __init__ (self, cnx, account_id):
    self.account_id     = account_id
    self.message_count  = 0
    self.start_date     = ""
    self.end_date       = ""
    self.folder_ids     = []
    self.folder_names   = []
    self.folders        = None
    self.from_count     = 0
    self.to_count       = 0
    self.cc_count       = 0
    self.bcc_count      = 0
    self.external_files = 0
    self.account_name   = get_account_name(cnx, account_id)
    self.account_directory = get_account_directory(cnx, account_id)

    (self.message_count, self.start_date, self.end_date) = \
        get_message_counts_for_account(cnx, account_id)
    (self.from_count, self.to_count, self.cc_count, self.bcc_count) = \
      get_address_counts_for_account(cnx, account_id)
    self.external_files = count_unique_external(cnx, self.account_name)
    self.folders = get_folders_for_account(cnx, account_id)
    for fld in self.folders:
      self.folder_ids.append(fld.folder_id)
      self.folder_names.append(fld.folder_name)

######################################################################
class DbAddress ():
  def __init__ (self, cnx, account_id, address_id, name_id):
    self.account_id   = account_id
    self.account_name = None
    self.address_id   = address_id
    self.address      = None
    self.name_id      = name_id
    self.email_name   = None
    self.name_ids     = []
    self.email_names  = []
    self.from_msgs = 0
    self.to_msgs   = 0
    self.cc_msgs   = 0
    self.bcc_msgs  = 0

    ## If name_id was specified, then filter on both address_id AND name_id
    ## and use self.name_id, self.email_name
    ## If name_id was NOT specified, then filter only on address_id
    ## and use self.name_ids, self.email_names

    self.account_name = get_account_name(cnx, account_id)

    query = "select address from address where id=" + str(address_id)
    cursor = cnx.cursor()
    cursor.execute(query)
    for row in cursor:
      self.address = row[0]
    cursor.close()

    if name_id:
      query = "select n.name from name n where n.id=" + str(name_id)
      cursor = cnx.cursor()
      cursor.execute(query)
      for row in cursor:
        self.email_name = row[0]
      cursor.close()
    else:
      query = "select n.id, n.name from name n, address_name an where " + \
          "an.address_id=" + str(address_id) + " and an.name_id=n.id"
      cursor = cnx.cursor()
      cursor.execute(query)
      for row in cursor:
        self.name_ids.append(row[0])
        self.email_names.append(row[1])
      cursor.close()
 
    self.from_msgs = get_uses(cnx, account_id, address_id, name_id, "From")
    self.to_msgs   = get_uses(cnx, account_id, address_id, name_id, "To")
    self.cc_msgs   = get_uses(cnx, account_id, address_id, name_id, "Cc")
    self.bcc_msgs  = get_uses(cnx, account_id, address_id, name_id, "Bcc")

######################################################################
class DbContent ():

  ####################################################################
  def __init__ (self, cnx, content_id, folder_id, sequence_id, content_length,
      content_type, transfer_encoding, is_attachment, is_internal, content_ident,
      xml_wrapped, charset):
    self.content_id        = content_id
    self.folder_id         = folder_id
    self.sequence_id       = sequence_id
    self.content_length    = content_length
    self.content_type      = content_type
    self.transfer_encoding = transfer_encoding
    self.is_attachment     = is_attachment
    self.is_internal       = is_internal
    self.content_ident     = content_ident
    self.stored_file_name   = None
    self.original_file_name = None
    if is_internal:
      self.fetch_internal(cnx, content_id)
    else:
      self.fetch_external(cnx, content_id)
    self.xml_wrapped       = xml_wrapped
    self.charset           = charset

  ####################################################################
  def fetch_internal (self, cnx, content_id):
    query = "select original_file_name from " + \
      "internal_content where id=" + str(content_id)
    cursor = cnx.cursor()
    cursor.execute(query)
    for row in cursor:
      self.original_file_name = row[0]
    cursor.close()

  ####################################################################
  def fetch_external (self, cnx, content_id):
    query = "select original_file_name, stored_file_name from " + \
      "external_content where id=" + str(content_id)
    cursor = cnx.cursor()
    cursor.execute(query)
    for row in cursor:
      self.original_file_name = row[0]
      if self.original_file_name == None or self.original_file_name == "":
        self.original_file_name = self.content_ident
      self.stored_file_name   = row[1]
    cursor.close()

######################################################################
class DbFolder ():
  def __init__ (self, cnx, folder_id):
    self.folder_id     = folder_id
    self.folder_name   = None
    self.account_id    = None
    self.account_name  = None
    self.message_count = 0
    self.start_date    = ""
    self.end_date      = ""
    (self.folder_name, self.account_id, self.account_name) = \
        get_folder_info(cnx, folder_id)
    (self.message_count, self.start_date, self.end_date) = \
        get_message_counts_for_folder(cnx, folder_id)

######################################################################
class DbMessage ():

  ####################################################################
  def __init__ (self, cnx, message_id):
    self.message_id      = None
    self.folder_id       = None
    self.folder_name     = None
    self.account_id      = None
    self.account_name    = None
    self.global_id       = None
    self.date_time       = None
    self.from_line        = ""
    self.to_line          = ""
    self.cc_line          = ""
    self.bcc_line         = ""
    self.subject_line     = ""
    self.date_line        = ""
    # any given message is 'In-Reply-To' at most one message
    self.reply_to_global  = ""
    self.reply_to_id      = ""
    # has_reply_id holds internal db message ids
    self.has_reply_global = []
    self.has_reply_id     = []
    # list of DbContent
    self.body_info        = []

    query = "select m.id,f.id,f.folder_name,a.id,a.account_name," + \
        "m.global_message_id,m.date_time " + \
        "from folder f,account a, message m where f.account_id=a.id and " + \
        "f.id=m.folder_id and m.id=" + str(message_id)
    cursor = cnx.cursor()
    cursor.execute(query)
    for row in cursor:
      self.message_id        = row[0]
      self.folder_id         = row[1]
      self.folder_name       = row[2]
      self.account_id        = row[3]
      self.account_name      = row[4]
      self.global_id         = row[5]
      self.date_time         = row[6]
    cursor.close()

    query = "select t.original_name,mh.header_value from message_header mh, tag t " + \
        "where t.original_name in ('Subject','From','To','Cc','Bcc','In-Reply-To','Date') and " + \
        "mh.tag_id=t.id and mh.message_id=" + str(message_id)
    cursor = cnx.cursor()
    cursor.execute(query)
    for row in cursor:
      tag   = row[0]
      value = row[1]
      if tag == "From":
        self.from_line = value
      elif tag == "To":
        self.to_line = value
      elif tag == "Cc":
        self.cc_line = value
      elif tag == "Bcc":
        self.bcc_line = value
      elif tag == "In-Reply-To":
        self.reply_to_global = value
      elif tag == "Subject":
        self.subject_line = value
      elif tag == "Date":
        self.date_line = value
    cursor.close()

    if self.reply_to_global != "":
      query = "select id from message where global_message_id='" + \
        escape_sql(self.reply_to_global) + "'"
      cursor = cnx.cursor()
      cursor.execute(query)
      for row in cursor:
        self.reply_to_id = row[0]
      cursor.close()

    self.fetch_replies(cnx, self.global_id)
    self.fetch_body(cnx, self.message_id)

  ####################################################################
  def fetch_replies (self, cnx, global_id):
    query = "select m.global_message_id, m.id " + \
      "from replyto r, message m where " + \
      "r.repliedto_id='" + escape_sql(global_id) + "' and " + \
      "r.replying_id=m.id"
    cursor = cnx.cursor()
    cursor.execute(query)
    for row in cursor:
      self.has_reply_global.append(row[0])
      self.has_reply_id.append(row[1])
    cursor.close()

  ####################################################################
  def fetch_body (self, cnx, message_id):
    if False:
      query = "select p.sequence_id, m.folder_id, p.content_length, " + \
      "ph1.header_value, " + \
      "ph2.header_value, " + \
      "p.is_attachment, p.internal_content_id, p.external_content_id, " + \
      "p.content_ident " + \
      "from part p, tag t1, tag t2, part_header ph1, part_header ph2, message m where " + \
      "p.message_id=" + str(message_id) + " and " + \
      "m.id=p.message_id and " + \
      "t1.original_name='Content-Type' and " + \
      "t1.id=ph1.tag_id and " + \
      "t2.original_name='Content-Transfer-Encoding' and " + \
      "t2.id=ph2.tag_id and " + \
      "ph1.part_id=p.id and " + \
      "ph2.part_id=p.id " + \
      "order by p.sequence_id"

    query = "select p.sequence_id, m.folder_id, p.content_length, " + \
      "ph1.header_value, " + \
      "p.is_attachment, p.internal_content_id, p.external_content_id, " + \
      "p.content_ident " + \
      "from part p, tag t1, part_header ph1, message m where " + \
      "p.message_id=" + str(message_id) + " and " + \
      "m.id=p.message_id and " + \
      "t1.original_name='Content-Type' and " + \
      "t1.id=ph1.tag_id and " + \
      "ph1.part_id=p.id " + \
      "order by p.sequence_id"

    cursor = cnx.cursor()
    cursor.execute(query)
    temp = []
    for row in cursor:
      temp.append(row)
    cursor.close()
    for row in temp:
      sequence_id       = row[0]
      folder_id         = row[1]
      content_length    = row[2]
      content_type      = row[3]
      transfer_encoding = None
#      transfer_encoding = row[4]
      is_attachment     = row[4]
      is_internal       = (row[5] != None and row[5] != "")
      content_id        = row[5] if is_internal else row[6]
      content_ident     = row[7]
      xml_wrapped       = False
      charset           = ''

      ## Content-Transfer-Encoding
      query = "select ph2.header_value from part p, " + \
        "part_header ph2," + \
        "tag t2 where " + \
        "t2.original_name='Content-Transfer-Encoding' and " + \
        "t2.id=ph2.tag_id and " + \
        "ph2.part_id=p.id and " + \
        "p.message_id=" + str(message_id) + " and " + \
        "p.sequence_id=" + str(sequence_id)
      cursor = cnx.cursor()
      cursor.execute(query)
      for row in cursor:
        transfer_encoding = row[0]
      cursor.close()

      ## charset
      query = "select ph2.header_value from part p, " + \
        "part_header ph2," + \
        "tag t2 where " + \
        "t2.original_name='charset' and " + \
        "t2.id=ph2.tag_id and " + \
        "ph2.part_id=p.id and " + \
        "p.message_id=" + str(message_id) + " and " + \
        "p.sequence_id=" + str(sequence_id)
      cursor = cnx.cursor()
      cursor.execute(query)
      for row in cursor:
        charset = row[0]
      cursor.close()

      if (not is_internal and content_id):
        query = "select xml_wrapped from external_content where id=" + str(content_id)
        cursor = cnx.cursor()
        cursor.execute(query)
        for row in cursor:
          xml_wrapped = row[0]
#      if content_length > 0 and transfer_encoding:
# we've encountered cases where there is no Content-Transfer-Encoding specified
# for a viewable content-type
      if content_length > 0 and content_type[0:10] != "multipart/":
        self.body_info.append(DbContent(cnx, content_id, \
            folder_id, sequence_id, content_length,
            content_type, transfer_encoding, is_attachment,
            is_internal, content_ident,
            xml_wrapped, charset))

