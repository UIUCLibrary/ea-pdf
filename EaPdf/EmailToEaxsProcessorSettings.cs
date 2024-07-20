using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Xml;

namespace UIUCLibrary.EaPdf
{
    /// <summary>
    /// Basic mail message properties used for creating a CSV log file
    /// </summary>
    public class EmailToEaxsProcessorSettings
    {
        public const string MBOX_FILE_EXTENSION = ".mbox";
        public const string EML_FILE_EXTENSION = ".eml";

        public EmailToEaxsProcessorSettings(IConfiguration config)
        {

            //the ExtraHtmlCharacterEntities will be replaced by any ExtraHtmlCharacterEntities in the configuration file
            if (config.AsEnumerable().Any(s => s.Key.StartsWith("EmailToEaxsProcessorSettings:ExtraHtmlCharacterEntities:")))
            {
                ExtraHtmlCharacterEntities.Clear();
            }

            config.Bind("EmailToEaxsProcessorSettings", this);

            ValidateSettings();

        }


        public EmailToEaxsProcessorSettings()
        {
            ValidateSettings();
        }

        private void ValidateSettings()
        {
            //UNDONE:  Add some validation here
        }

        /// <summary>
        /// The name of the HashAlgorithm to use, must be one of the values in the System.Security.Cryptography.HashAlgorithmNames class.
        /// Default is MD5
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
        /// The default is true.  This must be true if the FO Processor is XEP (technically only if the PDF has other PDFs as attachments).
        /// </summary>
        public bool WrapExternalContentInXml { get; set; } = true;

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
        /// For mbox files, subfolders in the same directory as the mbox file and which match the name of the mbox file will also be processed, including all of its files and subfolders recursively
        /// For a folder of eml files, all subfolders will also be processed recursively
        /// </summary>
        public bool IncludeSubFolders { get; set; } = false;

        /// <summary>
        /// The folder to save external content to, always relative to the output folder
        /// </summary>
        public string ExternalContentFolder { get; set; } = "ExtBodyContent";

        /// <summary>
        /// If true, each message file will have its own output file
        /// If the message file contains multiple messages, the output will also contain multiple messages
        /// </summary>
        public bool OneFilePerMessageFile { get; set; } = false;

        //TODO:  Add a OneFilePerMessage option, or maybe use an enum: OneFile, OneFilePerMessage, OneFilePerMessageFile
        //       See https://github.com/UIUCLibrary/ea-pdf/issues/6

        /// <summary>
        /// Approximate maximum allowable size for the output XML files, in bytes.  If the size of the XML file exceeds this value, it will be split into multiple files.
        /// A value less than or equal to zero means no limit. The actual file size for splitting will be with 5% of this value.  Output is always split at message boundaries.
        /// </summary>
        public long MaximumXmlFileSize { get; set; } = 0; //no limit

        /// <summary>
        /// If true, all plain and html text will be converted to Xhtml when serialized into the XML
        /// This is to improve rendering when converting to PDF or other display formats
        /// </summary>
        public bool SaveTextAsXhtml { get; set; } = false;

        /// <summary>
        /// LogLevels equal to or above this threshold will also be written to the output XML file as comments
        /// </summary>
        public LogLevel LogToXmlThreshold { get; set; } = LogLevel.Information;

        private string _defaultFileExtension = MBOX_FILE_EXTENSION;
        /// <summary>
        /// If the source input file does not have a filename extension, this is the value that should be used.
        /// It should include the leading period.
        /// </summary>
        public string DefaultFileExtension
        {
            get
            { 
                return _defaultFileExtension;
            }
            set
            {
                if (value == null || value.Length == 0)
                    throw new Exception("DefaultFileExtension cannot be null or empty");
                else if (value[0] != '.')
                    throw new Exception("DefaultFileExtension must start with a period");
                else
                    _defaultFileExtension = value.ToLowerInvariant();
            }
        }

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
        public Dictionary<string, int>? ExtraHtmlCharacterEntities { get; set; } = new Dictionary<string, int>() { { "QUOT", 0x22 } };

        /// <summary>
        /// Force the message parser to run even if the file does not appear to be a valid message file format,
        /// Might be useful for debugging
        /// </summary>
        public bool ForceParse { get; set; } = true;

        public void WriteSettings(XmlWriter xwriter)
        {
            xwriter.WriteComment("Settings for the mbox to XML conversion:");
            xwriter.WriteProcessingInstruction("HashAlgorithmName", HashAlgorithmName);
            xwriter.WriteProcessingInstruction("SaveAttachmentsAndBinaryContentExternally", SaveAttachmentsAndBinaryContentExternally.ToString());
            xwriter.WriteProcessingInstruction("WrapExternalContentInXml", WrapExternalContentInXml.ToString());
            xwriter.WriteProcessingInstruction("PreserveBinaryAttachmentTransferEncodingIfPossible", PreserveBinaryAttachmentTransferEncodingIfPossible.ToString());
            xwriter.WriteProcessingInstruction("PreserveTextAttachmentTransferEncoding", PreserveTextAttachmentTransferEncoding.ToString());
            xwriter.WriteProcessingInstruction("IncludeSubFolders", IncludeSubFolders.ToString());
            xwriter.WriteProcessingInstruction("ExternalContentFolder", ExternalContentFolder);
            xwriter.WriteProcessingInstruction("OneFilePerMessageFile", OneFilePerMessageFile.ToString());
            xwriter.WriteProcessingInstruction("MaximumXmlFileSize", MaximumXmlFileSize.ToString());
            xwriter.WriteProcessingInstruction("SaveTextAsXhtml", SaveTextAsXhtml.ToString());
            xwriter.WriteProcessingInstruction("LogToXmlThreshold", LogToXmlThreshold.ToString());
            xwriter.WriteProcessingInstruction("DefaultFileExtension", DefaultFileExtension);
            if (SkipUntilMessageId != null)
                xwriter.WriteProcessingInstruction("SkipUntilMessageId", SkipUntilMessageId);
            if (SkipAfterMessageId != null)
                xwriter.WriteProcessingInstruction("SkipAfterMessageId", SkipAfterMessageId);
            xwriter.WriteProcessingInstruction("ForceParse", ForceParse.ToString());
        }
    }
}
