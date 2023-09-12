using static UIUCLibrary.EaPdf.Helpers.FontHelper;
using System.Text.RegularExpressions;

namespace UIUCLibrary.EaPdf
{
    public class EaxsToEaPdfProcessorSettings
    {
        public string XsltFoFilePath { get; set; } = "XResources\\eaxs_to_fo.xsl";

        public string XsltXmpFilePath { get; set; } = "XResources\\eaxs_to_xmp.xsl";

        public string XsltRootXmpFilePath { get; set; } = "XResources\\eaxs_to_root_xmp.xsl";

        /// <summary>
        /// The folder where the fonts are located, may contain subfolders of fonts
        /// </summary>
        public string FontsFolder { get; set; } = "Fonts";

        /// <summary>
        /// Regular expressions to match font names to base font styles, serif, sans-serif, and monospace
        /// </summary>
        public Dictionary<Regex, BaseFontFamily> BaseFontMapping { get; set; } = new Dictionary<Regex, BaseFontFamily>()
        {
            {new Regex("sans", RegexOptions.IgnoreCase), BaseFontFamily.SansSerif}, //this must be first so that it matches before serif
            {new Regex("serif", RegexOptions.IgnoreCase), BaseFontFamily.Serif},
            {new Regex("mono", RegexOptions.IgnoreCase), BaseFontFamily.Monospace}
        };

        /// <summary>
        /// Mapping of unicode language scripts to font families
        /// First key is a comma-separated list of ISO 15924 4-letter codes for scripts, empty string is the default and must be present in the dictionary with all three base font families
        /// The second key is the base font family, serif, sans-serif, or monospace; the first key is the default if no base font family is specified
        /// The value is a comma-separated list of font family names; these names must exist in the FO processor's font configuration
        /// </summary>
        public Dictionary<string, Dictionary<BaseFontFamily, string>> LanguageFontMapping { get; set; } = new Dictionary<string, Dictionary<BaseFontFamily, string>>()
        {
            {string.Empty, new Dictionary<BaseFontFamily, string>() //default fonts should cover most western languages; these should be mapped to actual fonts in the FO processor's font configuration
                {
                    {BaseFontFamily.Serif, SERIF},
                    {BaseFontFamily.SansSerif, SANS_SERIF},
                    {BaseFontFamily.Monospace, MONOSPACE}
                }
            },
            {"arab", new Dictionary<BaseFontFamily, string>() //Arabic
                {
                    {BaseFontFamily.Serif, "Traditional Arabic"},
                    {BaseFontFamily.SansSerif, "Simplified Arabic "},
                    {BaseFontFamily.Monospace, "Simplified Arabic Fixed"},
                }
            },
            {"hira,kana,hrkt", new Dictionary<BaseFontFamily, string>() //Hiragana, Katakana (Japanese)
                {
                    {BaseFontFamily.Serif, "Kurinto Text JP"},
                    {BaseFontFamily.SansSerif, "Kurinto Sans JP"},
                    {BaseFontFamily.Monospace, "Kurinto Mono JP"}
                }
            },
            {"hang", new Dictionary<BaseFontFamily, string>() //Hangul (Korean)  
                {
                    {BaseFontFamily.Serif, "Kurinto Text KR"},
                    {BaseFontFamily.SansSerif, "Kurinto Sans KR"},
                    {BaseFontFamily.Monospace, "Kurinto Mono KR"}
                }
            },
            {"hani", new Dictionary<BaseFontFamily, string>() //   (Chinese: Hong Kong - HK, Simplified - SC, Traditional - TC, and Rare - CJK  )
                {
                    {BaseFontFamily.Serif, "Kurinto Text SC,Kurinto Text TC,Kurinto Text HK,Kurinto Text CJK"},
                    {BaseFontFamily.SansSerif, "Kurinto Sans SC,Kurinto Sans TC,Kurinto Sans HK,Kurinto Sans CJK"},
                    {BaseFontFamily.Monospace, "Kurinto Mono SC,Kurinto Mono TC,Kurinto Mono HK,Kurinto Mono CJK"}
                }
            },
        };

        /// <summary>
        /// Get the default font family names for the given base font family
        /// </summary>
        /// <param name="baseFamily"></param>
        /// <returns></returns>
        public string GetDefaultFontFamily(BaseFontFamily baseFamily)
        {
            return LanguageFontMapping[string.Empty][baseFamily];
        }

        /// <summary>
        /// Get the font family names for the given script and base font family
        /// Returns null if the script is not supported
        /// </summary>
        /// <param name="script"></param>
        /// <param name="baseFamily"></param>
        /// <returns></returns>
        public string? GetFontFamily(string script, BaseFontFamily baseFamily)
        {
            string? ret = null;

            foreach (var entry in LanguageFontMapping)
            {
                // need to iterate bacause the key is a comma-separated list of scripts
                if (entry.Key.Contains(script, StringComparison.OrdinalIgnoreCase))
                {
                    if (entry.Value.ContainsKey(baseFamily))
                    {
                        ret = entry.Value[baseFamily];
                    }
                    else
                    {
                        ret = entry.Value.First().Value; //default to the first base font family in the dictionary
                    }
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Return a list of all supported scripts in the LanguageFontMapping
        /// </summary>
        public List<string> AllSupportedScripts
        {
            get
            {
                List<string> ret = new();

                foreach (var entry in LanguageFontMapping)
                {
                    ret.AddRange(entry.Key.Split(','));
                }

                return ret;
            }
        } 


    }
}
