using Microsoft.Extensions.Logging;
using MimeKit;
using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using Wiry.Base32;
using CsvHelper;

namespace UIUCLibrary.EaPdf
{
    public class EmailProcessor
    {

        //TODO: Need to add some IO Exception Handling throughout for creating, reading, and writing to files and folders.

        public const string EOL_TYPE_CR = "CR";
        public const string EOL_TYPE_LF = "LF";
        public const string EOL_TYPE_CRLF = "CRLF";
        public const string EOL_TYPE_UNK = "UNKNOWN";

        public const string EXT_CONTENT_DIR = "ExtBodyContent";

        //for LWSP (Linear White Space) detection, compaction, and trimming
        const byte CR = 13;
        const byte LF = 10;
        const byte SP = 32;
        const byte TAB = 9;

        //https://github.com/noelmartinon/mboxzilla/blob/master/nsMsgMessageFlags.h
        [Flags]
        enum XMozillaStatusFlags : ushort
        {
            MSG_FLAG_NULL = 0x0000,
            MSG_FLAG_READ = 0x0001,
            MSG_FLAG_REPLIED = 0x0002,
            MSG_FLAG_MARKED = 0x0004,
            MSG_FLAG_EXPUNGED = 0x0008,
            MSG_FLAG_HAS_RE = 0x0010,
            MSG_FLAG_ELIDED = 0x0020,
            MSG_FLAG_OFFLINE = 0x0080,
            MSG_FLAG_WATCHED = 0x0100,
            MSG_FLAG_SENDER_AUTHED = 0x0200,
            MSG_FLAG_PARTIAL = 0x0400,
            MSG_FLAG_QUEUED = 0x0800,
            MSG_FLAG_FORWARDED = 0x1000,
            MSG_FLAG_PRIORITIES = 0xE000
        }
        [Flags]
        enum XMozillaStatusFlags2 : uint
        {
            MSG_FLAG_NULL = 0x00000000,
            MSG_FLAG_NEW = 0x00010000,
            MSG_FLAG_IGNORED = 0x00040000,
            MSG_FLAG_IMAP_DELETED = 0x00200000,
            MSG_FLAG_MDN_REPORT_NEEDED = 0x00400000,
            MSG_FLAG_MDN_REPORT_SENT = 0x00800000,
            MSG_FLAG_TEMPLATE = 0x01000000,
            MSG_FLAG_LABELS = 0x0E000000,
            MSG_FLAG_ATTACHMENT = 0x10000000
        }

        const string STATUS_SEEN = "Seen";
        const string STATUS_ANSWERED = "Answered";
        const string STATUS_FLAGGED = "Flagged";
        const string STATUS_DELETED = "Deleted";
        const string STATUS_DRAFT = "Draft";
        const string STATUS_RECENT = "Recent";

        public const string XM = "xm";
        public const string XM_NS = "https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2";
        public const string XM_XSD = "eaxs_schema_v2.xsd";

        private readonly ILogger _logger;
        private readonly HashAlgorithm _cryptoHashAlg;

        public const string HASH_DEFAULT = "SHA256";


        public EmailProcessorSettings Settings { get; }


        /// <summary>
        /// Create a processor for email files, initializing the logger and settings
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public EmailProcessor(ILogger<EmailProcessor> logger, EmailProcessorSettings settings)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }


            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Settings = settings;

            _logger = logger;
            _logger.LogInformation("MboxProcessor Created");

            var alg = HashAlgorithm.Create(Settings.HashAlgorithmName);
            if (alg != null)
            {
                _cryptoHashAlg = alg;
            }
            else
            {
                //default to a known algorithm
                _logger.LogWarning($"Unable to instantiate hash algorithm '{Settings.HashAlgorithmName}', using '{HASH_DEFAULT}' instead");
                Settings.HashAlgorithmName = HASH_DEFAULT;
                _cryptoHashAlg = SHA256.Create();
            }

        }


        /// <summary>
        /// Convert the mbox directory or file into an archival email XML file.
        /// </summary>
        /// <param name="mboxFilePath">the path to the mbox file or directory of mbox files to process</param>
        /// <param name="outFolderPath">the path to the output folder; if blank, defaults to the same folder as the mbox file</param>
        /// <param name="accntId">Globally unique, permanent, absolute URI with no fragment conforming to the canonical form specified in RFC2396 as amended by RFC2732.</param>
        /// <param name="accntEmails">Comma-separated list of email addresses</param>
        /// <returns>the most recent localId number which is usually the total number of messages processed</returns>
        public long ConvertMbox2EAXS(string mboxFilePath, ref string outFolderPath, string accntId, string accntEmails = "")
        {
            //TODO: manage the case where the mboxFilePath is a directory and not just a single file
            //TODO: add code to process correspondingly named subdirectories as sub folders to a given mbox file.
            //TODO: add code to process directory and subdirectory or single email message EML files.

            if (string.IsNullOrWhiteSpace(mboxFilePath))
            {
                throw new ArgumentNullException(nameof(mboxFilePath));
            }

            if (!File.Exists(mboxFilePath) && !Directory.Exists(mboxFilePath))
            {
                throw new FileNotFoundException(mboxFilePath);
            }

            //Keep track of the line endings used in the mbox file
            Dictionary<string, int> eolCounts = new Dictionary<string, int>();
            string eol = EOL_TYPE_UNK;
            
            byte[] messageHash = Array.Empty<byte>();

            //open filestream and wrap it in a cryptostream so that we can hash the file as we process it
            mboxFilePath = Path.GetFullPath(mboxFilePath);
            using FileStream mboxStream = new FileStream(mboxFilePath, FileMode.Open, FileAccess.Read);
            using CryptoStream cryptoStream = new CryptoStream(mboxStream, _cryptoHashAlg, CryptoStreamMode.Read);

            long localId = 0;

            if (string.IsNullOrWhiteSpace(accntId))
            {
                throw new Exception("acctId is a required parameter");
            }

            if (string.IsNullOrWhiteSpace(outFolderPath))
            {
                outFolderPath = Path.GetDirectoryName(mboxFilePath) ?? "";
            }

            var outFilePath = Path.Combine(outFolderPath, Path.GetFileName(Path.ChangeExtension(mboxFilePath, "xml")));
            var csvFilePath = Path.Combine(outFolderPath, Path.GetFileName(Path.ChangeExtension(mboxFilePath, "csv")));

            var messageList = new List<MessageBrief>(); //used to create the CSV file


            _logger.LogInformation("Convert email file: '{0}' into XML file: '{1}'", mboxFilePath, outFilePath);


            var xset = new XmlWriterSettings()
            {
                CloseOutput = true,
                Indent = true,
                Encoding = System.Text.Encoding.UTF8
            };

            if (!Directory.Exists(Path.GetDirectoryName(outFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outFilePath) ?? "");
            }


            using var xwriter = XmlWriter.Create(outFilePath, xset);
            xwriter.WriteStartDocument();
            xwriter.WriteProcessingInstruction("Settings", $"HashAlgorithmName: {Settings.HashAlgorithmName}, SaveAttachmentsAndBinaryContentExternally: {Settings.SaveAttachmentsAndBinaryContentExternally}, WrapExternalContentInXml: {Settings.WrapExternalContentInXml}, PreserveContentTransferEncodingIfPossible: {Settings.PreserveContentTransferEncodingIfPossible}");
            xwriter.WriteStartElement("Account", XM_NS);
            xwriter.WriteAttributeString("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance", "eaxs_schema_v2.xsd");
            foreach (var addr in accntEmails.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                xwriter.WriteElementString("EmailAddress", XM_NS, addr);
            }
            xwriter.WriteElementString("GlobalId", XM_NS, accntId);

            xwriter.WriteStartElement("Folder", XM_NS);
            xwriter.WriteElementString("Name", XM_NS, Path.GetFileNameWithoutExtension(mboxFilePath));
            
            var parser = new MimeParser(cryptoStream, MimeFormat.Mbox);

            parser.MimeMessageEnd += (sender, e) => Parser_MimeMessageEnd(sender, e, mboxStream, eolCounts, ref eol, messageHash);
            
            while (!parser.IsEndOfStream)
            {
                localId = ProcessCurrentMessage(mboxFilePath, parser, xwriter, localId, messageList, outFilePath, accntId, ref eol, messageHash);
            }

            //make sure to read to the end of the stream so the hash is correct
            int i = -1;
            do
            {
                i = cryptoStream.ReadByte();
            } while (i != -1);

            xwriter.WriteStartElement("Mbox", XM_NS);
            var relPath = Path.GetRelativePath(Path.GetDirectoryName(outFilePath) ?? "", mboxFilePath);
            xwriter.WriteElementString("RelPath", XM_NS, relPath);
            xwriter.WriteElementString("Eol", XM_NS, MostCommonEol(eolCounts));
            if (UsesDifferentEols(eolCounts))
            {
                var warn = $"Mbox file contains multiple different EOLs: CR: {eolCounts["CR"]}, LF: {eolCounts["LF"]}, CRLF: {eolCounts["CRLF"]}";
                _logger.LogWarning(warn);
                xwriter.WriteComment($"WARNING: {warn}");
            }
            if (_cryptoHashAlg.Hash != null)
            {
                WriteHash(xwriter, _cryptoHashAlg.Hash, Settings.HashAlgorithmName);
            }
            else
            {
                var warn = $"Unable to calculate the hash value for the Mbox";
                _logger.LogWarning(warn);
                xwriter.WriteComment($"WARNING: {warn}");
            }

            xwriter.WriteEndElement(); //Mbox

            xwriter.WriteEndElement(); //Folder
            xwriter.WriteEndElement(); //Account

            xwriter.WriteEndDocument();

            //write the csv file
            using var csvStream = new StreamWriter(csvFilePath);
            using var csvWriter = new CsvWriter(csvStream, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(messageList);

            _logger.LogInformation("Converted {0} messages", localId);

            return localId;
        }

        private void WriteHash(XmlWriter xwriter, byte[] hash, string hashAlgorithmName)
        {
            xwriter.WriteStartElement("Hash", XM_NS);
            xwriter.WriteStartElement("Value", XM_NS);
            xwriter.WriteBinHex(hash, 0, hash.Length);
            xwriter.WriteEndElement(); //Value
            xwriter.WriteElementString("Function", XM_NS, hashAlgorithmName);
            xwriter.WriteEndElement(); //Hash
        }

        private long ProcessCurrentMessage(string mboxFilePath, MimeParser parser, XmlWriter xwriter, long localId, List<MessageBrief> messageList, string outFilePath, string accntId, ref string eol, byte[] messageHash)
        {
            MimeMessage? message;
            int errCnt = 0;
            string errMsg = String.Empty;
            try
            {
                message = parser.ParseMessage();
            }
            catch (Exception ex)
            {
                errCnt++;
                errMsg = ex.Message;
                message = new MimeMessage(); //create dummy message
            }

            if (message != null)
            {

                localId++;
                var messageId = localId;

                xwriter.WriteStartElement("Message", XM_NS);
                if (errCnt == 0) //process the message
                {
                    localId = ConvertMessageToEAXS(mboxFilePath, message, xwriter, localId, outFilePath, false, eol, messageHash);
                }
                else //log an error and create an incomplete message; <Incomplete> Seems related to errors
                {
                    //NEEDSTEST:  Will need to create a deliberately malformed mbox to test this
                    _logger.LogError(errMsg);
                    WriteIncompleteMessage(localId, accntId, xwriter, errMsg, parser.Position, eol);
                }
                xwriter.WriteEndElement(); //Message

                messageList.Add(new MessageBrief()
                {
                    LocalId = messageId,
                    From = message.From.ToString(),
                    To = message.To.ToString(),
                    Date = message.Date,
                    Subject = message.Subject,
                    MessageID = message.MessageId,
                    Hash = Convert.ToHexString(messageHash, 0, messageHash.Length),
                    Errors = errCnt,
                    FirstErrorMessage = errMsg

                });
            }
            eol = EOL_TYPE_UNK;

            return localId;
        }

        private void WriteIncompleteMessage(long localId, string accntId, XmlWriter xwriter, string errMsg, long position, string eol)
        {
            xwriter.WriteElementString("LocalId", XM_NS, localId.ToString());
            xwriter.WriteStartElement("MessageId", XM_NS);
            xwriter.WriteAttributeString("Supplied", "true");
            xwriter.WriteString($"{accntId}-{localId}");
            xwriter.WriteEndElement(); //MessageId
            xwriter.WriteStartElement("Incomplete", XM_NS);
            xwriter.WriteElementString("ErrorType", XM_NS, errMsg);
            xwriter.WriteElementString("ErrorLocation", XM_NS, $"Stream Position: {position}");
            xwriter.WriteEndElement(); //Incomplete
            xwriter.WriteElementString("Eol", XM_NS, eol); //This might be unknown at this point if there was an error
        }

        private long ConvertMessageToEAXS(string mboxFilePath, MimeMessage message, XmlWriter xwriter, long localId, string outFilePath, bool isChildMessage,string eol, byte[] messageHash)
        {

            _logger.LogInformation("Converting Message {0} Subject: {1}", localId, message.Subject);

            if (!isChildMessage)
            {
                xwriter.WriteElementString("RelPath", XM_NS, EXT_CONTENT_DIR);
            }

            xwriter.WriteStartElement("LocalId", XM_NS);
            xwriter.WriteValue(localId);
            xwriter.WriteEndElement();

            WriteStandardMessageHeaders(xwriter, message);

            WriteAllMessageHeaders(xwriter, message);

            if (!isChildMessage)
            {
                WriteMessageStatuses(xwriter, mboxFilePath, message);
            }

            localId = ConvertBody2EAXS(mboxFilePath, message.Body, xwriter, localId, outFilePath, eol, messageHash);

            if (!isChildMessage)
            {
                xwriter.WriteElementString("Eol", XM_NS, eol);

                WriteHash(xwriter, messageHash, Settings.HashAlgorithmName);
            }

            return localId;
        }

        private void WriteMessageStatuses(XmlWriter xwriter, string mboxFilepath, MimeMessage message)
        {
            //StatusFlags
            string status = "";
            if (TryGetSeen(message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (TryGetAnswered(message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (TryGetFlagged(message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (TryGetDeleted(message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (TryGetDraft(mboxFilepath, message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (TryGetRecent(message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
        }

        private void WriteAllMessageHeaders(XmlWriter xwriter, MimeMessage message)
        {
            foreach (var hdr in message.Headers) //All headers even if already covered above
            {
                xwriter.WriteStartElement("Header", XM_NS);
                xwriter.WriteElementString("Name", XM_NS, hdr.Field);
                xwriter.WriteElementString("Value", XM_NS, hdr.Value);
                //TODO: Comments, not currently supported by MimeKit
                xwriter.WriteEndElement();
            }
        }

        private void WriteStandardMessageHeaders(XmlWriter xwriter, MimeMessage message)
        {
            xwriter.WriteElementString("MessageId", XM_NS, message.MessageId);

            if (message.MimeVersion != null)
            {
                xwriter.WriteElementString("MimeVersion", XM_NS, message.MimeVersion.ToString());
            }

            xwriter.WriteStartElement("OrigDate", XM_NS);
            xwriter.WriteValue(message.Date);
            xwriter.WriteEndElement();

            foreach (var addr in message.From)
            {
                xwriter.WriteElementString("From", XM_NS, addr.ToString());
            }

            if (message.Sender != null)
            {
                xwriter.WriteElementString("Sender", XM_NS, message.Sender.ToString());
            }

            foreach (var addr in message.To)
            {
                xwriter.WriteElementString("To", XM_NS, addr.ToString());
            }

            foreach (var addr in message.Cc)
            {
                xwriter.WriteElementString("Cc", XM_NS, addr.ToString());
            }

            foreach (var addr in message.Bcc)
            {
                xwriter.WriteElementString("Bcc", XM_NS, addr.ToString());
            }

            if (!string.IsNullOrWhiteSpace(message.InReplyTo))
            {
                xwriter.WriteElementString("InReplyTo", XM_NS, message.InReplyTo);
            }

            foreach (var id in message.References)
            {
                xwriter.WriteElementString("References", XM_NS, id);
            }

            if (!string.IsNullOrWhiteSpace(message.Subject))
            {
                xwriter.WriteElementString("Subject", XM_NS, message.Subject);
            }

            //Comments
            string[] cmtHdrs = { "x-comments", "x-comment", "comments", "comment" };
            foreach (var kwds in message.Headers.Where(h => cmtHdrs.Contains(h.Field, StringComparer.InvariantCultureIgnoreCase)))
            {
                xwriter.WriteElementString("Comments", XM_NS, kwds.Value);
            }

            //Keywords
            string[] kwHdrs = { "x-keywords", "x-keyword", "keywords", "keyword" };
            foreach (var kwds in message.Headers.Where(h => kwHdrs.Contains(h.Field, StringComparer.InvariantCultureIgnoreCase)))
            {
                xwriter.WriteElementString("Keywords", XM_NS, kwds.Value);
            }
        }

        private long ConvertBody2EAXS(string mboxFilePath, MimeEntity mimeEntity, XmlWriter xwriter, long localId, string outFilePath,string eol, byte[] messageHash)
        {
            bool isMultipart = false;

            MimePart? part = mimeEntity as MimePart;
            Multipart? multipart = mimeEntity as Multipart;
            MessagePart? message = mimeEntity as MessagePart;

            if (mimeEntity is Multipart)
            {
                isMultipart = true;
                xwriter.WriteStartElement("MultiBody", XM_NS);
            }
            else if (mimeEntity is MimePart)
            {
                xwriter.WriteStartElement("SingleBody", XM_NS);
            }
            else if (mimeEntity is MessagePart)
            {
                xwriter.WriteStartElement("SingleBody", XM_NS);
            }
            else
            {
                string warn = $"Unexpected MIME Entity Type: '{mimeEntity.GetType().FullName}' -- '{mimeEntity.ContentType.MimeType}'";
                _logger.LogWarning(warn);
                xwriter.WriteComment($"WARNING: {warn}");
                xwriter.WriteStartElement("SingleBody", XM_NS);
            }

            xwriter.WriteStartAttribute("IsAttachment");
            xwriter.WriteValue(mimeEntity.IsAttachment);
            xwriter.WriteEndAttribute();

            WriteMimeContentType(xwriter, mimeEntity, isMultipart);

            WriteMimeOtherStandardHeaders(xwriter, mimeEntity, isMultipart);

            WriteMimeContentDisposition(xwriter, mimeEntity);

            WriteMimeOtherHeaders(xwriter, mimeEntity);

            if (isMultipart && multipart != null && !string.IsNullOrWhiteSpace(multipart.Preamble))
            {
                xwriter.WriteElementString("Preamble", XM_NS, multipart.Preamble.Trim());
            }

            if (isMultipart && multipart != null && multipart.Count > 0)
            {
                foreach (var item in multipart)
                {
                    localId = ConvertBody2EAXS(mboxFilePath, item, xwriter, localId, outFilePath, eol, messageHash);
                }
            }
            else if (isMultipart && multipart != null && multipart.Count == 0)
            {
                var warn = $"Item is multipart, but there are no parts";
                _logger.LogWarning(warn);
                xwriter.WriteComment($"WARNING: {warn}");
            }
            else if (isMultipart && multipart == null)
            {
                var warn = $"Item is erroneously flagged as multipart";
                _logger.LogWarning(warn);
                xwriter.WriteComment($"WARNING: {warn}");
            }
            else if (!isMultipart)
            {
                if (part != null)
                {
                    WriteSingleBodyContent(xwriter, part, localId, outFilePath);
                }
                else if (message != null)
                {
                    WriteSingleBodyChildMessage(xwriter, mboxFilePath, message, localId, outFilePath, eol, messageHash);
                }
                else
                {
                    string warn = $"Unexpected MimeEntity: {mimeEntity.GetType().FullName}";
                    _logger.LogWarning(warn);
                    xwriter.WriteComment($"WARNING: {warn}");
                }
            }
            else
            {
                string warn = $"Unexpected MimeEntity: {mimeEntity.GetType().FullName}";
                _logger.LogWarning(warn);
                xwriter.WriteComment($"WARNING: {warn}");
            }

            //PhantomBody; Content-Type message/external-body
            //NEEDSTEST: Need to write a test for this
            if (!isMultipart && part != null && part.ContentType.IsMimeType("message", "external-body"))
            {
                var streamReader = new StreamReader(part.Content.Open(), part.ContentType.CharsetEncoding, true);
                xwriter.WriteElementString("PhantomBody", XM_NS, streamReader.ReadToEnd());
            }


            if (isMultipart && multipart != null && !string.IsNullOrWhiteSpace(multipart.Epilogue))
            {
                xwriter.WriteElementString("Epilogue", XM_NS, multipart.Epilogue.Trim());
            }


            xwriter.WriteEndElement(); //SingleBody or MultiBody
            
            return localId;
        }

        private void WriteSingleBodyChildMessage(XmlWriter xwriter, string mboxFilePath, MessagePart message, long localId, string outFilePath, string eol, byte[] messageHash)
        {
            xwriter.WriteStartElement("ChildMessage", XM_NS);
            localId++;
            ConvertMessageToEAXS(mboxFilePath, message.Message, xwriter, localId, outFilePath, true, eol, messageHash);
            xwriter.WriteEndElement(); //ChildMessage
        }

        private void WriteSingleBodyContent(XmlWriter xwriter, MimePart part, long localId, string outFilePath)
        {
            var content = part.Content;

            //if it is text and not an attachment, save embedded in the XML
            if (part.ContentType.IsMimeType("text", "*") && !part.IsAttachment)
            {
                xwriter.WriteStartElement("BodyContent", XM_NS);
                xwriter.WriteStartElement("Content", XM_NS);
                //Decode the stream and treat it as whatever the charset advertised in the content-type header
                StreamReader reader = new StreamReader(content.Open(), part.ContentType.CharsetEncoding, true);
                xwriter.WriteCData(reader.ReadToEnd());
                xwriter.WriteEndElement(); //Content
                xwriter.WriteEndElement(); //BodyContent
            }
            else //it is not text or it is an attachment
            {
                if (!Settings.SaveAttachmentsAndBinaryContentExternally)
                {
                    //save non-text content or attachments as part of the XML
                    SerializeContentInXml(part, xwriter, false);
                }
                else
                {
                    //save non-text content or attachments externally, possibly wrapped in XML
                    SerializeContentInExtFile(part, xwriter, outFilePath, localId);
                }
            }
        }

        private void WriteMimeOtherStandardHeaders(XmlWriter xwriter, MimeEntity mimeEntity, bool isMultipart)
        {
            //MimeKit only exposes Content-Transfer-Encoding as a property for single body messages.
            //According to specs it can be used for multipart entities, but it must be 7bit, 8bit, or binary, and always 7bit for practical purposes.
            //Getting it directly from the Headers property to cover both cases since the XML schema allows it
            if (mimeEntity.Headers.Contains("Content-Transfer-Encoding"))
            {
                var transferEncoding = mimeEntity.Headers[HeaderId.ContentTransferEncoding].ToLowerInvariant();
                xwriter.WriteElementString("TransferEncoding", XM_NS, transferEncoding);
                if (isMultipart && !transferEncoding.Equals("7bit", StringComparison.InvariantCultureIgnoreCase))
                {
                    var warn = $"A multipart entity has a Content-Transfer-Encoding of '{transferEncoding}'; normally this should only be 7bit for multipart entities.";
                    _logger.LogWarning(warn);
                    xwriter.WriteComment($"WARNING: {warn}");
                }
            }
            //TODO: TransferEncodingComments, not currently supported by MimeKit

            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentId))
            {
                xwriter.WriteElementString("ContentId", XM_NS, mimeEntity.ContentId);
            }
            //TODO: ContentIdComments, not currently supported by MimeKit, actually might not be allowed by the RFC - not sure of ContentId is a structured header type

            MimePart? part = mimeEntity as MimePart;
            if (isMultipart && !string.IsNullOrWhiteSpace(part?.ContentDescription))
            {
                xwriter.WriteElementString("Description", XM_NS, part.ContentDescription);
            }
            //TODO: DescriptionComments, not currently supported by MimeKit, actually might not be allowed by the RFC since Description is not a structured header type
        }

        private void WriteMimeOtherHeaders(XmlWriter xwriter, MimeEntity mimeEntity)
        {
            string[] except = new string[] { "content-type", "content-transfer-encoding", "content-id", "content-description", "content-disposition" };
            foreach (var hdr in mimeEntity.Headers.Where(h => !except.Contains(h.Field, StringComparer.InvariantCultureIgnoreCase)))
            {
                xwriter.WriteStartElement("OtherMimeHeader", XM_NS);
                xwriter.WriteElementString("Name", XM_NS, hdr.Field);
                xwriter.WriteElementString("Value", XM_NS, hdr.Value);
                //TODO: OtherMimeHeader/Comments, not currently supported by MimeKit
                xwriter.WriteEndElement(); //OtherMimeHeaders
            }
        }

        private void WriteMimeContentDisposition(XmlWriter xwriter, MimeEntity mimeEntity)
        {
            if (mimeEntity.ContentDisposition != null)
            {
                if (!string.IsNullOrWhiteSpace(mimeEntity.ContentDisposition.Disposition)) //In original V1 XSD this was only applicable to multipart bodies
                {
                    xwriter.WriteElementString("Disposition", XM_NS, mimeEntity.ContentDisposition.Disposition);
                }
                if (!string.IsNullOrWhiteSpace(mimeEntity.ContentDisposition.FileName))
                {
                    xwriter.WriteElementString("DispositionFileName", XM_NS, mimeEntity.ContentDisposition.FileName);
                }

                //TODO: DispositionComments, not currently supported by MimeKit

                string[] except2 = { "filename" };
                foreach (var param in mimeEntity.ContentDisposition.Parameters.Where(p => !except2.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase)))
                {
                    xwriter.WriteStartElement("DispositionParam", XM_NS); //In original V1 XSD this was named DispositionParams (plural) for SingleBody and DispositionParam for MultiBody.  For consistency, singular form now used for both.
                    xwriter.WriteElementString("Name", XM_NS, param.Name);
                    xwriter.WriteElementString("Value", XM_NS, param.Value);
                    xwriter.WriteEndElement(); //DispositionParam(s)
                }
            }
        }

        private void WriteMimeContentType(XmlWriter xwriter, MimeEntity mimeEntity, bool isMultipart)
        {
            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentType.MimeType))
            {
                xwriter.WriteElementString("ContentType", XM_NS, mimeEntity.ContentType.MimeType);
            }
            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentType.Charset))
            {
                xwriter.WriteElementString("Charset", XM_NS, mimeEntity.ContentType.Charset);
            }
            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentType.Name))
            {
                xwriter.WriteElementString("ContentName", XM_NS, mimeEntity.ContentType.Name);
            }
            if (isMultipart && !string.IsNullOrWhiteSpace(mimeEntity.ContentType.Boundary))
            {
                xwriter.WriteElementString("BoundaryString", XM_NS, mimeEntity.ContentType.Boundary);
            }
            else if (!isMultipart && !string.IsNullOrWhiteSpace(mimeEntity.ContentType.Boundary))
            {
                string warn = $"MIME type boundary parameter '{mimeEntity.ContentType.Boundary}' found for a non-multipart mime type";
                _logger.LogWarning(warn);
                xwriter.WriteComment($"WARNING: {warn}");

            }
            else if (isMultipart && string.IsNullOrWhiteSpace(mimeEntity.ContentType.Boundary))
            {
                string warn = "MIME type boundary parameter is missing for a multipart mime type";
                _logger.LogWarning(warn);
                xwriter.WriteComment($"WARNING: {warn}");
            }

            //TODO: ContentTypeComments, not currently supported by MimeKit

            string[] except = { "boundary", "charset", "name" };  //QUESTION: XML Schema says to exclude id, name, and boundary.  Why id and not charset?
            foreach (var param in mimeEntity.ContentType.Parameters.Where(p => !except.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase)))
            {
                xwriter.WriteStartElement("ContentTypeParam", XM_NS);
                xwriter.WriteElementString("Name", XM_NS, param.Name);
                xwriter.WriteElementString("Value", XM_NS, param.Value);
                xwriter.WriteEndElement(); //ContentTypeParam
            }
        }

        /// <summary>
        /// Serialize the mime part as a string in the XML 
        /// </summary>
        /// <param name="part">the MIME part to serialize</param>
        /// <param name="xwriter">the XML writer to serialize it to</param>
        /// <param name="preserveEncodingIfPossible">if true write text as unicode and use the original content encoding if possible; if false write it as either unicode text or as base64 encoded binary</param>
        private void SerializeContentInXml(MimePart part, XmlWriter xwriter, bool ExtContent)
        {
            var content = part.Content;

            xwriter.WriteStartElement("BodyContent", XM_NS);
            if (ExtContent)
            {
                xwriter.WriteAttributeString("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance", "eaxs_schema_v2.xsd");
            }

            xwriter.WriteStartElement("Content", XM_NS);

            var encoding = GetContentEncodingString(content.Encoding);

            //7bit and 8bit should be text content, so decode it and use the streamreader with the contenttype charset, if any, to get the text and write it to the xml in a cdata section
            if (content.Encoding == ContentEncoding.EightBit || content.Encoding == ContentEncoding.SevenBit)
            {
                StreamReader reader = new StreamReader(content.Open(), part.ContentType.CharsetEncoding, true);
                xwriter.WriteCData(reader.ReadToEnd());
                encoding = String.Empty;
            }
            else if (Settings.PreserveContentTransferEncodingIfPossible && (content.Encoding == ContentEncoding.UUEncode || content.Encoding == ContentEncoding.QuotedPrintable || content.Encoding == ContentEncoding.Base64))
            //use the original content encoding in the XML
            {
                //treat the stream as ASCII because it is already encoded and just write it out using the same encoding
                StreamReader reader = new StreamReader(content.Stream, System.Text.Encoding.ASCII);
                xwriter.WriteCData(reader.ReadToEnd());
                encoding = GetContentEncodingString(content.Encoding);
            }
            else //anything is treated as binary content (binary, quoted-printable, uuencode, base64), so copy to a memory stream and write it to the XML as base64
            {
                byte[] byts;
                using (MemoryStream ms = new MemoryStream())
                {
                    content.Open().CopyTo(ms);
                    byts = ms.ToArray();
                }
                xwriter.WriteBase64(byts, 0, byts.Length);
                encoding = "base64";
            }

            xwriter.WriteEndElement(); //Content
            if (!string.IsNullOrWhiteSpace(encoding))
            {
                xwriter.WriteElementString("TransferEncoding", XM_NS, encoding);
            }
            xwriter.WriteEndElement(); //BodyContent
        }

        /// <summary>
        /// Serialize the mime part as a file in the external file system
        /// </summary>
        /// <param name="part">the MIME part to serialize</param>
        /// <param name="xwriter">the XML writer to serialize it to</param>
        /// <param name="outFilePath">path to the folder to write it to</param>
        /// <param name="localId">the current localId value</param>
        /// <param name="wrapInXml">if true the content is wrapped in XML; if false it is decoded an saved as the original file</param>
        /// <param name="preserveEncodingIfPossible">only applies if wrapInXml is true; if true write text as unicode and use the original content encoding if possible; if false write it as either unicode text or as base64 encoded binary</param>
        /// <returns>the new localId value after incrementing it for the new file</returns>
        /// <exception cref="Exception">thrown if unable to generate the hash</exception>
        private long SerializeContentInExtFile(MimePart part, XmlWriter xwriter, string outFilePath, long localId)
        {
            var content = part.Content;

            //get random file name that doesn't already exist
            string randomFilePath = "";
            do
            {
                randomFilePath = Path.Combine(Path.GetDirectoryName(outFilePath) ?? "", Path.GetRandomFileName());
            } while (File.Exists(randomFilePath));

            //Write the content to an external file
            //Use the hash as the filename, try to use the file extension if there is one
            //write content to a file and generate the hash which will be used as the file name
            using var contentStream = new FileStream(randomFilePath, FileMode.CreateNew, FileAccess.Write);
            using var cryptoHashAlg = HashAlgorithm.Create(Settings.HashAlgorithmName) ?? SHA256.Create();  //Fallback to known hash algorithm
            using var cryptoStream = new CryptoStream(contentStream, cryptoHashAlg, CryptoStreamMode.Write);

            if (!Settings.WrapExternalContentInXml)
            {
                content.DecodeTo(cryptoStream);
            }
            else
            {
                var extXmlWriter = XmlWriter.Create(cryptoStream, new XmlWriterSettings { Indent = true, Encoding = System.Text.Encoding.UTF8 });
                extXmlWriter.WriteStartDocument();
                SerializeContentInXml(part, extXmlWriter, true);
                extXmlWriter.WriteEndDocument();
                extXmlWriter.Close();
            }

            cryptoStream.Close();
            contentStream.Close();

            string hashStr = "";
            if (cryptoHashAlg.Hash != null)
                hashStr = Base32Encoding.ZBase32.GetString(cryptoHashAlg.Hash, 0, cryptoHashAlg.Hash.Length); // uses z-base-32 encoding for file names, https://en.wikipedia.org/wiki/Base32
            else
                throw new Exception($"Unable to calculate hash value for the content");

            var ext = Path.GetExtension(part.FileName);
            if (Settings.WrapExternalContentInXml)
            {
                ext = ".xml";
            }

            var hashFileName = Path.Combine(Path.GetDirectoryName(outFilePath) ?? "", EXT_CONTENT_DIR, hashStr[..2], Path.ChangeExtension(hashStr, ext));
            Directory.CreateDirectory(Path.GetDirectoryName(hashFileName) ?? "");

            //Deal with duplicate attachments, which should only be stored once, make sure the randomFilePath file is deleted
            if (File.Exists(hashFileName))
            {
                var msg = $"Duplicate attachment has already been saved";
                _logger.LogInformation(msg);
                xwriter.WriteComment($"INFO: {msg}");
                File.Delete(randomFilePath);
            }
            else
            {
                File.Move(randomFilePath, hashFileName);
            }

            xwriter.WriteStartElement("ExtBodyContent", XM_NS);
            xwriter.WriteElementString("RelPath", XM_NS, Path.GetRelativePath(Path.Combine(Path.GetDirectoryName(outFilePath) ?? "", EXT_CONTENT_DIR), hashFileName));
            //CharSet and TransferEncoding are the same as for the SingleBody
            localId++;
            xwriter.WriteElementString("LocalId", XM_NS, localId.ToString());
            xwriter.WriteElementString("XMLWrapped", XM_NS, Settings.WrapExternalContentInXml.ToString().ToLower());
            //Eol is not applicable since we are not wrapping the content in XML
            WriteHash(xwriter, cryptoHashAlg.Hash, Settings.HashAlgorithmName);


            xwriter.WriteEndElement(); //ExtBodyContent

            return localId;
        }


        private bool TryGetSeen(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //if the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead

            var ret = false;
            status = "";

            XMozillaStatusFlags xMozillaStatus = GetXMozillaStatus(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            if (mimeStatus.Contains('R')) //Read
            {
                ret = true;
                status = STATUS_SEEN;
            }

            if (xMozillaStatus.HasFlag(XMozillaStatusFlags.MSG_FLAG_READ))
            {
                ret = true;
                status = STATUS_SEEN;
            }

            return ret;
        }

        private bool TryGetAnswered(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //if the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead

            var ret = false;
            status = "";

            XMozillaStatusFlags xMozillaStatus = GetXMozillaStatus(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            if (mimeStatus.Contains('A')) //Answered
            {
                ret = true;
                status = STATUS_ANSWERED;
            }

            if (xMozillaStatus.HasFlag(XMozillaStatusFlags.MSG_FLAG_REPLIED))
            {
                ret = true;
                status = STATUS_ANSWERED;
            }

            return ret;
        }

        private bool TryGetFlagged(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //if the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead

            var ret = false;
            status = "";

            XMozillaStatusFlags xMozillaStatus = GetXMozillaStatus(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            if (mimeStatus.Contains('F')) //Flagged
            {
                ret = true;
                status = STATUS_FLAGGED;
            }

            if (xMozillaStatus.HasFlag(XMozillaStatusFlags.MSG_FLAG_MARKED))
            {
                ret = true;
                status = STATUS_FLAGGED;
            }

            return ret;
        }

        private bool TryGetDeleted(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //if the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead

            var ret = false;
            status = "";

            XMozillaStatusFlags xMozillaStatus = GetXMozillaStatus(message);
            XMozillaStatusFlags2 xMozillaStatus2 = GetXMozillaStatus2(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            if (mimeStatus.Contains('D')) //Deleted
            {
                ret = true;
                status = STATUS_DELETED;
            }

            //For Mozilla: waiting to be expunged by the client or marked as deleted on the IMAP Server
            if (xMozillaStatus.HasFlag(XMozillaStatusFlags.MSG_FLAG_EXPUNGED) || xMozillaStatus2.HasFlag(XMozillaStatusFlags2.MSG_FLAG_IMAP_DELETED))
            {
                ret = true;
                status = STATUS_DELETED;
            }

            return ret;
        }

        private bool TryGetDraft(string mboxFilePath, MimeMessage message, out string status)
        {
            //See https://docs.python.org/3/library/mailbox.html#mailbox.MaildirMessage for more hints -- maybe encoded in the filename of an "info" section?
            //See https://opensource.apple.com/source/dovecot/dovecot-293/dovecot/doc/wiki/MailboxFormat.mbox.txt.auto.html about the X-Status 'T' flag

            var ret = false;
            status = "";


            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];


            //If it is a Mozilla message that came from a file called 'Draft' then assume it is a 'draft' message
            //There is also the X-Mozilla-Draft-Info header which could indicate whether a message is draft
            if (
                (Path.GetFileNameWithoutExtension(mboxFilePath ?? "").Equals("Drafts", StringComparison.OrdinalIgnoreCase) && message.Headers.Contains("X-Mozilla-Status")) //if it is a Mozilla message and the filename is Drafts
                || message.Headers.Contains("X-Mozilla-Draft-Info")  //Mozilla uses this header for draft messages
                || mimeStatus.Contains('T')  //Some clients encode draft as "T" in the X-Status header
                )
            {
                ret = true;
                status = STATUS_DRAFT;
            }

            return ret;
        }

        private bool TryGetRecent(MimeMessage message, out string status)
        {
            //Look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //if the x-mozilla-status and x-mozilla-status2 headers are present they will be used instead

            var ret = false;
            status = "";

            XMozillaStatusFlags xMozillaStatus = GetXMozillaStatus(message);
            XMozillaStatusFlags2 xMozillaStatus2 = GetXMozillaStatus2(message);

            var mimeStatus = message.Headers[HeaderId.Status] + message.Headers[HeaderId.XStatus];

            //if there is a status header but it does not contain the 'O' (Old) flag then it is recent
            if (message.Headers.Contains(HeaderId.Status) && !mimeStatus.Contains('O')) //Not Old
            {
                ret = true;
                status = STATUS_RECENT;
            }

            //For Mozilla: If there is a "X-Mozilla-Status" header but there are no XMozillaStatusFlags or the XMozillaStatusFlags2 is set to NEW
            if ((message.Headers.Contains("X-Mozilla-Status") && xMozillaStatus == XMozillaStatusFlags.MSG_FLAG_NULL) || xMozillaStatus2.HasFlag(XMozillaStatusFlags2.MSG_FLAG_NEW))
            {
                ret = true;
                status = STATUS_RECENT;
            }

            return ret;
        }

        private XMozillaStatusFlags GetXMozillaStatus(MimeMessage message)
        {
            XMozillaStatusFlags ret = 0;

            var xMozillaStatus = message.Headers["X-Mozilla-Status"];
            if (!string.IsNullOrWhiteSpace(xMozillaStatus))
            {
                ushort xMozillaBitFlag;
                if (ushort.TryParse(xMozillaStatus, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out xMozillaBitFlag))
                {
                    ret = (XMozillaStatusFlags)xMozillaBitFlag;
                }
            }
            return ret;
        }

        private XMozillaStatusFlags2 GetXMozillaStatus2(MimeMessage message)
        {
            XMozillaStatusFlags2 ret = 0;

            var xMozillaStatus2 = message.Headers["X-Mozilla-Status2"];
            if (!string.IsNullOrWhiteSpace(xMozillaStatus2))
            {
                uint xMozillaBitFlag2;
                if (uint.TryParse(xMozillaStatus2, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out xMozillaBitFlag2))
                {
                    ret = (XMozillaStatusFlags2)xMozillaBitFlag2;
                }
            }
            return ret;
        }

        /// <summary>
        /// Convert the MimeKit enum into a standard encoding string value
        /// </summary>
        /// <param name="enc"></param>
        /// <returns></returns>
        private string GetContentEncodingString(ContentEncoding enc)
        {
            return enc switch
            {
                ContentEncoding.Default => "",
                ContentEncoding.SevenBit => "7bit",
                ContentEncoding.EightBit => "8bit",
                ContentEncoding.Binary => "binary",
                ContentEncoding.Base64 => "base64",
                ContentEncoding.QuotedPrintable => "quoted-printable",
                ContentEncoding.UUEncode => "uuencode",
                _ => "",
            };
        }

        private void Parser_MimeMessageEnd(object? sender, MimeMessageEndEventArgs e, Stream mboxStream, Dictionary<string, int> eolCounts, ref string eol, byte[] messageHash)
        {

            var parser = sender as MimeParser;
            var endOffset = e.EndOffset;
            var beginOffset = e.BeginOffset;
            var headersEndOffset = e.HeadersEndOffset;
            long mboxMarkerOffset = 0;
            if (parser != null)
            {
                mboxMarkerOffset = parser.MboxMarkerOffset;
            }
            else
            {
                mboxMarkerOffset = beginOffset; //use the start of the message, instead of the start of the Mbox marker
                _logger.LogWarning("Unable to determine the start of the Mbox marker");
            }

            // get the raw data from the stream to calculate eol and hash for the xml
            byte[] buffer = new byte[endOffset - mboxMarkerOffset];
            var origPos = mboxStream.Position;
            mboxStream.Seek(mboxMarkerOffset, SeekOrigin.Begin);
            mboxStream.Read(buffer, 0, buffer.Length);
            mboxStream.Position = origPos;

            //Look for first EOL marker to determine which kind are being used.
            //Assume the same kind will be used throughout
            long i = 1;
            while (i < buffer.Length - 1)
            {
                if (buffer[i] == LF && buffer[i - 1] == CR)
                {
                    eol = EOL_TYPE_CRLF;
                    break;
                }
                else if (buffer[i] == LF)
                {
                    eol = EOL_TYPE_LF;
                    break;
                }
                else if (buffer[i] == CR && buffer[i + 1] != LF)
                {
                    eol = EOL_TYPE_CR;
                    break;
                }
                i++;
            }

            //Check that messages use the same EOL treatment throughout the mbox
            if (eol != EOL_TYPE_UNK)
            {
                if (eolCounts.ContainsKey(eol))
                {
                    eolCounts[eol]++;
                }
                else
                {
                    eolCounts.Add(eol, 1);
                }
            }

            //trim all LWSP and EOL chars from the end and then add one eol marker back
            //assumes that the same EOL markers are used throughout the mbox
            i = buffer.Length - 1;
            while (buffer[i] == LF || buffer[i] == CR || buffer[i] == SP || buffer[i] == TAB)
                --i;
            long j = 1;
            if (eol == EOL_TYPE_CRLF)
                j = 2;
            byte[] newBuffer = new byte[i + 1 + j];

            Array.Copy(buffer, 0, newBuffer, 0, i + 1);
            if (eol == EOL_TYPE_CRLF)
            {
                newBuffer[newBuffer.Length - 1] = LF;
                newBuffer[newBuffer.Length - 2] = CR;
            }
            else if (eol == EOL_TYPE_LF)
            {
                newBuffer[newBuffer.Length - 1] = LF;
            }
            else if (eol == EOL_TYPE_CR)
            {
                newBuffer[newBuffer.Length - 1] = CR;
            }
            else
            {
                _logger.LogError("Unable to determine EOL marker");
                throw new Exception("Unable to determine EOL marker");
            }

            var hashAlg = HashAlgorithm.Create(Settings.HashAlgorithmName) ?? SHA256.Create(); //Fallback to known hash algorithm
            messageHash = hashAlg.ComputeHash(newBuffer);

        }

        /// <summary>
        /// The processor keeps tracks of the different line ending styles used in the mbox file.
        /// If thtere are different line endings in the file this will return true
        /// Otyherwise, it returns false
        /// </summary>
        private bool UsesDifferentEols(Dictionary<string, int> eolCounts)
        {
            return eolCounts.Count > 1;
        }

        /// <summary>
        /// The processor keeps tracks of the different line ending styles used in the mbox file.
        /// This will return the most common line ending style used in the file.
        /// </summary>
        private string MostCommonEol(Dictionary<string, int> eolCounts)
        {
            var ret = "";
            var max = 0;
            foreach (var kvp in eolCounts)
            {
                if (kvp.Value > max)
                {
                    max = kvp.Value;
                    ret = kvp.Key;
                }
            }
            return ret;
        }
    }
}