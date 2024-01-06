using Microsoft.Extensions.Logging;
using System.Text;
using static UIUCLibrary.EaPdf.Helpers.UnicodeScriptDetector;

namespace UIUCLibrary.EaPdf.Helpers
{
    public static class UnicodeHelpers
    {

        public const char ZWNJ = '\u200c';

        /// <summary>
        /// Place a zero-width non-joiner (ZWNJ) character between the two characters of a ligature to prevent the ligature from being formed.
        /// This seems to be needed to prevent FOP from sometimes forming ligatures (maybe depending on font-family), which causes problems with the PDF/A validation
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static string PreventLigatures(string original)
        {
            var ret = original;

            //these replacements will also cover ffi and ffl ligatures
            ret = ret.Replace("fi", $"f{ZWNJ}i");
            ret = ret.Replace("fl", $"f{ZWNJ}l");
            ret = ret.Replace("ff", $"f{ZWNJ}f");

            return ret;
        }

        /// <summary>
        /// Replace any characters in the Unicode Private Use Area (PUA) with the Unicode replacement character
        /// and log a warning message
        /// </summary>
        /// <param name="original"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public static string ReplacePuaChars_OLD(string original, out List<(LogLevel level, string message)> messages)
        {
            messages = new();
            StringBuilder ret = new();
            foreach (char c in original)
            {
                if ((c >= 0xE000 && c <= 0xF8FF)) // || (c >= 0xF0000 && c <= 0xFFFFD) || (c >= 0x100000 && c <= 0x10FFFD)) //FUTURE: Add support for other private use areas
                {
                    ret.Append('\uFFFD');
                    messages.Add((LogLevel.Warning, $"(PUA) Private use area character {c} U+{(int)c:X4} replaced with \uFFFD U+FFFD"));
                }
                else
                {
                    ret.Append(c);
                }
            }

            return ret.ToString();
        }

        /// <summary>
        /// Replace any characters in the Unicode Private Use Area (PUA) with the Unicode replacement character
        /// and log a warning message
        /// </summary>
        /// <param name="original"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public static string ReplacePuaChars(string original, out List<(LogLevel level, string message)> messages)
        {
            messages = new();
            StringBuilder ret = new();
            for (int charIndex = 0; charIndex < original.Length; charIndex++)
            {
                int codePoint = char.ConvertToUtf32(original, charIndex); //must accommodate unicode codepoints > 0xffff using surrogates
                if (codePoint > 0xffff)
                    charIndex++;

                if ((codePoint >= 0xE000 && codePoint <= 0xF8FF) || (codePoint >= 0xF0000 && codePoint <= 0xFFFFD) || (codePoint >= 0x100000 && codePoint <= 0x10FFFD)) 
                {
                    ret.Append('\uFFFD');
                    messages.Add((LogLevel.Warning, $"(PUA) Private use area character {codePoint} U+{(int)codePoint:X6} replaced with \uFFFD U+FFFD"));
                }
                else
                {
                    ret.Append(char.ConvertFromUtf32(codePoint));
                }
            }

            return ret.ToString();
        }

        /// <summary>
        /// Partition the the given text by which unicode script the characters belong to.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="messages">error or warning messages related to the partitioning</param>
        /// <returns>A list of character ranges and their corresponding Unicode script property</returns>
        static public List<(Range range, string scriptName)> PartitionTextByUnicodeScript(string? text, out List<(LogLevel level, string message)> messages)
        {
            messages = new();
            List<(Range range, string scriptName)> ret = new();

            if (string.IsNullOrEmpty(text))
                return ret;

            HashSet<string> prevNonCommonScriptNames = new();
            string? prevScriptName = null; // for handling inheritance

            int prevStart = 0;

            for (int charIndex = 0; charIndex < text.Length; charIndex++)
            {
                int codePoint = char.ConvertToUtf32(text, charIndex); //must accommodate unicode codepoints > 0xffff using surrogates
                if (codePoint > 0xffff)
                    charIndex++;

                var cps = GetCodepointScripts().SingleOrDefault(cs => cs.RangeStart <= codePoint && cs.RangeEnd >= codePoint);
                var cpsExt = GetCodepointScriptsExtended().SingleOrDefault(cs => cs.RangeStart <= codePoint && cs.RangeEnd >= codePoint);

                HashSet<string> curNonCommonScriptNames = new();
                string curScriptName = ScriptShortUnknown;

                if (cps != null)
                {
                    curScriptName = cps.Script.ShortName;
                }
                if (curScriptName != ScriptShortCommon)
                    curNonCommonScriptNames.Add(curScriptName);

                if (cpsExt != null)
                {
                    curNonCommonScriptNames.UnionWith(cpsExt.ScriptNamesShort);
                }

                //if the current scripts are empty, then the common script is used, so we need to set the current scripts to the previous scripts
                //this ensures that common script characters are bundled with the previous script segment
                if (!curNonCommonScriptNames.Any())
                {
                    curNonCommonScriptNames.UnionWith(prevNonCommonScriptNames);
                }

                //handle inheritance
                if (curScriptName == ScriptShortInherited && prevScriptName != null)
                {
                    curScriptName = prevScriptName;
                    curNonCommonScriptNames.Clear();
                    curNonCommonScriptNames.UnionWith(prevNonCommonScriptNames);
                }
                else if (curScriptName == ScriptShortInherited && prevScriptName == null)
                {
                    messages.Add((LogLevel.Warning, $"Unexpected inherited script at start of text, codepoint: {codePoint}"));
                }

                var sharedScripts = curNonCommonScriptNames.Intersect(prevNonCommonScriptNames);

                //if there are no shared scripts between the current and previous lists, we need to start a new segment
                if (!sharedScripts.Any() && prevNonCommonScriptNames.Any())
                {
                    var names1 = prevNonCommonScriptNames.First(); //just use the first script name in the list
                    ret.Add((new Range(prevStart, charIndex - (codePoint > 0xffff ? 1 : 0)), names1));
                    prevStart = charIndex - (codePoint > 0xffff ? 1 : 0);
                }

                if (sharedScripts.Any())
                {
                    //set the previous to the intersection of the previous and current
                    prevNonCommonScriptNames.IntersectWith(curNonCommonScriptNames);
                }
                else if (curNonCommonScriptNames.Any())
                {
                    //set the previous to the current
                    prevNonCommonScriptNames.Clear();
                    prevNonCommonScriptNames.UnionWith(curNonCommonScriptNames);
                }

                //set the previous script name to the current one for the next iteration
                prevScriptName = curScriptName;
            }

            //add the last segment of text
            var names2 = prevNonCommonScriptNames.FirstOrDefault(); //just use the first script name in the list
            if (string.IsNullOrEmpty(names2))
                names2 = prevScriptName ?? ScriptShortCommon;
            ret.Add((new Range(prevStart, text.Length), names2));

            return ret;
        }


    }
}
