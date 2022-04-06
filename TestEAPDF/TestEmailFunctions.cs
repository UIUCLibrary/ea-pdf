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


        [TestMethod]
        public void Test2Pdf()
        {
            if (logger != null)
            {
                var sampleFile = @"..\..\..\..\SampleFiles\DLF Distributed Library";
                var pdfFile = "";
                var eapdf = new MboxProcessor(logger, sampleFile);
                var cnt = eapdf.ConvertMbox2EAPDF(ref pdfFile);

                Assert.AreEqual(".pdf", Path.GetExtension(pdfFile));
                Assert.AreEqual(Path.GetFileNameWithoutExtension(sampleFile), Path.GetFileNameWithoutExtension(pdfFile));
                Assert.IsTrue(cnt > 0);
            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }
        }

        [TestMethod]
        public void Test2Xml()
        {
            if (logger != null)
            {
                var sampleFile = @"..\..\..\..\SampleFiles\DLF Distributed Library";
                var xmlFile = "";
                var eapdf = new MboxProcessor(logger, sampleFile);
                var cnt = eapdf.ConvertMbox2EAXS(ref xmlFile, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");

                Assert.AreEqual(".xml", Path.GetExtension(xmlFile));
                Assert.AreEqual(Path.GetFileNameWithoutExtension(sampleFile), Path.GetFileNameWithoutExtension(xmlFile));
                Assert.IsTrue(cnt > 0);

                var xdoc = new XmlDocument();
                xdoc.Load(xmlFile);

                var xmlns = new XmlNamespaceManager(xdoc.NameTable);
                xmlns.AddNamespace(MboxProcessor.XM, MboxProcessor.XM_NS);

                //make sure hash is correct
                XmlNode? hashValueNd = xdoc.SelectSingleNode("/xm:Account/xm:Folder/xm:Mbox/xm:Hash/xm:Value", xmlns);
                XmlNode? hashFuncNd = xdoc.SelectSingleNode("/xm:Account/xm:Folder/xm:Mbox/xm:Hash/xm:Function", xmlns);

                Assert.AreEqual("SHA1", hashFuncNd?.InnerText);

                var expectedHash = CalculateSHA1(sampleFile);
                Assert.AreEqual(expectedHash, hashValueNd?.InnerText);

                //make sure xml is schema valid
                validXml = true;
                xdoc.Schemas.Add(MboxProcessor.XM_NS, "eaxs_schema_v1.xsd");
                xdoc.Validate(XmlValidationEventHandler);
                Assert.IsTrue(validXml);

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
                var xmlFile = "";
                var eapdf = new MboxProcessor(logger, sampleFile);
                var cnt = eapdf.ConvertMbox2EAXS(ref xmlFile, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");

                Assert.AreEqual(".xml", Path.GetExtension(xmlFile));
                Assert.AreEqual(Path.GetFileNameWithoutExtension(sampleFile), Path.GetFileNameWithoutExtension(xmlFile));
                Assert.IsTrue(cnt > 0);

                var xdoc = new XmlDocument();
                xdoc.Load(xmlFile);

                var xmlns = new XmlNamespaceManager(xdoc.NameTable);
                xmlns.AddNamespace(MboxProcessor.XM, MboxProcessor.XM_NS);

                var messages = xdoc.SelectNodes("/xm:Account/xm:Folder/xm:Message", xmlns);
                //make sure each message is marked as draft
                foreach (XmlElement message in messages)
                {
                    var draft = message.SelectSingleNode("xm:StatusFlag[normalize-space(text()) = 'Draft']", xmlns);
                    Assert.IsNotNull(draft);
                }

                //make sure xml is schema valid
                validXml = true;
                xdoc.Schemas.Add(MboxProcessor.XM_NS, "eaxs_schema_v1.xsd");
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
                var xmlFile = "";
                var eapdf = new MboxProcessor(logger, sampleFile);
                var cnt = eapdf.ConvertMbox2EAXS(ref xmlFile, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");

                Assert.AreEqual(".xml", Path.GetExtension(xmlFile));
                Assert.AreEqual(Path.GetFileNameWithoutExtension(sampleFile), Path.GetFileNameWithoutExtension(xmlFile));
                Assert.IsTrue(cnt > 0);

                var xdoc = new XmlDocument();
                xdoc.Load(xmlFile);

                var xmlns = new XmlNamespaceManager(xdoc.NameTable);
                xmlns.AddNamespace(MboxProcessor.XM, MboxProcessor.XM_NS);

                //make sure xml is schema valid
                validXml = true;
                xdoc.Schemas.Add(MboxProcessor.XM_NS, "eaxs_schema_v1.xsd");
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

        string CalculateSHA1(string filePath)
        {
            var fstream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var sha1 = SHA1.Create();
            var cstream = new CryptoStream(fstream, sha1, CryptoStreamMode.Read);

            //read to end of stream
            int i = -1;
            do 
            { 
                i = cstream.ReadByte(); 
            } while (i != -1);

            var hash = sha1.Hash;

            return Convert.ToHexString(hash);
        }
    }
}