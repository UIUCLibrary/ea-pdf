using Microsoft.VisualStudio.TestTools.UnitTesting;
using UIUCLibrary.EaPdf;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Security.Cryptography;
using System;
using Extensions.Logging.ListOfString;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;
using System.Text.RegularExpressions;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestEmailToXmlFunctions
    {
        ILogger<EmailToXmlProcessor>? logger;
        ILoggerFactory? loggerFactory;
        bool validXml = true;
        List<string> loggedLines = new List<string>();

        const string UPPER = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string LOWER = "abcdefghijklmnopqrstuvwxyz";

        string testFilesBaseDirectory = @"C:\Users\thabi\Source\UIUC\ea-pdf\SampleFiles\Testing";

        [TestInitialize]
        public void InitTest()
        {
            //See https://codeburst.io/unit-testing-with-net-core-ilogger-t-e8c16c503a80#767c for how to use ILogger with unit testing
            //See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/ for more info on ILogger in .NET Core

            //log to the testing console standard error; log all message levels: trace, debug, ..., info
            loggerFactory = LoggerFactory.Create(builder => builder.AddConsole(opts => opts.LogToStandardErrorThreshold = LogLevel.Trace).SetMinimumLevel(LogLevel.Trace));

            //using StringListLogger for testing purposes https://www.nuget.org/packages/Extensions.Logging.ListOfString https://github.com/chrisfcarroll/TestBase
            loggerFactory.AddStringListLogger(loggedLines);

            logger = loggerFactory.CreateLogger<EmailToXmlProcessor>();
            logger.LogDebug("Starting Test");  //all loging done by the test scripts are debug level
        }

        [TestCleanup]
        public void EndTest()
        {
            if (logger != null) 
            {
                logger.LogDebug($"Information: {StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Information]")).Count()}");
                logger.LogDebug($"Warnings: {StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Warning]")).Count()}");
                logger.LogDebug($"Errors: {StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Error]")).Count()}");
                logger.LogDebug("Ending Test"); 
            }
            if (loggerFactory != null) loggerFactory.Dispose();
        }

        //The expected error, warning, and message counts were set by running the test scripts as of 2022-12-08

        //Gmail Exports
        [DataRow("Gmail\\Eml\\Inbox", "Inbox.out", "SHA256", false, false, false, false, false, 0, 1, 330, DisplayName = "gmail-emls")] //gmail mbox export file
        [DataRow("Gmail\\Eml\\Inbox\\2016-06-23 143920 d3eb274969.eml", "d3eb274969", "SHA256", false, false, false, false, false, 0, 0, 1, DisplayName = "gmail-emls-2016-06-23-143920-d3eb274969")] //gmail mbox export file with some weirdness
        [DataRow("D:\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "all.out", "SHA256", true, false, false, false, false, 0, 99796, 245734, false, DisplayName = "gmail-ext-big-mbox")] //very large gmail mbox export file, save external content
        [DataRow("D:\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "split.out", "SHA256", true, false, false, false, false, 0, 99796, 245734, false, 10000000, DisplayName = "gmail-ext-big-mbox-10000000")] //very large gmail mbox export file, save external content, split at 10MB
        [DataRow("Gmail\\account.mbox", "", "SHA256", false, false, false, false, false, 0, 1, 331, DisplayName = "gmail-mbox")] //gmail mbox export file

        //Mozilla mbox with child mboxes, different combinations of settings
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha1", "SHA1", false, false, false, false, false, 0, 0, 384, DisplayName = "moz-dlf-sha1")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256", "SHA256", false, false, false, false, false, 0, 0, 384, DisplayName = "moz-dlf-sha256")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256-ext", "SHA256", true, false, false, false, false, 0, 0, 475, DisplayName = "moz-dlf-sha256-ext")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256-ext---includeSubs", "SHA256", true, false, false, true, false, 0, 9, 848, DisplayName = "moz-dlf-sha256-ext---includeSubs")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256-ext-wrap", "SHA256", true, true, false, false, false, 0, 0, 475, DisplayName = "moz-dlf-sha256-ext-wrap")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256-ext-wrap-presvEnc", "SHA256", true, true, true, false, false, 0, 0, 475, DisplayName = "moz-dlf-sha256-ext-wrap-presvEnc")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256----includeSubs", "SHA256", false, false, false, true, false, 0, 9, 701, DisplayName = "moz-dlf-sha256----includeSubs")]
        //Test maximum output file size
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256----includeSubs-10000000", "SHA256", false, false, false, true, false, 0, 9, 701, false, 10000000, DisplayName = "moz-dlf-sha256----includeSubs-10000000")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256---presvEnc", "SHA256", false, false, true, false, false, 0, 0, 384, DisplayName = "moz-dlf-sha256---presvEnc")]
        //xhtml output
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256-xhtml", "SHA256", false, false, false, false, false, 0, 2, 384, false, 0, true, DisplayName = "moz-dlf-sha256-xhtml")]
        //Mozilla special files
        [DataRow("MozillaThunderbird\\Drafts", "", "SHA256", false, false, false, false, false, 0, 0, 26, DisplayName = "moz-drafts")]
        [DataRow("MozillaThunderbird\\Inbox", "", "SHA256", false, false, false, false, false, 0, 0, 21, DisplayName = "moz-inbox")]


        //Pine mbox folder
        [DataRow("Pine", "", "SHA256", false, false, false, false, false, 0, 114, 20795, DisplayName = "pine-folder-one-file")]
        [DataRow("Pine", "pine-out-one", "SHA256", false, false, false, false, false, 0, 114, 20795, DisplayName = "pine-folder-one-file-in-subfolder")]
        [DataRow("Pine", "pine-out-many", "SHA256", false, false, false, false, true, 0, 59, 20795, DisplayName = "pine-folder-one-file-per-in-subfolder")]
        //Pine mbox files with special properties
        [DataRow("Pine\\sent-mail-aug-2007", "pine-sent-mail-aug-2007", "SHA256", false, false, false, false, false, 0, 6, 1300, DisplayName = "pine-sent-mail-aug-2007")] //not an mbox file
        [DataRow("Pine\\sent-mail-mar-2000", "pine-sent-mail-mar-2000", "SHA256", false, false, false, false, false, 0, 1, 100, DisplayName = "pine-sent-mail-mar-2000")] //incomplete message because of unmangled 'From ' line
        [DataRow("Pine\\sent-mail-jul-2006", "pine-sent-mail-jul-2006", "SHA256", false, false, false, false, false, 0, 0, 466, DisplayName = "pine-sent-mail-jul-2006")] //not an mbox file
        [DataRow("Pine\\sent-mail-jun-2004", "pine-sent-mail-jun-2004", "SHA256", false, false, false, false, false, 0, 1, 418, DisplayName = "pine-sent-mail-jun-2004")] //not an mbox file


        //Weird Emails
        [DataRow("Weird\\missing_ext2.mbox", "out_missing_ext2", "SHA256", true, false, false, false, false, 0, 0, 2, DisplayName = "weird-missing-ext2-mbox")] //message from very large mbox seems to be missing external files
        [DataRow("Weird\\missing_ext.mbox", "out_missing_ext", "SHA256", true, false, false, false, false, 0, 0, 3, DisplayName = "weird-missing-ext-mbox")] //message from very large mbox seems to be missing external files
        [DataRow("Weird\\rfc822headers2.mbox", "out_rfc822headers2", "SHA256", true, false, false, false, false, 0, 0, 2, DisplayName = "weird-rfc822headers2-mbox")] //message from very large mbox which contains txt/rfc822-headers
        [DataRow("Weird\\rfc822headers.mbox", "out_rfc822headers", "SHA256", true, false, false, false, false, 0, 0, 2, DisplayName = "weird-rfc822headers-mbox")] //message from very large mbox which contains txt/rfc822-headers
        [DataRow("Weird\\spam_hexa.mbox", "", "SHA256", false, false, false, false, false, 0, 2, 1, DisplayName = "weird-spam-hexa-mbox")] //weird spam email with 'hexa' encoded content
        [DataRow("Weird\\virus_notif.mbox", "", "SHA256", false, false, false, false, false, 0, 1, 2, DisplayName = "weird-virus-notif-mbox")] //weird virus notification with multipart/report, message/delivery-report. and text/rfc822-headers content types
        [DataRow("Weird\\virus_payload.mbox", "out_virus_payload", "SHA256", true, false, false, false, false, 0, 1, 2, DisplayName = "weird-virus-payload-mbox")] //message from very large mbox which contains a virus payload


        [DataTestMethod]
        public void TestSampleFiles
            (
            string relInPath,
            string relOutPath,
            string hashAlg,
            bool extContent,
            bool wrapExtInXml,
            bool preserveEnc,
            bool includeSub,
            bool oneFilePerMbox,
            int expectedErrors,
            int expectedWarnings,
            int expectedCounts,
            bool quick = false, //default to check everything
            long maxOutFileSize = 0, //default to no max
            bool xhtml = false
            )
        {

            testFilesBaseDirectory = Path.GetDirectoryName(Path.Combine(testFilesBaseDirectory, relInPath)) ?? testFilesBaseDirectory;
            string testFileName = Path.GetFileName(relInPath);

            if (logger != null)
            {
                var settings = new EmailToXmlProcessorSettings()
                {
                    HashAlgorithmName = hashAlg,
                    SaveAttachmentsAndBinaryContentExternally = extContent,
                    WrapExternalContentInXml = wrapExtInXml,
                    PreserveContentTransferEncodingIfPossible = preserveEnc,
                    IncludeSubFolders = includeSub,
                    OneFilePerMbox = oneFilePerMbox,
                    MaximumXmlFileSize = maxOutFileSize,
                    SaveTextAsXhtml = xhtml
                };

                //Also save the sample test files in the test project and automate the folder setup and cleanup
                var sampleFile = Path.Combine(testFilesBaseDirectory, testFileName);


                var expectedOutFolder = "";
                var outFolder = "";

                if (Directory.Exists(sampleFile))
                {
                    //sampleFile is a directory, so the output folder is relative to the sample folder's parent
                    expectedOutFolder = Path.Combine(Path.GetDirectoryName(sampleFile) ?? "", relOutPath);
                    outFolder = Path.Combine(Path.GetDirectoryName(sampleFile) ?? "", relOutPath);
                }
                else if (File.Exists(sampleFile))
                {
                    //sample file is a file, so the output folder is relative to the sample file's parent directory
                    expectedOutFolder = Path.Combine(testFilesBaseDirectory, relOutPath);
                    outFolder = Path.Combine(testFilesBaseDirectory, relOutPath);
                }
                else
                {
                    Assert.Fail($"Sample file or folder '{sampleFile}' does not exist");
                }

                //clean out the output folder
                if (!string.IsNullOrWhiteSpace(relOutPath) && Directory.Exists(outFolder))
                {
                    Directory.Delete(outFolder, true);
                }

                var eProc = new EmailToXmlProcessor(logger, settings);

                long validMessageCount = 0;
                if (Directory.Exists(sampleFile))
                {
                    validMessageCount = eProc.ConvertFolderOfMboxToEaxs(sampleFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                    if (expectedCounts == -1)
                        Assert.IsTrue(validMessageCount > 0, "Expected some valid messages");
                    else
                        Assert.AreEqual(expectedCounts, validMessageCount, "Expected valid message count does not match");
                }
                else if (File.Exists(sampleFile))
                {
                    validMessageCount = eProc.ConvertMboxToEaxs(sampleFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                    if (expectedCounts == -1)
                        Assert.IsTrue(validMessageCount > 0);
                    else
                        Assert.AreEqual(expectedCounts, validMessageCount);
                }
                else
                {
                    Assert.Fail($"Sample file or folder '{sampleFile}' does not exist");
                }

                //make sure output folders and files exist
                Assert.AreEqual(expectedOutFolder, outFolder);
                Assert.IsTrue(Directory.Exists(outFolder));

                string csvPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "csv"));
                Assert.IsTrue(File.Exists(csvPathStr));

                List<string> expectedXmlFiles = new List<string>();

                if (!oneFilePerMbox)
                {
                    string xmlPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "xml"));
                    Assert.IsTrue(File.Exists(xmlPathStr));
                    expectedXmlFiles.Add(xmlPathStr);

                    //Output might be split into multiple files
                    var files = Directory.GetFiles(outFolder, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_????.xml");
                    if (files != null) 
                    {
                        expectedXmlFiles.AddRange(files.Where(f => Regex.IsMatch(f, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_\\d{{4}}.xml")).ToList());
                    }
                }
                else
                {
                    if (Directory.Exists(sampleFile))
                    {
                        //the input path is a directory, so make sure there is one xml output file for each input file
                        foreach (var file in Directory.GetFiles(sampleFile))
                        {
                            string xmlPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(file), "xml"));
                            Assert.IsTrue(File.Exists(xmlPathStr));
                            expectedXmlFiles.Add(xmlPathStr);
                            
                            //Output might be split into multiple files
                            var files = Directory.GetFiles(outFolder, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_????.xml");
                            if (files != null)
                            {
                                expectedXmlFiles.AddRange(files.Where(f => Regex.IsMatch(f, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_\\d{{4}}.xml")).ToList());
                            }


                        }
                    }

                }

                if (quick) return;

                //make sure the xml files are valid
                foreach (var xmlFile in expectedXmlFiles)
                {
                    logger.LogDebug($"Validating xml file '{xmlFile}'");

                    //use an XmlReader to validate with line numbers as soon as the Xml document is loaded
                    XmlReaderSettings rdrSettings = new XmlReaderSettings();
                    rdrSettings.Schemas = new XmlSchemaSet();
                    rdrSettings.Schemas.Add(EmailToXmlProcessor.XM_NS, EmailToXmlProcessor.XM_XSD);
                    rdrSettings.ValidationType = ValidationType.Schema;
                    rdrSettings.ValidationEventHandler += XmlValidationEventHandler;
                    rdrSettings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;

                    validXml = true;
                    var xRdr = XmlReader.Create(xmlFile, rdrSettings);
                    var xDoc = new XmlDocument(); //this will validate the XML
                    xDoc.Load(xRdr);
                    Assert.IsTrue(validXml);

                    var xmlns = new XmlNamespaceManager(xDoc.NameTable);
                    xmlns.AddNamespace(EmailToXmlProcessor.XM, EmailToXmlProcessor.XM_NS);

                    //make sure the localId values in a each xml file all increase by 1
                    ValidateLocalIds(xDoc, xmlns);

                    //make sure hash values and sizes are correct for root-level mbox files

                    XmlNodeList? mboxNodes = xDoc.SelectNodes("/xm:Account/xm:Folder/xm:Mbox", xmlns);
                    if (mboxNodes != null)
                    {
                        foreach (XmlElement mboxElem in mboxNodes)
                        {
                            //get the hash values from the xml
                            XmlNode? hashValueNd = mboxElem.SelectSingleNode("xm:Hash/xm:Value", xmlns);
                            XmlNode? hashFuncNd = mboxElem.SelectSingleNode("xm:Hash/xm:Function", xmlns);
                            XmlNode? sizeNd = mboxElem.SelectSingleNode("xm:Size", xmlns);
                            XmlNode? relPath = mboxElem.SelectSingleNode("xm:RelPath", xmlns);
                            string absPath = Path.Combine(outFolder, relPath?.InnerText ?? "");

                            //make sure hashes match
                            Assert.AreEqual(settings.HashAlgorithmName, hashFuncNd?.InnerText);
                            Assert.AreEqual(hashAlg, hashFuncNd?.InnerText);

                            var expectedHash = CalculateHash(hashAlg, absPath);
                            Assert.AreEqual(expectedHash, hashValueNd?.InnerText);

                            //make sure size match
                            FileInfo fi = new FileInfo(absPath);
                            long expectedSize = fi.Length;
                            long actualSize = long.Parse(sizeNd?.InnerText ?? "-1");
                            Assert.AreEqual(expectedSize, actualSize);

                        }
                    }


                    //make sure we have the expected number of error or warning messages
                    //negative numbers mean the count should be greater than the absolute value of the number, i.e. -1 means at least 1 error
                    if (expectedErrors <= -1)
                        Assert.IsTrue(StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Error]")).Count() >= -expectedErrors, "Expected some errors");
                    else
                        Assert.AreEqual(expectedErrors, StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Error]")).Count(), "Expected error count does not match");

                    if (expectedWarnings <= -1)
                        Assert.IsTrue(StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Warning]")).Count() >= -expectedWarnings, "Expected some warnings");
                    else
                        Assert.AreEqual(expectedWarnings, StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Warning]")).Count(), "Expected warning count does not match");

                    //if there are no messages in the file, we can skip the rest of the tests
                    if (validMessageCount == 0)
                        return;

                    //if there are multiple xml files, need to check each one for content, it might be the case that one file has content and the other doesn't because of an error or warning condition
                    var message = xDoc.SelectSingleNode("/xm:Account//xm:Folder/xm:Message", xmlns);
                    if (message == null)
                    {
                        //if there are no messages in the file make sure there is a warning or error comment
                        var comment = xDoc.SelectSingleNode("/xm:Account//xm:Folder/comment()[starts-with(. , 'WARNING') or starts-with(. , 'ERROR')]", xmlns);
                        Assert.IsNotNull(comment);
                        return;
                    }

                    if (extContent)
                    {
                        //make sure all attachments or binary content are external and that the file exists and hashes match
                        //get all SingleBody elements that don't have a child element ChildMessage or child element DeliveryStatus and are attachments or not text/... or message/... mime types
                        XmlNodeList? extNodes = xDoc.SelectNodes($"/xm:Account//xm:Folder/xm:Message//xm:SingleBody[not(xm:ChildMessage) and not(xm:DeliveryStatus) and (xm:Disposition='attachment' or (not(starts-with(translate(xm:ContentType,'{UPPER}','{LOWER}'),'text/')) and not(starts-with(translate(xm:ContentType,'{UPPER}','{LOWER}'),'message/'))))]", xmlns);

                        if (extNodes != null)
                        {
                            var extdoc = new XmlDocument();
                            extdoc.Schemas.Add(EmailToXmlProcessor.XM_NS, EmailToXmlProcessor.XM_XSD);

                            foreach (XmlElement singleBodyNd in extNodes)
                            {

                                Assert.IsNull(singleBodyNd.SelectSingleNode("xm:BodyContent", xmlns));
                                XmlElement? extNode = singleBodyNd.SelectSingleNode("xm:ExtBodyContent", xmlns) as XmlElement;
                                XmlElement? phantomNode = singleBodyNd.SelectSingleNode("xm:PhantomBody", xmlns) as XmlElement;


                                string? id = null;
                                if (extNode == null)
                                {
                                    var msgIdNd = singleBodyNd.SelectSingleNode("ancestor::xm:Message/xm:MessageId", xmlns) as XmlElement;
                                    id = msgIdNd?.InnerText;
                                }

                                Assert.IsTrue(extNode != null || phantomNode != null, $"Message Id: {id} should have external body content and/or phantom content");

                                if(extNode==null) //no need for further checks
                                    continue;

                                string? extPath = extNode.SelectSingleNode("xm:RelPath", xmlns)?.InnerText;
                                Assert.IsFalse(string.IsNullOrWhiteSpace(extPath));
                                var extFilepath = Path.Combine(outFolder, eProc.Settings.ExternalContentFolder, extPath);
                                Assert.IsTrue(File.Exists(extFilepath));

                                //make sure the hash values match
                                var extHash = CalculateHash(hashAlg, extFilepath);
                                if (string.IsNullOrEmpty(extHash))
                                {
                                    if (logger != null) logger.LogDebug($"Unable to calculate the hash for external file: {extFilepath}");
                                }
                                else
                                {
                                    string? calcHash = extNode.SelectSingleNode("xm:Hash/xm:Value", xmlns)?.InnerText;
                                    Assert.AreEqual(calcHash, extHash);
                                }
                                string? calcHashAlg = extNode.SelectSingleNode("xm:Hash/xm:Function", xmlns)?.InnerText;
                                Assert.AreEqual(hashAlg, calcHashAlg);

                                //make sure the size values match
                                FileInfo fi = new FileInfo(extFilepath);
                                long expectedSize = fi.Length;
                                long actualSize = long.Parse(extNode.SelectSingleNode("xm:Size", xmlns)?.InnerText ?? "-1");
                                Assert.AreEqual(expectedSize, actualSize);

                                //get the actual wrapped in xml indicator
                                var xmlWrappedStr = extNode.SelectSingleNode("xm:XMLWrapped", xmlns)?.InnerText ?? "false";
                                bool actualWrapExtInXml = false;
                                if (bool.TryParse(xmlWrappedStr, out bool b))
                                    actualWrapExtInXml = b;

                                if (actualWrapExtInXml != wrapExtInXml && wrapExtInXml)
                                {
                                    var msg = $"External file: {extFilepath} is not wrapped in xml as expected";
                                    if (logger != null) logger.LogDebug(msg);
                                    Assert.Fail(msg); //shouldn't be any exceptions to this
                                }
                                else if (actualWrapExtInXml != wrapExtInXml && !wrapExtInXml)
                                {
                                    var msg = $"External file: {extFilepath} is wrapped in xml when it shouldn't be";
                                    if (logger != null) logger.LogDebug(msg);
                                    //Assert.Inconclusive(msg); //inconclusive since it might be wrapped in XML because of a virus or other file IO problem
                                }

                                if (actualWrapExtInXml)
                                {
                                    //make sure external content is wrapped in XML
                                    var wrapped = extNode.SelectSingleNode("xm:XMLWrapped", xmlns)?.InnerText ?? "false";
                                    Assert.IsTrue(wrapped.Equals("true", StringComparison.OrdinalIgnoreCase));

                                    Assert.IsTrue(Path.GetExtension(extFilepath) == ".xml");

                                    //make sure external file is valid xml
                                    validXml = true;
                                    extdoc.Load(extFilepath);
                                    Assert.IsTrue(extdoc.DocumentElement?.LocalName == "BodyContent");
                                    Assert.IsTrue(extdoc.DocumentElement?.NamespaceURI == EmailToXmlProcessor.XM_NS);
                                    extdoc.Validate(XmlValidationEventHandler, extdoc.DocumentElement);
                                    Assert.IsTrue(validXml);

                                    //Test the preserve transfer encoding setting
                                    var enc = extdoc.SelectSingleNode("/xm:BodyContent/xm:TransferEncoding", xmlns)?.InnerText;
                                    var origEnc = singleBodyNd.SelectSingleNode("xm:TransferEncoding", xmlns)?.InnerText;
                                    if (preserveEnc)
                                    {
                                        if (enc != null)
                                            Assert.AreEqual(origEnc, enc);
                                    }
                                    else
                                    {
                                        if (enc != null)
                                            Assert.AreEqual("base64", enc);
                                    }

                                }
                                else
                                {
                                    //make sure is not wrapped
                                    var wrapped = extNode.SelectSingleNode("xm:XMLWrapped", xmlns)?.InnerText ?? "false";
                                    Assert.IsTrue(wrapped.Equals("false", StringComparison.OrdinalIgnoreCase));
                                    //Test that file is not XML
                                    validXml = true;
                                    try
                                    {
                                        extdoc.Load(extFilepath);
                                        //must be well formed XML, so make sure it is not a wrapped email content xml
                                        Assert.IsFalse(extdoc.DocumentElement?.LocalName == "BodyContent");
                                        Assert.IsFalse(extdoc.DocumentElement?.NamespaceURI == EmailToXmlProcessor.XM_NS);
                                        validXml = false;  //if it gets here, it is XML but not a wrapped email content, so it is invalid
                                    }
                                    catch (XmlException)
                                    {
                                        //probably not even XML
                                        validXml = false;
                                    }
                                    catch (IOException ioex)
                                    {
                                        //file might be a virus or have some other problem, assume it is not valid XML
                                        if (logger != null) logger.LogDebug(ioex.Message);
                                        validXml = false;
                                    }
                                    Assert.IsFalse(validXml);


                                }
                            }
                        }
                    }
                    else
                    {
                        //make sure all content is saved in the XML 
                        XmlNodeList? nodes = xDoc.SelectNodes("/xm:Account//xm:Folder/xm:Message//xm:SingleBody", xmlns);
                        Assert.IsTrue(nodes != null && nodes.Count > 0);
                        foreach (XmlElement node in nodes)
                        {

                            var id = node.SelectSingleNode("ancestor::xm:Message/xm:MessageId", xmlns)?.InnerText;
                            
                            //logger.LogDebug($"RFC822 Message Id: {id}");

                            Assert.IsNull(node.SelectSingleNode("xm:ExtBodyContent", xmlns));

                            //unless this is a 'text/rfc822-headers', make sure there is a body
                            if (node.SelectSingleNode($"ancestor::xm:SingleBody[translate(xm:ContentType,'{UPPER}','{LOWER}') = 'text/rfc822-headers']", xmlns) == null)
                            {
                                Assert.IsNotNull(node.SelectSingleNode("xm:BodyContent | xm:ChildMessage | xm:DeliveryStatus | xm:PhantomBody", xmlns));
                            }
                            else
                            {
                                Assert.IsNull(node.SelectSingleNode("xm:BodyContent | xm:ChildMessage | xm:DeliveryStatus | xm:PhantomBody", xmlns));
                            }


                            //Test the preserve transfer encoding setting
                            var enc = node.SelectSingleNode("xm:BodyContent/xm:TransferEncoding", xmlns)?.InnerText;
                            var origEnc = node.SelectSingleNode("xm:TransferEncoding", xmlns)?.InnerText;
                            if (preserveEnc)
                            {
                                if (enc != null)
                                    Assert.AreEqual(origEnc, enc);
                            }
                            else
                            {
                                //quoted-printable will be used if the content is text but has XML invalid control chars 
                                //otherwise it should be base64
                                if (enc != null)
                                {
                                    var contentType = node.SelectSingleNode("xm:ContentType", xmlns)?.InnerText;

                                    Assert.IsTrue(enc == "base64" || (enc == "quoted-printable" && contentType != null && contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)));
                                }
                            }

                        }
                    }

                    //If mime type is message/rfc822 or text/rfc822-headers, make sure there is a ChildMessage 
                    var rfc822Nds = xDoc.SelectNodes($"//xm:SingleBody[translate(xm:ContentType,'{UPPER}','{LOWER}') = 'text/rfc822-headers' or translate(xm:ContentType,'{UPPER}','{LOWER}') = 'message/rfc822']", xmlns);
                    if (rfc822Nds != null)
                    {
                        foreach (XmlElement nd in rfc822Nds)
                        {
                            var id = nd.SelectSingleNode("ancestor::xm:Message/xm:MessageId", xmlns)?.InnerText;
                            
                            //logger.LogDebug($"RFC822 Message Id: {id}");
                            
                            var encoding = nd.SelectSingleNode("xm:TransferEncoding", xmlns)?.InnerText ?? "";
                            if (!new[]{"7bit", "8bit", "binary", ""}.Contains(encoding))
                            {
                                //if the encoding is anything other than 7bit, 8bit, or binary, it is treated as normal BodyContent
                                Assert.IsNotNull(nd.SelectSingleNode("xm:BodyContent", xmlns));
                                continue;
                            }

                            var childNds = nd.SelectNodes("xm:ChildMessage", xmlns);
                            Assert.IsNotNull(childNds);
                            Assert.AreEqual(1,childNds.Count);

                            //if the mime type is text/rfc822-headers, make sure the ChildMessage has no content, it might have a body but it should be empty
                            if (nd.SelectSingleNode("xm:ContentType", xmlns)?.InnerText.ToLower() == "text/rfc822-headers")
                            {
                                var childMsg = childNds[0] as XmlElement;
                                if (childMsg != null)
                                {
                                    Assert.IsNull(childMsg.SelectSingleNode("xm:SingleBody/xm:BodyContent", xmlns));
                                    Assert.IsNull(childMsg.SelectSingleNode("xm:SingleBody/xm:ExtBodyContent", xmlns));
                                    Assert.IsNull(childMsg.SelectSingleNode("xm:SingleBody/xm:ChildMessage", xmlns));
                                    Assert.IsNull(childMsg.SelectSingleNode("xm:SingleBody/xm:DeliveryStatus", xmlns));

                                    Assert.IsNull(childMsg.SelectSingleNode("xm:MultiBody/xm:MultiBody", xmlns));
                                    Assert.IsNull(childMsg.SelectSingleNode("xm:MultiBody/xm:SingleBody", xmlns));

                                }
                            }
                        }
                    }


                    //check that any PhantomBody elements are valid
                    XmlNodeList? nds = xDoc.SelectNodes("//xm:SingleBody[xm:PhantomBody]", xmlns);
                    if (nds != null)
                    {
                        foreach (XmlElement nd in nds)
                        {
                            //check that the content-type is message/external-body or that there are X-Mozilla-* headers
                            var contentType = nd.SelectSingleNode("xm:ContentType", xmlns)?.InnerText;
                            var xMozillaExternal = nd.SelectSingleNode("xm:OtherMimeHeader/xm:Name['X-Mozilla-External-Attachment-URL']", xmlns);
                            Assert.IsTrue((!string.IsNullOrWhiteSpace(contentType) && contentType.Equals("message/external-body", StringComparison.OrdinalIgnoreCase)) || (xMozillaExternal != null));
                        }
                    }
                    XmlNodeList? nds2 = xDoc.SelectNodes($"//xm:SingleBody[translate(xm:ContentType,'{UPPER}','{LOWER}') = 'message/external-body'] | //xm:SingleBody[translate(xm:OtherMimeHeader/xm:Name,'{UPPER}','{LOWER}') = 'x-mozilla-external-attachment-url']", xmlns);
                    if (nds2 != null)
                    {
                        foreach (XmlElement nd in nds2)
                        {
                            //check that there is a PhantomBody element
                            var phantom = nd.SelectSingleNode("xm:PhantomBody", xmlns);
                            Assert.IsNotNull(phantom);
                        }
                    }

                    if (relInPath == "MozillaThunderbird\\Drafts")
                    {
                        //make sure each message is marked as draft
                        CheckThatAllMessagesAreDraft(xDoc, xmlns);
                    }

                }

            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }
        }

        [DataRow("MozillaThunderbird\\Drafts", "MozillaThunderbird\\folder", false, DisplayName = "path_check_drafts_folder")]
        [DataRow("MozillaThunderbird\\Drafts", "MozillaThunderbird\\\\Drafts.out", true, DisplayName = "path_check_drafts_out")]
        [DataRow("MozillaThunderbird\\Drafts", "MozillaThunderbird\\\\Drafts.out\\test", true, DisplayName = "path_check_drafts_out_test")]
        [DataTestMethod]
        public void TestPathErrorChecks(string inFilePath, string outFolderPath, bool shouldFail)
        {

            if (logger != null)
            {
                var settings = new EmailToXmlProcessorSettings();
                var eProc = new EmailToXmlProcessor(logger, settings);

                var inFile = Path.Combine(testFilesBaseDirectory, inFilePath);
                var outFolder = Path.Combine(testFilesBaseDirectory, outFolderPath);

                try
                {
                    var validMessageCount = eProc.ConvertMboxToEaxs(inFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                    if (shouldFail)
                    {
                        Assert.Fail("Expected an ArgumentException");
                    }
                }
                catch (ArgumentException aex)
                {
                    if (!shouldFail)
                    {
                        Assert.Fail(aex.Message);
                    }
                }
                finally
                {
                    //cleanup
                    if (Directory.Exists(outFolder))
                    {
                        Directory.Delete(outFolder, true);
                    }
                }
            }
        }

        void CheckThatAllMessagesAreDraft(XmlDocument xdoc, XmlNamespaceManager xmlns)
        {
            var messages = xdoc.SelectNodes("/xm:Account/xm:Folder/xm:Message", xmlns);
            if (messages != null)
            {
                foreach (XmlElement message in messages)
                {
                    var draft = message.SelectSingleNode("xm:StatusFlag[normalize-space(text()) = 'Draft']", xmlns);
                    var deleted = message.SelectSingleNode("xm:StatusFlag[normalize-space(text()) = 'Deleted']", xmlns);
                    //if it is deleted, it may not be marked as draft even if it is in the draft folder
                    Assert.IsTrue(draft != null || deleted != null);
                }
            }
        }

        void XmlValidationEventHandler(object? sender, ValidationEventArgs e)
        {
            validXml = false;
            if (e.Severity == XmlSeverityType.Warning)
            {
                if (logger != null) logger.LogDebug($"Line: {e.Exception.LineNumber} -- {e.Message}");
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                if (logger != null) logger.LogDebug($"Line: {e.Exception.LineNumber} -- {e.Message}");
            }
        }

        string CalculateHash(string algName, string filePath)
        {
            byte[] hash = new byte[0];

            using var alg = HashAlgorithm.Create(algName) ?? SHA256.Create(); //Fallback to know hash algorithm

            try
            {
                using var fstream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                hash = alg.ComputeHash(fstream);
            }
            catch
            {
                hash = new byte[0];
            }

            return Convert.ToHexString(hash);

        }

        void ValidateLocalIds(XmlDocument xdoc, XmlNamespaceManager xmlns)
        {
            //make sure the localId values in a given xml file all increase by 1
            var localIds = xdoc.SelectNodes("//xm:LocalId", xmlns);
            if (localIds != null && localIds.Count > 0)
            {
                //init the prevId to one less than the first localId
                if(!long.TryParse(localIds[0]?.InnerText, out long prevId))
                {
                    Assert.Fail("localId is not a number");
                }
                prevId--;
                
                foreach (XmlElement localId in localIds)
                {
                    if (long.TryParse(localId.InnerText, out long id))
                    {
                        Assert.AreEqual(id, prevId + 1);
                        prevId = id;
                    }
                    else
                    {
                        Assert.Fail("localId is not a number");
                    }
                }
            }
            else
            {
                //This is not an error because some files will contain no messages, usually if the source file was not a valid mbox file
                //Assert.Fail("No localIds found");
                if(logger !=null) logger.LogDebug("No localIds found");
            }
        }

    }
}