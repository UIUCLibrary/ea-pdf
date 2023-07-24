﻿using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Org.BouncyCastle.Ocsp;
using RoyT.TrueType;
using RoyT.TrueType.Helpers;
using RoyT.TrueType.Tables.Name;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class FontHelper
    {
        public enum BaseFontFamily
        {
            [Description("Serif")] Serif = 0,
            [Description("Sans-Serif")] SansSerif = 1,
            [Description("Monospace")] Monospace = 2
        }


        public static Dictionary<BaseFontFamily, List<string>> GetDictionaryOfFonts(string fontFolder, Dictionary<Regex, BaseFontFamily> baseFontMapping)
        {
            var fontList = FontData.GetList(fontFolder, baseFontMapping);
            return GetDictionaryOfFonts(fontList);
        }

        private static Dictionary<BaseFontFamily, List<string>> GetDictionaryOfFonts(List<FontData> fontList)
        {
            var ret = new Dictionary<BaseFontFamily, List<string>>();
            var grouped = fontList.GroupBy(f => f.BaseFamily);
            foreach (var group in grouped)
            {
                var fontset = new List<string>();
                //for a font set using unified naming scheme shorter family names usually indicate basic western fonts set, then smaller files size indicates more common languages (???)
                foreach (var f in group.DistinctBy(g => g.Family).OrderBy(f => f.Family.Length).ThenBy(f => f.FileSize)) 
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
            xwriter.WriteAttributeString("name", BaseFontFamily.Serif.GetDescriptionLower());
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
            xwriter.WriteAttributeString("name", BaseFontFamily.SansSerif.GetDescriptionLower());
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
            xwriter.WriteAttributeString("name", BaseFontFamily.Monospace.GetDescriptionLower());
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

        private static string GuessFontStyle(TrueTypeFont font)
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

        private static string GuessFontWeight(TrueTypeFont font)
        {
            var subfamily = NameHelper.GetName(NameId.FontSubfamilyName, CultureInfo.CurrentCulture, font);

            var weight = "normal";
            if (subfamily.Contains("Bold", StringComparison.OrdinalIgnoreCase))
            {
                weight = "bold";
            }
            return weight;
        }

        private static BaseFontFamily GuessBaseFontFamily(TrueTypeFont font, Dictionary<Regex, FontHelper.BaseFontFamily> baseFontMapping)
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

        public class FontData
        {
            public string Family { get; set; } = string.Empty;
            public string Subfamily { get; set; } = string.Empty;
            public string Style { get; set; } = string.Empty;
            public string Weight { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public BaseFontFamily BaseFamily { get; set; }
            public long FileSize { get; set; }

            public static List<FontData> GetList(string fontFolder, Dictionary<Regex, FontHelper.BaseFontFamily> baseFontMapping)
            {
                List<FontData> fontDataList = new();

                var ttfFiles = Directory.GetFiles(fontFolder, "*.ttf", SearchOption.AllDirectories);

                foreach (var ttfFile in ttfFiles)
                {
                    var font = TrueTypeFont.FromFile(ttfFile);
                    var family = NameHelper.GetName(NameId.FontFamilyName, CultureInfo.CurrentCulture, font);
                    var subfamily = NameHelper.GetName(NameId.FontSubfamilyName, CultureInfo.CurrentCulture, font);

                    var style = GuessFontStyle(font);
                    var weight = GuessFontWeight(font);
                    var bas = GuessBaseFontFamily(font, baseFontMapping);

                    var fontData = new FontData
                    {
                        Family = family,
                        Subfamily = subfamily,
                        Style = style,
                        Weight = weight,
                        Path = ttfFile,
                        BaseFamily = bas,
                        FileSize = new FileInfo(ttfFile).Length
                    };

                    fontDataList.Add(fontData);
                }

                return fontDataList;
            }


        }
    }

}