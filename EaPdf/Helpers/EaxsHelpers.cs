using static UIUCLibrary.EaPdf.Helpers.FontHelpers;
using System.Xml;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class EaxsHelpers
    {

        public XmlDocument EaxsDocument { get; init; } = new();

        private string EaxsFilePath { get; init; } = string.Empty;

        public EaxsHelpers(string eaxsFilePath)
        {
            EaxsFilePath = eaxsFilePath;
            EaxsDocument.Load(EaxsFilePath);
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
        public (string serifFonts, string sansFonts, string monoFonts, bool complexScripts) GetBaseFontsToUse(EaxsToEaPdfProcessorSettings settings)
        {
            HashSet<string> serifFonts = new() { SERIF };
            HashSet<string> sansSerifFonts = new() { SANS_SERIF };
            HashSet<string> monospaceFonts = new() { MONOSPACE };
            bool complexScripts = false;

            var text = EaxsDocument.DocumentElement?.InnerText ?? string.Empty;

            if (text != null)
            {
                //get the list of all scripts used in the text, ranked by how commonly they occur
                var scripts = UnicodeScriptDetector.GetUsedScripts(text);

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
