using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using UIUCLibrary.EaPdf;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestConfiguration
    {

        [TestMethod]
        public void TestEmailToEaxsProcessorSettings()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("TEST_App.config", optional: false, reloadOnChange: false)
                .Build();

            var b = config.GetValue<bool?>("junk:bogus:key");
            Assert.IsNull(b);

            var settings = new EmailToEaxsProcessorSettings(config);

            CheckEmailToEaxsProcessorSettings(settings);
        }

        [TestMethod]
        public void TestAppSettingsJson()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("TEST_appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var settings1 = new EmailToEaxsProcessorSettings(config);

            var settings2 = new EaxsToEaPdfProcessorSettings(config);

            CheckEmailToEaxsProcessorSettings(settings1);
            CheckEaxsToEaPdfProcessorSettings(settings2);
        }

        private void CheckEmailToEaxsProcessorSettings(EmailToEaxsProcessorSettings settings)
        {
            Assert.AreEqual("MD5", settings.HashAlgorithmName);
            Assert.IsTrue(settings.SaveAttachmentsAndBinaryContentExternally);
            Assert.IsTrue( settings.WrapExternalContentInXml);
            Assert.IsTrue(settings.PreserveBinaryAttachmentTransferEncodingIfPossible);
            Assert.IsTrue(settings.PreserveTextAttachmentTransferEncoding);
            Assert.IsTrue(settings.IncludeSubFolders);
            Assert.AreEqual("ExternalContentFolder", settings.ExternalContentFolder);
            Assert.IsTrue(settings.OneFilePerMessageFile);
            Assert.AreEqual(100000, settings.MaximumXmlFileSize);
            Assert.IsTrue(settings.SaveTextAsXhtml);
            Assert.AreEqual(LogLevel.Warning, settings.LogToXmlThreshold);
            Assert.AreEqual(".mbx", settings.DefaultFileExtension);
            Assert.AreEqual("SkipUntilMessageId", settings.SkipUntilMessageId);
            Assert.IsTrue(string.IsNullOrEmpty(settings.SkipAfterMessageId));

            Assert.AreEqual(3, settings?.ExtraHtmlCharacterEntities?.Count ?? 0);
            Assert.AreEqual(44, settings?.ExtraHtmlCharacterEntities?["COMMA"] ?? 0);
            Assert.AreEqual(46, settings?.ExtraHtmlCharacterEntities?["PERIOD"] ?? 0);
            Assert.AreEqual(0x22, settings?.ExtraHtmlCharacterEntities?["QUOT"] ?? 0);
        }

        [TestMethod]
        public void TestEaxsToEaPdfProcessorSettings()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("TEST_App.config", optional: false, reloadOnChange: false)
                .Build();

            var settings = new EaxsToEaPdfProcessorSettings(config);

            CheckEaxsToEaPdfProcessorSettings(settings);
        }

        [TestMethod]
        public void TestEaxsToEaPdfProcessorSettingsInvalidBaseFont()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("TEST_App_Extra_BaseFont.config", optional: false, reloadOnChange: false)
                .Build();

            //The extra base font should be ignored
            var settings = new EaxsToEaPdfProcessorSettings(config);

            CheckEaxsToEaPdfProcessorSettings(settings);
        }

        private void CheckEaxsToEaPdfProcessorSettings(EaxsToEaPdfProcessorSettings settings)
        {
            Assert.AreEqual("XResources\\aaa.xsl", settings.XsltFoFilePath);
            Assert.AreEqual("XResources\\bbb.xsl", settings.XsltDpartFilePath);
            Assert.AreEqual("XResources\\ccc.xsl", settings.XsltRootXmpFilePath);

            Assert.AreEqual(2, settings.ScriptFontMapping.Count);
            Assert.IsTrue(settings.ScriptFontMapping.ContainsKey(FontHelpers.DEFAULT_SCRIPT));
            Assert.IsTrue(settings.ScriptFontMapping.ContainsKey("Hebr"));

            Assert.AreEqual(3, settings.ScriptFontMapping[FontHelpers.DEFAULT_SCRIPT].Count);
            Assert.AreEqual("serif1",settings.ScriptFontMapping[FontHelpers.DEFAULT_SCRIPT][FontHelpers.BaseFontFamily.Serif]);
            Assert.AreEqual("sans-serif1", settings.ScriptFontMapping[FontHelpers.DEFAULT_SCRIPT][FontHelpers.BaseFontFamily.SansSerif], "sans-serif1");
            Assert.AreEqual("monospace1", settings.ScriptFontMapping[FontHelpers.DEFAULT_SCRIPT][FontHelpers.BaseFontFamily.Monospace]);

            Assert.AreEqual(3, settings.ScriptFontMapping["Hebr"].Count);
            Assert.AreEqual("serif2", settings.ScriptFontMapping["Hebr"][FontHelpers.BaseFontFamily.Serif]);
            Assert.AreEqual("sans-serif2", settings.ScriptFontMapping["Hebr"][FontHelpers.BaseFontFamily.SansSerif]);
            Assert.AreEqual("monospace2", settings.ScriptFontMapping["Hebr"][FontHelpers.BaseFontFamily.Monospace]);


            Assert.AreEqual(1, settings.AllSupportedScripts.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void TestEaxsToEaPdfProcessorSettingsBadScript()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("TEST_App_bad_script.config", optional: false, reloadOnChange: false)
                .Build();
            _ = new EaxsToEaPdfProcessorSettings(config);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void TestEaxsToEaPdfProcessorSettingsMissingFamilies()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("TEST_App_Missing_Families.config", optional: false, reloadOnChange: false)
                .Build();

            _ = new EaxsToEaPdfProcessorSettings(config);
        }

    }
}
