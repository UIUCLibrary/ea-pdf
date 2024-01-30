using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoyT.TrueType;
using System;
using System.Collections.Generic;
using System.Linq;
using UIUCLibrary.EaPdf.Helpers;
using static UIUCLibrary.EaPdf.Helpers.UnicodeScriptDetector;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestHelpers
    {
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

            var text = LATIN_TXT;
            var results = GetUsedScripts(text);
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(LATIN_CD, results[0].ScriptNameShort);
            Assert.AreEqual(1, results[0].Probabilty);

            text = ARABIC_TXT;
            results = GetUsedScripts(text);
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(ARABIC_CD, results[0].ScriptNameShort);
            Assert.AreEqual(1, results[0].Probabilty);

            text = LATIN_TXT + ARABIC_TXT;
            results = GetUsedScripts(text);
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
