#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import sys
import os
import string
import re
import dm_common as dmc
import folder_data as fld

#from xml.sax.saxutils import escape, unescape

indent_space = 2
spaces = "                                        " + \
         "                                        "
# "]" = &#093;
# "<" = &#060;
# ">" = &#062;
# "=" = &#061;

# left singlequote  = &#145; \x91
# right singlequote = &#146; \x92
# left doubldquote  = &#147; \x93
# left doubldquote  = &#148; \x94
# bullet            = &#149; \x95
# endash            = &#150; \x96
# emdash            = &#151; \x97

# nbsp              = &#160; \xAO
# registered tm     = &#174; \xAE
# middle dot        = &#183; \xB7

message_predef = set()
message_predef.add("RelPath")
message_predef.add("LocalId")
message_predef.add("MessageId")
message_predef.add("MimeVersion")

## following are legit for top-level message OR child message
## top-level MUST have From, Date, and at least one of (To, Cc, Bcc)
## child MUST have at least one of (From, Subject, Date)
message_predef.add("OrigDate")
message_predef.add("From")
message_predef.add("Sender")
message_predef.add("To")
message_predef.add("Cc")
message_predef.add("Bcc")
message_predef.add("InReplyTo")
message_predef.add("References")
message_predef.add("Subject")
message_predef.add("Comments")
message_predef.add("Keywords")

message_predef_seq = (
  "RelPath",
  "LocalId",
  "MessageId",
  "MimeVersion",
  "OrigDate",
  "From",
  "Sender",
  "To",
  "Cc",
  "Bcc",
  "InReplyTo",
  "References",
  "Subject",
  "Comments",
  "Keywords"
)

single_body_predef = set()
single_body_predef.add("ContentType")
single_body_predef.add("Charset")
single_body_predef.add("ContentName")
single_body_predef.add("ContentTypeComments")
#single_body_predef.add("ContentTypeParam")     # complex type
single_body_predef.add("TransferEncoding")
single_body_predef.add("TransferEncodingComments")
single_body_predef.add("ContentId")
single_body_predef.add("ContentIdComments")
single_body_predef.add("Description")
single_body_predef.add("DescriptionComments")
single_body_predef.add("Disposition")
single_body_predef.add("DispositionFileName")
single_body_predef.add("DispositionComments")
#single_body_predef.add("DispositionParam")     # complex type
#single_body_predef.add("OtherMimeHeader")       # complex type
single_body_predef_seq = (
  "ContentType",
  "Charset",
  "ContentName",
  "ContentTypeComments",
  "TransferEncoding",
  "TransferEndocingComments",
  "ContentId",
  "ContentIdComments",
  "Description",
  "DescriptionComments",
  "Disposition",
  "DispositionFileName",
  "DispositionComments"
)

multi_body_predef = set()
multi_body_predef.add("ContentType")
multi_body_predef.add("Charset")
multi_body_predef.add("ContentName")
multi_body_predef.add("BoundaryString")        # only in multibody
multi_body_predef.add("ContentTypeComments")
#multi_body_predef.add("ContentTypeParam")     # complex type
multi_body_predef.add("TransferEncoding")
multi_body_predef.add("TransferEncodingComments")
multi_body_predef.add("ContentId")
multi_body_predef.add("ContentIdComments")
multi_body_predef.add("Description")
multi_body_predef.add("DescriptionComments")
multi_body_predef.add("Disposition")
multi_body_predef.add("DispositionFileName")
multi_body_predef.add("DispositionComments")
#multi_body_predef.add("DispositionParam")     # complex type
#multi_body_predef.add("OtherMimeHeader")      # complex type
multi_body_predef.add("Preamble")              # only in multibody

body_predef_seq = (
  "ContentType",
  "Charset",
  "ContentName",
  "BoundaryString",
  "ContentTypeComments",
  "TransferEncoding",
  "TransferEncodingComments",
  "ContentId",
  "ContentIdComments",
  "Description",
  "DescriptionComments",
  "Disposition",
  "DispositionFileName",
  "DispositionComments",
  "Preamble"
)

body_predef = set()
for t in single_body_predef:
  body_predef.add(t)
for t in multi_body_predef:
  body_predef.add(t)

## heuristic (for now): how to tell an "other header" or an
## "other mime header":
##   if it's not a message_predef AND not a body_predef, then:
##     if parsing the main message headers
##       if it begins with '[Cc]ontent-' then:
##         it's a an "other mime header", so save for the part headers
##        else:
##         it's a "other header"
##     else (parsing the part headers):
##       it's an "other mime header"

#####################################################################
def isOtherMimeHeader (name):
  if name not in message_predef and name not in body_predef:
    m = re.match("content-", name,re.I)
    if m:
      return True
  return False

#####################################################################
def cdata (s):
#  return "<![CDATA[" + s + "]]>"
  return "<![CDATA[" + s.replace("]]", "&#093;&#093;") + "]]>"

#####################################################################
def escape_xml (s):
  if s:
    s = dmc.remove_illegal_ascii(s)
    m = re.match(".*[<>&]", s, re.DOTALL)
    if m:
      return cdata(s)
    else:
      return s
  else:
    return s

######################################################################
class XMLCommon ():

  indent_level = 0
  _LocalId_    = 1000
  fh           = None
  nbytes       = 0
  
  ######################################################################
  def __init__ (self):
    pass

  #####################################################################
  def GetCurrentByteCount (self):
    return self.nbytes
    
  #####################################################################
  def getLocalId (self):
    self._LocalId_ = self._LocalId_ + 1
    return self._LocalId_

  #####################################################################
  def get_fh (self):
    return self.fh
  
  #####################################################################
  def set_fh (self, xml_output_filehandle):
    self.nbytes = 0
    self.fh = xml_output_filehandle
  
  #####################################################################
  def pr (self, s):
    if self.indent_level > 0:
      i = self.indent_level * indent_space
      if self.fh:
        self.nbytes += self.fh.write(spaces[0:i] + s)
      else:
        sys.stdout.write(spaces[0:i] + s)
    else:
      if self.fh:
        self.nbytes += self.fh.write(s)
      else:
        sys.stdout.write(s)
  
  #####################################################################
  def prl (self, s):
    self.pr(s + "\n")

  #####################################################################
  def indent (self):
    self.indent_level = self.indent_level + 1

  #####################################################################
  def exdent (self):
    self.indent_level = self.indent_level - 1

  ######################################################################
  def terminal (self, tag_name, tag_value):
    self.prl("<" + tag_name + ">" + escape_xml(tag_value) + \
                    "</" + tag_name + ">")

  ######################################################################
  def xml_header (self):
    self.prl('<?xml version="1.0" encoding="UTF-8"?>')

  #####################################################################
  def account_head (self, account_name):
    self.prl( \
      '<Account xmlns="http://www.archives.ncdcr.gov/mail-account"' + \
      "\n" + '  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"' + \
      "\n" + '  xsi:schemaLocation="http://www.history.ncdcr.gov/SHRAB/' + \
      'ar/emailpreservation/mail-account/mail-account.xsd">')
    self.indent()
    self.terminal("GlobalId", account_name)
  
  #####################################################################
  def finish_account (self):
    self.exdent()
    self.prl("</Account>")

  #####################################################################
  def folder_head (self, folder_name):
    self.prl("<Folder>")
    self.indent()
    self.terminal("Name", folder_name)

  #####################################################################
  def finish_folder (self, folder_name, eol_chars):
    self.Mbox(os.path.join(".", folder_name + ".mbox"),
                             eol_chars)
    self.exdent()
    self.prl("</Folder>")
  
  ######################################################################
  def Mbox (self, path, eol_chars):
    self.prl("<Mbox>")
    self.indent()
    self.RelPath(path)
    self.terminal("Eol", eol_chars)
    self.exdent()
    self.prl("</Mbox>")

  ######################################################################
  def XMLWrapped (self, bool):
    return self.terminal("XMLWrapped", ("true" if bool else "false"))

  ######################################################################
  def Hash (self, algorithm, sha1_digest):
    self.prl("<Hash>")
    self.indent()
    self.terminal("Value", sha1_digest)
    self.terminal("Function", algorithm)
    self.exdent()
    self.prl("</Hash>")

  ######################################################################
  def Header (self, tag_name, tag_value):
    self.prl("<Header>")
    self.indent()
    self.terminal("Name", tag_name)
    self.terminal("Value", tag_value)
    self.exdent()
    self.prl("</Header>")
  
  ######################################################################
  def OtherMimeHeader (self, tag_name, tag_value):
    self.prl("<OtherMimeHeader>")
    self.indent()
    self.terminal("Name", tag_name)
    self.terminal("Value", tag_value)
    self.exdent()
    self.prl("</OtherMimeHeader>")

  ######################################################################
  def RelPath (self, relpath):
    self.terminal("RelPath", dmc.os_common_path(relpath))

  ######################################################################
  def ExtBodyContent (self, relpath, localid, sha1):
    self.prl("<ExtBodyContent>")
    self.indent()
    self.RelPath(relpath)
    self.terminal("LocalId", localid)
    self.XMLWrapped(True)
    ## <Eol> is optional
    ## Hash is optional, but useful
    self.Hash("SHA1", sha1)
    self.exdent()
    self.prl("</ExtBodyContent>")

  ######################################################################
  def Content (self, content, transfer_encoding):
    self.prl("<BodyContent>")
    if transfer_encoding != "base64":
      content = escape_xml(content)
    self.prl("<Content>" + content + "</Content>")
    self.prl("</BodyContent>")
