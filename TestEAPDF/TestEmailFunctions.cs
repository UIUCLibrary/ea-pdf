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

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestEmailFunctions
    {
        ILogger<EmailToXmlProcessor>? logger;
        ILoggerFactory? loggerFactory;
        bool validXml = true;
        List<string> loggedLines = new List<string>();

        string testFilesBaseDirectory = @"C:\Users\thabi\Source\UIUC\ea-pdf\SampleFiles\Testing";

        [TestInitialize]
        public void InitTest()
        {
            loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            //using StringListLogger for testing purposes https://www.nuget.org/packages/Extensions.Logging.ListOfString https://github.com/chrisfcarroll/TestBase
            loggerFactory.AddStringListLogger(loggedLines);
            logger = loggerFactory.CreateLogger<EmailToXmlProcessor>();
            logger.LogInformation("Starting Test");
        }

        [TestCleanup]
        public void EndTest()
        {
            if (logger != null) logger.LogInformation("Ending Test");
            if (loggerFactory != null) loggerFactory.Dispose();
        }

        //Mozilla mbox with child mboxes, different combinations of settings
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256----includeSubs", "SHA256", false, false, false, true, false, 0, 9, -1, DisplayName = "moz-dlf-sha256----includeSubs")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256-ext---includeSubs", "SHA256", true, false, false, true, false, 0, 9, -1, DisplayName = "moz-dlf-sha256-ext---includeSubs")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256", "SHA256", false, false, false, false, false, 0, 0, -1, DisplayName = "moz-dlf-sha256")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha1", "SHA1", false, false, false, false, false, 0, 0, -1, DisplayName = "moz-dlf-sha1")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256-ext", "SHA256", true, false, false, false, false, 0, 0, -1, DisplayName = "moz-dlf-sha256-ext")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256-ext-wrap", "SHA256", true, true, false, false, false, 0, 0, -1, DisplayName = "moz-dlf-sha256-ext-wrap")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256---presvEnc", "SHA256", false, false, true, false, false, 0, 0, -1, DisplayName = "moz-dlf-sha256---presvEnc")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "dlf-sha256-ext-wrap-presvEnc", "SHA256", true, true, true, false, false, 0, 0, -1, DisplayName = "moz-dlf-sha256-ext-wrap-presvEnc")]

        //Mozilla special files
        [DataRow("MozillaThunderbird\\Drafts", "", "SHA256", false, false, false, false, false, 0, 0, -1, DisplayName = "moz-drafts")]
        [DataRow("MozillaThunderbird\\Inbox", "", "SHA256", false, false, false, false, false, 0, 0, -1, DisplayName = "moz-inbox")]

        //Pine mbox folder
        [DataRow("Pine", "", "SHA256", false, false, false, false, false, 0, -1, -1, DisplayName = "pine-folder-one-file")]
        [DataRow("Pine", "pine-out-many", "SHA256", false, false, false, false, true, 0, -1, -1, DisplayName = "pine-folder-one-file-per-in-subfolder")]
        [DataRow("Pine", "pine-out-one", "SHA256", false, false, false, false, false, 0, -1, -1, DisplayName = "pine-folder-one-file-in-subfolder")]

        //Pine mbox files with special properties
        [DataRow("Pine\\sent-mail-mar-2000", "pine-sent-mail-mar-2000", "SHA256", false, false, false, false, false, 0, 1, -1, DisplayName = "pine-sent-mail-mar-2000")] //incomplete message because of unmangled 'From ' line
        [DataRow("Pine\\sent-mail-jul-2006", "pine-sent-mail-jul-2006", "SHA256", false, false, false, false, false, 0, 1, 0, DisplayName = "pine-sent-mail-jul-2006")] //not an mbox file
        [DataRow("Pine\\sent-mail-aug-2007", "pine-sent-mail-aug-2007", "SHA256", false, false, false, false, false, 0, 1, 0, DisplayName = "pine-sent-mail-aug-2007")] //not an mbox file
        [DataRow("Pine\\sent-mail-jun-2004", "pine-sent-mail-jun-2004", "SHA256", false, false, false, false, false, 0, 1, 0, DisplayName = "pine-sent-mail-jun-2004")] //not an mbox file

        //Gmail Exports
        [DataRow("Gmail\\account.mbox", "", "SHA256", false, false, false, false, false, 0, -1, -1, DisplayName = "gmail-mbox")] //gmail mbox export file

        [DataTestMethod]
        public void TestSampleFiles(string relInPath, string relOutPath, string hashAlg, bool extContent, bool wrapExtInXml, bool preserveEnc, bool includeSub, bool oneFilePerMbox, int expectedErrors, int expectedWarnings, int expectedCounts)
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
                    OneFilePerMbox = oneFilePerMbox
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
                        Assert.IsTrue(validMessageCount > 0);
                    else
                        Assert.AreEqual(expectedCounts, validMessageCount);
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
                        }
                    }

                }

                //make sure the xml files are valid
                foreach (var xmlFile in expectedXmlFiles)
                {
                    var xdoc = new XmlDocument();
                    xdoc.Load(xmlFile);

                    var xmlns = new XmlNamespaceManager(xdoc.NameTable);
                    xmlns.AddNamespace(EmailToXmlProcessor.XM, EmailToXmlProcessor.XM_NS);

                    //make sure the localId values start at 1 and all increase by 1
                    ValidateLocalIds(xdoc, xmlns);

                    //make sure hash values are correct for root-level mbox files

                    XmlNodeList? mboxNodes = xdoc.SelectNodes("/xm:Account/xm:Folder/xm:Mbox", xmlns);
                    if (mboxNodes != null)
                    {
                        foreach (XmlElement mboxElem in mboxNodes)
                        {
                            //get the hash values from the xml
                            XmlNode? hashValueNd = mboxElem.SelectSingleNode("xm:Hash/xm:Value", xmlns);
                            XmlNode? hashFuncNd = mboxElem.SelectSingleNode("xm:Hash/xm:Function", xmlns);
                            XmlNode? relPath = mboxElem.SelectSingleNode("xm:RelPath", xmlns);
                            string absPath = Path.Combine(outFolder, relPath?.InnerText ?? "");

                            //make sure hashes match
                            Assert.AreEqual(settings.HashAlgorithmName, hashFuncNd?.InnerText);
                            Assert.AreEqual(hashAlg, hashFuncNd?.InnerText);

                            var expectedHash = CalculateHash(hashAlg, absPath);
                            Assert.AreEqual(expectedHash, hashValueNd?.InnerText);
                        }
                    }


                    //make sure xml is schema valid 
                    ValidateXmlSchema(xdoc);

                    //make sure we have the expected number of error or warning messages
                    //negative numbers mean the count should be greater than the absolute value of the number, i.e. -1 means at least 1 error
                    if (expectedErrors <= -1)
                        Assert.IsTrue(StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Error]")).Count() >= -expectedErrors);
                    else
                        Assert.AreEqual(expectedErrors, StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Error]")).Count());

                    if (expectedWarnings <= -1)
                        Assert.IsTrue(StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Warning]")).Count() >= -expectedWarnings);
                    else
                        Assert.AreEqual(expectedWarnings, StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Warning]")).Count());

                    //if there are no messages in the file, we can skip the rest of the tests
                    if (validMessageCount == 0)
                        return;

                    //if there are multiple xml files, need to check each one for content, it might be the case that one file has content and the other doesn't because of an error or warning condition
                    var message = xdoc.SelectSingleNode("/xm:Account/xm:Folder/xm:Message", xmlns);
                    if (message == null)
                    {
                        //if there are no messages in the file make sure there is a warning or error comment
                        var comment = xdoc.SelectSingleNode("/xm:Account/xm:Folder/comment()[starts-with(. , 'WARNING') or starts-with(. , 'ERROR')]", xmlns);
                         Assert.IsNotNull(comment);
                        return;
                    }

                    if (extContent)
                    {
                        //make sure all attachments or binary content are external and that the file exists and hashes match
                        XmlNodeList? nodes = xdoc.SelectNodes("/xm:Account/xm:Folder/xm:Message//xm:SingleBody[xm:Disposition='attachment' or (not(starts-with(translate(xm:ContentType,'TEXT','tex'),'tex')) and not(starts-with(translate(xm:ContentType,'MESAG','mesag'),'message')))]", xmlns);
                        Assert.IsTrue(nodes != null && nodes.Count > 0);

                        var extdoc = new XmlDocument();
                        extdoc.Schemas.Add(EmailToXmlProcessor.XM_NS, EmailToXmlProcessor.XM_XSD);

                        foreach (XmlElement node in nodes)
                        {
                            Assert.IsNull(node.SelectSingleNode("xm:BodyContent", xmlns));
                            XmlElement? extNode = node.SelectSingleNode("xm:ExtBodyContent", xmlns) as XmlElement;
                            Assert.IsNotNull(extNode);
                            string? extPath = extNode.SelectSingleNode("xm:RelPath", xmlns)?.InnerText;
                            Assert.IsFalse(string.IsNullOrWhiteSpace(extPath));
                            var extFilepath = Path.Combine(outFolder, eProc.Settings.ExternalContentFolder, extPath);
                            Assert.IsTrue(File.Exists(extFilepath));
                            var extHash = CalculateHash(hashAlg, extFilepath);
                            string? calcHash = extNode.SelectSingleNode("xm:Hash/xm:Value", xmlns)?.InnerText;
                            Assert.AreEqual(extHash, calcHash);
                            string? calcHashAlg = extNode.SelectSingleNode("xm:Hash/xm:Function", xmlns)?.InnerText;
                            Assert.AreEqual(hashAlg, calcHashAlg);
                            if (wrapExtInXml)
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
                                var origEnc = node.SelectSingleNode("xm:TransferEncoding", xmlns)?.InnerText;
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
                                Assert.IsFalse(validXml);


                            }
                        }
                    }
                    else
                    {
                        //make sure all content is saved in the XML 
                        XmlNodeList? nodes = xdoc.SelectNodes("/xm:Account/xm:Folder/xm:Message//xm:SingleBody", xmlns);
                        Assert.IsTrue(nodes != null && nodes.Count > 0);
                        foreach (XmlElement node in nodes)
                        {
                            Assert.IsNull(node.SelectSingleNode("xm:ExtBodyContent", xmlns));
                            Assert.IsNotNull(node.SelectSingleNode("xm:BodyContent | xm:ChildMessage", xmlns));

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



                    //check that any PhantomBody elements are valid
                    XmlNodeList? nds = xdoc.SelectNodes("//xm:SingleBody[xm:PhantomBody]", xmlns);
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
                    XmlNodeList? nds2 = xdoc.SelectNodes("//xm:SingleBody[translate(xm:ContentType,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz') = 'message/external-body'] | //xm:SingleBody[translate(xm:OtherMimeHeader/xm:Name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz') = 'x-mozilla-external-attachment-url']", xmlns);
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
                        CheckThatAllMessagesAreDraft(xdoc, xmlns);
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
                    Assert.IsNotNull(draft);
                }
            }
        }

        void XmlValidationEventHandler(object? sender, ValidationEventArgs e)
        {
            validXml = false;
            if (e.Severity == XmlSeverityType.Warning)
            {
                if (logger != null) logger.LogWarning($"Line: {e.Exception.LineNumber} -- {e.Message}");
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                if (logger != null) logger.LogError($"Line: {e.Exception.LineNumber} -- {e.Message}");
            }
        }

        string CalculateHash(string algName, string filePath)
        {
            var fstream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var alg = HashAlgorithm.Create(algName) ?? SHA256.Create(); //Fallback to know hash algorithm
            var cstream = new CryptoStream(fstream, alg, CryptoStreamMode.Read);

            //read to end of stream
            int i = -1;
            do
            {
                i = cstream.ReadByte();
            } while (i != -1);

            var hash = alg.Hash ?? new byte[0];

            return Convert.ToHexString(hash);
        }

        void ValidateLocalIds(XmlDocument xdoc, XmlNamespaceManager xmlns)
        {
            //make sure the localId values start at 1 and all increase by 1
            var localIds = xdoc.SelectNodes("//xm:LocalId", xmlns);
            if (localIds != null)
            {
                long prevId = 0;
                long id = 0;
                foreach (XmlElement localId in localIds)
                {
                    if (long.TryParse(localId.InnerText, out id))
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
                Assert.Fail("No localIds found");
            }
        }

        void ValidateXmlSchema(XmlDocument xdoc)
        {
            validXml = true;
            xdoc.Schemas.Add(EmailToXmlProcessor.XM_NS, EmailToXmlProcessor.XM_XSD);
            Assert.IsTrue(xdoc.DocumentElement?.LocalName == "Account");
            xdoc.Validate(XmlValidationEventHandler, xdoc.DocumentElement);
            Assert.IsTrue(validXml);
        }
    }
}