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

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestTransformers
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
            loggerFactory?.Dispose();
        }

        //NOTE:  These are named so they will run in order.  Test01 runs first, then Test02, etc.

        [TestMethod]
        public void Test01SaxonXsltFopTransformer()
        {
            if (logger != null)
            {
                var xmlFile = Path.Combine(testFilesBaseDirectory, "MozillaThunderbird\\short-test\\DLF Distributed Library_short_test.xml");
                var xsltFile = "XResources\\eaxs_to_fo.xslt";
                var foFile = Path.ChangeExtension(xmlFile, "fop");

                File.Delete(foFile);

                var messages = new List<(LogLevel level, string message)>();
                var parms = new Dictionary<string,object>() { { "fo-processor-version", "FOP Version 2.8" } };

                var tran = new SaxonXsltTransformer();

                var version = tran.ProcessorVersion;
                Assert.IsTrue(version.StartsWith("Saxon",StringComparison.OrdinalIgnoreCase));
                logger.LogInformation("Version: {version}", version);

                int ret = tran.Transform(xmlFile, xsltFile, foFile, parms, ref messages);

                foreach (var (level, message) in messages)
                {
                    logger.Log(level, message);
                }

                Assert.AreEqual(0, ret);

                Assert.IsTrue(File.Exists(foFile));

            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }

        }

        [TestMethod]
        public void Test02FopToPdfTransformer()
        {
            if (logger != null)
            {
                var foFile = Path.Combine(testFilesBaseDirectory, "MozillaThunderbird\\short-test\\DLF Distributed Library_short_test.fop");
                var configFile = Path.GetFullPath("XResources\\fop.xconf");
                var pdfFile = Path.ChangeExtension(foFile, "fop.pdf");

                File.Delete(pdfFile);

                var messages = new List<(LogLevel level, string message)>();

                var tran = new FopToPdfTransformer(configFile);

                var version = tran.ProcessorVersion;
                Assert.IsTrue(version.StartsWith("FOP", StringComparison.OrdinalIgnoreCase));
                logger.LogInformation("Version: {version}", version);

                int ret = tran.Transform(foFile, pdfFile, ref messages);

                foreach (var (level, message) in messages)
                {
                    logger.Log(level, message);
                }

                Assert.AreEqual(0, ret);

                Assert.IsTrue(File.Exists(pdfFile));

                if(OPEN_PDFS)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pdfFile) { UseShellExecute = true });

            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }

        }

        [TestMethod]
        public void Test03SaxonXsltXepTransformer()
        {
            if (logger != null)
            {
                var xmlFile = Path.Combine(testFilesBaseDirectory, "MozillaThunderbird\\short-test\\DLF Distributed Library_short_test.xml");
                var xsltFile = "XResources\\eaxs_to_fo.xslt";
                var foFile = Path.ChangeExtension(xmlFile, ".xep");

                File.Delete(foFile);

                var messages = new List<(LogLevel level, string message)>();
                var parms = new Dictionary<string, object>() { { "fo-processor-version", "XEP 4.30.961" } };

                var tran = new SaxonXsltTransformer();

                var version = tran.ProcessorVersion;
                Assert.IsTrue(version.StartsWith("Saxon", StringComparison.OrdinalIgnoreCase));
                logger.LogInformation("Version: {version}",version);

                int ret = tran.Transform(xmlFile, xsltFile, foFile, parms, ref messages);

                foreach (var (level, message) in messages)
                {
                    logger.Log(level, message);
                }

                Assert.AreEqual(0, ret);

                Assert.IsTrue(File.Exists(foFile));

            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }

        }

        [TestMethod]
        public void Test04XepToPdfTransformer()
        {
            if (logger != null)
            {
                var foFile = Path.Combine(testFilesBaseDirectory, "MozillaThunderbird\\short-test\\DLF Distributed Library_short_test.xep");
                var configFile = Path.GetFullPath("XResources\\xep.xml"); 
                var pdfFile = Path.ChangeExtension(foFile, "xep.pdf");

                File.Delete(pdfFile);

                var messages = new List<(LogLevel level, string message)>();

                var tran = new XepToPdfTransformer(configFile);

                var version = tran.ProcessorVersion;
                Assert.IsTrue(version.StartsWith("XEP", StringComparison.OrdinalIgnoreCase));
                logger.LogInformation("Version: {version}", version);

                int ret = tran.Transform(foFile, pdfFile, ref messages);

                foreach (var (level, message) in messages)
                {
                    logger.Log(level, message);
                }

                Assert.AreEqual(0, ret);

                Assert.IsTrue(File.Exists(pdfFile));

                if(OPEN_PDFS)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pdfFile) { UseShellExecute = true });

            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }

        }

    }
}