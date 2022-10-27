using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class XmlHelpers
    {
        //See: https://stackoverflow.com/questions/397250/unicode-regex-invalid-xml-characters/961504#961504

        // Filters control characters but allows only properly-formed surrogate sequences
        private static Regex _invalidXMLChars = new Regex(
            @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
            RegexOptions.Compiled);

        /// <summary>
        /// Replace any unicode characters that can't be encoded into XML with unicode replacement character FFFD
        /// </summary>
        public static string ReplaceInvalidXMLChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return _invalidXMLChars.Replace(text, "\uFFFD");
        }

        /// <summary>
        /// Remove any unicode characters that can't be encoded into XML 
        /// </summary>
        public static string RemoveInvalidXMLChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return _invalidXMLChars.Replace(text, "");
        }

    }
}
