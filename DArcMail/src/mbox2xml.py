#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import sys
import os
import re
import string
import hashlib
import uuid
import mailbox
import email
import base64
import xml_common
import message_log
import stop_watch
import dm_common as dmc
import dm_defaults as dmd
from xml_common import message_predef_seq, body_predef_seq, \
    message_predef, body_predef, \
    isOtherMimeHeader, escape_xml

eol_chars           = "CRLF"
MLOG_NAME           = "MessageSummary"
SUBDIR_LEVELS = 1

## <RelPath>
## This can go on a <Message> or an <ExtBodyContent> or an <Mbox>.

## On an <Mbox> the path is the .mbox file path (including the ".mbox")
## but relative to the directory in which the XML file is placed.

## On a <Message>, the path identifies the directory that is the root
## directory that contains external body parts for this message, all relative
## to the directory in which the XML file is placed.
## In DArcMail, the root for storage of external body parts of a message is
## ALWAYS the directory that contains the .mbox file that contains the message.
## If the xml file is being generated for a single folder, then
## the xml file is placed in the directory containing the .mbox file,
## so in this case the <RelPath> on a <Message> is simply "."
## But if the xml file is generated for the entire account, then it is
## placed in the account directory; thus in this case, the <RelPath> on a
## <Message> is the path from the account directory to the directory
## that contains the .mbox file that contains the message.

## On a <ExtBodyContent>, the path identifies the file containing the
## external body content, relative to the <RelPath> of the <Message>
## that contains the <ExtBodyContent>.
## In DArcMail, the <RelPath> on an <ExtBodyContent> part is simply
## the name of the external file, optionally preceded by the automatically-
## create subdirectories (if this option was exercised).

## name/address formats
## "Last, First" <last@abc.gov>
na_case1 = '("([^"]+,?[^"]+") *<[^>]*>)'
## last@abc.gov
na_case2 = '([^",<> ]+@[^",<> ]+)'
## First Last <last@abc.gov>
na_case3 = '([^",]+ *<[^>]+>)'
na_rx = re.compile("(" + "|".join((na_case1, na_case2, na_case3)) + ")")

log_file_name = "mbox2db.log.txt"

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
    print("mbox file " + f + " does not exist")
    return False
  if not os.path.isfile(f):
    print(f + " must be a file, not a directory")
    return False
  if not os.access(f, os.R_OK):
    print("mbox file " + f + " is not readable")
    return False
  return True

######################################################################
## Mbox2Xml
######################################################################
class Mbox2Xml ():

  #####################################################################
  def __init__ (self, logf, account_name, output_chunksize):

    self.logf              = logf
    self.account_name      = account_name
    self.output_chunksize  = output_chunksize
    self.lc_original2xml   = self.build_tag_dictionary()
    self.xc                = xml_common.XMLCommon()

    self.warnings           = {}
    self.estimated_total_messages = 0
    self.total_mbox_messages = 0    ## number before removing duplicates
    self.skipped_duplicates  = 0
    self.total_msg_count     = 0    ## total messages in XML output
    self.message_count       = 0    ## resettable count for chunking
    self.account_output_files = []
    self.folder_output_files = []
    self.folder_file_idx  = 0
    self.external_content_files = 0
    self.sw = stop_watch.StopWatch()
    self.glidr = GlobalIdRecord()

  #####################################################################
  def build_tag_dictionary (self):
    dict = {
      "boundary" : "BoundaryString",
      "cc" : "Cc",
      "charset" : "Charset",
      "content-id" : "ContentId",
      "content-type" : "ContentType",
      "content-disposition" : "Disposition",
      "filename" : "DispositionFileName",
      "from" : "From",
      "in-reply-to" : "InReplyTo",
      "message-id" : "MessageId",
      "mime-version" : "MimeVersion",
      "date" : "OrigDate",
      "references" : "References",
      "sender" : "Sender",
      "subject" : "Subject",
      "to" : "To",
      "content-transfer-encoding" : "TransferEncoding",
      "bcc" : "Bcc"
    }
    return dict

  ######################################################################
  def warning (self, w):
    if w in self.warnings.keys():
      self.warnings[w] = self.warnings[w] + 1
    else:
      self.warnings[w] = 1

  ######################################################################
  def write_warnings (self):
    for w in self.warnings.keys():
      self.logf.write(w + " (" + str(self.warnings[w]) + " times)" + "\n")

  ######################################################################
  def write_log (self):
    for fn in self.account_output_files:
      self.logf.write("output file: " + fn + "\n")
    self.logf.write("\n")
    self.logf.write("########## WARNINGS ##########" + "\n")
    self.write_warnings()
    self.logf.write("\n")
    self.logf.write("########## SUMMARY ##########" + "\n")
    self.logf.write("total messages in mbox file(s): " + \
        str(self.total_mbox_messages) + "\n")
    self.logf.write("duplicate messages skipped: " + \
        str(self.skipped_duplicates) + "\n")
    self.logf.write("total messages in XML output: " + str(self.total_msg_count) + "\n")
    self.logf.write("external content files: " + str(self.external_content_files) + "\n")
    self.logf.write("\n")
    self.logf.write("########## ELAPSED TIME ##########" + "\n")
    self.logf.write(self.sw.elapsed_time() + "\n")

  ######################################################################
  def add_subdirs (self, storage_root, uuid):
    # take the second group
    d1 = uuid[ 9:11]
#    d2 = uuid[11:13]
    added_levels = d1
    subdir = os.path.join(storage_root, d1)
    if not os.path.exists(subdir):
      os.mkdir(subdir)
      os.chmod(subdir, dmd.EXTERNAL_CONTENT_DIRECTORY_PERMISSIONS)
    elif not os.path.isdir(subdir):
      print(subdir, " is not a directory ")
      sys.exit(1)
    return added_levels
  
  ######################################################################
  def get_content (self, part):
    content = part.as_string()
    # Skip headers, go to the content
    content_start = content.find("\n\n")
    # ..and skip the two linefeeds
    content = content[content_start+2:]
    m = re.match("^\s*$", content)
    if m:
      return (0, "")
    else:
      if part.get("Content-Transfer-Encoding") == "quoted-printable":
       # have to remove some illegal quoted-printable moves that mbox makes
        content = re.sub("(=\r\n)|(=\r)|(=\n)", "", content)
      return (len(content), content)

  ######################################################################
  def external_content (self, part, content, fd):
    ## we are not attempting to optimize external storage,
    ## so just go with the "full_sha1_digest"

    # uuid4 makes random uuids (not based on host id and time)
    cs_uuid = str(uuid.uuid4())
    stored_file_name = cs_uuid + ".xml"
    added_levels = self.add_subdirs(fd.folder_dir, cs_uuid)
    stored_file_name = os.path.join(added_levels, stored_file_name)
    full_stored_file_name = os.path.join(fd.folder_dir, stored_file_name)
    fh = open(full_stored_file_name, "w", errors="replace")
    os.chmod(full_stored_file_name, dmd.EXTERNAL_CONTENT_FILE_PERMISSIONS)
    self.external_content_files = self.external_content_files + 1

    content_type              = part.get_content_type()
    content_transfer_encoding = part.get("Content-Transfer-Encoding")
    disposition               = part.get("Content-Disposition")
    if re.match("attachment", disposition):
      disposition = "attachment"
    disposition_filename      = part.get_filename()

    fh.write("<ExternalBodyPart>" + "\n")
    fh.write("<LocalUniqueID>" + cs_uuid + "</LocalUniqueID>" + "\n")
    fh.write("<ContentType>" + content_type + "</ContentType>" + "\n")
    if disposition:
      fh.write("<Disposition>" + disposition + "</Disposition>" + "\n")
    if disposition_filename:
      fh.write("<DispositionFileName>" + escape_xml(disposition_filename) + \
          "</DispositionFileName>" + "\n")
    if content_transfer_encoding:
      fh.write("<ContentTransferEncoding>" + content_transfer_encoding + \
          "</ContentTransferEncoding>" + "\n")
    fh.write("<Content>" + "\n")
    if content_transfer_encoding == "base64":
      fh.write(content)
    else:
      fh.write(escape_xml(content))
    fh.write("</Content>" + "\n")
    fh.write("</ExternalBodyPart>" + "\n")
    fh.close()

    fh = open(full_stored_file_name)
    full_sha1  = hashlib.sha1()
    full_sha1.update(fh.read().encode(errors="replace"))
    full_sha1_digest = full_sha1.hexdigest()
    fh.close()
    return (stored_file_name, full_sha1_digest)

  #####################################################################
  def do_content (self, part, fd):
    (content_length, content_text) = self.get_content(part)
    if content_length < 1:
      return
    cd = part.get("Content-Disposition")
    is_attachment = cd and re.match("attachment", cd)
    if is_attachment:
      (stored_file_name, sha1_digest) = \
          self.external_content(part, content_text, fd)
      self.xc.ExtBodyContent("./" + stored_file_name,
          str(self.xc.getLocalId()), sha1_digest)
    else:
      transfer_encoding = part.get("Content-Transfer-Encoding")
      self.xc.Content(content_text, transfer_encoding)

 ######################################################################
  def do_child_message (self, part, fd, merrors):
    # <ChildMessage> needs to have these two initial headers
    # <LocalId>
    # <MessageID>    --- but this one appears to be absent...
    self.xc.prl("<SingleBody>")
    self.xc.indent()
    child = None
    ok = True
    pls = part.get_payload()
    if not pls or len(pls) == 0:
      ok = False
      me = "no MessageId for child"
      self.warning(me)
      merrors.append(me)
    elif len(pls) > 1:
      ok = False
      me = "child message has multiple parts"
      self.warning(me)
      merrors.append(me)
    else:
      child = pls[0]
      htags = child.keys()
      for tag in ["From", "To", "Date", "Subject"]:
        if tag not in htags:
          me = "child message lacks required header " + tag + ":"
          self.warning(me)
          merrors.append(me)
          ok = False
          break
    if ok:
      self.do_part_headers(part)
      self.xc.prl("<ChildMessage>")
      self.xc.indent()
      self.xc.terminal("LocalId", str(self.xc.getLocalId()))
      (predef, non_predef) = self.process_headers(child)
      if "MessageId" not in predef.keys():
          self.xc.terminal("MessageId", str(uuid.uuid4()))
      dummy = message_log.MessageLogRow()
      self.do_message_headers(child, dummy)
      self.do_part(child, fd, merrors, first_part=True)
      self.xc.exdent()
      self.xc.prl("</ChildMessage>")
    else:
      self.xc.Content(part.as_string(), transfer_encoding=None)
    self.xc.exdent()
    self.xc.prl("</SingleBody>")

 ######################################################################
  def do_part (self, part, fd, merrors, first_part=False):
    # parts with Content-Type == "message/..." are parsed
    # such that part.is_multipart() is True, but they
    # need to be chid messages
    # message/rfc822
    # message/delivery-status
    content_type = part.get_content_type()
    if re.match("message/rfc822", content_type):
      self.do_child_message(part, fd, merrors)
    elif re.match("message/", content_type):
      me = "skipping message part with ContentType: " + content_type
      self.warning(me)
      merrors.append(me)
    elif part.is_multipart():
      self.xc.prl("<MultiBody>")
      self.xc.indent()
      if not part.get_boundary():
        me = "multipart with no boundary string"
        self.warning(me)
        merrors.append(me)
      self.do_part_headers(part)
      # any content for a "main" multipart is a preamble
      if part.preamble:
        self.xc.terminal("Preamble", part.preamble)
      for subpart in part.get_payload():
        self.do_part(subpart, fd, merrors, first_part=False)
      self.xc.exdent()
      self.xc.prl("</MultiBody>")
    else:
      self.xc.prl("<SingleBody>")
      self.xc.indent()
      self.do_part_headers(part)
      self.do_content(part, fd)
      self.xc.exdent()
      self.xc.prl("</SingleBody>")
    if part.defects:
      for d in part.defects:
        me = "message defect: " + str(d)
        self.warning(me)
        merrors.append(me)

  ######################################################################
  def do_message_headers (self, em, mlrow):
    regular_date = None
    (predef, non_predef) = self.process_headers(em)
    for tag in message_predef_seq:
      if tag in predef.keys() and tag not in body_predef:
        if tag == "OrigDate":
          self.xc.terminal(tag, dmc.strict_datetime(predef[tag]))
          regular_date = predef[tag]
          mlrow.add_date(regular_date)
        else:
          if tag   == "From":
            mlrow.add_from(predef[tag])
          elif tag == "To":
            mlrow.add_to(predef[tag])
          elif tag == "Subject":
            mlrow.add_subject(predef[tag])
          elif tag == "Date":
            mlrow.add_date(regular_date)
          elif tag == "MessageId":
            mlrow.add_messageid(predef[tag])
          self.xc.terminal(tag, predef[tag])
    if regular_date:
      self.xc.Header("Date", regular_date)
    for tag in non_predef.keys():
      if not isOtherMimeHeader(tag):
        self.xc.Header(tag, non_predef[tag])

  ######################################################################
  def do_part_headers (self, part):
    (predef, non_predef) = self.process_headers(part)
    for tag in body_predef_seq:
      if tag in predef.keys() and tag not in message_predef:
        self.xc.terminal(tag, predef[tag])
    for tag in non_predef:
      if isOtherMimeHeader(tag):
        self.xc.OtherMimeHeader(tag, non_predef[tag])

  ######################################################################
  def process_headers (self, em):
    predef      = {}
    non_predef  = {}
    for (tag, value) in em.items():
      if not value:
#        self.warning("skipping tag " +  tag + "; empty value")
        continue
      xml_tag = ""
      ltag = tag.lower()
      if ltag in self.lc_original2xml.keys():
        xml_tag = self.lc_original2xml[ltag]
      if xml_tag != "":
        # it"s a predefined tag
        if xml_tag == "ContentType":
          predef[xml_tag] = em.get_content_type()
          charset = em.get_content_charset();
          if charset:
            predef["Charset"] = charset
          boundary = em.get_boundary()
          if boundary:
            predef["BoundaryString"] = boundary
        elif xml_tag == "TransferEncoding":
          predef[xml_tag] = value
        elif xml_tag == "Disposition":
          m = re.match("attachment", value)
          if m:
            predef[xml_tag] = "attachment"
            fn = em.get_filename()
            if fn:
              predef["DispositionFileName"] = fn
        else:
          predef[xml_tag] = value
      else:
        # it"s NOT a predefined tag
        non_predef[tag] = value
    return (predef, non_predef)

  ######################################################################
  def do_message (self, em, eol, sha1_digest, fd):
    merrors = []
    (predef, non_predef) = self.process_headers(em)
    mlrow = message_log.MessageLogRow()
    if "MessageId" not in predef.keys():
      merrors.append("no 'MessageId' header for message")
      return
    self.xc.prl("<Message>")
    self.xc.indent()
    self.xc.RelPath(".")
    self.xc.terminal("LocalId", str(self.xc.getLocalId()))
    self.do_message_headers(em, mlrow)
    self.do_part(em, fd, merrors, first_part=True)
    self.xc.terminal("Eol", eol)
    self.xc.Hash("SHA1", sha1_digest)
    self.xc.exdent()
    self.xc.prl("</Message>")
    mlrow.add_hash(sha1_digest)
    mlrow.add_errors(len(merrors))
    if len(merrors) > 0:
      mlrow.add_firstmessage(merrors[0])
    self.mlf.writerow(mlrow)

  #####################################################################
  def finish_output_file (self):
    xml_file = self.xc.get_fh()
    if xml_file:
      xml_file.close()
    self.xc.set_fh(None)
    self.mlog.close()
    self.mlog = None
    self.mlf  = None

  #####################################################################
  def make_file_names (self, fd):
    self.folder_file_idx += 1
    xml_file = os.path.join(fd.account_dir,
                            fd.folder_name + \
                            "_" + str(self.folder_file_idx) + ".xml")
    mlfn = os.path.join(fd.account_dir,
                            fd.folder_name + \
                            "_" + str(self.folder_file_idx) + ".csv")
    return (xml_file, mlfn)
    
  #####################################################################
  def new_output_file (self, fd):
    (xml_file, mlfn) = self.make_file_names(fd)
    fh = open(xml_file, "w", errors="replace")
    os.chmod(xml_file, dmd.XML_FILE_PERMISSIONS)
    self.folder_output_files.append(xml_file)
    self.xc.set_fh(fh)
    self.mlog = open(mlfn, "w", errors="replace")
    self.mlf = message_log.MessageLog(self.mlog)

  ######################################################################
  def do_mbox_folder (self, fd):
    self.xc.folder_head(fd.folder_name)
    mb = mailbox.mbox(fd.mbox_path, create=False)
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
      glid = em["Message-ID"]
      if glid and self.glidr.has_message(glid):
        self.skipped_duplicates = self.skipped_duplicates + 1
        self.warning("skipping duplicate message: " + glid + \
        " in folder " + fd.folder_name)
        continue
      else:
        self.glidr.add_message(glid)
      self.total_msg_count = self.total_msg_count + 1
      self.message_count = self.message_count + 1
      if self.xc.GetCurrentByteCount() > self.output_chunksize:
        ## <RelPath> on mbox is relative to where the XML is placed
        self.xc.finish_folder(os.path.basename(fd.folder_name), eol_chars)
        self.xc.finish_account()
        self.finish_output_file()
        self.new_output_file(fd)
        self.xc.xml_header()
        self.xc.account_head(self.account_name)
        self.xc.folder_head(fd.folder_name)
        self.message_count = 1
      self.do_message(em, eol, sha1_digest, fd)
      if em.defects:
        for d in em.defects:
          self.warning("msg " + str(i) + ", defect: " + d)
    ## <RelPath> on mbox is relative to where the XML is placed
    self.xc.finish_folder(os.path.basename(fd.folder_name), eol_chars)
    mb.close()

  ######################################################################
  def walk_folder (self, fd):
    self.folder_file_idx = 0
    self.folder_output_files = []
    self.new_output_file(fd)
    self.xc.xml_header()
    self.xc.account_head(self.account_name)
    if not check_file_for_reading(fd.mbox_path):
      sys.exit(0)
    self.do_mbox_folder(fd)
    self.xc.finish_account()
    self.finish_output_file()
    if len(self.folder_output_files) == 1:
      old_xml_file = self.folder_output_files[0]
      old_mlfn = re.sub(".xml$", ".csv", old_xml_file)
      old_stem = re.sub("_1.xml$", "", old_xml_file)
      new_xml_file = old_stem + ".xml"
      new_mlfn = old_stem + ".csv"
      if os.path.exists(new_xml_file):
        os.remove(new_xml_file)
      os.rename(old_xml_file, new_xml_file)
      if os.path.exists(new_mlfn):
        os.remove(new_mlfn)
      os.rename(old_mlfn, new_mlfn)
      self.folder_output_files[0] = new_xml_file
    self.account_output_files += self.folder_output_files
      
######################################################################
def main ():
  pass

######################################################################
if __name__ == "__main__":
  main()
