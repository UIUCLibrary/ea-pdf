using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class EmbeddedFile
    {

        public enum AFRelationship
        {
            Source,
            Data,
            Alternative,
            Supplement,
            EncryptedPayload,
            FormData,
            Schema,
            Unspecified,
            Mail_Attachment
        }

        public AFRelationship? Relationship { get; init; } = null;

        public string Subtype { get; init; } = MimeTypeMap.DefaultMimeType;

        public string OriginalFileName { get; init; } = string.Empty;

        public string Hash { get; init; } = string.Empty;
        public string HashAlgorithm { get; init; } = string.Empty;
        public byte[] HashBytes
        {
            get
            {
                if (string.IsNullOrEmpty(Hash))
                {
                    return Array.Empty<byte>();
                }
                var bytes = Convert.FromHexString(Hash);
                return bytes;
            }
        }

        public long Size { get; init; } = 0;

        public DateTime? ModDate { get; init; } = null;

        public DateTime? CreationDate { get; init; } = null;

        public string Description { get; init; } = string.Empty;

        public string UniqueName { get; init; } = string.Empty;

        public XmlDocument? Metadata { get; init; } = null;

        public string MessageId { get; init; } = string.Empty;
    }
}
