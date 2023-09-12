using Microsoft.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using static UIUCLibrary.EaPdf.Helpers.UnicodeScriptDetector;


namespace UIUCLibrary.EaPdf.Helpers
{

    /// <summary>
    /// Derived from https://github.com/DaniRK/UnicodeScriptDetectorNet
    /// License: MIT https://github.com/DaniRK/UnicodeScriptDetectorNet/blob/master/LICENSE.md
    /// </summary>
    static public partial class UnicodeScriptDetector
    {
        public enum ScriptType { Normal, Common, Inherited, Unknown };

        public static ScriptType? GetScriptType(string name)
        {
            switch (name)
            {
                case ScriptLongCommon:
                case ScriptShortCommon:
                    return ScriptType.Common;
                case ScriptLongInherited:
                case ScriptShortInherited:
                    return ScriptType.Inherited;
                case ScriptLongUnknown:
                case ScriptShortUnknown:
                    return ScriptType.Unknown;
                default: 
                    return ScriptType.Normal;
            }
        }

        /// <summary>
        /// One of the writing scripts defined in Unicode
        /// </summary>
        public class Script
        {
            public string ShortName { get; init; } = ScriptShortUnknown;
            public string LongName { get; init; } = ScriptLongUnknown;
            public ScriptType Type { get; init; } = ScriptType.Unknown;
            public int TempIndex { get; init; } = -1; // internal index. Can change when importing newer version of Unicode data!!! Don't use this value in persistent data!

            public Script GetCopy()
            {
                Script ret = new()
                {
                    ShortName = this.ShortName,
                    LongName = this.LongName,
                    Type = this.Type,
                    TempIndex = this.TempIndex
                };
                return ret;
            }
        }

        /// <summary>
        /// A range of codepoints belonging to the same Script
        /// </summary>
        [Serializable]
        public class CodepointScript
        {
            public int RangeStart { get; init; }
            public int RangeEnd { get; init; }
            public Script Script { get; init; } = new();

        }

        /// <summary>
        /// A range of codepoints with the same list of scripts found in codepoint extended properties  
        /// </summary>
        [Serializable]
        public class CodepointScriptExtended
        {
            public int RangeStart { get; init; }
            public int RangeEnd { get; init; }
            public string[] ScriptNamesShort { get; init; } = Array.Empty<string>();

        }

        /// <summary>
        /// One of the results returned by detection methods
        /// </summary>
        public class Result
        {
            public string? ScriptNameShort;
            public string? ScriptNameLong;
            public float Probabilty;
        }

        public class Results : List<Result> { }

        // short/long  names for special Script names 

        public const string ScriptShortInherited = "Zinh"; // Inherited
        public const string ScriptShortCommon = "Zyyy";    // Common
        public const string ScriptShortUnknown = "Zzzz";   // Unknown

        public const string ScriptLongInherited = "Inherited"; // Inherited
        public const string ScriptLongCommon = "Common";    // Common
        public const string ScriptLongUnknown = "Unknown";   // Unknown

        /// <summary>
        /// Return a list of possible "writing scripts" (like 'Latin', 'Arabic') that might have been used to write the specified text, together with a probablity for each
        /// Multiple scripts may be returned if a text either is composed of mixed scripts OR if only codePoints where used that belong
        /// to multiple scripts.
        /// An empty list will be returned if the string is null or empty or if no Script at all could be detected (such as "123," which only contains 'common' codepoints)
        /// 
        /// </summary>
        /// <param name="testText">Text to evaluate</param>
        /// <param name="ignoreInherited">If true: special characters that inherit their Script from the preceeding character are not counted</param>
        /// <returns>Result list</returns>
        static public Results GetUsedScripts(string testText, bool ignoreInherited = true /*, bool useExtendedProperties = false*/ )
        {
            // for logic and technical details, see http://www.unicode.org/reports/tr24/

            if (testText == null || testText.Length == 1)
                return new Results();

            int[] buckets = new int[Scripts.Length];
            int totalRelevantCharacters = 0;

            CodepointScript? lastCps = null; // for inheritance

            //foreach (char c in testText)
            for (int charIndex = 0; charIndex < testText.Length; charIndex++)
            {
                //var codePoint = Convert.ToInt32(c);
                // .net/windows hold characters as utf16. Unicode codepoints > 0xffff are represented as 
                // two characters (using surrogates), therefor we cannot just loop through the characters and use their always 16 bit numeric value
                // (string length property grows accordingly)

                int codePoint = char.ConvertToUtf32(testText, charIndex);
                if (codePoint > 0xffff)
                    charIndex++;

                var cps = CodepointScripts.Where(cs => cs.RangeStart <= codePoint && cs.RangeEnd >= codePoint).FirstOrDefault();

                if (cps == null) // not in table means implicitely ScriptShortUnknown
                    continue;

                if (cps.Script.Type == ScriptType.Unknown) // explicitly set to ScriptShortUnknown
                    continue;

                if (cps.Script.Type == ScriptType.Common)
                    continue;

                if (cps.Script.Type == ScriptType.Inherited)
                {
                    if (ignoreInherited)
                        continue;

                    if (lastCps == null) // should not happen in real written text
                        continue;
                    else
                        cps = lastCps;
                }

                lastCps = cps;
                totalRelevantCharacters++;

                buckets[cps.Script.TempIndex]++;
            }

            Results results = new Results();

            for (int i = 0; i < buckets.Length; i++)
            {
                var bucket = buckets[i];
                if (bucket > 0)
                {
                    float p = (float)bucket / totalRelevantCharacters;
                    var scriptName = Scripts.Where(sn => sn.TempIndex == i).First();

                    Console.WriteLine($"Script {scriptName.LongName}: {p:P0}");

                    results.Add(new Result
                    {
                        ScriptNameShort = scriptName.ShortName,
                        ScriptNameLong = scriptName.LongName,
                        Probabilty = p
                    });
                }
            }

            results.Sort((item1, item2) => item2.Probabilty.CompareTo(item1.Probabilty)); // reverse sort, highest probability first

            return results;
        }

        /// <summary>
        /// Returns the percentage (0.0 to 1.0) of characters in a string, that can be written in a specified Script name.
        /// Common characters (e.g. digits, space) will pass the test for any Script, unless strict is set to true.
        /// Null or empty strings will always return 1
        /// </summary>
        /// <param name="testText">Text to evaluate</param>
        /// <param name="scriptName">Short or long Script name (e.g.'Latn', 'Latin')</param>
        /// <param name="strict">Common characters are not counted as belonging to the Script</param>
        /// <param name="applyExtendedProperties">
        /// If a common character has extended properties limiting it to a list of scripts and none of them matches 
        /// the scriptName parm, then the common character does not match. Ignored if strict parm is true  
        /// </param>
        /// <returns>value between 0.0 and 1.0, 1.0 for full fit</returns>
        /// <exception cref="System.ArgumentException">Thrown when an invalid scriptName is passed</exception>
        static public float ProbablyInScript(string testText, string scriptName, bool strict = false, bool applyExtendedProperties = true)
        {
            // The main difference to GetUsedScripts is in case of a text built only with common code points:
            // here we would return 1.0 for ANY specified scriptName, while GetUsedScripts() would return an empty result set (since it
            // no specific Script can be detected)

            if (testText == null || testText.Length == 0)
                return 1;

            Script? sn = Scripts.Where(n => n.ShortName.ToLower() == scriptName.ToLower() || n.LongName.ToLower() == scriptName.ToLower()).FirstOrDefault();

            if (sn == null)
                throw new ArgumentException("Invalid short or long scriptName supplied", scriptName);

            string ShortName = sn.ShortName;


            // for logic and technical details, see http://www.unicode.org/reports/tr24/

            int inScriptCount = 0;

            CodepointScript? lastCps = null; // for inheritance

            //foreach (char c in testText)
            for (int charIndex = 0; charIndex < testText.Length; charIndex++)
            {

                // .net/windows hold characters as utf16. Unicode codepoints > 0xffff are represented as 
                // two characters (using surrogates), therefor we cannot just loop through the characters and use their numeric value
                // (string length property grows accordingly)

                int codePoint = char.ConvertToUtf32(testText, charIndex);
                if (codePoint > 0xffff)
                    charIndex++;

                var cps = CodepointScripts.Where(cs => cs.RangeStart <= codePoint && cs.RangeEnd >= codePoint).FirstOrDefault();

                if (cps == null) // not in table, implicitely Unknown and therefore not in Script, this is a mismatch
                    continue;

                if (cps.Script.Type == ScriptType.Unknown) // implicitely Unknown, not in Script, this is a mismatch
                    continue;

                if (cps.Script.Type == ScriptType.Common)
                {
                    // most common code points can be used in any Script, so this is a match, unless strict parm is set to true
                    // but some common code point have extended scripts property, which says in which limited set of scripts the common might be used

                    if (strict)
                        continue; // not a match

                    if (applyExtendedProperties)
                    {
                        var cpsExtended = CodepointScriptsExtended.Where(cs => cs.RangeStart <= codePoint && cs.RangeEnd >= codePoint).FirstOrDefault();

                        if (cpsExtended != null && !cpsExtended.ScriptNamesShort.Contains(ShortName))
                            continue; // not a match
                    }

                    inScriptCount++; //match
                    continue;
                }

                if (cps.Script.Type == ScriptType.Inherited)
                {
                    // inherit from preceeding character

                    if (lastCps == null) // inherited as first char should not happen in real written text, see this as a mismatch 
                        continue;
                    else
                    {
                        // though there are a few cases of inherited chars with extended properties, this is 
                        // meaningless in the context of this method

                        cps = lastCps;
                    }
                }

                if (cps.Script.ShortName == ShortName)
                    inScriptCount++;

                lastCps = cps;
            }

            return (float)inScriptCount / testText.Length;
        }

        /// <summary>
        /// Returns true if all characters in a string can be written in a specified Script name.
        /// Common characters (e.g. digits, space) will pass the test for any Script, unless strict is set to true.
        /// Null or empty strings will always yield 0
        /// </summary>
        /// <param name="testText">Text to evaluate</param>
        /// <param name="scriptName">Short or long Script name (e.g.'Latn', 'Latin')</param>
        /// <param name="strict">Common characters are not counted as belonging to the Script</param>
        /// <param name="applyExtendedProperties">If a common character has extended properties limiting it to a list of scripts and none of them matches the scriptName parm, then it does not match. Ignored if strict parm is true  </param>
        static public bool IsInScript(string testText, string scriptName, bool strict = false, bool applyExtendedProperties = true)
        {
            return ProbablyInScript(testText, scriptName, strict, applyExtendedProperties) == 1.0;
        }

        public static IList<Script> GetScripts() => Array.AsReadOnly(Scripts);
        public static IList<CodepointScript> GetCodepointScripts() => Array.AsReadOnly(CodepointScripts);
        public static IList<CodepointScriptExtended> GetCodepointScriptsExtended() => Array.AsReadOnly(CodepointScriptsExtended);

        /// <summary>
        /// Derived from Unicode data, see https://github.com/DaniRK/UnicodeScriptDetectorNet/tree/master/ImportDataToCSharp
        /// </summary>
        private static readonly Script[] Scripts = new Script[]
        {

                new Script {ShortName = "Adlm", LongName = "Adlam", TempIndex = 0, Type = ScriptType.Normal},
                new Script {ShortName = "Aghb", LongName = "Caucasian_Albanian", TempIndex = 1, Type = ScriptType.Normal},
                new Script {ShortName = "Ahom", LongName = "Ahom", TempIndex = 2, Type = ScriptType.Normal},
                new Script {ShortName = "Arab", LongName = "Arabic", TempIndex = 3, Type = ScriptType.Normal},
                new Script {ShortName = "Armi", LongName = "Imperial_Aramaic", TempIndex = 4, Type = ScriptType.Normal},
                new Script {ShortName = "Armn", LongName = "Armenian", TempIndex = 5, Type = ScriptType.Normal},
                new Script {ShortName = "Avst", LongName = "Avestan", TempIndex = 6, Type = ScriptType.Normal},
                new Script {ShortName = "Bali", LongName = "Balinese", TempIndex = 7, Type = ScriptType.Normal},
                new Script {ShortName = "Bamu", LongName = "Bamum", TempIndex = 8, Type = ScriptType.Normal},
                new Script {ShortName = "Bass", LongName = "Bassa_Vah", TempIndex = 9, Type = ScriptType.Normal},
                new Script {ShortName = "Batk", LongName = "Batak", TempIndex = 10, Type = ScriptType.Normal},
                new Script {ShortName = "Beng", LongName = "Bengali", TempIndex = 11, Type = ScriptType.Normal},
                new Script {ShortName = "Bhks", LongName = "Bhaiksuki", TempIndex = 12, Type = ScriptType.Normal},
                new Script {ShortName = "Bopo", LongName = "Bopomofo", TempIndex = 13, Type = ScriptType.Normal},
                new Script {ShortName = "Brah", LongName = "Brahmi", TempIndex = 14, Type = ScriptType.Normal},
                new Script {ShortName = "Brai", LongName = "Braille", TempIndex = 15, Type = ScriptType.Normal},
                new Script {ShortName = "Bugi", LongName = "Buginese", TempIndex = 16, Type = ScriptType.Normal},
                new Script {ShortName = "Buhd", LongName = "Buhid", TempIndex = 17, Type = ScriptType.Normal},
                new Script {ShortName = "Cakm", LongName = "Chakma", TempIndex = 18, Type = ScriptType.Normal},
                new Script {ShortName = "Cans", LongName = "Canadian_Aboriginal", TempIndex = 19, Type = ScriptType.Normal},
                new Script {ShortName = "Cari", LongName = "Carian", TempIndex = 20, Type = ScriptType.Normal},
                new Script {ShortName = "Cham", LongName = "Cham", TempIndex = 21, Type = ScriptType.Normal},
                new Script {ShortName = "Cher", LongName = "Cherokee", TempIndex = 22, Type = ScriptType.Normal},
                new Script {ShortName = "Copt", LongName = "Coptic", TempIndex = 23, Type = ScriptType.Normal},
                new Script {ShortName = "Cprt", LongName = "Cypriot", TempIndex = 24, Type = ScriptType.Normal},
                new Script {ShortName = "Cyrl", LongName = "Cyrillic", TempIndex = 25, Type = ScriptType.Normal},
                new Script {ShortName = "Deva", LongName = "Devanagari", TempIndex = 26, Type = ScriptType.Normal},
                new Script {ShortName = "Dogr", LongName = "Dogra", TempIndex = 27, Type = ScriptType.Normal},
                new Script {ShortName = "Dsrt", LongName = "Deseret", TempIndex = 28, Type = ScriptType.Normal},
                new Script {ShortName = "Dupl", LongName = "Duployan", TempIndex = 29, Type = ScriptType.Normal},
                new Script {ShortName = "Egyp", LongName = "Egyptian_Hieroglyphs", TempIndex = 30, Type = ScriptType.Normal},
                new Script {ShortName = "Elba", LongName = "Elbasan", TempIndex = 31, Type = ScriptType.Normal},
                new Script {ShortName = "Elym", LongName = "Elymaic", TempIndex = 32, Type = ScriptType.Normal},
                new Script {ShortName = "Ethi", LongName = "Ethiopic", TempIndex = 33, Type = ScriptType.Normal},
                new Script {ShortName = "Geor", LongName = "Georgian", TempIndex = 34, Type = ScriptType.Normal},
                new Script {ShortName = "Glag", LongName = "Glagolitic", TempIndex = 35, Type = ScriptType.Normal},
                new Script {ShortName = "Gong", LongName = "Gunjala_Gondi", TempIndex = 36, Type = ScriptType.Normal},
                new Script {ShortName = "Gonm", LongName = "Masaram_Gondi", TempIndex = 37, Type = ScriptType.Normal},
                new Script {ShortName = "Goth", LongName = "Gothic", TempIndex = 38, Type = ScriptType.Normal},
                new Script {ShortName = "Gran", LongName = "Grantha", TempIndex = 39, Type = ScriptType.Normal},
                new Script {ShortName = "Grek", LongName = "Greek", TempIndex = 40, Type = ScriptType.Normal},
                new Script {ShortName = "Gujr", LongName = "Gujarati", TempIndex = 41, Type = ScriptType.Normal},
                new Script {ShortName = "Guru", LongName = "Gurmukhi", TempIndex = 42, Type = ScriptType.Normal},
                new Script {ShortName = "Hang", LongName = "Hangul", TempIndex = 43, Type = ScriptType.Normal},
                new Script {ShortName = "Hani", LongName = "Han", TempIndex = 44, Type = ScriptType.Normal},
                new Script {ShortName = "Hano", LongName = "Hanunoo", TempIndex = 45, Type = ScriptType.Normal},
                new Script {ShortName = "Hatr", LongName = "Hatran", TempIndex = 46, Type = ScriptType.Normal},
                new Script {ShortName = "Hebr", LongName = "Hebrew", TempIndex = 47, Type = ScriptType.Normal},
                new Script {ShortName = "Hira", LongName = "Hiragana", TempIndex = 48, Type = ScriptType.Normal},
                new Script {ShortName = "Hluw", LongName = "Anatolian_Hieroglyphs", TempIndex = 49, Type = ScriptType.Normal},
                new Script {ShortName = "Hmng", LongName = "Pahawh_Hmong", TempIndex = 50, Type = ScriptType.Normal},
                new Script {ShortName = "Hmnp", LongName = "Nyiakeng_Puachue_Hmong", TempIndex = 51, Type = ScriptType.Normal},
                new Script {ShortName = "Hrkt", LongName = "Katakana_Or_Hiragana", TempIndex = 52, Type = ScriptType.Normal},
                new Script {ShortName = "Hung", LongName = "Old_Hungarian", TempIndex = 53, Type = ScriptType.Normal},
                new Script {ShortName = "Ital", LongName = "Old_Italic", TempIndex = 54, Type = ScriptType.Normal},
                new Script {ShortName = "Java", LongName = "Javanese", TempIndex = 55, Type = ScriptType.Normal},
                new Script {ShortName = "Kali", LongName = "Kayah_Li", TempIndex = 56, Type = ScriptType.Normal},
                new Script {ShortName = "Kana", LongName = "Katakana", TempIndex = 57, Type = ScriptType.Normal},
                new Script {ShortName = "Khar", LongName = "Kharoshthi", TempIndex = 58, Type = ScriptType.Normal},
                new Script {ShortName = "Khmr", LongName = "Khmer", TempIndex = 59, Type = ScriptType.Normal},
                new Script {ShortName = "Khoj", LongName = "Khojki", TempIndex = 60, Type = ScriptType.Normal},
                new Script {ShortName = "Knda", LongName = "Kannada", TempIndex = 61, Type = ScriptType.Normal},
                new Script {ShortName = "Kthi", LongName = "Kaithi", TempIndex = 62, Type = ScriptType.Normal},
                new Script {ShortName = "Lana", LongName = "Tai_Tham", TempIndex = 63, Type = ScriptType.Normal},
                new Script {ShortName = "Laoo", LongName = "Lao", TempIndex = 64, Type = ScriptType.Normal},
                new Script {ShortName = "Latn", LongName = "Latin", TempIndex = 65, Type = ScriptType.Normal},
                new Script {ShortName = "Lepc", LongName = "Lepcha", TempIndex = 66, Type = ScriptType.Normal},
                new Script {ShortName = "Limb", LongName = "Limbu", TempIndex = 67, Type = ScriptType.Normal},
                new Script {ShortName = "Lina", LongName = "Linear_A", TempIndex = 68, Type = ScriptType.Normal},
                new Script {ShortName = "Linb", LongName = "Linear_B", TempIndex = 69, Type = ScriptType.Normal},
                new Script {ShortName = "Lisu", LongName = "Lisu", TempIndex = 70, Type = ScriptType.Normal},
                new Script {ShortName = "Lyci", LongName = "Lycian", TempIndex = 71, Type = ScriptType.Normal},
                new Script {ShortName = "Lydi", LongName = "Lydian", TempIndex = 72, Type = ScriptType.Normal},
                new Script {ShortName = "Mahj", LongName = "Mahajani", TempIndex = 73, Type = ScriptType.Normal},
                new Script {ShortName = "Maka", LongName = "Makasar", TempIndex = 74, Type = ScriptType.Normal},
                new Script {ShortName = "Mand", LongName = "Mandaic", TempIndex = 75, Type = ScriptType.Normal},
                new Script {ShortName = "Mani", LongName = "Manichaean", TempIndex = 76, Type = ScriptType.Normal},
                new Script {ShortName = "Marc", LongName = "Marchen", TempIndex = 77, Type = ScriptType.Normal},
                new Script {ShortName = "Medf", LongName = "Medefaidrin", TempIndex = 78, Type = ScriptType.Normal},
                new Script {ShortName = "Mend", LongName = "Mende_Kikakui", TempIndex = 79, Type = ScriptType.Normal},
                new Script {ShortName = "Merc", LongName = "Meroitic_Cursive", TempIndex = 80, Type = ScriptType.Normal},
                new Script {ShortName = "Mero", LongName = "Meroitic_Hieroglyphs", TempIndex = 81, Type = ScriptType.Normal},
                new Script {ShortName = "Mlym", LongName = "Malayalam", TempIndex = 82, Type = ScriptType.Normal},
                new Script {ShortName = "Modi", LongName = "Modi", TempIndex = 83, Type = ScriptType.Normal},
                new Script {ShortName = "Mong", LongName = "Mongolian", TempIndex = 84, Type = ScriptType.Normal},
                new Script {ShortName = "Mroo", LongName = "Mro", TempIndex = 85, Type = ScriptType.Normal},
                new Script {ShortName = "Mtei", LongName = "Meetei_Mayek", TempIndex = 86, Type = ScriptType.Normal},
                new Script {ShortName = "Mult", LongName = "Multani", TempIndex = 87, Type = ScriptType.Normal},
                new Script {ShortName = "Mymr", LongName = "Myanmar", TempIndex = 88, Type = ScriptType.Normal},
                new Script {ShortName = "Nand", LongName = "Nandinagari", TempIndex = 89, Type = ScriptType.Normal},
                new Script {ShortName = "Narb", LongName = "Old_North_Arabian", TempIndex = 90, Type = ScriptType.Normal},
                new Script {ShortName = "Nbat", LongName = "Nabataean", TempIndex = 91, Type = ScriptType.Normal},
                new Script {ShortName = "Newa", LongName = "Newa", TempIndex = 92, Type = ScriptType.Normal},
                new Script {ShortName = "Nkoo", LongName = "Nko", TempIndex = 93, Type = ScriptType.Normal},
                new Script {ShortName = "Nshu", LongName = "Nushu", TempIndex = 94, Type = ScriptType.Normal},
                new Script {ShortName = "Ogam", LongName = "Ogham", TempIndex = 95, Type = ScriptType.Normal},
                new Script {ShortName = "Olck", LongName = "Ol_Chiki", TempIndex = 96, Type = ScriptType.Normal},
                new Script {ShortName = "Orkh", LongName = "Old_Turkic", TempIndex = 97, Type = ScriptType.Normal},
                new Script {ShortName = "Orya", LongName = "Oriya", TempIndex = 98, Type = ScriptType.Normal},
                new Script {ShortName = "Osge", LongName = "Osage", TempIndex = 99, Type = ScriptType.Normal},
                new Script {ShortName = "Osma", LongName = "Osmanya", TempIndex = 100, Type = ScriptType.Normal},
                new Script {ShortName = "Palm", LongName = "Palmyrene", TempIndex = 101, Type = ScriptType.Normal},
                new Script {ShortName = "Pauc", LongName = "Pau_Cin_Hau", TempIndex = 102, Type = ScriptType.Normal},
                new Script {ShortName = "Perm", LongName = "Old_Permic", TempIndex = 103, Type = ScriptType.Normal},
                new Script {ShortName = "Phag", LongName = "Phags_Pa", TempIndex = 104, Type = ScriptType.Normal},
                new Script {ShortName = "Phli", LongName = "Inscriptional_Pahlavi", TempIndex = 105, Type = ScriptType.Normal},
                new Script {ShortName = "Phlp", LongName = "Psalter_Pahlavi", TempIndex = 106, Type = ScriptType.Normal},
                new Script {ShortName = "Phnx", LongName = "Phoenician", TempIndex = 107, Type = ScriptType.Normal},
                new Script {ShortName = "Plrd", LongName = "Miao", TempIndex = 108, Type = ScriptType.Normal},
                new Script {ShortName = "Prti", LongName = "Inscriptional_Parthian", TempIndex = 109, Type = ScriptType.Normal},
                new Script {ShortName = "Rjng", LongName = "Rejang", TempIndex = 110, Type = ScriptType.Normal},
                new Script {ShortName = "Rohg", LongName = "Hanifi_Rohingya", TempIndex = 111, Type = ScriptType.Normal},
                new Script {ShortName = "Runr", LongName = "Runic", TempIndex = 112, Type = ScriptType.Normal},
                new Script {ShortName = "Samr", LongName = "Samaritan", TempIndex = 113, Type = ScriptType.Normal},
                new Script {ShortName = "Sarb", LongName = "Old_South_Arabian", TempIndex = 114, Type = ScriptType.Normal},
                new Script {ShortName = "Saur", LongName = "Saurashtra", TempIndex = 115, Type = ScriptType.Normal},
                new Script {ShortName = "Sgnw", LongName = "SignWriting", TempIndex = 116, Type = ScriptType.Normal},
                new Script {ShortName = "Shaw", LongName = "Shavian", TempIndex = 117, Type = ScriptType.Normal},
                new Script {ShortName = "Shrd", LongName = "Sharada", TempIndex = 118, Type = ScriptType.Normal},
                new Script {ShortName = "Sidd", LongName = "Siddham", TempIndex = 119, Type = ScriptType.Normal},
                new Script {ShortName = "Sind", LongName = "Khudawadi", TempIndex = 120, Type = ScriptType.Normal},
                new Script {ShortName = "Sinh", LongName = "Sinhala", TempIndex = 121, Type = ScriptType.Normal},
                new Script {ShortName = "Sogd", LongName = "Sogdian", TempIndex = 122, Type = ScriptType.Normal},
                new Script {ShortName = "Sogo", LongName = "Old_Sogdian", TempIndex = 123, Type = ScriptType.Normal},
                new Script {ShortName = "Sora", LongName = "Sora_Sompeng", TempIndex = 124, Type = ScriptType.Normal},
                new Script {ShortName = "Soyo", LongName = "Soyombo", TempIndex = 125, Type = ScriptType.Normal},
                new Script {ShortName = "Sund", LongName = "Sundanese", TempIndex = 126, Type = ScriptType.Normal},
                new Script {ShortName = "Sylo", LongName = "Syloti_Nagri", TempIndex = 127, Type = ScriptType.Normal},
                new Script {ShortName = "Syrc", LongName = "Syriac", TempIndex = 128, Type = ScriptType.Normal},
                new Script {ShortName = "Tagb", LongName = "Tagbanwa", TempIndex = 129, Type = ScriptType.Normal},
                new Script {ShortName = "Takr", LongName = "Takri", TempIndex = 130, Type = ScriptType.Normal},
                new Script {ShortName = "Tale", LongName = "Tai_Le", TempIndex = 131, Type = ScriptType.Normal},
                new Script {ShortName = "Talu", LongName = "New_Tai_Lue", TempIndex = 132, Type = ScriptType.Normal},
                new Script {ShortName = "Taml", LongName = "Tamil", TempIndex = 133, Type = ScriptType.Normal},
                new Script {ShortName = "Tang", LongName = "Tangut", TempIndex = 134, Type = ScriptType.Normal},
                new Script {ShortName = "Tavt", LongName = "Tai_Viet", TempIndex = 135, Type = ScriptType.Normal},
                new Script {ShortName = "Telu", LongName = "Telugu", TempIndex = 136, Type = ScriptType.Normal},
                new Script {ShortName = "Tfng", LongName = "Tifinagh", TempIndex = 137, Type = ScriptType.Normal},
                new Script {ShortName = "Tglg", LongName = "Tagalog", TempIndex = 138, Type = ScriptType.Normal},
                new Script {ShortName = "Thaa", LongName = "Thaana", TempIndex = 139, Type = ScriptType.Normal},
                new Script {ShortName = "Thai", LongName = "Thai", TempIndex = 140, Type = ScriptType.Normal},
                new Script {ShortName = "Tibt", LongName = "Tibetan", TempIndex = 141, Type = ScriptType.Normal},
                new Script {ShortName = "Tirh", LongName = "Tirhuta", TempIndex = 142, Type = ScriptType.Normal},
                new Script {ShortName = "Ugar", LongName = "Ugaritic", TempIndex = 143, Type = ScriptType.Normal},
                new Script {ShortName = "Vaii", LongName = "Vai", TempIndex = 144, Type = ScriptType.Normal},
                new Script {ShortName = "Wara", LongName = "Warang_Citi", TempIndex = 145, Type = ScriptType.Normal},
                new Script {ShortName = "Wcho", LongName = "Wancho", TempIndex = 146, Type = ScriptType.Normal},
                new Script {ShortName = "Xpeo", LongName = "Old_Persian", TempIndex = 147, Type = ScriptType.Normal},
                new Script {ShortName = "Xsux", LongName = "Cuneiform", TempIndex = 148, Type = ScriptType.Normal},
                new Script {ShortName = "Yiii", LongName = "Yi", TempIndex = 149, Type = ScriptType.Normal},
                new Script {ShortName = "Zanb", LongName = "Zanabazar_Square", TempIndex = 150, Type = ScriptType.Normal},

                new Script {ShortName = "Zinh", LongName = "Inherited", TempIndex = 151, Type = ScriptType.Inherited},
                new Script {ShortName = "Zyyy", LongName = "Common", TempIndex = 152, Type = ScriptType.Common},
                new Script {ShortName = "Zzzz", LongName = "Unknown", TempIndex = 153, Type = ScriptType.Unknown},

        };

        /// <summary>
        /// Derived from Unicode data, see https://github.com/DaniRK/UnicodeScriptDetectorNet/tree/master/ImportDataToCSharp
        /// </summary>
        private static readonly CodepointScript[] CodepointScripts = new CodepointScript[]
            {

                new CodepointScript {RangeStart = 0x0, RangeEnd = 0x40, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x41, RangeEnd = 0x5A, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x5B, RangeEnd = 0x60, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x61, RangeEnd = 0x7A, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x7B, RangeEnd = 0xA9, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xAA, RangeEnd = 0xAA, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xAB, RangeEnd = 0xB9, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xBA, RangeEnd = 0xBA, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xBB, RangeEnd = 0xBF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xC0, RangeEnd = 0xD6, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xD7, RangeEnd = 0xD7, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xD8, RangeEnd = 0xF6, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xF7, RangeEnd = 0xF7, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xF8, RangeEnd = 0x2B8, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x2B9, RangeEnd = 0x2DF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2E0, RangeEnd = 0x2E4, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x2E5, RangeEnd = 0x2E9, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2EA, RangeEnd = 0x2EB, Script = Scripts[13]},
                new CodepointScript {RangeStart = 0x2EC, RangeEnd = 0x2FF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x300, RangeEnd = 0x36F, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x370, RangeEnd = 0x373, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x374, RangeEnd = 0x374, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x375, RangeEnd = 0x377, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x37A, RangeEnd = 0x37D, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x37E, RangeEnd = 0x37E, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x37F, RangeEnd = 0x37F, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x384, RangeEnd = 0x384, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x385, RangeEnd = 0x385, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x386, RangeEnd = 0x386, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x387, RangeEnd = 0x387, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x388, RangeEnd = 0x38A, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x38C, RangeEnd = 0x38C, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x38E, RangeEnd = 0x3A1, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x3A3, RangeEnd = 0x3E1, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x3E2, RangeEnd = 0x3EF, Script = Scripts[23]},
                new CodepointScript {RangeStart = 0x3F0, RangeEnd = 0x3FF, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x400, RangeEnd = 0x484, Script = Scripts[25]},
                new CodepointScript {RangeStart = 0x485, RangeEnd = 0x486, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x487, RangeEnd = 0x52F, Script = Scripts[25]},
                new CodepointScript {RangeStart = 0x531, RangeEnd = 0x556, Script = Scripts[5]},
                new CodepointScript {RangeStart = 0x559, RangeEnd = 0x588, Script = Scripts[5]},
                new CodepointScript {RangeStart = 0x589, RangeEnd = 0x589, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x58A, RangeEnd = 0x58A, Script = Scripts[5]},
                new CodepointScript {RangeStart = 0x58D, RangeEnd = 0x58F, Script = Scripts[5]},
                new CodepointScript {RangeStart = 0x591, RangeEnd = 0x5C7, Script = Scripts[47]},
                new CodepointScript {RangeStart = 0x5D0, RangeEnd = 0x5EA, Script = Scripts[47]},
                new CodepointScript {RangeStart = 0x5EF, RangeEnd = 0x5F4, Script = Scripts[47]},
                new CodepointScript {RangeStart = 0x600, RangeEnd = 0x604, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x605, RangeEnd = 0x605, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x606, RangeEnd = 0x60B, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x60C, RangeEnd = 0x60C, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x60D, RangeEnd = 0x61A, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x61B, RangeEnd = 0x61B, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x61C, RangeEnd = 0x61C, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x61E, RangeEnd = 0x61E, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x61F, RangeEnd = 0x61F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x620, RangeEnd = 0x63F, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x640, RangeEnd = 0x640, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x641, RangeEnd = 0x64A, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x64B, RangeEnd = 0x655, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x656, RangeEnd = 0x66F, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x670, RangeEnd = 0x670, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x671, RangeEnd = 0x6DC, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x6DD, RangeEnd = 0x6DD, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x6DE, RangeEnd = 0x6FF, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x700, RangeEnd = 0x70D, Script = Scripts[128]},
                new CodepointScript {RangeStart = 0x70F, RangeEnd = 0x74A, Script = Scripts[128]},
                new CodepointScript {RangeStart = 0x74D, RangeEnd = 0x74F, Script = Scripts[128]},
                new CodepointScript {RangeStart = 0x750, RangeEnd = 0x77F, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x780, RangeEnd = 0x7B1, Script = Scripts[139]},
                new CodepointScript {RangeStart = 0x7C0, RangeEnd = 0x7FA, Script = Scripts[93]},
                new CodepointScript {RangeStart = 0x7FD, RangeEnd = 0x7FF, Script = Scripts[93]},
                new CodepointScript {RangeStart = 0x800, RangeEnd = 0x82D, Script = Scripts[113]},
                new CodepointScript {RangeStart = 0x830, RangeEnd = 0x83E, Script = Scripts[113]},
                new CodepointScript {RangeStart = 0x840, RangeEnd = 0x85B, Script = Scripts[75]},
                new CodepointScript {RangeStart = 0x85E, RangeEnd = 0x85E, Script = Scripts[75]},
                new CodepointScript {RangeStart = 0x860, RangeEnd = 0x86A, Script = Scripts[128]},
                new CodepointScript {RangeStart = 0x8A0, RangeEnd = 0x8B4, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x8B6, RangeEnd = 0x8BD, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x8D3, RangeEnd = 0x8E1, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x8E2, RangeEnd = 0x8E2, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x8E3, RangeEnd = 0x8FF, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x900, RangeEnd = 0x950, Script = Scripts[26]},
                new CodepointScript {RangeStart = 0x951, RangeEnd = 0x954, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x955, RangeEnd = 0x963, Script = Scripts[26]},
                new CodepointScript {RangeStart = 0x964, RangeEnd = 0x965, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x966, RangeEnd = 0x97F, Script = Scripts[26]},
                new CodepointScript {RangeStart = 0x980, RangeEnd = 0x983, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x985, RangeEnd = 0x98C, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x98F, RangeEnd = 0x990, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x993, RangeEnd = 0x9A8, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x9AA, RangeEnd = 0x9B0, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x9B2, RangeEnd = 0x9B2, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x9B6, RangeEnd = 0x9B9, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x9BC, RangeEnd = 0x9C4, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x9C7, RangeEnd = 0x9C8, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x9CB, RangeEnd = 0x9CE, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x9D7, RangeEnd = 0x9D7, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x9DC, RangeEnd = 0x9DD, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x9DF, RangeEnd = 0x9E3, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0x9E6, RangeEnd = 0x9FE, Script = Scripts[11]},
                new CodepointScript {RangeStart = 0xA01, RangeEnd = 0xA03, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA05, RangeEnd = 0xA0A, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA0F, RangeEnd = 0xA10, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA13, RangeEnd = 0xA28, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA2A, RangeEnd = 0xA30, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA32, RangeEnd = 0xA33, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA35, RangeEnd = 0xA36, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA38, RangeEnd = 0xA39, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA3C, RangeEnd = 0xA3C, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA3E, RangeEnd = 0xA42, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA47, RangeEnd = 0xA48, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA4B, RangeEnd = 0xA4D, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA51, RangeEnd = 0xA51, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA59, RangeEnd = 0xA5C, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA5E, RangeEnd = 0xA5E, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA66, RangeEnd = 0xA76, Script = Scripts[42]},
                new CodepointScript {RangeStart = 0xA81, RangeEnd = 0xA83, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xA85, RangeEnd = 0xA8D, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xA8F, RangeEnd = 0xA91, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xA93, RangeEnd = 0xAA8, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xAAA, RangeEnd = 0xAB0, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xAB2, RangeEnd = 0xAB3, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xAB5, RangeEnd = 0xAB9, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xABC, RangeEnd = 0xAC5, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xAC7, RangeEnd = 0xAC9, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xACB, RangeEnd = 0xACD, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xAD0, RangeEnd = 0xAD0, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xAE0, RangeEnd = 0xAE3, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xAE6, RangeEnd = 0xAF1, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xAF9, RangeEnd = 0xAFF, Script = Scripts[41]},
                new CodepointScript {RangeStart = 0xB01, RangeEnd = 0xB03, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB05, RangeEnd = 0xB0C, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB0F, RangeEnd = 0xB10, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB13, RangeEnd = 0xB28, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB2A, RangeEnd = 0xB30, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB32, RangeEnd = 0xB33, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB35, RangeEnd = 0xB39, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB3C, RangeEnd = 0xB44, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB47, RangeEnd = 0xB48, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB4B, RangeEnd = 0xB4D, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB56, RangeEnd = 0xB57, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB5C, RangeEnd = 0xB5D, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB5F, RangeEnd = 0xB63, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB66, RangeEnd = 0xB77, Script = Scripts[98]},
                new CodepointScript {RangeStart = 0xB82, RangeEnd = 0xB83, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xB85, RangeEnd = 0xB8A, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xB8E, RangeEnd = 0xB90, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xB92, RangeEnd = 0xB95, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xB99, RangeEnd = 0xB9A, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xB9C, RangeEnd = 0xB9C, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xB9E, RangeEnd = 0xB9F, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xBA3, RangeEnd = 0xBA4, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xBA8, RangeEnd = 0xBAA, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xBAE, RangeEnd = 0xBB9, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xBBE, RangeEnd = 0xBC2, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xBC6, RangeEnd = 0xBC8, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xBCA, RangeEnd = 0xBCD, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xBD0, RangeEnd = 0xBD0, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xBD7, RangeEnd = 0xBD7, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xBE6, RangeEnd = 0xBFA, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0xC00, RangeEnd = 0xC0C, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC0E, RangeEnd = 0xC10, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC12, RangeEnd = 0xC28, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC2A, RangeEnd = 0xC39, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC3D, RangeEnd = 0xC44, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC46, RangeEnd = 0xC48, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC4A, RangeEnd = 0xC4D, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC55, RangeEnd = 0xC56, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC58, RangeEnd = 0xC5A, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC60, RangeEnd = 0xC63, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC66, RangeEnd = 0xC6F, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC77, RangeEnd = 0xC7F, Script = Scripts[136]},
                new CodepointScript {RangeStart = 0xC80, RangeEnd = 0xC8C, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xC8E, RangeEnd = 0xC90, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xC92, RangeEnd = 0xCA8, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xCAA, RangeEnd = 0xCB3, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xCB5, RangeEnd = 0xCB9, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xCBC, RangeEnd = 0xCC4, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xCC6, RangeEnd = 0xCC8, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xCCA, RangeEnd = 0xCCD, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xCD5, RangeEnd = 0xCD6, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xCDE, RangeEnd = 0xCDE, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xCE0, RangeEnd = 0xCE3, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xCE6, RangeEnd = 0xCEF, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xCF1, RangeEnd = 0xCF2, Script = Scripts[61]},
                new CodepointScript {RangeStart = 0xD00, RangeEnd = 0xD03, Script = Scripts[82]},
                new CodepointScript {RangeStart = 0xD05, RangeEnd = 0xD0C, Script = Scripts[82]},
                new CodepointScript {RangeStart = 0xD0E, RangeEnd = 0xD10, Script = Scripts[82]},
                new CodepointScript {RangeStart = 0xD12, RangeEnd = 0xD44, Script = Scripts[82]},
                new CodepointScript {RangeStart = 0xD46, RangeEnd = 0xD48, Script = Scripts[82]},
                new CodepointScript {RangeStart = 0xD4A, RangeEnd = 0xD4F, Script = Scripts[82]},
                new CodepointScript {RangeStart = 0xD54, RangeEnd = 0xD63, Script = Scripts[82]},
                new CodepointScript {RangeStart = 0xD66, RangeEnd = 0xD7F, Script = Scripts[82]},
                new CodepointScript {RangeStart = 0xD82, RangeEnd = 0xD83, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xD85, RangeEnd = 0xD96, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xD9A, RangeEnd = 0xDB1, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xDB3, RangeEnd = 0xDBB, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xDBD, RangeEnd = 0xDBD, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xDC0, RangeEnd = 0xDC6, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xDCA, RangeEnd = 0xDCA, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xDCF, RangeEnd = 0xDD4, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xDD6, RangeEnd = 0xDD6, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xDD8, RangeEnd = 0xDDF, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xDE6, RangeEnd = 0xDEF, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xDF2, RangeEnd = 0xDF4, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0xE01, RangeEnd = 0xE3A, Script = Scripts[140]},
                new CodepointScript {RangeStart = 0xE3F, RangeEnd = 0xE3F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xE40, RangeEnd = 0xE5B, Script = Scripts[140]},
                new CodepointScript {RangeStart = 0xE81, RangeEnd = 0xE82, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xE84, RangeEnd = 0xE84, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xE86, RangeEnd = 0xE8A, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xE8C, RangeEnd = 0xEA3, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xEA5, RangeEnd = 0xEA5, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xEA7, RangeEnd = 0xEBD, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xEC0, RangeEnd = 0xEC4, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xEC6, RangeEnd = 0xEC6, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xEC8, RangeEnd = 0xECD, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xED0, RangeEnd = 0xED9, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xEDC, RangeEnd = 0xEDF, Script = Scripts[64]},
                new CodepointScript {RangeStart = 0xF00, RangeEnd = 0xF47, Script = Scripts[141]},
                new CodepointScript {RangeStart = 0xF49, RangeEnd = 0xF6C, Script = Scripts[141]},
                new CodepointScript {RangeStart = 0xF71, RangeEnd = 0xF97, Script = Scripts[141]},
                new CodepointScript {RangeStart = 0xF99, RangeEnd = 0xFBC, Script = Scripts[141]},
                new CodepointScript {RangeStart = 0xFBE, RangeEnd = 0xFCC, Script = Scripts[141]},
                new CodepointScript {RangeStart = 0xFCE, RangeEnd = 0xFD4, Script = Scripts[141]},
                new CodepointScript {RangeStart = 0xFD5, RangeEnd = 0xFD8, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFD9, RangeEnd = 0xFDA, Script = Scripts[141]},
                new CodepointScript {RangeStart = 0x1000, RangeEnd = 0x109F, Script = Scripts[88]},
                new CodepointScript {RangeStart = 0x10A0, RangeEnd = 0x10C5, Script = Scripts[34]},
                new CodepointScript {RangeStart = 0x10C7, RangeEnd = 0x10C7, Script = Scripts[34]},
                new CodepointScript {RangeStart = 0x10CD, RangeEnd = 0x10CD, Script = Scripts[34]},
                new CodepointScript {RangeStart = 0x10D0, RangeEnd = 0x10FA, Script = Scripts[34]},
                new CodepointScript {RangeStart = 0x10FB, RangeEnd = 0x10FB, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x10FC, RangeEnd = 0x10FF, Script = Scripts[34]},
                new CodepointScript {RangeStart = 0x1100, RangeEnd = 0x11FF, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0x1200, RangeEnd = 0x1248, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x124A, RangeEnd = 0x124D, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x1250, RangeEnd = 0x1256, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x1258, RangeEnd = 0x1258, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x125A, RangeEnd = 0x125D, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x1260, RangeEnd = 0x1288, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x128A, RangeEnd = 0x128D, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x1290, RangeEnd = 0x12B0, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x12B2, RangeEnd = 0x12B5, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x12B8, RangeEnd = 0x12BE, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x12C0, RangeEnd = 0x12C0, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x12C2, RangeEnd = 0x12C5, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x12C8, RangeEnd = 0x12D6, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x12D8, RangeEnd = 0x1310, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x1312, RangeEnd = 0x1315, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x1318, RangeEnd = 0x135A, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x135D, RangeEnd = 0x137C, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x1380, RangeEnd = 0x1399, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x13A0, RangeEnd = 0x13F5, Script = Scripts[22]},
                new CodepointScript {RangeStart = 0x13F8, RangeEnd = 0x13FD, Script = Scripts[22]},
                new CodepointScript {RangeStart = 0x1400, RangeEnd = 0x167F, Script = Scripts[19]},
                new CodepointScript {RangeStart = 0x1680, RangeEnd = 0x169C, Script = Scripts[95]},
                new CodepointScript {RangeStart = 0x16A0, RangeEnd = 0x16EA, Script = Scripts[112]},
                new CodepointScript {RangeStart = 0x16EB, RangeEnd = 0x16ED, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x16EE, RangeEnd = 0x16F8, Script = Scripts[112]},
                new CodepointScript {RangeStart = 0x1700, RangeEnd = 0x170C, Script = Scripts[138]},
                new CodepointScript {RangeStart = 0x170E, RangeEnd = 0x1714, Script = Scripts[138]},
                new CodepointScript {RangeStart = 0x1720, RangeEnd = 0x1734, Script = Scripts[45]},
                new CodepointScript {RangeStart = 0x1735, RangeEnd = 0x1736, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1740, RangeEnd = 0x1753, Script = Scripts[17]},
                new CodepointScript {RangeStart = 0x1760, RangeEnd = 0x176C, Script = Scripts[129]},
                new CodepointScript {RangeStart = 0x176E, RangeEnd = 0x1770, Script = Scripts[129]},
                new CodepointScript {RangeStart = 0x1772, RangeEnd = 0x1773, Script = Scripts[129]},
                new CodepointScript {RangeStart = 0x1780, RangeEnd = 0x17DD, Script = Scripts[59]},
                new CodepointScript {RangeStart = 0x17E0, RangeEnd = 0x17E9, Script = Scripts[59]},
                new CodepointScript {RangeStart = 0x17F0, RangeEnd = 0x17F9, Script = Scripts[59]},
                new CodepointScript {RangeStart = 0x1800, RangeEnd = 0x1801, Script = Scripts[84]},
                new CodepointScript {RangeStart = 0x1802, RangeEnd = 0x1803, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1804, RangeEnd = 0x1804, Script = Scripts[84]},
                new CodepointScript {RangeStart = 0x1805, RangeEnd = 0x1805, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1806, RangeEnd = 0x180E, Script = Scripts[84]},
                new CodepointScript {RangeStart = 0x1810, RangeEnd = 0x1819, Script = Scripts[84]},
                new CodepointScript {RangeStart = 0x1820, RangeEnd = 0x1878, Script = Scripts[84]},
                new CodepointScript {RangeStart = 0x1880, RangeEnd = 0x18AA, Script = Scripts[84]},
                new CodepointScript {RangeStart = 0x18B0, RangeEnd = 0x18F5, Script = Scripts[19]},
                new CodepointScript {RangeStart = 0x1900, RangeEnd = 0x191E, Script = Scripts[67]},
                new CodepointScript {RangeStart = 0x1920, RangeEnd = 0x192B, Script = Scripts[67]},
                new CodepointScript {RangeStart = 0x1930, RangeEnd = 0x193B, Script = Scripts[67]},
                new CodepointScript {RangeStart = 0x1940, RangeEnd = 0x1940, Script = Scripts[67]},
                new CodepointScript {RangeStart = 0x1944, RangeEnd = 0x194F, Script = Scripts[67]},
                new CodepointScript {RangeStart = 0x1950, RangeEnd = 0x196D, Script = Scripts[131]},
                new CodepointScript {RangeStart = 0x1970, RangeEnd = 0x1974, Script = Scripts[131]},
                new CodepointScript {RangeStart = 0x1980, RangeEnd = 0x19AB, Script = Scripts[132]},
                new CodepointScript {RangeStart = 0x19B0, RangeEnd = 0x19C9, Script = Scripts[132]},
                new CodepointScript {RangeStart = 0x19D0, RangeEnd = 0x19DA, Script = Scripts[132]},
                new CodepointScript {RangeStart = 0x19DE, RangeEnd = 0x19DF, Script = Scripts[132]},
                new CodepointScript {RangeStart = 0x19E0, RangeEnd = 0x19FF, Script = Scripts[59]},
                new CodepointScript {RangeStart = 0x1A00, RangeEnd = 0x1A1B, Script = Scripts[16]},
                new CodepointScript {RangeStart = 0x1A1E, RangeEnd = 0x1A1F, Script = Scripts[16]},
                new CodepointScript {RangeStart = 0x1A20, RangeEnd = 0x1A5E, Script = Scripts[63]},
                new CodepointScript {RangeStart = 0x1A60, RangeEnd = 0x1A7C, Script = Scripts[63]},
                new CodepointScript {RangeStart = 0x1A7F, RangeEnd = 0x1A89, Script = Scripts[63]},
                new CodepointScript {RangeStart = 0x1A90, RangeEnd = 0x1A99, Script = Scripts[63]},
                new CodepointScript {RangeStart = 0x1AA0, RangeEnd = 0x1AAD, Script = Scripts[63]},
                new CodepointScript {RangeStart = 0x1AB0, RangeEnd = 0x1ABE, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1B00, RangeEnd = 0x1B4B, Script = Scripts[7]},
                new CodepointScript {RangeStart = 0x1B50, RangeEnd = 0x1B7C, Script = Scripts[7]},
                new CodepointScript {RangeStart = 0x1B80, RangeEnd = 0x1BBF, Script = Scripts[126]},
                new CodepointScript {RangeStart = 0x1BC0, RangeEnd = 0x1BF3, Script = Scripts[10]},
                new CodepointScript {RangeStart = 0x1BFC, RangeEnd = 0x1BFF, Script = Scripts[10]},
                new CodepointScript {RangeStart = 0x1C00, RangeEnd = 0x1C37, Script = Scripts[66]},
                new CodepointScript {RangeStart = 0x1C3B, RangeEnd = 0x1C49, Script = Scripts[66]},
                new CodepointScript {RangeStart = 0x1C4D, RangeEnd = 0x1C4F, Script = Scripts[66]},
                new CodepointScript {RangeStart = 0x1C50, RangeEnd = 0x1C7F, Script = Scripts[96]},
                new CodepointScript {RangeStart = 0x1C80, RangeEnd = 0x1C88, Script = Scripts[25]},
                new CodepointScript {RangeStart = 0x1C90, RangeEnd = 0x1CBA, Script = Scripts[34]},
                new CodepointScript {RangeStart = 0x1CBD, RangeEnd = 0x1CBF, Script = Scripts[34]},
                new CodepointScript {RangeStart = 0x1CC0, RangeEnd = 0x1CC7, Script = Scripts[126]},
                new CodepointScript {RangeStart = 0x1CD0, RangeEnd = 0x1CD2, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1CD3, RangeEnd = 0x1CD3, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1CD4, RangeEnd = 0x1CE0, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1CE1, RangeEnd = 0x1CE1, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1CE2, RangeEnd = 0x1CE8, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1CE9, RangeEnd = 0x1CEC, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1CED, RangeEnd = 0x1CED, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1CEE, RangeEnd = 0x1CF3, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1CF4, RangeEnd = 0x1CF4, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1CF5, RangeEnd = 0x1CF7, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1CF8, RangeEnd = 0x1CF9, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1CFA, RangeEnd = 0x1CFA, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D00, RangeEnd = 0x1D25, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x1D26, RangeEnd = 0x1D2A, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1D2B, RangeEnd = 0x1D2B, Script = Scripts[25]},
                new CodepointScript {RangeStart = 0x1D2C, RangeEnd = 0x1D5C, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x1D5D, RangeEnd = 0x1D61, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1D62, RangeEnd = 0x1D65, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x1D66, RangeEnd = 0x1D6A, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1D6B, RangeEnd = 0x1D77, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x1D78, RangeEnd = 0x1D78, Script = Scripts[25]},
                new CodepointScript {RangeStart = 0x1D79, RangeEnd = 0x1DBE, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x1DBF, RangeEnd = 0x1DBF, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1DC0, RangeEnd = 0x1DF9, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1DFB, RangeEnd = 0x1DFF, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1E00, RangeEnd = 0x1EFF, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x1F00, RangeEnd = 0x1F15, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1F18, RangeEnd = 0x1F1D, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1F20, RangeEnd = 0x1F45, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1F48, RangeEnd = 0x1F4D, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1F50, RangeEnd = 0x1F57, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1F59, RangeEnd = 0x1F59, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1F5B, RangeEnd = 0x1F5B, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1F5D, RangeEnd = 0x1F5D, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1F5F, RangeEnd = 0x1F7D, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1F80, RangeEnd = 0x1FB4, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1FB6, RangeEnd = 0x1FC4, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1FC6, RangeEnd = 0x1FD3, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1FD6, RangeEnd = 0x1FDB, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1FDD, RangeEnd = 0x1FEF, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1FF2, RangeEnd = 0x1FF4, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1FF6, RangeEnd = 0x1FFE, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x2000, RangeEnd = 0x200B, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x200C, RangeEnd = 0x200D, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x200E, RangeEnd = 0x2064, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2066, RangeEnd = 0x2070, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2071, RangeEnd = 0x2071, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x2074, RangeEnd = 0x207E, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x207F, RangeEnd = 0x207F, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x2080, RangeEnd = 0x208E, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2090, RangeEnd = 0x209C, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x20A0, RangeEnd = 0x20BF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x20D0, RangeEnd = 0x20F0, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x2100, RangeEnd = 0x2125, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2126, RangeEnd = 0x2126, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x2127, RangeEnd = 0x2129, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x212A, RangeEnd = 0x212B, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x212C, RangeEnd = 0x2131, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2132, RangeEnd = 0x2132, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x2133, RangeEnd = 0x214D, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x214E, RangeEnd = 0x214E, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x214F, RangeEnd = 0x215F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2160, RangeEnd = 0x2188, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x2189, RangeEnd = 0x218B, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2190, RangeEnd = 0x2426, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2440, RangeEnd = 0x244A, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2460, RangeEnd = 0x27FF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2800, RangeEnd = 0x28FF, Script = Scripts[15]},
                new CodepointScript {RangeStart = 0x2900, RangeEnd = 0x2B73, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2B76, RangeEnd = 0x2B95, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2B98, RangeEnd = 0x2BFF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2C00, RangeEnd = 0x2C2E, Script = Scripts[35]},
                new CodepointScript {RangeStart = 0x2C30, RangeEnd = 0x2C5E, Script = Scripts[35]},
                new CodepointScript {RangeStart = 0x2C60, RangeEnd = 0x2C7F, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0x2C80, RangeEnd = 0x2CF3, Script = Scripts[23]},
                new CodepointScript {RangeStart = 0x2CF9, RangeEnd = 0x2CFF, Script = Scripts[23]},
                new CodepointScript {RangeStart = 0x2D00, RangeEnd = 0x2D25, Script = Scripts[34]},
                new CodepointScript {RangeStart = 0x2D27, RangeEnd = 0x2D27, Script = Scripts[34]},
                new CodepointScript {RangeStart = 0x2D2D, RangeEnd = 0x2D2D, Script = Scripts[34]},
                new CodepointScript {RangeStart = 0x2D30, RangeEnd = 0x2D67, Script = Scripts[137]},
                new CodepointScript {RangeStart = 0x2D6F, RangeEnd = 0x2D70, Script = Scripts[137]},
                new CodepointScript {RangeStart = 0x2D7F, RangeEnd = 0x2D7F, Script = Scripts[137]},
                new CodepointScript {RangeStart = 0x2D80, RangeEnd = 0x2D96, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x2DA0, RangeEnd = 0x2DA6, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x2DA8, RangeEnd = 0x2DAE, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x2DB0, RangeEnd = 0x2DB6, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x2DB8, RangeEnd = 0x2DBE, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x2DC0, RangeEnd = 0x2DC6, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x2DC8, RangeEnd = 0x2DCE, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x2DD0, RangeEnd = 0x2DD6, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x2DD8, RangeEnd = 0x2DDE, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0x2DE0, RangeEnd = 0x2DFF, Script = Scripts[25]},
                new CodepointScript {RangeStart = 0x2E00, RangeEnd = 0x2E4F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x2E80, RangeEnd = 0x2E99, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x2E9B, RangeEnd = 0x2EF3, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x2F00, RangeEnd = 0x2FD5, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x2FF0, RangeEnd = 0x2FFB, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x3000, RangeEnd = 0x3004, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x3005, RangeEnd = 0x3005, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x3006, RangeEnd = 0x3006, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x3007, RangeEnd = 0x3007, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x3008, RangeEnd = 0x3020, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x3021, RangeEnd = 0x3029, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x302A, RangeEnd = 0x302D, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x302E, RangeEnd = 0x302F, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0x3030, RangeEnd = 0x3037, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x3038, RangeEnd = 0x303B, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x303C, RangeEnd = 0x303F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x3041, RangeEnd = 0x3096, Script = Scripts[48]},
                new CodepointScript {RangeStart = 0x3099, RangeEnd = 0x309A, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x309B, RangeEnd = 0x309C, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x309D, RangeEnd = 0x309F, Script = Scripts[48]},
                new CodepointScript {RangeStart = 0x30A0, RangeEnd = 0x30A0, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x30A1, RangeEnd = 0x30FA, Script = Scripts[57]},
                new CodepointScript {RangeStart = 0x30FB, RangeEnd = 0x30FC, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x30FD, RangeEnd = 0x30FF, Script = Scripts[57]},
                new CodepointScript {RangeStart = 0x3105, RangeEnd = 0x312F, Script = Scripts[13]},
                new CodepointScript {RangeStart = 0x3131, RangeEnd = 0x318E, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0x3190, RangeEnd = 0x319F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x31A0, RangeEnd = 0x31BA, Script = Scripts[13]},
                new CodepointScript {RangeStart = 0x31C0, RangeEnd = 0x31E3, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x31F0, RangeEnd = 0x31FF, Script = Scripts[57]},
                new CodepointScript {RangeStart = 0x3200, RangeEnd = 0x321E, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0x3220, RangeEnd = 0x325F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x3260, RangeEnd = 0x327E, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0x327F, RangeEnd = 0x32CF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x32D0, RangeEnd = 0x32FE, Script = Scripts[57]},
                new CodepointScript {RangeStart = 0x3300, RangeEnd = 0x3357, Script = Scripts[57]},
                new CodepointScript {RangeStart = 0x3358, RangeEnd = 0x33FF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x3400, RangeEnd = 0x4DB5, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x4DC0, RangeEnd = 0x4DFF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x4E00, RangeEnd = 0x9FEF, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0xA000, RangeEnd = 0xA48C, Script = Scripts[149]},
                new CodepointScript {RangeStart = 0xA490, RangeEnd = 0xA4C6, Script = Scripts[149]},
                new CodepointScript {RangeStart = 0xA4D0, RangeEnd = 0xA4FF, Script = Scripts[70]},
                new CodepointScript {RangeStart = 0xA500, RangeEnd = 0xA62B, Script = Scripts[144]},
                new CodepointScript {RangeStart = 0xA640, RangeEnd = 0xA69F, Script = Scripts[25]},
                new CodepointScript {RangeStart = 0xA6A0, RangeEnd = 0xA6F7, Script = Scripts[8]},
                new CodepointScript {RangeStart = 0xA700, RangeEnd = 0xA721, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xA722, RangeEnd = 0xA787, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xA788, RangeEnd = 0xA78A, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xA78B, RangeEnd = 0xA7BF, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xA7C2, RangeEnd = 0xA7C6, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xA7F7, RangeEnd = 0xA7FF, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xA800, RangeEnd = 0xA82B, Script = Scripts[127]},
                new CodepointScript {RangeStart = 0xA830, RangeEnd = 0xA839, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xA840, RangeEnd = 0xA877, Script = Scripts[104]},
                new CodepointScript {RangeStart = 0xA880, RangeEnd = 0xA8C5, Script = Scripts[115]},
                new CodepointScript {RangeStart = 0xA8CE, RangeEnd = 0xA8D9, Script = Scripts[115]},
                new CodepointScript {RangeStart = 0xA8E0, RangeEnd = 0xA8FF, Script = Scripts[26]},
                new CodepointScript {RangeStart = 0xA900, RangeEnd = 0xA92D, Script = Scripts[56]},
                new CodepointScript {RangeStart = 0xA92E, RangeEnd = 0xA92E, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xA92F, RangeEnd = 0xA92F, Script = Scripts[56]},
                new CodepointScript {RangeStart = 0xA930, RangeEnd = 0xA953, Script = Scripts[110]},
                new CodepointScript {RangeStart = 0xA95F, RangeEnd = 0xA95F, Script = Scripts[110]},
                new CodepointScript {RangeStart = 0xA960, RangeEnd = 0xA97C, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0xA980, RangeEnd = 0xA9CD, Script = Scripts[55]},
                new CodepointScript {RangeStart = 0xA9CF, RangeEnd = 0xA9CF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xA9D0, RangeEnd = 0xA9D9, Script = Scripts[55]},
                new CodepointScript {RangeStart = 0xA9DE, RangeEnd = 0xA9DF, Script = Scripts[55]},
                new CodepointScript {RangeStart = 0xA9E0, RangeEnd = 0xA9FE, Script = Scripts[88]},
                new CodepointScript {RangeStart = 0xAA00, RangeEnd = 0xAA36, Script = Scripts[21]},
                new CodepointScript {RangeStart = 0xAA40, RangeEnd = 0xAA4D, Script = Scripts[21]},
                new CodepointScript {RangeStart = 0xAA50, RangeEnd = 0xAA59, Script = Scripts[21]},
                new CodepointScript {RangeStart = 0xAA5C, RangeEnd = 0xAA5F, Script = Scripts[21]},
                new CodepointScript {RangeStart = 0xAA60, RangeEnd = 0xAA7F, Script = Scripts[88]},
                new CodepointScript {RangeStart = 0xAA80, RangeEnd = 0xAAC2, Script = Scripts[135]},
                new CodepointScript {RangeStart = 0xAADB, RangeEnd = 0xAADF, Script = Scripts[135]},
                new CodepointScript {RangeStart = 0xAAE0, RangeEnd = 0xAAF6, Script = Scripts[86]},
                new CodepointScript {RangeStart = 0xAB01, RangeEnd = 0xAB06, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0xAB09, RangeEnd = 0xAB0E, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0xAB11, RangeEnd = 0xAB16, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0xAB20, RangeEnd = 0xAB26, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0xAB28, RangeEnd = 0xAB2E, Script = Scripts[33]},
                new CodepointScript {RangeStart = 0xAB30, RangeEnd = 0xAB5A, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xAB5B, RangeEnd = 0xAB5B, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xAB5C, RangeEnd = 0xAB64, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xAB65, RangeEnd = 0xAB65, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0xAB66, RangeEnd = 0xAB67, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xAB70, RangeEnd = 0xABBF, Script = Scripts[22]},
                new CodepointScript {RangeStart = 0xABC0, RangeEnd = 0xABED, Script = Scripts[86]},
                new CodepointScript {RangeStart = 0xABF0, RangeEnd = 0xABF9, Script = Scripts[86]},
                new CodepointScript {RangeStart = 0xAC00, RangeEnd = 0xD7A3, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0xD7B0, RangeEnd = 0xD7C6, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0xD7CB, RangeEnd = 0xD7FB, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0xF900, RangeEnd = 0xFA6D, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0xFA70, RangeEnd = 0xFAD9, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0xFB00, RangeEnd = 0xFB06, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xFB13, RangeEnd = 0xFB17, Script = Scripts[5]},
                new CodepointScript {RangeStart = 0xFB1D, RangeEnd = 0xFB36, Script = Scripts[47]},
                new CodepointScript {RangeStart = 0xFB38, RangeEnd = 0xFB3C, Script = Scripts[47]},
                new CodepointScript {RangeStart = 0xFB3E, RangeEnd = 0xFB3E, Script = Scripts[47]},
                new CodepointScript {RangeStart = 0xFB40, RangeEnd = 0xFB41, Script = Scripts[47]},
                new CodepointScript {RangeStart = 0xFB43, RangeEnd = 0xFB44, Script = Scripts[47]},
                new CodepointScript {RangeStart = 0xFB46, RangeEnd = 0xFB4F, Script = Scripts[47]},
                new CodepointScript {RangeStart = 0xFB50, RangeEnd = 0xFBC1, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0xFBD3, RangeEnd = 0xFD3D, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0xFD3E, RangeEnd = 0xFD3F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFD50, RangeEnd = 0xFD8F, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0xFD92, RangeEnd = 0xFDC7, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0xFDF0, RangeEnd = 0xFDFD, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0xFE00, RangeEnd = 0xFE0F, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0xFE10, RangeEnd = 0xFE19, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFE20, RangeEnd = 0xFE2D, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0xFE2E, RangeEnd = 0xFE2F, Script = Scripts[25]},
                new CodepointScript {RangeStart = 0xFE30, RangeEnd = 0xFE52, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFE54, RangeEnd = 0xFE66, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFE68, RangeEnd = 0xFE6B, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFE70, RangeEnd = 0xFE74, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0xFE76, RangeEnd = 0xFEFC, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0xFEFF, RangeEnd = 0xFEFF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFF01, RangeEnd = 0xFF20, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFF21, RangeEnd = 0xFF3A, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xFF3B, RangeEnd = 0xFF40, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFF41, RangeEnd = 0xFF5A, Script = Scripts[65]},
                new CodepointScript {RangeStart = 0xFF5B, RangeEnd = 0xFF65, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFF66, RangeEnd = 0xFF6F, Script = Scripts[57]},
                new CodepointScript {RangeStart = 0xFF70, RangeEnd = 0xFF70, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFF71, RangeEnd = 0xFF9D, Script = Scripts[57]},
                new CodepointScript {RangeStart = 0xFF9E, RangeEnd = 0xFF9F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFFA0, RangeEnd = 0xFFBE, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0xFFC2, RangeEnd = 0xFFC7, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0xFFCA, RangeEnd = 0xFFCF, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0xFFD2, RangeEnd = 0xFFD7, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0xFFDA, RangeEnd = 0xFFDC, Script = Scripts[43]},
                new CodepointScript {RangeStart = 0xFFE0, RangeEnd = 0xFFE6, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFFE8, RangeEnd = 0xFFEE, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xFFF9, RangeEnd = 0xFFFD, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x10000, RangeEnd = 0x1000B, Script = Scripts[69]},
                new CodepointScript {RangeStart = 0x1000D, RangeEnd = 0x10026, Script = Scripts[69]},
                new CodepointScript {RangeStart = 0x10028, RangeEnd = 0x1003A, Script = Scripts[69]},
                new CodepointScript {RangeStart = 0x1003C, RangeEnd = 0x1003D, Script = Scripts[69]},
                new CodepointScript {RangeStart = 0x1003F, RangeEnd = 0x1004D, Script = Scripts[69]},
                new CodepointScript {RangeStart = 0x10050, RangeEnd = 0x1005D, Script = Scripts[69]},
                new CodepointScript {RangeStart = 0x10080, RangeEnd = 0x100FA, Script = Scripts[69]},
                new CodepointScript {RangeStart = 0x10100, RangeEnd = 0x10102, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x10107, RangeEnd = 0x10133, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x10137, RangeEnd = 0x1013F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x10140, RangeEnd = 0x1018E, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x10190, RangeEnd = 0x1019B, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x101A0, RangeEnd = 0x101A0, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x101D0, RangeEnd = 0x101FC, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x101FD, RangeEnd = 0x101FD, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x10280, RangeEnd = 0x1029C, Script = Scripts[71]},
                new CodepointScript {RangeStart = 0x102A0, RangeEnd = 0x102D0, Script = Scripts[20]},
                new CodepointScript {RangeStart = 0x102E0, RangeEnd = 0x102E0, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x102E1, RangeEnd = 0x102FB, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x10300, RangeEnd = 0x10323, Script = Scripts[54]},
                new CodepointScript {RangeStart = 0x1032D, RangeEnd = 0x1032F, Script = Scripts[54]},
                new CodepointScript {RangeStart = 0x10330, RangeEnd = 0x1034A, Script = Scripts[38]},
                new CodepointScript {RangeStart = 0x10350, RangeEnd = 0x1037A, Script = Scripts[103]},
                new CodepointScript {RangeStart = 0x10380, RangeEnd = 0x1039D, Script = Scripts[143]},
                new CodepointScript {RangeStart = 0x1039F, RangeEnd = 0x1039F, Script = Scripts[143]},
                new CodepointScript {RangeStart = 0x103A0, RangeEnd = 0x103C3, Script = Scripts[147]},
                new CodepointScript {RangeStart = 0x103C8, RangeEnd = 0x103D5, Script = Scripts[147]},
                new CodepointScript {RangeStart = 0x10400, RangeEnd = 0x1044F, Script = Scripts[28]},
                new CodepointScript {RangeStart = 0x10450, RangeEnd = 0x1047F, Script = Scripts[117]},
                new CodepointScript {RangeStart = 0x10480, RangeEnd = 0x1049D, Script = Scripts[100]},
                new CodepointScript {RangeStart = 0x104A0, RangeEnd = 0x104A9, Script = Scripts[100]},
                new CodepointScript {RangeStart = 0x104B0, RangeEnd = 0x104D3, Script = Scripts[99]},
                new CodepointScript {RangeStart = 0x104D8, RangeEnd = 0x104FB, Script = Scripts[99]},
                new CodepointScript {RangeStart = 0x10500, RangeEnd = 0x10527, Script = Scripts[31]},
                new CodepointScript {RangeStart = 0x10530, RangeEnd = 0x10563, Script = Scripts[1]},
                new CodepointScript {RangeStart = 0x1056F, RangeEnd = 0x1056F, Script = Scripts[1]},
                new CodepointScript {RangeStart = 0x10600, RangeEnd = 0x10736, Script = Scripts[68]},
                new CodepointScript {RangeStart = 0x10740, RangeEnd = 0x10755, Script = Scripts[68]},
                new CodepointScript {RangeStart = 0x10760, RangeEnd = 0x10767, Script = Scripts[68]},
                new CodepointScript {RangeStart = 0x10800, RangeEnd = 0x10805, Script = Scripts[24]},
                new CodepointScript {RangeStart = 0x10808, RangeEnd = 0x10808, Script = Scripts[24]},
                new CodepointScript {RangeStart = 0x1080A, RangeEnd = 0x10835, Script = Scripts[24]},
                new CodepointScript {RangeStart = 0x10837, RangeEnd = 0x10838, Script = Scripts[24]},
                new CodepointScript {RangeStart = 0x1083C, RangeEnd = 0x1083C, Script = Scripts[24]},
                new CodepointScript {RangeStart = 0x1083F, RangeEnd = 0x1083F, Script = Scripts[24]},
                new CodepointScript {RangeStart = 0x10840, RangeEnd = 0x10855, Script = Scripts[4]},
                new CodepointScript {RangeStart = 0x10857, RangeEnd = 0x1085F, Script = Scripts[4]},
                new CodepointScript {RangeStart = 0x10860, RangeEnd = 0x1087F, Script = Scripts[101]},
                new CodepointScript {RangeStart = 0x10880, RangeEnd = 0x1089E, Script = Scripts[91]},
                new CodepointScript {RangeStart = 0x108A7, RangeEnd = 0x108AF, Script = Scripts[91]},
                new CodepointScript {RangeStart = 0x108E0, RangeEnd = 0x108F2, Script = Scripts[46]},
                new CodepointScript {RangeStart = 0x108F4, RangeEnd = 0x108F5, Script = Scripts[46]},
                new CodepointScript {RangeStart = 0x108FB, RangeEnd = 0x108FF, Script = Scripts[46]},
                new CodepointScript {RangeStart = 0x10900, RangeEnd = 0x1091B, Script = Scripts[107]},
                new CodepointScript {RangeStart = 0x1091F, RangeEnd = 0x1091F, Script = Scripts[107]},
                new CodepointScript {RangeStart = 0x10920, RangeEnd = 0x10939, Script = Scripts[72]},
                new CodepointScript {RangeStart = 0x1093F, RangeEnd = 0x1093F, Script = Scripts[72]},
                new CodepointScript {RangeStart = 0x10980, RangeEnd = 0x1099F, Script = Scripts[81]},
                new CodepointScript {RangeStart = 0x109A0, RangeEnd = 0x109B7, Script = Scripts[80]},
                new CodepointScript {RangeStart = 0x109BC, RangeEnd = 0x109CF, Script = Scripts[80]},
                new CodepointScript {RangeStart = 0x109D2, RangeEnd = 0x109FF, Script = Scripts[80]},
                new CodepointScript {RangeStart = 0x10A00, RangeEnd = 0x10A03, Script = Scripts[58]},
                new CodepointScript {RangeStart = 0x10A05, RangeEnd = 0x10A06, Script = Scripts[58]},
                new CodepointScript {RangeStart = 0x10A0C, RangeEnd = 0x10A13, Script = Scripts[58]},
                new CodepointScript {RangeStart = 0x10A15, RangeEnd = 0x10A17, Script = Scripts[58]},
                new CodepointScript {RangeStart = 0x10A19, RangeEnd = 0x10A35, Script = Scripts[58]},
                new CodepointScript {RangeStart = 0x10A38, RangeEnd = 0x10A3A, Script = Scripts[58]},
                new CodepointScript {RangeStart = 0x10A3F, RangeEnd = 0x10A48, Script = Scripts[58]},
                new CodepointScript {RangeStart = 0x10A50, RangeEnd = 0x10A58, Script = Scripts[58]},
                new CodepointScript {RangeStart = 0x10A60, RangeEnd = 0x10A7F, Script = Scripts[114]},
                new CodepointScript {RangeStart = 0x10A80, RangeEnd = 0x10A9F, Script = Scripts[90]},
                new CodepointScript {RangeStart = 0x10AC0, RangeEnd = 0x10AE6, Script = Scripts[76]},
                new CodepointScript {RangeStart = 0x10AEB, RangeEnd = 0x10AF6, Script = Scripts[76]},
                new CodepointScript {RangeStart = 0x10B00, RangeEnd = 0x10B35, Script = Scripts[6]},
                new CodepointScript {RangeStart = 0x10B39, RangeEnd = 0x10B3F, Script = Scripts[6]},
                new CodepointScript {RangeStart = 0x10B40, RangeEnd = 0x10B55, Script = Scripts[109]},
                new CodepointScript {RangeStart = 0x10B58, RangeEnd = 0x10B5F, Script = Scripts[109]},
                new CodepointScript {RangeStart = 0x10B60, RangeEnd = 0x10B72, Script = Scripts[105]},
                new CodepointScript {RangeStart = 0x10B78, RangeEnd = 0x10B7F, Script = Scripts[105]},
                new CodepointScript {RangeStart = 0x10B80, RangeEnd = 0x10B91, Script = Scripts[106]},
                new CodepointScript {RangeStart = 0x10B99, RangeEnd = 0x10B9C, Script = Scripts[106]},
                new CodepointScript {RangeStart = 0x10BA9, RangeEnd = 0x10BAF, Script = Scripts[106]},
                new CodepointScript {RangeStart = 0x10C00, RangeEnd = 0x10C48, Script = Scripts[97]},
                new CodepointScript {RangeStart = 0x10C80, RangeEnd = 0x10CB2, Script = Scripts[53]},
                new CodepointScript {RangeStart = 0x10CC0, RangeEnd = 0x10CF2, Script = Scripts[53]},
                new CodepointScript {RangeStart = 0x10CFA, RangeEnd = 0x10CFF, Script = Scripts[53]},
                new CodepointScript {RangeStart = 0x10D00, RangeEnd = 0x10D27, Script = Scripts[111]},
                new CodepointScript {RangeStart = 0x10D30, RangeEnd = 0x10D39, Script = Scripts[111]},
                new CodepointScript {RangeStart = 0x10E60, RangeEnd = 0x10E7E, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x10F00, RangeEnd = 0x10F27, Script = Scripts[123]},
                new CodepointScript {RangeStart = 0x10F30, RangeEnd = 0x10F59, Script = Scripts[122]},
                new CodepointScript {RangeStart = 0x10FE0, RangeEnd = 0x10FF6, Script = Scripts[32]},
                new CodepointScript {RangeStart = 0x11000, RangeEnd = 0x1104D, Script = Scripts[14]},
                new CodepointScript {RangeStart = 0x11052, RangeEnd = 0x1106F, Script = Scripts[14]},
                new CodepointScript {RangeStart = 0x1107F, RangeEnd = 0x1107F, Script = Scripts[14]},
                new CodepointScript {RangeStart = 0x11080, RangeEnd = 0x110C1, Script = Scripts[62]},
                new CodepointScript {RangeStart = 0x110CD, RangeEnd = 0x110CD, Script = Scripts[62]},
                new CodepointScript {RangeStart = 0x110D0, RangeEnd = 0x110E8, Script = Scripts[124]},
                new CodepointScript {RangeStart = 0x110F0, RangeEnd = 0x110F9, Script = Scripts[124]},
                new CodepointScript {RangeStart = 0x11100, RangeEnd = 0x11134, Script = Scripts[18]},
                new CodepointScript {RangeStart = 0x11136, RangeEnd = 0x11146, Script = Scripts[18]},
                new CodepointScript {RangeStart = 0x11150, RangeEnd = 0x11176, Script = Scripts[73]},
                new CodepointScript {RangeStart = 0x11180, RangeEnd = 0x111CD, Script = Scripts[118]},
                new CodepointScript {RangeStart = 0x111D0, RangeEnd = 0x111DF, Script = Scripts[118]},
                new CodepointScript {RangeStart = 0x111E1, RangeEnd = 0x111F4, Script = Scripts[121]},
                new CodepointScript {RangeStart = 0x11200, RangeEnd = 0x11211, Script = Scripts[60]},
                new CodepointScript {RangeStart = 0x11213, RangeEnd = 0x1123E, Script = Scripts[60]},
                new CodepointScript {RangeStart = 0x11280, RangeEnd = 0x11286, Script = Scripts[87]},
                new CodepointScript {RangeStart = 0x11288, RangeEnd = 0x11288, Script = Scripts[87]},
                new CodepointScript {RangeStart = 0x1128A, RangeEnd = 0x1128D, Script = Scripts[87]},
                new CodepointScript {RangeStart = 0x1128F, RangeEnd = 0x1129D, Script = Scripts[87]},
                new CodepointScript {RangeStart = 0x1129F, RangeEnd = 0x112A9, Script = Scripts[87]},
                new CodepointScript {RangeStart = 0x112B0, RangeEnd = 0x112EA, Script = Scripts[120]},
                new CodepointScript {RangeStart = 0x112F0, RangeEnd = 0x112F9, Script = Scripts[120]},
                new CodepointScript {RangeStart = 0x11300, RangeEnd = 0x11303, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x11305, RangeEnd = 0x1130C, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x1130F, RangeEnd = 0x11310, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x11313, RangeEnd = 0x11328, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x1132A, RangeEnd = 0x11330, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x11332, RangeEnd = 0x11333, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x11335, RangeEnd = 0x11339, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x1133B, RangeEnd = 0x1133B, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1133C, RangeEnd = 0x11344, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x11347, RangeEnd = 0x11348, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x1134B, RangeEnd = 0x1134D, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x11350, RangeEnd = 0x11350, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x11357, RangeEnd = 0x11357, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x1135D, RangeEnd = 0x11363, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x11366, RangeEnd = 0x1136C, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x11370, RangeEnd = 0x11374, Script = Scripts[39]},
                new CodepointScript {RangeStart = 0x11400, RangeEnd = 0x11459, Script = Scripts[92]},
                new CodepointScript {RangeStart = 0x1145B, RangeEnd = 0x1145B, Script = Scripts[92]},
                new CodepointScript {RangeStart = 0x1145D, RangeEnd = 0x1145F, Script = Scripts[92]},
                new CodepointScript {RangeStart = 0x11480, RangeEnd = 0x114C7, Script = Scripts[142]},
                new CodepointScript {RangeStart = 0x114D0, RangeEnd = 0x114D9, Script = Scripts[142]},
                new CodepointScript {RangeStart = 0x11580, RangeEnd = 0x115B5, Script = Scripts[119]},
                new CodepointScript {RangeStart = 0x115B8, RangeEnd = 0x115DD, Script = Scripts[119]},
                new CodepointScript {RangeStart = 0x11600, RangeEnd = 0x11644, Script = Scripts[83]},
                new CodepointScript {RangeStart = 0x11650, RangeEnd = 0x11659, Script = Scripts[83]},
                new CodepointScript {RangeStart = 0x11660, RangeEnd = 0x1166C, Script = Scripts[84]},
                new CodepointScript {RangeStart = 0x11680, RangeEnd = 0x116B8, Script = Scripts[130]},
                new CodepointScript {RangeStart = 0x116C0, RangeEnd = 0x116C9, Script = Scripts[130]},
                new CodepointScript {RangeStart = 0x11700, RangeEnd = 0x1171A, Script = Scripts[2]},
                new CodepointScript {RangeStart = 0x1171D, RangeEnd = 0x1172B, Script = Scripts[2]},
                new CodepointScript {RangeStart = 0x11730, RangeEnd = 0x1173F, Script = Scripts[2]},
                new CodepointScript {RangeStart = 0x11800, RangeEnd = 0x1183B, Script = Scripts[27]},
                new CodepointScript {RangeStart = 0x118A0, RangeEnd = 0x118F2, Script = Scripts[145]},
                new CodepointScript {RangeStart = 0x118FF, RangeEnd = 0x118FF, Script = Scripts[145]},
                new CodepointScript {RangeStart = 0x119A0, RangeEnd = 0x119A7, Script = Scripts[89]},
                new CodepointScript {RangeStart = 0x119AA, RangeEnd = 0x119D7, Script = Scripts[89]},
                new CodepointScript {RangeStart = 0x119DA, RangeEnd = 0x119E4, Script = Scripts[89]},
                new CodepointScript {RangeStart = 0x11A00, RangeEnd = 0x11A47, Script = Scripts[150]},
                new CodepointScript {RangeStart = 0x11A50, RangeEnd = 0x11AA2, Script = Scripts[125]},
                new CodepointScript {RangeStart = 0x11AC0, RangeEnd = 0x11AF8, Script = Scripts[102]},
                new CodepointScript {RangeStart = 0x11C00, RangeEnd = 0x11C08, Script = Scripts[12]},
                new CodepointScript {RangeStart = 0x11C0A, RangeEnd = 0x11C36, Script = Scripts[12]},
                new CodepointScript {RangeStart = 0x11C38, RangeEnd = 0x11C45, Script = Scripts[12]},
                new CodepointScript {RangeStart = 0x11C50, RangeEnd = 0x11C6C, Script = Scripts[12]},
                new CodepointScript {RangeStart = 0x11C70, RangeEnd = 0x11C8F, Script = Scripts[77]},
                new CodepointScript {RangeStart = 0x11C92, RangeEnd = 0x11CA7, Script = Scripts[77]},
                new CodepointScript {RangeStart = 0x11CA9, RangeEnd = 0x11CB6, Script = Scripts[77]},
                new CodepointScript {RangeStart = 0x11D00, RangeEnd = 0x11D06, Script = Scripts[37]},
                new CodepointScript {RangeStart = 0x11D08, RangeEnd = 0x11D09, Script = Scripts[37]},
                new CodepointScript {RangeStart = 0x11D0B, RangeEnd = 0x11D36, Script = Scripts[37]},
                new CodepointScript {RangeStart = 0x11D3A, RangeEnd = 0x11D3A, Script = Scripts[37]},
                new CodepointScript {RangeStart = 0x11D3C, RangeEnd = 0x11D3D, Script = Scripts[37]},
                new CodepointScript {RangeStart = 0x11D3F, RangeEnd = 0x11D47, Script = Scripts[37]},
                new CodepointScript {RangeStart = 0x11D50, RangeEnd = 0x11D59, Script = Scripts[37]},
                new CodepointScript {RangeStart = 0x11D60, RangeEnd = 0x11D65, Script = Scripts[36]},
                new CodepointScript {RangeStart = 0x11D67, RangeEnd = 0x11D68, Script = Scripts[36]},
                new CodepointScript {RangeStart = 0x11D6A, RangeEnd = 0x11D8E, Script = Scripts[36]},
                new CodepointScript {RangeStart = 0x11D90, RangeEnd = 0x11D91, Script = Scripts[36]},
                new CodepointScript {RangeStart = 0x11D93, RangeEnd = 0x11D98, Script = Scripts[36]},
                new CodepointScript {RangeStart = 0x11DA0, RangeEnd = 0x11DA9, Script = Scripts[36]},
                new CodepointScript {RangeStart = 0x11EE0, RangeEnd = 0x11EF8, Script = Scripts[74]},
                new CodepointScript {RangeStart = 0x11FC0, RangeEnd = 0x11FF1, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0x11FFF, RangeEnd = 0x11FFF, Script = Scripts[133]},
                new CodepointScript {RangeStart = 0x12000, RangeEnd = 0x12399, Script = Scripts[148]},
                new CodepointScript {RangeStart = 0x12400, RangeEnd = 0x1246E, Script = Scripts[148]},
                new CodepointScript {RangeStart = 0x12470, RangeEnd = 0x12474, Script = Scripts[148]},
                new CodepointScript {RangeStart = 0x12480, RangeEnd = 0x12543, Script = Scripts[148]},
                new CodepointScript {RangeStart = 0x13000, RangeEnd = 0x1342E, Script = Scripts[30]},
                new CodepointScript {RangeStart = 0x13430, RangeEnd = 0x13438, Script = Scripts[30]},
                new CodepointScript {RangeStart = 0x14400, RangeEnd = 0x14646, Script = Scripts[49]},
                new CodepointScript {RangeStart = 0x16800, RangeEnd = 0x16A38, Script = Scripts[8]},
                new CodepointScript {RangeStart = 0x16A40, RangeEnd = 0x16A5E, Script = Scripts[85]},
                new CodepointScript {RangeStart = 0x16A60, RangeEnd = 0x16A69, Script = Scripts[85]},
                new CodepointScript {RangeStart = 0x16A6E, RangeEnd = 0x16A6F, Script = Scripts[85]},
                new CodepointScript {RangeStart = 0x16AD0, RangeEnd = 0x16AED, Script = Scripts[9]},
                new CodepointScript {RangeStart = 0x16AF0, RangeEnd = 0x16AF5, Script = Scripts[9]},
                new CodepointScript {RangeStart = 0x16B00, RangeEnd = 0x16B45, Script = Scripts[50]},
                new CodepointScript {RangeStart = 0x16B50, RangeEnd = 0x16B59, Script = Scripts[50]},
                new CodepointScript {RangeStart = 0x16B5B, RangeEnd = 0x16B61, Script = Scripts[50]},
                new CodepointScript {RangeStart = 0x16B63, RangeEnd = 0x16B77, Script = Scripts[50]},
                new CodepointScript {RangeStart = 0x16B7D, RangeEnd = 0x16B8F, Script = Scripts[50]},
                new CodepointScript {RangeStart = 0x16E40, RangeEnd = 0x16E9A, Script = Scripts[78]},
                new CodepointScript {RangeStart = 0x16F00, RangeEnd = 0x16F4A, Script = Scripts[108]},
                new CodepointScript {RangeStart = 0x16F4F, RangeEnd = 0x16F87, Script = Scripts[108]},
                new CodepointScript {RangeStart = 0x16F8F, RangeEnd = 0x16F9F, Script = Scripts[108]},
                new CodepointScript {RangeStart = 0x16FE0, RangeEnd = 0x16FE0, Script = Scripts[134]},
                new CodepointScript {RangeStart = 0x16FE1, RangeEnd = 0x16FE1, Script = Scripts[94]},
                new CodepointScript {RangeStart = 0x16FE2, RangeEnd = 0x16FE3, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x17000, RangeEnd = 0x187F7, Script = Scripts[134]},
                new CodepointScript {RangeStart = 0x18800, RangeEnd = 0x18AF2, Script = Scripts[134]},
                new CodepointScript {RangeStart = 0x1B000, RangeEnd = 0x1B000, Script = Scripts[57]},
                new CodepointScript {RangeStart = 0x1B001, RangeEnd = 0x1B11E, Script = Scripts[48]},
                new CodepointScript {RangeStart = 0x1B150, RangeEnd = 0x1B152, Script = Scripts[48]},
                new CodepointScript {RangeStart = 0x1B164, RangeEnd = 0x1B167, Script = Scripts[57]},
                new CodepointScript {RangeStart = 0x1B170, RangeEnd = 0x1B2FB, Script = Scripts[94]},
                new CodepointScript {RangeStart = 0x1BC00, RangeEnd = 0x1BC6A, Script = Scripts[29]},
                new CodepointScript {RangeStart = 0x1BC70, RangeEnd = 0x1BC7C, Script = Scripts[29]},
                new CodepointScript {RangeStart = 0x1BC80, RangeEnd = 0x1BC88, Script = Scripts[29]},
                new CodepointScript {RangeStart = 0x1BC90, RangeEnd = 0x1BC99, Script = Scripts[29]},
                new CodepointScript {RangeStart = 0x1BC9C, RangeEnd = 0x1BC9F, Script = Scripts[29]},
                new CodepointScript {RangeStart = 0x1BCA0, RangeEnd = 0x1BCA3, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D000, RangeEnd = 0x1D0F5, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D100, RangeEnd = 0x1D126, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D129, RangeEnd = 0x1D166, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D167, RangeEnd = 0x1D169, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1D16A, RangeEnd = 0x1D17A, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D17B, RangeEnd = 0x1D182, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1D183, RangeEnd = 0x1D184, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D185, RangeEnd = 0x1D18B, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1D18C, RangeEnd = 0x1D1A9, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D1AA, RangeEnd = 0x1D1AD, Script = Scripts[151]},
                new CodepointScript {RangeStart = 0x1D1AE, RangeEnd = 0x1D1E8, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D200, RangeEnd = 0x1D245, Script = Scripts[40]},
                new CodepointScript {RangeStart = 0x1D2E0, RangeEnd = 0x1D2F3, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D300, RangeEnd = 0x1D356, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D360, RangeEnd = 0x1D378, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D400, RangeEnd = 0x1D454, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D456, RangeEnd = 0x1D49C, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D49E, RangeEnd = 0x1D49F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D4A2, RangeEnd = 0x1D4A2, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D4A5, RangeEnd = 0x1D4A6, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D4A9, RangeEnd = 0x1D4AC, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D4AE, RangeEnd = 0x1D4B9, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D4BB, RangeEnd = 0x1D4BB, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D4BD, RangeEnd = 0x1D4C3, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D4C5, RangeEnd = 0x1D505, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D507, RangeEnd = 0x1D50A, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D50D, RangeEnd = 0x1D514, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D516, RangeEnd = 0x1D51C, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D51E, RangeEnd = 0x1D539, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D53B, RangeEnd = 0x1D53E, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D540, RangeEnd = 0x1D544, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D546, RangeEnd = 0x1D546, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D54A, RangeEnd = 0x1D550, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D552, RangeEnd = 0x1D6A5, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D6A8, RangeEnd = 0x1D7CB, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D7CE, RangeEnd = 0x1D7FF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1D800, RangeEnd = 0x1DA8B, Script = Scripts[116]},
                new CodepointScript {RangeStart = 0x1DA9B, RangeEnd = 0x1DA9F, Script = Scripts[116]},
                new CodepointScript {RangeStart = 0x1DAA1, RangeEnd = 0x1DAAF, Script = Scripts[116]},
                new CodepointScript {RangeStart = 0x1E000, RangeEnd = 0x1E006, Script = Scripts[35]},
                new CodepointScript {RangeStart = 0x1E008, RangeEnd = 0x1E018, Script = Scripts[35]},
                new CodepointScript {RangeStart = 0x1E01B, RangeEnd = 0x1E021, Script = Scripts[35]},
                new CodepointScript {RangeStart = 0x1E023, RangeEnd = 0x1E024, Script = Scripts[35]},
                new CodepointScript {RangeStart = 0x1E026, RangeEnd = 0x1E02A, Script = Scripts[35]},
                new CodepointScript {RangeStart = 0x1E100, RangeEnd = 0x1E12C, Script = Scripts[51]},
                new CodepointScript {RangeStart = 0x1E130, RangeEnd = 0x1E13D, Script = Scripts[51]},
                new CodepointScript {RangeStart = 0x1E140, RangeEnd = 0x1E149, Script = Scripts[51]},
                new CodepointScript {RangeStart = 0x1E14E, RangeEnd = 0x1E14F, Script = Scripts[51]},
                new CodepointScript {RangeStart = 0x1E2C0, RangeEnd = 0x1E2F9, Script = Scripts[146]},
                new CodepointScript {RangeStart = 0x1E2FF, RangeEnd = 0x1E2FF, Script = Scripts[146]},
                new CodepointScript {RangeStart = 0x1E800, RangeEnd = 0x1E8C4, Script = Scripts[79]},
                new CodepointScript {RangeStart = 0x1E8C7, RangeEnd = 0x1E8D6, Script = Scripts[79]},
                new CodepointScript {RangeStart = 0x1E900, RangeEnd = 0x1E94B, Script = Scripts[0]},
                new CodepointScript {RangeStart = 0x1E950, RangeEnd = 0x1E959, Script = Scripts[0]},
                new CodepointScript {RangeStart = 0x1E95E, RangeEnd = 0x1E95F, Script = Scripts[0]},
                new CodepointScript {RangeStart = 0x1EC71, RangeEnd = 0x1ECB4, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1ED01, RangeEnd = 0x1ED3D, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1EE00, RangeEnd = 0x1EE03, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE05, RangeEnd = 0x1EE1F, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE21, RangeEnd = 0x1EE22, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE24, RangeEnd = 0x1EE24, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE27, RangeEnd = 0x1EE27, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE29, RangeEnd = 0x1EE32, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE34, RangeEnd = 0x1EE37, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE39, RangeEnd = 0x1EE39, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE3B, RangeEnd = 0x1EE3B, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE42, RangeEnd = 0x1EE42, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE47, RangeEnd = 0x1EE47, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE49, RangeEnd = 0x1EE49, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE4B, RangeEnd = 0x1EE4B, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE4D, RangeEnd = 0x1EE4F, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE51, RangeEnd = 0x1EE52, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE54, RangeEnd = 0x1EE54, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE57, RangeEnd = 0x1EE57, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE59, RangeEnd = 0x1EE59, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE5B, RangeEnd = 0x1EE5B, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE5D, RangeEnd = 0x1EE5D, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE5F, RangeEnd = 0x1EE5F, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE61, RangeEnd = 0x1EE62, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE64, RangeEnd = 0x1EE64, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE67, RangeEnd = 0x1EE6A, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE6C, RangeEnd = 0x1EE72, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE74, RangeEnd = 0x1EE77, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE79, RangeEnd = 0x1EE7C, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE7E, RangeEnd = 0x1EE7E, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE80, RangeEnd = 0x1EE89, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EE8B, RangeEnd = 0x1EE9B, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EEA1, RangeEnd = 0x1EEA3, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EEA5, RangeEnd = 0x1EEA9, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EEAB, RangeEnd = 0x1EEBB, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1EEF0, RangeEnd = 0x1EEF1, Script = Scripts[3]},
                new CodepointScript {RangeStart = 0x1F000, RangeEnd = 0x1F02B, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F030, RangeEnd = 0x1F093, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F0A0, RangeEnd = 0x1F0AE, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F0B1, RangeEnd = 0x1F0BF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F0C1, RangeEnd = 0x1F0CF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F0D1, RangeEnd = 0x1F0F5, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F100, RangeEnd = 0x1F10C, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F110, RangeEnd = 0x1F16C, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F170, RangeEnd = 0x1F1AC, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F1E6, RangeEnd = 0x1F1FF, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F200, RangeEnd = 0x1F200, Script = Scripts[48]},
                new CodepointScript {RangeStart = 0x1F201, RangeEnd = 0x1F202, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F210, RangeEnd = 0x1F23B, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F240, RangeEnd = 0x1F248, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F250, RangeEnd = 0x1F251, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F260, RangeEnd = 0x1F265, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F300, RangeEnd = 0x1F6D5, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F6E0, RangeEnd = 0x1F6EC, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F6F0, RangeEnd = 0x1F6FA, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F700, RangeEnd = 0x1F773, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F780, RangeEnd = 0x1F7D8, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F7E0, RangeEnd = 0x1F7EB, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F800, RangeEnd = 0x1F80B, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F810, RangeEnd = 0x1F847, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F850, RangeEnd = 0x1F859, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F860, RangeEnd = 0x1F887, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F890, RangeEnd = 0x1F8AD, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F900, RangeEnd = 0x1F90B, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F90D, RangeEnd = 0x1F971, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F973, RangeEnd = 0x1F976, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F97A, RangeEnd = 0x1F9A2, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F9A5, RangeEnd = 0x1F9AA, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F9AE, RangeEnd = 0x1F9CA, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1F9CD, RangeEnd = 0x1FA53, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1FA60, RangeEnd = 0x1FA6D, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1FA70, RangeEnd = 0x1FA73, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1FA78, RangeEnd = 0x1FA7A, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1FA80, RangeEnd = 0x1FA82, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x1FA90, RangeEnd = 0x1FA95, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0x20000, RangeEnd = 0x2A6D6, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x2A700, RangeEnd = 0x2B734, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x2B740, RangeEnd = 0x2B81D, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x2B820, RangeEnd = 0x2CEA1, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x2CEB0, RangeEnd = 0x2EBE0, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0x2F800, RangeEnd = 0x2FA1D, Script = Scripts[44]},
                new CodepointScript {RangeStart = 0xE0001, RangeEnd = 0xE0001, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xE0020, RangeEnd = 0xE007F, Script = Scripts[152]},
                new CodepointScript {RangeStart = 0xE0100, RangeEnd = 0xE01EF, Script = Scripts[151]},

            };

        /// <summary>
        /// Derived from Unicode data, see https://github.com/DaniRK/UnicodeScriptDetectorNet/tree/master/ImportDataToCSharp
        /// </summary>
        private static readonly CodepointScriptExtended[] CodepointScriptsExtended = new CodepointScriptExtended[]
            {

                new CodepointScriptExtended {RangeStart = 0x342, RangeEnd = 0x342, ScriptNamesShort = new string[]{"Grek", }},
                new CodepointScriptExtended {RangeStart = 0x345, RangeEnd = 0x345, ScriptNamesShort = new string[]{"Grek", }},
                new CodepointScriptExtended {RangeStart = 0x363, RangeEnd = 0x36F, ScriptNamesShort = new string[]{"Latn", }},
                new CodepointScriptExtended {RangeStart = 0x483, RangeEnd = 0x483, ScriptNamesShort = new string[]{"Cyrl", "Perm", }},
                new CodepointScriptExtended {RangeStart = 0x484, RangeEnd = 0x484, ScriptNamesShort = new string[]{"Cyrl", "Glag", }},
                new CodepointScriptExtended {RangeStart = 0x485, RangeEnd = 0x486, ScriptNamesShort = new string[]{"Cyrl", "Latn", }},
                new CodepointScriptExtended {RangeStart = 0x487, RangeEnd = 0x487, ScriptNamesShort = new string[]{"Cyrl", "Glag", }},
                new CodepointScriptExtended {RangeStart = 0x589, RangeEnd = 0x589, ScriptNamesShort = new string[]{"Armn", "Geor", }},
                new CodepointScriptExtended {RangeStart = 0x60C, RangeEnd = 0x60C, ScriptNamesShort = new string[]{"Arab", "Rohg", "Syrc", "Thaa", }},
                new CodepointScriptExtended {RangeStart = 0x61B, RangeEnd = 0x61B, ScriptNamesShort = new string[]{"Arab", "Rohg", "Syrc", "Thaa", }},
                new CodepointScriptExtended {RangeStart = 0x61C, RangeEnd = 0x61C, ScriptNamesShort = new string[]{"Arab", "Syrc", "Thaa", }},
                new CodepointScriptExtended {RangeStart = 0x61F, RangeEnd = 0x61F, ScriptNamesShort = new string[]{"Arab", "Rohg", "Syrc", "Thaa", }},
                new CodepointScriptExtended {RangeStart = 0x640, RangeEnd = 0x640, ScriptNamesShort = new string[]{"Adlm", "Arab", "Mand", "Mani", "Phlp", "Rohg", "Sogd", "Syrc", }},
                new CodepointScriptExtended {RangeStart = 0x64B, RangeEnd = 0x655, ScriptNamesShort = new string[]{"Arab", "Syrc", }},
                new CodepointScriptExtended {RangeStart = 0x660, RangeEnd = 0x669, ScriptNamesShort = new string[]{"Arab", "Thaa", }},
                new CodepointScriptExtended {RangeStart = 0x670, RangeEnd = 0x670, ScriptNamesShort = new string[]{"Arab", "Syrc", }},
                new CodepointScriptExtended {RangeStart = 0x6D4, RangeEnd = 0x6D4, ScriptNamesShort = new string[]{"Arab", "Rohg", }},
                new CodepointScriptExtended {RangeStart = 0x951, RangeEnd = 0x951, ScriptNamesShort = new string[]{"Beng", "Deva", "Gran", "Gujr", "Guru", "Knda", "Latn", "Mlym", "Orya", "Shrd", "Taml", "Telu", "Tirh", }},
                new CodepointScriptExtended {RangeStart = 0x952, RangeEnd = 0x952, ScriptNamesShort = new string[]{"Beng", "Deva", "Gran", "Gujr", "Guru", "Knda", "Latn", "Mlym", "Orya", "Taml", "Telu", "Tirh", }},
                new CodepointScriptExtended {RangeStart = 0x964, RangeEnd = 0x964, ScriptNamesShort = new string[]{"Beng", "Deva", "Dogr", "Gong", "Gonm", "Gran", "Gujr", "Guru", "Knda", "Mahj", "Mlym", "Nand", "Orya", "Sind", "Sinh", "Sylo", "Takr", "Taml", "Telu", "Tirh", }},
                new CodepointScriptExtended {RangeStart = 0x965, RangeEnd = 0x965, ScriptNamesShort = new string[]{"Beng", "Deva", "Dogr", "Gong", "Gonm", "Gran", "Gujr", "Guru", "Knda", "Limb", "Mahj", "Mlym", "Nand", "Orya", "Sind", "Sinh", "Sylo", "Takr", "Taml", "Telu", "Tirh", }},
                new CodepointScriptExtended {RangeStart = 0x966, RangeEnd = 0x96F, ScriptNamesShort = new string[]{"Deva", "Dogr", "Kthi", "Mahj", }},
                new CodepointScriptExtended {RangeStart = 0x9E6, RangeEnd = 0x9EF, ScriptNamesShort = new string[]{"Beng", "Cakm", "Sylo", }},
                new CodepointScriptExtended {RangeStart = 0xA66, RangeEnd = 0xA6F, ScriptNamesShort = new string[]{"Guru", "Mult", }},
                new CodepointScriptExtended {RangeStart = 0xAE6, RangeEnd = 0xAEF, ScriptNamesShort = new string[]{"Gujr", "Khoj", }},
                new CodepointScriptExtended {RangeStart = 0xBE6, RangeEnd = 0xBF3, ScriptNamesShort = new string[]{"Gran", "Taml", }},
                new CodepointScriptExtended {RangeStart = 0xCE6, RangeEnd = 0xCEF, ScriptNamesShort = new string[]{"Knda", "Nand", }},
                new CodepointScriptExtended {RangeStart = 0x1040, RangeEnd = 0x1049, ScriptNamesShort = new string[]{"Cakm", "Mymr", "Tale", }},
                new CodepointScriptExtended {RangeStart = 0x10FB, RangeEnd = 0x10FB, ScriptNamesShort = new string[]{"Geor", "Latn", }},
                new CodepointScriptExtended {RangeStart = 0x1735, RangeEnd = 0x1736, ScriptNamesShort = new string[]{"Buhd", "Hano", "Tagb", "Tglg", }},
                new CodepointScriptExtended {RangeStart = 0x1802, RangeEnd = 0x1803, ScriptNamesShort = new string[]{"Mong", "Phag", }},
                new CodepointScriptExtended {RangeStart = 0x1805, RangeEnd = 0x1805, ScriptNamesShort = new string[]{"Mong", "Phag", }},
                new CodepointScriptExtended {RangeStart = 0x1CD0, RangeEnd = 0x1CD0, ScriptNamesShort = new string[]{"Beng", "Deva", "Gran", "Knda", }},
                new CodepointScriptExtended {RangeStart = 0x1CD1, RangeEnd = 0x1CD1, ScriptNamesShort = new string[]{"Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CD2, RangeEnd = 0x1CD2, ScriptNamesShort = new string[]{"Beng", "Deva", "Gran", "Knda", }},
                new CodepointScriptExtended {RangeStart = 0x1CD3, RangeEnd = 0x1CD3, ScriptNamesShort = new string[]{"Deva", "Gran", }},
                new CodepointScriptExtended {RangeStart = 0x1CD4, RangeEnd = 0x1CD4, ScriptNamesShort = new string[]{"Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CD5, RangeEnd = 0x1CD6, ScriptNamesShort = new string[]{"Beng", "Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CD7, RangeEnd = 0x1CD7, ScriptNamesShort = new string[]{"Deva", "Shrd", }},
                new CodepointScriptExtended {RangeStart = 0x1CD8, RangeEnd = 0x1CD8, ScriptNamesShort = new string[]{"Beng", "Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CD9, RangeEnd = 0x1CD9, ScriptNamesShort = new string[]{"Deva", "Shrd", }},
                new CodepointScriptExtended {RangeStart = 0x1CDA, RangeEnd = 0x1CDA, ScriptNamesShort = new string[]{"Deva", "Knda", "Mlym", "Orya", "Taml", "Telu", }},
                new CodepointScriptExtended {RangeStart = 0x1CDB, RangeEnd = 0x1CDB, ScriptNamesShort = new string[]{"Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CDC, RangeEnd = 0x1CDD, ScriptNamesShort = new string[]{"Deva", "Shrd", }},
                new CodepointScriptExtended {RangeStart = 0x1CDE, RangeEnd = 0x1CDF, ScriptNamesShort = new string[]{"Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CE0, RangeEnd = 0x1CE0, ScriptNamesShort = new string[]{"Deva", "Shrd", }},
                new CodepointScriptExtended {RangeStart = 0x1CE1, RangeEnd = 0x1CE1, ScriptNamesShort = new string[]{"Beng", "Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CE2, RangeEnd = 0x1CE8, ScriptNamesShort = new string[]{"Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CE9, RangeEnd = 0x1CE9, ScriptNamesShort = new string[]{"Deva", "Nand", }},
                new CodepointScriptExtended {RangeStart = 0x1CEA, RangeEnd = 0x1CEA, ScriptNamesShort = new string[]{"Beng", "Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CEB, RangeEnd = 0x1CEC, ScriptNamesShort = new string[]{"Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CED, RangeEnd = 0x1CED, ScriptNamesShort = new string[]{"Beng", "Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CEE, RangeEnd = 0x1CF1, ScriptNamesShort = new string[]{"Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CF2, RangeEnd = 0x1CF2, ScriptNamesShort = new string[]{"Beng", "Deva", "Gran", "Knda", "Nand", "Orya", "Telu", "Tirh", }},
                new CodepointScriptExtended {RangeStart = 0x1CF3, RangeEnd = 0x1CF3, ScriptNamesShort = new string[]{"Deva", "Gran", }},
                new CodepointScriptExtended {RangeStart = 0x1CF4, RangeEnd = 0x1CF4, ScriptNamesShort = new string[]{"Deva", "Gran", "Knda", }},
                new CodepointScriptExtended {RangeStart = 0x1CF5, RangeEnd = 0x1CF6, ScriptNamesShort = new string[]{"Beng", "Deva", }},
                new CodepointScriptExtended {RangeStart = 0x1CF7, RangeEnd = 0x1CF7, ScriptNamesShort = new string[]{"Beng", }},
                new CodepointScriptExtended {RangeStart = 0x1CF8, RangeEnd = 0x1CF9, ScriptNamesShort = new string[]{"Deva", "Gran", }},
                new CodepointScriptExtended {RangeStart = 0x1CFA, RangeEnd = 0x1CFA, ScriptNamesShort = new string[]{"Nand", }},
                new CodepointScriptExtended {RangeStart = 0x1DC0, RangeEnd = 0x1DC1, ScriptNamesShort = new string[]{"Grek", }},
                new CodepointScriptExtended {RangeStart = 0x202F, RangeEnd = 0x202F, ScriptNamesShort = new string[]{"Latn", "Mong", }},
                new CodepointScriptExtended {RangeStart = 0x20F0, RangeEnd = 0x20F0, ScriptNamesShort = new string[]{"Deva", "Gran", "Latn", }},
                new CodepointScriptExtended {RangeStart = 0x2E43, RangeEnd = 0x2E43, ScriptNamesShort = new string[]{"Cyrl", "Glag", }},
                new CodepointScriptExtended {RangeStart = 0x3001, RangeEnd = 0x3002, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", "Yiii", }},
                new CodepointScriptExtended {RangeStart = 0x3003, RangeEnd = 0x3003, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x3006, RangeEnd = 0x3006, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0x3008, RangeEnd = 0x3011, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", "Yiii", }},
                new CodepointScriptExtended {RangeStart = 0x3013, RangeEnd = 0x3013, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x3014, RangeEnd = 0x301B, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", "Yiii", }},
                new CodepointScriptExtended {RangeStart = 0x301C, RangeEnd = 0x301F, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x302A, RangeEnd = 0x302D, ScriptNamesShort = new string[]{"Bopo", "Hani", }},
                new CodepointScriptExtended {RangeStart = 0x3030, RangeEnd = 0x3030, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x3031, RangeEnd = 0x3035, ScriptNamesShort = new string[]{"Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x3037, RangeEnd = 0x3037, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x303C, RangeEnd = 0x303D, ScriptNamesShort = new string[]{"Hani", "Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x303E, RangeEnd = 0x303F, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0x3099, RangeEnd = 0x309C, ScriptNamesShort = new string[]{"Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x30A0, RangeEnd = 0x30A0, ScriptNamesShort = new string[]{"Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x30FB, RangeEnd = 0x30FB, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", "Yiii", }},
                new CodepointScriptExtended {RangeStart = 0x30FC, RangeEnd = 0x30FC, ScriptNamesShort = new string[]{"Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x3190, RangeEnd = 0x319F, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0x31C0, RangeEnd = 0x31E3, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0x3220, RangeEnd = 0x3247, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0x3280, RangeEnd = 0x32B0, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0x32C0, RangeEnd = 0x32CB, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0x3358, RangeEnd = 0x3370, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0x337B, RangeEnd = 0x337F, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0x33E0, RangeEnd = 0x33FE, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0xA66F, RangeEnd = 0xA66F, ScriptNamesShort = new string[]{"Cyrl", "Glag", }},
                new CodepointScriptExtended {RangeStart = 0xA830, RangeEnd = 0xA832, ScriptNamesShort = new string[]{"Deva", "Dogr", "Gujr", "Guru", "Khoj", "Knda", "Kthi", "Mahj", "Mlym", "Modi", "Nand", "Sind", "Takr", "Tirh", }},
                new CodepointScriptExtended {RangeStart = 0xA833, RangeEnd = 0xA835, ScriptNamesShort = new string[]{"Deva", "Dogr", "Gujr", "Guru", "Khoj", "Knda", "Kthi", "Mahj", "Modi", "Nand", "Sind", "Takr", "Tirh", }},
                new CodepointScriptExtended {RangeStart = 0xA836, RangeEnd = 0xA839, ScriptNamesShort = new string[]{"Deva", "Dogr", "Gujr", "Guru", "Khoj", "Kthi", "Mahj", "Modi", "Sind", "Takr", "Tirh", }},
                new CodepointScriptExtended {RangeStart = 0xA8F1, RangeEnd = 0xA8F1, ScriptNamesShort = new string[]{"Beng", "Deva", }},
                new CodepointScriptExtended {RangeStart = 0xA8F3, RangeEnd = 0xA8F3, ScriptNamesShort = new string[]{"Deva", "Taml", }},
                new CodepointScriptExtended {RangeStart = 0xA92E, RangeEnd = 0xA92E, ScriptNamesShort = new string[]{"Kali", "Latn", "Mymr", }},
                new CodepointScriptExtended {RangeStart = 0xA9CF, RangeEnd = 0xA9CF, ScriptNamesShort = new string[]{"Bugi", "Java", }},
                new CodepointScriptExtended {RangeStart = 0xFDF2, RangeEnd = 0xFDF2, ScriptNamesShort = new string[]{"Arab", "Thaa", }},
                new CodepointScriptExtended {RangeStart = 0xFDFD, RangeEnd = 0xFDFD, ScriptNamesShort = new string[]{"Arab", "Thaa", }},
                new CodepointScriptExtended {RangeStart = 0xFE45, RangeEnd = 0xFE46, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0xFF61, RangeEnd = 0xFF65, ScriptNamesShort = new string[]{"Bopo", "Hang", "Hani", "Hira", "Kana", "Yiii", }},
                new CodepointScriptExtended {RangeStart = 0xFF70, RangeEnd = 0xFF70, ScriptNamesShort = new string[]{"Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0xFF9E, RangeEnd = 0xFF9F, ScriptNamesShort = new string[]{"Hira", "Kana", }},
                new CodepointScriptExtended {RangeStart = 0x10100, RangeEnd = 0x10102, ScriptNamesShort = new string[]{"Cprt", "Linb", }},
                new CodepointScriptExtended {RangeStart = 0x10107, RangeEnd = 0x10133, ScriptNamesShort = new string[]{"Cprt", "Lina", "Linb", }},
                new CodepointScriptExtended {RangeStart = 0x10137, RangeEnd = 0x1013F, ScriptNamesShort = new string[]{"Cprt", "Linb", }},
                new CodepointScriptExtended {RangeStart = 0x102E0, RangeEnd = 0x102FB, ScriptNamesShort = new string[]{"Arab", "Copt", }},
                new CodepointScriptExtended {RangeStart = 0x11301, RangeEnd = 0x11301, ScriptNamesShort = new string[]{"Gran", "Taml", }},
                new CodepointScriptExtended {RangeStart = 0x11303, RangeEnd = 0x11303, ScriptNamesShort = new string[]{"Gran", "Taml", }},
                new CodepointScriptExtended {RangeStart = 0x1133B, RangeEnd = 0x1133C, ScriptNamesShort = new string[]{"Gran", "Taml", }},
                new CodepointScriptExtended {RangeStart = 0x11FD0, RangeEnd = 0x11FD1, ScriptNamesShort = new string[]{"Gran", "Taml", }},
                new CodepointScriptExtended {RangeStart = 0x11FD3, RangeEnd = 0x11FD3, ScriptNamesShort = new string[]{"Gran", "Taml", }},
                new CodepointScriptExtended {RangeStart = 0x1BCA0, RangeEnd = 0x1BCA3, ScriptNamesShort = new string[]{"Dupl", }},
                new CodepointScriptExtended {RangeStart = 0x1D360, RangeEnd = 0x1D371, ScriptNamesShort = new string[]{"Hani", }},
                new CodepointScriptExtended {RangeStart = 0x1F250, RangeEnd = 0x1F251, ScriptNamesShort = new string[]{"Hani", }},

            };
    }
}