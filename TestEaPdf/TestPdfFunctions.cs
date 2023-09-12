using Extensions.Logging.ListOfString;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            Dictionary<Regex, FontHelper.BaseFontFamily> baseFontMapping = new()
            {
                { new Regex("^Kurinto Mono.*") , FontHelper.BaseFontFamily.Monospace },
                { new Regex("^Kurinto Sans.*") , FontHelper.BaseFontFamily.SansSerif },
                { new Regex("^Kurinto Text.*") , FontHelper.BaseFontFamily.Serif }
            };

            var fonts = FontHelper.GetDictionaryOfFonts("Fonts", baseFontMapping);

            Assert.IsNotNull(fonts);
            Assert.IsTrue(fonts.Count > 0);
            Assert.IsTrue(fonts.ContainsKey(FontHelper.BaseFontFamily.Serif));
        }

        [TestMethod]
        public void TestXepFontConfig()
        {
            Dictionary<Regex, FontHelper.BaseFontFamily> baseFontMapping = new()
            {
                { new Regex("^Kurinto Mono.*") , FontHelper.BaseFontFamily.Monospace },
                { new Regex("^Kurinto Sans.*") , FontHelper.BaseFontFamily.SansSerif },
                { new Regex("^Kurinto Text.*") , FontHelper.BaseFontFamily.Serif }
            };

            var fonts = FontHelper.GenerateXepFontsConfig(@"Fonts", baseFontMapping);

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
            Dictionary<Regex, FontHelper.BaseFontFamily> baseFontMapping = new()
            {
                { new Regex("^Kurinto Mono.*") , FontHelper.BaseFontFamily.Monospace },
                { new Regex("^Kurinto Sans.*") , FontHelper.BaseFontFamily.SansSerif },
                { new Regex("^Kurinto Text.*") , FontHelper.BaseFontFamily.Serif }
            };

            var fonts = FontHelper.GenerateFopFontsConfig(@"Fonts", baseFontMapping);

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

        [TestMethod]
        public void TestMozillaThunderbirdWithNestedFoldersFop()
        {
            if (logger != null)
            {
                string inPath = "MozillaThunderbird\\DLF Distributed Library";
                ConvertMBoxToEaxs(inPath);

                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.ChangeExtension(inPath, "xml"));
                var pdfFile = Path.ChangeExtension(xmlFile, "fop.pdf");
                var configFile = Path.GetFullPath("XResources\\fop.xconf");

                var xslt = new SaxonXsltTransformer();
                var fop = new FopToPdfTransformer(configFile);
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, fop, iText, set);

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

        [TestMethod]
        public void TestMozillaThunderbirdWithNestedFoldersXep()
        {
            if (logger != null)
            {
                string inPath = "MozillaThunderbird\\DLF Distributed Library";
                ConvertMBoxToEaxs(inPath);

                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.ChangeExtension(inPath, "xml"));
                var pdfFile = Path.ChangeExtension(xmlFile, "xep.pdf");
                var configFile = Path.GetFullPath("XResources\\xep.xml");

                var xslt = new SaxonXsltTransformer();
                var xep = new XepToPdfTransformer(configFile);
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, xep, iText, set);

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
        [TestMethod]
        public void TestChineseEmailsFop()
        {
            if (logger != null)
            {
                string inPath = "Non-Western\\Chinese";
                ConvertEmlFolderToEaxs(inPath);

                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.GetDirectoryName(inPath) ?? ".", Path.GetFileName(inPath) + "Out", Path.ChangeExtension(Path.GetFileName(inPath), "xml"));
                var pdfFile = Path.ChangeExtension(xmlFile, "fop.pdf");
                var configFile = Path.GetFullPath("XResources\\fop.xconf");

                var xslt = new SaxonXsltTransformer();
                var fop = new FopToPdfTransformer(configFile);
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, fop, iText, set);

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

        [TestMethod]
        public void TestChineseEmailsXep()
        {
            if (logger != null)
            {
                string inPath = "Non-Western\\Chinese";
                ConvertEmlFolderToEaxs(inPath);

                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.GetDirectoryName(inPath) ?? ".", Path.GetFileName(inPath) + "Out", Path.ChangeExtension(Path.GetFileName(inPath), "xml"));
                var pdfFile = Path.ChangeExtension(xmlFile, "xep.pdf");
                var configFile = Path.GetFullPath("XResources\\xep.xml");

                var xslt = new SaxonXsltTransformer();
                var xep = new XepToPdfTransformer(configFile);
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, xep, iText, set);

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

        [TestMethod]
        public void TestArabicEmailsFop()
        {
            if (logger != null)
            {
                string inPath = "Non-Western\\Arabic";
                ConvertEmlFolderToEaxs(inPath);

                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.GetDirectoryName(inPath) ?? ".", Path.GetFileName(inPath) + "Out", Path.ChangeExtension(Path.GetFileName(inPath), "xml"));
                var pdfFile = Path.ChangeExtension(xmlFile, "fop.pdf");
                var configFile = Path.GetFullPath("XResources\\fop.xconf");

                var xslt = new SaxonXsltTransformer();
                var fop = new FopToPdfTransformer(configFile);
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, fop, iText, set);

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

        [TestMethod]
        public void TestArabicEmailsXep()
        {
            if (logger != null)
            {
                string inPath = "Non-Western\\Arabic";
                ConvertEmlFolderToEaxs(inPath);

                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.GetDirectoryName(inPath) ?? ".", Path.GetFileName(inPath) + "Out", Path.ChangeExtension(Path.GetFileName(inPath), "xml"));
                var pdfFile = Path.ChangeExtension(xmlFile, "xep.pdf");
                var configFile = Path.GetFullPath("XResources\\xep.xml");

                var xslt = new SaxonXsltTransformer();
                var xep = new XepToPdfTransformer(configFile);
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, xep, iText, set);

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

        [TestMethod]
        public void TestEaxsToPdfProcessorFop()
        {
            if (logger != null)
            {
                string inPath = "MozillaThunderbird\\short-test\\short-test.mbox";
                ConvertMBoxToEaxs(inPath);

                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.ChangeExtension(inPath, "xml"));
                var pdfFile = Path.ChangeExtension(xmlFile, "fop.pdf");
                var configFile = Path.GetFullPath("XResources\\fop.xconf");

                var xslt = new SaxonXsltTransformer();
                var fop = new FopToPdfTransformer(configFile);
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, fop, iText, set);

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

        [TestMethod]
        public void TestEaxsToPdfProcessorXep()
        {
            if (logger != null)
            {
                string inPath = "MozillaThunderbird\\short-test\\short-test.mbox";
                ConvertMBoxToEaxs(inPath);

                var xmlFile = Path.Combine(testFilesBaseDirectory, Path.ChangeExtension(inPath, "xml"));
                var pdfFile = Path.ChangeExtension(xmlFile, "xep.pdf");
                var configFile = Path.GetFullPath("XResources\\xep.xml");

                var xslt = new SaxonXsltTransformer();
                var xep = new XepToPdfTransformer(configFile);
                var iText = new ITextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, xep, iText, set);

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

        private void ConvertMBoxToEaxs(string filePath, string? skipAfterMsgId = null)
        {
            if (logger != null)
            {
                var inFile = Path.Combine(testFilesBaseDirectory, filePath);
                var outFolder = Path.GetDirectoryName(inFile);

                if (outFolder == null)
                    Assert.Fail("Could not get directory name from " + inFile);

                var settings = new EmailToEaxsProcessorSettings();

                settings.WrapExternalContentInXml = true;  //required for XEP to properly attach external PDFs
                settings.SaveTextAsXhtml = true; //required to render html inside the PDF
                if (!string.IsNullOrWhiteSpace(skipAfterMsgId))
                    settings.SkipAfterMessageId = skipAfterMsgId;

                var eProc = new EmailToEaxsProcessor(logger, settings);

                var count = eProc.ConvertMboxToEaxs(inFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
            }
        }

        private void ConvertEmlFolderToEaxs(string folderPath)
        {
            if (logger != null)
            {
                var inFolder = Path.Combine(testFilesBaseDirectory, folderPath);
                var outFolder = Path.Combine(Path.GetDirectoryName(inFolder) ?? ".", Path.GetFileName(inFolder) + "Out");

                var settings = new EmailToEaxsProcessorSettings();

                settings.WrapExternalContentInXml = true;  //required for XEP to properly attach external PDFs
                settings.SaveTextAsXhtml = true; //required to render html inside the PDF

                var eProc = new EmailToEaxsProcessor(logger, settings);

                var count = eProc.ConvertFolderOfEmlToEaxs(inFolder, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
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