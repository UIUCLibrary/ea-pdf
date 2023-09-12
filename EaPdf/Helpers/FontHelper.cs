using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using RoyT.TrueType;
using RoyT.TrueType.Helpers;
using RoyT.TrueType.Tables.Name;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class FontHelper
    {

        public const string SERIF = "serif";
        public const string SANS_SERIF = "sans-serif";
        public const string MONOSPACE = "monospace";


        public enum BaseFontFamily
        {
            Serif = 0,
            SansSerif = 1,
            Monospace = 2
        }

        /// <summary>
        /// Return a dictionary of fonts where the key is the base font family (serif, sans-serif, monospace) and the value is a list of font families in that family
        /// The list of fonts is derived from a folder of font files
        /// </summary>
        /// <param name="fontFolder"></param>
        /// <param name="baseFontMapping"></param>
        /// <returns></returns>
        public static Dictionary<BaseFontFamily, List<string>> GetDictionaryOfFonts(string fontFolder, Dictionary<Regex, BaseFontFamily> baseFontMapping)
        {
            var fontList = FontData.GetList(fontFolder, baseFontMapping);
            return GetDictionaryOfFonts(fontList);
        }

        /// <summary>
        /// Return a dictionary of fonts where the key is the base font family (serif, sans-serif, monospace) and the value is a list of font families in that family
        /// This values are sorted so that the smallest font file is first in the list; this seems to make for smaller PDF files, especially for the RenderX XEP processor
        /// </summary>
        /// <param name="fontList"></param>
        /// <returns></returns>
        private static Dictionary<BaseFontFamily, List<string>> GetDictionaryOfFonts(List<FontData> fontList)
        {
            var ret = new Dictionary<BaseFontFamily, List<string>>();
            var grouped = fontList.GroupBy(f => f.BaseFamily);
            foreach (var group in grouped)
            {
                var fontset = new List<string>();

                //for a font set, look in the smaller fonts first; this seems to minimize the PDF file size for RenderX XEP processor, and doesn't make much different for Apache FOP processor
                foreach (var f in group.DistinctBy(g => g.Family).OrderBy(f => f.FileSize))
                {
                    fontset.Add(f.Family);
                }
                ret.Add(group.Key, fontset);
            }
            return ret;
        }

        public static string GenerateXepFontsConfig(string fontFolder, Dictionary<Regex, BaseFontFamily> baseFontMapping)
        {
            var fontList = FontData.GetList(fontFolder, baseFontMapping);
            var fontDict = GetDictionaryOfFonts(fontList);

            var sb = new StringBuilder();
            var xwriter = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true });


            xwriter.WriteStartDocument();
            xwriter.WriteStartElement("fonts");
            xwriter.WriteAttributeString("default-family", fontDict[BaseFontFamily.Serif][0]); //first font in serif set
            xwriter.WriteStartElement("font-group");
            xwriter.WriteAttributeString("xml", "base", "http://www.w3.org/XML/1998/namespace", "../Fonts/"); //make sure there is a trailing slash
            xwriter.WriteAttributeString("label", Path.GetFileName(fontFolder));
            xwriter.WriteAttributeString("embed", "true");
            xwriter.WriteAttributeString("subset", "true");
            xwriter.WriteAttributeString("initial-encoding", "standard");


            var grouped = fontList.GroupBy(f => f.Family);
            foreach (var group in grouped)
            {
                xwriter.WriteStartElement("font-family");
                xwriter.WriteAttributeString("name", group.Key);
                foreach (var font in group)
                {
                    xwriter.WriteStartElement("font");
                    if (font.Weight != "normal")
                    {
                        xwriter.WriteAttributeString("weight", font.Weight);
                    }
                    if (font.Style != "normal")
                    {
                        xwriter.WriteAttributeString("style", font.Style);
                    }
                    xwriter.WriteStartElement("font-data");
                    xwriter.WriteAttributeString("ttf", Path.GetRelativePath(fontFolder, font.Path));
                    xwriter.WriteEndElement(); // font-data
                    xwriter.WriteEndElement(); // font
                }
                xwriter.WriteEndElement(); // font-family
            }

            xwriter.WriteEndElement(); // font-group

            WriteXepAliases(xwriter, fontList);

            xwriter.WriteEndElement(); // fonts
            xwriter.WriteEndDocument();
            xwriter.Flush();
            xwriter.Close();
            return sb.ToString();
        }

        private static void WriteXepAliases(XmlWriter xwriter, List<FontData> fonts)
        {
            var dict = GetDictionaryOfFonts(fonts);

            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", SERIF);
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.Serif]));
            xwriter.WriteEndElement(); // font-alias
            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", "Times");
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.Serif]));
            xwriter.WriteEndElement(); // font-alias
            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", "Times Roman");
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.Serif]));
            xwriter.WriteEndElement(); // font-alias
            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", "Times New Roman");
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.Serif]));
            xwriter.WriteEndElement(); // font-alias
            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", "Times-Roman");
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.Serif]));
            xwriter.WriteEndElement(); // font-alias

            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", SANS_SERIF);
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.SansSerif]));
            xwriter.WriteEndElement(); // font-alias
            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", "Helvetica");
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.SansSerif]));
            xwriter.WriteEndElement(); // font-alias
            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", "Arial");
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.SansSerif]));
            xwriter.WriteEndElement(); // font-alias
            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", "SansSerif");
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.SansSerif]));
            xwriter.WriteEndElement(); // font-alias

            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", MONOSPACE);
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.Monospace]));
            xwriter.WriteEndElement(); // font-alias
            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", "Courier");
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.Monospace]));
            xwriter.WriteEndElement(); // font-alias
            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", "Courier New");
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.Monospace]));
            xwriter.WriteEndElement(); // font-alias
            xwriter.WriteStartElement("font-alias");
            xwriter.WriteAttributeString("name", "Monospaced");
            xwriter.WriteAttributeString("value", string.Join(",", dict[BaseFontFamily.Monospace]));
            xwriter.WriteEndElement(); // font-alias
        }

        public static string GenerateFopFontsConfig(string fontFolder, Dictionary<Regex, BaseFontFamily> baseFontMapping)
        {
            var fontList = FontData.GetList(fontFolder, baseFontMapping);

            var sb = new StringBuilder();
            var xwriter = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true });

            xwriter.WriteStartDocument();
            xwriter.WriteStartElement("fonts");
            foreach (var font in fontList.OrderBy(f => f.Family))
            {
                xwriter.WriteStartElement("font");
                xwriter.WriteAttributeString("embed-url", Path.GetRelativePath(fontFolder, font.Path));
                WriteFopFontTriplet(xwriter, font.Family, font.Style, font.Weight);
                xwriter.WriteEndElement(); // font
            }
            WriteFopSubstitutions(xwriter, fontList);
                
            xwriter.WriteEndElement(); // fonts

            xwriter.WriteEndDocument();
            xwriter.Flush();
            xwriter.Close();
            return sb.ToString();
        }

        private static void WriteFopSubstitutions(XmlWriter xwriter, List<FontData> fonts)
        {
            //UNDONE:  not sure if I even need this
        }

        private static void WriteFopFontTriplet(XmlWriter xwriter, FontData font)
        {
            switch (font.BaseFamily)
            {
                case BaseFontFamily.SansSerif:
                    WriteFopSansSerifTriplet(xwriter, font.Style, font.Weight);
                    break;
                case BaseFontFamily.Serif:
                    WriteFopSerifTriplet(xwriter, font.Style, font.Weight);
                    break;
                case BaseFontFamily.Monospace:
                    WriteFopMonospacedTriplet(xwriter, font.Style, font.Weight);
                    break;
                default:
                    WriteFopFontTriplet(xwriter, font.Family, font.Style, font.Weight);
                    break;
            }
        }

        private static void WriteFopSansSerifTriplet(XmlWriter xwriter, string style, string weight)
        {
            WriteFopFontTriplet(xwriter, "Helvetica", style, weight);
            WriteFopFontTriplet(xwriter, "Arial", style, weight);
            WriteFopFontTriplet(xwriter, "sans-serif", style, weight);
            WriteFopFontTriplet(xwriter, "SansSerif", style, weight);
        }

        private static void WriteFopSerifTriplet(XmlWriter xwriter, string style, string weight)
        {
            WriteFopFontTriplet(xwriter, "Times", style, weight);
            WriteFopFontTriplet(xwriter, "Times Roman", style, weight);
            WriteFopFontTriplet(xwriter, "Time New Roman", style, weight);
            WriteFopFontTriplet(xwriter, "Times-Roman", style, weight);
            WriteFopFontTriplet(xwriter, "serif", style, weight);
            WriteFopFontTriplet(xwriter, "any", style, weight);
        }

        private static void WriteFopMonospacedTriplet(XmlWriter xwriter, string style, string weight)
        {
            WriteFopFontTriplet(xwriter, "Courier", style, weight);
            WriteFopFontTriplet(xwriter, "monospace", style, weight);
            WriteFopFontTriplet(xwriter, "Monospaced", style, weight);
        }

        private static void WriteFopFontTriplet(XmlWriter xwriter, string name, string style, string weight)
        {
            xwriter.WriteStartElement("font-triplet");
            xwriter.WriteAttributeString("name", name);
            xwriter.WriteAttributeString("weight", weight);
            xwriter.WriteAttributeString("style", style);
            xwriter.WriteEndElement(); // font-triplet
        }

        protected internal static string GuessFontStyle(TrueTypeFont font)
        {
            var subfamily = NameHelper.GetName(NameId.FontSubfamilyName, CultureInfo.CurrentCulture, font);

            var style = "normal";
            if (subfamily.Contains("Italic", StringComparison.OrdinalIgnoreCase) ||
                subfamily.Contains("Oblique", StringComparison.OrdinalIgnoreCase) ||
                subfamily.Contains("Inclined", StringComparison.OrdinalIgnoreCase))
            {
                style = "italic";
            }
            return style;
        }

        protected internal static string GuessFontWeight(TrueTypeFont font)
        {
            var subfamily = NameHelper.GetName(NameId.FontSubfamilyName, CultureInfo.CurrentCulture, font);

            var weight = "normal";
            if (subfamily.Contains("Bold", StringComparison.OrdinalIgnoreCase))
            {
                weight = "bold";
            }
            return weight;
        }

        protected internal static BaseFontFamily GuessBaseFontFamily(TrueTypeFont font, Dictionary<Regex, FontHelper.BaseFontFamily> baseFontMapping)
        {
            var family = NameHelper.GetName(NameId.FontFamilyName, CultureInfo.CurrentCulture, font);

            if (family.Contains("Sans", StringComparison.OrdinalIgnoreCase))
            {
                return BaseFontFamily.SansSerif;
            }
            else if (family.Contains("Serif", StringComparison.OrdinalIgnoreCase))
            {
                return BaseFontFamily.Serif;
            }
            else if (family.Contains("Mono", StringComparison.OrdinalIgnoreCase))
            {
                return BaseFontFamily.Monospace;
            }
            else //use the baseFontMapping to guess the base font family
            {
                return baseFontMapping.FirstOrDefault(kv => kv.Key.IsMatch(family)).Value;
            }
        }

        public static bool FontContainsCharacter(TrueTypeFont font, char c)
        {
            uint glyphIndex = GlyphHelper.GetGlyphIndex(c,font);
            return glyphIndex != 0;
        }   
    }

}
