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

        /// <summary>
        /// Try replacing XML-invalid characters from the string
        /// </summary>
        /// <param name="value">the string that will have its invalid characters replaced</param>
        /// <param name="msg">if characters were replaced this will provide an explanation</param>
        /// <returns>true if characters were replaced; otherwise false</returns>
        public static bool TryReplaceInvalidXMLChars(ref string value, out string msg)
        {
            bool ret = false; //Return true if any characters were replaced
            msg = "";

            if (string.IsNullOrEmpty(value))
            {
                return ret;
            }
            try
            {
                value = XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException xex)
            {
                msg = xex.Message;
                value = XmlHelpers.ReplaceInvalidXMLChars(value);
                ret = true;
            }

            return ret;
        }

        /// <summary>
        /// Try removing XML-invalid characters from the string
        /// </summary>
        /// <param name="value">the string that will have its invalid characters removed</param>
        /// <param name="msg">if characters were removed this will provide an explanation</param>
        /// <returns>true if characters were removed; otherwsie false</returns>
        public static bool TryRemoveInvalidXMLChars(ref string value, out string msg)
        {
            bool ret = false; //Return true if any characters were removed
            msg = "";

            if (string.IsNullOrEmpty(value))
            {
                return ret;
            }
            try
            {
                value = XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException xex)
            {
                msg = xex.Message;
                value = XmlHelpers.RemoveInvalidXMLChars(value);
                ret = true;
            }

            return ret;
        }


    }
}
