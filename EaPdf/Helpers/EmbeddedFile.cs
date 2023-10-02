using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Unspecified
        }

        public AFRelationship Relationship { get; init; } = AFRelationship.Unspecified;

        public string Subtype { get; init; } = MimeTypeMap.DefaultMimeType;

        public string OriginalFileName { get; init; } = string.Empty;

        public string Hash { get; init; } = string.Empty;

        public long Size { get; init; } = 0;

        public DateTime? ModDate { get; init; } = null;

        public DateTime? CreationDate { get; init; } = null;

    }
}
