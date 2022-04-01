#!/usr/bin/env python3
######################################################################
## Author: Carl Schaefer, Smithsonian Institution Archives 
######################################################################

DEFAULT_DATABASE = ""

## for partial path to account directories (load_panel.py)
DEFAULT_ACCOUNT_DIRECTORY_PREFIX = ""

## for partial path to export directories (export_panel.py)
DEFAULT_EXPORT_DIRECTORY_PREFIX = ""

## permissions for creating external content
## 0775 owner&group: read,write,execute/navigate; world:read,execute/navigate
## 0664 owner&group: read,write; world: read 
EXTERNAL_CONTENT_DIRECTORY_PERMISSIONS = 0o775
EXTERNAL_CONTENT_FILE_PERMISSIONS      = 0o664

## permissions for XML export files created in the account_directory (dm2xml.py)
XML_FILE_PERMISSIONS = 0o664

VERSION="v2.0"

DEFAULT_HIGHLITE_COLOR = "#FF00FF"  # magenta
#DEFAULT_HIGHLITE_COLOR = "#88FF00"  # lime green
#DEFAULT_HIGHLITE_COLOR = "#FF6666"  # light red

