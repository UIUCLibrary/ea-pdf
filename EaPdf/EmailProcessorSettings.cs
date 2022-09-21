using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf
{
    public class EmailProcessorSettings
    {
        /// <summary>
        /// The name of the HashAlgorithm to use, must be one of the values in the System.Security.Cryptography.HashAlgorithmNames class.
        /// Default is SHA256
        /// </summary>
        public string HashAlgorithmName { get; set; } = EmailProcessor.HASH_DEFAULT;

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
    }
}
