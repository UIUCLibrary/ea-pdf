namespace UIUCLibrary.EaPdf
{
    /// <summary>
    /// Properties that need to be persisted while processing an email message
    /// </summary>
    public class MimeMessageProperties
    {        
        public const string EOL_TYPE_CR = "CR";
        public const string EOL_TYPE_LF = "LF";
        public const string EOL_TYPE_CRLF = "CRLF";
        public const string EOL_TYPE_UNK = "UNKNOWN";

        //Include the mbx message header so it can be used for other purposes if needed, like message statuses
        public MbxMessageHeader? MbxMessageHeader { get; set; } 

        /// <summary>
        /// The type of line ending used in a MIME message 
        /// </summary>
        public string Eol { get; set; } = EOL_TYPE_UNK;

        /// <summary>
        /// The hash for the mime message
        /// </summary>
        public byte[] MessageHash { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The size of the message, counting the same bytes used to calculate the Hash
        /// </summary>
        public long MessageSize { get; set; } = -1;

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

        //FUTURE: The XML schema allows multiple <Incomplete> tags per message, currently the properties allow only one

    }
}
