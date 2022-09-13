using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf
{
    /// <summary>
    /// Represents misc. properties of a MIME message needed for the EAXS XML Schema
    /// </summary>
    internal class MimeMessageProperties
    {
        public const string EOL_TYPE_CR = "CR";
        public const string EOL_TYPE_LF = "LF";
        public const string EOL_TYPE_CRLF = "CRLF";
        public const string EOL_TYPE_UNK = "UNKNOWN";

        /// <summary>
        /// The type of line ending used in a MIME message 
        /// </summary>
        public string Eol { get; set; } = EOL_TYPE_UNK;

        /// <summary>
        /// The hash for the mime message
        /// The hash type is a global property of the EmailProcessor
        /// </summary>
        public byte[] MessageHash { get; set; } = Array.Empty<byte>();

    }
}
