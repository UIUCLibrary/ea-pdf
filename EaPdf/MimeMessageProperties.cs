using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf
{
    /// <summary>
    /// Properties that need to be persisted while processing an email message
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
        /// </summary>
        public byte[] MessageHash { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// If the message was incomplete because of some error, this be the name of the error
        /// </summary>
        public string? IncompleteErrorType { get; set; } = null;

        /// <summary>
        /// If the message was incomplete because of some error, this be the location or position of the error in the mbox file
        /// </summary>
        public string? IncompleteErrorLocation { get; set; } = null;

        /// <summary>
        /// Reset the message properties, so that there are no incomplete errors
        /// </summary>
        public void NotIncomplete()
        {
            IncompleteErrorType = null;
            IncompleteErrorLocation = null;
        }

        /// <summary>
        /// Set the message's IncompleteErrorType and IncompleteErrorLocation values
        /// </summary>
        /// <param name="errorType"></param>
        /// <param name="errorLocation"></param>
        public void Incomplete(string errorType, string errorLocation)
        {
            IncompleteErrorType = errorType;
            IncompleteErrorLocation = errorLocation;
        }

        //TODO: The XML schema allows multiple <Incomplete> tags per message, currently the properties allow only one

    }
}
