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
using MimeKit.Encodings;
using System.Text;
using NDepend.Path;
using System.ComponentModel;
using UIUCLibrary.EaPdf.Helpers;
using System.Reflection.PortableExecutable;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Net.Mime;
using CsvHelper.Configuration;
using FilePathHelpers = UIUCLibrary.EaPdf.Helpers.FilePathHelpers;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using MimeKit.Text;

namespace UIUCLibrary.EaPdf
{
    public class EmailToXmlProcessor
    {
        //TODO: Need to add support for TNEF format, especially if will be processing Microsoft PST files

        //TODO: Need to check for XML invalid characters almost anyplace I write XML string content, see the WriteElementStringReplacingInvalidChars function

        //for LWSP (Linear White Space) detection, compaction, and trimming
        const byte CR = 13;
        const byte LF = 10;
        const byte SP = 32;
        const byte TAB = 9;


        public const string XM = "xm";
        public const string XM_NS = "https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2";
        public const string XM_XSD = "eaxs_schema_v2.xsd";

        public const string XHTML = "xhtml";
        public const string XHTML_NS = "http://www.w3.org/1999/xhtml";
        public const string XHTML_XSD = "eaxs_xhtml_mini.xsd";

        private readonly ILogger _logger;

        public const string HASH_DEFAULT = "SHA256";

        const string EX_MBOX_FROM_MARKER = "Failed to find mbox From marker";
        const string EX_MBOX_PARSE_HEADERS = "Failed to parse message headers";

        public EmailToXmlProcessorSettings Settings { get; }

        //stats used for development and debuging
        private readonly Dictionary<string, int> contentTypeCounts = new();
        private readonly Dictionary<string, int> xGmailLabelCounts = new();
        private readonly Dictionary<string, int> xGmailLabelComboCounts = new();

        //need to keep track of folders in case output file is split into multiple files and the split happens while processing a subfolder
        private readonly Stack<string> _folders = new();

        /// <summary>
        /// Create a processor for email files, initializing the logger and settings
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public EmailToXmlProcessor(ILogger<EmailToXmlProcessor> logger, EmailToXmlProcessorSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("MboxProcessor Created");

        }

        //QUESTION: Can an EML file start with a 'From ' line just like an mbox file?
        public long ConvertEmlToEaxs()
        {
            //UNDONE: Convert just a single EML file
            return 0;
        }

        public long ConvertFolderOfEmlToEaxs()
        {
            //UNDONE: Convert a folder of EML files
            return 0;
        }

        /// <summary>
        /// Convert a folder of mbox files into an archival email XML file
        /// </summary>
        /// <param name="mboxFolderPath">the path to the folder to process, all mbox files in the folder will be processed</param>
        /// <param name="outFolderPath">the path to the output folder, must be different than the mboxFolderPath</param>
        /// <param name="globalId">Globally unique, permanent, absolute URI with no fragment conforming to the canonical form specified in RFC2396 as amended by RFC2732.</param>
        /// <param name="accntEmails">Comma-separated list of email addresses</param>
        /// <param name="includeSubFolders">if true subfolders in the directory will also be processed</param>
        /// <returns>the most recent localId number which is usually the total number of messages processed</returns>
        public long ConvertFolderOfMboxToEaxs(string mboxFolderPath, string outFolderPath, string globalId, string accntEmails = "", long startingLocalId = 0, List<MessageBrief>? messageList = null, bool saveCsv = true)
        {
            //TODO: May want to modify the XML Schema to allow for references to child folders instead of embedding child folders in the same mbox, see account-ref-type.  Maybe add ParentFolder type

            if (string.IsNullOrWhiteSpace(mboxFolderPath))
            {
                throw new ArgumentNullException(nameof(mboxFolderPath));
            }

            if (!Directory.Exists(mboxFolderPath))
            {
                throw new DirectoryNotFoundException(mboxFolderPath);
            }

            if (string.IsNullOrWhiteSpace(outFolderPath))
            {
                throw new ArgumentNullException(nameof(outFolderPath));
            }

            if (string.IsNullOrWhiteSpace(globalId))
            {
                throw new ArgumentNullException(nameof(globalId));
            }

            var fullMboxFolderPath = Path.GetFullPath(mboxFolderPath);
            var fullOutFolderPath = Path.GetFullPath(outFolderPath);

            //Determine whether the output folder path is valid given the input path
            if (!Helpers.FilePathHelpers.IsValidOutputPathForMboxFolder(fullMboxFolderPath, fullOutFolderPath))
            {
                throw new ArgumentException($"The outFolderPath, '{fullOutFolderPath}', cannot be the same as or a child of the mboxFolderPath, '{fullMboxFolderPath}'");
            }

            messageList ??= new List<MessageBrief>(); //used to create the CSV file

            long localId = startingLocalId;

            if (Settings.OneFilePerMbox)
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

                _logger.LogInformation("Convert mbox files in directory: '{fullMboxFolderPath}' into XML file: '{outFilePath}'", fullMboxFolderPath, xmlFilePath);

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

                var xstream = new FileStream(xmlFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                var xwriter = XmlWriter.Create(xstream, xset);


                xwriter.WriteStartDocument();
                WriteDocType(xwriter);
                WriteAccountHeaderFields(xwriter, globalId, accntEmails);

                var mboxProps = new MboxProperties()
                {
                    GlobalId = globalId,
                    AccountEmails = accntEmails,
                    OutFilePath = xmlFilePath,
                };
                SetHashAlgorithm(mboxProps, xwriter);

                foreach (string mboxFilePath in Directory.EnumerateFiles(mboxFolderPath))
                {
                    WriteToLogInfoMessage(xwriter, $"Processing mbox file: {mboxFilePath}");
                    mboxProps.MboxFilePath = mboxFilePath;
                    mboxProps.MessageCount = 0;

                    localId = ProcessMbox(mboxProps, ref xwriter, ref xstream, localId, messageList);
                }

                xwriter.WriteEndElement(); //Account

                xwriter.WriteEndDocument();

                xwriter.Flush();
                xwriter.Close(); //this should close the underlying stream
                xwriter.Dispose();
                xstream.Dispose();

                //TODO: Open the XML file and do some post processing to make sure that all html id attributes are unique across the whole document

                _logger.LogInformation("Output XML File: {xmlFilePath}, Total messages: {messageCount}", xmlFilePath, localId - startingLocalId);
            }

            //write the csv file
            if (saveCsv)
            {
                var csvFilePath = Path.Combine(fullOutFolderPath, Path.GetFileName(Path.ChangeExtension(fullMboxFolderPath, "csv")));
                MessageBrief.SaveMessageBriefsToCsvFile(csvFilePath, messageList);
                csvFilePath = Path.Combine(fullOutFolderPath, Path.GetFileNameWithoutExtension(fullMboxFolderPath) + "_stats.csv");
                SaveStatsToCsv(csvFilePath);
            }

            return localId;
        }

        /// <summary>
        /// Convert one mbox file into an archival email XML file.
        /// </summary>
        /// <param name="fullMboxFilePath">the path to the mbox file to process</param>
        /// <param name="outFolderPath">the path to the output folder, must be different than the folder containing the mboxFilePath</param>
        /// <param name="globalId">Globally unique, permanent, absolute URI with no fragment conforming to the canonical form specified in RFC2396 as amended by RFC2732.</param>
        /// <param name="accntEmails">Comma-separated list of email addresses</param>
        /// <returns>the most recent localId number which is usually the total number of messages processed</returns>
        public long ConvertMboxToEaxs(string mboxFilePath, string outFolderPath, string globalId, string accntEmails = "", long startingLocalId = 0, List<MessageBrief>? messageList = null, bool saveCsv = true)
        {

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
                throw new ArgumentNullException(nameof(outFolderPath));
            }

            if (string.IsNullOrWhiteSpace(globalId))
            {
                throw new Exception("globalId is a required parameter");
            }

            string fullMboxFilePath = Path.GetFullPath(mboxFilePath);
            string fullOutFolderPath = Path.GetFullPath(outFolderPath);

            //Determine whether the output folder path is valid given the input path
            if (!Helpers.FilePathHelpers.IsValidOutputPathForMboxFile(fullMboxFilePath, fullOutFolderPath))
            {
                throw new ArgumentException($"The outFolderPath, '{fullOutFolderPath}', cannot be the same as or a child of the mboxFilePath, '{fullMboxFilePath}', ignoring any extensions");
            }

            var xmlFilePath = Path.Combine(fullOutFolderPath, Path.GetFileName(Path.ChangeExtension(fullMboxFilePath, "xml")));

            messageList ??= new List<MessageBrief>(); //used to create the CSV file

            long localId = startingLocalId;

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


            var xstream = new FileStream(xmlFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var xwriter = XmlWriter.Create(xstream, xset);

            xwriter.WriteStartDocument();
            WriteDocType(xwriter);
            WriteAccountHeaderFields(xwriter, globalId, accntEmails);

            WriteToLogInfoMessage(xwriter, $"Processing mbox file: {fullMboxFilePath}");

            var mboxProps = new MboxProperties()
            {
                MboxFilePath = fullMboxFilePath,
                GlobalId = globalId,
                OutFilePath = xmlFilePath,
            };
            SetHashAlgorithm(mboxProps, xwriter);

            localId = ProcessMbox(mboxProps, ref xwriter, ref xstream, localId, messageList);

            xwriter.WriteEndElement(); //WriteXmlAccountHeaderFields

            xwriter.WriteEndDocument();

            xwriter.Flush();
            xwriter.Close(); //this should close the underlying stream
            xwriter.Dispose();
            xstream.Dispose();

            //TODO: Open the XML file and do some post processing to make sure that all html id attributes are unique across the whole document


            //write the csv file
            if (saveCsv)
            {
                var csvFilePath = Path.Combine(fullOutFolderPath, Path.GetFileName(Path.ChangeExtension(fullMboxFilePath, "csv")));
                MessageBrief.SaveMessageBriefsToCsvFile(csvFilePath, messageList);
                csvFilePath = Path.Combine(fullOutFolderPath, Path.GetFileNameWithoutExtension(fullMboxFilePath) + "_stats.csv");
                SaveStatsToCsv(csvFilePath);
            }

            _logger.LogInformation("Output XML File: {xmlFilePath}", xmlFilePath);

            return localId;
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
                xwriter.WriteProcessingInstruction("Settings", 
                    $"HashAlgorithmName: {Settings.HashAlgorithmName}, " +
                    $"SaveAttachmentsAndBinaryContentExternally: {Settings.SaveAttachmentsAndBinaryContentExternally}, " +
                    $"WrapExternalContentInXml: {Settings.WrapExternalContentInXml}, " +
                    $"PreserveBinaryAttachmentTransferEncodingIfPossible: {Settings.PreserveBinaryAttachmentTransferEncodingIfPossible}, " +
                    $"PreserveTextAttachmentTransferEncoding: {Settings.PreserveTextAttachmentTransferEncoding}, " +
                    $"IncludeSubFolders: {Settings.IncludeSubFolders}, " +
                    $"ExternalContentFolder: {Settings.ExternalContentFolder}, " +
                    $"OneFilePerMbox: {Settings.OneFilePerMbox}," +
                    $"MaximumXmlFileSize: {Settings.MaximumXmlFileSize}, " +
                    $"SaveTextAsXhtml: {Settings.SaveTextAsXhtml}, " +
                    $"LogToXmlThreshold: {Settings.LogToXmlThreshold}, " +
                    $"DefaultFileExtension: {Settings.DefaultFileExtension}"
                    );

                xwriter.WriteStartElement("Account", XM_NS);
                xwriter.WriteAttributeString("xsi", "schemaLocation", "http://www.w3.org/2001/XMLSchema-instance", "eaxs_schema_v2.xsd");
                foreach (var addr in accntEmails.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    xwriter.WriteElementString("EmailAddress", XM_NS, addr);
                }
                xwriter.WriteElementString("GlobalId", XM_NS, globalId);
            }
        }

        /// <summary>
        /// Write nested folders as needed according the _folders stack
        /// </summary>
        /// <param name="xwriter"></param>
        private void WriteFolders(XmlWriter xwriter)
        {
            foreach (var fld in _folders.Reverse())
            {
                xwriter.WriteStartElement("Folder", XM_NS);
                xwriter.WriteElementString("Name", XM_NS, fld);
            }
        }

        private long ProcessMbox(MboxProperties mboxProps, ref XmlWriter xwriter, ref FileStream xstream, long localId, List<MessageBrief> messageList)
        {

            _folders.Push(mboxProps.MboxName);
            xwriter.WriteStartElement("Folder", XM_NS);
            xwriter.WriteElementString("Name", XM_NS, mboxProps.MboxName);

            //Keep track of properties for an individual messager, such as Eol and Hash
            MimeMessageProperties msgProps = new();

            //open filestream and wrap it in a cryptostream so that we can hash the file as we process it
            using FileStream mboxStream = new(mboxProps.MboxFilePath, FileMode.Open, FileAccess.Read);


            if (Helpers.MimeKitHelpers.IsStreamAnMbx(mboxStream))
            {
                //This is a Pine *mbx* file, so it requires special parsing
                WriteToLogInfoMessage(xwriter, $"File '{mboxProps.MboxFilePath}' is a Pine *mbx* file; using an alternate parsing strategy");

                using var mbxParser = new MbxParser(mboxStream, mboxProps.HashAlgorithm);

                mbxParser.MimeMessageEnd += (sender, e) => MimeMessageEndEventHandler(sender, e, mboxStream, mboxProps, msgProps, true);

                MimeMessage? message = null;

                while (!mbxParser.IsEndOfStream)
                {
                    if (Settings.MaximumXmlFileSizeThreshold > 0 && xstream.Position >= Settings.MaximumXmlFileSizeThreshold)
                    {
                        //close the current xml file and open a new one
                        StartNewXmlFile(ref xwriter, ref xstream, mboxProps);
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
                        msgProps.MbxMessageHeader = mbxParser.CurrentHeader;
                        localId = ProcessCurrentMessage(message, xwriter, localId, messageList, mboxProps, msgProps);
                        mboxProps.MessageCount++;
                    }
                }

                //make sure to read to the end of the stream so the hash is correct
                ReadToEnd(mbxParser.CryptoStream);

                if (Settings.IncludeSubFolders)
                {
                    localId = ProcessChildMboxes(mboxProps, ref xwriter, ref xstream, localId, messageList);
                }

                WriteMbox(xwriter, mboxProps, mboxStream.Length);

            }
            else
            {
                using CryptoStream cryptoStream = new(mboxStream, mboxProps.HashAlgorithm, CryptoStreamMode.Read);

                var mboxParser = new MimeParser(cryptoStream, MimeFormat.Mbox);

                mboxParser.MimeMessageEnd += (sender, e) => MimeMessageEndEventHandler(sender, e, mboxStream, mboxProps, msgProps, false);

                //Need to record the previous message so we can defer writing it to the XML until the next message can be interogated for error conditions 
                //and we can add the <Incomplete> tag if needed.
                MimeMessage? prevMessage = null;
                MimeMessage? message = null;

                while (!mboxParser.IsEndOfStream)
                {
                    if (Settings.MaximumXmlFileSizeThreshold > 0 && xstream.Position >= Settings.MaximumXmlFileSizeThreshold)
                    {
                        //close the current xml file and open a new one
                        StartNewXmlFile(ref xwriter, ref xstream, mboxProps);
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
                        if (mboxProps.MessageCount == 0)
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
                        if (mboxProps.MessageCount > 0)
                        {
                            var msg = $"{fex2.Message} The content of the message is probably incomplete because of an unmangled 'From ' line in the message body. Content starting from offset {mboxParser.MboxMarkerOffset} to the beginning of the next message will be skipped.";
                            _logger.LogWarning(msg);
                            msgProps.Incomplete(msg, $"Stream Position: {mboxParser.MboxMarkerOffset}");

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

                    if (prevMessage != null)
                    {
                        localId = ProcessCurrentMessage(prevMessage, xwriter, localId, messageList, mboxProps, msgProps);
                        mboxProps.MessageCount++;
                        msgProps.NotIncomplete();
                    }
                    else if (mboxProps.MessageCount > 0 && prevMessage == null)
                    {
                        WriteToLogErrorMessage(xwriter, "Message is null");
                    }
                    prevMessage = message;

                }

                if (message != null)
                {
                    //process the last message
                    localId = ProcessCurrentMessage(message, xwriter, localId, messageList, mboxProps, msgProps);
                    mboxProps.MessageCount++;
                }

                //make sure to read to the end of the stream so the hash is correct
                ReadToEnd(cryptoStream);

                if (Settings.IncludeSubFolders)
                {
                    localId = ProcessChildMboxes(mboxProps, ref xwriter, ref xstream, localId, messageList);
                }

                WriteMbox(xwriter, mboxProps, mboxStream.Length);
            }

            xwriter.WriteEndElement(); //Folder
            _folders.Pop();

            return localId;
        }

        private void WriteMbox(XmlWriter xwriter, MboxProperties mboxProps, long size)
        {
            xwriter.WriteStartElement("Mbox", XM_NS);

            var relPath = new Uri(Path.GetRelativePath(mboxProps.OutDirectoryName, mboxProps.MboxFilePath), UriKind.Relative);
            var ext = Path.GetExtension(mboxProps.MboxFilePath).TrimStart('.');
            if(string.IsNullOrWhiteSpace(ext))
                ext = Settings.DefaultFileExtension;
            xwriter.WriteElementString("RelPath", XM_NS, relPath.ToString().Replace('\\','/'));
            xwriter.WriteElementString("FileExt", XM_NS, ext);
            xwriter.WriteElementString("Eol", XM_NS, mboxProps.MostCommonEol);
            if (mboxProps.UsesDifferentEols)
            {
                mboxProps.EolCounts.TryGetValue("CR", out int crCount);
                mboxProps.EolCounts.TryGetValue("LF", out int lfCount);
                mboxProps.EolCounts.TryGetValue("CRLF", out int crlfCount);
                WriteToLogWarningMessage(xwriter, $"Mbox file contains multiple different EOLs: CR: {crCount}, LF: {lfCount}, CRLF: {crlfCount}");
            }
            if (mboxProps.HashAlgorithm.Hash != null)
            {
                WriteHash(xwriter, mboxProps.HashAlgorithm.Hash, mboxProps.HashAlgorithmName);
            }
            else
            {
                WriteToLogWarningMessage(xwriter, $"Unable to calculate the hash value for the Mbox");
            }

            if (size >= 0)
            {
                xwriter.WriteElementString("Size", XM_NS, size.ToString());
            }
            else
            {
                WriteToLogWarningMessage(xwriter, $"Unable to determine the size of the Mbox");
            }

            string cntMsg = $"File {mboxProps.MboxName} contains {mboxProps.MessageCount} valid messages";
            WriteToLogInfoMessage(xwriter, cntMsg);

            xwriter.WriteEndElement(); //Mbox
        }

        private long ProcessChildMboxes(MboxProperties mboxProps, ref XmlWriter xwriter, ref FileStream xstream, long localId, List<MessageBrief> messageList)
        {
            //TODO: Add accomodations for OneFilePerMbox and use the ReferencesAccount xml element
            //TODO: May want to modify the XML Schema to allow for references to child folders instead of embedding child folders in the same mbox, see account-ref-type.  Maybe add ParentFolder type

            //look for a subfolder named the same as the mbox file ignoring extensions
            //i.e. Mozilla Thunderbird will append the extension '.sbd' to the folder name
            string? subfolderName;
            try
            {
                subfolderName = Directory.GetDirectories(mboxProps.MboxDirectoryName, $"{mboxProps.MboxName}.*").SingleOrDefault();
            }
            catch (InvalidOperationException)
            {
                WriteToLogErrorMessage(xwriter, $"There is more than one folder that matches '{mboxProps.MboxName}.*'; skipping all subfolders");
                subfolderName = null;
            }
            catch (Exception ex)
            {
                WriteToLogErrorMessage(xwriter, $"Skipping subfolders. {ex.GetType().Name}: {ex.Message}");
                subfolderName = null;
            }

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
                    foreach (var childMbox in childMboxes)
                    {
                        //create new MboxProperties which is copy of parent MboxProperties except for the MboxFilePath and the checksum hash
                        MboxProperties childMboxProps = new(mboxProps)
                        {
                            MboxFilePath = childMbox
                        };
                        SetHashAlgorithm(childMboxProps, xwriter);

                        //just try to process it, if no errors thrown, its probably an mbox file
                        WriteToLogInfoMessage(xwriter, $"Processing Child Mbox: {childMbox}");
                        localId = ProcessMbox(childMboxProps, ref xwriter, ref xstream, localId, messageList);
                    }
                }

            }
            return localId;
        }

        /// <summary>
        /// Read to the end of a stream to ensure the hash is correct
        /// </summary>
        /// <param name="stream"></param>
        private void ReadToEnd(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            int i;
            do
            {
                i = stream.ReadByte();
            } while (i != -1);
        }

        private void StartNewXmlFile(ref XmlWriter xwriter, ref FileStream xstream, MboxProperties mboxProps)
        {
            var origXmlFilePath = mboxProps.OutFilePath;

            //increment the file number; this also updates the OutFilePath
            _ = mboxProps.IncrementOutFileNumber();

            var newXmlFilePath = mboxProps.OutFilePath;

            //close any opened folder elements
            for (int c = 0; c < _folders.Count; c++)
            {
                xwriter.WriteEndElement(); //Folder
            }

            xwriter.WriteProcessingInstruction("ContinuedIn", $"'{Path.GetFileName(newXmlFilePath)}'");

            //close the current xml file and start a new one
            xwriter.WriteEndDocument(); //should write out any unclosed elements
            xwriter.Flush();
            xwriter.Close(); //this should close the underlying stream
            xwriter.Dispose();
            xstream.Dispose();

            //TODO: Open the XML file and do some post processing to make sure that all html id attributes are unique across the whole document

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

            WriteAccountHeaderFields(xwriter, mboxProps.GlobalId, mboxProps.AccountEmails);
            WriteToLogInfoMessage(xwriter, $"Processing mbox file: {mboxProps.MboxFilePath}");
            WriteFolders(xwriter);
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

        private void SetHashAlgorithm(MboxProperties mboxProps, XmlWriter xwriter)
        {
            var name = mboxProps.TrySetHashAlgorithm(Settings.HashAlgorithmName);
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

        private long ProcessCurrentMessage(MimeMessage message, XmlWriter xwriter, long localId, List<MessageBrief> messageList, MboxProperties mboxProps, MimeMessageProperties msgProps)
        {

            localId++;
            var messageId = localId;

            xwriter.WriteStartElement("Message", XM_NS);

            localId = WriteMessage(xwriter, message, localId, false, true, mboxProps, msgProps);

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
                Errors = (string.IsNullOrWhiteSpace(msgProps.IncompleteErrorType) && string.IsNullOrWhiteSpace(msgProps.IncompleteErrorLocation)) ? 0 : 1,
                FirstErrorMessage = $"{msgProps.IncompleteErrorLocation} {msgProps.IncompleteErrorType}".Trim()

            });

            msgProps.Eol = MimeMessageProperties.EOL_TYPE_UNK;

            return localId;
        }

        private long WriteMessage(XmlWriter xwriter, MimeMessage message, long localId, bool isChildMessage, bool expectingBodyContent, MboxProperties mboxProps, MimeMessageProperties msgProps)
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
            if (!isChildMessage && !MimeKitHelpers.TryGetDraft(message, msgProps, out _))
            {
                if (message.Headers[HeaderId.From] == null)
                {
                    WriteToLogWarningMessage(xwriter, "The message does not have a From header.");
                }
                if (message.Headers[HeaderId.Date] == null)
                {
                    WriteToLogWarningMessage(xwriter, "The message does not have a Date header.");
                }
                if (message.Headers[HeaderId.To] == null && message.Headers[HeaderId.Cc] == null && message.Headers[HeaderId.Bcc] == null)
                {
                    WriteToLogWarningMessage(xwriter, "The message does not have a To, Cc, or Bcc.");
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
                WriteMessageStatuses(xwriter, message, msgProps);
            }

            localId = WriteMessageBody(xwriter, message.Body, localId, expectingBodyContent, mboxProps, msgProps);

            if (!string.IsNullOrWhiteSpace(msgProps.IncompleteErrorType) || !string.IsNullOrWhiteSpace(msgProps.IncompleteErrorLocation))
            {
                xwriter.WriteStartElement("Incomplete", XM_NS);
                xwriter.WriteElementString("ErrorType", XM_NS, msgProps.IncompleteErrorType ?? "Unknown");
                xwriter.WriteElementString("ErrorLocation", XM_NS, msgProps.IncompleteErrorLocation ?? "Unknown");
                xwriter.WriteEndElement(); //Incomplete
            }

            if (!isChildMessage)
            {
                xwriter.WriteElementString("Eol", XM_NS, msgProps.Eol);

                WriteHash(xwriter, msgProps.MessageHash, Settings.HashAlgorithmName);

                if (msgProps.MessageSize >= 0)
                {
                    xwriter.WriteElementString("Size", XM_NS, msgProps.MessageSize.ToString());
                }
                else
                {
                    WriteToLogWarningMessage(xwriter, $"Unable to determine the size of the Message");
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
            foreach (var hdr in message.Headers) //All headers even if already covered above
            {
                xwriter.WriteStartElement("Header", XM_NS);

                xwriter.WriteElementString("Name", XM_NS, hdr.Field);

                //According to the XML schema, header values should be the raw headers, not converted to Unicode
                var rawValue = System.Text.Encoding.ASCII.GetString(hdr.RawValue);
                WriteElementStringReplacingInvalidChars(xwriter, "Value", XM_NS, rawValue.Trim());

                //UNSUPPORTED: Comments, not currently supported by MimeKit

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
                var warn = $"{localName} contains characters which are not allowed in XML; they have been replaced with \uFFFD.  {msg}";
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

        private long WriteMessageBody(XmlWriter xwriter, MimeEntity mimeEntity, long localId, bool expectingBodyContent, MboxProperties mboxProps, MimeMessageProperties msgProps)
        {
            bool isMultipart = false;

            MimePart? part = mimeEntity as MimePart;
            //TODO: Check if the entity is a TnefPart for application/ms-tnef content type attachments 
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
                    localId = WriteMessageBody(xwriter, item, localId, true, mboxProps, msgProps);
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
                if (mimeEntity is MessageDeliveryStatus deliveryStatus)
                {
                    localId = WriteDeliveryStatus(xwriter, deliveryStatus, localId, mboxProps);
                }
                else if (part != null && !MimeKitHelpers.IsXMozillaExternalAttachment(part))
                {
                    localId = WriteSingleBodyContent(xwriter, part, localId, expectingBodyContent, mboxProps);
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
                    localId = WriteSingleBodyChildMessage(xwriter, message, localId, expectingBodyContent, mboxProps, msgProps);
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
        /// <param name="mboxProps"></param>
        /// <returns></returns>
        private long WriteDeliveryStatus(XmlWriter xwriter, MessageDeliveryStatus deliveryStatus, long localId, MboxProperties mboxProps)
        {
            //Deal with malformed delivery status messages, instead just write a warning and then WriteSingleBodyContent
            if (deliveryStatus.StatusGroups.Count < 2)
            {
                WriteToLogWarningMessage(xwriter, $"Delivery status message is malformed. It should have at least 2 status groups; it has only {deliveryStatus.StatusGroups.Count}. Writing message as a single body content instead.");
                return WriteSingleBodyContent(xwriter, deliveryStatus, localId, true, mboxProps);
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

        private long WriteSingleBodyChildMessage(XmlWriter xwriter, MessagePart message, long localId, bool expectingBodyContent, MboxProperties mboxProps, MimeMessageProperties msgProps)
        {
            //The message parameter might contain a MessagePart or its subclass TextRfc822Headers
            //If it is TextRfc822Headers it will not have a MessageBody.  This is handle correctly in the WriteMessageBody function 
            xwriter.WriteStartElement("ChildMessage", XM_NS);
            localId++;

            localId = WriteMessage(xwriter, message.Message, localId, true, expectingBodyContent, mboxProps, msgProps);
            xwriter.WriteEndElement(); //ChildMessage
            return localId;
        }

        private long WriteSingleBodyContent(XmlWriter xwriter, MimePart part, long localId, bool expectingBodyContent, MboxProperties mboxProps)
        {
            //if it is text and not an attachment, save embedded in the XML
            if (part.ContentType.IsMimeType("text", "*") && !part.IsAttachment)
            {
                var (text, encoding, warning) = GetContentText(part);

                if (!string.IsNullOrWhiteSpace(text) || expectingBodyContent)
                {
                    xwriter.WriteStartElement("BodyContent", XM_NS);

                    if (!string.IsNullOrWhiteSpace(text) && !expectingBodyContent)
                    {
                        WriteToLogWarningMessage(xwriter, $"Not expecting body content for '{part.ContentType.MimeType}'.");
                    }

                    if (Settings.SaveTextAsXhtml)
                    {
                        var xhtml = GetTextAsXhtml(part, out List<(LogLevel level, string message)> messages);
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
                    localId = SerializeContentInExtFile(part, xwriter, localId, mboxProps);
                }
            }
            return localId;
        }

        private string GetTextAsXhtml(MimePart part, out List<(LogLevel level, string message)> messages)
        {

            messages = new List<(LogLevel level, string message)>();

            if (part is not TextPart txtPart)
            {
                throw new Exception($"Unexpected part type '{part.GetType().Name}'");
            }

            string htmlText = txtPart.Text;

            if (string.IsNullOrWhiteSpace(htmlText))
            {
                return htmlText;
            }


            if (txtPart.ContentType.IsMimeType("text", "html") || txtPart.ContentType.IsMimeType("text", "plain"))
            {
                if (txtPart.IsHtml)
                {
                    //clean up the html so it is valid-ish xhtml, log any issues to the messages list
                    htmlText = HtmlHelpers.ConvertHtmlToXhtml(htmlText, ref messages, false);
                }
                else if (txtPart.IsFlowed)
                {
                    //Use the MimeKit converters to convert plain/text, flowed to html,
                    var converter = new FlowedToHtml();
                    if (txtPart.ContentType.Parameters.TryGetValue("delsp", out string delsp))
                        converter.DeleteSpace = delsp.Equals("yes", StringComparison.OrdinalIgnoreCase);

                    htmlText = converter.Convert(htmlText);
                    //clean up the html so it is valid-ish xhtml, ignoring any issues since this was already derived from plain text
                    htmlText = HtmlHelpers.ConvertHtmlToXhtml(htmlText, ref messages, true);
                }
                else //plain text not flowed
                {
                    //Use the MimeKit converters to convert plain/text, fixed to html,
                    var converter = new TextToHtml();
                    htmlText = converter.Convert(htmlText);
                    //clean up the html so it is valid-ish xhtml, ignoring any issues since this was already derived from plain text
                    htmlText = HtmlHelpers.ConvertHtmlToXhtml(htmlText, ref messages, true);
                }

            }
            else
            {
                //TODO: Need to make accomodations for text/enriched (and text/richtext? -- not Microsoft RTF), see Pine sent-mail-aug-2007, message id: 4d2cbdd341d0e87da57ba2245562265f@uiuc.edu                 

                messages.Add((LogLevel.Error, $"The '{part.ContentType.MimeType}' content is not plain text or html."));
            }


            return htmlText;
        }

        private void WriteContentCData(XmlWriter xwriter, string text, string encoding, string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                WriteToLogWarningMessage(xwriter, warning);
            }

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

        /// <summary>
        /// Get the text content from a MimePart.  If the text contains characters not valid in XML, the output will be encoded as quoted-printable, and a warning will be returned
        /// </summary>
        /// <param name="part"></param>
        /// <returns>A tuple containing the output text, output text encoding, and any warning message</returns>
        /// <exception cref="ArgumentException"></exception>
        private (string text, string encoding, string warning) GetContentText(MimePart part)
        {
            string contentStr = "";
            string encoding = "";
            string warning = "";

            if (!part.ContentType.IsMimeType("text", "*"))
            {
                throw new ArgumentException("The MimePart is not 'text/*'");
            }

            if (part != null && part.Content != null)
            {
                //Decode the stream and treat it as whatever the charset advertised in the content-type header
                using StreamReader reader = new(part.Content.Open(), part.ContentType.CharsetEncoding, true);
                string xmlStr = reader.ReadToEnd();

                //The content stream may contain characters that are not allowed in XML, i.e. ASCII control characters
                //Check the content, and if this is the case encode it as quoted-printable before saving to XML
                bool validXmlChars = true;
                try
                {
                    xmlStr = XmlConvert.VerifyXmlChars(xmlStr);
                }
                catch (XmlException xex)
                {
                    warning = $"Characters not valid in XML.  Line {xex.LinePosition}: {xex.Message}";
                    validXmlChars = false;
                }

                if (validXmlChars)
                {
                    contentStr = xmlStr;
                    encoding = "";
                }
                else
                {
                    //Use the quoted-printable encoding which should escape the low ascii characters
                    var qpEncoder = new QuotedPrintableEncoder();
                    byte[] xmlStrByts = Encoding.ASCII.GetBytes(xmlStr);
                    int len = qpEncoder.EstimateOutputLength(xmlStrByts.Length);
                    byte[] qpStrByts = new byte[len];

                    int outLen = qpEncoder.Encode(xmlStrByts, 0, xmlStrByts.Length, qpStrByts);

                    var qpStr = Encoding.ASCII.GetString(qpStrByts, 0, outLen);

                    contentStr = qpStr;
                    encoding = "quoted-printable";
                }
            }

            return (contentStr, encoding, warning);
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
            long? fileSize = null; //FUTURE: Add support for file size

            string? encoding;
            //7bit and 8bit should be text content, so if preserving the encoding, decode it and use the streamreader with the contenttype charset, if any, to get the text and write it to the xml in a cdata section.  Default is the same as 7bit.
            if (Settings.PreserveTextAttachmentTransferEncoding && (content.Encoding == ContentEncoding.EightBit || content.Encoding == ContentEncoding.SevenBit || content.Encoding == ContentEncoding.Default))
            {
                var baseStream = content.Open(); //get decoded stream
                using CryptoStream cryptoStream = new(baseStream, cryptoHashAlg, CryptoStreamMode.Read);
                
                using StreamReader reader = new(cryptoStream, part.ContentType.CharsetEncoding, true);
                xwriter.WriteCData(reader.ReadToEnd());
                hash = cryptoHashAlg.Hash;
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
                using CryptoStream cryptoStream = new(baseStream, cryptoHashAlg, CryptoStreamMode.Read);
                
                //treat the stream as ASCII because it is already encoded and just write it out using the same encoding
                using StreamReader reader = new(cryptoStream, System.Text.Encoding.ASCII);
                xwriter.WriteCData(reader.ReadToEnd());
                hash = cryptoHashAlg.Hash;
                encoding = MimeKitHelpers.GetContentEncodingString(content.Encoding);
            }
            else //anything is treated as binary content, so copy to a memory stream and write it to the XML as base64
            {
                byte[] byts;
                using (MemoryStream ms = new())
                {
                    content.Open().CopyTo(ms); //get decoded stream and copy it to memory stream and then get an array
                    byts = ms.ToArray();
                }
                hash = cryptoHashAlg.ComputeHash(byts);
                xwriter.WriteBase64(byts, 0, byts.Length);
                encoding = "base64";
            }

            xwriter.WriteEndElement(); //Content

            if (!string.IsNullOrWhiteSpace(encoding))
            {
                xwriter.WriteElementString("TransferEncoding", XM_NS, encoding);
            }

            if (fileSize != null) //FUTURE:  Add support for file size
            {
                xwriter.WriteElementString("FileSize", XM_NS, fileSize.ToString());
            }

            if (hash != null)
            {
                WriteHash(xwriter, hash, Settings.HashAlgorithmName);
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

            bool wrapInXml = Settings.WrapExternalContentInXml;

            string randomFilePath = FilePathHelpers.GetRandomFilePath(mboxProps.OutDirectoryName);

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

            var hashFileName = FilePathHelpers.GetOutputFilePathBasedOnHash(hash, part, Path.Combine(mboxProps.OutDirectoryName, Settings.ExternalContentFolder), wrapInXml);
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
            xwriter.WriteElementString("RelPath", XM_NS, new Uri(Path.GetRelativePath(Path.Combine(mboxProps.OutDirectoryName, Settings.ExternalContentFolder), hashFileName), UriKind.Relative).ToString().Replace('\\', '/'));

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
                    var msg = $"{lvl.ToString().ToUpperInvariant()}: {XmlHelpers.ReplaceInvalidXMLChars(message)}";
                    if (lvl >= Settings.LogToXmlThreshold)
                        xwriter.WriteComment(msg);
                    _logger.LogInformation(msg);
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
                var warn = $"{localName} contains characters which are not allowed in XML; they have been replaced with \uFFFD.  {msg}";
                WriteToLogWarningMessage(xwriter, warn);
            }

            xwriter.WriteElementString(localName, ns, value);

        }

        private void MimeMessageEndEventHandler(object? sender, MimeMessageEndEventArgs e, Stream mboxStream, MboxProperties mboxProps, MimeMessageProperties msgProps, bool isMbxFile)
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
            //Note: Some malformed messages may not have any eol characters at the end of the message, so when comparing hashes, these must be artifically added to get the correct hash  
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
                newBuffer[^1] = LF;
                newBuffer[^2] = CR;
            }
            else if (msgProps.Eol == MimeMessageProperties.EOL_TYPE_LF)
            {
                newBuffer[^1] = LF;
            }
            else if (msgProps.Eol == MimeMessageProperties.EOL_TYPE_CR)
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

            msgProps.MessageSize = newBuffer.Length;

            var hashAlg = HashAlgorithm.Create(Settings.HashAlgorithmName) ?? SHA256.Create(); //Fallback to known hash algorithm
            msgProps.MessageHash = hashAlg.ComputeHash(newBuffer);

            //just for debugging
            //var hexStr = Convert.ToHexString(msgProps.MessageHash).ToUpperInvariant();

        }
    }
}