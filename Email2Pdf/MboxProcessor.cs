using Microsoft.Extensions.Logging;
using MimeKit;
using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using Wiry.Base32;
using CsvHelper;

namespace Email2Pdf
{
    public class MboxProcessor : IDisposable
    {
        private bool disposedValue;

        public const string EOL_TYPE_CR = "CR";
        public const string EOL_TYPE_LF = "LF";
        public const string EOL_TYPE_CRLF = "CRLF";
        public const string EOL_TYPE_UNK = "UNKNOWN";

        public const string EXT_CONTENT_DIR = "ExtBodyContent";

        //for LWSP (Linear White Space) detection
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

        private readonly string _mboxFilePath;
        private readonly FileStream _mboxStream;
        private readonly CryptoStream _cryptoStream;
        private readonly HashAlgorithm _cryptoHashAlg;

        public const string HASH_DEFAULT = "SHA256";

        private string Eol = EOL_TYPE_UNK;
        private byte[] MessageHash = new byte[0];
        private Dictionary<string, int> EolCounts = new Dictionary<string, int>();


        public MBoxProcessorSettings Settings { get; } 
        

        public MboxProcessor(ILogger<MboxProcessor> logger, string mboxFilePath, MBoxProcessorSettings settings)
        {
            Settings = settings;
            
            if (string.IsNullOrWhiteSpace(mboxFilePath))
            {
                throw new ArgumentNullException(nameof(mboxFilePath));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
            _logger.LogInformation("MboxProcessor Created");

            _mboxFilePath = Path.GetFullPath(mboxFilePath);
            _mboxStream = new FileStream(_mboxFilePath, FileMode.Open, FileAccess.Read);
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
            _cryptoStream = new CryptoStream(_mboxStream, _cryptoHashAlg, CryptoStreamMode.Read);

        }

        public MboxProcessor(ILogger<MboxProcessor> logger, string mboxFilePath) : this(logger, mboxFilePath, new MBoxProcessorSettings()) {  }



        /// <summary>
        /// Convert the mbox file into an archival XML file
        /// </summary>
        /// <param name="outFolderPath">the path to the output folder; if blank, defaults to the same folder as the mbox file</param>
        /// <param name="accntId">Globally unique, permanent, absolute URI with no fragment conforming to the canonical form specified in RFC2396 as amended by RFC2732.</param>
        /// <param name="accntEmails">Comma-separated list of email addresses</param>
        /// <param name="saveBinaryExt">If true binary content and attachments will be saved external to the XML; otherwise, all content will be encoded into the XML file</param>
        /// <returns>the most recent localId number</returns>
        public long ConvertMbox2EAXS(ref string outFolderPath, string accntId, string accntEmails = "")
        {
            long localId = 0;


            if (string.IsNullOrWhiteSpace(accntId))
            {
                throw new Exception("acctId is a required parameter");
            }

            if (string.IsNullOrWhiteSpace(outFolderPath))
            {
                outFolderPath = Path.GetDirectoryName(_mboxFilePath) ?? "";
            }

            var outFilePath = Path.Combine(outFolderPath, Path.GetFileName(Path.ChangeExtension(_mboxFilePath, "xml")));
            var csvFilePath = Path.Combine(outFolderPath, Path.GetFileName(Path.ChangeExtension(_mboxFilePath, "csv")));

            var messageList = new List<MessageBrief>();


            _logger.LogInformation("Convert email file: '{0}' into XML file: '{1}'", _mboxFilePath, outFilePath);


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
            //TODO:  Need to find a web location for the new schema version
            xwriter.WriteAttributeString("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance", "eaxs_schema_v2.xsd");
            foreach (var addr in accntEmails.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                xwriter.WriteElementString("EmailAddress", XM_NS, addr);
            }
            xwriter.WriteElementString("GlobalId", XM_NS, accntId);

            xwriter.WriteStartElement("Folder", XM_NS);
            xwriter.WriteElementString("Name", XM_NS, Path.GetFileNameWithoutExtension(_mboxFilePath));

            var parser = new MimeParser(_cryptoStream, MimeFormat.Mbox);

            parser.MimeMessageEnd += Parser_MimeMessageEnd;

            while (!parser.IsEndOfStream)
            {
                MimeMessage? message = null;
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
                    message = new MimeMessage();
                }
                if (message != null)
                {
                    localId++;
                    var messageId = localId;

                    xwriter.WriteStartElement("Message", XM_NS);
                    if (errCnt == 0) //process the message
                    {
                        localId = ConvertMessageToEAXS(message, xwriter, localId, outFilePath, false);
                    }
                    else //log an error and create an incomplete message 
                    {
                        //TEST:  Will need to create a deliberately malformed mbox to test this
                        _logger.LogError(errMsg);
                        xwriter.WriteElementString("LocalId", XM_NS, localId.ToString());
                        xwriter.WriteStartElement("MessageId", XM_NS);
                        xwriter.WriteAttributeString("Supplied", "true");
                        xwriter.WriteString($"{accntId}-{localId}");
                        xwriter.WriteEndElement(); //MessageId
                        xwriter.WriteStartElement("Incomplete", XM_NS);
                        xwriter.WriteElementString("ErrorType", XM_NS, errMsg);
                        xwriter.WriteElementString("ErrorLocation", XM_NS, $"Stream Position: {parser.Position}");
                        xwriter.WriteEndElement(); //Incomplete
                        xwriter.WriteElementString("Eol", XM_NS, Eol); //This might be unknown at this point if there was an error
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
                        Hash = Convert.ToHexString(MessageHash, 0, MessageHash.Length),
                        Errors = errCnt,
                        FirstErrorMessage = errMsg

                    }); ;
                }
                Eol = EOL_TYPE_UNK;
            }

            //make sure to read to the end of the stream so the hash is correct
            int i = -1;
            do
            {
                i = _cryptoStream.ReadByte();
            } while (i != -1);

            xwriter.WriteStartElement("Mbox", XM_NS);
            var relPath = Path.GetRelativePath(Path.GetDirectoryName(outFilePath) ?? "", _mboxFilePath);
            xwriter.WriteElementString("RelPath", XM_NS, relPath);
            xwriter.WriteElementString("Eol", XM_NS, MostCommonEol);
            if (UsesDifferentEols)
            {
                var warn = $"Mbox file contains multiple different EOLs: CR: {EolCounts["CR"]}, LF: {EolCounts["LF"]}, CRLF: {EolCounts["CRLF"]}";
                _logger.LogWarning(warn);
                xwriter.WriteComment($"WARNING: {warn}");
            }
            if (_cryptoHashAlg.Hash != null)
            {
                xwriter.WriteStartElement("Hash", XM_NS);
                xwriter.WriteStartElement("Value", XM_NS);
                xwriter.WriteBinHex(_cryptoHashAlg.Hash, 0, _cryptoHashAlg.Hash.Length);
                xwriter.WriteEndElement(); //Value
                xwriter.WriteElementString("Function", XM_NS, Settings.HashAlgorithmName);
                xwriter.WriteEndElement(); //Hash
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

        private long ConvertMessageToEAXS(MimeMessage message, XmlWriter xwriter, long localId, string outFilePath, bool isChildMessage)
        {

            _logger.LogInformation("Converting Message {0} Subject: {1}", localId, message.Subject);

            if (!isChildMessage)
            {
                xwriter.WriteElementString("RelPath", XM_NS, EXT_CONTENT_DIR);
            }

            xwriter.WriteStartElement("LocalId", XM_NS);
            xwriter.WriteValue(localId);
            xwriter.WriteEndElement();

            xwriter.WriteStartElement("MessageId", XM_NS);
            xwriter.WriteString(message.MessageId);
            xwriter.WriteEndElement();

            if (message.MimeVersion != null)
            {
                xwriter.WriteStartElement("MimeVersion", XM_NS);
                xwriter.WriteString(message.MimeVersion.ToString());
                xwriter.WriteEndElement();
            }

            xwriter.WriteStartElement("OrigDate", XM_NS);
            xwriter.WriteValue(message.Date);
            xwriter.WriteEndElement();

            foreach (var addr in message.From)
            {
                xwriter.WriteStartElement("From", XM_NS);
                xwriter.WriteString(addr.ToString());
                xwriter.WriteEndElement();
            }

            if (message.Sender != null)
            {
                xwriter.WriteStartElement("Sender", XM_NS);
                xwriter.WriteValue(message.Sender.ToString());
                xwriter.WriteEndElement();
            }

            foreach (var addr in message.To)
            {
                xwriter.WriteStartElement("To", XM_NS);
                xwriter.WriteValue(addr.ToString());
                xwriter.WriteEndElement();
            }

            foreach (var addr in message.Cc)
            {
                xwriter.WriteStartElement("Cc", XM_NS);
                xwriter.WriteValue(addr.ToString());
                xwriter.WriteEndElement();
            }

            foreach (var addr in message.Bcc)
            {
                xwriter.WriteStartElement("Bcc", XM_NS);
                xwriter.WriteValue(addr.ToString());
                xwriter.WriteEndElement();
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

            foreach (var hdr in message.Headers) //All headers even if already covered above
            {
                xwriter.WriteStartElement("Header", XM_NS);
                xwriter.WriteElementString("Name", XM_NS, hdr.Field);
                xwriter.WriteElementString("Value", XM_NS, hdr.Value);
                //TODO: Comments, not currently supported by MimeKit
                xwriter.WriteEndElement();
            }

            if (!isChildMessage)
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
                if (TryGetDraft(message, out status))
                {
                    xwriter.WriteElementString("StatusFlag", XM_NS, status);
                }
                if (TryGetRecent(message, out status))
                {
                    xwriter.WriteElementString("StatusFlag", XM_NS, status);
                }
            }

            localId = ConvertBody2EAXS(message.Body, xwriter, localId, outFilePath);

            //TODO: Incomplete; Seems related to errors

            if (!isChildMessage)
            {
                xwriter.WriteElementString("Eol", XM_NS, Eol);

                xwriter.WriteStartElement("Hash", XM_NS);
                xwriter.WriteStartElement("Value", XM_NS);
                xwriter.WriteBinHex(MessageHash, 0, MessageHash.Length);
                xwriter.WriteEndElement();//Value
                xwriter.WriteElementString("Function", XM_NS, Settings.HashAlgorithmName);
                xwriter.WriteEndElement();//Hash
            }

            return localId;
        }

        private long ConvertBody2EAXS(MimeEntity mimeEntity, XmlWriter xwriter, long localId, string outFilePath)
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

            if (isMultipart && !string.IsNullOrWhiteSpace(part?.ContentDescription))
            {
                xwriter.WriteElementString("Description", XM_NS, part.ContentDescription);
            }

            //TODO: DescriptionComments, not currently supported by MimeKit, actually might not be allowed by the RFC since Description is not a structured header type

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

            except = new string[] { "content-type", "content-transfer-encoding", "content-id", "content-description", "content-disposition" };
            foreach (var hdr in mimeEntity.Headers.Where(h => !except.Contains(h.Field, StringComparer.InvariantCultureIgnoreCase)))
            {
                xwriter.WriteStartElement("OtherMimeHeader", XM_NS);
                xwriter.WriteElementString("Name", XM_NS, hdr.Field);
                xwriter.WriteElementString("Value", XM_NS, hdr.Value);
                //TODO: Comments, not currently supported by MimeKit
                xwriter.WriteEndElement(); //OtherMimeHeaders
            }

            if (isMultipart && multipart != null && !string.IsNullOrWhiteSpace(multipart.Preamble))
            {
                xwriter.WriteElementString("Preamble", XM_NS, multipart.Preamble.Trim());
            }

            if (isMultipart && multipart != null && multipart.Count > 0)
            {
                foreach (var item in multipart)
                {
                    localId = ConvertBody2EAXS(item, xwriter, localId, outFilePath);
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

                    var content = part.Content;

                    //if it is text and not an attachment, save embedded in the XML
                    if (part.ContentType.IsMimeType("text","*") && !part.IsAttachment)
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
                            SerializeContentInXml(part, xwriter,false);
                        }
                        else
                        {
                            //save non-text content or attachments externally, wrapped in XML, and converted to base64
                            SerializeContentInExtFile(part, xwriter, outFilePath, localId);
                        }
                    }

                }
                else if (message != null)
                {
                    xwriter.WriteStartElement("ChildMessage", XM_NS);
                    localId++;
                    ConvertMessageToEAXS(message.Message, xwriter, localId, outFilePath, true);
                    xwriter.WriteEndElement(); //ChildMessage
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
            //TEST: Need to write a test for this
            if(!isMultipart && part!=null && part.ContentType.IsMimeType("message", "external-body"))
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

        /// <summary>
        /// Serialize the mime part as a string in the XML 
        /// </summary>
        /// <param name="part">the MIME part to serialize</param>
        /// <param name="xwriter">the XML writer to serialize it to</param>
        /// <param name="preserveEncodingIfPossible">if true write text as unicode and use the original content encoding if possible; if false write it as either unicode text or as base64 encoded binary</param>
        private void SerializeContentInXml(MimePart part, XmlWriter xwriter, bool ExtContent)
        {
            //TEST: Need a test of preserveEncodingIfPossible true and false
            var content = part.Content;

            xwriter.WriteStartElement("BodyContent", XM_NS);
            if(ExtContent)
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
            else if(Settings.PreserveContentTransferEncodingIfPossible && (content.Encoding == ContentEncoding.UUEncode || content.Encoding==ContentEncoding.QuotedPrintable || content.Encoding==ContentEncoding.Base64))
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
            //TODO: Add a parameter to wrap the content in XML, instead of just saving it as the original decoded file
            //probably use the SerializeContentInXml function with a different xmlWiter to do this
            
            var content = part.Content;

            //get random file name that doesn't already exist
            string randomFilePath = "";
            do
            {
                randomFilePath = Path.Combine(Path.GetDirectoryName(outFilePath) ?? "", Path.GetRandomFileName());
            } while (File.Exists(randomFilePath));

            //TODO: IO Exception Handling

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
                SerializeContentInXml(part, extXmlWriter,true);
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
            xwriter.WriteStartElement("Hash", XM_NS);
            xwriter.WriteStartElement("Value", XM_NS);
            xwriter.WriteBinHex(cryptoHashAlg.Hash, 0, cryptoHashAlg.Hash.Length);
            xwriter.WriteEndElement(); //Value
            xwriter.WriteElementString("Function", XM_NS, Settings.HashAlgorithmName);
            xwriter.WriteEndElement(); //Hash
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

        private bool TryGetDraft(MimeMessage message, out string status)
        {
            //TODO: Need to determine how other email clients might encode these values
            
            var ret = false;
            status = "";

            //If it is a Mozilla message that came from a file called 'Draft' then assume it is a 'draft' message
            //There is also the X-Mozilla-Draft-Info header which could indicate whether a message is draft
            if (
                (Path.GetFileNameWithoutExtension(_mboxFilePath).Equals("Drafts", StringComparison.OrdinalIgnoreCase) && message.Headers.Contains("X-Mozilla-Status"))
                || message.Headers.Contains("X-Mozilla-Draft-Info")
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


        private void Parser_MimeMessageEnd(object? sender, MimeMessageEndEventArgs e)
        {
            var parser = sender as MimeParser;
            var endOffset = e.EndOffset;
            var beginOffset = e.BeginOffset;
            var headersEndOffset = e.HeadersEndOffset;
            long mboxMarkerOffset = 0;
            if (parser != null)
                mboxMarkerOffset = parser.MboxMarkerOffset;
            else
            {
                mboxMarkerOffset = beginOffset; //use the start of the message, instead of the start of the Mbox marker
                _logger.LogWarning("Unable to determine the start of the Mbox marker");
            }

            // get the raw data from the stream to calculate eol and hash for the xml
            byte[] buffer = new byte[endOffset - mboxMarkerOffset];
            var origPos = _mboxStream.Position;
            _mboxStream.Seek(mboxMarkerOffset, SeekOrigin.Begin);
            _mboxStream.Read(buffer, 0, buffer.Length);
            _mboxStream.Position = origPos;

            //Look for first EOL marker to determine which kind are being used.
            //Assume the same kind will be used throughout
            long i = 1;
            while (i < buffer.Length - 1)
            {
                if (buffer[i] == LF && buffer[i - 1] == CR)
                {
                    Eol = EOL_TYPE_CRLF;
                    break;
                }
                else if (buffer[i] == LF)
                {
                    Eol = EOL_TYPE_LF;
                    break;
                }
                else if (buffer[i] == CR && buffer[i + 1] != LF)
                {
                    Eol = EOL_TYPE_CR;
                    break;
                }
                i++;
            }

            //Check that messages use the same EOL treatment throughout
            if (Eol != EOL_TYPE_UNK)
            {
                if (EolCounts.ContainsKey(Eol))
                {
                    EolCounts[Eol]++;
                }
                else
                {
                    EolCounts.Add(Eol, 1);
                }
            }

            //trim all LWSP and EOL chars from the end and then add one eol marker back
            i = buffer.Length - 1;
            while (buffer[i] == LF || buffer[i] == CR || buffer[i] == SP || buffer[i] == TAB)
                --i;
            long j = 1;
            if (Eol == EOL_TYPE_CRLF)
                j = 2;
            byte[] newBuffer = new byte[i + 1 + j];

            Array.Copy(buffer, 0, newBuffer, 0, i + 1);
            if (Eol == EOL_TYPE_CRLF)
            {
                newBuffer[newBuffer.Length - 1] = LF;
                newBuffer[newBuffer.Length - 2] = CR;
            }
            else if (Eol == EOL_TYPE_LF)
            {
                newBuffer[newBuffer.Length - 1] = LF;
            }
            else if (Eol == EOL_TYPE_CR)
            {
                newBuffer[newBuffer.Length - 1] = CR;
            }
            else
            {
                _logger.LogError("Unable to determine EOL marker");
                throw new Exception("Unable to determine EOL marker");
            }

            var hashAlg = HashAlgorithm.Create(Settings.HashAlgorithmName) ?? SHA256.Create(); //Fallback to known hash algorithm
            MessageHash = hashAlg.ComputeHash(newBuffer);

        }

        private bool UsesDifferentEols
        {
            get
            {
                return EolCounts.Count > 1;
            }
        }

        private string MostCommonEol
        {
            get
            {
                var ret = "";
                var max = 0;
                foreach (var kvp in EolCounts)
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


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    _cryptoStream.Close();
                    _mboxStream.Close();
                    _cryptoStream.Dispose();
                    _mboxStream.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MboxProcessor()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private string GetContentEncodingString(ContentEncoding enc)
        {
            switch (enc)
            {
                case ContentEncoding.Default:
                    return "";
                case ContentEncoding.SevenBit:
                    return "7bit";
                case ContentEncoding.EightBit:
                    return "8bit";
                case ContentEncoding.Binary:
                    return "binary";
                case ContentEncoding.Base64:
                    return "base64";
                case ContentEncoding.QuotedPrintable:
                    return "quoted-printable";
                case ContentEncoding.UUEncode:
                    return "uuencode";
                default:
                    return "";
            }
        }
    }
}