#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import sys
import time
import os
import re
import hashlib
import xml_common

COMMON_PATH_SEP = "/"

CHARACTER_SET = "utf8"
# python does not recognize "latin1"
# mysql does not recognize "iso-8859-1"

CHUNK_BYTES = (2**31)-200000
"""
(2**30) = 1GB
(2**31) = 2GB
200,000 used as estimate of a larger XML message size minus attachments,
an estimated buffer to keep a chunk < target limit in size
(2**31)-200,000 = 2,147,463,648
"""

NOW         = time.time()
DAY_SECONDS = 24*60*60
DEFAULT_LAG = DAY_SECONDS * 31

month2num = {
   "Jan" : 1,
   "Feb" : 2,
   "Mar" : 3,
   "Apr" : 4,
   "May" : 5,
   "Jun" : 6,
   "Jul" : 7,
   "Aug" : 8,
   "Sep" : 9,
   "Oct" : 10,
   "Nov" : 11,
   "Dec" : 12
}

illegal_ascii_pat = re.compile(".*([\000-\010]|[\013-\014]|[\016-\037])", re.DOTALL)
# illegal decimal values: 0-8, 11-12, 14-31
illegal_ascii = set()
for a in range(9):
  illegal_ascii.add(a)
for a in (11, 12):
  illegal_ascii.add(a)
for a in range(14, 32):
  illegal_ascii.add(a)

######################################################################
def os_common_path (path):
  return re.sub("\\\\", COMMON_PATH_SEP, os.path.normcase(path))

######################################################################
def YMD (seconds):
  return time.strftime("%Y-%m-%d", time.localtime(seconds))

######################################################################
def MonthOf (seconds):
  return time.strftime("%m", time.localtime(seconds))

######################################################################
def YearOf (seconds):
  return time.strftime("%Y", time.localtime(seconds))

######################################################################
TODAY       = time.strftime("%Y-%m-%d", time.localtime(NOW))

######################################################################
def strict_datetime (s):
  (dt, zoffset) = mail_date2datetime(s)
  (d, t) = re.split(" ", dt)
  m = re.match("([+\-])([0-9]{2})([0-9]{2})$", zoffset)
  if m:
    zoffset = m.group(1) + m.group(2) + ":" + m.group(3)
  return d + "T" + t + zoffset

######################################################################
def mail_date2datetime (d):
  if not d:
    return ("0000-00-00 00:00:00", "+0000")

  exp = \
        "([^0-9]+)?" + \
        "(\d+)" + \
        "[^a-zA-Z]+" \
        "([a-zA-Z]+) " + \
        "(\d{4}).+" + \
        "(\d\d:\d\d(:\d\d))[^0-9\-+]+" + \
        "([\-+]\d\d:?\d\d)?"
  m = re.match(exp, d)
  if m:
    day    = m.group(2)
    month  = m.group(3)
    if month in month2num.keys():
      month = month2num[month]
    else:
      month = "0"
    year   = m.group(4)
    time   = m.group(5)
    offset = m.group(7)
    if not offset:
      offset = ""
    return (year + "-" + "{:02d}".format(int(month)) + \
        "-" "{:02d}".format(int(day)) + " " + time, offset)
  else:
    return ("0000-00-00 00:00:00", "+0000")

######################################################################
def get_message_as_string (msg):
  msg_bytes = None
  msg_string = None
  msg_id = ""
  errors = []
  try:
    msg_id = msg.get("Message-ID")
  except Exception as e0:
    er = "ERROR0 getting Message-ID: " + str(e0)
    errors.append(er)
    self.stderr.write(er + "\n")
  try:
    msg_string = msg.as_string()
  except Exception as e1:
    er = "ERROR1 getting message as string: " + msg_id + " " + str(e1)
    sys.stderr.write(er + "\n")
    try:
      msg_bytes  = msg.as_bytes(unixfrom=True)
      msg_string = msg_bytes.decode(errors="replace")
    except Exception as e2:
      er = "ERROR2 getting message as string: " + msg_id + " " + str(e2)
      sys.stderr.write(er + "\n")
  return (msg_string, errors)

######################################################################
def message_hash (s):
  eol = None
  n = len(s)
## can't seem to get this eol match to work correctly
## not sure why...
  if s.rfind("\r") == n-1 or s.rfind("\r\n") == n-2:
    eol = "CRLF"
  elif s.rfind("\n") == n-1:
    eol = "LF"
  else:
    eol = None
  sha1  = hashlib.sha1()
  sha1.update(s.encode(errors="replace"))
  return (eol, sha1.hexdigest())

#####################################################################
def remove_illegal_ascii (s):
  if illegal_ascii_pat.match(s):
    chars = []
    for c in s:
      if ord(c) in illegal_ascii:
        # mark replaced character with a "middle dot"
        chars.append("&#183;")
      else:
        chars.append(c)
    return "".join(chars)
  else:
    return s

####################################################################
def escape_html (s):
  s = re.sub("&", "&amp;", s)
  s = re.sub("<", "&lt;", s)
  s = re.sub(">", "&gt;", s)
  s = remove_illegal_ascii(s)
  return s
