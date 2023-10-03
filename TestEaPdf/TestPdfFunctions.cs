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
        private readonly bool OPEN_PDFS = true;  //set to true to open the PDFs in the default PDF viewer
        private readonly bool VALIDATE_PDFS = true;  //set to true to validate the PDFs using the PDF/A validator

        ILogger<EaxsToEaPdfProcessor>? logger;
        ILoggerFactory? loggerFactory;
        readonly List<string> loggedLines = new();
        readonly string testFilesBaseDirectory = @"C:\Users\thabi\Source\UIUC\ea-pdf\SampleFiles\Testing";

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
            loggerFactory?.Dispose();
        }

        [TestMethod]
        public void TestFontList()
        {
            Dictionary<Regex, FontHelpers.BaseFontFamily> baseFontMapping = new()
            {
                { new Regex("^Kurinto Mono.*") , FontHelpers.BaseFontFamily.Monospace },
                { new Regex("^Kurinto Sans.*") , FontHelpers.BaseFontFamily.SansSerif },
                { new Regex("^Kurinto Text.*") , FontHelpers.BaseFontFamily.Serif }
            };

            var fonts = FontHelpers.GetDictionaryOfFonts("Fonts", baseFontMapping);

            Assert.IsNotNull(fonts);
            Assert.IsTrue(fonts.Count > 0);
            Assert.IsTrue(fonts.ContainsKey(FontHelpers.BaseFontFamily.Serif));
        }

        [TestMethod]
        public void TestXepFontConfig()
        {
            Dictionary<Regex, FontHelpers.BaseFontFamily> baseFontMapping = new()
            {
                { new Regex("^Kurinto Mono.*") , FontHelpers.BaseFontFamily.Monospace },
                { new Regex("^Kurinto Sans.*") , FontHelpers.BaseFontFamily.SansSerif },
                { new Regex("^Kurinto Text.*") , FontHelpers.BaseFontFamily.Serif }
            };

            var fonts = FontHelpers.GenerateXepFontsConfig(@"Fonts", baseFontMapping);

            Assert.IsFalse(string.IsNullOrWhiteSpace(fonts));

            Debug.Print(fonts);

            var xdoc = new XmlDocument();
            xdoc.LoadXml(fonts);

            var node = xdoc.SelectSingleNode("/fonts");
            Assert.IsNotNull(node);

            node = xdoc.SelectSingleNode("/fonts/font-group");
            Assert.IsNotNull(node);

            node = xdoc.SelectSingleNode("/fonts/font-group/font-family");
            Assert.IsNotNull(node);

            node = xdoc.SelectSingleNode("/fonts/font-group/font-family/font");
            Assert.IsNotNull(node);

            node = xdoc.SelectSingleNode("/fonts/font-alias");
            Assert.IsNotNull(node);

        }

        [TestMethod]
        public void TestFopFontConfig()
        {
            Dictionary<Regex, FontHelpers.BaseFontFamily> baseFontMapping = new()
            {
                { new Regex("^Kurinto Mono.*") , FontHelpers.BaseFontFamily.Monospace },
                { new Regex("^Kurinto Sans.*") , FontHelpers.BaseFontFamily.SansSerif },
                { new Regex("^Kurinto Text.*") , FontHelpers.BaseFontFamily.Serif }
            };

            var fonts = FontHelpers.GenerateFopFontsConfig(@"Fonts", baseFontMapping);

            Assert.IsFalse(string.IsNullOrWhiteSpace(fonts));

            Debug.Print(fonts);

            var xdoc = new XmlDocument();
            xdoc.LoadXml(fonts);

            var node = xdoc.SelectSingleNode("/fonts");
            Assert.IsNotNull(node);

            node = xdoc.SelectSingleNode("/fonts/font");
            Assert.IsNotNull(node);
            node = xdoc.SelectSingleNode("/fonts/font/@embed-url");
            Assert.IsNotNull(node);

            node = xdoc.SelectSingleNode("/fonts/font/font-triplet");
            Assert.IsNotNull(node);
            node = xdoc.SelectSingleNode("/fonts/font/font-triplet/@name");
            Assert.IsNotNull(node);
            node = xdoc.SelectSingleNode("/fonts/font/font-triplet/@style");
            Assert.IsNotNull(node);
            node = xdoc.SelectSingleNode("/fonts/font/font-triplet/@weight");
            Assert.IsNotNull(node);

        }


        [DataRow("Non-Western\\Arabic", "fop", false, DisplayName = "ARABIC-FOP-NO-WRAP")] //Use Arabic folder and FOP and do not wrap external files in XML
        [DataRow("Non-Western\\Arabic", "fop", true, DisplayName = "ARABIC-FOP-WRAP")] //Use Arabic folder and FOP and wrap external files in XML
        [DataRow("Non-Western\\Arabic", "xep", true, DisplayName = "ARABIC-XEP-WRAP")] //Use Arabic folder and XEP and wrap external files in XML

        [DataRow("Non-Western\\Chinese", "fop", false, DisplayName = "CHINESE-FOP-NO-WRAP")] //Use Chinese folder and FOP and do not wrap external files in XML
        [DataRow("Non-Western\\Chinese", "fop", true, DisplayName = "CHINESE-FOP-WRAP")] //Use Chinese folder and FOP and wrap external files in XML
        [DataRow("Non-Western\\Chinese", "xep", true, DisplayName = "CHINESE-XEP-WRAP")] //Use Chinese folder and XEP and wrap external files in XML

        [DataRow("MozillaThunderbird\\short-test\\short-test.mbox", "fop", false, DisplayName = "ENGLISH-FOP-NO-WRAP")] //Use short-test file and FOP and do not wrap external files in XML
        [DataRow("MozillaThunderbird\\short-test\\short-test.mbox", "fop", true, DisplayName = "ENGLISH-FOP-WRAP")] //Use short-test file and FOP and wrap external files in XML
        [DataRow("MozillaThunderbird\\short-test\\short-test.mbox", "xep", true, DisplayName = "ENGLISH-XEP-WRAP")] //Use short-test file and XEP and wrap external files in XML

        [DataRow("MozillaThunderbird\\DLF Distributed Library", "fop", false, DisplayName = "MOZILLA-FOP-NO-WRAP")] //Use Mozilla file and FOP and do not wrap external files in XML
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "fop", true, DisplayName = "MOZILLA-FOP-WRAP")] //Use Mozilla file and FOP and wrap external files in XML
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "xep", true, DisplayName = "MOZILLA-XEP-WRAP")] //Use Mozilla file and XEP and wrap external files in XML

        [DataTestMethod]
        public void TestEaxsToPdfProcessor(string inPath, string foProcessor, bool wrap)
        {
            if (!foProcessor.Equals("fop", System.StringComparison.OrdinalIgnoreCase) && !foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(nameof(foProcessor));

            if (foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase) && !wrap)
                throw new ArgumentException("XEP requires external files to be wrapped in XML");

            if (logger != null)
            {
                var xmlFile = ConvertToEaxs(inPath, null, wrap);

                string pdfFile, configFile;
                IXslFoTransformer foTransformer;
                if (foProcessor.Equals("fop", System.StringComparison.OrdinalIgnoreCase))
                {
                    pdfFile = Path.ChangeExtension(xmlFile, $"fop{(wrap ? "_x" : "")}.pdf");
                    configFile = Path.GetFullPath("XResources\\fop.xconf");
                    foTransformer = new FopToPdfTransformer(configFile);
                }
                else if (foProcessor.Equals("xep", System.StringComparison.OrdinalIgnoreCase))
                {
                    pdfFile = Path.ChangeExtension(xmlFile, $"xep_{(wrap ? "_x" : "")}.pdf");
                    configFile = Path.GetFullPath("XResources\\xep.xml");
                    foTransformer = new XepToPdfTransformer(configFile);
                }
                else
                {
                    throw new ArgumentException(nameof(foProcessor));
                }

                var xslt = new SaxonXsltTransformer();
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, foTransformer, iText, set);

                proc.ConvertEaxsToPdf(xmlFile, pdfFile);

                Assert.IsTrue(File.Exists(pdfFile));

                Assert.IsTrue(IsPdfValid(pdfFile));

                if (VALIDATE_PDFS) Helpers.ValidatePdfAUsingVeraPdf(pdfFile);

                if (OPEN_PDFS)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pdfFile) { UseShellExecute = true });
            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }

        }



        private string ConvertToEaxs(string filePath, string? skipAfterMsgId = null, bool wrapExtContentInXml = true)
        {
            var inFile = Path.Combine(testFilesBaseDirectory, filePath);

            FileAttributes attr = File.GetAttributes(inFile);

            if (attr.HasFlag(FileAttributes.Directory))
                //directory of EML files
                return ConvertEmlFolderToEaxs(filePath, wrapExtContentInXml);
            else
                //single MBOX file
                return ConvertMBoxToEaxs(filePath, skipAfterMsgId, wrapExtContentInXml);
        }

        private string ConvertMBoxToEaxs(string filePath, string? skipAfterMsgId = null, bool wrapExtContentInXml = true)
        {
            if (logger != null)
            {
                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.ChangeExtension(filePath, "xml"));

                var inFile = Path.Combine(testFilesBaseDirectory, filePath);
                var outFolder = Path.GetDirectoryName(inFile);

                if (outFolder == null)
                    Assert.Fail("Could not get directory name from " + inFile);

                var settings = new EmailToEaxsProcessorSettings();

                settings.WrapExternalContentInXml = wrapExtContentInXml;  //must be true for XEP to properly attach external PDFs
                settings.SaveTextAsXhtml = true; //required to render html inside the PDF
                if (!string.IsNullOrWhiteSpace(skipAfterMsgId))
                    settings.SkipAfterMessageId = skipAfterMsgId;

                var eProc = new EmailToEaxsProcessor(logger, settings);

                var count = eProc.ConvertMboxToEaxs(inFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");

                return xmlFile;
            }
            else
            {
                return "";
            }
        }

        private string ConvertEmlFolderToEaxs(string folderPath, bool wrapExtContentInXml = true)
        {
            if (logger != null)
            {
                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.GetDirectoryName(folderPath) ?? ".", Path.GetFileName(folderPath) + "Out", Path.ChangeExtension(Path.GetFileName(folderPath), "xml"));
                var inFolder = Path.Combine(testFilesBaseDirectory, folderPath);
                var outFolder = Path.Combine(Path.GetDirectoryName(inFolder) ?? ".", Path.GetFileName(inFolder) + "Out");

                var settings = new EmailToEaxsProcessorSettings();

                settings.WrapExternalContentInXml = wrapExtContentInXml;  //Must be true for XEP to properly attach external PDFs
                settings.SaveTextAsXhtml = true; //required to render html inside the PDF

                var eProc = new EmailToEaxsProcessor(logger, settings);

                var count = eProc.ConvertFolderOfEmlToEaxs(inFolder, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
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