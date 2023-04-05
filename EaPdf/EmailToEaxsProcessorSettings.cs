using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf
{
    /// <summary>
    /// Basic mail message properties used for creating a CSV log file
    /// </summary>
    public class EmailToEaxsProcessorSettings
    {
        /// <summary>
        /// The name of the HashAlgorithm to use, must be one of the values in the System.Security.Cryptography.HashAlgorithmNames class.
        /// Default is SHA256
        /// </summary>
        public string HashAlgorithmName { get; set; } = EmailToEaxsProcessor.HASH_DEFAULT;

        /// <summary>
        /// If true, all attachments and binary content is saved external to the XML file; if false, attachments and binary content is saved inline.
        /// Not attached text content is always serialized directly in the XML.
        /// Default is true.
        /// </summary>
        public bool SaveAttachmentsAndBinaryContentExternally { get; set; } = true;

        /// <summary>
        /// If true, external content is wrapped inside of an XML file; if false, it is saved as the decoded original file.
        /// The default is false.
        /// </summary>
        public bool WrapExternalContentInXml { get; set; } = false;

        /// <summary>
        /// This only applies to binary attachments wrapped in XML, internally or externally.  If true, the original Content-Transfer-Encoding for binary 
        /// content (base64, quoted-printable, or uuencode) is used if possible (binary is always serialized as base64) to serialize the content in XML; 
        /// if false, all non-text content is serialized as base64 when saved inside the XML. For textual attachments see 
        /// <see cref="PreserveTextAttachmentTransferEncoding"/>.  The default is false; XSL FO processors require all attachments be base64 encoded.
        /// </summary>
        public bool PreserveBinaryAttachmentTransferEncodingIfPossible { get; set; } = false;

        /// <summary>
        /// This only applies to textual attachments wrapped in XML, internally or externally.  If true, textual content (7bit and 8bit) are always serialized as UTF-8 text 
        /// inside the XML; if false, all textual content is serialized as base64 when saved inside the XML. The default is false.
        /// For binary attachments see <see cref="PreserveBinaryAttachmentTransferEncodingIfPossible"/>.  The default is false; XSL FO processors require all attachments be base64 encoded.
        /// </summary>
        public bool PreserveTextAttachmentTransferEncoding { get; set; } = false;

        /// <summary>
        /// If true, any subfolders (if any) in the same directory as the mbox file and which match the name of the mbox file will also be processed, including all of its files and subfolders recursively
        /// </summary>
        public bool IncludeSubFolders { get; set; } = true;

        /// <summary>
        /// The folder to save external content to, always relative to the output folder
        /// </summary>
        public string ExternalContentFolder { get; set; } = "ExtBodyContent";

        /// <summary>
        /// If true, each mbox will have its own output file
        /// </summary>
        public bool OneFilePerMbox { get; set; } = false;

        /// <summary>
        /// Approximate maximum allowable size for the output XML files, in bytes.  If the size of the XML file exceeds this value, it will be split into multiple files.
        /// A value less than or equal to zero means no limit. The actual threshold for splitting will be with 5% of this value, <see cref="MaximumXmlFileSizeThreshold"/>
        /// </summary>
        public long MaximumXmlFileSize { get; set; } = 1024 * 1024 * 1024; // 1GB

        //TODO: This is just cruft, get rid of it, so what if files can be slightly larger than the MaximumXmlFileSize
        /// <summary>
        /// When this output file size threshold is reached, the file will be split into multiple files
        /// It is within 5% of the <see cref="MaximumXmlFileSize"/>
        /// </summary>
        public long MaximumXmlFileSizeThreshold
        {
            get
            {
                return (long)(MaximumXmlFileSize * 0.95);
            }
        }

        /// <summary>
        /// If true, all plain and html text will be converted to Xhtml when serialized into the XML
        /// This is to improve rendering when converting to PDF or other display formats
        /// </summary>
        public bool SaveTextAsXhtml { get; set; } = false;

        /// <summary>
        /// LogLevels equal to or above this threshold will also be written to the output XML file as comments
        /// </summary>
        public LogLevel LogToXmlThreshold { get; set; } = LogLevel.Information;

        /// <summary>
        /// If the source input file does not have a filename extension, this is the value that should be used.
        /// It should not include the leading period.
        /// </summary>
        public string DefaultFileExtension { get; set; } = "mbox";

        /// <summary>
        /// Skip processing of all messages until this MessageId is reached, then proceed as normal
        /// If SkipUntilMessageId and SkipAfterMessageId are both the same, then only that one message will be processed
        /// If SkipUntilMessageId and SkipAfterMessageId are both set, it is assumed that the SkipUntilMessageId occurs before the SkipAfterMessageId
        /// Mostly useful for debugging
        /// </summary>
        public string? SkipUntilMessageId { get; set; }

        /// <summary>
        /// Skip processing of all messages after this MessageId is reached
        /// If SkipUntilMessageId and SkipAfterMessageId are both the same, then only that one message will be processed
        /// If SkipUntilMessageId and SkipAfterMessageId are both set, it is assumed that the SkipUntilMessageId occurs before the SkipAfterMessageId
        /// Mostly useful for debugging
        /// </summary>
        public string? SkipAfterMessageId { get; set; }

        /// <summary>
        /// Extra non-standard HTML character entities to add to the list of entities that are converted to their Unicode equivalent
        /// </summary>
        public Dictionary<string, int>? ExtraHtmlCharacterEntities { get; set; } = new Dictionary<string, int>(){ { "QUOT", 0x22} };
    }
}
