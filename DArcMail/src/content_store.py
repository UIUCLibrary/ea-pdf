#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import uuid
import sys
import os
import hashlib
import re
import db_access as dba
import dm_defaults as dmd
from xml_common import escape_xml, cdata

import quopri

# for uuid1, could use these substrings to make the directory levels:
# level1: uuid[9:11]
# level2: uuid[11:13]
# this would give 256*256=66536 level-2 directories

######################################################################
class ContentStore ():

  ######################################################################
  def __init__ (self, cnx, fd, external_attachment,
      external_subdir_levels):
    self.cnx                    = cnx
    self.folder_id              = fd.folder_id
    self.folder_directory       = fd.folder_dir
    self.external_attachment    = external_attachment
    self.external_subdir_levels = external_subdir_levels

  ######################################################################
  def is_attachment (self, part):
    cd = part.get("Content-Disposition")
    if cd:
      m = re.match("^attachment", cd)
      if m:
        return True
    return False

  ######################################################################
  def add_subdirs (self, storage_root, uuid):

    # take the second group
    d1 = uuid[ 9:11]
    d2 = uuid[11:13]
    SUBDIR_LEVELS = self.external_subdir_levels
    added_levels = ""
    if SUBDIR_LEVELS == 0:
      return added_levels
    elif SUBDIR_LEVELS > 2:
      print("too many subdir levels: ", SUBDIR_LEVELS)
      sys.exit(1) 
    else:
      subdir = os.path.join(storage_root, d1)
      added_levels = d1
      if not os.path.exists(subdir):
        os.mkdir(subdir)
        os.chmod(subdir, dmd.EXTERNAL_CONTENT_DIRECTORY_PERMISSIONS)
      elif not os.path.isdir(subdir):
        print(subdir, " is not a directory ")
        sys.exit(1)
      if SUBDIR_LEVELS == 1:
        return added_levels
      else:                   # SUBDIR_LEVELS == 2
        subdir = os.path.join(subdir, d2)
        added_levels = os.path.join(d1, d2)
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
    m = re.match("^\s+$", content)
    if m:
      return (0, "")
    else:
      if part.get("Content-Transfer-Encoding") == "quoted-printable":
       # have to remove some illegal quoted-printable moves that mbox makes
        content = re.sub("(=\r\n)|(=\r)|(=\n)", "", content)
      return (len(content), content)

  ######################################################################
  def get_preamble (self, part):
    content = part.preamble
    if content:
      return(len(content), content)
    else:
      return(0, "")

  ######################################################################
  def external_content (self, part, original_file_name,
                        content_length, content):
    raw_sha1  = hashlib.sha1()
    raw_sha1.update(content.encode(errors="replace"))
    raw_sha1_digest = raw_sha1.hexdigest()

    external_content_id = dba.existing_external_content(self.cnx, \
        raw_sha1_digest, content_length, original_file_name,
        self.folder_id)

    if not external_content_id:
      # uuid4 makes random uuids (not based on host id and time)
      cs_uuid = str(uuid.uuid4())
      stored_file_name = ""

      stored_file_name = cs_uuid + ".raw"

      added_levels = self.add_subdirs(self.folder_directory, cs_uuid)
      stored_file_name = os.path.join(added_levels, stored_file_name)
      full_stored_file_name = os.path.join(self.folder_directory,
                                           stored_file_name)
      fh = open(full_stored_file_name, "w", errors="replace")
      os.chmod(full_stored_file_name, dmd.EXTERNAL_CONTENT_FILE_PERMISSIONS)

      fh.write(content)
      fh.close()
      external_content_id = dba.insert_external_content(self.cnx,
          original_file_name, stored_file_name,
          self.folder_id, content_length,
          raw_sha1_digest, xml_wrapped=False)

    return (external_content_id, raw_sha1_digest, content_length)

  ######################################################################
  def internal_content (self, part, original_file_name,
                        content_length, content):
    sha1  = hashlib.sha1()
    sha1.update(content.encode(errors="replace"))
    sha1_digest = sha1.hexdigest()
    internal_content_id = \
        dba.existing_internal_content(self.cnx, sha1_digest,
                                      content_length,
        original_file_name,self.folder_id)
    if not internal_content_id:
      internal_content_id = dba.insert_internal_content(self.cnx, content,
          original_file_name, self.folder_id, content_length, sha1_digest)
    return (internal_content_id, sha1_digest, content_length)

  ######################################################################
  def store_content (self, part, part_id, part_sequence_id, is_attachment):
    (content_length, content) = (0, "")
    if part.is_multipart() and part_sequence_id == 1:
      ## if it is the "main" part of a message (sequence_id == 1) AND
      ## it is a multipart, THEN
      ## whatever we store as content is to be interpreted as a preamble
      ## (..., well it MAY be a preamble)
      (content_length, content) = self.get_preamble(part)
      if content_length == 0:
        (content_length, content) = self.get_content(part)
    else:
      (content_length, content) = self.get_content(part)
    if (content_length > 0):
      if is_attachment and self.external_attachment:
        (external_content_id, content_sha1, content_length) = \
            self.external_content(part,
            (part.get_filename() if is_attachment else None),
            content_length, content)
        dba.add_external_content(self.cnx, part_id,
            external_content_id, content_length, content_sha1)
      else:
        (internal_content_id, content_sha1, content_length) = \
            self.internal_content(part,
            (part.get_filename() if is_attachment else None),
            content_length, content)
        dba.add_internal_content(self.cnx, part_id,
            internal_content_id, content_length, content_sha1)
