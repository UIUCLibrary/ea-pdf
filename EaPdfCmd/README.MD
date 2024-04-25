# EaPdfCmd

Console Application for Creating Email Archives Conforming to the EA-PDF (PDF/mail) Specification

## Introduction

This application will take an email file as input and create an archival PDF file that conforms to the
EA-PDF (PDF/mail) specification as output. The [EA-PDF specification](https://pdfa.org/resource/ea-pdf/) (not yet published) describes standard
archival PDF files that are designed for long-term preservation of emails, including content, 
metadata, attachments, and source files. All EA-PDF files are also archival PDF/A files, either PDF/A-3 
(ISO 19005-3:2012, PDF 1.7) or PDF/A-4 (ISO 19005-4:2020, PDF 2.0).  

The EA-PDF specification is being developed by the PDF Association in partnership with the Library, 
University of Illinois at Urbana-Champaign and others. Development is being funded by the Institute of 
Museum and Library Services (IMLS), grant number [LG-250129-OLS-21](https://www.imls.gov/grants/awarded/lg-250129-ols-21).

## Installation

The latest version of the application can be downloaded from the [GitHub](https://github.com/orgs/UIUCLibrary/packages?repo_name=ea-pdf). The application is distributed 
as a zip file that contains the executable and all required library dependencies. The executable is 
[framework-dependent and cross-platform](https://learn.microsoft.com/en-us/dotnet/core/deploying/); however, it has currently only been tested on Windows 10. The file 
can be unzipped into a folder or directory of your choice. You may want to add this directory to your system's 
PATH environment variable to make it easier to run the application from the command line.

### External Dependencies

Besides the above, the application also requires the following dependencies to be installed:

- The application is written in C# and requires version 6 of the .NET Runtime to be installed. The 
  runtime can be downloaded from [Microsoft](https://dotnet.microsoft.com/download/dotnet/6.0). Be sure to install the runtime for your particular OS 
  and hardware platform; note that you **do not need** the SDK, ASP.NET, nor Desktop runtimes.

- The Saxon XSLT processor must be installed to support the XSLT transformations. The Saxon Home  
  Edition is free open source and can be downloaded from [Saxonica](https://www.saxonica.com/download/jav). The program has only been 
  tested with Saxon-HE 11.5, but other versions, including the licensed commercial editions, might 
  also work, but are not required.

- An XSL-FO processor is also required. The application currently supports Apache FOP and RenderX XEP.   
  - The FOP processor is free and can be downloaded from [Apache](https://xmlgraphics.apache.org/fop/download.html).  This is the recommended
    processor.  The program has been tested with FOP 2.8.  It does not currently work with FOP 2.9.

  - The XEP processor is a commercial product and can be downloaded from [RenderX](https://www.renderx.com/download/).  If you 
    want to use the XEP processor you will need to purchase a license from RenderX.  The 
    program has only been tested with free trial edition of XEP 4.30.

  - Only one of the above two FO processors is required, but you must configure the application to use the 
    correct processor.  The configuration settings are described below.

- Both Saxon and the FOP and XEP processors are Java applications, and therefore require a 
  Java runtime to be installed. During development, the `openjdk 11.0.16.1 2022-08-12 LTS` 
  runtime, was used.  Other runtimes might work, but have not been tested at this time.  
  Refer to the documentation for each of these applications for more information on supported
  runtimes and specific installation and configuration instructions.

The XSLT and FO processors may be installed in directories of your choice.  The assumed defaults for Windows 
are:

- Saxon: `C:\Program Files\SaxonHE11-5J\...`
- FOP: `C:\Program Files\Apache FOP\fop-2.8\...`
- XEP: `C:\Program Files\RenderX\XEP\...`

If you install these applications in different directories, you will need to update the configuration; see below for 
information on how to configure the application to use these dependencies.

## Configuration

The application will attempt to load configuration settings, either JSON or XML, from these files in 
this order. Later configuration settings will override earlier settings with same name.

- Appsettings.json or App.config in the application directory
- Appsettings.json or App.config in the current directory
- A JSON or XML file specified using the `-c, --config` command line parameter

If a directory contains both Appsettings.json and App.config, the Appsettings.json file will be used.
If none of these files are found or if specific settings are missing from the files, default values will be used.

The sample JSON configuration file included in the installation zip file is show below:

```appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },

  "XsltProcessors": {
    "Default": "Saxon", // "Saxon" is currently the only supported XSLT processor
    "Saxon": {
      "ClassPath": "C:\\Program Files\\SaxonHE11-5J\\saxon-he-11.5.jar"
    }
  },

  "FoProcessors": {
    "Default": "FOP", // "Fop" or "Xep"
    "Fop": {
      "JarFilePath": "C:\\Program Files\\Apache FOP\\fop-2.8\\fop\\build\\fop.jar",
      "ConfigFilePath": "XResources\\fop.xconf"
    },
    "Xep": {
      "ClassPath": "C:\\Program Files\\RenderX\\XEP\\lib\\xep.jar;C:\\Program Files\\RenderX\\XEP\\lib\\saxon6.5.5\\saxon.jar;C:\\Program Files\\RenderX\\XEP\\lib\\saxon6.5.5\\saxon-xml-apis.jar;C:\\Program Files\\RenderX\\XEP\\lib\\xt.jar",
      "ConfigFilePath": "XResources\\xep.xml" 
    }
  },

  "EmailToEaxsProcessorSettings": {
    "HashAlgorithmName": "SHA256",
    "SaveAttachmentsAndBinaryContentExternally": true,
    "WrapExternalContentInXml": true,  //this must be true if the FO processor is XEP (really only if there are PDF attachments)
    "PreserveBinaryAttachmentTransferEncodingIfPossible": false,
    "PreserveTextAttachmentTransferEncoding": false,
    "IncludeSubFolders": false,
    "ExternalContentFolder": "ExtBodyContent",
    "OneFilePerMessageFile": false,
    "MaximumXmlFileSize": 0, //no limit
    "SaveTextAsXhtml": true,
    "LogToXmlThreshold": "Information",
    "DefaultFileExtension": ".mbox",
    "SkipUntilMessageId": null,
    "SkipAfterMessageId": null,
    "ExtraHtmlCharacterEntities": {
      "QUOT": 134
    },
    "ForceParse": false
  },

  "EaxsToEaPdfProcessorSettings": {
    "XsltFoFilePath": "XResources\\eaxs_to_fo.xsl",
    "XsltXmpFilePath": "XResources\\eaxs_to_xmp.xsl",
    "XsltRootXmpFilePath": "XResources\\eaxs_to_root_xmp.xsl",

    /*
        Mapping of unicode language scripts to font families.  This is used to select the appropriate font family for a given script.  The font family names must exist in the FO processor's font configuration.
        The outer element name is an ISO 15924 4-letter codes for the script. A special entry with key 'default' should be in the dictionary with all three base font families specified. 
        This will be used as the default if a script entry is not found in the list.  Usually, this will be fonts with Latin or Western character sets, but could be fonts for a specific language.
        If a 'default' entry is not present, an entry for 'latn' (Latin) will be used as the default.  If neither 'default' nor 'latn' is found, the first alphabetical entry in the dictionary will be used 
        as the default which may produce unintended results.  Regardless of original order, the mappings are sorted alphabetically when loaded from the config file.
		
        The inner element name is the base font family, <Serif>, <SansSerif>, or <Monospace>; the first element is the default if no base font family is specified
        The value is a comma-separated list of font family names; these names must exist in the FO processor's font configuration
    */
    "LanguageFontMapping": {
      "Default": { //This will be the default of no other entry matches the script
        "Serif": "serif",
        "SansSerif": "sans-serif",
        "Monospace": "monospace"
      },
      "Hebr": { //Hebrew
        "Serif": "serif",
        "SansSerif": "sans-serif",
        "Monospace": "monospace"
      },
      "Arab": { //Arabic
        "Serif": "Traditional Arabic,serif",
        "SansSerif": "Simplified Arabic,sans-serif",
        "Monospace": "Simplified Arabic Fixed,monospace"
      },
      "Hira": { //Hiragana (Japanese)
        "Serif": "Kurinto Text JP",
        "SansSerif": "Kurinto Sans JP",
        "Monospace": "Kurinto Mono JP"
      },
      "Kana": { // Katakana (Japanese)
        "Serif": "Kurinto Text JP",
        "SansSerif": "Kurinto Sans JP",
        "Monospace": "Kurinto Mono JP"
      },
      "Hrkt": { //Hiragana or Katakana (Japanese)
        "Serif": "Kurinto Text JP",
        "SansSerif": "Kurinto Sans JP",
        "Monospace": "Kurinto Mono JP"
      },
      "Hang": { //Hangul (Korean)
        "Serif": "Kurinto Text KR",
        "SansSerif": "Kurinto Sans KR",
        "Monospace": "Kurinto Mono KR"
      },
      "Hani": { //Han (Chinese: Simplified - SC, Traditional - TC, Hong Kong - HK, and Rare - CJK)
        "Serif": "Kurinto Text SC,Kurinto Text TC,Kurinto Text HK,Kurinto Text CJK",
        "SansSerif": "Kurinto Sans SC,Kurinto Sans TC,Kurinto Sans HK,Kurinto Sans CJK",
        "Monospace": "Kurinto Mono SC,Kurinto Mono TC,Kurinto Mono HK,Kurinto Mono CJK"
      }
    }
  }
}
```

### Configuration Settings

#### Logging

This section follows the format of the [Microsoft.Extensions.Logging configuration settings](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line#configure-logging). The only setting 
that has been currently tested is the `Logging:LogLevel:Default`.  It supports the following values: Trace, Debug, 
Information, Warning, Error, Critical, None.  The default value is Information.  This setting can also be set 
using the `'-l, --log-level'` command line option.

#### XsltProcessors

This section contains the configuration settings for the XSLT processors.  The Saxon processor is the only 
supported XSLT processor at this time, so the `XsltProcessors:Default` setting must be set to 'Saxon'.  

The `Saxon:ClassPath` setting must be set to a semicolon-separated list of the **absolute** paths of all 
the required Saxon jar files which depends on where you installed the Saxon libraries.  For Saxon HE this is
just the one `saxon-he-*.jar` file.

#### FoProcessors

This section contains the configuration settings for the XSL-FO processors.  The only currently supported 
processors are 'FOP' and 'XEP', so the `FoProcessors:Default` setting must be set to either 'FOP' or 'XEP',
depending on which processors are installed and which you want to use.  The `FOProcessors:Default` setting can 
be overridden by the `-f, --fo-processor` command line option.`

##### FOP

The `FoProcessors:Fop:JarFilePath` setting must be set to the **absolute** path of the FOP jar file which depends
on where you installed the Apache FOP libraries.  

The `FoProcessors:Fop:ConfigFilePath` setting must be set to the path of the FOP configuration file.  If the path 
is relative, it is relative to the directory containing this configuration file. There is a default fop.xconf file 
included in the distribution that can be used as a starting point.

##### XEP

The `FoProcessors:Xep:ClassPath` setting must be set to a semicolon-separated list of the **absolute** paths of all 
the required the XEP jar files which depends on where you installed the RanderX XEP libraries. 

The `FoProcessors:Xep:ConfigFilePath` setting must be set to the path of the XEP configuration file.  If the path 
is relative, it is relative to the directory containing this configuration file.  There is a default xep.config file
included in the distribution that can be used as a starting point.

#### EmailToEaxsProcessorSettings

The conversion of email messages to PDFs consists of two steps: 1) converting emails to XML, and 2) converting 
XML to PDFs; this settings section controls the emails are converted to XML files.  These settings should not 
need to be changed unless your final product is just the XML files and not the PDFs.  Certain of these settings 
are required for the conversion to PDFs, so they should not be changed unless you are sure of what you are doing.

##### HashAlgorithmName

The XML schema requires that the source files, messages, attachments, etc. have a hashed checksum value.
This is the algorithm used to generate that checksum.  The default is SHA256. Supported values are SHA1, MD5,
SHA256, SHA384, SHA512.  There is no need to change this unless you have a specific requirement.

##### SaveAttachmentsAndBinaryContentExternally

This setting controls whether binary attachments are saved as separate files or embedded in the XML file as 
Base64.  The default is true.  If you want a single standalone XML file with all content and attachments, set this to 
false.  

Related settings are `WrapExternalContentInXml`, `PreserveBinaryAttachmentTransferEncodingIfPossible`, 
`PreserveTextAttachmentTransferEncoding`, and `ExternalContentFolder`.  

##### WrapExternalContentInXml
If true, external content is wrapped inside of an XML file; if false, it is saved as the decoded original file.  This must 
be true (default value) if the FO Processor is XEP (technically only if the attachment is another PDF).

Here is an example of the external XML file generated with `WrapExternalContentInXml` set to true:

```xmlw
<?xml version="1.0" encoding="utf-8"?>
<BodyContent xsi:schemaLocation="eaxs_schema_v2.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns="https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2">
  <Content><![CDATA[ ... ]]></Content>
  <TransferEncoding>base64</TransferEncoding>
  <Hash>
    <Value>929FD5495907ECC396D475297D62C3EC1C9CAD043D33596706A4001CFD1633F8</Value>
    <Function>SHA256</Function>
  </Hash>
  <Size>29184</Size>
</BodyContent>
```
The `<Content><![CDATA[ ... ]]></Content>` contains the Base64 encoded content of the attachment.

##### PreserveBinaryAttachmentTransferEncodingIfPossible

This only applies to binary attachments wrapped in XML, internally or externally.  If true, the original 
Content-Transfer-Encoding for binary content (base64, quoted-printable, or uuencode) is used if possible (binary is always serialized 
as base64) to serialize the content in XML; if false, all non-text content is serialized as base64 when saved inside the XML. The default 
is false; FO processors require all attachments be base64 encoded, so this default should not be changed.

##### PreserveTextAttachmentTransferEncoding

This only applies to textual attachments wrapped in XML, internally or externally.  If true, textual content (7bit and 8bit) are always 
serialized as UTF-8 text inside the XML; if false, all textual content is serialized as base64 when saved inside the XML. The default is 
false; XSL FO processors require all attachments be base64 encoded, so this default should not be changed.

##### IncludeSubFolders

For MBOX files, subfolders in the same directory as the MBOX file and which match the name of the MBOX file will also be processed, 
including all of its files and subfolders recursively. For a folder of EML files, all subfolders will also be processed recursively.

##### ExternalContentFolder

The folder to save external content to, always relative to the output folder.  The default is 'ExtBodyContent'.  This folder will be
created if it does not exist.

##### OneFilePerMessageFile

If true, each message file will have its own output XML file.  If false, all messages will be combined into a single output XML file.  The 
default is false.  This currently does not effect the PDF output, only the XML output, so it should not be changed unless you have a 
specific requirement.

##### MaximumXmlFileSize

Approximate maximum allowable size for the output XML files, in bytes.  If the size of the XML file exceeds this value, it will be split into 
multiple files. A value less than or equal to zero means no limit. Output is always split at message boundaries, so a single message will never 
be split across files.

##### SaveTextAsXhtml

If true, all plain and html text will be converted to XHTML when serialized into the XML.  This is to improve rendering when converting 
to PDF or other display formats.  The default is true.

##### LogToXmlThreshold

LogLevels equal to or above this threshold will also be written to the output XML file as comments in the XML.  The default is 
Information.  This should not be changed unless you have a specific requirement.  The valid values are Trace, Debug, Information,
Warning, Error, Critical, None.  Note that this is not effected by the log level setting in the Logging section or the `-l, --log-level`
command line option.

##### DefaultFileExtension

If the source input file does not have a filename extension, this is the value that should be used. It should include the leading period.
This is used to create the filename when the source file is included in the PDF as an attachment.  The default is '.mbox'.

##### SkipUntilMessageId, SkipAfterMessageId

Skip processing of all messages until this MessageId is reached, then proceed as normal, or skip processing of all messages after this 
MessageId is reached, respectively. If SkipUntilMessageId and SkipAfterMessageId are both the same, then only that one message 
will be processed. If SkipUntilMessageId and SkipAfterMessageId are both set, it is assumed that the SkipUntilMessageId occurs 
before the SkipAfterMessageId.  This is useful for debugging, but can also be used to create an archive of a subset of the messages in 
the email.

##### ExtraHtmlCharacterEntities

Extra non-standard HTML character entities to add to the list of entities that are converted to their Unicode equivalent.  The key is the
name of the entity, and the value is the Unicode code point.  Standard HTML entities are already handled by the code and XSLT processor, so 
this is only needed if you encounter non-standard entities.  The default is a single entity: `"QUOT": 134`.

##### ForceParse

Force the message parser to run even if the file does not appear to be a valid message file format. This might be useful for debugging.  The 
default is false.

#### EaxsToEaPdfProcessorSettings

This section contains the configuration settings for the conversion of XML files to PDFs.  These settings should not need to be changed 
unless you have a specific requirement.

##### XsltFoFilePath

This is the path to the root XSLT file that converts the XML files to XSL-FO files.  If the path is relative, it is relative to the directory 
containing this configuration file.

Note that the root XSLT file includes other XSLT files, namely:

- eaxs_xhtml2fo.xsl which converts XHTML to XSL-FO
- eaxs_helpers.xsl which contains common functions and templates
- eaxs_helpers_test.xsl which templates to test the helpers

These files should be in the same directory as the root XSLT file. 

In general this file should not be changed.  However, someone proficient in XSLT could modify this file to change the appearance of the
PDF output.  But caution should be used as the XSLT is complex and the output has specific features used during post-processing to ensure
the PDFs conform to the EA-PDF specification.

##### XsltXmpFilePath

This is the path to the XSLT file that converts the XML files to XMP metadata for individual folders and messages.   If the path is relative, 
it is relative to the directory containing this configuration file.  

The XML created by this XSLT contain the core email metadata fields required by the EA-PDF specification. During post-processing, these 
metadata are inserted into the PDF file.

##### XsltRootXmpFilePath

This is the path to the XSLT file that converts the XML files to document-level XMP metadata.  If the path is relative, it is relative to the
directory containing this configuration file.  

These metadata contain the top-level metadata by the EA-PDF specification, such as PDF/A and PDF/mail conformanace levels, PDF version, 
creator, dates, etc.  This XSLT also copies the custom XMP schema, EaPdfXmpSchema.xmp, into the output; this file must be included in the 
same directory as this XSLT. During post-processing, these metadata are inserted into the PDF file.

##### LanguageFontMapping

This section contains the mapping of unicode language scripts to font families.  This is used to select the appropriate font family for a
given script.  The font family names must exist in the FO processor's font configuration.  The outer element name is an ISO 15924 4-letter
codes for the script. A special entry with key 'default' should be in the dictionary with all three base font families specified. This will
be used as the default if a script entry is not found in the list.  Usually, this will be fonts with Latin or Western character sets, but
could be fonts for a specific language.  If a 'default' entry is not present, an entry for 'latn' (Latin) will be used as the default.  If
neither 'default' nor 'latn' is found, the first alphabetical entry in the dictionary will be used as the default which may produce unintended
results.  Regardless of original order, the mappings are sorted alphabetically when loaded from the config file.

The base font families are <Serif>, <SansSerif>, or <Monospace>; the first element is the default if no base font family is specified.  The
value is a comma-separated list of font family names; these names must exist in the FO processor's font configuration.

Below is an example of the default font mapping:

```json
    "LanguageFontMapping": {
      "Default": { //This will be the default of no other entry matches the script
        "Serif": "serif",
        "SansSerif": "sans-serif",
        "Monospace": "monospace"
      },
      "Arab": { //Arabic
        "Serif": "Traditional Arabic,serif",
        "SansSerif": "Simplified Arabic,sans-serif",
        "Monospace": "Simplified Arabic Fixed,monospace"
      },
      ...
      }
```
For the `Default` entry, the `Serif`, `SansSerif`, and `Monospace` typefaces are mapped to the `serif`, `sans-serif`, and `monospace` font 
families, respectively.

Similarly, `Arab` script characters that need a serif typeface are mapped to the `Traditional Arabic, serif` font families.  If an 
appropriate character can be found in the `Traditional Arabic` font, it will be used; if not, the `serif` font will be used.  Similary
for the `SansSerif` and `Monospace` typefaces.  

Note that in the above example, the `serif`, `san-serif`, `monospace`, `Traditional Arabic`, `Simplified Arabic`, and 
`Simplified Arabic Fixed` font families must all be declared in the FO processor's font configuration.  `fop.xconf` and `xep.xml` files
included in the distribution have examples of how to declare these font families.  You should refer to the documentation for the FO
processor you are using for more information on how to configure fonts.

Also included with the distributed files is a `Fonts` directory that contains various open source fonts that can be used with application.

## Usage

The application is a command line tool that takes the following parameters:

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

Basic command line errors are output to the console stderr stream.  More detailed error messages are written to the log which goes to the
console stdout stream by default.  The log level can be set using the `-l, --log-level` command line option.  For large email files, you may 
want to redirect the console output to a file for easier reading.  Use the `> file.txt` operator to redirect the output to a file.

The files created by the application are written to the output folder specified by the `-o, --out` option.  The output folder is created if it does
not exist.  If the output folder is not empty, the application will overwrite any existing files with the same name.  The output filenames are
based on the input filenames, but with a new extension appended.

For example if the input file was named `email.mbox`, the application will create these subfolders and files in the output folder:

- `email.mbox.pdf` This is the EA-PDF file that contains the email messages converted to PDF.  This file will conform to the EA-PDF 
  specification.  This is the primary archival PDF output file.  The remainder of the output folders and files are intermediate supporting files 
  for this file, and can be discarded if desired.

- `ExtBodyContent`  This folder contains all the external content files, such as attachments, that are saved as separate files.  These are 
  organized into subfolders based on the first two characters of the checksum of the content.  The files are named with the checksum and 
  the original file extension, or if the files are wrapped in XML, the `.xmlw` file extension. 

- `email.mbox.xml`  This is the file that contains the email messages converted to XML.  This file will conform to the EAXS XML Schema, 
  `eaxs_schema_v2.xsd`, which is included with the distribution.

- `email.mbox.csv` This is a comma-separated-value (CSV) file with a row containing brief metadata about each message that was contained in
  the input file.  

## Changelog

### 0.2.0-alpha (2024-04-19)
- Initial release
- The tool is based on early, non-public drafts of the EA-PDF specification, specifically 0.2 and 0.3.
- The tool is capable of creating an EA-PDF from a single email message in the EML format or a collection  
  of email messages in the MBOX format.
- This is alpha-level software, so expect bugs and missing features.
  - If you want to report bugs or make feature requests, please us the [GitHub Issue Tracker](https://github.com/UIUCLibrary/ea-pdf/issues).
  