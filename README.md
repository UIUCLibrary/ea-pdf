# Email2Pdf

Code for Creating Email Archives Conforming to the EA-PDF (PDF/mail) Specification

This solution contains projects used to transform email files (currently EML or MBOX) into archival PDF files 
that conform to the EA-PDF specification as output. The [EA-PDF specification](https://pdfa.org/resource/ea-pdf/) (not yet published) describes standard
archival PDF files that are designed for long-term preservation of emails, including content, metadata, 
attachments, and source files. All EA-PDF files are also archival PDF/A files, either PDF/A-3 (ISO 19005-3:2012, PDF 1.7) 
or PDF/A-4 (ISO 19005-4:2020, PDF 2.0).  

The EA-PDF specification is being developed by the PDF Association in partnership with the Library of the 
University of Illinois at Urbana-Champaign and others. Development is being funded by the Institute of 
Museum and Library Services (IMLS), grant number [LG-250129-OLS-21](https://www.imls.gov/grants/awarded/lg-250129-ols-21).

This solution consists of three projects:
- Class Library (EaPdf)
- Console Application (EaPdfCmd)
- Unit and Integrations Tests (TestEaPdf)

Refer to the README.md file in the EaPdfCmd project for more information on how to use the code.

Also note that the most of the sample emails used in the tests are not currently included in the code repository.  For
the time being you will need to substitute your own emails for testing purposes.  

## Changelog

### 0.2.5-alpha (2024-04-19)
- Initial release
- The tool is based on early, non-public drafts of the EA-PDF specification, specifically 0.2 and 0.3.
- The tool is capable of creating an EA-PDF from a single email message in the EML format or a collection  
  of email messages in the MBOX format.
- This is [alpha-level](https://en.wikipedia.org/wiki/Software_release_life_cycle#Alpha) software, so expect bugs and missing features.
  - If you want to report bugs or make feature requests, please us the [GitHub Issue Tracker](https://github.com/UIUCLibrary/ea-pdf/issues).
  