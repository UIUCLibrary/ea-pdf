﻿# EaPdfCmd

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
    processor.  The program has been tested with FOP 2.8 and 2.9.  Version 2.9 is recommended.

  - The XEP processor is a commercial product and can be downloaded from [RenderX](https://www.renderx.com/download/).  If you 
    want to use the XEP processor you will need to purchase a license from RenderX.  The 
    program has only been tested with the free trial edition of XEP 4.30.  Note that the tagging features of XEP are not as 
    robust as FOP, so the output will not be fully compliant with the logical structure (tagged PDF) requirements of the EA-PDF specification.

  - Only one of the above two FO processors is required, but you must configure the application to use the 
    correct processor.  The configuration settings are described below.

  - Note that if you are only using one processor or the other, you should remove the configuration settings for the 
	unused processor from the configuration file.  Otherwise, the application will attempt to validate the processor 
    installation and fail if it is not installed.  Regardless, you still need to specify the "Default" processor to use
    in the configuration file.

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

## Usage

The application is intended to be run at the command line.  The distribution zip file contains a Windows executable `EaPdfCmd.exe` that
can be run from the command line.  On other platforms you can run the application using the `dotnet EaPdfCmd.dll` command.  

The app supports the following parameters:

### Command Line Options

```
>EaPdfCmd --help
EaPdfCmd 0.5.0-beta.1
Copyright 2024 University of Illinois Board of Trustees. All Rights Reserved.


  -i, --in                             Required. Input file or folder.

  -o, --out                            Required. Output folder.

  -g, --global-id                      Required. Globally unique, permanent,
                                       absolute URI identifying the email
                                       archive.

  -c, --config                         Configuration file.

  -e, --email                          Email address(es) associated with the
                                       archive, repeatable.

  -l, --log-level                      Default log level. Valid values: Trace,
                                       Debug, Information, Warning, Error,
                                       Critical, None

  -f, --fo-processor                   Which XSL-FO processor to use. Valid
                                       values: Fop, Xep

  -s, --include-sub-folders            Include sub-folders. Valid values: False,
                                       True

  -m, --allow-multiple-source-files    Allow multiple source files in a single
                                       PDF. Valid values: False, True

  --help                               Display this help screen.

  --version                            Display version information.

```

Basic command line errors are output to the console stderr stream.  More detailed error messages are written to the log which goes to the
console stdout stream by default.  The log level can be set using the `-l, --log-level` command line option.  For large email files, you may 
want to redirect the console output to a file for easier reading.  Use the `> file.txt` operator to redirect the output to a file.

The `-i, --in` option specifies the input file or folder.  If a folder is specified, the application will process all the files in the folder.
Note that a folder may contain a mixture of email message files (EML or MBOX) and other files.  The application will only process the email message files;
other files will be ignored. Also note that a folder may not contain a mixture of EML and MBOX files; the application will only process one type of file 
per folder.

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

- Optionally, `email.mbox.fo`  This is the XSL-FO file that is generated from the XML file.  This file is used by the FO processor to generate the PDF file.
  It is only saved if the `SaveFoFiles` configuration setting is set to true.  It can be useful for debugging purposes; some messages generated by FOP or XEP
  may reference line numbers in this file.

Note that the `-s, --include-sub-folders` option corresponds to the `IncludeSubFolders` configuration setting.  The `-m, --allow-multiple-source-files` option
corresponds to the `AllowMultipleSourceFilesPerOutputFile` configuration setting.  The `--fo-processor` option corresponds to the `FoProcessors:Default`
configuration setting.  Refer to the configuration settings below for more information on these settings.

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
    "Default": "FOP", // "Fop" or "Xep" If you are only using one of these, the unused section can be removed, but the "Default" must still be set to the remaining processor
    "Fop": {
      "ClassPath": "C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\build\\fop-2.9.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\build\\fop-core-2.9.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\build\\fop-events-2.9.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\build\\fop-util-2.9.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-anim-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-awt-util-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-bridge-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-codec-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-constants-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-css-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-dom-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-ext-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-extension-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-gvt-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-i18n-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-parser-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-script-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-shared-resources-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-svg-dom-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-svggen-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-transcoder-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-util-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-xml-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\commons-io-2.11.0.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\commons-logging-1.0.4.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\fontbox-2.0.27.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\xml-apis-1.4.01.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\xml-apis-ext-1.3.04.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\xmlgraphics-commons-2.9.jar",
      //"ClassPath": "C:\Program Files\Apache FOP\fop-2.8\fop\build\fop.jar;C:\Program Files\Apache FOP\fop-2.8\fop\lib\batik-all-1.16.jar;C:\Program Files\Apache FOP\fop-2.8\fop\lib\commons-io-2.11.0.jar;C:\Program Files\Apache FOP\fop-2.8\fop\lib\commons-logging-1.0.4.jar;C:\Program Files\Apache FOP\fop-2.8\fop\lib\fontbox-2.0.24.jar;C:\Program Files\Apache FOP\fop-2.8\fop\lib\serializer-2.7.2.jar;C:\Program Files\Apache FOP\fop-2.8\fop\lib\xml-apis-1.4.01.jar;C:\Program Files\Apache FOP\fop-2.8\fop\lib\xml-apis-ext-1.3.04.jar;C:\Program Files\Apache FOP\fop-2.8\fop\lib\xmlgraphics-commons-2.8.jar"
      "ConfigFilePath": "XResources\\fop.xconf"
    },
    "Xep": {
      "ClassPath": "C:\\Program Files\\RenderX\\XEP\\lib\\xep.jar;C:\\Program Files\\RenderX\\XEP\\lib\\saxon6.5.5\\saxon.jar;C:\\Program Files\\RenderX\\XEP\\lib\\saxon6.5.5\\saxon-xml-apis.jar;C:\\Program Files\\RenderX\\XEP\\lib\\xt.jar",
      "ConfigFilePath": "XResources\\xep.xml"
    }
  },

  "EmailToEaxsProcessorSettings": {
    "HashAlgorithmName": "MD5",
    "SaveAttachmentsAndBinaryContentExternally": true,
    "WrapExternalContentInXml": true,  //this must be true if the FO processor is XEP (really only if there are PDF attachments)
    "PreserveBinaryAttachmentTransferEncodingIfPossible": false,
    "PreserveTextAttachmentTransferEncoding": false,
    "IncludeSubFolders": false,
    "ExternalContentFolder": "ExtBodyContent",
    "AllowMultipleSourceFilesPerOutputFile": false,
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
    "XsltDpartFilePath": "XResources\\eaxs_to_dpart.xsl",
    "XsltRootXmpFilePath": "XResources\\eaxs_to_root_xmp.xsl",
    "SaveFoFiles": false,

    /*
        Mapping of unicode language scripts to font families.  This is used to select the appropriate font family for a given script.  The font family names must exist in the FO processor's font configuration.
        The outer element name is an ISO 15924 4-letter codes for the script. A special entry with key 'default' should be in the dictionary with all three base font families specified. 
        This will be used as the default if a script entry is not found in the list.  Usually, this will be fonts with Latin or Western character sets, but could be fonts for a specific language.
        If a 'default' entry is not present, an entry for 'latn' (Latin) will be used as the default.  If neither 'default' nor 'latn' is found, the first alphabetical entry in the dictionary will be used 
        as the default which may produce unintended results.  Regardless of original order, the mappings are sorted alphabetically when loaded from the config file.
		
        The inner element name is the base font family, <Serif>, <SansSerif>, or <Monospace>; the first element is the default if no base font family is specified
        The value is a comma-separated list of font family names; these names must exist in the FO processor's font configuration
    */
    "ScriptFontMapping": {
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

The `FoProcessors:Fop:ClassPath` setting must be set to a semicolon-separated list of the **absolute** paths of all 
the required FOP jar files which depends on where you installed the Apache FOP libraries.  

The `FoProcessors:Fop:ConfigFilePath` setting must be set to the path of the FOP configuration file.  If the path 
is relative, it is relative to the directory containing this configuration file. There is a default fop.xconf file 
included in the distribution that can be used as a starting point.

##### XEP

The `FoProcessors:Xep:ClassPath` setting must be set to a semicolon-separated list of the **absolute** paths of all 
the required XEP jar files which depends on where you installed the RenderX XEP libraries. 

The `FoProcessors:Xep:ConfigFilePath` setting must be set to the path of the XEP configuration file.  If the path 
is relative, it is relative to the directory containing this configuration file.  There is a default xep.config file
included in the distribution that can be used as a starting point.

#### EmailToEaxsProcessorSettings

The conversion of email messages to PDFs consists of two steps: 1) converting emails to XML and extracting any 
attachments, and 2) converting the XML and attachments to PDFs; this settings section controls how the emails are 
converted to XML files. These settings should not need to be changed unless your final product is just the XML 
files and not the PDFs. Certain of these settings are required for the conversion to PDFs, so they should not be 
changed unless you are sure of what you are doing.

##### HashAlgorithmName

The XML schema requires that the source files, messages, attachments, etc. have a hashed checksum value.
This is the algorithm used to generate that checksum.  The default is MD5. Supported values are SHA1, MD5,
SHA256, SHA384, SHA512.  For the EA-PDF specification, the required value is MD5; therefore, do not change this value 
unless you have specific requirements.

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
is false; FO processors require all attachments be base64 encoded, so this default should not be changed.  Note that the setting will
not effect how the content is embedded in the resulting PDF files; only how it is embedded in the XML file.

##### PreserveTextAttachmentTransferEncoding

This only applies to textual attachments (MIME Type 'test/*') wrapped in XML, internally or externally.  If true, textual content 
(7bit and 8bit) are always serialized as UTF-8 text inside the XML; if false, all textual content is serialized as base64 when 
saved inside the XML. The default is false; XSL FO processors require all attachments be base64 encoded, so this default should not 
be changed. Note that the setting will not effect how the content is embedded in the resulting PDF files; only how it is embedded in the XML file.

##### IncludeSubFolders

For MBOX files, subfolders in the same directory as the MBOX file and which match the name of the MBOX file, ignoring extensions, (this is how the Mozilla 
Thunderbird email client stores emails) will also be processed, including all of its files and subfolders recursively. This will have an
effect if the `-i, --in` option is a single MBOX file or a folder of MBOX files.

For a folder of EML files, all subfolders will also be processed recursively, looking for EML files.  For EML files, this parameter is only used if the
`-i, --in` option is a folder of EML files.  It is ignored if the input is a single EML file.

The `EmailToEaxsProcessorSettings:IncludeSubFolders` setting can be overridden by the `-s, --include-sub-folders` command line option.`

##### ExternalContentFolder

The folder to save external content to, always relative to the output folder.  The default is 'ExtBodyContent'.  This folder will be
created if it does not exist.

##### AllowMultipleSourceFilesPerOutputFile

If it is false, each input file will be processed separately, resulting in multiple output files, one for each source file.  This is the default.

If true and processing multiple input files, the single output XML file will contain all messages from all input files
and the resulting single PDF will contain multiple source files and all the messages from all the input files.  Note that this is not 
recommended by the specification which prefers one source file per PDF.  

The `EmailToEaxsProcessorSettings:AllowMultipleSourceFilesPerOutputFile` setting can be overridden by the `-m, --allow-multiple-source-files` 
command line option. 

##### MaximumXmlFileSize

Approximate maximum allowable size for the output XML files, in bytes.  If the size of the XML file exceeds this value, it will be split into 
multiple files. The default is 0, meaning no limit. Output is always split at message boundaries, so a single message will never 
be split across files.

This will result in multiple PDF files, one for each XML file.  Note that the resulting PDF files will not be fully compliant with the
EA-PDF specification. Therefore, this setting is not recommended at this time.

##### SaveTextAsXhtml

If true, all plain and html text will be converted to XHTML when serialized into the XML.  This is to improve rendering when converting 
to PDF or other display formats.  The default is true.

##### LogToXmlThreshold

Messages with LogLevels equal to or above this threshold will also be written to the output XML file as comments in the XML.  The default is 
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
this is only needed if you encounter non-standard entities.  The default is a single entity: `"QUOT": 134`.  Note that HTML entities are
case-sensitive, so for example, `quot` and `QUOT` are different entities.

##### ForceParse

Force the message parser to run even if the file does not appear to be a valid message file format. This might be useful for debugging.  The 
default is false.  Before parsing starts the system will snoop on the first few bytes of the file to determine if it is a valid message file
type.  If it is not, the file will be skipped.  If this setting is true, the file will be parsed regardless of the file type which can result
in lots of spurious errors and warnings in the log output

#### EaxsToEaPdfProcessorSettings

This section contains the configuration settings for the conversion of XML files to PDFs.  These settings should not need to be changed 
unless you have a specific requirement.

##### XsltFoFilePath

This is the path to the root XSLT file that converts the XML files to XSL-FO files.  If the path is relative, it is relative to the directory 
containing this configuration file.

Note that the root XSLT file includes other XSLT files, namely:

- eaxs_xhtml2fo.xsl which converts XHTML to XSL-FO
- eaxs_helpers.xsl which contains common functions and templates
    - eaxs_contentset_helpers.xsl which contains templates generate identifiers and links for content sets
- eaxs_helpers_test.xsl which templates to test the helpers

These files should be in the same directory as the root XSLT file. 

In general these files should not be changed.  However, someone proficient in XSLT could modify this file to change the appearance of the
PDF output.  But caution should be used as the XSLT is complex and the output has specific features used during post-processing to ensure
the PDFs conform to the EA-PDF specification.

##### XsltDpartFilePath

This is the path to the XSLT file that converts the XML files to DPart DPM metadata for individual folders, messages, and content sets.   
If the path is relative, it is relative to the directory containing this configuration file.  

The XML created by this XSLT represents the DPart hierarchy required by the EA-PDF specification. During post-processing, these 
metadata are inserted into the PDF file.

##### XsltRootXmpFilePath

This is the path to the XSLT file that converts the XML files to document-level XMP metadata.  If the path is relative, it is relative to the
directory containing this configuration file.  

These metadata contain the top-level metadata by the EA-PDF specification, such as PDF/A and PDF/mail conformance levels, PDF version, 
creator, dates, etc.  It also includes lists of source files, messages, and attachments included in the PDF. This XSLT also copies the 
custom XMP schema, EaPdfXmpSchema.xmp, into the output; this file must be included in the same directory as this XSLT. During post-processing, 
these metadata are inserted into the PDF file.

##### SaveFoFiles

If true, the XSL-FO files generated by the XSLT processor will be saved to the output folder.  This is useful for debugging purposes.  The
default is false.  The FOP and XEP FO processors can generate error messages that reference line numbers in the XSL-FO file.  If you are
having trouble with the PDF generation, you can look at the XSL-FO file to see if there are any obvious problems.  

##### ScriptFontMapping

This section contains the mapping of Unicode language scripts and primary typefaces (Serif, SansSerif, or Monospace) to font families.  This is used 
to select the appropriate font family for a given script and desired typeface.  The font family names must exist in the FO processor's font 
configuration.  The outer element name is an ISO 15924 4-letter codes for the script. A special entry with key `Default` should be in the 
dictionary with all three typefaces specified. This will be used as the default if a script entry is not found in the list.  Usually, this 
will be fonts with Latin or Western character sets, but could be fonts for a specific language.  If a `Default` entry is not present, an entry 
for `Latn` (Latin) will be used as the default.  If neither `Default` nor `Latn` is found, the first alphabetical entry in the dictionary will 
be used as the default which may produce unintended results.  Regardless of original order, the mappings are sorted alphabetically when 
loaded from the config file.

The supported primary typefaces are `Serif`, `SansSerif`, or `Monospace`. Some scripts may not support all typefaces.  If a given typeface is
desired but not present in the mapping, the first typeface in the list will be used instead. The typeface value is a comma-separated list 
of font family names to be used for the script and typeface; these names must exist in the FO processor's font configuration.

Below is an example of the default font mapping:

```json
    "ScriptFontMapping": {
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
families, respectively.  The `serif`, `sans-serif`, and `monospace` fonts must be defined in the FO processor's font configuration.

Similarly, `Arab` scripts that need a `Serif` typeface are mapped to the `Traditional Arabic, serif` font families.  If an 
appropriate character can be found in the `Traditional Arabic` font, it will be used; if not, the `serif` font will be used.  Similarly
for the `SansSerif` and `Monospace` typefaces.  

Note that in the above example, the `serif`, `san-serif`, `monospace`, `Traditional Arabic`, `Simplified Arabic`, and 
`Simplified Arabic Fixed` font families must all be declared in the FO processor's font configuration.  The `fop.xconf` and `xep.xml` files
included in the distribution have examples of how to declare these font families.  You should refer to the documentation for the FO
processor you are using for more information on how to configure fonts.

Also included with the distributed files is a `Fonts` directory that contains various open source fonts that can be used with the application.

### FOP Configuration

The FOP configuration file, `fop.xconf`, is included in the distribution.  This file contains the configuration settings for the FOP processor.
The default settings should work for most users.  However, a few settings require special attention.  Refer to the Apache FOP documentation
for more information on these settings.

#### Compression

The `filterList` setting is used to specify how PDF objects are compressed.  
````fop.xconf
	<renderers>
		<renderer mime="application/pdf">
			<filterList>
				<value>null</value>
			</filterList>
			<filterList type="image">
				<value>flate</value>
				<value>ascii-85</value>
			</filterList>
````

Currently, this is set to disable compression, except for images.  This is
to facilitate debugging of the resulting PDF files.  You can open the PDF file in a text editor and read the raw data and settings.  In a real production
environment, you would want to enable compression to reduce the size of the PDF files.  

````
			<filterList>
				<value>flate</value>
			</filterList>
````

Refer to the Apache FOP documentation for more information on
how to configure compression. [Apache FOP PDF Renderer](https://xmlgraphics.apache.org/fop/2.9/configuration.html#pdf-renderer)

#### ICC Color Profile

There is a color profile, `sRGB2014.icc`, included with the application.  This is the default color profile used by the FOP processor.  This is a
standard sRGB color profile.  Refer to the Apache FOP documentation for more information on how to configure color profiles. 
[Apache FOP PDF Renderer](https://xmlgraphics.apache.org/fop/2.9/configuration.html#pdf-renderer)

#### Fonts

Configuring fonts is too involved to cover here.  However, as mentioned in the [ScriptFontMapping section above](#scriptfontmapping), all the fonts
declared in the `ScriptFontMapping` section and used in the PDF must be declared in the FOP configuration file.

Refer to the included `fop.xconf` file and the Apache FOP documentation for more information on how to configure fonts.
[Apache FOP Fonts](https://xmlgraphics.apache.org/fop/2.9/fonts.html)]

### XEP Configuration

The XEP configuration file, `xep.xml`, is included in the distribution.  This file contains the configuration settings for the XEP processor.
Refer to the RenderX XEP documentation for more information on these settings. 
[Configuring XEP via the XEP Configuration File](https://www.renderx.com/reference.html#Configuring_via_file)

#### Compression

Similar to the FOP configuration, the `<option name="COMPRESS" value="false"/>` setting is used to specify how PDF objects are compressed.  
Currently, this is set to disable compression.  You can enable compression by setting the `value="true"`.  

#### ICC Color Profile

XEP uses the same color profile as FOP, `sRGB2014.icc`.  Unlike FOP this is set in the FO file using a processing instruction, such as
`<?xep-pdf-icc-profile url(file:/C:/Program Files/RenderX/XEP/sRGB2014.icc)?>`.

#### Fonts

Just as with FOP, all the fonts declared in the `ScriptFontMapping` section and used in the PDF must be declared in the XEP configuration file.
Refer to [Configuring Fonts](https://www.renderx.com/reference.html#Configuring_ConfigurationFile_Fonts) for more information on how to configure fonts in XEP.