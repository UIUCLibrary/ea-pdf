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
    public class TestFontUtils
    {

        ILogger<EaxsToEaPdfProcessor>? logger;
        ILoggerFactory? loggerFactory;
        readonly List<string> loggedLines = new();

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


    }
}