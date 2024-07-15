using static UIUCLibrary.EaPdf.Helpers.FontHelpers;
using System.Xml;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class EaxsHelpers
    {

        /// <summary>
        /// In the EAXS XML, the Content-Type header is parsed and stored as a series of child elements.
        /// This function will recombine them into a single string which will not be 
        /// exactly the same as the original Content-Type header, but should be equivalent.
        /// </summary>
        /// <param name="bodyNode"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public static string GetOriginalContentTypeHeader(XmlNode bodyNode)
        {
            if (bodyNode is not XmlElement body) 
            { 
                throw new ArgumentException($"The body element must be an XmlElement; it is a '{bodyNode.NodeType}'", nameof(bodyNode));
            }

            var xmlns = new XmlNamespaceManager(body.OwnerDocument.NameTable);
            xmlns.AddNamespace(EmailToEaxsProcessor.XM, EmailToEaxsProcessor.XM_NS);

            var ret = new StringBuilder();
            var contentType = body.SelectSingleNode("xm:ContentType", xmlns);
            if (contentType != null)
                ret.Append(contentType.InnerText);
            else
                throw new Exception("No ContentType element found in the EAXS file");

            var charset = body.SelectSingleNode("xm:Charset", xmlns);
            if (charset != null)
                ret.Append("; charset=").Append(QuoteIfNeeded(charset.InnerText));

            var name = body.SelectSingleNode("xm:ContentName", xmlns);
            if (name != null)
                ret.Append("; name=").Append(QuoteIfNeeded(name.InnerText));

            var boundary = body.SelectSingleNode("xm:BoundaryString", xmlns);
            if(boundary != null)
                ret.Append("; boundary=").Append(QuoteIfNeeded(boundary.InnerText));

            var parms = body.SelectNodes("xm:ContentTypeParam", xmlns);
            if(parms != null)
            {
                foreach(XmlNode parm in parms)
                {
                    var parmName = parm.SelectSingleNode("xm:Name", xmlns) ?? throw new Exception("ContentTypeParam Name is missing");
                    var parmValue = parm.SelectSingleNode("xm:Value", xmlns) ?? throw new Exception("ContentTypeParam Value is missing");
                    ret.Append("; ").Append(parmName.InnerText).Append('=').Append(QuoteIfNeeded(parmValue.InnerText));
                }
            }

            var comments = body.SelectSingleNode("xm:ContentTypeComments", xmlns);
            if(comments != null)
            {
                ret.Append(" (");
                ret.Append(Regex.Replace(comments.InnerText, @"\(|\)|\\", @"\$0"));
                ret.Append(')');
            }

            return ret.ToString();
        }

        /// <summary>
        /// Quote a string according to the rules for message header parameters
        /// <see cref="https://datatracker.ietf.org/doc/html/rfc5322#section-3.2.4"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string QuoteIfNeeded(string value)
        {
            var rex = new Regex(@"\(|\)|<|>|@|,|;|:|\\|""|/|\[|\]|\?|=| |[\u0001-\u001f]|\u007f");

            if(rex.IsMatch(value))
            {
                return "\"" + Regex.Replace(value, @"""|\\", @"\$0") + "\"";
            }
            else
            {
                return value;
            }
        }

        public XmlDocument EaxsDocument { get; init; } = new();
        private XmlNamespaceManager Xmlns { get; init; }

        private string EaxsFilePath { get; init; } = string.Empty;

        public EaxsHelpers(string eaxsFilePath)
        {
            EaxsFilePath = eaxsFilePath;
            EaxsDocument.Load(EaxsFilePath);
            Xmlns = new XmlNamespaceManager(EaxsDocument.NameTable);
            Xmlns.AddNamespace(EmailToEaxsProcessor.XM, EmailToEaxsProcessor.XM_NS);
        }

        /// <summary>
        /// Return the number of messages in the EAXS file, child messages do not count
        /// </summary>
        public int MessageCount
        {
            get
            {
                return EaxsDocument.SelectNodes("//xm:Message", Xmlns)?.Count ?? 0;
            }
        }

        /// <summary>
        /// Get the value of the processing instruction "ContinuedIn" in the EAXS file, if not present return empty string
        /// </summary>
        public string ContinuedInFile
        {
            get
            {
                var continuedInFile = EaxsDocument.SelectSingleNode("//processing-instruction('ContinuedIn')", Xmlns)?.Value ?? String.Empty;
                return continuedInFile;
            }
        }

        /// <summary>
        /// Get the value of the processing instruction "ContinuedFrom" in the EAXS file, if not present return empty string
        /// </summary>
        public string ContinuedFromFile
        {
            get
            {
                var continuedInFile = EaxsDocument.SelectSingleNode("//processing-instruction('ContinuedFrom')",Xmlns)?.Value ?? String.Empty;
                return continuedInFile;
            }
        }
        /// <summary>
        /// Save the XSL-FO back to the same file as was opened
        /// </summary>
        public void SaveFoFile()
        {
            SaveFoFile(EaxsFilePath);
        }

        /// <summary>
        /// Save the XSL-FO to the given file path
        /// </summary>
        /// <param name="eaxsFilePath"></param>
        public void SaveFoFile(string eaxsFilePath)
        {
            EaxsDocument.Save(eaxsFilePath);
        }

        /// <summary>
        /// Based on the Unicode scripts used in the text in the EAXS file and the font settings, determine which fonts to use as the default fonts
        /// </summary>
        /// <param name="eaxsFilePath"></param>
        /// <param name="settings"></param>
        /// <returns>4-tuple with comma-separated lists of serif, sans-serif, and monospace font names, plus a bool indicating whether complex scripts are present</returns>
        public (string serifFonts, string sansFonts, string monoFonts, bool complexScripts) GetBaseFontsToUse(EaxsToEaPdfProcessorSettings settings, ref List<(LogLevel level, string message)> messages)
        {
            HashSet<string> serifFonts = new() { SERIF };
            HashSet<string> sansSerifFonts = new() { SANS_SERIF };
            HashSet<string> monospaceFonts = new() { MONOSPACE };
            bool complexScripts = false;

            var text = EaxsDocument.DocumentElement?.InnerText ?? string.Empty;

            if (text != null)
            {
                //get the list of all scripts used in the text, ranked by how commonly they occur
                var scripts = UnicodeScriptDetector.GetUsedScripts(text, ref messages);

                foreach (var script in scripts)
                {
                    if (script != null)
                    {
                        serifFonts.UnionWith((settings.GetFontFamily(script.ScriptNameShort ?? SERIF, BaseFontFamily.Serif) ?? SERIF).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        sansSerifFonts.UnionWith((settings.GetFontFamily(script.ScriptNameShort ?? SANS_SERIF, BaseFontFamily.SansSerif) ?? SANS_SERIF).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        monospaceFonts.UnionWith((settings.GetFontFamily(script.ScriptNameShort ?? MONOSPACE, BaseFontFamily.Monospace) ?? MONOSPACE).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    }
                }

                complexScripts = scripts.Any(s => UnicodeScriptDetector.IsComplexScript(s.ScriptNameShort));
            }


            return (string.Join(',', serifFonts), string.Join(',', sansSerifFonts), string.Join(',', monospaceFonts), complexScripts);
        }

    }
}
