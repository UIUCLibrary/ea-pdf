#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

message_groups = {}

######################################################################
def message_group_for_account (account_id):
  global message_groups
  if account_id not in message_groups.keys():
    message_groups[account_id] = set()
  return message_groups[account_id]
