using Microsoft.Extensions.Logging;
using MimeKit;
using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using Wiry.Base32;
using CsvHelper;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Asn1.X509.Qualified;
using MimeKit.Utils;

namespace UIUCLibrary.EaPdf
{
    public class EmailProcessor
    {

        //TODO: Need to add some IO Exception Handling throughout for creating, reading, and writing to files and folders.

        //TODO: Add support for mbx files, see https://uofi.box.com/s/51v7xzfzqod2dv9lxmjgbrrgz5ejjydk 

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

        //Constants for the Status and X-Status header values, see https://docs.python.org/3/library/mailbox.html#mboxmessage
        const char STATUS_FLAG_READ = 'R';
        const char STATUS_FLAG_OLD = 'O';
        const char STATUS_FLAG_DELETED = 'D';
        const char STATUS_FLAG_FLAGGED = 'F';
        const char STATUS_FLAG_ANSWERED = 'A';
        const char STATUS_FLAG_DRAFT = 'T';

        public const string XM = "xm";
        public const string XM_NS = "https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2";
        public const string XM_XSD = "eaxs_schema_v2.xsd";

        private readonly ILogger _logger;

        public const string HASH_DEFAULT = "SHA256";

        const string EX_MBOX_FROM_MARKER = "Failed to find mbox From marker";
        const string EX_MBOX_PARSE_HEADERS = "Failed to parse message headers";

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

        }

        /// <summary>
        /// Convert a folder of mbox files into an archival emal XML file
        /// </summary>
        /// <param name="mboxFolderPath">the path to the folder to process, all mbox files in the folder will be processed</param>
        /// <param name="outFolderPath">the path to the output folder; if blank, defaults to the same folder as the mboxFolderPath</param>
        /// <param name="accntId">Globally unique, permanent, absolute URI with no fragment conforming to the canonical form specified in RFC2396 as amended by RFC2732.</param>
        /// <param name="accntEmails">Comma-separated list of email addresses</param>
        /// <param name="includeSubFolders">if true subfolders in the directory will also be processed</param>
        /// <returns>the most recent localId number which is usually the total number of messages processed</returns>
        public long ConvertFolderOfMbox2EAXS(string mboxFolderPath, ref string outFolderPath, string accntId, string accntEmails = "")
        {
            if (string.IsNullOrWhiteSpace(mboxFolderPath))
            {
                throw new ArgumentNullException(nameof(mboxFolderPath));
            }

            if (!Directory.Exists(mboxFolderPath))
            {
                throw new DirectoryNotFoundException(mboxFolderPath);
            }

            long localId = 0;

            //UNDONE

            return localId;
        }



        /// <summary>
        /// Convert one mbox file into an archival email XML file.
        /// </summary>
        /// <param name="mboxFilePath">the path to the mbox file to process</param>
        /// <param name="outFolderPath">the path to the output folder; if blank, defaults to the same folder as the mboxFilePath</param>
        /// <param name="accntId">Globally unique, permanent, absolute URI with no fragment conforming to the canonical form specified in RFC2396 as amended by RFC2732.</param>
        /// <param name="accntEmails">Comma-separated list of email addresses</param>
        /// <returns>the most recent localId number which is usually the total number of messages processed</returns>
        public long ConvertMbox2EAXS(string mboxFilePath, ref string outFolderPath, string accntId, string accntEmails = "")
        {
            //TODO: add code to process correspondingly named subdirectories as sub folders to a given mbox file.
            //TODO: add code to process directory and subdirectory or single email message EML files.

            if (string.IsNullOrWhiteSpace(mboxFilePath))
            {
                throw new ArgumentNullException(nameof(mboxFilePath));
            }

            if (!File.Exists(mboxFilePath))
            {
                throw new FileNotFoundException(mboxFilePath);
            }

            if (string.IsNullOrWhiteSpace(outFolderPath))
            {
                //default to the same folder as the mbox file
                outFolderPath = Path.GetDirectoryName(mboxFilePath) ?? "";
            }

            if (string.IsNullOrWhiteSpace(accntId))
            {
                throw new Exception("acctId is a required parameter");
            }

            mboxFilePath = Path.GetFullPath(mboxFilePath);

            long localId = 0;

            var outFilePath = Path.Combine(outFolderPath, Path.GetFileName(Path.ChangeExtension(mboxFilePath, "xml")));
            var csvFilePath = Path.Combine(outFolderPath, Path.GetFileName(Path.ChangeExtension(mboxFilePath, "csv")));

            var messageList = new List<MessageBrief>(); //used to create the CSV file

            _logger.LogInformation("Convert email file: '{mboxFilePath}' into XML file: '{outFilePath}'", mboxFilePath, outFilePath);

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
            xwriter.WriteProcessingInstruction("Settings", $"HashAlgorithmName: {Settings.HashAlgorithmName}, SaveAttachmentsAndBinaryContentExternally: {Settings.SaveAttachmentsAndBinaryContentExternally}, WrapExternalContentInXml: {Settings.WrapExternalContentInXml}, PreserveContentTransferEncodingIfPossible: {Settings.PreserveContentTransferEncodingIfPossible}, IncludeSubFolders: {Settings.IncludeSubFolders}");
            xwriter.WriteStartElement("Account", XM_NS);
            xwriter.WriteAttributeString("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance", "eaxs_schema_v2.xsd");
            foreach (var addr in accntEmails.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                xwriter.WriteElementString("EmailAddress", XM_NS, addr);
            }
            xwriter.WriteElementString("GlobalId", XM_NS, accntId);

            var mboxProps = new MboxProperties()
            {
                MboxFilePath = mboxFilePath,
                AccountId = accntId,
                OutFilePath = outFilePath,
            };
            SetHashAlgorithm(xwriter, mboxProps);

            localId = ProcessMbox(xwriter, localId, messageList, mboxProps);

            xwriter.WriteEndElement(); //Account

            xwriter.WriteEndDocument();

            //write the csv file
            using var csvStream = new StreamWriter(csvFilePath);
            using var csvWriter = new CsvWriter(csvStream, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(messageList);

            _logger.LogInformation("Converted {localId} messages", localId);

            return localId;
        }

        private long ProcessMbox(XmlWriter xwriter, long localId, List<MessageBrief> messageList, MboxProperties mboxProps)
        {
            //Keep track of properties for an individual messager, such as Eol and Hash
            MimeMessageProperties msgProps = new MimeMessageProperties();

            //open filestream and wrap it in a cryptostream so that we can hash the file as we process it
            using FileStream mboxStream = new FileStream(mboxProps.MboxFilePath, FileMode.Open, FileAccess.Read);
            using CryptoStream cryptoStream = new CryptoStream(mboxStream, mboxProps.HashAlgorithm, CryptoStreamMode.Read);


            var parser = new MimeParser(cryptoStream, MimeFormat.Mbox);

            parser.MimeMessageEnd += (sender, e) => Parser_MimeMessageEnd(sender, e, mboxStream, mboxProps, msgProps);

            long prevParserPos = 0;
            bool validMboxFile = false;
            MimeMessage? message = null;
            while (!parser.IsEndOfStream)
            {
                try
                {
                    message = parser.ParseMessage();
                }
                catch (FormatException fex1) when (fex1.Message.Contains(EX_MBOX_FROM_MARKER, StringComparison.OrdinalIgnoreCase))
                {
                    if (prevParserPos == 0)
                    {
                        WriteInfoMessage(xwriter, $"{fex1.Message} -- skipping file, probably not an mbox file");
                        return localId; //the file probably isn't an mbox file, so just bail on the whole file
                    }
                    else
                    {
                        WriteErrorMessage(xwriter, fex1.Message);
                        break; //don't try processing any more messages
                        //TODO: might still be salvageable records in the mbox if we can adjust the position and keep moving
                    }
                }
                catch (FormatException fex2) when (fex2.Message.Contains(EX_MBOX_PARSE_HEADERS, StringComparison.OrdinalIgnoreCase))
                {
                    WriteErrorMessage(xwriter, fex2.Message);
                    break;//don't try processing any more messages
                    //TODO: might still be salvageable records in the mbox if we can adjust the position and keep moving
                }
                catch (Exception ex)
                {
                    WriteErrorMessage(xwriter, ex.Message);
                    break; //don't try processing any more messages
                    //TODO: might still be salvageable records in the mbox if we can adjust the position and keep moving
                }

                //NOTE:  if the parser throws an exception, the position may not advance which could lead to an endless loop,
                //so do a position check here, if the position hasn't advanced, then there is a problem
                if (parser.Position == prevParserPos)
                {
                    WriteErrorMessage(xwriter, "Parse position has not advanced which indicates a possible problem with the file");
                    break;//don't try processing any more messages
                    //TODO: might still be salvageable records in the mbox if we can adjust the position and keep moving
                }

                if (prevParserPos == 0)  //this is the first message in the file
                {
                    //Defer creating these tags until after we can verify that the file is a valid mbox file
                    xwriter.WriteStartElement("Folder", XM_NS);
                    xwriter.WriteElementString("Name", XM_NS, mboxProps.MboxName);
                    validMboxFile = true;
                }
                if (message != null)
                {
                    localId = ProcessCurrentMessage(message, xwriter, localId, messageList, mboxProps, msgProps);
                }
                else
                {
                    ProcessIncompleteMessage(localId, xwriter, "ERROR", parser.Position, messageList, mboxProps, msgProps);
                }
                prevParserPos = parser.Position;

            }

            //make sure to read to the end of the stream so the hash is correct
            int i = -1;
            do
            {
                i = cryptoStream.ReadByte();
            } while (i != -1);

            if (validMboxFile && Settings.IncludeSubFolders)
            {
                //look for a subfolder named the same as the mbox file ignoring extensions
                //i.e. Mozilla Thunderbird will append the extension '.sbd' to the folder name
                string? subfolderName = null;
                try
                {
                    subfolderName = Directory.GetDirectories(mboxProps.MboxDirectoryName, $"{mboxProps.MboxName}.*").SingleOrDefault();
                }
                catch (InvalidOperationException)
                {
                    WriteErrorMessage(xwriter, $"There is more than one folder that matches '{mboxProps.MboxName}.*'; skipping all subfolders");
                    subfolderName = null;
                }
                catch (Exception ex)
                {
                    WriteErrorMessage(xwriter, $"Skipping subfolders. {ex.GetType().Name}: {ex.Message}");
                    subfolderName = null;
                }

                if (!string.IsNullOrWhiteSpace(subfolderName))
                {
                    _logger.LogInformation($"Processing Subfolder: {subfolderName}");
                    //look for mbox files in this subdirectory
                    string[]? childMboxes = null;
                    try
                    {
                        childMboxes = Directory.GetFiles(subfolderName);
                    }
                    catch (Exception ex)
                    {
                        WriteErrorMessage(xwriter, $"Skipping this subfolder. {ex.GetType().Name}: {ex.Message}");
                        subfolderName = null;
                    }

                    if (childMboxes != null && childMboxes.Count() > 0)
                    {
                        //this is all the files, so need to determine which ones are mbox files or not
                        foreach (var childMbox in childMboxes)
                        {
                            //create new MboxProperties which is copy of parent MboxProperties except for the MboxFilePath
                            MboxProperties childMboxProps = new MboxProperties()
                            {
                                MboxFilePath = childMbox,
                                AccountId = mboxProps.AccountId,
                                OutFilePath = mboxProps.OutFilePath,
                            };
                            SetHashAlgorithm(xwriter, mboxProps);

                            //just try to process it, if no errors thrown, its probably an mbox file
                            WriteInfoMessage(xwriter, $"Processing Child Mbox: {childMbox}");
                            localId = ProcessMbox(xwriter, localId, messageList, childMboxProps);
                        }
                    }

                }
            }

            xwriter.WriteStartElement("Mbox", XM_NS);
            var relPath = Path.GetRelativePath(mboxProps.OutDirectoryName, mboxProps.MboxFilePath);
            xwriter.WriteElementString("RelPath", XM_NS, relPath);
            xwriter.WriteElementString("Eol", XM_NS, mboxProps.MostCommonEol);
            if (mboxProps.UsesDifferentEols)
            {
                WriteWarningMessage(xwriter, $"Mbox file contains multiple different EOLs: CR: {mboxProps.EolCounts["CR"]}, LF: {mboxProps.EolCounts["LF"]}, CRLF: {mboxProps.EolCounts["CRLF"]}");
            }
            if (mboxProps.HashAlgorithm.Hash != null)
            {
                WriteHash(xwriter, mboxProps.HashAlgorithm.Hash, mboxProps.HashAlgorithmName);
            }
            else
            {
                WriteWarningMessage(xwriter, $"Unable to calculate the hash value for the Mbox");
            }

            xwriter.WriteEndElement(); //Mbox

            xwriter.WriteEndElement(); //Folder

            return localId;
        }

        private void SetHashAlgorithm(XmlWriter xwriter, MboxProperties mboxProps)
        {
            var name = mboxProps.TrySetHashAlgorithm(Settings.HashAlgorithmName);
            if (name != Settings.HashAlgorithmName)
            {
                WriteWarningMessage(xwriter, $"The hash algorithm '{Settings.HashAlgorithmName}' is not supported.  Using '{name}' instead.");
                Settings.HashAlgorithmName = name;
            }
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

        private long ProcessCurrentMessage(MimeMessage message, XmlWriter xwriter, long localId, List<MessageBrief> messageList, MboxProperties mboxProps, MimeMessageProperties msgProps)
        {

            localId++;
            var messageId = localId;

            xwriter.WriteStartElement("Message", XM_NS);

            localId = ConvertMessageToEAXS(message, xwriter, localId, false, mboxProps, msgProps);

            xwriter.WriteEndElement(); //Message

            messageList.Add(new MessageBrief()
            {
                LocalId = messageId,
                From = message.From.ToString(),
                To = message.To.ToString(),
                Date = message.Date,
                Subject = message.Subject,
                MessageID = message.MessageId,
                Hash = Convert.ToHexString(msgProps.MessageHash, 0, msgProps.MessageHash.Length),
                Errors = 0,
                FirstErrorMessage = ""

            });

            msgProps.Eol = MimeMessageProperties.EOL_TYPE_UNK;

            return localId;
        }
        /// <summary>
        /// Create a dummy message as a placeholder for incomplete messages which indicate some sort of error while parsing the mbox
        /// </summary>
        /// <param name="localId"></param>
        /// <param name="xwriter"></param>
        /// <param name="errMsg"></param>
        /// <param name="position"></param>
        /// <param name="messageList"></param>
        /// <param name="mboxProps"></param>
        /// <param name="msgProps"></param>
        private void ProcessIncompleteMessage(long localId, XmlWriter xwriter, string errMsg, long position, List<MessageBrief> messageList, MboxProperties mboxProps, MimeMessageProperties msgProps)
        {
            string messageId = MimeUtils.GenerateMessageId();

            xwriter.WriteStartElement("Message", XM_NS);
            xwriter.WriteElementString("LocalId", XM_NS, localId.ToString());
            xwriter.WriteStartElement("MessageId", XM_NS);
            xwriter.WriteAttributeString("Supplied", "true");
            xwriter.WriteString(messageId);
            xwriter.WriteEndElement(); //MessageId
            xwriter.WriteStartElement("Incomplete", XM_NS);
            xwriter.WriteElementString("ErrorType", XM_NS, errMsg);
            xwriter.WriteElementString("ErrorLocation", XM_NS, $"Stream Position: {position}");
            xwriter.WriteEndElement(); //Incomplete
            xwriter.WriteElementString("Eol", XM_NS, msgProps.Eol); //This might be unknown at this point if there was an error
            xwriter.WriteEndElement(); //Message

            //create dummy message
            MimeMessage message = new MimeMessage()
            {
                MessageId = messageId
            };

            messageList.Add(new MessageBrief()
            {
                LocalId = localId,
                From = message.From.ToString(),
                To = message.To.ToString(),
                Date = message.Date,
                Subject = message.Subject,
                MessageID = message.MessageId,
                Hash = Convert.ToHexString(msgProps.MessageHash, 0, msgProps.MessageHash.Length),
                Errors = 1,
                FirstErrorMessage = errMsg

            });

            msgProps.Eol = MimeMessageProperties.EOL_TYPE_UNK;

        }

        private long ConvertMessageToEAXS(MimeMessage message, XmlWriter xwriter, long localId, bool isChildMessage, MboxProperties mboxProps, MimeMessageProperties msgProps)
        {

            _logger.LogInformation("Converting {messageType} {localId} Subject: {subject}", isChildMessage ? "Child Message" : "Message", localId, message.Subject);

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
                WriteMessageStatuses(xwriter, mboxProps.MboxFilePath, message);
            }

            localId = ConvertBody2EAXS(message.Body, xwriter, localId, mboxProps, msgProps);

            if (!isChildMessage)
            {
                xwriter.WriteElementString("Eol", XM_NS, msgProps.Eol);

                WriteHash(xwriter, msgProps.MessageHash, Settings.HashAlgorithmName);
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

        private long ConvertBody2EAXS(MimeEntity mimeEntity, XmlWriter xwriter, long localId, MboxProperties mboxProps, MimeMessageProperties msgProps)
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
                WriteWarningMessage(xwriter, $"Unexpected MIME Entity Type: '{mimeEntity.GetType().FullName}' -- '{mimeEntity.ContentType.MimeType}'");
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
                    localId = ConvertBody2EAXS(item, xwriter, localId, mboxProps, msgProps);
                }
            }
            else if (isMultipart && multipart != null && multipart.Count == 0)
            {
                WriteWarningMessage(xwriter, $"Item is multipart, but there are no parts");
            }
            else if (isMultipart && multipart == null)
            {
                WriteWarningMessage(xwriter, $"Item is erroneously flagged as multipart");
            }
            else if (!isMultipart)
            {
                if (part != null && !IsXMozillaExternalAttachment(part))
                {
                    localId = WriteSingleBodyContent(xwriter, part, localId, mboxProps);
                }
                else if (part != null && IsXMozillaExternalAttachment(part))
                {
                    WriteInfoMessage(xwriter, "The content is an inaccessible external attachment");
                }
                else if (message != null)
                {
                    localId = WriteSingleBodyChildMessage(xwriter, message, localId, mboxProps, msgProps);
                }
                else
                {
                    WriteWarningMessage(xwriter, $"Unexpected MimeEntity: {mimeEntity.GetType().FullName}");
                }
            }
            else
            {
                WriteWarningMessage(xwriter, $"Unexpected MimeEntity: {mimeEntity.GetType().FullName}");
            }

            //PhantomBody; Content-Type message/external-body
            //Mozilla uses different headers to indicate this situtation, for example:
            //      X-Mozilla-External-Attachment-URL: file://///libgrrudra/Users/thabing/My%20Documents/EMAILS/Attachments/DLFSCHOLARS2004-2.pdf
            //      X-Mozilla-Altered: AttachmentDetached; date = "Fri May 19 09:27:32 2006"
            //NEEDSTEST:  Find or construct a sample message with content-type message/external-body

            if (!isMultipart && part != null && (part.ContentType.IsMimeType("message", "external-body") || IsXMozillaExternalAttachment(part)))
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

        private long WriteSingleBodyChildMessage(XmlWriter xwriter, MessagePart message, long localId, MboxProperties mboxProps, MimeMessageProperties msgProps)
        {
            xwriter.WriteStartElement("ChildMessage", XM_NS);
            localId++;
            localId = ConvertMessageToEAXS(message.Message, xwriter, localId, true, mboxProps, msgProps);
            xwriter.WriteEndElement(); //ChildMessage
            return localId;
        }

        private long WriteSingleBodyContent(XmlWriter xwriter, MimePart part, long localId, MboxProperties mboxProps)
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
                //TODO:  Need to see if we can process message/external-body parts where the content is referenced by the other content-type parameters
                //       See https://www.oreilly.com/library/view/programming-internet-email/9780596802585/ch04s04s01.html
                //       Also consider the "X-Mozilla-External-Attachment-URL: url" and the "X-Mozilla-Altered: AttachmentDetached; date="Thu Jul 06 21:38:39 2006"" headers
                
                if (!Settings.SaveAttachmentsAndBinaryContentExternally)
                {
                    //save non-text content or attachments as part of the XML
                    SerializeContentInXml(part, xwriter, false, localId);
                }
                else
                {
                    //save non-text content or attachments externally, possibly wrapped in XML
                    localId = SerializeContentInExtFile(part, xwriter, localId, mboxProps);
                }
            }
            return localId;
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
                    WriteWarningMessage(xwriter, $"A multipart entity has a Content-Transfer-Encoding of '{transferEncoding}'; normally this should only be 7bit for multipart entities.");
                }
            }
            //TODO: TransferEncodingComments, not currently supported by MimeKit

            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentId))
            {
                xwriter.WriteElementString("ContentId", XM_NS, mimeEntity.ContentId);
            }
            //TODO: ContentIdComments, not currently supported by MimeKit, actually might not be allowed by the RFC - not sure if ContentId is a structured header type

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
                WriteWarningMessage(xwriter, $"MIME type boundary parameter '{mimeEntity.ContentType.Boundary}' found for a non-multipart mime type");

            }
            else if (isMultipart && string.IsNullOrWhiteSpace(mimeEntity.ContentType.Boundary))
            {
                WriteWarningMessage(xwriter, "MIME type boundary parameter is missing for a multipart mime type");
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
        /// <param name="ExtContent">if true, it is being written to an external file</param>
        /// <param name="localId">The local id of the content being written to an external file</param>
        /// <param name="preserveEncodingIfPossible">if true write text as unicode and use the original content encoding if possible; if false write it as either unicode text or as base64 encoded binary</param>
        private void SerializeContentInXml(MimePart part, XmlWriter xwriter, bool ExtContent, long localId)
        {
            var content = part.Content;

            xwriter.WriteStartElement("BodyContent", XM_NS);
            if (ExtContent)
            {
                xwriter.WriteAttributeString("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance", "eaxs_schema_v2.xsd");
                WriteInfoMessage(xwriter, $"LocalId {localId} written to external file");
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
        /// <param name="mboxProps">Mbox properties needed to serialize the content</param>
        /// <returns>the new localId value after incrementing it for the new file</returns>
        /// <exception cref="Exception">thrown if unable to generate the hash</exception>
        private long SerializeContentInExtFile(MimePart part, XmlWriter xwriter, long localId, MboxProperties mboxProps)
        {
            localId++;
            var content = part.Content;

            //get random file name that doesn't already exist
            string randomFilePath = "";
            do
            {
                randomFilePath = Path.Combine(mboxProps.OutDirectoryName, Path.GetRandomFileName());
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
                SerializeContentInXml(part, extXmlWriter, true, localId);
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

            var hashFileName = Path.Combine(mboxProps.OutDirectoryName, EXT_CONTENT_DIR, hashStr[..2], Path.ChangeExtension(hashStr, ext));
            Directory.CreateDirectory(Path.GetDirectoryName(hashFileName) ?? "");

            //Deal with duplicate attachments, which should only be stored once, make sure the randomFilePath file is deleted
            if (File.Exists(hashFileName))
            {
                WriteInfoMessage(xwriter, "Duplicate attachment has already been saved");
                File.Delete(randomFilePath);
            }
            else
            {
                File.Move(randomFilePath, hashFileName);
            }

            xwriter.WriteStartElement("ExtBodyContent", XM_NS);
            xwriter.WriteElementString("RelPath", XM_NS, Path.GetRelativePath(Path.Combine(mboxProps.OutDirectoryName, EXT_CONTENT_DIR), hashFileName));
            //CharSet and TransferEncoding are the same as for the SingleBody
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

            if (mimeStatus.Contains(STATUS_FLAG_READ)) //Read
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

            if (mimeStatus.Contains(STATUS_FLAG_ANSWERED)) //Answered
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

            if (mimeStatus.Contains(STATUS_FLAG_FLAGGED)) //Flagged
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

            if (mimeStatus.Contains(STATUS_FLAG_DELETED)) //Deleted
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
                || mimeStatus.Contains(STATUS_FLAG_DRAFT)  //Some clients encode draft as "T" in the X-Status header
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
            if (message.Headers.Contains(HeaderId.Status) && !mimeStatus.Contains(STATUS_FLAG_OLD)) //Not Old
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

        private bool IsXMozillaExternalAttachment(MimePart part)
        {
            return part.Headers.Contains("X-Mozilla-External-Attachment-URL") && (part.Headers["X-Mozilla-Altered"] ?? "").Contains("AttachmentDetached");
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

        /// <summary>
        /// Write a message to both the log and to the XML output file
        /// </summary>
        /// <param name="xwriter"></param>
        /// <param name="message"></param>
        private void WriteErrorMessage(XmlWriter xwriter, string message)
        {
            xwriter.WriteComment($"ERROR: {message}");
            _logger.LogError(message);
        }
        private void WriteWarningMessage(XmlWriter xwriter, string message)
        {
            xwriter.WriteComment($"WARNING: {message}");
            _logger.LogWarning(message);
        }
        private void WriteInfoMessage(XmlWriter xwriter, string message)
        {
            xwriter.WriteComment($"INFO: {message}");
            _logger.LogInformation(message);
        }

        private void Parser_MimeMessageEnd(object? sender, MimeMessageEndEventArgs e, Stream mboxStream, MboxProperties mboxProps, MimeMessageProperties msgProps)
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
                    msgProps.Eol = MimeMessageProperties.EOL_TYPE_CRLF;
                    break;
                }
                else if (buffer[i] == LF)
                {
                    msgProps.Eol = MimeMessageProperties.EOL_TYPE_LF;
                    break;
                }
                else if (buffer[i] == CR && buffer[i + 1] != LF)
                {
                    msgProps.Eol = MimeMessageProperties.EOL_TYPE_CR;
                    break;
                }
                i++;
            }

            //Check that messages use the same EOL treatment throughout the mbox
            if (msgProps.Eol != MimeMessageProperties.EOL_TYPE_UNK)
            {
                if (mboxProps.EolCounts.ContainsKey(msgProps.Eol))
                {
                    mboxProps.EolCounts[msgProps.Eol]++;
                }
                else
                {
                    mboxProps.EolCounts.Add(msgProps.Eol, 1);
                }
            }

            //trim all LWSP and EOL chars from the end and then add one eol marker back
            //assumes that the same EOL markers are used throughout the mbox
            i = buffer.Length - 1;
            while (buffer[i] == LF || buffer[i] == CR || buffer[i] == SP || buffer[i] == TAB)
                --i;
            long j = 1;
            if (msgProps.Eol == MimeMessageProperties.EOL_TYPE_CRLF)
                j = 2;
            byte[] newBuffer = new byte[i + 1 + j];

            Array.Copy(buffer, 0, newBuffer, 0, i + 1);
            if (msgProps.Eol == MimeMessageProperties.EOL_TYPE_CRLF)
            {
                newBuffer[newBuffer.Length - 1] = LF;
                newBuffer[newBuffer.Length - 2] = CR;
            }
            else if (msgProps.Eol == MimeMessageProperties.EOL_TYPE_LF)
            {
                newBuffer[newBuffer.Length - 1] = LF;
            }
            else if (msgProps.Eol == MimeMessageProperties.EOL_TYPE_CR)
            {
                newBuffer[newBuffer.Length - 1] = CR;
            }
            else
            {
                _logger.LogError("Unable to determine EOL marker");
                throw new Exception("Unable to determine EOL marker");
            }

            var hashAlg = HashAlgorithm.Create(Settings.HashAlgorithmName) ?? SHA256.Create(); //Fallback to known hash algorithm
            msgProps.MessageHash = hashAlg.ComputeHash(newBuffer);

        }
    }
}