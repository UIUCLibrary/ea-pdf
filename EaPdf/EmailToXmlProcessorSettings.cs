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
    public class EmailToXmlProcessorSettings
    {
        /// <summary>
        /// The name of the HashAlgorithm to use, must be one of the values in the System.Security.Cryptography.HashAlgorithmNames class.
        /// Default is SHA256
        /// </summary>
        public string HashAlgorithmName { get; set; } = EmailToXmlProcessor.HASH_DEFAULT;

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
        /// This only applies to content wrapped in XML, internally or externally.  If true, the original Content-Transfer-Encoding for binary 
        /// content (base64, quoted-printable, or uuencode) is used if possible (binary is always serialized as base64) to serialize the content in XML; if false, all non-text content is serialized as base64 when saved inbside the XML
        /// 7bit and 8bit content are always serialized as UTF-8 text inside the XML.  The default is false.
        /// </summary>
        public bool PreserveContentTransferEncodingIfPossible { get; set; } = false;

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

    }
}
