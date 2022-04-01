using Microsoft.Extensions.Logging;
using MimeKit;
using System.Globalization;
using System.Xml;

namespace Email2Pdf
{
    public class MboxProcessor : IDisposable
    {
        public const string EOL_TYPE_CR = "CR";
        public const string EOL_TYPE_LF = "LF";
        public const string EOL_TYPE_CRLF = "CRLF";
        public const string EOL_TYPE_UNK = "UNKNOWN";

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

        public const string XM = "xm";
        public const string XM_NS = "https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs";

        private readonly ILogger _logger;

        private readonly FileStream _mboxStream;

        private readonly string _mboxFilePath;
        private bool disposedValue;

        private string Eol = EOL_TYPE_UNK;
        private byte[] Sha1Hash;

        public MboxProcessor(ILogger<MboxProcessor> logger, string mboxFilePath)
        {
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

            _mboxFilePath = mboxFilePath;
            _mboxStream = new FileStream(_mboxFilePath, FileMode.Open, FileAccess.Read);

            //calculate sha1 hash for whole file and also determine eol for whole file
            int c = -1;
            long i = 0;
            do {
                c = _mboxStream.ReadByte();
                i++;
            } while (c !=-1);

            _mboxStream.Position = 0;

        }

        public int ConvertMbox2EAPDF(ref string outFilePath)
        {
            int ret = 0;

            if (string.IsNullOrWhiteSpace(outFilePath))
            {
                outFilePath = Path.ChangeExtension(_mboxFilePath, "pdf");
            }

            _logger.LogInformation("Convert email file: '{0}' into PDF file: '{1}'", _mboxFilePath, outFilePath);

            var parser = new MimeParser(_mboxStream, MimeFormat.Mbox);
            while (!parser.IsEndOfStream)
            {
                var message = parser.ParseMessage();
                if (message != null)
                {
                    ret++;
                    _logger.LogInformation("Message {0} Parsed\r\nSubject: {1}", ret, message.Subject);
                }
            }

            _logger.LogInformation("Parsed {0} messages", ret);

            return ret;
        }

        public int ConvertMbox2EAXS(ref string outFilePath, string accntId, string accntEmails = "")
        {
            int ret = 0;


            if (string.IsNullOrWhiteSpace(accntId))
            {
                throw new Exception("acctId is a required parameter");
            }

            if (string.IsNullOrWhiteSpace(outFilePath))
            {
                outFilePath = Path.ChangeExtension(_mboxFilePath, "xml");
            }

            _logger.LogInformation("Convert email file: '{0}' into XML file: '{1}'", _mboxFilePath, outFilePath);


            var xset = new XmlWriterSettings()
            {
                CloseOutput = true,
                Indent = true
            };


            using var xwriter = XmlWriter.Create(outFilePath, xset);
            xwriter.WriteStartDocument();
            xwriter.WriteStartElement("Account", XM_NS);
            xwriter.WriteAttributeString("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance", "https://raw.githubusercontent.com/StateArchivesOfNorthCarolina/tomes-eaxs/master/versions/1/eaxs_schema_v1.xsd");
            foreach (var addr in accntEmails.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                xwriter.WriteElementString("EmailAddress", XM_NS, addr);
            }
            xwriter.WriteElementString( "GlobalId", XM_NS, accntId);

            xwriter.WriteStartElement( "Folder", XM_NS);
            xwriter.WriteElementString( "Name", XM_NS, Path.GetFileNameWithoutExtension(_mboxFilePath));

            var parser = new MimeParser(_mboxStream, MimeFormat.Mbox);

            //parser.MimeMessageBegin += Parser_MimeMessageBegin;
            parser.MimeMessageEnd += Parser_MimeMessageEnd;
            //parser.MimeEntityBegin += Parser_MimeEntityBegin;
            //parser.MimeEntityEnd += Parser_MimeEntityEnd;

            while (!parser.IsEndOfStream)
            {
                var message = parser.ParseMessage();
                if (message != null)
                {
                    ret++;

                    xwriter.WriteStartElement("Message", XM_NS);
                    ConvertMessageToEAXS(message, xwriter, ret);
                    xwriter.WriteEndElement(); //Message

                }
                Eol = EOL_TYPE_UNK;
            }


            xwriter.WriteEndElement(); //Folder
            xwriter.WriteEndElement(); //Account

            xwriter.WriteEndDocument();

            _logger.LogInformation("Converted {0} messages", ret);

            return ret;
        }

        private void ConvertMessageToEAXS(MimeMessage message, XmlWriter xwriter, long cnt, bool isChildMessage=false)
        {
            _logger.LogInformation("Converting Message {0} Subject: {1}", cnt, message.Subject);

            //TODO: RelPath 

            xwriter.WriteStartElement( "LocalId", XM_NS);
            xwriter.WriteValue(cnt);
            xwriter.WriteEndElement();

            xwriter.WriteStartElement( "MessageId", XM_NS);
            xwriter.WriteString(message.MessageId);
            xwriter.WriteEndElement();

            if (message.MimeVersion != null)
            {
                xwriter.WriteStartElement( "MimeVersion", XM_NS);
                xwriter.WriteString(message.MimeVersion.ToString());
                xwriter.WriteEndElement();
            }

            xwriter.WriteStartElement( "OrigDate", XM_NS);
            xwriter.WriteValue(message.Date);
            xwriter.WriteEndElement();

            foreach (var addr in message.From)
            {
                xwriter.WriteStartElement( "From", XM_NS);
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
                xwriter.WriteStartElement( "To", XM_NS);
                xwriter.WriteValue(addr.ToString());
                xwriter.WriteEndElement();
            }

            foreach (var addr in message.Cc)
            {
                xwriter.WriteStartElement( "Cc", XM_NS);
                xwriter.WriteValue(addr.ToString());
                xwriter.WriteEndElement();
            }

            foreach (var addr in message.Bcc)
            {
                xwriter.WriteStartElement( "Bcc", XM_NS);
                xwriter.WriteValue(addr.ToString());
                xwriter.WriteEndElement();
            }

            if (!string.IsNullOrWhiteSpace(message.InReplyTo))
            {
                xwriter.WriteElementString( "InReplyTo", XM_NS, message.InReplyTo);
            }

            foreach (var id in message.References)
            {
                xwriter.WriteElementString( "References", XM_NS, id);
            }

            if (!string.IsNullOrWhiteSpace(message.Subject))
            {
                xwriter.WriteElementString( "Subject", XM_NS, message.Subject);
            }

            //TODO: Comments

            //TODO:  Keywords

            foreach (var hdr in message.Headers) //TODO: Might need to use the raw values here; the schema docs indicate to use the minumum transformations
            {
                xwriter.WriteStartElement( "Header", XM_NS);
                xwriter.WriteElementString( "Name", XM_NS, hdr.Field);
                xwriter.WriteElementString( "Value", XM_NS, hdr.Value);
                //TODO: Comments
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

            ConvertBody2EAXS(message.Body, xwriter);

            //TODO: Incomplete

            if (!isChildMessage)
            {
                xwriter.WriteElementString("Eol", XM_NS, Eol);

                xwriter.WriteStartElement("Hash", XM_NS);
                xwriter.WriteStartElement("Value", XM_NS);
                xwriter.WriteBinHex(Sha1Hash, 0, Sha1Hash.Length);
                xwriter.WriteEndElement();//Value
                xwriter.WriteElementString("Function", XM_NS, "SHA1");
                xwriter.WriteEndElement();//Hash
            }

        }

        private void ConvertBody2EAXS(MimeEntity mimeEntity, XmlWriter xwriter)
        {
            bool isMultipart = false;

            MimePart? part = mimeEntity as MimePart;
            Multipart? multipart = mimeEntity as Multipart;
            MessagePart? message = mimeEntity as MessagePart;

            if (mimeEntity is Multipart)
            {
                isMultipart = true;
                xwriter.WriteStartElement( "MultiBody", XM_NS);
            }
            else if (mimeEntity is MimePart)
            {
                xwriter.WriteStartElement( "SingleBody", XM_NS);
            }
            else if (mimeEntity is MessagePart)
            {
                xwriter.WriteStartElement( "SingleBody", XM_NS);
            }
            else
            {
                string warn = $"Unexpected MIME Entity Type: '{mimeEntity.GetType().FullName}' -- '{mimeEntity.ContentType.MimeType}'";
                _logger.LogWarning(warn);
                xwriter.WriteComment(warn);
                xwriter.WriteStartElement("SingleBody", XM_NS);
            }

            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentType.MimeType))
            {
                xwriter.WriteElementString( "ContentType", XM_NS, mimeEntity.ContentType.MimeType);
            }
            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentType.Charset))
            {
                xwriter.WriteElementString( "Charset", XM_NS, mimeEntity.ContentType.Charset);
            }
            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentType.Name))
            {
                xwriter.WriteElementString( "ContentName", XM_NS, mimeEntity.ContentType.Name);
            }
            if (isMultipart && !string.IsNullOrWhiteSpace(mimeEntity.ContentType.Boundary))
            {
                xwriter.WriteElementString( "BoundaryString", XM_NS, mimeEntity.ContentType.Boundary);
            }
            else if (!isMultipart && !string.IsNullOrWhiteSpace(mimeEntity.ContentType.Boundary))
            {
                string warn = $"MIME type boundary parameter '{mimeEntity.ContentType.Boundary}' found for a non-multipart mime type";
                _logger.LogWarning(warn);
                xwriter.WriteComment(warn);

            }
            else if (isMultipart && string.IsNullOrWhiteSpace(mimeEntity.ContentType.Boundary))
            {
                string warn = "MIME type boundary parameter is missing for a multipart mime type";
                _logger.LogWarning(warn);
                xwriter.WriteComment(warn);
            }

            //TODO: ContentTypeComments

            string[] except = { "id", "name" };
            foreach (var param in mimeEntity.ContentType.Parameters.Where(p => !except.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase))) //Except id and name, case-insensitive; TODO: Why exclude id and name?
            {
                xwriter.WriteStartElement( "ContentTypeParam", XM_NS);
                xwriter.WriteElementString( "Name", XM_NS, param.Name);
                xwriter.WriteElementString( "Value", XM_NS, param.Value);
                xwriter.WriteEndElement(); //ContentTypeParam
            }
            //TODO: Content-Transfer-Encoding is only applicable to single body messages, so not sure why it is allowed in MultiBody by the XML Schema
            if (part != null && part.ContentTransferEncoding != ContentEncoding.Default)
            {
                xwriter.WriteElementString( "TransferEncoding", XM_NS, GetContentEncodingString(part.ContentTransferEncoding));
            }

            //TODO: TransferEncodingComments

            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentId))
            {
                xwriter.WriteElementString( "ContentId", XM_NS, mimeEntity.ContentId);
            }

            //TODO: ContentIdComments

            if (isMultipart && !string.IsNullOrWhiteSpace(part?.ContentDescription))
            {
                xwriter.WriteElementString( "Description", XM_NS, part.ContentDescription);
            }

            //TODO: DescriptionComments

            if (mimeEntity.ContentDisposition != null)
            {
                if (isMultipart && !string.IsNullOrWhiteSpace(mimeEntity.ContentDisposition.Disposition)) //only used for multipart.  TODO:  Why is this?
                {
                    xwriter.WriteElementString( "Disposition", XM_NS, mimeEntity.ContentDisposition.Disposition);
                }
                else if (!isMultipart && !string.IsNullOrWhiteSpace(mimeEntity.ContentDisposition.Disposition))
                {
                    string warn = $"Single bodied MIME type has disposition: {mimeEntity.ContentDisposition.Disposition}";
                    _logger.LogInformation(warn);
                    xwriter.WriteComment(warn);
                }
                if (!string.IsNullOrWhiteSpace(mimeEntity.ContentDisposition.FileName))
                {
                    xwriter.WriteElementString( "DispositionFileName", XM_NS, mimeEntity.ContentDisposition.FileName);
                }

                //TODO: DispositionComments

                foreach (var param in mimeEntity.ContentDisposition.Parameters.Where(p => !p.Name.Equals("filename", StringComparison.InvariantCultureIgnoreCase)))
                {
                    //This is because of a likely typo in the XML Schema
                    if (isMultipart)
                    {
                        xwriter.WriteStartElement("DispositionParams", XM_NS);
                    }
                    else
                    {
                        xwriter.WriteStartElement("DispositionParam", XM_NS);
                    }
                    xwriter.WriteElementString( "Name", XM_NS, param.Name);
                    xwriter.WriteElementString( "Value", XM_NS, param.Value);
                    xwriter.WriteEndElement(); //DispositionParam(s)
                }
            }

            except =  new string[] {"content-type","content-transfer-encoding","content-id","content-description","content-disposition"};
            foreach (var hdr in mimeEntity.Headers.Where(h=>!except.Contains(h.Field,StringComparer.InvariantCultureIgnoreCase)))
            {
                xwriter.WriteStartElement( "OtherMimeHeader", XM_NS);
                xwriter.WriteElementString( "Name", XM_NS, hdr.Field);
                xwriter.WriteElementString( "Value", XM_NS, hdr.Value);
                //TODO: Comments
                xwriter.WriteEndElement(); //OtherMimeHeaders
            }

            if(isMultipart && multipart!=null && !string.IsNullOrWhiteSpace(multipart.Preamble))
            {
                xwriter.WriteElementString( "Preamble", XM_NS, multipart.Preamble);
            }

            if (isMultipart && multipart != null && multipart.Count>0)
            {
                foreach(var item in multipart)
                {
                    ConvertBody2EAXS(item, xwriter);
                }
            }
            else if(isMultipart && multipart != null && multipart.Count == 0)
            {
                var warning = $"Item is multipart, but there are no parts";
                _logger.LogWarning(warning);
                xwriter.WriteComment(warning);
            }
            else if(isMultipart && multipart == null)
            {
                var warning = $"Item is erroneously flagged as multipart";
                _logger.LogWarning(warning);
                xwriter.WriteComment(warning);
            }
            else
            {
                if (part != null)
                {
                    xwriter.WriteStartElement( "BodyContent", XM_NS);

                    var content = part.Content;
  
                    if (part.ContentType.MediaType.Equals("text", StringComparison.InvariantCultureIgnoreCase))
                    {
                        xwriter.WriteStartElement("Content", XM_NS);
                        Stream decoded = content.Open();
                        //decode the stream and treat it as whatever the charset advertised in the content-type header
                        StreamReader reader = new StreamReader(decoded,part.ContentType.CharsetEncoding,true);
                        xwriter.WriteString(reader.ReadToEnd());
                        xwriter.WriteEndElement(); //Content
                    }
                    else
                    {
                        xwriter.WriteStartElement("Content", XM_NS);
                        //treat the stream as ASCII
                        StreamReader reader = new StreamReader(content.Stream,System.Text.Encoding.ASCII);
                        xwriter.WriteCData(reader.ReadToEnd());
                        xwriter.WriteEndElement(); //Content
                        xwriter.WriteElementString( "TransferEncoding", XM_NS, GetContentEncodingString(content.Encoding));
                    }

                    xwriter.WriteEndElement(); //BodyContent
                    //TODO: Do we need to support ExtBodyContent?
                }
                else if(message != null)
                {
                    xwriter.WriteStartElement( "ChildMessage", XM_NS);
                    long cnt = 1; //TODO: Not sure what to use for child messages
                    ConvertMessageToEAXS(message.Message, xwriter, cnt, true);
                    xwriter.WriteEndElement(); //ChildMessage
                }
                else
                {
                    string warn = $"Unexpected MimeEntity: {mimeEntity.GetType().FullName}";
                    _logger.LogWarning(warn);
                    xwriter.WriteComment(warn);
                }
            }

            //TODO: PhantomBody

            if (isMultipart && multipart != null && !string.IsNullOrWhiteSpace(multipart.Epilogue))
            {
                xwriter.WriteElementString( "Epilogue", XM_NS, multipart.Epilogue);
            }


            xwriter.WriteEndElement(); //SingleBody or MultiBody

        }


        private bool TryGetSeen(MimeMessage message, out string status)
        {
            //TODO: Need to determine how other email clients might encode these values; look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //So far just using x-mozilla-status and x-mozilla-status2 headers for this
            var ret = false;
            status = "";

            XMozillaStatusFlags xMozillaStatus = GetXMozillaStatus(message);

            if (xMozillaStatus.HasFlag(XMozillaStatusFlags.MSG_FLAG_READ))
            {
                ret = true;
                status = "Seen";
            }

            return ret;
        }

        private bool TryGetAnswered(MimeMessage message, out string status)
        {
            //TODO: Need to determine how other email clients might encode these values; look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //So far just using x-mozilla-status and x-mozilla-status2 headers for this
            var ret = false;
            status = "";

            XMozillaStatusFlags xMozillaStatus = GetXMozillaStatus(message);

            if (xMozillaStatus.HasFlag(XMozillaStatusFlags.MSG_FLAG_REPLIED))
            {
                ret = true;
                status = "Answered";
            }

            return ret;
        }

        private bool TryGetFlagged(MimeMessage message, out string status)
        {
            //TODO: Need to determine how other email clients might encode these values; look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //So far just using x-mozilla-status and x-mozilla-status2 headers for this
            var ret = false;
            status = "";

            XMozillaStatusFlags xMozillaStatus = GetXMozillaStatus(message);

            if (xMozillaStatus.HasFlag(XMozillaStatusFlags.MSG_FLAG_MARKED))
            {
                ret = true;
                status = "Flagged";
            }

            return ret;
        }

        private bool TryGetDeleted(MimeMessage message, out string status)
        {
            //TODO: Need to determine how other email clients might encode these values; look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //So far just using x-mozilla-status and x-mozilla-status2 headers for this
            var ret = false;
            status = "";

            XMozillaStatusFlags xMozillaStatus = GetXMozillaStatus(message);
            XMozillaStatusFlags2 xMozillaStatus2 = GetXMozillaStatus2(message);

            //For Mozilla: waiting to be expunged by the client or marked as deleted on the IMAP Server
            if (xMozillaStatus.HasFlag(XMozillaStatusFlags.MSG_FLAG_EXPUNGED) || xMozillaStatus2.HasFlag(XMozillaStatusFlags2.MSG_FLAG_IMAP_DELETED))
            {
                ret = true;
                status = "Deleted";
            }

            return ret;
        }

        private bool TryGetDraft(MimeMessage message, out string status)
        {
            //TODO: Need to determine how other email clients might encode these values
            var ret = false;
            status = "";

            //For Mozilla: I think the way to get this for Mozilla is to determine if the message came from a file called 'Draft'
            //TODO: There is also the X-Mozilla-Draft-Info header which could indicate whether a message is draft
            if (Path.GetFileNameWithoutExtension(_mboxFilePath).Equals("Drafts", StringComparison.OrdinalIgnoreCase) && message.Headers.Contains("X-Mozilla-Status"))
            {
                ret = true;
                status = "Draft";
            }

            return ret;
        }

        private bool TryGetRecent(MimeMessage message, out string status)
        {
            //TODO: Need to determine how other email clients might encode these values; look at the Status and X-Status headers, https://docs.python.org/3/library/mailbox.html#mboxmessage
            //So far just using x-mozilla-status and x-mozilla-status2 headers for this
            var ret = false;
            status = "";

            XMozillaStatusFlags xMozillaStatus = GetXMozillaStatus(message);
            XMozillaStatusFlags2 xMozillaStatus2 = GetXMozillaStatus2(message);

            //For Mozilla: There are no XMozillaStatusFlags or the XMozillaStatusFlags2 is set to NEW
            if (xMozillaStatus == XMozillaStatusFlags.MSG_FLAG_NULL || xMozillaStatus2.HasFlag(XMozillaStatusFlags2.MSG_FLAG_NEW))
            {
                ret = true;
                status = "Recent";
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
            var mboxMarkerOffset = parser.MboxMarkerOffset;

            // get the raw data from the stream to calculate eol and hash for the xml
            byte[] buffer = new byte[endOffset - mboxMarkerOffset];
            var origPos = _mboxStream.Position;
            _mboxStream.Seek(mboxMarkerOffset, SeekOrigin.Begin);
            _mboxStream.Read(buffer, 0, buffer.Length);
            _mboxStream.Position = origPos;

            //Look for first EOL marker to determine which kind are being used.
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

            var sha1 = System.Security.Cryptography.SHA1.Create();
            Sha1Hash = sha1.ComputeHash(newBuffer);

            //convert buffer to string
            //var bufStr = parser.Options.CharsetEncoding.GetString(buffer);
            //var newBufStr = parser.Options.CharsetEncoding.GetString(newBuffer);


        }

        //private void Parser_MimeMessageBegin(object? sender, MimeMessageBeginEventArgs e)
        //{
        //}

        //private void Parser_MimeEntityEnd(object? sender, MimeEntityEndEventArgs e)
        //{
        //}

        //private void Parser_MimeEntityBegin(object? sender, MimeEntityBeginEventArgs e)
        //{
        //}

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    _mboxStream.Close();
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