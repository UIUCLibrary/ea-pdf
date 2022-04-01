#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import sys
import os
import re
import string
import hashlib
import mailbox
import email
import base64
import db_access as dba
import content_store
import stop_watch
import dm_common as dmc
from xml_common import message_predef, body_predef, isOtherMimeHeader

## name/address formats
## "Last, First" <last@abc.gov>
na_case1 = '("([^"]+,?[^"]+") *<[^>]*>)'
## last@abc.gov
na_case2 = '([^",<> ]+@[^",<> ]+)'
## First Last <last@abc.gov>
na_case3 = '([^",]+ *<[^>]+>)'
## <last@abc.gov>
na_case4 = '(<[^",<> ]+@[^",<> ]+>)'
na_rx = re.compile('(' + '|'.join((na_case1, na_case2, na_case3, na_case4)) + ')')

log_file_name = 'mbox2db.log.txt'

######################################################################
class GlobalIdRecord ():
  def __init__ (self):
   self.glidr = set()
  def add_message (self, glid):
    self.glidr.add(glid)
  def has_message (self, glid):
    return glid in self.glidr

######################################################################
def check_file_for_reading (f):
  if not os.path.exists(f):
    print('mbox file ' + f + ' does not exist')
    return False
  if not os.path.isfile(f):
    print(f + ' must be a file, not a directory')
    return False
  if not os.access(f, os.R_OK):
    print('mbox file ' + f + ' is not readable')
    return False
  return True

######################################################################
## Mbox2Db
######################################################################
class Mbox2Db ():

  cnx                = None
  logf               = None
  account_name       = None
  account_directory  = None
  external_size      = None
  external_subdir_levels = None

  warnings           = {}
  id2original_tag    = {}
  lc_original_lookup = {}
  id2xml_tag         = {}
  xml_tag2id         = None
  original_tag2id    = None

  progress_dialog    = None

  #####################################################################
  def __init__ (self, cnx, logf, account_name, account_directory,
      account_id, external_attachment, external_subdir_levels):

    self.cnx               = cnx
    self.logf              = logf
    self.account_name      = account_name
    self.account_directory = account_directory
    self.account_id        = account_id
    self.external_attachment     = external_attachment
    self.external_subdir_levels = external_subdir_levels

    self.build_tag_dictionaries()
    self.sw = stop_watch.StopWatch()
    self.glidr = GlobalIdRecord()

    self.estimated_total_messages = 0
    self.total_mbox_messages = 0    ## number before removing duplicates
    self.skipped_duplicates  = 0
    self.total_msg_count     = 0    ## total messages loaded into db

  #####################################################################
  def build_tag_dictionaries (self):
    self.xml_tag2id      = dba.xml_tag_dictionary(self.cnx)
    self.original_tag2id = dba.original_tag_dictionary(self.cnx)
    for (tag, id) in self.original_tag2id.items():
      self.id2original_tag[id] = tag
      self.lc_original_lookup[tag.lower()] = tag
    for (tag, id) in self.xml_tag2id.items():
      self.id2xml_tag[id] = tag

  ######################################################################
  def warning (self, w):
    if w in self.warnings.keys():
      self.warnings[w] = self.warnings[w] + 1
    else:
      self.warnings[w] = 1

  ######################################################################
  def write_warnings (self):
    for w in self.warnings.keys():
      self.logf.write(w + ' (' + str(self.warnings[w]) + ' times)' + "\n")

  ######################################################################
  def get_stats (self):
    account = self.account_name
    lines = []
    lines.append('Database contents for account \'' + account + '\'')
    lines.append('  messages: ' +  "{:,}".format(
        dba.count_message (self.cnx, account)))
    lines.append('  parts: ' + "{:,}".format(
        dba.count_part (self.cnx, account)))
    lines.append('  unique internally stored content: ' "{:,}".format(
        dba.count_unique_internal (self.cnx, account)))
    lines.append('  unique externally stored content: ' "{:,}".format(
        dba.count_unique_external (self.cnx, account)))
    lines.append('  redundant internally stored content: ' "{:,}".format(
        dba.count_redundant_internal (self.cnx, account)))
    lines.append('  redundant externally stored content: ' "{:,}".format(
        dba.count_redundant_external (self.cnx, account)))
    lines.append('  attachments: ' "{:,}".format(
        dba.count_attachment (self.cnx, account)))
    lines.append('  unique content size: ' + "{:,}".format(
        dba.unique_content_size (self.cnx, account)))
    lines.append('  redundant content size: ' + "{:,}".format(
        dba.redundant_content_size (self.cnx, account)))
    return '\n'.join(lines) + '\n'

  ######################################################################
  def write_log (self):
    self.logf.write('########## WARNINGS ##########' + "\n")
    self.write_warnings()
    self.logf.write("\n" + '########## SUMMARY ##########' + "\n")
    self.logf.write('total messages in mbox file(s): ' + \
        str(self.total_mbox_messages) + "\n")
    self.logf.write('duplicate messages skipped: ' + \
        str(self.skipped_duplicates) + "\n")
    self.logf.write('total messages loaded: ' + str(self.total_msg_count) + "\n")
    self.logf.write(self.get_stats())
    self.logf.write("\n" + '########## ELAPSED TIME ##########' + "\n")
    self.logf.write(self.sw.elapsed_time() + "\n")

  ######################################################################
  def do_address_list (self, line):

    line = re.sub('[\n\r\t]+', ' ', re.sub('\\\\[ntr]', ' ', line)).strip()
    line = line.lower()
    addr_list = []
    if line == '':
      return addr_list

    pairs = na_rx.findall(line)

    for pair in pairs:
      ## findall returns a tuple because we have groups in the rx
      pair = pair[0].strip()
      name       = None
      address    = None
      name_id    = None
      address_id = None

      ## look for a name
      m = re.match('([^<]+)<', pair)
      if m:
        name = m.group(1).strip()
        m1 = re.match('^"(.+)"$', name)
        if m1:
          name = m1.group(1)
          m2 = re.match("^'(.+)'$", name)
          if m2:
            name = m2.group(1)
      ## look for an address
      m = re.match(".*<'?([^'>]+)'?>", pair)
      if m:
        address = m.group(1)
      if not name and not address:
        m = re.match('"([^"@ ]+@[^"@ ]+)"', pair)
        if m:
          address = m.group(1)
          name    = address
        else:
          name = pair

      if name:
        name_id = dba.get_name_id(self.cnx, name, True)
      if address:
        address_id = dba.get_address_id(self.cnx, address, True)
      elif name:
        address_id = dba.get_address_id(self.cnx, name, True)

      if name_id or address_id:
        if name_id and address_id:
          dba.insert_address_name(self.cnx, address_id, name_id)
        addr_list.append((address_id, name_id))

    return addr_list

  ######################################################################
  def do_part (self, cs, message_id, part, part_sequence_id,
      parent_sequence_id, merrors):

    # parent_sequence_id == 0 iff this is the main message
    # else, parent_sequence_id is the sequence id of the multipart
    # part of which this is a part...

    # parts with Content-Type == 'message/...' are parsed
    # such that part.is_multipart() is True, but they
    # need to be chid messages
    # message/rfc822
    # message/delivery-status

    j = part_sequence_id
    content_type = part.get_content_type()
    multipart = part.is_multipart()
    (predef, non_predef) = self.process_headers(part)
    if part.is_multipart() and 'BoundaryString' not in predef.keys():
      me = 'multipart with no boundary string'
      self.warning(me)
      merrors.append(me)
      multipart = False
    part_id = dba.insert_part(self.cnx, message_id, part_sequence_id,
        parent_sequence_id, multipart)
    self.do_part_headers(part_id, part_sequence_id, predef, non_predef)
    is_attachment = 'Disposition' in predef.keys() and predef['Disposition'] == 'attachment'
    dba.set_is_attachment(self.cnx, part_id, is_attachment)
    cs.store_content(part, part_id, part_sequence_id, is_attachment)
    if part.is_multipart():
      for subpart in part.get_payload():
        j = j+1
        j = self.do_part(cs, message_id, subpart, j, part_sequence_id, merrors)
    if part.defects:
      for d in part.defects:
        self.warning('msg defect: ' + str(d))
    return j

  ######################################################################
  def do_message_headers (self, message_id, em):
    (predef, non_predef) = self.process_headers(em)
    ## message_search presumes the existence of a Subject header
    ## message display assumes Subject, OrigDate, From, To
    gmid = em['Message-ID']
    if not gmid:
      gmid = '-'
    for t in ['OrigDate', 'Subject', 'To', 'From']:
      if t not in predef.keys():
        predef[t] = '-'
        if t == 'OrigDate':
          self.warning('No "Date" header for message ' + gmid)
        else:
          self.warning('No "' + t + '" header for message ' + gmid)
    for (tag, value) in predef.items():
      id = self.xml_tag2id[tag]
      if not id:
        id = original_tag2id[tag]
      if id and tag in message_predef and tag not in body_predef:
        dba.insert_message_header(self.cnx, message_id, id, value)
      if tag in ('From', 'To', 'Cc', 'Bcc'):
        for (address_id, name_id) in self.do_address_list(value):
          dba.insert_message_address(self.cnx, message_id, id,
              address_id, name_id)
      elif tag == 'InReplyTo':
        dba.insert_replyto(self.cnx, message_id, value)

  ######################################################################
  def do_part_headers (self, part_id, part_seq_id, predef, non_predef):
    for (tag, value) in predef.items():
      id = self.xml_tag2id[tag]
      if not id:
        id = original_tag2id[tag]
      if id and tag in body_predef and tag not in message_predef:
        dba.insert_part_header(self.cnx, part_id, id, value)
 
  ######################################################################
  def process_headers (self, em):

    global lc_original_lookup, xml_tag2id, original_tag2id, id2xml_tag

    predef      = {}
    non_predef  = {}
    for (tag, value) in em.items():
      if not value:
#        self.warning('skipping tag ' +  tag + '; empty value')
        continue
      ltag = tag.lower()
      if ltag not in self.lc_original_lookup.keys():
        continue
      tag     = self.lc_original_lookup[ltag]
      id      = self.original_tag2id[tag]
      xml_tag = ''
      if id in self.id2xml_tag.keys():
        xml_tag = self.id2xml_tag[id]
      if xml_tag != '':
        if xml_tag == 'ContentType':
          predef[xml_tag] = em.get_content_type()
          charset = em.get_content_charset();
          if charset:
            predef['Charset'] = charset
          boundary = em.get_boundary()
          if boundary:
            predef['BoundaryString'] = boundary
        elif xml_tag == 'TransferEncoding':
          predef[xml_tag] = value
        elif xml_tag == 'Disposition':
          m = re.match('attachment', value)
          if m:
            predef[xml_tag] = 'attachment'
            fn = em.get_filename()
            if fn:
              predef['DispositionFileName'] = fn
        else:
          predef[xml_tag] = value
      else:
        non_predef[tag] = value
    return (predef, non_predef)

  ######################################################################
  def do_message (self, cs, folder_id, em, eol, sha1_digest):
    merrors = []
    global_message_id = em['Message-ID']
    if not global_message_id:
      global_messsage_id = '-'
      self.warning('No "Message-ID" header for message')
      return
    else:
      if self.glidr.has_message(global_message_id):
        self.warning('Skipping duplicate message: ' + global_message_id + \
            ' in folder ' + dba.get_folder_info(self.cnx, folder_id)[0])
        return
      self.glidr.add_message(global_message_id)
    (date_line, zoffset) = dmc.mail_date2datetime (em['Date'])

    if date_line is not None:
      if self.folder_date_from is None or \
          date_line < self.folder_date_from:
        self.folder_date_from = date_line
      if self.folder_date_to is None or \
          date_line > self.folder_date_to:
        self.folder_date_to = date_line
    self.folder_n_messages += 1
    
    message_id = dba.insert_message(self.cnx, folder_id, global_message_id, eol,
        sha1_digest, date_line, zoffset)
    if not message_id:
      print('message insert failed')
      sys.exit(1)
    # count of messages actually stored
    self.total_msg_count = self.total_msg_count + 1
    self.do_message_headers(message_id, em)
    self.do_part(cs, message_id, em, 1, 0, merrors)

  ######################################################################
  def do_mbox_folder (self, fd):
    
    self.folder_n_messages = 0
    self.folder_date_from = None
    self.folder_date_to = None
    
    mb = mailbox.mbox(fd.mbox_path, create=False)
    cs = content_store.ContentStore(self.cnx, fd,
        self.external_attachment, self.external_subdir_levels)
    for i in range(len(mb)):
      self.total_mbox_messages = self.total_mbox_messages + 1
      mes = mb.get_message(i)
      (mes_string, errors) = dmc.get_message_as_string(mes)
      if mes_string is None:
        for er in errors:
          self.warning(er)
        continue
      (eol, sha1_digest) = dmc.message_hash(mes_string)
      em = email.message_from_string(mes_string)
      glid = em['Message-ID']
      if glid:
        if self.glidr.has_message(glid):
          self.skipped_duplicates = self.skipped_duplicates + 1
          self.warning('Skipping duplicate message: ' + glid + \
              ' in folder ' + dba.get_folder_info(self.cnx, fd.folder_id)[0])
          continue
      else:
        self.glidr.add_message(glid)
      try:
        self.do_message(cs, fd.folder_id, em, eol, sha1_digest)
        self.cnx.commit()
      except Exception as e:
        self.warning(str(e))
        print(str(e))
    mb.close()
    
    if self.folder_n_messages > 0:
      dba.update_folder_info(self.cnx, fd.folder_id,
                             self.folder_n_messages,
                             self.folder_date_from,
                             self.folder_date_to)
      self.cnx.commit()

######################################################################
def main ():
  pass

######################################################################
if __name__ == "__main__":
  main()


