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

namespace UIUCLibrary.TestEAPDF
{
    [TestClass]
    public class TestEmailFunctions
    {
        ILogger<EmailProcessor>? logger;
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
            logger = loggerFactory.CreateLogger<EmailProcessor>();
            logger.LogInformation("Starting Test");
        }

        [TestCleanup]
        public void EndTest()
        {
            if (logger != null) logger.LogInformation("Ending Test");
            if (loggerFactory != null) loggerFactory.Dispose();
        }

        [DataRow("SHA256", false, false, false, "sha256----includeSubs", true, DisplayName = "sha256----includeSubs")]
        [DataRow("SHA256", true, false, false, "sha256-ext---includeSubs", true, DisplayName = "sha256-ext---includeSubs")]
        [DataRow("SHA256", false, false, false, "sha256", false, DisplayName = "sha256")]
        [DataRow("SHA1", false, false, false, "sha1", false, DisplayName = "sha1")]
        [DataRow("SHA256", true, false, false, "sha256-ext", false, DisplayName = "sha256-ext")]
        [DataRow("SHA256", true, true, false, "sha256-ext-wrap", false, DisplayName = "sha256-ext-wrap")]
        [DataRow("SHA256", false, false, true, "sha256---presvEnc", false, DisplayName = "sha256---presvEnc")]
        [DataRow("SHA256", true, true, true, "sha256-ext-wrap-presvEnc", false, DisplayName = "sha256-ext-wrap-presvEnc")]
        [DataTestMethod]
        public void Test2Xml(string hashAlg, bool extContent, bool wrapExtInXml, bool preserveEnc, string testOutFolder, bool includeSub)
        {
            if (logger != null)
            {
                var settings = new EmailProcessorSettings()
                {
                    HashAlgorithmName = hashAlg,
                    SaveAttachmentsAndBinaryContentExternally = extContent,
                    WrapExternalContentInXml = wrapExtInXml,
                    PreserveContentTransferEncodingIfPossible = preserveEnc,
                    IncludeSubFolders=includeSub
                };

                //TODO: make the tests resilient to moving code to different folder structure
                //Also save the sample test files in the test project and automate the folder setup and cleanup
                var sampleFile = Path.Combine(testFilesBaseDirectory,"DLF Distributed Library");
                var expectedOutFolder = Path.Combine(testFilesBaseDirectory, testOutFolder);
                var outFolder = Path.Combine(testFilesBaseDirectory, testOutFolder);

                //clean out the output folder
                if (Directory.Exists(outFolder))
                {
                    Directory.Delete(outFolder, true);
                }

                var proc = new EmailProcessor(logger, settings);
                var cnt = proc.ConvertMbox2EAXS(sampleFile, ref outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                Assert.IsTrue(cnt > 0);

                //make sure output folders and files exist
                Assert.AreEqual(expectedOutFolder, outFolder);
                Assert.IsTrue(Directory.Exists(outFolder));
                string xmlPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "xml"));
                string csvPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "csv"));
                Assert.IsTrue(File.Exists(xmlPathStr));
                Assert.IsTrue(File.Exists(csvPathStr));

                var xdoc = new XmlDocument();
                xdoc.Load(xmlPathStr);

                var xmlns = new XmlNamespaceManager(xdoc.NameTable);
                xmlns.AddNamespace(EmailProcessor.XM, EmailProcessor.XM_NS);

                //make sure hash is correct
                XmlNode? hashValueNd = xdoc.SelectSingleNode("/xm:Account/xm:Folder/xm:Mbox/xm:Hash/xm:Value", xmlns);
                XmlNode? hashFuncNd = xdoc.SelectSingleNode("/xm:Account/xm:Folder/xm:Mbox/xm:Hash/xm:Function", xmlns);

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
                            Assert.AreEqual(id,prevId+1);
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

                //make sure hashes match
                Assert.AreEqual(settings.HashAlgorithmName, hashFuncNd?.InnerText);
                Assert.AreEqual(hashAlg, hashFuncNd?.InnerText);

                var expectedHash = CalculateHash(hashAlg, sampleFile);
                Assert.AreEqual(expectedHash, hashValueNd?.InnerText);

                //make sure xml is schema valid 
                validXml = true;
                xdoc.Schemas.Add(EmailProcessor.XM_NS, EmailProcessor.XM_XSD);
                Assert.IsTrue(xdoc.DocumentElement?.LocalName == "Account");
                xdoc.Validate(XmlValidationEventHandler, xdoc.DocumentElement);
                Assert.IsTrue(validXml);

                if (extContent)
                {
                    //make sure all attachments or binary content are external and that the file exists and hashes match
                    XmlNodeList? nodes = xdoc.SelectNodes("/xm:Account/xm:Folder/xm:Message//xm:SingleBody[xm:Disposition='attachment' or (not(starts-with(translate(xm:ContentType,'TEXT','tex'),'tex')) and not(starts-with(translate(xm:ContentType,'MESAG','mesag'),'message')))]", xmlns);
                    Assert.IsTrue(nodes != null && nodes.Count > 0);

                    var extdoc = new XmlDocument();
                    extdoc.Schemas.Add(EmailProcessor.XM_NS, EmailProcessor.XM_XSD);

                    foreach (XmlElement node in nodes)
                    {
                        Assert.IsNull(node.SelectSingleNode("xm:BodyContent", xmlns));
                        XmlElement? extNode = node.SelectSingleNode("xm:ExtBodyContent", xmlns) as XmlElement;
                        Assert.IsNotNull(extNode);
                        string? extPath = extNode.SelectSingleNode("xm:RelPath", xmlns)?.InnerText;
                        Assert.IsFalse(string.IsNullOrWhiteSpace(extPath));
                        var extFilepath = Path.Combine(outFolder, EmailProcessor.EXT_CONTENT_DIR, extPath);
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
                            Assert.IsTrue(extdoc.DocumentElement?.NamespaceURI == EmailProcessor.XM_NS);
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
                                Assert.IsFalse(extdoc.DocumentElement?.NamespaceURI == EmailProcessor.XM_NS);
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
                            if (enc != null)
                                Assert.AreEqual("base64", enc);
                        }

                    }
                }

                //make sure there is nothing but info messages in the log output
                Assert.AreEqual(0, StringListLogger.Instance.LoggedLines.Where(s => !s.StartsWith("[Information]")).Count());

            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }
        }

        [TestMethod]
        public void TestMozillaDrafts2Xml()
        {
            if (logger != null)
            {
                var sampleFile = Path.Combine(testFilesBaseDirectory,"Drafts");
                var outFolder = "";
                var eapdf = new EmailProcessor(logger, new EmailProcessorSettings() { IncludeSubFolders=false});
                var cnt = eapdf.ConvertMbox2EAXS(sampleFile, ref outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");

                //output folder is the same as the mbox folder
                var samplePathStr = Path.GetFullPath(Path.GetDirectoryName(sampleFile) ?? sampleFile);
                var outPathStr = Path.GetFullPath(outFolder);
                Assert.AreEqual(samplePathStr, outPathStr);
                var xmlPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "xml"));
                var csvPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "csv"));
                Assert.IsTrue(File.Exists(xmlPathStr));
                Assert.IsTrue(File.Exists(csvPathStr));

                Assert.IsTrue(cnt > 0);

                var xdoc = new XmlDocument();
                xdoc.Load(xmlPathStr);

                var xmlns = new XmlNamespaceManager(xdoc.NameTable);
                xmlns.AddNamespace(EmailProcessor.XM, EmailProcessor.XM_NS);

                var messages = xdoc.SelectNodes("/xm:Account/xm:Folder/xm:Message", xmlns);
                //make sure each message is marked as draft
                if (messages != null)
                {
                    foreach (XmlElement message in messages)
                    {
                        var draft = message.SelectSingleNode("xm:StatusFlag[normalize-space(text()) = 'Draft']", xmlns);
                        Assert.IsNotNull(draft);
                    }
                }

                //make sure xml is schema valid
                validXml = true;
                xdoc.Schemas.Add(EmailProcessor.XM_NS, EmailProcessor.XM_XSD);
                Assert.IsTrue(xdoc.DocumentElement?.LocalName == "Account");
                xdoc.Validate(XmlValidationEventHandler, xdoc.DocumentElement);
                Assert.IsTrue(validXml);

                //make sure there is nothing but info messages in the log output
                Assert.AreEqual(0, StringListLogger.Instance.LoggedLines.Where(s => !s.StartsWith("[Information]")).Count());

            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }
        }
        
        [TestMethod]
        public void TestInboxXml()
        {
            if (logger != null)
            {
                var sampleFile = Path.Combine(testFilesBaseDirectory,"Inbox");
                var outFolder = "";
                var eapdf = new EmailProcessor(logger, new EmailProcessorSettings() { IncludeSubFolders=false});
                var cnt = eapdf.ConvertMbox2EAXS(sampleFile, ref outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");

                //output folder is the same as the mbox folder
                var samplePathStr = Path.GetFullPath(Path.GetDirectoryName(sampleFile) ?? sampleFile);
                var outPathStr = Path.GetFullPath(outFolder);
                Assert.AreEqual(samplePathStr, outPathStr);
                var xmlPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "xml"));
                var csvPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "csv"));
                Assert.IsTrue(File.Exists(xmlPathStr));
                Assert.IsTrue(File.Exists(csvPathStr));

                Assert.IsTrue(cnt > 0);

                var xdoc = new XmlDocument();
                xdoc.Load(xmlPathStr);

                var xmlns = new XmlNamespaceManager(xdoc.NameTable);
                xmlns.AddNamespace(EmailProcessor.XM, EmailProcessor.XM_NS);

                //make sure xml is schema valid
                validXml = true;
                xdoc.Schemas.Add(EmailProcessor.XM_NS, EmailProcessor.XM_XSD);
                Assert.IsTrue(xdoc.DocumentElement?.LocalName == "Account");
                xdoc.Validate(XmlValidationEventHandler, xdoc.DocumentElement);
                Assert.IsTrue(validXml);

                //make sure there is nothing but info messages in the log output
                Assert.AreEqual(0, StringListLogger.Instance.LoggedLines.Where(s => !s.StartsWith("[Information]")).Count());
            }
            else
            {
                Assert.Fail("Logger was not initialized");
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
    }
}