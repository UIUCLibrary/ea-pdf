## Changelog

### 0.2.5-alpha (2024-04-19)
- Initial release
- The tool is based on early, non-public drafts of the EA-PDF specification, specifically 0.2 and 0.3.
- The tool is capable of creating an EA-PDF from a single email message in the EML format or a collection  
  of email messages in the MBOX format.
- This is [alpha-level](https://en.wikipedia.org/wiki/Software_release_life_cycle#Alpha) software, so expect bugs and missing features.
  - If you want to report bugs or make feature requests, please us the [GitHub Issue Tracker](https://github.com/UIUCLibrary/ea-pdf/issues).

### 0.2.6-alpha (2024-06-04)
- This release is still based on the 0.2 and 0.3 drafts of the EA-PDF specification.
- Miscellaneous quality improvements and bug fixes, such as:
  	- Better deal with XEP-generated error messages, such as a missing license file.
    - Update to the latest NuGet packages.
    - Improvements to testing.
- To better support legacy PDF Readers. synchonizing document-level metadata between the XMP and the Document Info Dictionary, such as Creator, Producer, Dates, etc.
- XMP metadata now more closely matches the requirements of the 0.2 spec; added a GUID to identify messages; removed the DACS metadata extension schema.
- Each Content Set as defined by the spec now starts on a new page.
- Changed the Front Matter to include the name and version of the three primary tools used to create the EA-PDF.
- Major changes to how the DPart and DPM metdata is created and attached to the PDF.
- Removed the dependency on the NDepend.Path library in order to better support Linux-style file paths.