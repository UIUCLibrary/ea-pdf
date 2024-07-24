using AngleSharp.Text;
using Extensions.Logging.ListOfString;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            //FUTURE: skip for the time being, takes too long
            if (foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase))
                Assert.Inconclusive("Test is commented out");

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

                foreach (var file in files)
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
                if (nodesToRemove != null)
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
            //FUTURE: skip for the time being, takes too long
            if (foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase))
                Assert.Inconclusive("Test is commented out");

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
                var suffix = "_mbox" + (saveAttachmentsExt ? "_ext" : "") + (wrapExtContentInXml ? "_wrap" : "");
                var outFolder = Path.Combine(Path.GetDirectoryName(inFile) ?? ".", "out_" + Path.GetFileNameWithoutExtension(inFile) + suffix);

                if (outFolder == null)
                    Assert.Fail("Could not get directory name from " + inFile);

                var xmlFile = FilePathHelpers.GetXmlOutputFilePath(outFolder, inFile);

                if (outFolder == null)
                    Assert.Fail("Could not get directory name from " + inFile);

                var settings = new EmailToEaxsProcessorSettings
                {
                    SaveAttachmentsAndBinaryContentExternally = saveAttachmentsExt,
                    WrapExternalContentInXml = wrapExtContentInXml,  //must be true for XEP to properly attach external PDFs
                    SaveTextAsXhtml = true, //required to render html inside the PDF
                    MaximumXmlFileSize = maxFileSize,
                    IncludeSubFolders = true,
                    AllowMultipleSourceFilesPerOutputFile = true
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
                var suffix = "_eml" + (saveAttachmentsExt ? "_ext" : "") + (wrapExtContentInXml ? "_wrap" : "");
                var outFolder = Path.Combine(Path.GetDirectoryName(inFolder) ?? ".", "out_" + Path.GetFileName(inFolder) + suffix);

                var settings = new EmailToEaxsProcessorSettings
                {
                    SaveAttachmentsAndBinaryContentExternally = saveAttachmentsExt,
                    WrapExternalContentInXml = wrapExtContentInXml,  //Must be true for XEP to properly attach external PDFs
                    SaveTextAsXhtml = true, //required to render html inside the PDF
                    MaximumXmlFileSize = maxFileSize,
                    IncludeSubFolders = true
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

            var catalog = reader.Catalog;

            if (catalog == null)
            {
                logger?.LogDebug("PDF Catalog is missing; no further tests were run");
                ret = false;
                return false; // no use even continuing if this is missing
            }

            if (!catalog.Contains(new PdfName("DPartRoot")))
            {
                logger?.LogDebug("Catalog is missing DPartRoot");
                ret = false;
            }

            if (reader.PdfVersion != '7')
            {
                logger?.LogDebug("Pdf version is not 1.7");
                ret = false;
            }

            //some checks for the XMP metadata
            var xmp = reader.Metadata;
            if (xmp == null)
            {
                logger?.LogDebug("Metadata is missing");
                ret = false;
            }
            else
            {
                var xmpString = Encoding.UTF8.GetString(xmp);
                var xmpDoc = new XmlDocument();
                xmpDoc.LoadXml(xmpString);

                var xmlns = new XmlNamespaceManager(xmpDoc.NameTable);
                xmlns.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                xmlns.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
                xmlns.AddNamespace("dcterms", "http://purl.org/dc/terms/");
                xmlns.AddNamespace("foaf", "http://xmlns.com/foaf/0.1/");
                xmlns.AddNamespace("pdf", "http://ns.adobe.com/pdf/1.3/");
                xmlns.AddNamespace("pdfaid", "http://www.aiim.org/pdfa/ns/id/");
                xmlns.AddNamespace("pdfmail", "http://www.pdfa.org/eapdf/");
                xmlns.AddNamespace("pdfmailid", "http://www.pdfa.org/eapdf/ns/id/");
                xmlns.AddNamespace("pdfmailmeta", "http://www.pdfa.org/eapdf/ns/meta/");
                xmlns.AddNamespace("pdfx", "http://ns.adobe.com/pdfx/1.3/");
                xmlns.AddNamespace("xmp", "http://ns.adobe.com/xap/1.0/");

                var keywords = xmpDoc.SelectSingleNode("//pdf:Keywords", xmlns)?.InnerText ?? "";
                if (!keywords.Contains("EA-PDF"))
                {
                    logger?.LogDebug("pdf:Keywords does not contain 'EA-PDF'");
                    ret = false;
                }

                var pdfVersion = xmpDoc.SelectSingleNode("//pdf:PDFVersion", xmlns)?.InnerText ?? "";
                if (pdfVersion != "1.7")
                {
                    logger?.LogDebug("pdf:PDFVersion does nopt equal '1.7'");
                    ret = false;
                }

                if (Enum.TryParse(xmpDoc.SelectSingleNode("//pdfaid:conformance", xmlns)?.InnerText, out PdfAIdConformance pdfaidConformance))
                {
                    var pdfaidPart = xmpDoc.SelectSingleNode("//pdfaid:part", xmlns)?.InnerText ?? "";
                    if (string.IsNullOrWhiteSpace(pdfaidPart))
                    {
                        logger?.LogDebug("PDF/A identifying part is missing");
                        ret = false;
                    }
                    else
                    {
                        if (pdfaidConformance != PdfAIdConformance.A && pdfaidConformance != PdfAIdConformance.U)
                        {
                            logger?.LogDebug($"PDF/A conformance is {pdfaidConformance}.  It must be either {PdfAIdConformance.A} or {PdfAIdConformance.U}");
                            ret = false;
                        }

                        if (!pdfaidPart.Equals("3"))
                        {
                            logger?.LogDebug($"PDF/A part is {pdfaidPart}.  It must be 3");
                            ret = false;
                        }

                    }
                }
                else
                {
                    logger?.LogDebug("PDF/A identifying conformance level is missing");
                    ret = false;
                }

                if (Enum.TryParse(xmpDoc.SelectSingleNode("//pdfmailid:conformance", xmlns)?.InnerText, out PdfMailIdConformance pdfmailidConformance))
                {
                    var pdfmailidVersion = xmpDoc.SelectSingleNode("//pdfmailid:version", xmlns)?.InnerText ?? "";
                    var pdfmailidRev = xmpDoc.SelectSingleNode("//pdfmailid:rev", xmlns)?.InnerText ?? "";
                    if (string.IsNullOrWhiteSpace(pdfmailidVersion) || string.IsNullOrWhiteSpace(pdfmailidRev))
                    {
                        logger?.LogDebug("EA-PDF identifying metadata (version or rev) is missing");
                        ret = false;
                    }
                    else
                    {
                        if (!pdfmailidVersion.Equals("1"))
                        {
                            logger?.LogDebug($"EA-PDF version is {pdfmailidVersion}.  It must be 1.");
                            ret = false;
                        }

                        if (!pdfmailidRev.Equals("2024"))
                        {
                            logger?.LogDebug($"PDF/A revision is {pdfmailidRev}.  It must be 2024.");
                            ret = false;
                        }

                        //get the number of email messages in the metadata
                        var messageCount = xmpDoc.SelectNodes("//pdfmailmeta:email/rdf:Seq/rdf:li", xmlns)?.Count ?? 0;
                        if (messageCount == 0)
                        {
                            logger?.LogDebug("No email messages found in the metadata");
                            ret = false;
                        }

                        PdfMailIdConformance[] conformanceLevels = { PdfMailIdConformance.s, PdfMailIdConformance.si, PdfMailIdConformance.m, PdfMailIdConformance.mi };
                        //NOTE: This tool does not support 'c' or 'ci' conformance levels
                        if (!conformanceLevels.Contains(pdfmailidConformance))
                        {
                            logger?.LogDebug($"EA-PDF conformance is {pdfmailidConformance}.  It must be one of: '{string.Join("','", conformanceLevels)}'");
                            ret = false;
                        }
                        if (messageCount <= 1 && pdfmailidConformance != PdfMailIdConformance.s && pdfmailidConformance != PdfMailIdConformance.si)
                        {
                            logger?.LogDebug($"EA-PDF conformance is {pdfmailidConformance}.  It must be {PdfMailIdConformance.s} if there is a single messages in the PDF.");
                            ret = false;
                        }
                        if (messageCount > 1 && pdfmailidConformance != PdfMailIdConformance.m && pdfmailidConformance != PdfMailIdConformance.mi)
                        {
                            logger?.LogDebug($"EA-PDF conformance is {pdfmailidConformance}.  It must be {PdfMailIdConformance.m} if there are multiple messages in the PDF.");
                            ret = false;
                        }
                    }

                    //get the count of attachments in the PDF
                    var pdfAttachmentCount = xmpDoc.SelectNodes("//pdfmailmeta:attachments/rdf:Seq/rdf:li", xmlns)?.Count ?? 0;

                    var pageMode = catalog.GetAsName(PdfName.Pagemode);
                    if (pageMode == null)
                    {
                        logger?.LogDebug("PageMode is not set");
                        ret = false;
                    }
                    else if ((pdfmailidConformance == PdfMailIdConformance.s || pdfmailidConformance == PdfMailIdConformance.si) && pdfAttachmentCount > 0 && !pageMode.ToString().Equals("/UseAttachments"))
                    {
                        logger?.LogDebug("Conformance level is s and there are attachments, but PageMode is not set to /UseAttachments");
                        ret = false;
                    }
                    else if ((pdfmailidConformance == PdfMailIdConformance.m || pdfmailidConformance == PdfMailIdConformance.mi) && !pageMode.ToString().Equals("/UseOutlines"))
                    {
                        logger?.LogDebug("Conformance level is m, but PageMode is not set to /UseOutlines");
                        ret = false;
                    }
                    else if ((pdfmailidConformance == PdfMailIdConformance.s || pdfmailidConformance == PdfMailIdConformance.si) && pdfAttachmentCount == 0 && !pageMode.ToString().Equals("/UseOutlines"))
                    {
                        logger?.LogDebug("Conformance level is s and there are no attachments, but PageMode is not set to /UseOutlines");
                        ret = false;
                    }

                    var viewerPreferences = catalog.GetAsDict(PdfName.Viewerpreferences);
                    if (viewerPreferences == null)
                    {
                        logger?.LogDebug("ViewerPreferences is not set");
                        ret = false;
                    }
                    else
                    {
                        var displayDocTitle = viewerPreferences.GetAsBoolean(PdfName.Displaydoctitle);
                        if (displayDocTitle == null || !displayDocTitle.BooleanValue)
                        {
                            logger?.LogDebug("DisplayDocTitle is not set to true");
                            ret = false;
                        }
                        var nonFullScreenPageMode = viewerPreferences.GetAsName(PdfName.Nonfullscreenpagemode);
                        if (nonFullScreenPageMode == null || !nonFullScreenPageMode.ToString().Equals("/UseOutlines"))
                        {
                            logger?.LogDebug("NonFullScreenPageMode is not set to /UseOutlines");
                            ret = false;
                        }
                    }

                }
                else
                {
                    logger?.LogDebug("EA-PDF identifying conformance level is missing");
                    ret = false;
                }


            }

            //TODO: Add more tests


            return ret;
        }
    }
}