using Microsoft.VisualStudio.TestTools.UnitTesting;
using Email2Pdf;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Security.Cryptography;
using System;

namespace TestEAPDF
{
    [TestClass]
    public class TestEmailFunctions
    {
        ILogger<MboxProcessor>? logger;
        ILoggerFactory? loggerFactory;
        bool validXml = true;

        [TestInitialize]
        public void InitTest()
        {
            loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            logger = loggerFactory.CreateLogger<MboxProcessor>();
            logger.LogInformation("Starting Test");
        }

        [TestCleanup]
        public void EndTest()
        {
            if (logger != null) logger.LogInformation("Ending Test");
            if (loggerFactory != null) loggerFactory.Dispose();
        }



        //TODO: Test different combinations of settings; maybe experiment with using parameterized tests
        //TODO: Test saveBinaryExt parameter
        //TODO: Test the SerializeContentInXml preserveEncodingIfPossible parameter

        [DataRow("SHA256", true, false, false,"test1")]
        [DataRow("SHA1", true, false, false,"test2")]
        [DataTestMethod]
        public void Test2Xml(string hashAlg, bool extContent, bool wrapExtInXml, bool preserveEnc, string testOutFolder)
        {
            if (logger != null)
            {
                var settings = new MBoxProcessorSettings(){
                    HashAlgorithmName = hashAlg,
                    SaveAttachmentsAndBinaryContentExternally = extContent,
                    WrapExternalContentInXml = wrapExtInXml,
                    PreserveContentTransferEncodingIfPossible = preserveEnc
                };

                var sampleFile = @"..\..\..\..\SampleFiles\DLF Distributed Library";
                var expectedOutFolder = Path.Combine(@"C:\Users\thabi\Source\UIUC\Email2Pdf\SampleFiles", testOutFolder);
                var outFolder = Path.Combine(@"C:\Users\thabi\Source\UIUC\Email2Pdf\SampleFiles",testOutFolder);
                
                //clean out the output folder
                if (Directory.Exists(outFolder))
                {
                    Directory.Delete(outFolder, true);
                }

                var proc = new MboxProcessor(logger, sampleFile, settings);
                var cnt = proc.ConvertMbox2EAXS(ref outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                Assert.IsTrue(cnt > 0);

                //make sure output folders and files exist
                Assert.AreEqual(expectedOutFolder, outFolder);
                Assert.IsTrue(Directory.Exists(outFolder));
                var xmlPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "xml"));
                var csvPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "csv"));
                Assert.IsTrue(File.Exists(xmlPathStr));
                Assert.IsTrue(File.Exists(csvPathStr));

                var xdoc = new XmlDocument();
                xdoc.Load(xmlPathStr);

                var xmlns = new XmlNamespaceManager(xdoc.NameTable);
                xmlns.AddNamespace(MboxProcessor.XM, MboxProcessor.XM_NS);

                //make sure hash is correct
                XmlNode? hashValueNd = xdoc.SelectSingleNode("/xm:Account/xm:Folder/xm:Mbox/xm:Hash/xm:Value", xmlns);
                XmlNode? hashFuncNd = xdoc.SelectSingleNode("/xm:Account/xm:Folder/xm:Mbox/xm:Hash/xm:Function", xmlns);

                //make sure hashes match
                Assert.AreEqual(settings.HashAlgorithmName, hashFuncNd?.InnerText);
                Assert.AreEqual(hashAlg, hashFuncNd?.InnerText);

                var expectedHash = CalculateHash(hashAlg,sampleFile);
                Assert.AreEqual(expectedHash, hashValueNd?.InnerText);

                //make sure xml is schema valid 
                validXml = true;
                xdoc.Schemas.Add(MboxProcessor.XM_NS, MboxProcessor.XM_XSD);
                xdoc.Validate(XmlValidationEventHandler);
                Assert.IsTrue(validXml);
                
                if (extContent)
                {
                    //make sure all attachments or binary content are external and that the file exists and hashes match
                    XmlNodeList? nodes = xdoc.SelectNodes("/xm:Account/xm:Folder/xm:Message//xm:SingleBody[xm:Disposition='attachment' or (not(starts-with(translate(xm:ContentType,'TEXT','tex'),'tex')) and not(starts-with(translate(xm:ContentType,'MESAG','mesag'),'message')))]", xmlns);
                    Assert.IsTrue(nodes != null && nodes.Count > 0);
                    foreach (XmlElement node in nodes)
                    {
                        Assert.IsNull(node.SelectSingleNode("xm:BodyContent", xmlns));
                        XmlElement? extNode = node.SelectSingleNode("xm:ExtBodyContent", xmlns) as XmlElement;
                        Assert.IsNotNull(extNode);
                        string? extPath = extNode.SelectSingleNode("xm:RelPath", xmlns)?.InnerText ;
                        Assert.IsFalse(string.IsNullOrWhiteSpace(extPath));
                        var extFilepath = Path.Combine(outFolder, MboxProcessor.EXT_CONTENT_DIR, extPath);
                        Assert.IsTrue(File.Exists(extFilepath));
                        var extHash = CalculateHash(hashAlg, extFilepath);
                        string? calcHash = extNode.SelectSingleNode("xm:Hash/xm:Value", xmlns)?.InnerText;
                        Assert.AreEqual(extHash, calcHash);
                        string? calcHashAlg = extNode.SelectSingleNode("xm:Hash/xm:Function", xmlns)?.InnerText;
                        Assert.AreEqual(hashAlg, calcHashAlg);
                    }
                }
                else
                {
                    
                }

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
                var sampleFile = @"..\..\..\..\SampleFiles\Drafts";
                var outFolder = "";
                var eapdf = new MboxProcessor(logger, sampleFile);
                var cnt = eapdf.ConvertMbox2EAXS(ref outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");

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
                xmlns.AddNamespace(MboxProcessor.XM, MboxProcessor.XM_NS);

                var messages = xdoc.SelectNodes("/xm:Account/xm:Folder/xm:Message", xmlns);
                //make sure each message is marked as draft
                if(messages!= null)
                {
                    foreach (XmlElement message in messages)
                    {
                        var draft = message.SelectSingleNode("xm:StatusFlag[normalize-space(text()) = 'Draft']", xmlns);
                        Assert.IsNotNull(draft);
                    }
                }

                //make sure xml is schema valid
                validXml = true;
                xdoc.Schemas.Add(MboxProcessor.XM_NS, MboxProcessor.XM_XSD);
                xdoc.Validate(XmlValidationEventHandler);
                Assert.IsTrue(validXml);

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
                var sampleFile = @"..\..\..\..\SampleFiles\Inbox";
                var outFolder = "";
                var eapdf = new MboxProcessor(logger, sampleFile);
                var cnt = eapdf.ConvertMbox2EAXS(ref outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");

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
                xmlns.AddNamespace(MboxProcessor.XM, MboxProcessor.XM_NS);

                //make sure xml is schema valid
                validXml = true;
                xdoc.Schemas.Add(MboxProcessor.XM_NS, MboxProcessor.XM_XSD);
                xdoc.Validate(XmlValidationEventHandler);
                Assert.IsTrue(validXml);

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

        string CalculateHash(string algName,string filePath)
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