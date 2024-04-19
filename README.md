# EaPdf

Application Library for Creating Email Archives Conforming to the EA-PDF (PDF/mail) Specification

## Introduction

This library contains classes and methods used to transform an email file into an archival PDF file that conforms to the
EA-PDF specification as output. The [EA-PDF specification](https://pdfa.org/resource/ea-pdf/) (not yet published) describes standard
archival PDF files that are designed for long-term preservation of emails, including content, 
metadata, attachments, and source files. All EA-PDF files are also archival PDF/A files, either PDF/A-3 
(ISO 19005-3:2012, PDF 1.7) or PDF/A-4 (ISO 19005-4:2020, PDF 2.0).  

The EA-PDF specification is being developed by the PDF Association in partnership with the Library of the 
University of Illinois at Urbana-Champaign and others. Development is being funded by the Institute of 
Museum and Library Services (IMLS), grant number [LG-250129-OLS-21](https://www.imls.gov/grants/awarded/lg-250129-ols-21).Email2Pdf

See the [EaPdfCmd console application](https://github.com/UIUCLibrary/ea-pdf/tree/master/EaPdfCmd) for an example of how to use this library to create EA-PDF files.