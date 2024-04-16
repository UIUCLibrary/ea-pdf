using static UIUCLibrary.EaPdf.Helpers.FontHelpers;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class EaxsHelpers
    {

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
