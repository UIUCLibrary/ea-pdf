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
using PathHelpers = UIUCLibrary.EaPdf.Helpers.PathHelpers;

namespace UIUCLibrary.EaPdf
{
    public class EmailToXmlProcessor
    {

        //FUTURE: Add support for mbx files, see https://uofi.box.com/s/51v7xzfzqod2dv9lxmjgbrrgz5ejjydk 
        //TODO: Need to check for XML invalid characters almost anyplace I write XML string content, see the WriteElementStringReplacingInvalidChars function

        //for LWSP (Linear White Space) detection, compaction, and trimming
        const byte CR = 13;
        const byte LF = 10;
        const byte SP = 32;
        const byte TAB = 9;


        public const string XM = "xm";
        public const string XM_NS = "https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2";
        public const string XM_XSD = "eaxs_schema_v2.xsd";

        private readonly ILogger _logger;

        public const string HASH_DEFAULT = "SHA256";

        const string EX_MBOX_FROM_MARKER = "Failed to find mbox From marker";
        const string EX_MBOX_PARSE_HEADERS = "Failed to parse message headers";

        public EmailToXmlProcessorSettings Settings { get; }

        //stats used for development and debuging
        private Dictionary<string, int> contentTypeCounts = new();
        private Dictionary<string, int> xGmailLabelCounts = new();
        private Dictionary<string, int> xGmailLabelComboCounts = new();

        //need to keep track of folders in case output file is split into multiple files and the split happens while processing a subfolder
        private Stack<string> _folders = new();

        /// <summary>
        /// Create a processor for email files, initializing the logger and settings
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public EmailToXmlProcessor(ILogger<EmailToXmlProcessor> logger, EmailToXmlProcessorSettings settings)
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
            _logger.LogTrace("MboxProcessor Created");

        }

        //QUESTION: Can an EML file start with a 'From ' line just like an mbox file?
        public long ConvertEmlToEaxs()
        {
            //UNDONE
            return 0;
        }

        public long ConvertFolderOfEmlToEaxs()
        {
            //UNDONE
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
            if (!Helpers.PathHelpers.IsValidOutputPathForMboxFolder(fullMboxFolderPath, fullOutFolderPath))
            {
                throw new ArgumentException($"The outFolderPath, '{fullOutFolderPath}', cannot be the same as or a child of the mboxFolderPath, '{fullMboxFolderPath}'");
            }

            if (messageList == null)
            {
                messageList = new List<MessageBrief>(); //used to create the CSV file
            }

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

                _logger.LogInformation($"Files with messages: {filesWithMessagesCnt}, Files without messages: {filesWithoutMessagesCnt}, Total messages: {localId - startingLocalId}");
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

                WriteXmlAccountHeaderFields(xwriter, globalId, accntEmails);

                var mboxProps = new MboxProperties()
                {
                    GlobalId = globalId,
                    AccountEmails = accntEmails,
                    OutFilePath = xmlFilePath,
                };
                SetHashAlgorithm(mboxProps, xwriter);

                foreach (string mboxFilePath in Directory.EnumerateFiles(mboxFolderPath))
                {
                    WriteInfoMessage(xwriter, $"Processing mbox file: {mboxFilePath}");
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
            if (!Helpers.PathHelpers.IsValidOutputPathForMboxFile(fullMboxFilePath, fullOutFolderPath))
            {
                throw new ArgumentException($"The outFolderPath, '{fullOutFolderPath}', cannot be the same as or a child of the mboxFilePath, '{fullMboxFilePath}', ignoring any extensions");
            }

            var xmlFilePath = Path.Combine(fullOutFolderPath, Path.GetFileName(Path.ChangeExtension(fullMboxFilePath, "xml")));

            if (messageList == null)
            {
                messageList = new List<MessageBrief>(); //used to create the CSV file
            }

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

            WriteXmlAccountHeaderFields(xwriter, globalId, accntEmails);

            WriteInfoMessage(xwriter, $"Processing mbox file: {fullMboxFilePath}");

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
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(contentTypeCounts);
                }
            }
            
            using (var writer = new StreamWriter(csvFilepath, true))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(xGmailLabelCounts);
                }
            }

            using (var writer = new StreamWriter(csvFilepath, true))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(xGmailLabelComboCounts);
                }
            }

        }

        private void WriteXmlAccountHeaderFields(XmlWriter xwriter, string globalId, string accntEmails = "")
        {
            {
                xwriter.WriteProcessingInstruction("Settings", $"HashAlgorithmName: {Settings.HashAlgorithmName}, SaveAttachmentsAndBinaryContentExternally: {Settings.SaveAttachmentsAndBinaryContentExternally}, WrapExternalContentInXml: {Settings.WrapExternalContentInXml}, PreserveContentTransferEncodingIfPossible: {Settings.PreserveContentTransferEncodingIfPossible}, IncludeSubFolders: {Settings.IncludeSubFolders}, OneFilePerMbox: {Settings.OneFilePerMbox}");
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
            foreach(var fld in _folders.Reverse())
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
            MimeMessageProperties msgProps = new MimeMessageProperties();

            //open filestream and wrap it in a cryptostream so that we can hash the file as we process it
            using FileStream mboxStream = new FileStream(mboxProps.MboxFilePath, FileMode.Open, FileAccess.Read);

            //first look for magic numbers, so certain non-mbox files can be ignored
            byte[] magic = new byte[5];
            byte[] mbx = { 0x2a, 0x6d, 0x62, 0x78, 0x2a }; // *mbx* - Pine email format
            mboxStream.Read(magic, 0, magic.Length);
            if (magic.SequenceEqual(mbx))
            {
                //This is a Pine *mbx* file, so we can just skip it
                WriteWarningMessage(xwriter, $"File '{mboxProps.MboxFilePath}' is a Pine *mbx* file which cannot be parsed as an mbox file");
            }
            else
            {

                mboxStream.Position = 0; //reset file stream position to the start
                using CryptoStream cryptoStream = new CryptoStream(mboxStream, mboxProps.HashAlgorithm, CryptoStreamMode.Read);

                var parser = new MimeParser(cryptoStream, MimeFormat.Mbox);

                parser.MimeMessageEnd += (sender, e) => MimeMessageEndEventHandler(sender, e, mboxStream, mboxProps, msgProps);

                //Need to record the previous message so we can defer writing it to the XML until the next message can be interogated for error conditions 
                //and we can add the <Incomplete> tag if needed.
                MimeMessage? prevMessage = null;
                MimeMessage? message = null;

                while (!parser.IsEndOfStream)
                {
                    if (Settings.MaximumXmlFileSizeThreshold > 0 && xstream.Position >= Settings.MaximumXmlFileSizeThreshold)
                    {
                        var origXmlFilePath = mboxProps.OutFilePath;

                        //increment the file number; this also updates the OutFilePath
                        var fileNum = mboxProps.IncrementOutFileNumber();
                        
                        var newXmlFilePath = mboxProps.OutFilePath;
                        
                        //close any opened folder elements
                        for (int c = 0; c < _folders.Count; c++)
                        {
                            xwriter.WriteEndElement(); //Folder
                        }
                        
                        xwriter.WriteProcessingInstruction("ContinuedIn",$"'{Path.GetFileName(newXmlFilePath)}'");

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
                        xwriter.WriteProcessingInstruction("ContinuedFrom", $"'{Path.GetFileName(origXmlFilePath)}'");
                        WriteXmlAccountHeaderFields(xwriter, mboxProps.GlobalId, mboxProps.AccountEmails);
                        WriteInfoMessage(xwriter, $"Processing mbox file: {mboxProps.MboxFilePath}");
                        WriteFolders(xwriter);
                    }

                    try
                    {

                        message = parser.ParseMessage();

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
                            WriteWarningMessage(xwriter, $"{fex1.Message} -- skipping file, probably not an mbox file");
                            //return localId; //the file probably isn't an mbox file, so just bail on the whole file
                            break;
                        }
                        else
                        {
                            WriteErrorMessage(xwriter, $"{fex1.Message} -- this is unexpected");
                            parser.SetStream(cryptoStream, MimeFormat.Mbox); //reset the parser and try to continue
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
                            var msg = $"{fex2.Message} The content of the message is probably incomplete because of an unmangled 'From ' line in the message body. Content starting from offset {parser.MboxMarkerOffset} to the beginning of the next message will be skipped.";
                            _logger.LogWarning(msg);
                            msgProps.Incomplete(msg, $"Stream Position: {parser.MboxMarkerOffset}");

                            //FUTURE: Maybe try to recover lost message content when this happens.  This is probably very tricky except for the most basic cases of content-type: text/plain with no multipart messages or binary attachments
                        }

                        parser.SetStream(cryptoStream, MimeFormat.Mbox); //reset the parser and try to continue
                        continue; //skip the message, but keep going
                    }
                    catch (Exception ex)
                    {
                        WriteErrorMessage(xwriter, $"{ex.GetType().Name}: {ex.Message}");
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
                        WriteErrorMessage(xwriter, "Message is null");
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
                int i = -1;
                do
                {
                    i = cryptoStream.ReadByte();
                } while (i != -1);

                if (Settings.IncludeSubFolders)
                {
                    //TODO: Add accomodations for OneFilePerMbox and use the ReferencesAccount xml element
                    //TODO: May want to modify the XML Schema to allow for references to child folders instead of embedding child folders in the same mbox, see account-ref-type.  Maybe add ParentFolder type

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
                                //create new MboxProperties which is copy of parent MboxProperties except for the MboxFilePath and the checksum hash
                                MboxProperties childMboxProps = new MboxProperties(mboxProps)
                                {
                                    MboxFilePath = childMbox
                                };
                                SetHashAlgorithm(childMboxProps, xwriter);

                                //just try to process it, if no errors thrown, its probably an mbox file
                                WriteInfoMessage(xwriter, $"Processing Child Mbox: {childMbox}");
                                localId = ProcessMbox(childMboxProps, ref xwriter, ref xstream, localId, messageList);
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
                    mboxProps.EolCounts.TryGetValue("CR", out int crCount);
                    mboxProps.EolCounts.TryGetValue("LF", out int lfCount);
                    mboxProps.EolCounts.TryGetValue("CRLF", out int crlfCount);
                    WriteWarningMessage(xwriter, $"Mbox file contains multiple different EOLs: CR: {crCount}, LF: {lfCount}, CRLF: {crlfCount}");
                }
                if (mboxProps.HashAlgorithm.Hash != null)
                {
                    WriteHash(xwriter, mboxProps.HashAlgorithm.Hash, mboxProps.HashAlgorithmName);
                }
                else
                {
                    WriteWarningMessage(xwriter, $"Unable to calculate the hash value for the Mbox");
                }

                string cntMsg = $"File {mboxProps.MboxName} contains {mboxProps.MessageCount} valid messages";
                WriteInfoMessage(xwriter, cntMsg);

                xwriter.WriteEndElement(); //Mbox
            }

            xwriter.WriteEndElement(); //Folder
            _folders.Pop();

            return localId;
        }

        private void SetHashAlgorithm(MboxProperties mboxProps, XmlWriter xwriter)
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
                xwriter.WriteElementString("RelPath", XM_NS, Settings.ExternalContentFolder);
            }

            xwriter.WriteStartElement("LocalId", XM_NS);
            xwriter.WriteValue(localId);
            xwriter.WriteEndElement();

            //check for minimum required headers as per the XML schema, unless the message is a draft
            if (!isChildMessage && !MimeKitHelpers.TryGetDraft(mboxProps.MboxFilePath, message, out _))
            {
                if (message.Headers[HeaderId.From] == null)
                {
                    WriteWarningMessage(xwriter, "The message does not have a From header.");
                }
                if (message.Headers[HeaderId.Date] == null)
                {
                    WriteWarningMessage(xwriter, "The message does not have a Date header.");
                }
                if (message.Headers[HeaderId.To] == null && message.Headers[HeaderId.Cc] == null && message.Headers[HeaderId.Bcc] == null)
                {
                    WriteWarningMessage(xwriter, "The message does not have a To, Cc, or Bcc.");
                }
            }
            else
            {
                if (message.Headers[HeaderId.From] == null && message.Headers[HeaderId.Subject] == null && message.Headers[HeaderId.Date] == null)
                {
                    WriteWarningMessage(xwriter, "The child message does not have a From, Subject, or Date.");
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
                WriteMessageStatuses(xwriter, mboxProps.MboxFilePath, message);
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
            }

            return localId;
        }

        private void WriteMessageStatuses(XmlWriter xwriter, string mboxFilepath, MimeMessage message)
        {
            //StatusFlags
            string status = "";
            if (MimeKitHelpers.TryGetSeen(message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (MimeKitHelpers.TryGetAnswered(message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (MimeKitHelpers.TryGetFlagged(message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (MimeKitHelpers.TryGetDeleted(message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (MimeKitHelpers.TryGetDraft(mboxFilepath, message, out status))
            {
                xwriter.WriteElementString("StatusFlag", XM_NS, status);
            }
            if (MimeKitHelpers.TryGetRecent(message, out status))
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

            foreach (var addr in message.From)
            {
                WriteElementStringReplacingInvalidChars(xwriter, "From", XM_NS, addr.ToString());
            }

            if (message.Sender != null)
            {
                xwriter.WriteElementString("Sender", XM_NS, message.Sender.ToString());
            }

            foreach (var addr in message.To)
            {
                WriteElementStringReplacingInvalidChars(xwriter, "To", XM_NS, addr.ToString());
            }

            foreach (var addr in message.Cc)
            {
                WriteElementStringReplacingInvalidChars(xwriter, "Cc", XM_NS, addr.ToString());
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

        private long WriteMessageBody(XmlWriter xwriter, MimeEntity mimeEntity, long localId, bool expectingBodyContent, MboxProperties mboxProps, MimeMessageProperties msgProps)
        {
            bool isMultipart = false;

            MimePart? part = mimeEntity as MimePart;
            Multipart? multipart = mimeEntity as Multipart;
            MessagePart? message = mimeEntity as MessagePart;
            MessageDeliveryStatus? deliveryStatus = mimeEntity as MessageDeliveryStatus;

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
                WriteWarningMessage(xwriter, $"Item is multipart, but there are no parts");
                //need to write an empty part so that XML is schema valid
                xwriter.WriteStartElement("MissingBody", XM_NS);
                xwriter.WriteEndElement(); //MissingBody
            }
            else if (isMultipart && multipart == null)
            {
                WriteWarningMessage(xwriter, $"Item is erroneously flagged as multipart");
                //need to write an empty part so that XML is schema valid
                xwriter.WriteStartElement("MissingBody", XM_NS);
                xwriter.WriteEndElement(); //MissingBody
            }
            else if (!isMultipart)
            {
                if (deliveryStatus != null)
                {
                    localId = WriteDeliveryStatus(xwriter, deliveryStatus, localId, mboxProps);
                }
                else if (part != null && !MimeKitHelpers.IsXMozillaExternalAttachment(part))
                {
                    localId = WriteSingleBodyContent(xwriter, part, localId, expectingBodyContent, mboxProps);
                }
                else if (part != null && MimeKitHelpers.IsXMozillaExternalAttachment(part))
                {
                    WriteInfoMessage(xwriter, "The content is an inaccessible external attachment");
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
                WriteWarningMessage(xwriter, $"Delivery status message is malformed. It should have at least 2 status groups; it has only {deliveryStatus.StatusGroups.Count}. Writing message as a single body content instead.");
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
                var (text, encoding) = GetContentText(part);

                if (!string.IsNullOrWhiteSpace(text) || expectingBodyContent)
                {
                    xwriter.WriteStartElement("BodyContent", XM_NS);

                    if (!string.IsNullOrWhiteSpace(text) && !expectingBodyContent)
                    {
                        WriteWarningMessage(xwriter, $"Not expecting body content for '{part.ContentType.MimeType}'.");
                    }

                    xwriter.WriteStartElement("Content", XM_NS);

                    xwriter.WriteCData(text);

                    xwriter.WriteEndElement(); //Content

                    if (!string.IsNullOrWhiteSpace(encoding))
                    {
                        xwriter.WriteElementString("TransferEncoding", XM_NS, encoding);
                        xwriter.WriteComment($"WARNING: Used {encoding} because the content contains characters that are not valid in XML");
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

        private (string, string) GetContentText(MimePart part)
        {
            string contentStr = "";
            string encoding = "";

            if (!part.ContentType.IsMimeType("text", "*"))
            {
                throw new ArgumentException("The MimePart is not 'text/*'");
            }

            if (part != null && part.Content != null)
            {
                //Decode the stream and treat it as whatever the charset advertised in the content-type header
                using StreamReader reader = new StreamReader(part.Content.Open(), part.ContentType.CharsetEncoding, true);
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
                    _logger.LogWarning($"Characters not valid in XML.  Line {xex.LinePosition}: {xex.Message}", xex.LinePosition, xex.Message);
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

            return (contentStr, encoding);
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
                    WriteWarningMessage(xwriter, $"The TransferEncoding '{transferEncoding}' is not a recognized standard; treating it as '{MimeKitHelpers.GetContentEncodingString(part.ContentTransferEncoding, "default")}'.");
                }
                if (isMultipart && !transferEncoding.Equals("7bit", StringComparison.InvariantCultureIgnoreCase))
                {
                    WriteWarningMessage(xwriter, $"A multipart entity has a Content-Transfer-Encoding of '{transferEncoding}'; normally this should only be 7bit for multipart entities.");
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
                xwriter.WriteElementString("Value", XM_NS, hdr.Value);
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
                WriteWarningMessage(xwriter, "MIME type boundary parameter is missing for a multipart mime type");
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
                WriteInfoMessage(xwriter, $"LocalId {localId} written to external file");
            }

            var encoding = MimeKitHelpers.GetContentEncodingString(content.Encoding);
            var actualEncoding = "";
            if (part.Headers.Contains(HeaderId.ContentTransferEncoding))
            {
                actualEncoding = part.Headers[HeaderId.ContentTransferEncoding].ToLowerInvariant();
            }

            if (!MimeKitHelpers.ContentEncodings.Contains(actualEncoding, StringComparer.OrdinalIgnoreCase))
            {
                WriteWarningMessage(xwriter, $"The TransferEncoding '{actualEncoding}' is not a recognized standard; treating it as '{MimeKitHelpers.GetContentEncodingString(part.ContentTransferEncoding, "default")}'.");
            }

            xwriter.WriteStartElement("Content", XM_NS);

            //7bit and 8bit should be text content, so decode it and use the streamreader with the contenttype charset, if any, to get the text and write it to the xml in a cdata section.  Default is the same as 7bit.
            if (content.Encoding == ContentEncoding.EightBit || content.Encoding == ContentEncoding.SevenBit || content.Encoding == ContentEncoding.Default)
            {
                StreamReader reader = new StreamReader(content.Open(), part.ContentType.CharsetEncoding, true);
                xwriter.WriteCData(reader.ReadToEnd());
                encoding = String.Empty;
                if (content.Encoding == ContentEncoding.Default && !string.IsNullOrWhiteSpace(actualEncoding))
                {
                    WriteWarningMessage(xwriter, $"Using the default transfer encoding.  The actual transfer encoding is '{actualEncoding}' and charset encoding '{part.ContentType.CharsetEncoding}'.");
                    encoding = actualEncoding;
                }
            }
            else if (Settings.PreserveContentTransferEncodingIfPossible && (content.Encoding == ContentEncoding.UUEncode || content.Encoding == ContentEncoding.QuotedPrintable || content.Encoding == ContentEncoding.Base64))
            //use the original content encoding in the XML
            {
                //treat the stream as ASCII because it is already encoded and just write it out using the same encoding
                StreamReader reader = new StreamReader(content.Stream, System.Text.Encoding.ASCII);
                xwriter.WriteCData(reader.ReadToEnd());
                encoding = MimeKitHelpers.GetContentEncodingString(content.Encoding);
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

            bool wrapInXml = Settings.WrapExternalContentInXml;

            string randomFilePath = PathHelpers.GetRandomFilePath(mboxProps.OutDirectoryName);

            byte[] hash;

            if (!wrapInXml)
            {
                hash = SaveContentAsRaw(randomFilePath, part);

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
                    WriteWarningMessage(xwriter, msg);
                    wrapInXml = true;
                    File.Delete(randomFilePath); //so we can try saving a new stream there
                    hash = SaveContentAsXml(randomFilePath, part, localId, msg);
                }
            }
            else
            {
                hash = SaveContentAsXml(randomFilePath, part, localId, "");
            }

            _logger.LogTrace($"Created temporary file: '{randomFilePath}'");

            var hashFileName = PathHelpers.GetOutputFilePathBasedOnHash(hash, part, Path.Combine(mboxProps.OutDirectoryName, Settings.ExternalContentFolder), wrapInXml);
            //create folder if needed
            Directory.CreateDirectory(Path.GetDirectoryName(hashFileName) ?? "");

            //FUTURE: It might be good for performance to do this check prior to actually saving the temporary files
            //        Right now the hash is created by saving the temporary file, so this would require multiple passes through the stream, one to generate the hash and another to actually save it if needed
            
            //Deal with duplicate attachments, which should only be stored once, make sure the randomFilePath file is deleted
            if (File.Exists(hashFileName))
            {
                WriteInfoMessage(xwriter, "Duplicate attachment has already been saved");
                File.Delete(randomFilePath);
            }
            else
            {
                try
                {
                    File.Move(randomFilePath, hashFileName);
                    _logger.LogTrace($"File moved: '{randomFilePath}' -> '{hashFileName}'");
                }
                catch (IOException ioex)
                {
                    var msg = $"Content was not saved to external file.  {ioex.Message}";
                    WriteErrorMessage(xwriter, msg);
                }
            }

            xwriter.WriteStartElement("ExtBodyContent", XM_NS);
            xwriter.WriteElementString("RelPath", XM_NS, Path.GetRelativePath(Path.Combine(mboxProps.OutDirectoryName, Settings.ExternalContentFolder), hashFileName));

            //The CharSet and TransferEncoding elements are not needed here since they are same as for the SingleBody

            xwriter.WriteElementString("LocalId", XM_NS, localId.ToString());
            xwriter.WriteElementString("XMLWrapped", XM_NS, wrapInXml.ToString().ToLower());
            //Eol is not applicable since we are not wrapping the content in XML
            WriteHash(xwriter, hash, Settings.HashAlgorithmName);

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
        private byte[] SaveContentAsRaw(string filePath, MimePart part)
        {
            var content = part.Content;

            using var contentStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
            using var cryptoHashAlg = HashAlgorithm.Create(Settings.HashAlgorithmName) ?? SHA256.Create();  //Fallback to known hash algorithm
            using var cryptoStream = new CryptoStream(contentStream, cryptoHashAlg, CryptoStreamMode.Write);

            content.DecodeTo(cryptoStream);

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
        private byte[] SaveContentAsXml(string filePath, MimePart part, long localId, string comment)
        {
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
        private void WriteErrorMessage(XmlWriter xwriter, string message)
        {
            xwriter.WriteComment($"ERROR: {XmlHelpers.ReplaceInvalidXMLChars(message)}");
            _logger.LogError(message);
        }
        private void WriteWarningMessage(XmlWriter xwriter, string message)
        {
            xwriter.WriteComment($"WARNING: {XmlHelpers.ReplaceInvalidXMLChars(message)}");
            _logger.LogWarning(message);
        }
        private void WriteInfoMessage(XmlWriter xwriter, string message)
        {
            xwriter.WriteComment($"INFO: {XmlHelpers.ReplaceInvalidXMLChars(message)}");
            _logger.LogInformation(message);
        }

        public void WriteElementStringReplacingInvalidChars(XmlWriter xwriter, string localName, string? ns, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                xwriter.WriteElementString(localName, ns, value);
                return;
            }
            try
            {
                value = XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException xex)
            {
                value = XmlHelpers.ReplaceInvalidXMLChars(value);
                var msg = $"{localName} contains characters which are not allowed in XML; they have been replaced with \uFFFD.  Line {xex.LineNumber}: {xex.Message}";
                WriteWarningMessage(xwriter, msg);
            }

            xwriter.WriteElementString(localName, ns, value);

        }

        private void MimeMessageEndEventHandler(object? sender, MimeMessageEndEventArgs e, Stream mboxStream, MboxProperties mboxProps, MimeMessageProperties msgProps)
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