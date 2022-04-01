#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

## DO NOT USE BEGIN/END TRANSACTION IN A PYTHON CONNECTION
## INSTEAD, USE cnx.commit()

create_table = [

"""CREATE TABLE account (
id INTEGER PRIMARY KEY AUTOINCREMENT,
account_name TEXT NOT NULL,
account_directory TEXT DEFAULT NULL
);""",

"""CREATE TABLE address (
id INTEGER PRIMARY KEY AUTOINCREMENT,
address TEXT NOT NULL
);""",

"""CREATE TABLE address_name (
address_id INTEGER NOT NULL,
name_id INTEGER NOT NULL,
CONSTRAINT address_name_ibfk_1 FOREIGN KEY (address_id) REFERENCES address (id),
CONSTRAINT address_name_ibfk_2 FOREIGN KEY (name_id) REFERENCES name (id)
);""",

"""CREATE TABLE external_content (
id INTEGER PRIMARY KEY AUTOINCREMENT,
folder_id INTEGER NOT NULL,
xml_wrapped BOOLEAN DEFAULT \'0\',
original_file_name TEXT DEFAULT NULL,
stored_file_name TEXT NOT NULL,
content_length bigint DEFAULT \'0\',
content_sha1 TEXT DEFAULT NULL,
CONSTRAINT external_content_ibfk_1 FOREIGN KEY (folder_id) REFERENCES folder (id)
);""",

"""CREATE TABLE folder (
id INTEGER PRIMARY KEY AUTOINCREMENT,
account_id INTEGER NOT NULL,
folder_name TEXT NOT NULL,
n_messages INTEGER DEFAULT 0,
date_from DATETIME DEFAULT NULL,
date_to DATETIME DEFAULT NULL,
CONSTRAINT folder_ibfk_1 FOREIGN KEY (account_id) REFERENCES account (id)
);""",

"""CREATE TABLE internal_content (
id INTEGER PRIMARY KEY AUTOINCREMENT,
folder_id INTEGER NOT NULL,
original_file_name TEXT DEFAULT NULL,
content_text BLOB NOT NULL,
content_length bigint DEFAULT \'0\',
content_sha1 TEXT DEFAULT NULL,
CONSTRAINT internal_content_ibfk_1 FOREIGN KEY (folder_id) REFERENCES folder (id)
);""",

"""CREATE TABLE message (
id INTEGER PRIMARY KEY AUTOINCREMENT,
folder_id INTEGER NOT NULL,
global_message_id TEXT NOT NULL,
eol TEXT DEFAULT NULL,
sha1_hash TEXT NOT NULL,
date_time datetime NOT NULL,
zoffset TEXT NOT NULL,
CONSTRAINT message_ibfk_1 FOREIGN KEY (folder_id) REFERENCES folder (id)
);""",

"""CREATE TABLE message_address (
message_id INTEGER NOT NULL,
tag_id INTEGER NOT NULL,
address_id INTEGER DEFAULT NULL,
name_id INTEGER DEFAULT NULL,
CONSTRAINT message_address_ibfk_1 FOREIGN KEY (message_id) REFERENCES message (id),
CONSTRAINT message_address_ibfk_2 FOREIGN KEY (tag_id) REFERENCES tag (id),
CONSTRAINT message_address_ibfk_3 FOREIGN KEY (address_id) REFERENCES address (id),
CONSTRAINT message_address_ibfk_4 FOREIGN KEY (name_id) REFERENCES name (id)
);""",

"""CREATE TABLE message_header (
id INTEGER PRIMARY KEY AUTOINCREMENT,
message_id INTEGER NOT NULL,
tag_id INTEGER NOT NULL,
header_value BLOB NOT NULL,
CONSTRAINT message_header_ibfk_1 FOREIGN KEY (message_id) REFERENCES message (id),
CONSTRAINT message_header_ibfk_2 FOREIGN KEY (tag_id) REFERENCES tag (id)
);""",

"""CREATE TABLE name (
id INTEGER PRIMARY KEY AUTOINCREMENT,
name TEXT NOT NULL
);""",

"""CREATE TABLE part (
id INTEGER PRIMARY KEY AUTOINCREMENT,
message_id INTEGER NOT NULL,
sequence_id INTEGER NOT NULL,
parent_sequence_id INTEGER NOT NULL,
is_multipart BOOLEAN NOT NULL,
content_length INTEGER DEFAULT \'0\',
content_sha1 TEXT DEFAULT NULL,
content_ident TEXT DEFAULT NULL,
is_attachment BOOLEAN DEFAULT \'0\',
internal_content_id INTEGER DEFAULT NULL,
external_content_id INTEGER DEFAULT NULL,
CONSTRAINT part_ibfk_1 FOREIGN KEY (message_id) REFERENCES message (id),
CONSTRAINT part_ibfk_2 FOREIGN KEY (internal_content_id) REFERENCES internal_content (id),
CONSTRAINT part_ibfk_3 FOREIGN KEY (external_content_id) REFERENCES external_content (id)
);""",

"""CREATE TABLE part_header (
id INTEGER PRIMARY KEY AUTOINCREMENT,
part_id INTEGER NOT NULL,
tag_id INTEGER NOT NULL,
header_value BLOB NOT NULL,
CONSTRAINT part_header_ibfk_1 FOREIGN KEY (part_id) REFERENCES part (id),
CONSTRAINT part_header_ibfk_2 FOREIGN KEY (tag_id) REFERENCES tag (id)
);""",

"""CREATE TABLE replyto (
replying_id INTEGER NOT NULL,
repliedto_id TEXT NOT NULL,
CONSTRAINT replyto_ibfk_1 FOREIGN KEY (replying_id) REFERENCES message (id)
);""",

"""CREATE TABLE tag (
id INTEGER  PRIMARY KEY AUTOINCREMENT,
original_name TEXT NOT NULL,
xml_name TEXT NOT NULL
);"""

]

create_index = [

'CREATE INDEX IF NOT EXISTS address_idx_0 ON address (address);',
'CREATE INDEX IF NOT EXISTS address_name_idx_1 ON address_name (address_id);',
'CREATE INDEX IF NOT EXISTS address_name_idx_2 ON address_name (name_id);',
'CREATE INDEX IF NOT EXISTS external_content_idx_3 ON external_content (folder_id);',
'CREATE INDEX IF NOT EXISTS external_content_idx_4 ON external_content (content_length);',
'CREATE INDEX IF NOT EXISTS external_content_idx_5 ON external_content (content_sha1);',
'CREATE INDEX IF NOT EXISTS folder_idx_6 ON folder (account_id);',
'CREATE INDEX IF NOT EXISTS internal_content_idx_7 ON internal_content (folder_id);',
'CREATE INDEX IF NOT EXISTS internal_content_idx_8 ON internal_content (content_length);',
'CREATE INDEX IF NOT EXISTS internal_content_idx_9 ON internal_content (content_sha1);',
'CREATE INDEX IF NOT EXISTS message_idx_10 ON message (folder_id);',
'CREATE INDEX IF NOT EXISTS message_idx_11 ON message (global_message_id);',
'CREATE INDEX IF NOT EXISTS message_address_idx_12 ON message_address (message_id);',
'CREATE INDEX IF NOT EXISTS message_address_idx_13 ON message_address (tag_id);',
'CREATE INDEX IF NOT EXISTS message_address_idx_14 ON message_address (address_id);',
'CREATE INDEX IF NOT EXISTS message_address_idx_15 ON message_address (name_id);',
'CREATE INDEX IF NOT EXISTS message_header_idx_16 ON message_header (message_id);',
'CREATE INDEX IF NOT EXISTS message_header_idx_17 ON message_header (tag_id);',
'CREATE INDEX IF NOT EXISTS name_idx_18 ON name (name);',
'CREATE INDEX IF NOT EXISTS part_idx_19 ON part (content_sha1);',
'CREATE INDEX IF NOT EXISTS part_idx_20 ON part (message_id);',
'CREATE INDEX IF NOT EXISTS part_idx_21 ON part (internal_content_id);',
'CREATE INDEX IF NOT EXISTS part_idx_22 ON part (external_content_id);',
'CREATE INDEX IF NOT EXISTS part_header_idx_23 ON part_header (part_id);',
'CREATE INDEX IF NOT EXISTS part_header_idx_24 ON part_header (tag_id);',
'CREATE INDEX IF NOT EXISTS replyto_idx_25 ON replyto (repliedto_id);',
'CREATE INDEX IF NOT EXISTS replyto_idx_26 ON replyto (replying_id);',
'CREATE INDEX IF NOT EXISTS message_idx_27 ON message (date_time);'
]


drop_index = [

# 'DROP INDEX IF EXISTS address_idx_0;',
'DROP INDEX IF EXISTS address_name_idx_1;',
'DROP INDEX IF EXISTS address_name_idx_2;',
'DROP INDEX IF EXISTS external_content_idx_3;',
'DROP INDEX IF EXISTS external_content_idx_4;',
'DROP INDEX IF EXISTS external_content_idx_5;',
'DROP INDEX IF EXISTS folder_idx_6;',
'DROP INDEX IF EXISTS internal_content_idx_7;',
'DROP INDEX IF EXISTS internal_content_idx_8;',
'DROP INDEX IF EXISTS internal_content_idx_9;',
'DROP INDEX IF EXISTS message_idx_10;',
# keep the index on global_message_id; needed for checking duplicates
# 'DROP INDEX IF EXISTS message_idx_11;',
'DROP INDEX IF EXISTS message_address_idx_12;',
'DROP INDEX IF EXISTS message_address_idx_13;',
'DROP INDEX IF EXISTS message_address_idx_14;',
'DROP INDEX IF EXISTS message_address_idx_15;',
'DROP INDEX IF EXISTS message_header_idx_16;',
'DROP INDEX IF EXISTS message_header_idx_17;',
# 'DROP INDEX IF EXISTS name_idx_18;',
'DROP INDEX IF EXISTS part_idx_19;',
'DROP INDEX IF EXISTS part_idx_20;',
'DROP INDEX IF EXISTS part_idx_21;',
'DROP INDEX IF EXISTS part_idx_22;',
'DROP INDEX IF EXISTS part_header_idx_23;',
'DROP INDEX IF EXISTS part_header_idx_24;',
'DROP INDEX IF EXISTS replyto_idx_25;',
'DROP INDEX IF EXISTS replyto_idx_26;',

]

load_tags = [
"insert into tag(original_name,xml_name) values ('','Account');",
"insert into tag(original_name,xml_name) values ('','BodyContent');",
"insert into tag(original_name,xml_name) values ('boundary','BoundaryString');",
"insert into tag(original_name,xml_name) values ('Cc','Cc');",
"insert into tag(original_name,xml_name) values ('charset','Charset');",
"insert into tag(original_name,xml_name) values ('','Comments');",
"insert into tag(original_name,xml_name) values ('','Content');",
"insert into tag(original_name,xml_name) values ('Content-ID','ContentId');",
"insert into tag(original_name,xml_name) values ('Content-Type','ContentType');",
"insert into tag(original_name,xml_name) values ('Content-Disposition','Disposition');",
"insert into tag(original_name,xml_name) values ('filename','DispositionFileName');",
"insert into tag(original_name,xml_name) values ('','Eol');",
"insert into tag(original_name,xml_name) values ('','ExtBodyContent');",
"insert into tag(original_name,xml_name) values ('','Folder');",
"insert into tag(original_name,xml_name) values ('From','From');",
"insert into tag(original_name,xml_name) values ('','Function');",
"insert into tag(original_name,xml_name) values ('','GlobalId');",
"insert into tag(original_name,xml_name) values ('','Hash');",
"insert into tag(original_name,xml_name) values ('','Header');",
"insert into tag(original_name,xml_name) values ('In-Reply-To','InReplyTo');",
"insert into tag(original_name,xml_name) values ('','LocalId');",
"insert into tag(original_name,xml_name) values ('','Mbox');",
"insert into tag(original_name,xml_name) values ('','Message');",
"insert into tag(original_name,xml_name) values ('Message-ID','MessageId');",
"insert into tag(original_name,xml_name) values ('MIME-Version','MimeVersion');",
"insert into tag(original_name,xml_name) values ('','MultiBody');",
"insert into tag(original_name,xml_name) values ('','Name');",
"insert into tag(original_name,xml_name) values ('Date','OrigDate');",
"insert into tag(original_name,xml_name) values ('','OtherMimeHeader');",
"insert into tag(original_name,xml_name) values ('','Preamble');",
"insert into tag(original_name,xml_name) values ('References','References');",
"insert into tag(original_name,xml_name) values ('','RelPath');",
"insert into tag(original_name,xml_name) values ('Sender','Sender');",
"insert into tag(original_name,xml_name) values ('','SingleBody');",
"insert into tag(original_name,xml_name) values ('Subject','Subject');",
"insert into tag(original_name,xml_name) values ('To','To');",
"insert into tag(original_name,xml_name) values ('Content-Transfer-Encoding','TransferEncoding');",
"insert into tag(original_name,xml_name) values ('','Value');",
"insert into tag(original_name,xml_name) values ('','XMLWrapped');",
"insert into tag(original_name,xml_name) values ('Bcc','Bcc');",
"insert into tag(original_name,xml_name) values ('','Keywords');",
"insert into tag(original_name,xml_name) values ('','ContentTypeComments');",
"insert into tag(original_name,xml_name) values ('','Description');",
"insert into tag(original_name,xml_name) values ('','DescriptionComments');",
"insert into tag(original_name,xml_name) values ('','DispositionComments');"

]

######################################################################
def run_query (cnx, query):
  cursor = cnx.cursor()
  cursor.execute(query)
  cursor.close()

######################################################################
def run_create_table (cnx):
  for query in create_table:
    run_query(cnx, query)
  cnx.commit()

######################################################################
def run_create_index (cnx):
  for query in create_index:
    run_query(cnx, query)
  cnx.commit()

######################################################################
def run_drop_index (cnx):
  for query in drop_index:
    run_query(cnx, query)
  cnx.commit()

######################################################################
def run_load_tags (cnx):
  for query in load_tags:
    run_query(cnx, query)
  cnx.commit()
