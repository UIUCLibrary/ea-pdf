using CsvHelper;
using iTextSharp.text.xml.xmp;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Tnef;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.EaPdf
{
    public class EmailToEaxsProcessor
    {
        //TODO: Need to check for XML invalid characters almost anyplace I write XML string content, see the WriteElementStringReplacingInvalidChars function
        //      See https://github.com/orgs/UIUCLibrary/projects/39/views/2?pane=issue&itemId=25980601

        //for LWSP (Linear White Space) detection, compaction, and trimming
        const byte CR = 13;
        const byte LF = 10;
        const byte SP = 32;
        const byte TAB = 9;


        public const string XM = "xm";
        public const string XM_NS = "https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2";
        public const string XM_XSD = "XResources/eaxs_schema_v2.xsd";
        public const string XM_CC_XSD = "XResources/CountryCodes.xsd";


        public const string XHTML = "xhtml";
        public const string XHTML_NS = "http://www.w3.org/1999/xhtml";
        public const string XHTML_XSD = "XResources/eaxs_xhtml_mini.xsd";

        private readonly ILogger _logger;

        public const string HASH_DEFAULT = "SHA256";

        const string EX_MBOX_FROM_MARKER = "Failed to find mbox From marker";
        const string EX_MBOX_PARSE_HEADERS = "Failed to parse message headers";

        const int EPILOGUE_THRESHOLD = 200; //maximum number of characters before the epilogue is considered suspicious and a warning is logged

        const double MAX_FILE_SIZE_THRESHOLD = 0.95;

        public EmailToEaxsProcessorSettings Settings { get; }

        //stats used for development and debuging
        private readonly Dictionary<string, int> contentTypeCounts = new();
        private readonly Dictionary<string, int> xGmailLabelCounts = new();
        private readonly Dictionary<string, int> xGmailLabelComboCounts = new();

        //need to keep track of folders in case output file is split into multiple files and the split happens while processing a subfolder
        private readonly Stack<string> _folders = new();

        private bool skippingMessages = false; //will skip processing messages while this is true
        private bool allDone = false; //will stop processing messages when this is true

        /// <summary>
        /// Create a processor for email files, initializing the logger and settings
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public EmailToEaxsProcessor(ILogger logger, EmailToEaxsProcessorSettings settings)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace($"{this.GetType().Name} Created");

            //Set the skippingMessages flag if both SkipUntilMessageId  is set
            if (!string.IsNullOrWhiteSpace(Settings.SkipUntilMessageId))
            {
                skippingMessages = true;
                _logger.LogInformation($"Skipping all messages until MessagedId '{Settings.SkipUntilMessageId}' is found");
            }
            if (!string.IsNullOrWhiteSpace(Settings.SkipAfterMessageId))
            {
                _logger.LogInformation($"Skipping all messages after MessagedId '{Settings.SkipAfterMessageId}' is found");
            }

            //add any extra character entities to the HtmlAgilityPack.HtmlEntity class
            if (Settings.ExtraHtmlCharacterEntities != null)
            {
                foreach (var ent in Settings.ExtraHtmlCharacterEntities)
                {
                    _ = HtmlAgilityPack.HtmlEntity.EntityValue.TryAdd(ent.Key, ent.Value);
                    _ = HtmlAgilityPack.HtmlEntity.EntityName.TryAdd(ent.Value, ent.Key);
                }
            }

        }

        /// <summary>
        /// Convert a single EML file into an archival email XML file
        /// An EML file is a single email message; they are the same format as messages inside an mbox file, except they do not start with a 'From ' header
        /// </summary>
        /// <param name="emlFilePath"></param>
        /// <param name="outFolderPath"></param>
        /// <param name="globalId"></param>
        /// <param name="accntEmails"></param>
        /// <param name="startingLocalId"></param>
        /// <param name="messageList"></param>
        /// <param name="saveCsv"></param>
        /// <returns></returns>
        public long ConvertEmlToEaxs(string emlFilePath, string outFolderPath, string globalId, string accntEmails = "", long startingLocalId = 0, List<MessageBrief>? messageList = null, bool saveCsv = true)
        {
            (MessageFileProperties msgFileProps, Stream xstream, XmlWriter xwriter) = MessageFileAndXmlStreamSetup(emlFilePath, outFolderPath, globalId, accntEmails);
            msgFileProps.MessageFormat = MimeFormat.Entity;

            messageList ??= new List<MessageBrief>(); //used to create the CSV file

            long localId = startingLocalId;

            WriteFolderOpen(xwriter, msgFileProps);
            localId = ProcessEml(msgFileProps, ref xwriter, ref xstream, localId, messageList);
            WriteFolderClose(xwriter);

            XmlStreamTeardown(xwriter, xstream);

            //write the csv file
            if (saveCsv)
            {
                SaveCsvs(msgFileProps.OutDirectoryName, msgFileProps.MessageFilePath, messageList);
            }

            _logger.LogInformation("Output XML File: {xmlFilePath}, ending: {endTime}", msgFileProps.OutFilePath, DateTime.Now.ToString("s"));

            return localId;
        }

        private (XmlWriter xwriter, Stream xstream) XmlStreamSetup(string xmlFilePath, string globalId, string accntEmails)
        {
            var xset = new XmlWriterSettings()
            {
                CloseOutput = true,
                Indent = true,
                Encoding = System.Text.Encoding.UTF8
            };

            if (!Directory.Exists(Path.GetDirectoryName(xmlFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(xmlFilePath) ?? "");
            }

            Stream xstream = new FileStream(xmlFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var xwriter = XmlWriter.Create(xstream, xset);


            xwriter.WriteStartDocument();
            WriteDocType(xwriter);
            WriteAccountHeaderFields(xwriter, globalId, accntEmails);

            return (xwriter, xstream);

        }

        private void XmlStreamTeardown(XmlWriter xwriter, Stream xstream)
        {
            xwriter.WriteEndElement(); //Account
            xwriter.WriteEndDocument();

            xwriter.Flush();
            xwriter.Close(); //this should close the underlying stream
            xwriter.Dispose();
            xstream.Dispose();
        }

        private (MessageFileProperties msgFileProps, Stream xstream, XmlWriter xwriter) MessageFileAndXmlStreamSetup(string inFilePath, string outFolderPath, string globalId, string accntEmails)
        {
            if (string.IsNullOrWhiteSpace(inFilePath))
            {
                throw new ArgumentNullException(nameof(inFilePath));
            }

            if (!File.Exists(inFilePath))
            {
                throw new FileNotFoundException(inFilePath);
            }

            if (string.IsNullOrWhiteSpace(outFolderPath))
            {
                throw new ArgumentNullException(nameof(outFolderPath));
            }

            if (string.IsNullOrWhiteSpace(globalId))
            {
                throw new Exception("globalId is a required parameter");
            }

            string fullInFilePath = Path.GetFullPath(inFilePath);
            string fullOutFolderPath = Path.GetFullPath(outFolderPath);

            //Determine whether the output folder path is valid given the input path
            if (!Helpers.FilePathHelpers.IsValidOutputPathForMboxFile(fullInFilePath, fullOutFolderPath))
            {
                throw new ArgumentException($"The outFolderPath, '{fullOutFolderPath}', cannot be the same as or a child of the emlFilePath, '{fullInFilePath}', ignoring any extensions");
            }

            var xmlFilePath = Path.Combine(fullOutFolderPath, Path.GetFileName(Path.ChangeExtension(fullInFilePath, "xml")));

            (XmlWriter xwriter, Stream xstream) = XmlStreamSetup(xmlFilePath, globalId, accntEmails);

            WriteToLogInfoMessage(xwriter, $"Processing EML file: {fullInFilePath}, starting: {DateTime.Now:s}");

            var msgFileProps = new MessageFileProperties()
            {
                MessageFilePath = fullInFilePath,
                GlobalId = globalId,
                OutFilePath = xmlFilePath,
                AccountEmails = accntEmails
            };
            SetHashAlgorithm(msgFileProps, xwriter);

            return (msgFileProps, xstream, xwriter);
        }

        public long ConvertFolderOfEmlToEaxs(string emlFolderPath, string outFolderPath, string globalId, string accntEmails = "", long startingLocalId = 0, List<MessageBrief>? messageList = null, bool saveCsv = true)
        {
            (string fullEmlFolderPath, string fullOutFolderPath) = MessageFolderSetup(emlFolderPath, outFolderPath, globalId);

            messageList ??= new List<MessageBrief>(); //used to create the CSV file

            long localId = startingLocalId;

            if (Settings.OneFilePerMessageFile)
            {
                long filesWithMessagesCnt = 0;
                long filesWithoutMessagesCnt = 0;
                long prevLocalId = startingLocalId;

                MessageFileProperties msgFileProps;
                foreach (string emlFilePath in Directory.EnumerateFiles(emlFolderPath))
                {
                    var xmlFilePath = Path.Combine(fullOutFolderPath, Path.GetFileName(Path.ChangeExtension(fullEmlFolderPath, "xml")));
                    (msgFileProps, Stream xstream, XmlWriter xwriter) = MessageFileAndXmlStreamSetup(emlFilePath, fullOutFolderPath, globalId, accntEmails);
                    msgFileProps.MessageFormat = MimeFormat.Entity;

                    WriteFolderOpeners(xwriter);
                    WriteFolderOpen(xwriter, msgFileProps);
                    localId = ProcessEml(msgFileProps, ref xwriter, ref xstream, localId, messageList);
                    WriteFolderClose(xwriter);
                    WriteFolderClosers(xwriter);

                    XmlStreamTeardown(xwriter, xstream);

                    if (localId > prevLocalId)
                    {
                        filesWithMessagesCnt++;
                    }
                    else
                    {
                        filesWithoutMessagesCnt++;
                    }
                    prevLocalId = localId;
                }

                if (Settings.IncludeSubFolders)
                {
                    _folders.Push(Path.GetFileName(fullEmlFolderPath));
                    foreach (string emlSubfolderPath in Directory.EnumerateDirectories(fullEmlFolderPath))
                    {
                        localId = ConvertFolderOfEmlToEaxs(emlSubfolderPath, outFolderPath, globalId, accntEmails, localId, messageList, false);
                    }

                }

                _logger.LogInformation("Files with messages: {filesWithMessagesCnt}, Files without messages: {filesWithoutMessagesCnt}, Total messages: {localId - startingLocalId}", filesWithMessagesCnt, filesWithoutMessagesCnt, localId - startingLocalId);
            }
            else
            {

                var xmlFilePath = Path.Combine(fullOutFolderPath, Path.GetFileName(Path.ChangeExtension(fullEmlFolderPath, "xml")));

                _logger.LogInformation("Convert EML files in directory: '{fullMessageFolderPath}' into XML file: '{outFilePath}'", fullEmlFolderPath, xmlFilePath);

                (XmlWriter xwriter, Stream xstream) = XmlStreamSetup(xmlFilePath, globalId, accntEmails);

                var msgFileProps = new MessageFileProperties()
                {
                    GlobalId = globalId,
                    AccountEmails = accntEmails,
                    OutFilePath = xmlFilePath,
                    MessageFormat = MimeFormat.Entity,
                    MessageFilePath = Path.Combine(fullEmlFolderPath, Guid.NewGuid().ToString()) // use Guid to ensure its not a real file; this is a dummy file path that will be overwritten in the foreach loop below, but is needed to initialize the MessageFileProperties object with the correct file directory
                };
                SetHashAlgorithm(msgFileProps, xwriter);

                WriteFolderOpen(xwriter, msgFileProps);

                foreach (string emlFilePath in Directory.EnumerateFiles(fullEmlFolderPath))
                {
                    WriteToLogInfoMessage(xwriter, $"Processing EML file: {emlFilePath}");
                    msgFileProps.MessageFilePath = emlFilePath;
                    msgFileProps.MessageCount = 0;

                    localId = ProcessEml(msgFileProps, ref xwriter, ref xstream, localId, messageList);
                }

                if (Settings.IncludeSubFolders)
                {
                    localId = ProcessEmlSubfolders(msgFileProps, ref xwriter, ref xstream, localId, messageList);
                }

                WriteFolderClose(xwriter);

                XmlStreamTeardown(xwriter, xstream);

                _logger.LogInformation("Output XML File: {xmlFilePath}, Total messages: {messageCount}", xmlFilePath, localId - startingLocalId);
            }

            //write the csv file
            if (saveCsv)
            {
                SaveCsvs(fullOutFolderPath, fullEmlFolderPath, messageList);
            }

            return localId;
        }

        private (string fullMessageFolderPath, string fullOutFolderPath) MessageFolderSetup(string messageFolderPath, string outFolderPath, string globalId)
        {
            if (string.IsNullOrWhiteSpace(messageFolderPath))
            {
                throw new ArgumentNullException(nameof(messageFolderPath));
            }

            if (!Directory.Exists(messageFolderPath))
            {
                throw new DirectoryNotFoundException(messageFolderPath);
            }

            if (string.IsNullOrWhiteSpace(outFolderPath))
            {
                throw new ArgumentNullException(nameof(outFolderPath));
            }

            if (string.IsNullOrWhiteSpace(globalId))
            {
                throw new ArgumentNullException(nameof(globalId));
            }

            var fullMessageFolderPath = Path.GetFullPath(messageFolderPath);
            var fullOutFolderPath = Path.GetFullPath(outFolderPath);

            //Determine whether the output folder path is valid given the input path
            if (!Helpers.FilePathHelpers.IsValidOutputPathForMboxFolder(fullMessageFolderPath, fullOutFolderPath))
            {
                throw new ArgumentException($"The outFolderPath, '{fullOutFolderPath}', cannot be the same as or a child of the messageFolderPath, '{fullMessageFolderPath}'");
            }

            return (fullMessageFolderPath, fullOutFolderPath);
        }

        /// <summary>
        /// Convert a folder of mbox files into an archival email XML file
        /// </summary>
        /// <param name="mboxFolderPath">the path to the folder to process, all mbox files in the folder will be processed</param>
        /// <param name="outFolderPath">the path to the output folder, must be different than the messageFolderPath</param>
        /// <param name="globalId">Globally unique, permanent, absolute URI with no fragment conforming to the canonical form specified in RFC2396 as amended by RFC2732.</param>
        /// <param name="accntEmails">Comma-separated list of email addresses</param>
        /// <param name="includeSubFolders">if true subfolders in the directory will also be processed</param>
        /// <returns>the most recent localId number which is usually the total number of messages processed</returns>
        public long ConvertFolderOfMboxToEaxs(string mboxFolderPath, string outFolderPath, string globalId, string accntEmails = "", long startingLocalId = 0, List<MessageBrief>? messageList = null, bool saveCsv = true)
        {
            (string fullMboxFolderPath, string fullOutFolderPath) = MessageFolderSetup(mboxFolderPath, outFolderPath, globalId);

            messageList ??= new List<MessageBrief>(); //used to create the CSV file

            long localId = startingLocalId;

            if (Settings.OneFilePerMessageFile)
            {
                long filesWithMessagesCnt = 0;
                long filesWithoutMessagesCnt = 0;
                long prevLocalId = startingLocalId;

                foreach (string mboxFilePath in Directory.EnumerateFiles(mboxFolderPath))
                {
                    localId = ConvertMboxToEaxs(mboxFilePath, fullOutFolderPath, globalId, accntEmails, localId, messageList, false);

                    if (localId > prevLocalId)
                    {
                        filesWithMessagesCnt++;
                    }
                    else
                    {
                        filesWithoutMessagesCnt++;
                    }
                    prevLocalId = localId;
                }

                _logger.LogInformation("Files with messages: {filesWithMessagesCnt}, Files without messages: {filesWithoutMessagesCnt}, Total messages: {localId - startingLocalId}", filesWithMessagesCnt, filesWithoutMessagesCnt, localId - startingLocalId);
            }
            else
            {

                var xmlFilePath = Path.Combine(fullOutFolderPath, Path.GetFileName(Path.ChangeExtension(fullMboxFolderPath, "xml")));

                _logger.LogInformation("Convert mbox files in directory: '{fullMessageFolderPath}' into XML file: '{outFilePath}'", fullMboxFolderPath, xmlFilePath);

                (XmlWriter xwriter, Stream xstream) = XmlStreamSetup(xmlFilePath, globalId, accntEmails);

                var mboxProps = new MessageFileProperties()
                {
                    GlobalId = globalId,
                    AccountEmails = accntEmails,
                    OutFilePath = xmlFilePath,
                    MessageFormat = MimeFormat.Mbox
                };
                SetHashAlgorithm(mboxProps, xwriter);

                foreach (string mboxFilePath in Directory.EnumerateFiles(mboxFolderPath))
                {
                    WriteToLogInfoMessage(xwriter, $"Processing mbox file: {mboxFilePath}");
                    mboxProps.MessageFilePath = mboxFilePath;
                    mboxProps.MessageCount = 0;

                    localId = ProcessMbox(mboxProps, ref xwriter, ref xstream, localId, messageList);
                }

                XmlStreamTeardown(xwriter, xstream);

                _logger.LogInformation("Output XML File: {xmlFilePath}, Total messages: {messageCount}", xmlFilePath, localId - startingLocalId);
            }

            //write the csv file
            if (saveCsv)
            {
                SaveCsvs(fullOutFolderPath, fullMboxFolderPath, messageList);
            }

            return localId;
        }

        /// <summary>
        /// Convert one mbox file into an archival email XML file.
        /// </summary>
        /// <param name="fullMboxFilePath">the path to the mbox file to process</param>
        /// <param name="outFolderPath">the path to the output folder, must be different than the folder containing the emlFilePath</param>
        /// <param name="globalId">Globally unique, permanent, absolute URI with no fragment conforming to the canonical form specified in RFC2396 as amended by RFC2732.</param>
        /// <param name="accntEmails">Comma-separated list of email addresses</param>
        /// <returns>the most recent localId number which is usually the total number of messages processed</returns>
        public long ConvertMboxToEaxs(string mboxFilePath, string outFolderPath, string globalId, string accntEmails = "", long startingLocalId = 0, List<MessageBrief>? messageList = null, bool saveCsv = true)
        {
            (MessageFileProperties msgFileProps, Stream xstream, XmlWriter xwriter) = MessageFileAndXmlStreamSetup(mboxFilePath, outFolderPath, globalId, accntEmails);
            msgFileProps.MessageFormat = MimeFormat.Mbox;

            messageList ??= new List<MessageBrief>(); //used to create the CSV file

            long localId = startingLocalId;

            localId = ProcessMbox(msgFileProps, ref xwriter, ref xstream, localId, messageList);

            //safety check
            if (_folders.Count > 0)
            {
                throw new Exception($"There are {_folders.Count} folders that were not closed.");
            }

            XmlStreamTeardown(xwriter, xstream);

            //write the csv file
            if (saveCsv)
            {
                SaveCsvs(msgFileProps.OutDirectoryName, msgFileProps.MessageFilePath, messageList);
            }

            _logger.LogInformation("Output XML File: {xmlFilePath}, ending: {endTime}", msgFileProps.OutFilePath, DateTime.Now.ToString("s"));

            return localId;
        }

        private void SaveCsvs(string outDirectoryName, string messageFilePath, List<MessageBrief> messageList)
        {
            var csvFilePath = Path.Combine(outDirectoryName, Path.GetFileName(Path.ChangeExtension(messageFilePath, "csv")));
            MessageBrief.SaveMessageBriefsToCsvFile(csvFilePath, messageList);
            csvFilePath = Path.Combine(outDirectoryName, Path.GetFileNameWithoutExtension(messageFilePath) + "_stats.csv");
            SaveStatsToCsv(csvFilePath);
        }

        private void SaveStatsToCsv(string csvFilepath)
        {
            //FUTURE: Save to Excel https://learn.microsoft.com/en-us/previous-versions/technet-magazine/cc161037(v=msdn.10)?redirectedfrom=MSDN
            using (var writer = new StreamWriter(csvFilepath))
            {
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.WriteRecords(contentTypeCounts);
            }

            using (var writer = new StreamWriter(csvFilepath, true))
            {
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.WriteRecords(xGmailLabelCounts);
            }

            using (var writer = new StreamWriter(csvFilepath, true))
            {
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.WriteRecords(xGmailLabelComboCounts);
            }

        }

        private void WriteAccountHeaderFields(XmlWriter xwriter, string globalId, string accntEmails = "")
        {
            {
                Settings.WriteSettings( xwriter );

                xwriter.WriteProcessingInstruction("DateGenerated", DateTime.Now.ToString("u"));

                xwriter.WriteStartElement("Account", XM_NS);
                xwriter.WriteAttributeString("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance", "eaxs_schema_v2.xsd");
                foreach (var addr in accntEmails.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    xwriter.WriteElementString("EmailAddress", XM_NS, addr);
                }
                xwriter.WriteElementString("GlobalId", XM_NS, globalId);
            }
        }


        private long ProcessEml(MessageFileProperties msgFileProps, ref XmlWriter xwriter, ref Stream xstream, long localId, List<MessageBrief> messageList)
        {
            if (msgFileProps.MessageFormat != MimeFormat.Entity)
            {
                throw new Exception($"Unexpected MessageFormat '{msgFileProps.MessageFormat}'; skipping file ");
            }

            return ProcessFile(msgFileProps, ref xwriter, ref xstream, localId, messageList);
        }

        private long ProcessMbox(MessageFileProperties msgFileProps, ref XmlWriter xwriter, ref Stream xstream, long localId, List<MessageBrief> messageList)
        {
            if (msgFileProps.MessageFormat != MimeFormat.Mbox)
            {
                throw new Exception($"Unexpected MessageFormat '{msgFileProps.MessageFormat}'; skipping file ");
            }

            WriteFolderOpen(xwriter, msgFileProps);

            long retId = ProcessFile(msgFileProps, ref xwriter, ref xstream, localId, messageList);

            if (Settings.IncludeSubFolders)
            {
                retId = ProcessChildMboxes(msgFileProps, ref xwriter, ref xstream, retId, messageList);
            }

            WriteMessageFileProperties(xwriter, msgFileProps, "FolderProperties");
            WriteFolderClose(xwriter);


            return retId;
        }

        private long ProcessFile(MessageFileProperties msgFileProps, ref XmlWriter xwriter, ref Stream xstream, long localId, List<MessageBrief> messageList)
        {
            msgFileProps.ResetEolCounts(); //reset counts to zero for each file

            //Keep track of properties for an individual message, such as Eol and Hash
            MimeMessageProperties mimeMsgProps = new();

            //open filestream and wrap it in a cryptostream so that we can hash the file as we process it
            //TODO: Put inside try catch in case of IO error
            using FileStream mboxStream = new(msgFileProps.MessageFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (msgFileProps.MessageFormat == MimeFormat.Mbox && Helpers.MimeKitHelpers.IsStreamAnMbx(mboxStream))
            {
                //This is a Pine *mbx* file, so it requires special parsing
                WriteToLogInfoMessage(xwriter, $"File '{msgFileProps.MessageFilePath}' is a Pine *mbx* file; using an alternate parsing strategy");

                using var mbxParser = new MbxParser(mboxStream, msgFileProps.HashAlgorithm);

                mbxParser.MimeMessageEnd += (sender, e) => MimeMessageEndEventHandler(sender, e, mboxStream, msgFileProps, mimeMsgProps, true);

                MimeMessage? message = null;

                while (!mbxParser.IsEndOfStream)
                {
                    if (Settings.MaximumXmlFileSize > 0 && xstream.Position >= Settings.MaximumXmlFileSize * MAX_FILE_SIZE_THRESHOLD)
                    {
                        //close the current xml file and open a new one
                        StartOverflowXmlFile(ref xwriter, ref xstream, msgFileProps);
                    }

                    try
                    {
                        message = mbxParser.ParseMessage();
                    }
                    catch (FormatException)
                    {
                        WriteToLogWarningMessage(xwriter, $"The mbx file was improperly formatted. The previous message might be prematurely truncated or it might include parts of two or more messages.");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        WriteToLogErrorMessage(xwriter, $"{ex.GetType().Name}: {ex.Message}");
                        break; //some error we probably can't recover from, so just bail
                    }

                    if (message != null)
                    {
                        _logger.LogTrace("Mbx Message Header: {header}", mbxParser.CurrentHeader?.Header);
                        if (!string.IsNullOrWhiteSpace(mbxParser.OverflowText))
                        {
                            WriteToLogWarningMessage(xwriter, "Because of the previous issue there is unprocessed overflow text.  See the comment below:");
                            xwriter.WriteComment(mbxParser.OverflowText);
                        }

                        //Need to include the mbxParser.CurrentHeader in the MimeMessageProperties, so we can use it later to determine the message statuses
                        mimeMsgProps.MbxMessageHeader = mbxParser.CurrentHeader;
                        localId = ProcessCurrentMessage(message, xwriter, localId, messageList, msgFileProps, mimeMsgProps);
                        msgFileProps.MessageCount++;
                    }
                    if (allDone)
                    {
                        WriteToLogMessage(xwriter, $"Skipping to the end", LogLevel.Debug);
                        break;
                    }
                }

                //make sure to read to the end of the stream so the hash is correct
                FilePathHelpers.ReadToEnd(mbxParser.CryptoStream);

            }
            else
            {
                using CryptoStream cryptoStream = new(mboxStream, msgFileProps.HashAlgorithm, CryptoStreamMode.Read);

                var mboxParser = new MimeParser(cryptoStream, msgFileProps.MessageFormat);

                mboxParser.MimeMessageEnd += (sender, e) => MimeMessageEndEventHandler(sender, e, mboxStream, msgFileProps, mimeMsgProps, false);

                //Need to record the previous message so we can defer writing it to the XML until the next message can be interogated for error conditions 
                //and we can add the <Incomplete> tag if needed.
                MimeMessage? prevMessage = null;
                MessageFileProperties? prevMsgFileProps = null;
                MimeMessageProperties? prevMimeMsgProps = null;
                MimeMessage? message = null;

                while (!mboxParser.IsEndOfStream)
                {
                    if (Settings.MaximumXmlFileSize > 0 && xstream.Position >= Settings.MaximumXmlFileSize * MAX_FILE_SIZE_THRESHOLD)
                    {
                        //close the current xml file and open a new one
                        StartOverflowXmlFile(ref xwriter, ref xstream, msgFileProps);
                    }

                    try
                    {

                        message = mboxParser.ParseMessage();

                        if (message.Headers.Count == 0)
                        {
                            //If an arbitrary '^From ' line is found with a blank line after it, the parser assumes that it is a valid message without any headers, but just message content
                            //This will almost certainly indicate an invalid mbox file, throw an error and try to keep going
                            throw new FormatException(EX_MBOX_PARSE_HEADERS);
                        }

                    }
                    catch (FormatException fex1) when (fex1.Message.Contains(EX_MBOX_FROM_MARKER, StringComparison.OrdinalIgnoreCase))
                    {
                        if (msgFileProps.MessageCount == 0)
                        {
                            WriteToLogWarningMessage(xwriter, $"{fex1.Message} -- skipping file, probably not an mbox file");
                            //return localId; //the file probably isn't an mbox file, so just bail on the whole file
                            break;
                        }
                        else
                        {
                            WriteToLogErrorMessage(xwriter, $"{fex1.Message} -- this is unexpected");
                            mboxParser.SetStream(cryptoStream, MimeFormat.Mbox); //reset the parser and try to continue
                            continue; //skip the message, but keep going
                        }
                    }
                    catch (FormatException fex2) when (fex2.Message.Contains(EX_MBOX_PARSE_HEADERS, StringComparison.OrdinalIgnoreCase))
                    {
                        //This is thrown when the parser discovers a '^From ' line followed by non-blank lines which are not valid headers
                        //Or when a message has no headers, see above "if (message.Headers.Count == 0)"

                        //if there have been some messages found, we probably encountered an unmangled 'From ' line in the message body, so create an incomplete message (the previous message is the one which is probably incomplete
                        if (msgFileProps.MessageCount > 0)
                        {
                            var msg = $"{fex2.Message} The content of the message is probably incomplete because of an unmangled 'From ' line in the message body. Content starting from offset {mboxParser.MboxMarkerOffset} to the beginning of the next message will be skipped.";
                            _logger.LogWarning(msg);
                            mimeMsgProps.Incomplete(msg, $"Stream Position: {mboxParser.MboxMarkerOffset}");

                            //FUTURE: Maybe try to recover lost message content when this happens.  This is probably very tricky except for the most basic cases of content-type: text/plain with no multipart messages or binary attachments
                        }

                        mboxParser.SetStream(cryptoStream, MimeFormat.Mbox); //reset the parser and try to continue
                        continue; //skip the message, but keep going
                    }
                    catch (Exception ex)
                    {
                        WriteToLogErrorMessage(xwriter, $"{ex.GetType().Name}: {ex.Message}");
                        break; //some error we probably can't recover from, so just bail
                    }

                    if (prevMessage != null && prevMsgFileProps != null && prevMimeMsgProps != null)
                    {
                        localId = ProcessCurrentMessage(prevMessage, xwriter, localId, messageList, prevMsgFileProps, prevMimeMsgProps);
                        msgFileProps.MessageCount++;
                        mimeMsgProps.NotIncomplete();
                    }
                    else if (msgFileProps.MessageCount > 0 && prevMessage == null)
                    {
                        WriteToLogErrorMessage(xwriter, "Message is null");
                    }

                    prevMessage = message;
                    prevMsgFileProps = (MessageFileProperties?)msgFileProps.Clone();
                    prevMimeMsgProps = (MimeMessageProperties?)mimeMsgProps.Clone();

                    if (allDone)
                    {
                        WriteToLogMessage(xwriter, $"Skipping to the end", LogLevel.Debug);
                        break;
                    }

                }

                if (message != null)
                {
                    //process the last message
                    localId = ProcessCurrentMessage(message, xwriter, localId, messageList, msgFileProps, mimeMsgProps);
                    msgFileProps.MessageCount++;
                }

                //make sure to read to the end of the stream so the hash is correct
                FilePathHelpers.ReadToEnd(cryptoStream);

                cryptoStream.Close();
            }

            mboxStream.Close();

            return localId;
        }

        private (string relPath, string ext) GetRelPathAndExt(MessageFileProperties msgFileProps)
        {
            var relPath = msgFileProps.RelativePath;
            var ext = msgFileProps.Extension;
            if (string.IsNullOrWhiteSpace(ext))
                ext = Settings.DefaultFileExtension;
            return (relPath, ext);
        }

        private void WriteMessageFileProperties(XmlWriter xwriter, MessageFileProperties msgFileProps, string wrapperElement)
        {
            if (!msgFileProps.AlreadySerialized)
            {
                xwriter.WriteStartElement(wrapperElement, XM_NS);

                (string relPath, string ext) = GetRelPathAndExt(msgFileProps);
                xwriter.WriteElementString("RelPath", XM_NS, relPath.ToString().Replace('\\', '/'));
                xwriter.WriteElementString("FileExt", XM_NS, ext);

                xwriter.WriteElementString("Eol", XM_NS, msgFileProps.MostCommonEol);
                if (msgFileProps.UsesDifferentEols)
                {
                    msgFileProps.EolCounts.TryGetValue("CR", out int crCount);
                    msgFileProps.EolCounts.TryGetValue("LF", out int lfCount);
                    msgFileProps.EolCounts.TryGetValue("CRLF", out int crlfCount);
                    WriteToLogWarningMessage(xwriter, $"Mbox file contains multiple different EOLs: CR: {crCount}, LF: {lfCount}, CRLF: {crlfCount}");
                }
                if (msgFileProps.HashAlgorithm.Hash != null)
                {
                    WriteHash(xwriter, msgFileProps.HashAlgorithm.Hash, msgFileProps.HashAlgorithmName);
                }
                else
                {
                    WriteToLogWarningMessage(xwriter, $"Unable to calculate the hash value for the Mbox");
                }

                if (msgFileProps.FileSize >= 0)
                {
                    xwriter.WriteElementString("Size", XM_NS, msgFileProps.FileSize.ToString());
                }
                else
                {
                    WriteToLogWarningMessage(xwriter, $"Unable to determine the size of the Mbox");
                }

                xwriter.WriteElementString("MessageCount", XM_NS, msgFileProps.MessageCount.ToString());

                xwriter.WriteEndElement(); //wrapperElement

                if (Settings.OneFilePerMessageFile)
                    msgFileProps.AlreadySerialized = true;
            }
        }

        private long ProcessEmlSubfolders(MessageFileProperties msgFileProps, ref XmlWriter xwriter, ref Stream xstream, long localId, List<MessageBrief> messageList)
        {
            if (msgFileProps.MessageFormat != MimeFormat.Entity)
            {
                throw new Exception($"Unexpected MessageFormat '{msgFileProps.MessageFormat}'; skipping file ");
            }

            //When processing EML files, process all subfolders whether they match the filename or not.

            string[]? dirs = null;
            try
            {
                dirs = Directory.GetDirectories(msgFileProps.MessageDirectoryName);
            }
            catch (Exception ex)
            {
                WriteToLogErrorMessage(xwriter, $"Skipping subfolders. {ex.GetType().Name}: {ex.Message}");
                dirs = null;
            }

            if (dirs != null)
            {
                foreach (var dir in dirs)
                {
                    _logger.LogInformation("Processing Subfolder: {subfolderName}", dir);

                    //copy the message file properties into new class instance so we can change the file path
                    MessageFileProperties msgFileProps2 = new(msgFileProps);
                    msgFileProps2.MessageFilePath = Path.Combine(dir, Guid.NewGuid().ToString()); // use Guid to ensure its not a real file; this is a dummy file path that will be overwritten in the foreach loop below, but is needed to initialize the MessageFileProperties object with the correct file directory

                    WriteFolderOpen(xwriter, msgFileProps2);

                    string[]? files = null;
                    try
                    {
                        files = Directory.GetFiles(dir);
                    }
                    catch (Exception ex)
                    {
                        WriteToLogErrorMessage(xwriter, $"Skipping this subfolder. {ex.GetType().Name}: {ex.Message}");
                    }

                    if (files != null && files.Length > 0)
                    {

                        //this is all the files, so need to determine which ones are mbox files or not
                        foreach (var file in files)
                        {
                            WriteToLogInfoMessage(xwriter, $"Processing EML file: {file}");
                            msgFileProps2.MessageFilePath = file;
                            msgFileProps2.MessageCount = 0;
                            localId = ProcessEml(msgFileProps2, ref xwriter, ref xstream, localId, messageList);
                        }
                    }

                    localId = ProcessEmlSubfolders(msgFileProps2, ref xwriter, ref xstream, localId, messageList);

                    WriteFolderClose(xwriter);
                }
            }

            return localId;
        }
        /// <summary>
        /// Look for a subfolder named the same as the mbox file ignoring extensions
        /// i.e. Mozilla Thunderbird will append the extension '.sbd' to the folder name
        /// </summary>
        /// <param name="msgFileProps"></param>
        /// <param name="xwriter"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private string? GetMboxSubfolderName(MessageFileProperties msgFileProps, ref XmlWriter xwriter)
        {
            if (msgFileProps.MessageFormat != MimeFormat.Mbox)
            {
                throw new Exception($"Unexpected MessageFormat '{msgFileProps.MessageFormat}'; skipping file ");
            }

            string? subfolderName = null;

            string[]? subfolders = Directory.GetDirectories(msgFileProps.MessageDirectoryName, $"{msgFileProps.MessageFileName}.*"); //first look for folders matching the parent name, including extension
            if(subfolders == null || subfolders.Length == 0)
            {
                //if not found, look for names matching the parent without extension
                subfolders = Directory.GetDirectories(msgFileProps.MessageDirectoryName, $"{Path.GetFileNameWithoutExtension(msgFileProps.MessageFileName)}.*");
            }

            if(subfolders != null)
            { 
                try
                {
                    subfolderName = subfolders.SingleOrDefault();
                }
                catch (InvalidOperationException)
                {
                    WriteToLogErrorMessage(xwriter, $"There is more than one folder that matches '{msgFileProps.MessageFileName}.*'; skipping all subfolders");
                    subfolderName = null;
                }
                catch (Exception ex)
                {
                    WriteToLogErrorMessage(xwriter, $"Skipping subfolders. {ex.GetType().Name}: {ex.Message}");
                    subfolderName = null;
                }
            }

            return subfolderName;
        }

        /// <summary>
        /// Child mbox files are contained in a subfolder with the same name as the parent mbox file (ignoring any file extension)
        /// Any mbox files in the subfolder are considered to be children folders of the parent mbox folder.
        /// 
        /// NOTE: The above is true for Mozilla Thunderbird mbox files.  Other mbox files may have different folder structures.
        /// For alternatives see: https://doc.dovecot.org/configuration_manual/mail_location/mbox/mboxchildfolders/ or https://access.redhat.com/articles/6167512
        /// TODO: Add accommodations for alternative mbox folder structures
        ///       See https://github.com/orgs/UIUCLibrary/projects/39/views/2?pane=issue&itemId=26477769
        /// </summary>
        /// <param name="msgFileProps"></param>
        /// <param name="xwriter"></param>
        /// <param name="xstream"></param>
        /// <param name="localId"></param>
        /// <param name="messageList"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private long ProcessChildMboxes(MessageFileProperties msgFileProps, ref XmlWriter xwriter, ref Stream xstream, long localId, List<MessageBrief> messageList)
        {
            if (msgFileProps.MessageFormat != MimeFormat.Mbox)
            {
                throw new Exception($"Unexpected MessageFormat '{msgFileProps.MessageFormat}'; skipping file ");
            }

            var subfolderName = GetMboxSubfolderName(msgFileProps, ref xwriter);

            if (!string.IsNullOrWhiteSpace(subfolderName))
            {
                _logger.LogInformation("Processing Subfolder: {subfolderName}", subfolderName);
                //look for mbox files in this subdirectory
                string[]? childMboxes = null;
                try
                {
                    childMboxes = Directory.GetFiles(subfolderName);
                }
                catch (Exception ex)
                {
                    WriteToLogErrorMessage(xwriter, $"Skipping this subfolder. {ex.GetType().Name}: {ex.Message}");
                }

                if (childMboxes != null && childMboxes.Length > 0)
                {

                    //this is all the files, so need to determine which ones are mbox files or not
                    //we just try to process it, invalid mbox files are logged as such in the output and skipped
                    foreach (var childMbox in childMboxes)
                    {
                        //create new MessageFileProperties which is copy of parent MessageFileProperties except for the MessageFilePath and the checksum hash
                        MessageFileProperties childMsgFileProps = new(msgFileProps)
                        {
                            MessageFilePath = childMbox
                        };
                        SetHashAlgorithm(childMsgFileProps, xwriter);

                        if (Settings.OneFilePerMessageFile)
                        {
                            childMsgFileProps.OutFilePath = Path.Combine(childMsgFileProps.OutDirectoryName, Path.GetFileName(Path.GetDirectoryName(childMbox) ?? ""), Path.GetFileName(childMbox)) + ".xml";
                            StartNewXmlFile(ref xwriter, ref xstream, msgFileProps, childMsgFileProps);
                        }
                        localId = ProcessMbox(childMsgFileProps, ref xwriter, ref xstream, localId, messageList);
                    }
                }

            }
            return localId;
        }

        /// <summary>
        /// Write a single Folder open tag and push it on the stack for later use
        /// </summary>
        /// <param name="xwriter"></param>
        /// <param name="msgFileProps"></param>
        private void WriteFolderOpen(XmlWriter xwriter, MessageFileProperties msgFileProps)
        {
            _folders.Push(msgFileProps.XmlFolderName);
            xwriter.WriteStartElement("Folder", XM_NS);
            xwriter.WriteElementString("Name", XM_NS, Path.GetFileNameWithoutExtension(msgFileProps.XmlFolderName));
        }

        /// <summary>
        /// Write a single Folder close tag and pop it off the stack
        /// </summary>
        /// <param name="xwriter"></param>
        private void WriteFolderClose(XmlWriter xwriter)
        {
            xwriter.WriteEndElement(); //Folder
            _folders.Pop();
        }


        /// <summary>
        /// Write all nested folders as needed according the _folders stack
        /// </summary>
        /// <param name="xwriter"></param>
        private void WriteFolderOpeners(XmlWriter xwriter)
        {
            WriteFolderOpeners(xwriter, _folders);
        }

        /// <summary>
        /// Write all nested folders as needed according the given nesting stack
        /// </summary>
        /// <param name="xwriter"></param>
        /// <param name="nesting"></param>
        private void WriteFolderOpeners(XmlWriter xwriter, Stack<string> nesting)
        {
            foreach (var fld in nesting.Reverse())
            {
                xwriter.WriteStartElement("Folder", XM_NS);
                xwriter.WriteElementString("Name", XM_NS, Path.GetFileNameWithoutExtension(fld));
            }
        }

        /// <summary>
        /// Close any open folder tags based on the _folders stack
        /// Do not pop or clearStack the stack though; it is still needed later
        /// </summary>
        /// <param name="xwriter"></param>
        private void WriteFolderClosers(XmlWriter xwriter)
        {
            WriteFolderClosers(xwriter, _folders, false);
        }

        /// <summary>
        /// Close any open folder tags based on the given nesting stack
        /// if clearStack is true, clearStack the stack
        /// </summary>
        /// <param name="xwriter"></param>
        /// <param name="nesting"></param>
        /// <param name="clearStack"></param>
        private void WriteFolderClosers(XmlWriter xwriter, Stack<string> nesting, bool clearStack)
        {
            for (int c = 0; c < nesting.Count; c++)
            {
                xwriter.WriteEndElement(); //Folder
            }
            if (clearStack)
                nesting.Clear();
        }

        /// <summary>
        /// In order to split an overly large file into multiple files, we need to start a new xml file
        /// Close the current xml file and start a new one
        /// </summary>
        /// <param name="xwriter"></param>
        /// <param name="xstream"></param>
        /// <param name="msgFileProps"></param>
        private void StartOverflowXmlFile(ref XmlWriter xwriter, ref Stream xstream, MessageFileProperties msgFileProps)
        {
            var origXmlFilePath = msgFileProps.OutFilePath;

            //increment the file number; this also updates the OutFilePath
            _ = msgFileProps.IncrementOutFileNumber();

            var newXmlFilePath = msgFileProps.OutFilePath;

            WriteFolderClosers(xwriter);

            xwriter.WriteProcessingInstruction("ContinuedIn", $"'{Path.GetFileName(newXmlFilePath)}'");

            //close the current xml file and start a new one
            xwriter.WriteEndDocument(); //should write out any unclosed elements
            xwriter.Flush();
            xwriter.Close(); //this should close the underlying stream
            xwriter.Dispose();
            xstream.Dispose();

            xstream = new FileStream(newXmlFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var xset = new XmlWriterSettings()
            {
                CloseOutput = true,
                Indent = true,
                Encoding = System.Text.Encoding.UTF8
            };
            xwriter = XmlWriter.Create(xstream, xset);

            xwriter.WriteStartDocument();
            WriteDocType(xwriter);
            xwriter.WriteProcessingInstruction("ContinuedFrom", $"'{Path.GetFileName(origXmlFilePath)}'");

            WriteAccountHeaderFields(xwriter, msgFileProps.GlobalId, msgFileProps.AccountEmails);
            WriteToLogInfoMessage(xwriter, $"Processing mbox file: {msgFileProps.MessageFilePath}");
            WriteFolderOpeners(xwriter);
        }

        /// <summary>
        /// In order to save different child mbox files into different xml files, we need to start a new xml file
        /// Close the current xml file and start a new one
        /// </summary>
        /// <param name="xwriter"></param>
        /// <param name="xstream"></param>
        /// <param name="oldMsgFileProps"></param>
        /// <param name="newMsgFileProps"></param>
        private void StartNewXmlFile(ref XmlWriter xwriter, ref Stream xstream, MessageFileProperties oldMsgFileProps, MessageFileProperties newMsgFileProps)
        {
            WriteMessageFileProperties(xwriter, oldMsgFileProps, "FolderProperties");
            WriteFolderClosers(xwriter);

            //close the current xml file and start a new one
            xwriter.WriteEndDocument(); //should write out any unclosed elements
            xwriter.Flush();
            xwriter.Close(); //this should close the underlying stream
            xwriter.Dispose();
            xstream.Dispose();

            if (!Directory.Exists(newMsgFileProps.OutDirectoryName))
            {
                Directory.CreateDirectory(newMsgFileProps.OutDirectoryName);
            }


            xstream = new FileStream(newMsgFileProps.OutFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var xset = new XmlWriterSettings()
            {
                CloseOutput = true,
                Indent = true,
                Encoding = System.Text.Encoding.UTF8
            };
            xwriter = XmlWriter.Create(xstream, xset);

            xwriter.WriteStartDocument();
            WriteDocType(xwriter);

            WriteAccountHeaderFields(xwriter, newMsgFileProps.GlobalId, newMsgFileProps.AccountEmails);
            WriteToLogInfoMessage(xwriter, $"Processing mbox file: {newMsgFileProps.MessageFilePath}");
            WriteFolderOpeners(xwriter);
        }

        /// <summary>
        /// Write a DocType declaration with entity definitions
        /// </summary>
        /// <param name="xwriter"></param>
        private void WriteDocType(XmlWriter xwriter)
        {
            //This is only needed if we use named entities in the XML
            //xwriter.WriteDocType("Account", null, null, "<!ENTITY % xhtml-lat1 PUBLIC \"-//W3C//ENTITIES Latin 1 for XHTML//EN\" \"xhtml-lat1.ent\" > %xhtml-lat1;");
        }

        private void SetHashAlgorithm(MessageFileProperties msgFileProps, XmlWriter xwriter)
        {
            var name = msgFileProps.TrySetHashAlgorithm(Settings.HashAlgorithmName);
            if (name != Settings.HashAlgorithmName)
            {
                WriteToLogWarningMessage(xwriter, $"The hash algorithm '{Settings.HashAlgorithmName}' is not supported.  Using '{name}' instead.");
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

        private long ProcessCurrentMessage(MimeMessage message, XmlWriter xwriter, long localId, List<MessageBrief> messageList, MessageFileProperties msgFileProps, MimeMessageProperties mimeMsgProps)
        {

            if (!string.IsNullOrWhiteSpace(message.MessageId) && message.MessageId == Settings.SkipUntilMessageId)
            {
                skippingMessages = false;
            }

            if (skippingMessages)
            {
                WriteToLogMessage(xwriter, $"Skipping message Id: {message.MessageId}", LogLevel.Debug);
                return localId;
            }

            localId++;
            var messageId = localId;

            xwriter.WriteStartElement("Message", XM_NS);

            localId = WriteMessage(xwriter, message, localId, false, true, msgFileProps, mimeMsgProps);

            xwriter.WriteEndElement(); //Message

            messageList.Add(new MessageBrief()
            {
                LocalId = messageId,
                From = message.From.ToString(),
                To = message.To.ToString(),
                Date = message.Date,
                Subject = message.Subject,
                MessageID = message.MessageId,
                Hash = Convert.ToHexString(mimeMsgProps.MessageHash, 0, mimeMsgProps.MessageHash.Length),
                Errors = (string.IsNullOrWhiteSpace(mimeMsgProps.IncompleteErrorType) && string.IsNullOrWhiteSpace(mimeMsgProps.IncompleteErrorLocation)) ? 0 : 1,
                FirstErrorMessage = $"{mimeMsgProps.IncompleteErrorLocation} {mimeMsgProps.IncompleteErrorType}".Trim()

            });

            mimeMsgProps.Eol = MimeMessageProperties.EOL_TYPE_UNK;

            if (!string.IsNullOrWhiteSpace(message.MessageId) && message.MessageId == Settings.SkipAfterMessageId)
            {
                skippingMessages = true;
                allDone = true;
            }

            return localId;
        }

        private long WriteMessage(XmlWriter xwriter, MimeMessage message, long localId, bool isChildMessage, bool expectingBodyContent, MessageFileProperties msgFileProps, MimeMessageProperties mimeMsgProps)
        {

            _logger.LogInformation("Converting {messageType} {localId} Id: {messageId} Subject: {subject}", isChildMessage ? "Child Message" : "Message", localId, message.MessageId, message.Subject);

            if (!isChildMessage)
            {
                xwriter.WriteElementString("RelPath", XM_NS, new Uri(Settings.ExternalContentFolder, UriKind.Relative).ToString().Replace('\\', '/'));
            }

            xwriter.WriteStartElement("LocalId", XM_NS);
            xwriter.WriteValue(localId);
            xwriter.WriteEndElement();

            //check for minimum required headers as per the XML schema, unless the message is a draft
            if (!isChildMessage && !MimeKitHelpers.TryGetDraft(message, mimeMsgProps, out _))
            {
                if (message.Headers[HeaderId.From] == null)
                {
                    WriteToLogWarningMessage(xwriter, "The message does not have a From header.");
                }

                if (message.Headers[HeaderId.To] == null && message.Headers[HeaderId.Cc] == null && message.Headers[HeaderId.Bcc] == null)
                {
                    WriteToLogWarningMessage(xwriter, "The message does not have a To, Cc, or Bcc.");
                }

                if (message.Headers[HeaderId.Date] == null)
                {
                    WriteToLogWarningMessage(xwriter, "The message does not have a Date header.");
                }
                else if (message.Headers[HeaderId.Date] != null && string.IsNullOrWhiteSpace(message.Headers[HeaderId.Date])) //Date is set to blank value
                {
                    WriteToLogWarningMessage(xwriter, "The message has a blank or empty Date header.");
                }
                else if (message.Headers[HeaderId.Date] != null && message.Date == DateTimeOffset.MinValue) //Date is set to the min value if the date is missing or cannot be parsed
                {
                    WriteToLogWarningMessage(xwriter, $"Unable to parse invalid date: '{message.Headers[HeaderId.Date]}'");
                }

            }
            else
            {
                if (message.Headers[HeaderId.From] == null && message.Headers[HeaderId.Subject] == null && message.Headers[HeaderId.Date] == null)
                {
                    WriteToLogWarningMessage(xwriter, "The child message does not have a From, Subject, or Date.");
                }
            }

            //collect stats on X-GMAIL_LABELS header
            var lbl = message.Headers["X-Gmail-Labels"];
            if (!string.IsNullOrEmpty(lbl))
            {
                xGmailLabelComboCounts.TryGetValue(lbl, out int combocount);
                xGmailLabelComboCounts[lbl] = combocount + 1;

                var lbls = lbl.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var l in lbls)
                {
                    xGmailLabelCounts.TryGetValue(l, out int count);
                    xGmailLabelCounts[l] = count + 1;
                }
            }

            WriteStandardMessageHeaders(xwriter, message);

            WriteAllMessageHeaders(xwriter, message);

            if (!isChildMessage)
            {
                WriteMessageStatuses(xwriter, message, mimeMsgProps);
            }

            localId = WriteMessageBody(xwriter, message.Body, localId, expectingBodyContent, msgFileProps, mimeMsgProps);

            if (!string.IsNullOrWhiteSpace(mimeMsgProps.IncompleteErrorType) || !string.IsNullOrWhiteSpace(mimeMsgProps.IncompleteErrorLocation))
            {
                xwriter.WriteStartElement("Incomplete", XM_NS);
                xwriter.WriteElementString("ErrorType", XM_NS, mimeMsgProps.IncompleteErrorType ?? "Unknown");
                xwriter.WriteElementString("ErrorLocation", XM_NS, mimeMsgProps.IncompleteErrorLocation ?? "Unknown");
                xwriter.WriteEndElement(); //Incomplete
            }

            if (!isChildMessage)
            {

                if (msgFileProps.MessageFormat == MimeFormat.Entity)
                {
                    //for single Entity messages, the count is always 1, but just in case we are processing a folder of multiple messages,
                    //remember the total count and reset after writing the properties
                    var tempCnt = msgFileProps.MessageCount;
                    msgFileProps.MessageCount = 1;
                    WriteMessageFileProperties(xwriter, msgFileProps, "MessageProperties");
                    msgFileProps.MessageCount = tempCnt;
                }
                else
                {
                    xwriter.WriteStartElement("MessageProperties", XM_NS);
                    xwriter.WriteElementString("Eol", XM_NS, mimeMsgProps.Eol);

                    WriteHash(xwriter, mimeMsgProps.MessageHash, Settings.HashAlgorithmName);

                    if (mimeMsgProps.MessageSize >= 0)
                    {
                        xwriter.WriteElementString("Size", XM_NS, mimeMsgProps.MessageSize.ToString());
                    }
                    else
                    {
                        WriteToLogWarningMessage(xwriter, $"Unable to determine the size of the Message");
                    }
                    xwriter.WriteEndElement(); //MessageProperties
                }
            }

            return localId;
        }

        private void WriteMessageStatuses(XmlWriter xwriter, MimeMessage message, MimeMessageProperties msgProps)
        {
            //StatusFlags
            if (MimeKitHelpers.TryGetSeen(message, msgProps, out string status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (MimeKitHelpers.TryGetAnswered(message, msgProps, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (MimeKitHelpers.TryGetFlagged(message, msgProps, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (MimeKitHelpers.TryGetDeleted(message, msgProps, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (MimeKitHelpers.TryGetDraft(message, msgProps, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (MimeKitHelpers.TryGetRecent(message, msgProps, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
        }

        private void WriteAllMessageHeaders(XmlWriter xwriter, MimeMessage message)
        {
            xwriter.WriteStartElement("Headers", XM_NS);
            foreach (var hdr in message.Headers.Where(h => h.Value != null)) //All headers that have values even if already covered above
            {
                xwriter.WriteStartElement("Header", XM_NS);

                xwriter.WriteElementString("Name", XM_NS, hdr.Field);

                //According to the XML schema, header values should be the raw headers, not converted to Unicode
                var rawValue = System.Text.Encoding.ASCII.GetString(hdr.RawValue);
                WriteElementStringReplacingInvalidChars(xwriter, "Value", XM_NS, rawValue.Trim());

                //UNSUPPORTED: Comments, not currently supported by MimeKit

                xwriter.WriteEndElement();
            }
            xwriter.WriteEndElement(); //Headers
        }

        private void WriteStandardMessageHeaders(XmlWriter xwriter, MimeMessage message)
        {

            xwriter.WriteElementString("MessageId", XM_NS, message.MessageId);

            if (message.MimeVersion != null)
            {
                xwriter.WriteElementString("MimeVersion", XM_NS, message.MimeVersion.ToString());
            }

            if (message.Date != DateTimeOffset.MinValue) //MinValue is used if the message does not have a date field
            {
                xwriter.WriteStartElement("OrigDate", XM_NS);
                xwriter.WriteValue(message.Date);
                xwriter.WriteEndElement();
            }

            if (message.Date != DateTimeOffset.MinValue && message.Date.Year < 1971)
            {
                WriteToLogWarningMessage(xwriter, $"The first email was sent in 1971.  '{message.Date:u}' must be an invalid date.");
            }
            else if (message.Date.Year > DateTime.Now.Year)
            {
                WriteToLogWarningMessage(xwriter, $"This is an unlikely email from the future.  '{message.Date:u}' must be an invalid date.");
            }

            WriteInternetAddressList(xwriter, message.From, "From");

            if (message.From != null && message.From.Count > 1 && message.Sender == null)
            {
                WriteToLogWarningMessage(xwriter, "The message has multiple From addresses but no Sender.");
            }

            if (message.Sender != null)
            {
                WriteMailboxAddress(xwriter, message.Sender, "Sender");
            }

            WriteInternetAddressList(xwriter, message.To, "To");

            WriteInternetAddressList(xwriter, message.Cc, "Cc");

            WriteInternetAddressList(xwriter, message.Bcc, "Bcc");

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
                WriteElementStringReplacingInvalidChars(xwriter, "Subject", XM_NS, message.Subject);
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

        private void WriteInternetAddressList(XmlWriter xwriter, InternetAddressList addrList, string? localName)
        {
            if (addrList != null && addrList.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(localName))
                    xwriter.WriteStartElement(localName, XM_NS);

                foreach (var addr in addrList)
                {
                    WriteAddress(xwriter, addr);
                }

                if (!string.IsNullOrWhiteSpace(localName))
                    xwriter.WriteEndElement(); //localname
            }
        }

        private void WriteInternetAddressList(XmlWriter xwriter, InternetAddressList addrList)
        {
            WriteInternetAddressList(xwriter, addrList, null);
        }



        private void WriteMailboxAddress(XmlWriter xwriter, MailboxAddress addr, string localName)
        {

            var addrStr = addr.ToString();
            if (XmlHelpers.TryReplaceInvalidXMLChars(ref addrStr, out string msg))
            {
                var warn = $"{localName} contains characters which are not allowed in XML; {msg}";
                WriteToLogWarningMessage(xwriter, warn);
            }
            xwriter.WriteStartElement(localName, XM_NS);
            if (!string.IsNullOrWhiteSpace(addr.Name))
            {
                xwriter.WriteAttributeString("name", XmlHelpers.ReplaceInvalidXMLChars(addr.Name));
            }
            if (!string.IsNullOrWhiteSpace(addr.Address))
            {
                xwriter.WriteAttributeString("address", XmlHelpers.ReplaceInvalidXMLChars(addr.Address));
            }
            xwriter.WriteString(addrStr);
            xwriter.WriteEndElement(); //localName
        }

        private void WriteAddress(XmlWriter xwriter, InternetAddress addr)
        {
            if (addr is GroupAddress ga)
            {
                xwriter.WriteStartElement("Group", XM_NS);
                xwriter.WriteElementString("Name", XM_NS, ga.Name);
                WriteInternetAddressList(xwriter, ga.Members);
                xwriter.WriteEndElement(); //Group
            }
            else if (addr is MailboxAddress ma)
            {
                WriteMailboxAddress(xwriter, ma, "Mailbox");
            }
            else
            {
                throw new ArgumentException($"Unexpected address type: {addr.GetType().Name}");
            }
        }

        private long WriteMessageBody(XmlWriter xwriter, MimeEntity mimeEntity, long localId, bool expectingBodyContent, MessageFileProperties msgFileProps, MimeMessageProperties mimeMsgProps)
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
                WriteToLogWarningMessage(xwriter, $"Unexpected MIME Entity Type: '{mimeEntity.GetType().FullName}' -- '{mimeEntity.ContentType.MimeType}'");
                xwriter.WriteStartElement("SingleBody", XM_NS);
            }


            xwriter.WriteStartAttribute("IsAttachment");
            xwriter.WriteValue(mimeEntity.IsAttachment);
            xwriter.WriteEndAttribute();

            //check for invalid content-type and content-transfer-encoding combinations
            ContentEncoding[] allowedEnc = { ContentEncoding.SevenBit, ContentEncoding.EightBit, ContentEncoding.Binary };
            if (mimeEntity.ContentType.IsMimeType("message", "rfc822") && part != null && message == null && !allowedEnc.Contains(part.ContentTransferEncoding))
            {
                //FROM THE MIMEKIT DOCUMENTATION
                // Note: message/rfc822 and message/partial are not allowed to be encoded according to rfc2046
                // (sections 5.2.1 and 5.2.2, respectively). Since some broken clients will encode them anyway,
                // it is necessary for us to treat those as opaque blobs instead, and thus the parser should
                // parse them as normal MimeParts instead of MessageParts.
                WriteToLogWarningMessage(xwriter, $"A 'message/rfc822' part was found with an unallowable content-transfer-encoding of '{MimeKitHelpers.GetContentEncodingString(part.ContentTransferEncoding)}'.  The part will be treated as normal content instead of as a child message.");
            }

            WriteMimeContentType(xwriter, mimeEntity, isMultipart);

            WriteMimeOtherStandardHeaders(xwriter, mimeEntity, isMultipart);

            WriteMimeContentDisposition(xwriter, mimeEntity);

            if (mimeEntity.Headers[HeaderId.ContentLanguage] != null)
            {
                foreach (var hdr in mimeEntity.Headers.Where(h => h.Id == HeaderId.ContentLanguage))
                {
                    WriteElementStringReplacingInvalidChars(xwriter, "ContentLanguage", XM_NS, hdr.Value);
                }
            }

            WriteMimeOtherHeaders(xwriter, mimeEntity);

            if (isMultipart && multipart != null && !string.IsNullOrWhiteSpace(multipart.Preamble))
            {
                WriteElementStringReplacingInvalidChars(xwriter, "Preamble", XM_NS, multipart.Preamble.Trim());
            }

            if (isMultipart && multipart != null && multipart.Count > 0)
            {
                foreach (var item in multipart)
                {
                    localId = WriteMessageBody(xwriter, item, localId, true, msgFileProps, mimeMsgProps);
                }
            }
            else if (isMultipart && multipart != null && multipart.Count == 0)
            {
                WriteToLogWarningMessage(xwriter, $"Item is multipart, but there are no parts");
                //need to write an empty part so that XML is schema valid
                xwriter.WriteStartElement("MissingBody", XM_NS);
                xwriter.WriteEndElement(); //MissingBody
            }
            else if (isMultipart && multipart == null)
            {
                WriteToLogWarningMessage(xwriter, $"Item is erroneously flagged as multipart");
                //need to write an empty part so that XML is schema valid
                xwriter.WriteStartElement("MissingBody", XM_NS);
                xwriter.WriteEndElement(); //MissingBody
            }
            else if (!isMultipart)
            {
                if (mimeEntity is TnefPart tnefPart)
                {
                    //FUTURE: Instead of treating TNEF as a ChildMessage, maybe treat the same as a multipart mime type
                    var tnefMsg = tnefPart.ConvertToMessage();
                    localId = WriteSingleBodyChildMessage(xwriter, tnefMsg, localId, expectingBodyContent, msgFileProps, mimeMsgProps);
                }
                else if (mimeEntity is MessageDeliveryStatus deliveryStatus)
                {
                    localId = WriteDeliveryStatus(xwriter, deliveryStatus, localId, msgFileProps);
                }
                else if (part != null && !MimeKitHelpers.IsXMozillaExternalAttachment(part))
                {
                    localId = WriteSingleBodyContent(xwriter, part, localId, expectingBodyContent, msgFileProps);
                }
                else if (part != null && MimeKitHelpers.IsXMozillaExternalAttachment(part))
                {
                    WriteToLogInfoMessage(xwriter, "The content is an inaccessible external attachment");
                }
                else if (message != null)
                {
                    expectingBodyContent = true;
                    if (message.ContentType.IsMimeType("text", "rfc822-headers"))
                    {
                        expectingBodyContent = false;
                    }
                    localId = WriteSingleBodyChildMessage(xwriter, message, localId, expectingBodyContent, msgFileProps, mimeMsgProps);
                }
                else
                {
                    WriteToLogWarningMessage(xwriter, $"Unexpected MimeEntity: {mimeEntity.GetType().FullName}");
                }
            }
            else
            {
                WriteToLogWarningMessage(xwriter, $"Unexpected MimeEntity: {mimeEntity.GetType().FullName}");
            }

            //PhantomBody; Content-Type message/external-body
            //Mozilla uses different headers to indicate this situtation, for example:
            //      X-Mozilla-External-Attachment-URL: file://///libgrrudra/Users/thabing/My%20Documents/EMAILS/Attachments/DLFSCHOLARS2004-2.pdf
            //      X-Mozilla-Altered: AttachmentDetached; date = "Fri May 19 09:27:32 2006"
            //NEEDSTEST:  Find or construct a sample message with content-type message/external-body

            if (!isMultipart && part != null && (part.ContentType.IsMimeType("message", "external-body") || MimeKitHelpers.IsXMozillaExternalAttachment(part)))
            {
                var streamReader = new StreamReader(part.Content.Open(), part.ContentType.CharsetEncoding, true);
                xwriter.WriteElementString("PhantomBody", XM_NS, streamReader.ReadToEnd());
            }


            if (isMultipart && multipart != null && !string.IsNullOrWhiteSpace(multipart.Epilogue))
            {
                WriteElementStringReplacingInvalidChars(xwriter, "Epilogue", XM_NS, multipart.Epilogue.Trim());

                //The Entity parser will correctly parse the first message in the file, but will fail to parse the second message,
                //instead the Epilogue of the first messages body will contain all the text from the remaining messages
                if (msgFileProps.MessageFormat == MimeFormat.Entity && multipart.Epilogue.Trim().StartsWith("From ", StringComparison.Ordinal) && multipart.Epilogue.Trim().Length > EPILOGUE_THRESHOLD)
                {
                    WriteToLogErrorMessage(xwriter, "Unexpected Epilogue:  The file may be corrupt, or you may be parsing it as a single EML when it is actually an mbox file containing multiple messages.");
                }
            }


            xwriter.WriteEndElement(); //SingleBody or MultiBody 

            return localId;
        }

        /// <summary>
        /// Write the delivery status information, see RFC 3464
        /// </summary>
        /// <param name="xwriter"></param>
        /// <param name="deliveryStatus"></param>
        /// <param name="localId"></param>
        /// <param name="msgFileProps"></param>
        /// <returns></returns>
        private long WriteDeliveryStatus(XmlWriter xwriter, MessageDeliveryStatus deliveryStatus, long localId, MessageFileProperties msgFileProps)
        {
            //Deal with malformed delivery status messages, instead just write a warning and then WriteSingleBodyContent
            if (deliveryStatus.StatusGroups.Count < 2)
            {
                WriteToLogWarningMessage(xwriter, $"Delivery status message is malformed. It should have at least 2 status groups; it has only {deliveryStatus.StatusGroups.Count}. Writing message as a single body content instead.");
                return WriteSingleBodyContent(xwriter, deliveryStatus, localId, true, msgFileProps);
            }

            xwriter.WriteStartElement("DeliveryStatus", XM_NS);

            xwriter.WriteStartElement("MessageFields", XM_NS);
            foreach (var grp in deliveryStatus.StatusGroups[0])
            {
                xwriter.WriteStartElement("Field", XM_NS);
                xwriter.WriteElementString("Name", XM_NS, grp.Field);
                xwriter.WriteElementString("Value", XM_NS, grp.Value);
                //UNSUPPORTED: Comments, not currently supported by MimeKit
                xwriter.WriteEndElement(); //Field
            }
            xwriter.WriteEndElement(); //MessageFields

            for (int i = 1; i < deliveryStatus.StatusGroups.Count; i++)
            {
                if (deliveryStatus.StatusGroups[i].Count > 0)
                {
                    xwriter.WriteStartElement("RecipientFields", XM_NS);
                    foreach (var grp in deliveryStatus.StatusGroups[i])
                    {
                        xwriter.WriteStartElement("Field", XM_NS);
                        xwriter.WriteElementString("Name", XM_NS, grp.Field);
                        xwriter.WriteElementString("Value", XM_NS, grp.Value);
                        //UNSUPPORTED: Comments, not currently supported by MimeKit
                        xwriter.WriteEndElement(); //Field
                    }
                    xwriter.WriteEndElement(); //RecipientFields
                }
            }

            xwriter.WriteEndElement(); //DeliveryStatus

            return localId;
        }

        private long WriteSingleBodyChildMessage(XmlWriter xwriter, MessagePart message, long localId, bool expectingBodyContent, MessageFileProperties msgFileProps, MimeMessageProperties mimeMsgProps)
        {
            //The message parameter might contain a MessagePart or its subclass TextRfc822Headers
            //If it is TextRfc822Headers it will not have a MessageBody.  This is handle correctly in the WriteMessageBody function 
            xwriter.WriteStartElement("ChildMessage", XM_NS);
            localId++;

            localId = WriteMessage(xwriter, message.Message, localId, true, expectingBodyContent, msgFileProps, mimeMsgProps);
            xwriter.WriteEndElement(); //ChildMessage
            return localId;
        }

        private long WriteSingleBodyChildMessage(XmlWriter xwriter, MimeMessage message, long localId, bool expectingBodyContent, MessageFileProperties msgFileProps, MimeMessageProperties mimeMsgProps)
        {
            //The message parameter might contain a MessagePart or its subclass TextRfc822Headers
            //If it is TextRfc822Headers it will not have a MessageBody.  This is handle correctly in the WriteMessageBody function 
            xwriter.WriteStartElement("ChildMessage", XM_NS);
            localId++;

            localId = WriteMessage(xwriter, message, localId, true, expectingBodyContent, msgFileProps, mimeMsgProps);
            xwriter.WriteEndElement(); //ChildMessage
            return localId;
        }

        private long WriteSingleBodyContent(XmlWriter xwriter, MimePart part, long localId, bool expectingBodyContent, MessageFileProperties msgFileProps)
        {

            //if it is text and not an attachment, save embedded in the XML
            //FUTURE: Maybe accomodate txtPart.IsEnriched || txtPart.IsRichText instead of treating them as non-text
            if (part is TextPart txtPart && (txtPart.IsPlain || txtPart.IsHtml) && !part.IsAttachment)
            {
                var (text, encoding, warning) = MimeKitHelpers.GetContentText(part);

                if (!string.IsNullOrWhiteSpace(text) || expectingBodyContent)
                {
                    xwriter.WriteStartElement("BodyContent", XM_NS);

                    if (!string.IsNullOrWhiteSpace(text) && !expectingBodyContent)
                    {
                        WriteToLogWarningMessage(xwriter, $"Not expecting body content for '{part.ContentType.MimeType}'.");
                    }

                    if (Settings.SaveTextAsXhtml)
                    {
                        var xhtml = MimeKitHelpers.GetTextAsXhtml(txtPart, out List<(LogLevel level, string message)> messages);
                        WriteToLogMessages(xwriter, messages);

                        if (string.IsNullOrWhiteSpace(xhtml) || messages.Any(m => m.level == LogLevel.Error || m.level == LogLevel.Critical))
                        {
                            //if the html is blank or if there were serious errors converting to xhtml, we cannot save the content as xhtml
                            WriteContentCData(xwriter, text, encoding, warning);
                        }
                        else
                        {
                            WriteContentAsXhtmlRaw(xwriter, xhtml);
                        }
                    }
                    else
                    {
                        WriteContentCData(xwriter, text, encoding, warning);
                    }


                    xwriter.WriteEndElement(); //BodyContent
                }
            }
            else //it is not text or it is an attachment
            {
                //FUTURE:  Need to see if we can access and process 'message/external-body' parts where the content is referenced by the other content-type parameters
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
                    localId = SerializeContentInExtFile(part, xwriter, localId, msgFileProps);
                }
            }
            return localId;
        }

        private void WriteContentCData(XmlWriter xwriter, string text, string encoding, string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                WriteToLogWarningMessage(xwriter, warning);
            }

            text = UnicodeHelpers.ReplacePuaChars(text, out List<(LogLevel level, string message)> messages);
            WriteToLogMessages(xwriter, messages);

            //text = FontHelper.PreventLigatures(text);

            xwriter.WriteStartElement("Content", XM_NS);
            xwriter.WriteCData(text);
            xwriter.WriteEndElement(); //Content

            if (!string.IsNullOrWhiteSpace(encoding))
            {
                xwriter.WriteElementString("TransferEncoding", XM_NS, encoding);
                //just write this to the xml since there is already a warning in the log about invalid characters
                xwriter.WriteComment($"WARNING: Used '{encoding}' because the content contains characters that are not valid in XML");
            }

        }

        private void WriteContentAsXhtmlRaw(XmlWriter xwriter, string text)
        {
            text = UnicodeHelpers.ReplacePuaChars(text, out List<(LogLevel level, string message)> messages);
            WriteToLogMessages(xwriter, messages);

            //text = FontHelper.PreventLigatures(text);

            xwriter.WriteStartElement("ContentAsXhtml", XM_NS);
            try
            {
                xwriter.WriteRaw(text);
            }
            catch (Exception)
            {
                throw;
            }
            xwriter.WriteEndElement(); //ContentAsXhtml
        }

        private void WriteMimeOtherStandardHeaders(XmlWriter xwriter, MimeEntity mimeEntity, bool isMultipart)
        {
            MimePart? part = mimeEntity as MimePart;

            //MimeKit only exposes Content-Transfer-Encoding as a property for single body messages.
            //According to specs it can be used for multipart entities, but it must be 7bit, 8bit, or binary, and always 7bit for practical purposes.
            //Getting it directly from the Headers property to cover both cases since the XML schema allows it
            if (mimeEntity.Headers.Contains(HeaderId.ContentTransferEncoding))
            {
                var transferEncoding = mimeEntity.Headers[HeaderId.ContentTransferEncoding].ToLowerInvariant();
                xwriter.WriteElementString("TransferEncoding", XM_NS, transferEncoding);
                if (part != null && !MimeKitHelpers.ContentEncodings.Contains(transferEncoding, StringComparer.OrdinalIgnoreCase))
                {
                    WriteToLogWarningMessage(xwriter, $"The TransferEncoding '{transferEncoding}' is not a recognized standard; treating it as '{MimeKitHelpers.GetContentEncodingString(part.ContentTransferEncoding, "default")}'.");
                }
                if (isMultipart && !transferEncoding.Equals("7bit", StringComparison.InvariantCultureIgnoreCase))
                {
                    WriteToLogWarningMessage(xwriter, $"A multipart entity has a Content-Transfer-Encoding of '{transferEncoding}'; normally this should only be 7bit for multipart entities.");
                }
            }
            //UNSUPPORTED: TransferEncodingComments, not currently supported by MimeKit

            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentId))
            {
                xwriter.WriteElementString("ContentId", XM_NS, mimeEntity.ContentId);
            }
            //UNSUPPORTED: ContentIdComments, not currently supported by MimeKit, actually might not be allowed by the RFC - not sure if ContentId is a structured header type

            if (isMultipart && !string.IsNullOrWhiteSpace(part?.ContentDescription))
            {
                xwriter.WriteElementString("Description", XM_NS, part.ContentDescription);
            }
            //UNSUPPORTED: DescriptionComments, not currently supported by MimeKit, actually might not be allowed by the RFC since Description is not a structured header type
        }

        private void WriteMimeOtherHeaders(XmlWriter xwriter, MimeEntity mimeEntity)
        {
            string[] except = new string[] { "content-type", "content-transfer-encoding", "content-id", "content-description", "content-disposition" };
            foreach (var hdr in mimeEntity.Headers.Where(h => !except.Contains(h.Field, StringComparer.InvariantCultureIgnoreCase)))
            {
                xwriter.WriteStartElement("OtherMimeHeader", XM_NS);

                xwriter.WriteElementString("Name", XM_NS, hdr.Field);

                //According to the XML schema, header values should be the raw headers, not converted to Unicode
                var rawValue = System.Text.Encoding.ASCII.GetString(hdr.RawValue);
                WriteElementStringReplacingInvalidChars(xwriter, "Value", XM_NS, rawValue.Trim());

                //UNSUPPORTED: OtherMimeHeader/Comments, not currently supported by MimeKit

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
                    WriteElementStringReplacingInvalidChars(xwriter, "DispositionFileName", XM_NS, mimeEntity.ContentDisposition.FileName);
                }

                //UNSUPPORTED: DispositionComments, not currently supported by MimeKit

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
                contentTypeCounts.TryGetValue(mimeEntity.ContentType.MimeType.ToLowerInvariant(), out int count);
                contentTypeCounts[mimeEntity.ContentType.MimeType.ToLowerInvariant()] = count + 1;
            }
            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentType.Charset))
            {
                xwriter.WriteElementString("Charset", XM_NS, mimeEntity.ContentType.Charset);
            }
            if (!string.IsNullOrWhiteSpace(mimeEntity.ContentType.Name))
            {
                WriteElementStringReplacingInvalidChars(xwriter, "ContentName", XM_NS, mimeEntity.ContentType.Name);
            }
            if (isMultipart && !string.IsNullOrWhiteSpace(mimeEntity.ContentType.Boundary))
            {
                xwriter.WriteElementString("BoundaryString", XM_NS, mimeEntity.ContentType.Boundary);
            }
            else if (!isMultipart && !string.IsNullOrWhiteSpace(mimeEntity.ContentType.Boundary))
            {
                //QUESTION: This seems somewhat common, and parsers will just ignore the extraneous (unnecessary?) boundary, so should it be a warning?  My answer is no.
                //WriteWarningMessage(xwriter, $"MIME type boundary parameter '{mimeEntity.ContentType.Boundary}' found for a non-multipart mime type '{mimeEntity.ContentType.MimeType}'");
            }
            else if (isMultipart && string.IsNullOrWhiteSpace(mimeEntity.ContentType.Boundary))
            {
                WriteToLogWarningMessage(xwriter, "MIME type boundary parameter is missing for a multipart mime type");
            }

            //UNSUPPORTED: ContentTypeComments, not currently supported by MimeKit

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
        /// <param name="extContent">if true, it is being written to an external file</param>
        /// <param name="localId">The local id of the content being written to an external file</param>
        private void SerializeContentInXml(MimePart part, XmlWriter xwriter, bool extContent, long localId)
        {
            var content = part.Content;

            xwriter.WriteStartElement("BodyContent", XM_NS);
            if (extContent)
            {
                xwriter.WriteAttributeString("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance", "eaxs_schema_v2.xsd");
                WriteToLogInfoMessage(xwriter, $"LocalId {localId} written to external file");
            }

            var actualEncoding = "";
            if (part.Headers.Contains(HeaderId.ContentTransferEncoding))
            {
                actualEncoding = part.Headers[HeaderId.ContentTransferEncoding].ToLowerInvariant();
            }

            if (!MimeKitHelpers.ContentEncodings.Contains(actualEncoding, StringComparer.OrdinalIgnoreCase))
            {
                WriteToLogWarningMessage(xwriter, $"The TransferEncoding '{actualEncoding}' is not a recognized standard; treating it as '{MimeKitHelpers.GetContentEncodingString(part.ContentTransferEncoding, "default")}'.");
            }

            xwriter.WriteStartElement("Content", XM_NS);

            //set up hashing
            using var cryptoHashAlg = HashAlgorithm.Create(Settings.HashAlgorithmName) ?? SHA256.Create();  //Fallback to known hash algorithm
            byte[]? hash;
            long? fileSize = null; 

            //get the byte array of the decoded content, also the size of the decoded content, and the hash of the decoded content
            byte[] byts;
            using MemoryStream ms = new();
            content.Open().CopyTo(ms); //get decoded stream and copy it to memory stream and then get an array
            byts = ms.ToArray();
            fileSize = byts.Length;  //actual length of decoded content
            hash = cryptoHashAlg.ComputeHash(byts);

            //reset stream positions to beginning
            ms.Position= 0; 
            content.Stream.Position = 0;

            string? encoding;
            //7bit and 8bit should be text content, so if preserving the encoding, decode it and use the streamreader with the contenttype charset, if any, to get the text and write it to the xml in a cdata section.  Default is the same as 7bit.
            if (Settings.PreserveTextAttachmentTransferEncoding && (content.Encoding == ContentEncoding.EightBit || content.Encoding == ContentEncoding.SevenBit || content.Encoding == ContentEncoding.Default))
            {
                using StreamReader reader = new(ms, part.ContentType.CharsetEncoding, true);
                xwriter.WriteCData(reader.ReadToEnd());
                encoding = String.Empty;
                if (content.Encoding == ContentEncoding.Default && !string.IsNullOrWhiteSpace(actualEncoding))
                {
                    WriteToLogWarningMessage(xwriter, $"Using the default transfer encoding.  The actual transfer encoding is '{actualEncoding}' and charset encoding '{part.ContentType.CharsetEncoding}'.");
                    encoding = actualEncoding;
                }
            }
            else if (Settings.PreserveBinaryAttachmentTransferEncodingIfPossible && (content.Encoding == ContentEncoding.UUEncode || content.Encoding == ContentEncoding.QuotedPrintable || content.Encoding == ContentEncoding.Base64))
            //use the original content encoding in the XML
            {
                var baseStream = content.Stream; //get un-decoded stream

                //treat the stream as ASCII because it is already encoded and just write it out using the same encoding
                using StreamReader reader = new(baseStream, System.Text.Encoding.ASCII);
                xwriter.WriteCData(reader.ReadToEnd());
                encoding = MimeKitHelpers.GetContentEncodingString(content.Encoding);
            }
            else //anything is treated as binary content, so copy to a memory stream and write it to the XML as base64
            {
                xwriter.WriteBase64(byts, 0, byts.Length);
                encoding = "base64";
            }

            xwriter.WriteEndElement(); //Content

            if (!string.IsNullOrWhiteSpace(encoding))
            {
                xwriter.WriteElementString("TransferEncoding", XM_NS, encoding);
            }

            if (hash != null)
            {
                WriteHash(xwriter, hash, Settings.HashAlgorithmName);
            }

            if (fileSize != null) 
            {
                xwriter.WriteElementString("Size", XM_NS, fileSize.ToString());
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
        /// <param name="msgFileProps">Properties needed to serialize the content</param>
        /// <returns>the new localId value after incrementing it for the new file</returns>
        /// <exception cref="Exception">thrown if unable to generate the hash</exception>
        private long SerializeContentInExtFile(MimePart part, XmlWriter xwriter, long localId, MessageFileProperties msgFileProps)
        {
            localId++;

            bool wrapInXml = Settings.WrapExternalContentInXml;

            string randomFilePath = FilePathHelpers.GetRandomFilePath(msgFileProps.OutDirectoryName);

            byte[] hash;
            long size;

            if (!wrapInXml)
            {
                hash = SaveContentAsRaw(randomFilePath, part, out size);

                //Try to open the file to see if it has a virus or there was some other IO problem
                try
                {
                    //this seems to trigger the virus scanner, but an IO error may not occur until we try to move the file later
                    using var testStream = new FileStream(randomFilePath, FileMode.Open, FileAccess.Read);
                    testStream.Close();
                }
                catch (IOException ioex)
                {
                    var msg = $"Raw content was not saved to external file.  {ioex.Message}  Will save it wrapped in XML instead.";
                    WriteToLogWarningMessage(xwriter, msg);
                    wrapInXml = true;
                    File.Delete(randomFilePath); //so we can try saving a new stream there
                    hash = SaveContentAsXml(randomFilePath, part, localId, msg, out size);
                }
            }
            else
            {
                hash = SaveContentAsXml(randomFilePath, part, localId, "", out size);
            }

            _logger.LogTrace("Created temporary file: '{randomFilePath}'", randomFilePath);

            var hashFileName = FilePathHelpers.GetOutputFilePathBasedOnHash(hash, part, Path.Combine(msgFileProps.OutDirectoryName, Settings.ExternalContentFolder), wrapInXml);
            //create folder if needed
            Directory.CreateDirectory(Path.GetDirectoryName(hashFileName) ?? "");

            //FUTURE: It might be good for performance to do this check prior to actually saving the temporary files
            //        Right now the hash is created by saving the temporary file, so this would require multiple passes through the stream, one to generate the hash and another to actually save it if needed

            //Deal with duplicate attachments, which should only be stored once, make sure the randomFilePath file is deleted
            if (File.Exists(hashFileName))
            {
                WriteToLogInfoMessage(xwriter, "Duplicate attachment has already been saved");
                File.Delete(randomFilePath);
            }
            else
            {
                try
                {
                    File.Move(randomFilePath, hashFileName);
                    _logger.LogTrace("File moved: '{randomFilePath}' -> '{hashFileName}'", randomFilePath, hashFileName);
                }
                catch (IOException ioex)
                {
                    var msg = $"Content was not saved to external file.  {ioex.Message}";
                    WriteToLogErrorMessage(xwriter, msg);
                }
            }

            xwriter.WriteStartElement("ExtBodyContent", XM_NS);
            xwriter.WriteElementString("RelPath", XM_NS, new Uri(Path.GetRelativePath(Path.Combine(msgFileProps.OutDirectoryName, Settings.ExternalContentFolder), hashFileName), UriKind.Relative).ToString().Replace('\\', '/'));

            //The CharSet and TransferEncoding elements are not needed here since they are same as for the SingleBody

            xwriter.WriteElementString("LocalId", XM_NS, localId.ToString());
            xwriter.WriteElementString("XMLWrapped", XM_NS, wrapInXml.ToString().ToLower());
            //Eol is not applicable since we are not wrapping the content in XML
            WriteHash(xwriter, hash, Settings.HashAlgorithmName);

            if (size >= 0)
            {
                xwriter.WriteElementString("Size", XM_NS, size.ToString());
            }
            else
            {
                WriteToLogWarningMessage(xwriter, $"Unable to determine the size of the external file");
            }

            xwriter.WriteEndElement(); //ExtBodyContent

            return localId;
        }

        /// <summary>
        /// Save the Mime Part Content as a raw file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="part"></param>
        /// <returns>the hash of the saved file</returns>
        /// <exception cref="Exception"></exception>
        private byte[] SaveContentAsRaw(string filePath, MimePart part, out long size)
        {
            var content = part.Content;

            using var contentStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
            using var cryptoHashAlg = HashAlgorithm.Create(Settings.HashAlgorithmName) ?? SHA256.Create();  //Fallback to known hash algorithm
            using var cryptoStream = new CryptoStream(contentStream, cryptoHashAlg, CryptoStreamMode.Write);

            content.DecodeTo(cryptoStream);

            size = contentStream.Length;
            cryptoStream.Close();
            contentStream.Close();

            if (cryptoHashAlg.Hash != null)
                return cryptoHashAlg.Hash;
            else
                throw new NullReferenceException($"Unable to calculate hash value for the content");
        }

        /// <summary>
        /// Save the Mime Part Content wrapped in an XML file
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="part"></param>
        /// <param name="localId"></param>
        /// <returns>the hash of the saved file</returns>
        /// <exception cref="Exception"></exception>
        private byte[] SaveContentAsXml(string filePath, MimePart part, long localId, string comment, out long size)
        {
            //TODO: It might be good to embellish the external XML schema, maybe make it equivalent to the internal XML schema SingleBody element
            //      This would provide more context if the external file is ever separated from the main XML email message file

            using var contentStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
            using var cryptoHashAlg = HashAlgorithm.Create(Settings.HashAlgorithmName) ?? SHA256.Create();  //Fallback to known hash algorithm
            using var cryptoStream = new CryptoStream(contentStream, cryptoHashAlg, CryptoStreamMode.Write);

            var extXmlWriter = XmlWriter.Create(cryptoStream, new XmlWriterSettings
            {
                Indent = true,
                Encoding = System.Text.Encoding.UTF8
            });
            extXmlWriter.WriteStartDocument();
            if (!string.IsNullOrWhiteSpace(comment))
            {
                extXmlWriter.WriteComment(comment);
            }
            SerializeContentInXml(part, extXmlWriter, true, localId);
            extXmlWriter.WriteEndDocument();
            extXmlWriter.Close();

            size = contentStream.Length;
            cryptoStream.Close();
            contentStream.Close();

            if (cryptoHashAlg.Hash != null)
                return cryptoHashAlg.Hash;
            else
                throw new Exception($"Unable to calculate hash value for the content");
        }

        /// <summary>
        /// Write a message to both the log and to the XML output file
        /// </summary>
        /// <param name="xwriter"></param>
        /// <param name="message"></param>
        private void WriteToLogErrorMessage(XmlWriter xwriter, string message)
        {
            if (Settings.LogToXmlThreshold <= LogLevel.Error)
                xwriter.WriteComment($"ERROR: {XmlHelpers.ReplaceInvalidXMLChars(message)}");
            _logger.LogError(message);
        }
        private void WriteToLogWarningMessage(XmlWriter xwriter, string message)
        {
            if (Settings.LogToXmlThreshold <= LogLevel.Warning)
                xwriter.WriteComment($"WARNING: {XmlHelpers.ReplaceInvalidXMLChars(message)}");
            _logger.LogWarning(message);
        }
        private void WriteToLogInfoMessage(XmlWriter xwriter, string message)
        {
            if (Settings.LogToXmlThreshold <= LogLevel.Information)
                xwriter.WriteComment($"INFO: {XmlHelpers.ReplaceInvalidXMLChars(message)}");
            _logger.LogInformation(message);
        }

        private void WriteToLogMessage(XmlWriter xwriter, string message, LogLevel lvl)
        {
            switch (lvl)
            {
                case LogLevel.Information:
                    WriteToLogInfoMessage(xwriter, message);
                    break;
                case LogLevel.Warning:
                    WriteToLogWarningMessage(xwriter, message);
                    break;
                case LogLevel.Error:
                    WriteToLogErrorMessage(xwriter, message);
                    break;
                default:
                    var msg = XmlHelpers.ReplaceInvalidXMLChars(message);

                    if (lvl >= Settings.LogToXmlThreshold)
                        xwriter.WriteComment($"{lvl.ToString().ToUpperInvariant()}: {msg}");

                    _logger.Log(lvl, msg);
                    break;
            }
        }

        private void WriteToLogMessages(XmlWriter xwriter, List<(LogLevel level, string message)> messages)
        {
            // Get rid of duplicate messages and add a count
            var uniqueMessages = from m in messages
                                 group m by m into g
                                 let count = g.Count()
                                 select new { Message = g.Key, Count = count };

            foreach (var msg in uniqueMessages)
            {
                WriteToLogMessage(xwriter, $"{msg.Message.message}{(msg.Count > 1 ? $" [Occurrences: {msg.Count}]" : "")}", msg.Message.level);
            }
        }

        public void WriteElementStringReplacingInvalidChars(XmlWriter xwriter, string localName, string? ns, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                xwriter.WriteElementString(localName, ns, value);
                return;
            }

            if (XmlHelpers.TryReplaceInvalidXMLChars(ref value, out string msg))
            {
                var warn = $"{localName} contains characters which are not allowed in XML; {msg}";
                WriteToLogWarningMessage(xwriter, warn);
            }

            xwriter.WriteElementString(localName, ns, value);

        }

        private void MimeMessageEndEventHandler(object? sender, MimeMessageEndEventArgs e, Stream mboxStream, MessageFileProperties msgFileProps, MimeMessageProperties mimeMsgProps, bool isMbxFile)
        {
            var beginOffset = e.BeginOffset;
            var endOffset = e.EndOffset;
            long mboxMarkerOffset;
            if (sender is MimeParser parser)
            {
                mboxMarkerOffset = parser.MboxMarkerOffset;
            }
            else
            {
                mboxMarkerOffset = beginOffset; //use the start of the message, instead of the start of the Mbox marker
                _logger.LogWarning("Unable to determine the start of the Mbox marker");
            }

            if (isMbxFile)
            {
                //need to calculate offsets differently since the parser is for a single entity wrapped in a bound stream
                long length = endOffset - beginOffset;
                endOffset = mboxStream.Position;
                beginOffset = endOffset - length;
                mboxMarkerOffset = beginOffset;
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
                    mimeMsgProps.Eol = MimeMessageProperties.EOL_TYPE_CRLF;
                    break;
                }
                else if (buffer[i] == LF)
                {
                    mimeMsgProps.Eol = MimeMessageProperties.EOL_TYPE_LF;
                    break;
                }
                else if (buffer[i] == CR && buffer[i + 1] != LF)
                {
                    mimeMsgProps.Eol = MimeMessageProperties.EOL_TYPE_CR;
                    break;
                }
                i++;
            }

            //Check that messages use the same EOL treatment throughout the mbox
            if (mimeMsgProps.Eol != MimeMessageProperties.EOL_TYPE_UNK)
            {
                if (msgFileProps.EolCounts.ContainsKey(mimeMsgProps.Eol))
                {
                    msgFileProps.EolCounts[mimeMsgProps.Eol]++;
                }
                else
                {
                    msgFileProps.EolCounts.Add(mimeMsgProps.Eol, 1);
                }
            }

            //trim all LWSP and EOL chars from the end and then add one eol marker back
            //assumes that the same EOL markers are used throughout the mbox
            //Note: Some malformed messages may not have any eol characters at the end of the message, so when comparing hashes, these must be artifically added to get the correct hash  
            i = buffer.Length - 1;
            while (buffer[i] == LF || buffer[i] == CR || buffer[i] == SP || buffer[i] == TAB)
                --i;
            long j = 1;
            if (mimeMsgProps.Eol == MimeMessageProperties.EOL_TYPE_CRLF)
                j = 2;
            byte[] newBuffer = new byte[i + 1 + j];

            Array.Copy(buffer, 0, newBuffer, 0, i + 1);
            if (mimeMsgProps.Eol == MimeMessageProperties.EOL_TYPE_CRLF)
            {
                newBuffer[^1] = LF;
                newBuffer[^2] = CR;
            }
            else if (mimeMsgProps.Eol == MimeMessageProperties.EOL_TYPE_LF)
            {
                newBuffer[^1] = LF;
            }
            else if (mimeMsgProps.Eol == MimeMessageProperties.EOL_TYPE_CR)
            {
                newBuffer[^1] = CR;
            }
            else
            {
                _logger.LogError("Unable to determine EOL marker");
                throw new Exception("Unable to determine EOL marker");
            }

            //just for debugging
            //var str = System.Text.Encoding.ASCII.GetString(newBuffer);

            mimeMsgProps.MessageSize = newBuffer.Length;

            var hashAlg = HashAlgorithm.Create(Settings.HashAlgorithmName) ?? SHA256.Create(); //Fallback to known hash algorithm
            mimeMsgProps.MessageHash = hashAlg.ComputeHash(newBuffer);

            //just for debugging
            //var hexStr = Convert.ToHexString(mimeMsgProps.MessageHash).ToUpperInvariant();

        }
    }
}