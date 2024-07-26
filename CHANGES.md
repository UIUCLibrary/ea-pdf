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
- To better support legacy PDF Readers. synchronizing document-level metadata between the XMP and the Document Info Dictionary, such as Creator, Producer, Dates, etc.
- XMP metadata now more closely matches the requirements of the 0.2 spec; added a GUID to identify messages; removed the DACS metadata extension schema.
- Each Content Set as defined by the spec now starts on a new page.
- Changed the Front Matter to include the name and version of the three primary tools used to create the EA-PDF.
- Major changes to how the DPart and DPM metadata is created and attached to the PDF.
- Removed the dependency on the NDepend.Path library in order to better support Linux-style file paths.

### 0.5.0-beta.1 (2024-07-26)
- This release is based on the non-public 0.5 draft of the EA-PDF specification.
- Multiple changes to more correctly support the 0.5 draft of the EA-PDF specification:
  - MD5 checksums are now used instead of SHA-256.  These are now included in the metadata for source and attachment files and in the `Params` dictionary 
    for EmbeddedFile streams.
  - Renamed `pdfmailid:part` to `pdfmailid:version` and updated the `pdfmailid` values to match the specification.
  - Added `Keywords: EA-PDF` to the `Info` dictionary and the XMP metadata.
  - Removed all references to the speculative DACS metadata extension schema.
  - Updates to the XMP metadata to match the 0.5 draft of the EA-PDF specification.
  - When the PDF contains a single email message, it now uses the 's' conformance level instead of the 'm' conformance level.
  - There is now a bookmark for each "content set" in the document.  The bookmarks start out opened to the message level.
  - Setting `PageMode` and `ViewerPreferences` based on conformance level and the presence of attachments or not.
  - Renamed the DPM metadata fields to add the `Mail_` prefix, matching the V0.5 draft specification.
  - Fixing the logical structure or tagging so it conforms to the V0.5 draft specification. This will only effect the FOP rendering; 
    XEP does not allow tagging to be customized to support the requirements of the spec, so XEP uses its own default tagging.
  - Multiple changes to support embedded source and attachment files, sections 7.2, 7.5, and 9.4 of the V0.5 draft specification.
- Multiple miscellaneous code improvements, refactoring, and bug fixes:
  - Moved the conformance level determination for 's' or 'm' inside the XSLT; also added a test for valid conformance values.
  - Replaced some `const` values with `enum` values.
  - Added some configuration file validation checks to ensure that the configuration has the appropriate sections and values for the designated FO processor.
  - Renamed `LanguageFontMapping` to `ScriptFontMapping`; this more closely aligns to the actual function; languages and scripts are different things.
  - Fixed a problem with the DPart DPM for delivery status messages.
  - Renamed `eaxs_to_xmp.xsl` to `eaxs_to_dpart.xsl` to better reflect what it actually does.  The DPM metadata does not contain XMP metadata.
  - Updated to the latest NuGet packages.
  - Fixed a formatting problem where the front matter and attachments list sections were repeated when there were multiple top-level folders.
  - Fixed some problems when invalid HTML markup is encountered in email message bodies.
  - Fixed some problems with the FO file formatting that were causing spurious warnings depending on the FOP or XEP processor.
  - Improved how complex scripts are handled in the PDF, and added some missing `xml:lang` attributes.
  - Added a configuration setting `SaveFoFiles` which defaults to false.  If set to true, the FO files are saved to the output directory which can be useful for debugging.
- The command line interface can now process a directory of EML or MBOX files, instead of just a single file.  Just set the `--in` parameter to the directory path
  instead of a file path.
  - This includes two new parameters:
    - `-s, --include-sub-folders`  This instructs the processor to include all sub-folders of the specified folder.
    - `-m, --allow-multiple-source-files` This instructs the processor bundle multiple source files in a single EA-PDF output file.
      Note that this is not recommended by the specification which prefers one source file per PDF.  The default behavior is to create a separate PDF for 
      each source file.
- Updated the test code to use the latest version of the VeraPDF validation tool.
- Updated the FOP transformer code so that it supports the `ClassPath` and `MainClass` methods for running Java files.  The `JarFilePath` settings has been removed.
  This allowed the upgrade to latest FOP 2.9 version. One shortcoming of FOP 2.9 is that it does not correctly report its own version number, so some accommodations 
  were made for this.
