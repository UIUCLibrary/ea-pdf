# _Dev Tools_
Logging in Unit Tests:
https://codeburst.io/unit-testing-with-net-core-ilogger-t-e8c16c503a80

Getting a Linux version of the application
https://www.codeproject.com/Tips/5255423/Linux-on-Windows

## iTextSharp 4
 * Library:  https://github.com/VahidN/iTextSharp.LGPLv2.Core, https://www.nuget.org/packages/iTextSharp.LGPLv2.Core/
 * Docs: https://afterlogic.com/mailbee-net/docs-itextsharp/

# _Conversion Notes_
If the the RenderX XEP processor is used, attached files must be base64 encoded and if external, wrapped in XML.  XEP will fail when attaching a slightly malformed PDF file.
By wrapping them, we can trick the processor inot treating them as arbitrary binary attachments which it can handle just fine.

# Fonts
FOP: https://xmlgraphics.apache.org/fop/2.1/fonts.html

Embedding: https://stackoverflow.com/questions/19767205/embedding-font-into-apache-fop

# PDF/A

Good general explanation of the different types of PDF/A
https://apryse.com/blog/pdfa-format/what-are-the-different-types-of-pdfa

## Compliance
### FOP
  * I think FOP uses JavaScript in order to make embedded-file: links work.  This is not allowed in PDF/A.
  * The fonts/auto-detect feature of FOP does not work with PDF/A; the fonts are not embedded in a complaint way "cidset in subset font is incomplete".  The fonts must be specified explicitly.
  * FOP is not able to create the required PDF/A extension schema or properties, http://www.aiim.org/pdfa/ns/extension/, so we will need to addf them during a post-processing step.
  * XMP Metadata
    * dc:format is overridden
    * dc:language is overridden
    * a dc:date is added for the current date time.  Values are not validated.
    * pdf:Producer is overridden with the FOP string
    * pdf:PDFVersion is overridden with the value from the config file
    * xmp:MetadataDate is overridden with the current date time. It is not even validated as a valid date if present.
    * xmp:CreateDate is added if it is not specified. It also appears in the document properties. If omitted from the XMP, it defaults to the current date time.
    * xmp:ModifyDate appears in the document properties if specified. If not specified in the XMP, it is left blank in the document properties and the XMP.
    * Custom metatdata (pdf:info/pdf:name) is added to the custom properties, and also to the XMP namespace pdfx

### XEP
  * XEP-generated files do conform to the specified PDF/A profile.  
  * XEP does not support PDF/A-3u, but does support PDF/A-3b. Can be overridden in the XMP file.
  * The highest PDF version supported by XEP while still allowing PDF/A-3b is 1.4.  Can be overridden in the XMP file.
  * XMP Metadata
    * dc:creator is added with value of 'Unknown'
    * dc:title is added with value of 'x-default: Untitled'
    * dc:date values are not validated.
    * pdf:Trapped is added with a value of 'Unknown' -- maybe because of the RenderX logo added to the bottom of the page
    * xmp:MetadataDate is _not_ validated as a valid date, can contain any arbitrary string.
    * xmp:CreateDate is always omitted in the XMP output, but it does appear in the document proporties as entered in the XMP.  It is validated as an ISO 8601 date format (yyyy-mm-ddThh:mm:ssZ), but only Z is accepted as the timezone. 
    Defaults to the current date time if not specified in the XMP.
    * xmp:ModifyDate is always omitted in the XMP output, but it does appear in the document proporties as entered in the XMP.  It is validated as an ISO 8601 date format (yyyy-mm-ddThh:mm:ssZ), but only Z is accepted as the timezone.
    Defaults to the current date time if not specified in the XMP.

### Adobe Reader
  * For PDF/A-3b files, the attachment links (paperclip icons) don't work to download attachments, but the list of attachments does work to download files


