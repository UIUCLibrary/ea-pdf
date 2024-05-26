using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Configuration.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoyT.TrueType;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UIUCLibrary.EaPdf.Helpers;
using UIUCLibrary.EaPdf.Helpers.Pdf;
using static UIUCLibrary.EaPdf.Helpers.UnicodeScriptDetector;
using iTextSharp.text.pdf;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestHelpers
    {
        [TestMethod]
        public void TestDPartNode()
        {
            string input = @"
<DPart>
   <DPart>
      <DPart DPM_ContentSetType=""FrontMatter"" Id=""ContentSet_FrontMatter""/>
   </DPart>
   <DPart DPM_FolderName=""short-test"">
      <DPart DPM_EmailMessageID=""000001c503dc$b10d3b50$6601a8c0@FormerClaudia""
              DPM_EmailGUID=""c3538565-606c-4e2a-97ea-e95adfed8f58"">
         <DPart DPM_ContentSetType=""EmailHeaderRendering""
                 Id=""ContentSet_EmailHeaderRendering_1""/>
         <DPart DPM_ContentSetType=""BodyRendering""
                 DPM_Subtype=""text/html""
                 Id=""ContentSet_BodyRendering_text/html_1.0""/>
         <DPart DPM_ContentSetType=""BodyRendering""
                 DPM_Subtype=""text/plain""
                 Id=""ContentSet_BodyRendering_text/plain_1.1""/>
         <DPart DPM_ContentSetType=""BodyRendering""
                 DPM_Subtype=""text/plain""
                 Id=""ContentSet_BodyRendering_text/plain_1.0""/>
      </DPart>
      <DPart DPM_EmailMessageID=""003001c51eb0$8b2bd470$bc01fea9@dvaughn""
              DPM_EmailGUID=""bb4fa448-83f1-4ab5-a5e7-0db567510794"">
         <DPart DPM_ContentSetType=""EmailHeaderRendering""
                 Id=""ContentSet_EmailHeaderRendering_2""/>
         <DPart DPM_ContentSetType=""BodyRendering""
                 DPM_Subtype=""text/html""
                 Id=""ContentSet_BodyRendering_text/html_2.0""/>
         <DPart DPM_ContentSetType=""BodyRendering""
                 DPM_Subtype=""text/plain""
                 Id=""ContentSet_BodyRendering_text/plain_2.0""/>
      </DPart>
   </DPart>
   <DPart>
      <DPart DPM_ContentSetType=""AttachmentList"" Id=""ContentSet_AttachmentList""/>
   </DPart>
</DPart>";

            var root = new DPartNode() { Id = "root" };

            var start = DPartNode.CreateFromXmlString(root, input);

            Assert.AreSame(root, start);

            Assert.AreEqual(9,root.AllLeafNodes.Count);

            Assert.AreSame(root, root.FirstLeafNode.RootNode);

            Assert.AreSame(root.FirstLeafNode, root.AllLeafNodes[0]);

            var nameContentSetType = "ContentSetType";
            var nameFrontMatter = "FrontMatter";

            var first = root.FirstLeafNode;

            Assert.AreEqual("ContentSet_FrontMatter", first.Id);
            Assert.IsTrue(first.Dpm.ContainsKey(nameContentSetType));
            Assert.AreEqual(nameFrontMatter, first.Dpm[nameContentSetType]);

            var afterFirst = first.NextLeafNode;
            Assert.IsNotNull(afterFirst);

            var emailHeaderRendering = root.AllLeafNodes[1];
            Assert.AreEqual("ContentSet_EmailHeaderRendering_1", emailHeaderRendering.Id);

            var emailBodyRendering = root.AllLeafNodes[2];
            Assert.AreEqual("ContentSet_BodyRendering_text/html_1.0", emailBodyRendering.Id);


            var parent = emailHeaderRendering.Parent;
            Assert.IsNotNull(parent);
            Assert.IsTrue(parent.Dpm.ContainsKey("EmailMessageID"));
            Assert.IsTrue(parent.Dpm.ContainsKey("EmailGUID"));
            Assert.AreEqual("000001c503dc$b10d3b50$6601a8c0@FormerClaudia", parent.Dpm["EmailMessageID"]);
            Assert.AreEqual("c3538565-606c-4e2a-97ea-e95adfed8f58", parent.Dpm["EmailGUID"]);

            var nextLeaf = emailHeaderRendering.NextLeafNode;
            Assert.IsNotNull(nextLeaf);

            PrintDPartNode(start, 0);

        }

        private static void PrintDPartNode(DPartNode node, int level)
        {
            Debug.WriteLine($"{new string(' ', level * 2)}{node}");
            foreach (var child in node.DParts)
            {
                PrintDPartNode(child, level + 1);
            }
        }

        [DataRow("json", DisplayName = "appsettings.json")]
        [DataRow("xml", DisplayName = "app.config")]
        [DataRow("json_xml", DisplayName = "appsettings.json and app.config")]
        [DataRow("xml_json", DisplayName = "app.config and appsettings.json")]
        [DataRow("mem", DisplayName = "memory")]
        [DataRow("mem_json", DisplayName = "memory and appsettings.json")]
        [DataRow("json_mem", DisplayName = "appsettings.json and memory")]
        [DataTestMethod]
        public void TestMakeConfigPathAbsolute(string type)
        {
            string startingCurrDir = Directory.GetCurrentDirectory();

            string key = "EaxsToEaPdfProcessorSettings:XsltFoFilePath";
            string val = "XResources\\eaxs_to_fo.xsl";

            var configBldr = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());

            switch (type)
            {
                case "json":
                    configBldr.AddJsonFile("appsettings.json", optional: false);
                    break;
                case "xml":
                    configBldr.AddXmlFile("app.config", optional: false);
                    break;
                case "json_xml":
                    configBldr.AddJsonFile("appsettings.json", optional: false);
                    configBldr.AddXmlFile("app.config", optional: false);
                    break;
                case "xml_json":
                    configBldr.AddXmlFile("app.config", optional: false);
                    configBldr.AddJsonFile("appsettings.json", optional: false);
                    break;
                case "mem":
                    var kvp = new Dictionary<string, string?>
                    {
                        {"test-key-1",  "test-value-1" },
                        { key, val },
                        {"test-key-2", "test-value-2" }
                    };
                    configBldr.AddInMemoryCollection(kvp);
                    break;
                case "mem_json":
                    kvp = new Dictionary<string, string?>
                    {
                        {"test-key-1",  "test-value-1" },
                        { key, val },
                        {"test-key-2", "test-value-2" }
                    };
                    configBldr.AddInMemoryCollection(kvp);
                    configBldr.AddJsonFile("appsettings.json", optional: false);
                    break;
                case "json_mem":
                    kvp = new Dictionary<string, string?>
                    {
                        {"test-key-1",  "test-value-1" },
                        { key, val },
                        {"test-key-2", "test-value-2" }
                    };
                    configBldr.AddJsonFile("appsettings.json", optional: false);
                    configBldr.AddInMemoryCollection(kvp);
                    break;
                default:
                    throw new ArgumentException("Invalid type", nameof(type));
            }

            var config = configBldr.Build();


            var tempDir = Path.GetTempPath();
            Directory.SetCurrentDirectory(tempDir); //set the current directory to the temp directory so can test relative paths

            string? origPath = config[key];
            Assert.IsNotNull(origPath);
            Assert.IsFalse(Path.IsPathRooted(origPath));
            Assert.IsFalse(Path.IsPathFullyQualified(origPath));

            string? absPath = ConfigHelpers.MakeConfigPathAbsolute(config, key);

            Assert.IsNotNull(absPath);
            Assert.AreNotEqual(origPath, absPath);
            Assert.IsTrue(Path.IsPathRooted(absPath));
            Assert.IsTrue(Path.IsPathFullyQualified(absPath));

            if (!type.EndsWith("mem"))
            {
                Assert.IsTrue(absPath.StartsWith(startingCurrDir));
            }
            else
            {
                Assert.IsTrue(absPath.StartsWith(tempDir));
            }

            Assert.AreEqual(absPath, config[key]);

            string? newPath = null;

            var provider = config.Providers.Reverse().FirstOrDefault(p => p.TryGet(key, out newPath));

            switch (type)
            {
                case "json":
                    Assert.IsInstanceOfType(provider, typeof(JsonConfigurationProvider));
                    break;
                case "xml":
                    Assert.IsInstanceOfType(provider, typeof(XmlConfigurationProvider));
                    break;
                case "json_xml":
                    Assert.IsInstanceOfType(provider, typeof(XmlConfigurationProvider));
                    break;
                case "xml_json":
                    Assert.IsInstanceOfType(provider, typeof(JsonConfigurationProvider));
                    break;
                case "mem":
                    Assert.IsInstanceOfType(provider, typeof(MemoryConfigurationProvider));
                    break;
                case "mem_json":
                    Assert.IsInstanceOfType(provider, typeof(JsonConfigurationProvider));
                    break;
                case "json_mem":
                    Assert.IsInstanceOfType(provider, typeof(MemoryConfigurationProvider));
                    break;
                default:
                    throw new ArgumentException("Invalid type", nameof(type));
            }

            Assert.AreEqual(absPath, newPath);

            Directory.SetCurrentDirectory(startingCurrDir); //reset the current directory
        }


        [TestMethod]
        public void TestIsMbox()
        {
            string sampleMboxFile = "D:\\EmailsForTesting\\SampleFiles\\Testing\\MozillaThunderbird\\inbox";
            string sampleMboxSpFile = "D:\\EmailsForTesting\\SampleFiles\\Testing\\MozillaThunderbird\\Inbox_leading_whitespaces";
            string sampleMbxFile = "D:\\EmailsForTesting\\SampleFiles\\Testing\\Pine\\sent-mail-sep-2006";
            string sampleEmlFile = "D:\\EmailsForTesting\\SampleFiles\\Testing\\Non-Western\\Arabic\\FW Arabic email testing.eml";
            string sampleEmlSpFile = "D:\\EmailsForTesting\\SampleFiles\\Testing\\Gmail\\leading_white_space.eml";

            Assert.IsTrue(MimeKitHelpers.IsMboxFile(sampleMboxFile, out string leader1));
            Assert.IsTrue(string.IsNullOrEmpty(leader1));
            Assert.IsTrue(MimeKitHelpers.IsMboxFile(sampleMboxSpFile, out string leader2)); //leading white space is a valid mbox file
            Assert.IsFalse(string.IsNullOrEmpty(leader2));
            Assert.IsFalse(MimeKitHelpers.IsMboxFile(sampleMbxFile, out string leader3));
            Assert.IsTrue(string.IsNullOrEmpty(leader3));
            Assert.IsFalse(MimeKitHelpers.IsMboxFile(sampleEmlFile, out string leader4));
            Assert.IsTrue(string.IsNullOrEmpty(leader4));
            Assert.IsFalse(MimeKitHelpers.IsMboxFile(sampleEmlSpFile, out string leader5));
            Assert.IsTrue(string.IsNullOrEmpty(leader5));

            Assert.IsFalse(MimeKitHelpers.IsEmlFile(sampleMboxFile));
            Assert.IsFalse(MimeKitHelpers.IsEmlFile(sampleMboxSpFile));
            Assert.IsFalse(MimeKitHelpers.IsEmlFile(sampleMbxFile));
            Assert.IsTrue(MimeKitHelpers.IsEmlFile(sampleEmlFile));
            Assert.IsFalse(MimeKitHelpers.IsEmlFile(sampleEmlSpFile));  //leading white space is not a valid eml file

            Assert.IsFalse(MimeKitHelpers.IsPineMbxFile(sampleMboxFile));
            Assert.IsFalse(MimeKitHelpers.IsPineMbxFile(sampleMboxSpFile));
            Assert.IsTrue(MimeKitHelpers.IsPineMbxFile(sampleMbxFile));
            Assert.IsFalse(MimeKitHelpers.IsPineMbxFile(sampleEmlFile));
            Assert.IsFalse(MimeKitHelpers.IsPineMbxFile(sampleEmlSpFile));

            var typ1 = MimeKitHelpers.DetermineInputType(sampleMboxFile, out string leadin1);
            Assert.IsTrue(string.IsNullOrEmpty(leadin1));
            Assert.AreEqual(InputType.MboxFile, typ1);
            var typ2 = MimeKitHelpers.DetermineInputType(sampleMboxSpFile, out string leadin2);
            Assert.IsFalse(string.IsNullOrEmpty(leadin2));
            Assert.AreEqual(InputType.MboxFile, typ2);
            var typ3 = MimeKitHelpers.DetermineInputType(sampleMbxFile, out string leadin3);
            Assert.IsTrue(string.IsNullOrEmpty(leadin3));
            Assert.AreEqual(InputType.MboxFile, typ3);
            var typ4 = MimeKitHelpers.DetermineInputType(sampleEmlFile, out string leadin4);
            Assert.IsTrue(string.IsNullOrEmpty(leadin4));
            Assert.AreEqual(InputType.EmlFile, typ4);
            var typ5 = MimeKitHelpers.DetermineInputType(sampleEmlSpFile, out string leadin5);
            Assert.IsTrue(string.IsNullOrEmpty(leadin5));
            Assert.AreEqual(InputType.UnknownFile, typ5); //leading white space is not a valid eml file

            var typ1d = MimeKitHelpers.DetermineInputType(Path.GetDirectoryName(sampleMboxFile) ?? ".", out string leadin1d);
            Assert.IsTrue(string.IsNullOrEmpty(leadin1d));
            Assert.AreEqual(InputType.MboxFolder, typ1d);
            var typ2d = MimeKitHelpers.DetermineInputType(Path.GetDirectoryName(sampleMboxSpFile) ?? ".", out string leadin2d);
            Assert.IsTrue(string.IsNullOrEmpty(leadin2d));
            Assert.AreEqual(InputType.MboxFolder, typ2d);
            var typ3d = MimeKitHelpers.DetermineInputType(Path.GetDirectoryName(sampleMbxFile) ?? ".", out string leadin3d);
            Assert.IsTrue(string.IsNullOrEmpty(leadin3d));
            Assert.AreEqual(InputType.MboxFolder, typ3d);
            var typ4d = MimeKitHelpers.DetermineInputType(Path.GetDirectoryName(sampleEmlFile) ?? ".", out string leadin4d);
            Assert.IsTrue(string.IsNullOrEmpty(leadin4d));
            Assert.AreEqual(InputType.EmlFolder, typ4d);
            var typ5d = MimeKitHelpers.DetermineInputType(Path.GetDirectoryName(sampleEmlSpFile) ?? ".", out string leadin5d);
            Assert.IsTrue(string.IsNullOrEmpty(leadin5d));
            Assert.AreEqual(InputType.MboxFolder, typ5d); //leading white space is not a valid eml file, but the folder contains other valid mbox files

            var typ1s = MimeKitHelpers.DetermineInputType(sampleMboxFile, true, out string leadin1s);
            Assert.IsTrue(string.IsNullOrEmpty(leadin1s));
            Assert.AreEqual(InputType.MboxFile, typ1s);
            var typ2s = MimeKitHelpers.DetermineInputType(sampleMboxSpFile, true, out string leadin2s);
            Assert.IsFalse(string.IsNullOrEmpty(leadin2s));
            Assert.AreEqual(InputType.MboxFile, typ2s);
            var typ3s = MimeKitHelpers.DetermineInputType(sampleMbxFile, true, out string leadin3s);
            Assert.IsTrue(string.IsNullOrEmpty(leadin3s));
            Assert.AreEqual(InputType.MboxFile, typ3s);
            var typ4s = MimeKitHelpers.DetermineInputType(sampleEmlFile, true, out string leadin4s);
            Assert.IsTrue(string.IsNullOrEmpty(leadin4s));
            Assert.AreEqual(InputType.EmlFile, typ4s);
            var typ5s = MimeKitHelpers.DetermineInputType(sampleEmlSpFile, true, out string leadin5s);
            Assert.IsTrue(string.IsNullOrEmpty(leadin5s));
            Assert.AreEqual(InputType.UnknownFile, typ5s); //leading white space is not a valid eml file

            var typ1ds = MimeKitHelpers.DetermineInputType(Path.GetDirectoryName(sampleMboxFile) ?? ".", true, out string leadin1ds);
            Assert.IsTrue(string.IsNullOrEmpty(leadin1ds));
            Assert.AreEqual(InputType.MboxFolder, typ1ds);
            var typ2ds = MimeKitHelpers.DetermineInputType(Path.GetDirectoryName(sampleMboxSpFile) ?? ".", true, out string leadin2ds);
            Assert.IsTrue(string.IsNullOrEmpty(leadin2ds));
            Assert.AreEqual(InputType.MboxFolder, typ2ds);
            var typ3ds = MimeKitHelpers.DetermineInputType(Path.GetDirectoryName(sampleMbxFile) ?? ".", true, out string leadin3ds);
            Assert.IsTrue(string.IsNullOrEmpty(leadin3ds));
            Assert.AreEqual(InputType.MboxFolder, typ3ds);
            var typ4ds = MimeKitHelpers.DetermineInputType(Path.GetDirectoryName(sampleEmlFile) ?? ".", true, out string leadin4ds);
            Assert.IsTrue(string.IsNullOrEmpty(leadin4ds));
            Assert.AreEqual(InputType.EmlFolder, typ4ds);
            var typ5ds = MimeKitHelpers.DetermineInputType(Path.GetDirectoryName(sampleEmlSpFile) ?? ".", true, out string leadin5ds);
            Assert.IsTrue(string.IsNullOrEmpty(leadin5ds));
            Assert.AreEqual(InputType.MixedFolder, typ5ds); //mixed folder because the Gmail folder contains both mbox and eml files
        }

        [TestMethod]
        public void TestGetFilePathWithIncrementNumber()
        {
            string path = "C:\\temp\\test.txt";
            int increment = 2;
            string expected = "C:\\temp\\test_0002.txt";
            string actual = FilePathHelpers.GetFilePathWithIncrementNumber(path, increment);
            Assert.AreEqual(expected, actual);

            path = "test.txt";
            increment = 3;
            expected = "test_0003.txt";
            actual = FilePathHelpers.GetFilePathWithIncrementNumber(path, increment);
            Assert.AreEqual(expected, actual);

        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TestGetFilePathWithIncrementNumberException()
        {
            string path = "C:\\temp\\test.txt";
            int increment = 10001; //number too large
            string expected = "C:\\temp\\test_10001.txt";
            string actual = FilePathHelpers.GetFilePathWithIncrementNumber(path, increment);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestGetFilePathWithoutIncrementNumber()
        {
            string path = "C:\\temp\\test_0002.txt";
            string expected = "C:\\temp\\test.txt";
            string actual = FilePathHelpers.GetFilePathWithoutIncrementNumber(path);
            Assert.AreEqual(expected, actual);

            path = "test_0003.txt";
            expected = "test.txt";
            actual = FilePathHelpers.GetFilePathWithoutIncrementNumber(path);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TestGetFilePathWithoutIncrementNumberException()
        {
            string path = "C:\\temp\\test.txt"; //no increment number
            string expected = "C:\\temp\\test.txt";
            string actual = FilePathHelpers.GetFilePathWithoutIncrementNumber(path);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TestGetFilePathWithoutIncrementNumberException2()
        {
            string path = "C:\\temp\\test_1.txt"; //increment number does not have leading zeros
            string expected = "C:\\temp\\test.txt";
            string actual = FilePathHelpers.GetFilePathWithoutIncrementNumber(path);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TestGetFilePathWithoutIncrementNumberException3()
        {
            string path = "C:\\temp\\test_11111.txt"; //increment number too large
            string expected = "C:\\temp\\test.txt";
            string actual = FilePathHelpers.GetFilePathWithoutIncrementNumber(path);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestTryGetFilePathWithoutIncrementNumber()
        {
            string path = "C:\\temp\\test_0002.txt";
            string expectedPath = "C:\\temp\\test.txt";
            int expectedIncr = 2;
            int actualIncr = FilePathHelpers.TryGetFilePathWithoutIncrementNumber(path, out string actualPath);
            Assert.AreEqual(expectedIncr, actualIncr);
            Assert.AreEqual(expectedPath, actualPath);


            path = "test_0003.txt";
            expectedPath = "test.txt";
            expectedIncr = 3;
            actualIncr = FilePathHelpers.TryGetFilePathWithoutIncrementNumber(path, out actualPath);
            Assert.AreEqual(expectedIncr, actualIncr);
            Assert.AreEqual(expectedPath, actualPath);

            path = "test.txt";
            expectedPath = "test.txt";
            expectedIncr = 0;
            actualIncr = FilePathHelpers.TryGetFilePathWithoutIncrementNumber(path, out actualPath);
            Assert.AreEqual(expectedIncr, actualIncr);
            Assert.AreEqual(expectedPath, actualPath);

            path = "test_99999.txt"; //increment number too large
            expectedPath = "test_99999.txt";
            expectedIncr = 0;
            actualIncr = FilePathHelpers.TryGetFilePathWithoutIncrementNumber(path, out actualPath);
            Assert.AreEqual(expectedIncr, actualIncr);
            Assert.AreEqual(expectedPath, actualPath);

            path = "test_6.txt"; //increment number missing leading zeros
            expectedPath = "test_6.txt";
            expectedIncr = 0;
            actualIncr = FilePathHelpers.TryGetFilePathWithoutIncrementNumber(path, out actualPath);
            Assert.AreEqual(expectedIncr, actualIncr);
            Assert.AreEqual(expectedPath, actualPath);

        }

        [TestMethod]
        public void TestFontContainsCharacter()
        {
            var ttfFile = "C:\\WINDOWS\\FONTS\\ARIBLK.TTF"; //Arial Black

            var ttf = TrueTypeFont.FromFile(ttfFile);
            Assert.IsNotNull(ttf);

            char latin_a = 'a';
            Assert.IsTrue(FontHelpers.FontContainsCharacter(ttf, latin_a));

            char arabic_comma = '\x060C'; //not in the Arial Black font
            Assert.IsFalse(FontHelpers.FontContainsCharacter(ttf, arabic_comma));
        }

        [TestMethod]
        public void TestGetUsedScripts()
        {
            const string LATIN_CD = "Latn";
            const string LATIN_TXT = "Latin";

            const string ARABIC_CD = "Arab";
            const string ARABIC_TXT = "العربية";
            //const string ADLAM_CD = "Adlm";

            //const string SP2 = "  ";

            //const string ARAB_COMMA = "\x060C"; //this character has the common (Zyyy) script property,
            //but it has extended properties and applies to just the "Arab", "Rohg", "Syrc", or "Thaa" scripts (note: arab is first in the list)

            //const string ARAB_TATWEEL = "\x0640"; //this character has the common (Zyyy) script property,
            //but it has extended properties and applies to just the "Adlm", "Arab", "Mand", "Mani", "Phlp", "Rohg", "Sogd", "Syrc" scripts (note: arab is second in the list)

            List<(LogLevel level, string message)> messages = new();
            var text = LATIN_TXT;
            var results = GetUsedScripts(text, ref messages);
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(LATIN_CD, results[0].ScriptNameShort);
            Assert.AreEqual(1, results[0].Probabilty);

            text = ARABIC_TXT;
            results = GetUsedScripts(text, ref messages);
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(ARABIC_CD, results[0].ScriptNameShort);
            Assert.AreEqual(1, results[0].Probabilty);

            text = LATIN_TXT + ARABIC_TXT;
            results = GetUsedScripts(text, ref messages);
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(ARABIC_CD, results[0].ScriptNameShort); //this is first because there are more arabic characters than latin characters
            Assert.AreEqual((float)ARABIC_TXT.Length / text.Length, results[0].Probabilty);
            Assert.AreEqual(LATIN_CD, results[1].ScriptNameShort);
            Assert.AreEqual((float)LATIN_TXT.Length / text.Length, results[1].Probabilty);

        }

        [TestMethod]
        public void TestUnicodeScriptsExtended()
        {

            const string LATIN_CD = "Latn";
            const string LATIN_TXT = "Latin";

            const string ARABIC_CD = "Arab";
            const string ARABIC_TXT = "العربية";
            const string ADLAM_CD = "Adlm";

            const string SP2 = "  ";

            const string ARAB_COMMA = "\x060C"; //this character has the common (Zyyy) script property,
                                                //but it has extended properties and applies to just the "Arab", "Rohg", "Syrc", or "Thaa" scripts (note: arab is first in the list)

            const string ARAB_TATWEEL = "\x0640"; //this character has the common (Zyyy) script property,
                                                  //but it has extended properties and applies to just the "Adlm", "Arab", "Mand", "Mani", "Phlp", "Rohg", "Sogd", "Syrc" scripts (note: arab is second in the list)

            int codePoint = char.ConvertToUtf32(ARAB_COMMA, 0);

            var cps = GetCodepointScripts().SingleOrDefault(cs => cs.RangeStart <= codePoint && cs.RangeEnd >= codePoint);
            var cpsExt = GetCodepointScriptsExtended().SingleOrDefault(cs => cs.RangeStart <= codePoint && cs.RangeEnd >= codePoint);

            Assert.IsNotNull(cps);
            Assert.IsNotNull(cpsExt);

            Assert.AreEqual(ScriptType.Common, cps.Script.Type);
            Assert.IsTrue(cpsExt.ScriptNamesShort.Contains("Arab"));


            var test = ARAB_COMMA;
            var offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out List<(LogLevel, string)> messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(1, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual(1, offsets[0].range.End);
            Assert.AreEqual(ARABIC_CD, offsets[0].scriptName);

            test = LATIN_TXT + ARAB_COMMA;
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(2, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual(LATIN_TXT.Length, offsets[0].range.End);
            Assert.AreEqual(LATIN_CD, offsets[0].scriptName);
            Assert.AreEqual(LATIN_TXT.Length, offsets[1].range.Start);
            Assert.AreEqual(test.Length, offsets[1].range.End);
            Assert.AreEqual(ARABIC_CD, offsets[1].scriptName); //should be Arab because arab is the first script in the list of extended properties

            test = LATIN_TXT + ARAB_TATWEEL;
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(2, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual(LATIN_TXT.Length, offsets[0].range.End);
            Assert.AreEqual(LATIN_CD, offsets[0].scriptName);
            Assert.AreEqual(LATIN_TXT.Length, offsets[1].range.Start);
            Assert.AreEqual(test.Length, offsets[1].range.End);
            Assert.AreEqual(ADLAM_CD, offsets[1].scriptName); //should be Adlm because Adlam is the first script in the list of extended properties

            test = ARABIC_TXT + ARAB_COMMA;
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(1, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual(test.Length, offsets[0].range.End);
            Assert.AreEqual(ARABIC_CD, offsets[0].scriptName);

            test = ARABIC_TXT + ARAB_TATWEEL;
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(1, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual(test.Length, offsets[0].range.End);
            Assert.AreEqual(ARABIC_CD, offsets[0].scriptName);

            test = ARAB_COMMA + ARABIC_TXT;
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(1, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual(test.Length, offsets[0].range.End);
            Assert.AreEqual(ARABIC_CD, offsets[0].scriptName);

            test = ARAB_TATWEEL + ARABIC_TXT;
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(1, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual(test.Length, offsets[0].range.End);
            Assert.AreEqual(ARABIC_CD, offsets[0].scriptName);

            test = ARAB_TATWEEL + ARABIC_TXT + ARAB_COMMA;
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(1, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual(test.Length, offsets[0].range.End);
            Assert.AreEqual(ARABIC_CD, offsets[0].scriptName);

            test = ARAB_TATWEEL + ARABIC_TXT + ARAB_COMMA + SP2 + ARABIC_TXT;
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(1, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual(test.Length, offsets[0].range.End);
            Assert.AreEqual(ARABIC_CD, offsets[0].scriptName);

            test = SP2 + ARAB_TATWEEL + ARABIC_TXT + ARAB_COMMA + SP2 + ARABIC_TXT + SP2;
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(1, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual(test.Length, offsets[0].range.End);
            Assert.AreEqual(ARABIC_CD, offsets[0].scriptName);

            test = SP2 + LATIN_TXT + SP2 + ARAB_TATWEEL + ARABIC_TXT + ARAB_COMMA + SP2 + ARABIC_TXT + SP2;
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(2, offsets.Count);
            Assert.AreEqual(0, offsets[0].range.Start);
            Assert.AreEqual((SP2 + LATIN_TXT + SP2).Length, offsets[0].range.End);
            Assert.AreEqual(LATIN_CD, offsets[0].scriptName);
            Assert.AreEqual((SP2 + LATIN_TXT + SP2).Length, offsets[1].range.Start);
            Assert.AreEqual(test.Length, offsets[1].range.End);
            Assert.AreEqual(ARABIC_CD, offsets[1].scriptName);

        }

        [TestMethod]
        public void TestUnicodeGetUsedScriptOffsets()
        {
            const string LATIN_CD = "Latn";
            const string LATIN_TXT = "Latin";

            const string ARABIC_CD = "Arab";
            const string ARABIC_TXT = "العربية";

            const string SP2 = "  ";
            const string NUM12 = "12";
            const string CRLF = "\r\n";

            List<string> commons = new()
            {
                SP2, NUM12, CRLF, SP2 + CRLF, SP2 + NUM12, NUM12 + SP2, NUM12 + CRLF, NUM12 + SP2 + CRLF
            };

            //emtpy strings 
            string? test = null;  //null string
            var offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out List<(LogLevel, string)> messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(0, offsets.Count);

            test = ""; //empty string
            offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
            Assert.IsNotNull(messages);
            Assert.AreEqual(0, messages.Count);
            Assert.IsNotNull(offsets);
            Assert.AreEqual(0, offsets.Count);

            foreach (var common in commons) //test with different common chars
            {
                test = common; //simple string all common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(UnicodeScriptDetector.ScriptShortCommon, offsets[0].scriptName);

                string text = LATIN_TXT;
                string script = LATIN_CD;

                //Latin text
                test = text; //simple latin string
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                test = common + text; //simple latin string with leading common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                test = text + common; //simple latin string with trailing common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                test = common + text + common; //simple latin string with leading and trailing common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                test = text + common + text; //simple latin string with internal common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                test = common + text + common + text; //simple latin string with leading and internal common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);


                test = text + common + text + common; //simple latin string with trailing and internal common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);


                test = common + text + common + text + common; //simple latin string with leading, trailing and internal common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                text = ARABIC_TXT;
                script = ARABIC_CD;

                //Latin text
                test = text; //simple latin string
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                test = common + text; //simple latin string with leading common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                test = text + common; //simple latin string with trailing common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                test = common + text + common; //simple latin string with leading and trailing common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                test = text + common + text; //simple latin string with internal common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                test = common + text + common + text; //simple latin string with leading and internal common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);


                test = text + common + text + common; //simple latin string with trailing and internal common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);


                test = common + text + common + text + common; //simple latin string with leading, trailing and internal common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(1, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(test.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);

                //Mixed latin and arabic

                text = LATIN_TXT;
                string text2 = ARABIC_TXT;
                script = LATIN_CD;
                string script2 = ARABIC_CD;

                test = text + text2; //latin and arabic, no common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(2, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(text.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);
                Assert.AreEqual(text.Length, offsets[1].range.Start);
                Assert.AreEqual(test.Length, offsets[1].range.End);
                Assert.AreEqual(script2, offsets[1].scriptName);


                test = text2 + text; //arabic and latin, no common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(2, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(text2.Length, offsets[0].range.End);
                Assert.AreEqual(script2, offsets[0].scriptName);
                Assert.AreEqual(text2.Length, offsets[1].range.Start);
                Assert.AreEqual(test.Length, offsets[1].range.End);
                Assert.AreEqual(script, offsets[1].scriptName);

                test = text + common + text2; //latin and arabic separated by common chars, separating common chars attached to latin
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(2, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(text.Length + common.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);
                Assert.AreEqual(text.Length + common.Length, offsets[1].range.Start);
                Assert.AreEqual(test.Length, offsets[1].range.End);
                Assert.AreEqual(script2, offsets[1].scriptName);

                test = common + text + common + text2; //latin and arabic with leading common chars and separated by common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(2, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(text.Length + common.Length * 2, offsets[0].range.End); //the leading and separating common chars are attached to the latin
                Assert.AreEqual(script, offsets[0].scriptName);
                Assert.AreEqual(text.Length + common.Length * 2, offsets[1].range.Start);
                Assert.AreEqual(test.Length, offsets[1].range.End);
                Assert.AreEqual(script2, offsets[1].scriptName);

                test = text + common + text2 + common; //latin and arabic with trailing common chars and separated by common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(2, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(text.Length + common.Length, offsets[0].range.End);
                Assert.AreEqual(script, offsets[0].scriptName);
                Assert.AreEqual(text.Length + common.Length, offsets[1].range.Start);
                Assert.AreEqual(test.Length, offsets[1].range.End); //trailing common chars attached to arabic
                Assert.AreEqual(script2, offsets[1].scriptName);

                test = common + text + common + text2 + common; //latin and arabic with leading and trailing common chars and separated by common chars
                offsets = UnicodeHelpers.PartitionTextByUnicodeScript(test, out messages);
                Assert.IsNotNull(messages);
                Assert.AreEqual(0, messages.Count);
                Assert.IsNotNull(offsets);
                Assert.AreEqual(2, offsets.Count);
                Assert.AreEqual(0, offsets[0].range.Start);
                Assert.AreEqual(text.Length + common.Length * 2, offsets[0].range.End); //leadings and separating common chars attached to latin   
                Assert.AreEqual(script, offsets[0].scriptName);
                Assert.AreEqual(text.Length + common.Length * 2, offsets[1].range.Start);
                Assert.AreEqual(test.Length, offsets[1].range.End); //trailing common chars attached to arabic
                Assert.AreEqual(script2, offsets[1].scriptName);
            }
        }
    }
}
