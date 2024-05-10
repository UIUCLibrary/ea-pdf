using Extensions.Logging.ListOfString;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using UIUCLibrary.EaPdf;
using UIUCLibrary.EaPdf.Helpers;
using UIUCLibrary.EaPdf.Helpers.Pdf;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestPdfFunctions
    {
        private readonly bool OPEN_PDFS = false;  //set to true to open the PDFs in the default PDF viewer
        private readonly bool VALIDATE_PDFS = true;  //set to true to validate the PDFs using the PDF/A validator

        ILogger<EaxsToEaPdfProcessor>? logger;
        ILoggerFactory? loggerFactory;
        readonly List<string> loggedLines = new();
        readonly string testFilesBaseDirectory = @"D:\EmailsForTesting\SampleFiles\Testing";

        [TestInitialize]
        public void InitTest()
        {
            //See https://codeburst.io/unit-testing-with-net-core-ilogger-t-e8c16c503a80#767c for how to use ILogger with unit testing
            //See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/ for more info on ILogger in .NET Core

            //log to the testing console standard error; log all message levels: trace, debug, ..., info
            loggerFactory = LoggerFactory.Create(builder => builder.AddConsole(opts => opts.LogToStandardErrorThreshold = LogLevel.Trace).SetMinimumLevel(LogLevel.Trace));

            //using StringListLogger for testing purposes https://www.nuget.org/packages/Extensions.Logging.ListOfString https://github.com/chrisfcarroll/TestBase
            loggerFactory.AddStringListLogger(loggedLines);

            logger = loggerFactory.CreateLogger<EaxsToEaPdfProcessor>();
            logger.LogDebug("Starting Test");  //all logging done by the test scripts are debug level
        }

        [TestCleanup]
        public void EndTest()
        {
            if (logger != null)
            {
                logger.LogDebug("Information: {informationCount}", StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Information]")).Count());
                logger.LogDebug("Warnings: {warningCount}", StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Warning]")).Count());
                logger.LogDebug("Errors: {errorCount}", StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Error]")).Count());
                logger.LogDebug("Ending Test");
            }
            loggerFactory?.Dispose();
        }


        [DataRow("Non-Western\\Farsi", "fop", true, false, DisplayName = "FARSI-FOP-EXT-NO-WRAP")] //Use Farsi folder and FOP and do not wrap external files in XML
        [DataRow("Non-Western\\Farsi", "fop", true, true, DisplayName = "FARSI-FOP-EXT-WRAP")] //Use Farsi folder and FOP and wrap external files in XML
        [DataRow("Non-Western\\Farsi", "xep", true, true, DisplayName = "FARSI-XEP-EXT-WRAP")] //Use Farsi folder and XEP and wrap external files in XML

        [DataRow("Non-Western\\Hebrew", "fop", true, false, DisplayName = "HEBREW-FOP-EXT-NO-WRAP")] //Use Hebrew folder and FOP and do not wrap external files in XML
        [DataRow("Non-Western\\Hebrew", "fop", true, true, DisplayName = "HEBREW-FOP-EXT-WRAP")] //Use Hebrew folder and FOP and wrap external files in XML
        [DataRow("Non-Western\\Hebrew", "xep", true, true, DisplayName = "HEBREW-XEP-EXT-WRAP")] //Use Hebrew folder and XEP and wrap external files in XML

        [DataRow("Non-Western\\Arabic", "fop", true, false, DisplayName = "ARABIC-FOP-EXT-NO-WRAP")] //Use Arabic folder and FOP and do not wrap external files in XML
        [DataRow("Non-Western\\Arabic", "fop", true, true, DisplayName = "ARABIC-FOP-EXT-WRAP")] //Use Arabic folder and FOP and wrap external files in XML
        [DataRow("Non-Western\\Arabic", "xep", true, true, DisplayName = "ARABIC-XEP-EXT-WRAP")] //Use Arabic folder and XEP and wrap external files in XML

        [DataRow("Non-Western\\Chinese", "fop", true, false, DisplayName = "CHINESE-FOP-EXT-NO-WRAP")] //Use Chinese folder and FOP and do not wrap external files in XML
        [DataRow("Non-Western\\Chinese", "fop", true, true, DisplayName = "CHINESE-FOP-EXT-WRAP")] //Use Chinese folder and FOP and wrap external files in XML
        [DataRow("Non-Western\\Chinese", "xep", true, true, DisplayName = "CHINESE-XEP-EXT-WRAP")] //Use Chinese folder and XEP and wrap external files in XML
        [DataRow("Non-Western\\Chinese", "fop", false, false, DisplayName = "CHINESE-FOP")] //Use Chinese folder and FOP and embed files in XML
        [DataRow("Non-Western\\Chinese", "xep", false, false, DisplayName = "CHINESE-XEP")] //Use Chinese folder and XEP and embed files in XML

        [DataTestMethod]
        public void TestEaxsToPdfNonWesternLangs(string inPath, string foProcessor, bool ext, bool wrap)
        {
            TestEaxsToPdfProcessor(inPath, foProcessor, ext, wrap);
        }

        [DataRow("InlineImages\\EML", "fop", true, false, DisplayName = "INLINE-IMAGES-FOP-EXT")] //Sample EML with inline images of different sizes
        [DataRow("InlineImages\\EML", "xep", true, true, DisplayName = "INLINE-IMAGES-XEP-EXT-WRAP")] //Sample EML with inline images of different sizes
        [DataRow("InlineImages\\EML", "fop", false, false, DisplayName = "INLINE-IMAGES-FOP")] //Sample EML with inline images of different sizes
        [DataRow("InlineImages\\EML", "xep", false, false, DisplayName = "INLINE-IMAGES-XEP")] //Sample EML with inline images of different sizes

        [DataTestMethod]
        public void TestEaxsToPdfInlineImages(string inPath, string foProcessor, bool ext, bool wrap)
        {
            TestEaxsToPdfProcessor(inPath, foProcessor, ext, wrap);
        }

        [DataRow("MozillaThunderbird\\DLF Distributed Library", "fop", true, false, DisplayName = "MOZILLA-FOP-EXT-NO-WRAP")] //Use Mozilla file and FOP and do not wrap external files in XML
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "fop", true, true, DisplayName = "MOZILLA-FOP-EXT-WRAP")] //Use Mozilla file and FOP and wrap external files in XML
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "xep", true, true, DisplayName = "MOZILLA-XEP-EXT-WRAP")] //Use Mozilla file and XEP and wrap external files in XML
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "fop", false, false, DisplayName = "MOZILLA-FOP")] //Use Mozilla file and FOP with all attachments embedded in XML
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "xep", false, false, DisplayName = "MOZILLA-XEP")] //Use Mozilla file and XEP with all attachments embedded in XML

        [DataTestMethod]
        public void TestEaxsToPdfProcessorLargeFiles(string inPath, string foProcessor, bool ext, bool wrap)
        {
            TestEaxsToPdfProcessor(inPath, foProcessor, ext, wrap);
        }


        [DataRow("MozillaThunderbird\\short-test\\short-test.mbox", "fop", true, false, DisplayName = "ENGLISH-FOP-EXT-NO-WRAP")] //Use short-test file and FOP and do not wrap external files in XML
        [DataRow("MozillaThunderbird\\short-test\\short-test.mbox", "fop", true, true, DisplayName = "ENGLISH-FOP-EXT-WRAP")] //Use short-test file and FOP and wrap external files in XML
        [DataRow("MozillaThunderbird\\short-test\\short-test.mbox", "xep", true, true, DisplayName = "ENGLISH-XEP-EXT-WRAP")] //Use short-test file and XEP and wrap external files in XML
        [DataRow("MozillaThunderbird\\short-test\\short-test.mbox", "fop", false, false, DisplayName = "ENGLISH-FOP")] //Use short-test file and FOP and do not wrap external files in XML
        [DataRow("MozillaThunderbird\\short-test\\short-test.mbox", "xep", false, false, DisplayName = "ENGLISH-XEP")] //Use short-test file and XEP and do not wrap external files in XML

        [DataRow("MozillaThunderbird\\short-test-mult\\short-test.mbox", "fop", true, false, DisplayName = "ENGLISH-NESTED-FOP-EXT-NO-WRAP")] //Use short-test file and FOP and do not wrap external files in XML
        [DataRow("MozillaThunderbird\\short-test-mult\\short-test.mbox", "fop", true, true, DisplayName = "ENGLISH-NESTED-FOP-EXT-WRAP")] //Use short-test file and FOP and wrap external files in XML
        [DataRow("MozillaThunderbird\\short-test-mult\\short-test.mbox", "xep", true, true, DisplayName = "ENGLISH-NESTED-XEP-EXT-WRAP")] //Use short-test file and XEP and wrap external files in XML
        [DataRow("MozillaThunderbird\\short-test-mult\\short-test.mbox", "fop", false, false, DisplayName = "ENGLISH-NESTED-FOP")] //Use short-test file and FOP and do not wrap external files in XML
        [DataRow("MozillaThunderbird\\short-test-mult\\short-test.mbox", "xep", false, false, DisplayName = "ENGLISH-NESTED-XEP")] //Use short-test file and XEP and wrap external files in XML

        [DataTestMethod]
        public void TestEaxsToPdfProcessor(string inPath, string foProcessor, bool ext, bool wrap)
        {
            if (!foProcessor.Equals("fop", System.StringComparison.OrdinalIgnoreCase) && !foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"The {nameof(foProcessor)} param must be either 'fop' or 'xep', ignoring case",nameof(foProcessor));

            if (foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase) && ext && !wrap)
                throw new ArgumentException("XEP requires external files to be wrapped in XML");

            if (!ext && wrap)
                throw new ArgumentException("Attachments must be saved externally to be wrapped in XML");

            if (logger != null)
            {
                var xmlFile = ConvertToEaxs(inPath, ext, wrap, null);

                string pdfFile, configFile;
                IXslFoTransformer foTransformer;
                if (foProcessor.Equals("fop", System.StringComparison.OrdinalIgnoreCase))
                {
                    pdfFile = Path.ChangeExtension(xmlFile, $"fop{(ext ? "_x" : "")}{(wrap ? "_w" : "")}.pdf");
                    configFile = Path.GetFullPath("XResources\\fop.xconf");
                    foTransformer = new FopToPdfTransformer(configFile);
                }
                else if (foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase))
                {
                    pdfFile = Path.ChangeExtension(xmlFile, $"xep{(ext ? "_x" : "")}{(wrap ? "_w" : "")}.pdf");
                    configFile = Path.GetFullPath("XResources\\xep.xml");
                    foTransformer = new XepToPdfTransformer(configFile);
                }
                else
                {
                    throw new ArgumentException($"The {nameof(foProcessor)} param must be either 'fop' or 'xep', ignoring case", nameof(foProcessor));
                }

                var xslt = new SaxonXsltTransformer();
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, foTransformer, iText, set);

                var files = proc.ConvertEaxsToPdf(xmlFile, pdfFile);

                Assert.AreEqual(files.ElementAt(0).Key, xmlFile);
                Assert.AreEqual(files.ElementAt(0).Value, pdfFile);

                foreach(var file in files)
                {
                    Assert.IsTrue(File.Exists(file.Value));

                    Assert.IsTrue(IsPdfValid(file.Value));

                    if (VALIDATE_PDFS) 
                    {
                        Helpers.ValidatePdfAUsingPdfTools(file.Value);

                        Helpers.ValidatePdfAUsingVeraPdf(file.Value);
                    }

                    if (OPEN_PDFS)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.Value) { UseShellExecute = true });
                }
            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }

        }

        [DataRow("InlineImages\\EML", "fop", true, false, DisplayName = "INLINE-IMAGES-FOP-EXT")] //Sample EML with inline images of different sizes
        [DataRow("InlineImages\\EML", "xep", true, true, DisplayName = "INLINE-IMAGES-XEP-EXT-WRAP")] //Sample EML with inline images of different sizes
        [DataRow("InlineImages\\EML", "fop", false, false, DisplayName = "INLINE-IMAGES-FOP")] //Sample EML with inline images of different sizes
        [DataRow("InlineImages\\EML", "xep", false, false, DisplayName = "INLINE-IMAGES-XEP")] //Sample EML with inline images of different sizes

        [DataTestMethod]
        public void TestEaxsToPdfInlineImagesRemoveDimensions(string inPath, string foProcessor, bool ext, bool wrap)
        {
            if (!foProcessor.Equals("fop", System.StringComparison.OrdinalIgnoreCase) && !foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"The {nameof(foProcessor)} param must be either 'fop' or 'xep', ignoring case", nameof(foProcessor));

            if (foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase) && ext && !wrap)
                throw new ArgumentException("XEP requires external files to be wrapped in XML");

            if (!ext && wrap)
                throw new ArgumentException("Attachments must be saved externally to be wrapped in XML");

            if (logger != null)
            {
                var xmlFile = ConvertToEaxs(inPath, ext, wrap, null);

                string pdfFile, configFile;
                IXslFoTransformer foTransformer;
                if (foProcessor.Equals("fop", System.StringComparison.OrdinalIgnoreCase))
                {
                    pdfFile = Path.ChangeExtension(xmlFile, $"fop{(ext ? "_x" : "")}{(wrap ? "_w" : "")}_ni.pdf");
                    configFile = Path.GetFullPath("XResources\\fop.xconf");
                    foTransformer = new FopToPdfTransformer(configFile);
                }
                else if (foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase))
                {
                    pdfFile = Path.ChangeExtension(xmlFile, $"xep{(ext ? "_x" : "")}{(wrap ? "_w" : "")}_ni.pdf");
                    configFile = Path.GetFullPath("XResources\\xep.xml");
                    foTransformer = new XepToPdfTransformer(configFile);
                }
                else
                {
                    throw new ArgumentException($"The {nameof(foProcessor)} param must be either 'fop' or 'xep', ignoring case", nameof(foProcessor));
                }

                //For testing purposes, remove image dimensions from the XML file so that the PDF processor will have to calculate the dimensions
                var xdoc = new XmlDocument();
                xdoc.Load(xmlFile);
                XmlNamespaceManager xmlns = new(xdoc.NameTable);
                xmlns.AddNamespace("eaxs", "https://github.com/StateArchivesOfNorthCarolina/tomes-eaxs-2");
                var nodesToRemove = xdoc.SelectNodes("//eaxs:ImageProperties", xmlns);
                if(nodesToRemove != null)
                {
                    foreach (XmlNode node in nodesToRemove)
                    {
                        _ = node.ParentNode?.RemoveChild(node);
                    }
                }
                xdoc.Save(xmlFile);

                var xslt = new SaxonXsltTransformer();
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, foTransformer, iText, set);

                var files = proc.ConvertEaxsToPdf(xmlFile, pdfFile);

                Assert.AreEqual(files.ElementAt(0).Key, xmlFile);
                Assert.AreEqual(files.ElementAt(0).Value, pdfFile);

                foreach (var file in files)
                {
                    Assert.IsTrue(File.Exists(file.Value));

                    Assert.IsTrue(IsPdfValid(file.Value));

                    if (VALIDATE_PDFS) Helpers.ValidatePdfAUsingVeraPdf(file.Value);

                    if (OPEN_PDFS)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.Value) { UseShellExecute = true });
                }
            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }

        }

        [DataRow("MozillaThunderbird\\DLF Distributed Library", "fop", false, false, 10000000, DisplayName = "MOZILLA-FOP-10000000")] //Use Mozilla file and FOP and wrap external files in XML
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "xep", false, false, 10000000, DisplayName = "MOZILLA-XEP-10000000")] //Use Mozilla file and XEP and wrap external files in XML

        [DataTestMethod]
        public void TestEaxsToPdfProcessorWithContinuations(string inPath, string foProcessor, bool ext, bool wrap, long maxOutFileSize)
        {
            if (!foProcessor.Equals("fop", System.StringComparison.OrdinalIgnoreCase) && !foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"The {nameof(foProcessor)} param must be either 'fop' or 'xep', ignoring case", nameof(foProcessor));

            if (foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase) && ext && !wrap)
                throw new ArgumentException("XEP requires external files to be wrapped in XML");

            if (!ext && wrap)
                throw new ArgumentException("Attachments must be saved externally to be wrapped in XML");

            if (logger != null)
            {
                var xmlFile = ConvertToEaxs(inPath, ext, wrap, null, maxOutFileSize);

                string pdfFile, configFile;
                IXslFoTransformer foTransformer;
                if (foProcessor.Equals("fop", System.StringComparison.OrdinalIgnoreCase))
                {
                    pdfFile = Path.ChangeExtension(xmlFile, $"fop{(ext ? "_x" : "")}{(wrap ? "_w" : "")}_C.pdf");
                    configFile = Path.GetFullPath("XResources\\fop.xconf");
                    foTransformer = new FopToPdfTransformer(configFile);
                }
                else if (foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase))
                {
                    pdfFile = Path.ChangeExtension(xmlFile, $"xep{(ext ? "_x" : "")}{(wrap ? "_w" : "")}_C.pdf");
                    configFile = Path.GetFullPath("XResources\\xep.xml");
                    foTransformer = new XepToPdfTransformer(configFile);
                }
                else
                {
                    throw new ArgumentException($"The {nameof(foProcessor)} param must be either 'fop' or 'xep', ignoring case", nameof(foProcessor));
                }

                var xslt = new SaxonXsltTransformer();
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, foTransformer, iText, set);

                var files = proc.ConvertEaxsToPdf(xmlFile, pdfFile);

                Assert.AreEqual(files.ElementAt(0).Key, xmlFile);
                Assert.AreEqual(files.ElementAt(0).Value, pdfFile);

                foreach (var file in files)
                {
                    logger.LogDebug("Files: {xmlfile} --> {pdfFile}", file.Key, file.Value);

                    Assert.IsTrue(File.Exists(file.Value));

                    Assert.IsTrue(IsPdfValid(file.Value));

                    if (VALIDATE_PDFS) Helpers.ValidatePdfAUsingVeraPdf(file.Value);

                    if (OPEN_PDFS)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.Value) { UseShellExecute = true });
                }
            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }

        }


        private string ConvertToEaxs(string filePath, bool saveAttachmentsExt, bool wrapExtContentInXml, string? skipAfterMsgId, long maxFileSize = 0)
        {
            var inFile = Path.Combine(testFilesBaseDirectory, filePath);

            FileAttributes attr = File.GetAttributes(inFile);

            if (attr.HasFlag(FileAttributes.Directory))
                //directory of EML files
                return ConvertEmlFolderToEaxs(filePath, saveAttachmentsExt, wrapExtContentInXml, maxFileSize);
            else
                //single MBOX file
                return ConvertMBoxToEaxs(filePath, saveAttachmentsExt, wrapExtContentInXml, skipAfterMsgId, maxFileSize);
        }

        private string ConvertMBoxToEaxs(string filePath, bool saveAttachmentsExt, bool wrapExtContentInXml, string? skipAfterMsgId, long maxFileSize = 0)
        {
            if (logger != null)
            {

                var inFile = Path.Combine(testFilesBaseDirectory, filePath);
                var outFolder = Path.GetDirectoryName(inFile);

                if(outFolder == null)
                    Assert.Fail("Could not get directory name from " + inFile);

                var xmlFile = FilePathHelpers.GetXmlOutputFilePath(outFolder, inFile);

                if (outFolder == null)
                    Assert.Fail("Could not get directory name from " + inFile);

                var settings = new EmailToEaxsProcessorSettings
                {
                    SaveAttachmentsAndBinaryContentExternally = saveAttachmentsExt,
                    WrapExternalContentInXml = wrapExtContentInXml,  //must be true for XEP to properly attach external PDFs
                    SaveTextAsXhtml = true, //required to render html inside the PDF
                    MaximumXmlFileSize = maxFileSize
                };
                if (!string.IsNullOrWhiteSpace(skipAfterMsgId))
                    settings.SkipAfterMessageId = skipAfterMsgId;

                var eProc = new EmailToEaxsProcessor(logger, settings);
                _ = eProc.ConvertMboxToEaxs(inFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");

                return xmlFile;
            }
            else
            {
                return "";
            }
        }

        private string ConvertEmlFolderToEaxs(string folderPath, bool saveAttachmentsExt, bool wrapExtContentInXml, long maxFileSize = 0)
        {
            if (logger != null)
            {
                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.GetDirectoryName(folderPath) ?? ".", Path.GetFileName(folderPath) + "Out", Path.ChangeExtension(Path.GetFileName(folderPath), "xml"));
                var inFolder = Path.Combine(testFilesBaseDirectory, folderPath);
                var outFolder = Path.Combine(Path.GetDirectoryName(inFolder) ?? ".", Path.GetFileName(inFolder) + "Out");

                var settings = new EmailToEaxsProcessorSettings
                {
                    SaveAttachmentsAndBinaryContentExternally = saveAttachmentsExt,
                    WrapExternalContentInXml = wrapExtContentInXml,  //Must be true for XEP to properly attach external PDFs
                    SaveTextAsXhtml = true, //required to render html inside the PDF
                    MaximumXmlFileSize = maxFileSize
                };

                var eProc = new EmailToEaxsProcessor(logger, settings);
                _ = eProc.ConvertFolderOfEmlToEaxs(inFolder, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                return xmlFile;
            }
            else
            {
                return "";
            }
        }


        private bool IsPdfValid(string pdfFile)
        {

            if (VALIDATE_PDFS == false) return true;

            bool ret = true;

            using var reader = new PdfReader(pdfFile);

            if (!reader.Catalog.Contains(new PdfName("DPartRoot")))
            {
                logger?.LogDebug("Catalog is missing DPartRoot");
                ret = false;
            }

            if (reader.PdfVersion != '7')
            {
                logger?.LogDebug("Pdf version is not is not 1.7");
                ret = false;
            }

            //TODO: Add more tests


            return ret;
        }
    }
}