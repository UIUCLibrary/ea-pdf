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
using UIUCLibrary.EaPdf.Helpers;
using NDepend.Path;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.interfaces;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TesPdfFunctions
    {
        private bool OPEN_PDFS = false;  //set to true to open the PDFs in the default PDF viewer

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
            if (loggerFactory != null) loggerFactory.Dispose();
        }


        [TestMethod]
        public void TestEaxsToPdfProcessorFop()
        {
            if (logger != null)
            {
                var xmlFile = Path.Combine(testFilesBaseDirectory, "MozillaThunderbird\\short-test\\DLF Distributed Library_short_test.xml");
                var pdfFile = Path.ChangeExtension(xmlFile, "fop.pdf");
                var configFile = Path.GetFullPath("XResources\\fop.xconf");

                var xslt = new SaxonXsltTransformer();
                var fop = new FopToPdfTransformer(configFile);
                var iText = new iTextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, fop, iText, set);

                proc.ConvertEaxsToPdf(xmlFile, pdfFile);

                Assert.IsTrue(File.Exists(pdfFile));

                Assert.IsTrue(IsPdfValid(pdfFile));

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
                var xmlFile = Path.Combine(testFilesBaseDirectory, "MozillaThunderbird\\short-test\\DLF Distributed Library_short_test.xml");
                var pdfFile = Path.ChangeExtension(xmlFile, "xep.pdf");
                var configFile = Path.GetFullPath("XResources\\xep.xml");

                var xslt = new SaxonXsltTransformer();
                var fop = new XepToPdfTransformer(configFile);
                var iText = new iTextSharpPdfEnhancerFactory();
                var set = new EaxsToEaPdfProcessorSettings();

                var proc = new EaxsToEaPdfProcessor(logger, xslt, fop, iText, set);

                proc.ConvertEaxsToPdf(xmlFile, pdfFile);

                Assert.IsTrue(File.Exists(pdfFile));

                Assert.IsTrue(IsPdfValid(pdfFile));

                if (OPEN_PDFS)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pdfFile) { UseShellExecute = true });

            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }

        }

        private bool IsPdfValid(string pdfFile)
        {
            bool ret = true;

            using var reader = new PdfReader(pdfFile);

            if(!reader.Catalog.Contains(new PdfName("DPartRoot")))
            {
                logger?.LogDebug("Catalog is missing DPartRoot");
                ret = false;
            }

            if(reader.PdfVersion != '7')
            {
                logger?.LogDebug("Pdf version is not is not 1.7");
                ret = false;
            }

            //TODO: Add more tests


            return ret;
        }
    }
}