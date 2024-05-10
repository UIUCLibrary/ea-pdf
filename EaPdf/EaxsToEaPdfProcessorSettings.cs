using Microsoft.Extensions.Configuration;
using UIUCLibrary.EaPdf.Helpers;
using static UIUCLibrary.EaPdf.Helpers.FontHelpers;

namespace UIUCLibrary.EaPdf
{
    public class EaxsToEaPdfProcessorSettings
    {
        public EaxsToEaPdfProcessorSettings(IConfiguration config)
        {
            //the LanguageFontMapping will be replaced by any LanguageFontMapping in the configuration file
            if (config.AsEnumerable().Any(s => s.Key.StartsWith("EaxsToEaPdfProcessorSettings:LanguageFontMapping:")))
            {
                LanguageFontMapping.Clear();
            }

            config.Bind("EaxsToEaPdfProcessorSettings", this);

            ValidateSettings();

        }

        public EaxsToEaPdfProcessorSettings()
        {
            ValidateSettings();
        }

        private void ValidateSettings()
        {
            //make sure supported scripts are in the ISO 15924 list
            foreach (var script in LanguageFontMapping)
            {
                if (!script.Key.Equals(FontHelpers.DEFAULT_SCRIPT, StringComparison.OrdinalIgnoreCase) && !UnicodeScriptDetector.GetScripts().Any(s => s.ShortName.Equals(script.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new Exception($"Script code '{script.Key}' is not a valid ISO 15924 script code");
                }

                var families = script.Value;

                if (families == null || families.Count == 0)
                {
                    throw new Exception($"Script code '{script.Key}' does not specify any font families");
                }
            }

            //make sure the needed files are present
            if (!string.IsNullOrWhiteSpace(XsltFoFilePath) && !File.Exists(XsltFoFilePath))
            {
                throw new Exception($"XSLT file '{XsltFoFilePath}' not found");
            }
            if(!string.IsNullOrWhiteSpace(XsltXmpFilePath) && !File.Exists(XsltXmpFilePath))
            {
                throw new Exception($"XSLT file '{XsltXmpFilePath}' not found");
            }
            if(!string.IsNullOrWhiteSpace(XsltRootXmpFilePath) && !File.Exists(XsltRootXmpFilePath))
            {
                throw new Exception($"XSLT file '{XsltRootXmpFilePath}' not found");
            }

            //FUTURE: maybe add some other validations here


        }


        public string XsltFoFilePath { get; set; } = "XResources\\eaxs_to_fo.xsl";

        public string XsltXmpFilePath { get; set; } = "XResources\\eaxs_to_xmp.xsl";

        public string XsltRootXmpFilePath { get; set; } = "XResources\\eaxs_to_root_xmp.xsl";


        /// <summary>
        /// Mapping of unicode language scripts to font families
        /// The outer dictionary key is an ISO 15924 4-letter codes for the script.
        /// A special entry with key 'default' should be in the dictionary with all three base font families specified. 
        /// This will be used as the default if a script entry is not found in the list.  Usually, this will be fonts with Latin or Western character sets, but could be fonts for a specific language.
        /// If a 'default' entry is not present, an entry for 'latn' (Latin) will be used as the default.  If neither 'default' nor 'latn' is found, the first entry in the dictionary will be used 
        /// as the default which may produce unintended results.
        /// 
        /// The inner dictionary key is the BaseFontFamily enum, Serif, SansSerif, or Monospace; the first key is the default if no base font family is specified
        /// The value of the inner dictionary is a comma-separated list of font family names; these names must exist in the FO processor's font configuration
        /// </summary>
        public Dictionary<string, Dictionary<BaseFontFamily, string>> LanguageFontMapping { get; set; } = new Dictionary<string, Dictionary<BaseFontFamily, string>>(StringComparer.OrdinalIgnoreCase)
        {
            {FontHelpers.DEFAULT_SCRIPT, new Dictionary<BaseFontFamily, string>() //default fonts should cover most western languages; these should be mapped to actual fonts in the FO processor's font configuration
                {
                    {BaseFontFamily.Serif, SERIF},
                    {BaseFontFamily.SansSerif, SANS_SERIF},
                    {BaseFontFamily.Monospace, MONOSPACE}
                }
            },
            {"hebr", new Dictionary<BaseFontFamily, string>() //Hebrew
                {
                    {BaseFontFamily.Serif, SERIF},
                    {BaseFontFamily.SansSerif, SANS_SERIF},
                    {BaseFontFamily.Monospace, MONOSPACE},
                }
            },
            {"arab", new Dictionary<BaseFontFamily, string>() //Arabic
                {
                    {BaseFontFamily.Serif, "Traditional Arabic," + SERIF},
                    {BaseFontFamily.SansSerif, "Simplified Arabic," + SANS_SERIF},
                    {BaseFontFamily.Monospace, "Simplified Arabic Fixed," + MONOSPACE},
                }
            },
            {"hira", new Dictionary<BaseFontFamily, string>() //Hiragana (Japanese)
                {
                    {BaseFontFamily.Serif, "Kurinto Text JP"},
                    {BaseFontFamily.SansSerif, "Kurinto Sans JP"},
                    {BaseFontFamily.Monospace, "Kurinto Mono JP"}
                }
            },
            {"kana", new Dictionary<BaseFontFamily, string>() //Katakana (Japanese)
                {
                    {BaseFontFamily.Serif, "Kurinto Text JP"},
                    {BaseFontFamily.SansSerif, "Kurinto Sans JP"},
                    {BaseFontFamily.Monospace, "Kurinto Mono JP"}
                }
            },
            {"hrkt", new Dictionary<BaseFontFamily, string>() //Hiragana or Katakana (Japanese)
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
        /// This is either the 'default' or 'latn' entry in the LanguageFontMapping, or the first entry if neither of those is present
        /// </summary>
        /// <param name="baseFamily"></param>
        /// <returns></returns>
        public string GetDefaultFontFamily(BaseFontFamily baseFamily)
        {
            if (!LanguageFontMapping.TryGetValue(FontHelpers.DEFAULT_SCRIPT, out Dictionary<BaseFontFamily, string>? families))
            {
                if (!LanguageFontMapping.TryGetValue("latn", out families))
                {
                    families = LanguageFontMapping.First().Value;
                }
            }

            return GetFontFamily(families, baseFamily);
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

            if (LanguageFontMapping.TryGetValue(script, out Dictionary<BaseFontFamily, string>? families))
            {
                ret = GetFontFamily(families, baseFamily);
            }

            return ret;
        }

        /// <summary>
        /// Get the font family names for the given famlies and base font family
        /// Returns the first one if the base font family is not found
        /// </summary>
        /// <param name="families"></param>
        /// <param name="baseFamily"></param>
        /// <returns></returns>
        private string GetFontFamily(Dictionary<BaseFontFamily, string> families, BaseFontFamily baseFamily)
        {
            if (families.TryGetValue(baseFamily, out string? family))
            {
                return family;
            }
            else
            {
                return families.First().Value; //default to the first base font family in the dictionary
            }
        }

        /// <summary>
        /// Return a list of all supported scripts in the LanguageFontMapping
        /// </summary>
        public List<string> AllSupportedScripts
        {
            get
            {
                List<string> ret = LanguageFontMapping.Keys.Where(k => !k.Equals(FontHelpers.DEFAULT_SCRIPT, StringComparison.OrdinalIgnoreCase)).ToList();
                return ret;
            }
        }


    }
}
