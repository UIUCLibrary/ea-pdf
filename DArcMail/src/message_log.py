#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

import csv

######################################################################
class MessageLogRow ():
  ####################################################################
  def __init__ (self):
    self.sender       = ''
    self.to           = ''
    self.date         = ''
    self.subject      = ''
    self.messageid    = ''
    self.hash         = ''
    self.errors       = 0
    self.firstmessage = ''
  ####################################################################
  def add_from (self, sender):
    self.sender = sender
  ####################################################################
  def add_to (self, to):
    self.to = to
  ####################################################################
  def add_subject (self, subject):
    self.subject = subject
  ####################################################################
  def add_date (self, date):
    self.date = date
  ####################################################################
  def add_messageid (self, messageid):
    self.messageid = messageid
  ####################################################################
  def add_hash (self, hash):
    self.hash = hash
  ####################################################################
  def add_errors (self, errors):
    self.errors = errors
  ####################################################################
  def add_firstmessage (self, firstmessage):
    self.firstmessage = firstmessage

######################################################################
class MessageLog ():

  ####################################################################
  def __init__ (self, mlf):
    self.mlcsv = csv.writer(mlf, delimiter=',',
      quotechar='"', quoting=csv.QUOTE_MINIMAL)
    self.mlcsv.writerow(['From', 'To', 'Date', 'Subject', 'MessageID', 'Hash',
        'Errors', 'First Error Message'])

  ####################################################################
  def writerow (self, mlrow):
    self.mlcsv.writerow([
      mlrow.sender,
      mlrow.to,
      mlrow.date,
      mlrow.subject,
      mlrow.messageid,
      mlrow.hash,
      mlrow.errors,
      mlrow.firstmessage
    ])
