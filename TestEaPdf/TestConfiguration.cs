using Microsoft.Extensions.Configuration;
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
        public void TestEaxsToEaPdfProcessorSettings()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("App.config", optional: false, reloadOnChange: false)
                .Build();

            var settings = new EaxsToEaPdfProcessorSettings(config);

            Assert.AreEqual("XResources\\eaxs_to_fo.xsl", settings.XsltFoFilePath);
            Assert.AreEqual("XResources\\eaxs_to_xmp.xsl", settings.XsltXmpFilePath);
            Assert.AreEqual("XResources\\eaxs_to_root_xmp.xsl", settings.XsltRootXmpFilePath);
            Assert.AreEqual("Fonts", settings.FontsFolder);  

            Assert.AreEqual(8, settings.LanguageFontMapping.Count);
            Assert.IsTrue(settings.LanguageFontMapping.ContainsKey(FontHelpers.DEFAULT_SCRIPT));
            Assert.IsTrue(settings.LanguageFontMapping[FontHelpers.DEFAULT_SCRIPT].ContainsKey(FontHelpers.BaseFontFamily.Serif));
            Assert.IsTrue(settings.LanguageFontMapping[FontHelpers.DEFAULT_SCRIPT][FontHelpers.BaseFontFamily.Serif] == "serif");

            Assert.AreEqual(7, settings.AllSupportedScripts.Count);

        }

        [TestMethod]
        public void TestEaxsToEaPdfProcessorWonkySettings()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("App_Extra_BaseFont.config", optional: false, reloadOnChange: false)
                .Build();

            var settings = new EaxsToEaPdfProcessorSettings(config);

            Assert.AreEqual("XResources\\eaxs_to_fo.xsl", settings.XsltFoFilePath);
            Assert.AreEqual("XResources\\eaxs_to_xmp.xsl", settings.XsltXmpFilePath);
            Assert.AreEqual("XResources\\eaxs_to_root_xmp.xsl", settings.XsltRootXmpFilePath);
            Assert.AreEqual("Fonts", settings.FontsFolder);

            Assert.AreEqual(8, settings.LanguageFontMapping.Count);
            Assert.IsTrue(settings.LanguageFontMapping.ContainsKey(FontHelpers.DEFAULT_SCRIPT));
            Assert.IsTrue(settings.LanguageFontMapping[FontHelpers.DEFAULT_SCRIPT].ContainsKey(FontHelpers.BaseFontFamily.Serif));
            Assert.IsTrue(settings.LanguageFontMapping[FontHelpers.DEFAULT_SCRIPT][FontHelpers.BaseFontFamily.Serif] == "serif");

            Assert.AreEqual(7, settings.AllSupportedScripts.Count);

        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void TestEaxsToEaPdfProcessorSettingsBadScript()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("App_bad_script.config", optional: false, reloadOnChange: false)
                .Build();

            var settings = new EaxsToEaPdfProcessorSettings(config);
        }

        [TestMethod]
        [ExpectedException(typeof(System.Exception))]
        public void TestEaxsToEaPdfProcessorSettingsMissingFamilies()
        {
            var config = new ConfigurationBuilder()
                .AddXmlFile("App_Missing_Families.config", optional: false, reloadOnChange: false)
                .Build();

            var settings = new EaxsToEaPdfProcessorSettings(config);
        }

    }
}
