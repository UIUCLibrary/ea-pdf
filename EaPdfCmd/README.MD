# EaPdfCmd

Console Application for Creating Email Archives Conforming to the EA-PDF (PDF/mail) Specification

## Introduction

This application will take an email file as input and create an archival PDF file that conforms to the
EA-PDF specification as output. The [EA-PDF specification](https://pdfa.org/resource/ea-pdf/) (not yet published) describes standard
archival PDF files that are designed for long-term preservation of emails, including content, 
metadata, attachments, and source files. All EA-PDF files are also archival PDF/A files, either PDF/A-3 
(ISO 19005-3:2012, PDF 1.7) or PDF/A-4 (ISO 19005-4:2020, PDF 2.0).  

The EA-PDF specification is being developed by the PDF Association in partnership with the Library, 
University of Illinois at Urbana-Champaign and others. Development is being funded by the Institute of 
Museum and Library Services (IMLS), grant number [LG-250129-OLS-21](https://www.imls.gov/grants/awarded/lg-250129-ols-21).

## Installation

### Dependencies


## Configuration


## Usage
### Command Line Options

```
>EaPdfCmd --help
EaPdfCmd 0.2.0-alpha+7ac2a8e478842b624eef6165034c1a692271e756
Copyright 2024 University of Illinois Board of Trustees. All Rights Reserved.


  -i, --in              Required. Input file or folder.

  -o, --out             Required. Output folder.

  -g, --global-id       Required. Globally unique, permanent, absolute URI
                        identifying the email archive.

  -c, --config          Configuration file.

  -e, --email           Email address(es) associated with the archive,
                        repeatable.

  -l, --log-level       Default log level. Valid values: Trace, Debug,
                        Information, Warning, Error, Critical, None

  -f, --fo-processor    Which XSL-FO processor to use. Valid values: Fop, Xep

  --help                Display this help screen.

  --version             Display version information.

```

## Changelog

### 0.2.0-alpha (2024-04-19)
- Initial release
- The tool is based on early, non-public drafts of the EA-PDF specification, specifically 0.2 and 0.3.
- The tool is capable of creating an EA-PDF from a single email message in the EML format or a collection  
  of email messages in the MBOX format.
- This is alpha-level software, so expect bugs and missing features.
  - If you want to report bugs or make feature requests, please us the [GitHub Issue Tracker](https://github.com/UIUCLibrary/ea-pdf/issues).
  