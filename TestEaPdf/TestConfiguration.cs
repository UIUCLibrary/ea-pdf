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
                .AddXmlFile("App.config", optional: false, reloadOnChange: false)
                .Build();

            var settings = new EmailToEaxsProcessorSettings(config);

            CheckEmailToEaxsProcessorSettings(settings);
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
                .AddXmlFile("App.config", optional: false, reloadOnChange: false)
                .Build();

            var settings = new EaxsToEaPdfProcessorSettings(config);

            CheckEaxsToEaPdfProcessorSettings(settings);
        }

        [TestMethod]
        public void TestEaxsToEaPdfProcessorSettingsInvalidBaseFont()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("App_Extra_BaseFont.config", optional: false, reloadOnChange: false)
                .Build();

            //The extra base font should be ignored
            var settings = new EaxsToEaPdfProcessorSettings(config);

            CheckEaxsToEaPdfProcessorSettings(settings);
        }

        private void CheckEaxsToEaPdfProcessorSettings(EaxsToEaPdfProcessorSettings settings)
        {
            Assert.AreEqual("aaa\\XResources\\eaxs_to_fo.xsl", settings.XsltFoFilePath);
            Assert.AreEqual("bbb\\XResources\\eaxs_to_xmp.xsl", settings.XsltXmpFilePath);
            Assert.AreEqual("ccc\\XResources\\eaxs_to_root_xmp.xsl", settings.XsltRootXmpFilePath);
            Assert.AreEqual("ddd\\Fonts", settings.FontsFolder);

            Assert.AreEqual(2, settings.LanguageFontMapping.Count);
            Assert.IsTrue(settings.LanguageFontMapping.ContainsKey(FontHelpers.DEFAULT_SCRIPT));
            Assert.IsTrue(settings.LanguageFontMapping.ContainsKey("Hebr"));

            Assert.AreEqual(3, settings.LanguageFontMapping[FontHelpers.DEFAULT_SCRIPT].Count);
            Assert.AreEqual("serif1",settings.LanguageFontMapping[FontHelpers.DEFAULT_SCRIPT][FontHelpers.BaseFontFamily.Serif]);
            Assert.AreEqual("sans-serif1", settings.LanguageFontMapping[FontHelpers.DEFAULT_SCRIPT][FontHelpers.BaseFontFamily.SansSerif], "sans-serif1");
            Assert.AreEqual("monospace1", settings.LanguageFontMapping[FontHelpers.DEFAULT_SCRIPT][FontHelpers.BaseFontFamily.Monospace]);

            Assert.AreEqual(3, settings.LanguageFontMapping["Hebr"].Count);
            Assert.AreEqual("serif2", settings.LanguageFontMapping["Hebr"][FontHelpers.BaseFontFamily.Serif]);
            Assert.AreEqual("sans-serif2", settings.LanguageFontMapping["Hebr"][FontHelpers.BaseFontFamily.SansSerif]);
            Assert.AreEqual("monospace2", settings.LanguageFontMapping["Hebr"][FontHelpers.BaseFontFamily.Monospace]);


            Assert.AreEqual(1, settings.AllSupportedScripts.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void TestEaxsToEaPdfProcessorSettingsBadScript()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("App_bad_script.config", optional: false, reloadOnChange: false)
                .Build();
            _ = new EaxsToEaPdfProcessorSettings(config);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void TestEaxsToEaPdfProcessorSettingsMissingFamilies()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("App_Missing_Families.config", optional: false, reloadOnChange: false)
                .Build();

            _ = new EaxsToEaPdfProcessorSettings(config);
        }

    }
}
