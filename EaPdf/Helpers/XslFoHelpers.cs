using Microsoft.Extensions.Logging;
using System.Xml;
using System.Xml.Linq;
using static UIUCLibrary.EaPdf.Helpers.FontHelpers;

namespace UIUCLibrary.EaPdf.Helpers
{
    class XslFoHelpers
    {
        public const string XSL_FO = "fo";
        public const string XSL_FO_NS = "http://www.w3.org/1999/XSL/Format";


        private XmlDocument XDoc { get; init; } = new();
        private string FoFilePath { get; init; } = string.Empty;

        public XslFoHelpers(string foFilePath)
        {
            FoFilePath = foFilePath;
            XDoc.Load(FoFilePath);
        }

        /// <summary>
        /// Save the XSL-FO back to the same file as was opened
        /// </summary>
        public void SaveFoFile()
        {
            SaveFoFile(FoFilePath);
        }

        /// <summary>
        /// Save the XSL-FO to the given file path
        /// </summary>
        /// <param name="foFilePath"></param>
        public void SaveFoFile(string foFilePath)
        {
            XDoc.Save(foFilePath);
        }

        /// <summary>
        /// Prevent certain ligatures from being formed in the XSL-FO
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void PreventLigatures()
        {

            XmlNodeList? nodes = XDoc.SelectNodes("//text()");

            if (nodes != null)
            {
                foreach (XmlText xtext in nodes)
                {
                    var parent = xtext.ParentNode;
                    if (parent != null)
                    {
                        var newTxt = UnicodeHelpers.PreventLigatures(xtext.InnerText);
                        xtext.InnerText = newTxt;
                    }
                    else
                    {
                        throw new Exception($"Unable to find parent node for text node: '{xtext.OuterXml}'");
                    }
                }
            }

        }

        /// <summary>
        /// Based on the unicode script of the text and the font settings, wrap non-Latin text in an inline element with font-family attribute
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="Exception"></exception>
        public void WrapLanguagesInFontFamily(EaxsToEaPdfProcessorSettings settings)
        {

            XmlNodeList? nodes = XDoc.SelectNodes("//text()");

            List<string> allUsedFonts = new();

            if (nodes != null)
            {
                foreach (XmlText xtextNode in nodes)
                {
                    //get the nearest ancestor that has one of the default font-families; used to determine the default font-family for this block of text
                    XmlElement? ancestorFontFamilyElem = xtextNode.SelectSingleNode($"ancestor::*[contains(@font-family,'{SERIF}') or contains(@font-family,'{SANS_SERIF}') or contains(@font-family,'{MONOSPACE}')]") as XmlElement;
                    BaseFontFamily defaultFontFamily = BaseFontFamily.Serif;
                    if (ancestorFontFamilyElem != null)
                    {
                        string fontFamily = ancestorFontFamilyElem.GetAttribute("font-family");
                        if (!string.IsNullOrWhiteSpace(fontFamily))
                        {
                            var fontFamilies = fontFamily.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            if (fontFamilies.Contains(settings.GetDefaultFontFamily(BaseFontFamily.Serif)))
                            {
                                defaultFontFamily = BaseFontFamily.Serif;
                            }
                            else if (fontFamilies.Contains(settings.GetDefaultFontFamily(BaseFontFamily.SansSerif)))
                            {
                                defaultFontFamily = BaseFontFamily.SansSerif;
                            }
                            else if (fontFamilies.Contains(settings.GetDefaultFontFamily(BaseFontFamily.Monospace)))
                            {
                                defaultFontFamily = BaseFontFamily.Monospace;
                            }
                        }
                    }


                    var parent = xtextNode.ParentNode;
                    if (parent != null)
                    {
                        if (parent.LocalName != "bookmark-title") //bookmark-title cannot have inline elements
                        {
                            List<string> usedFonts = new();
                            var wrappedTextNode = WrapFontFamilyForSpecialLanguages(xtextNode, defaultFontFamily, settings, ref usedFonts);
                            parent.ReplaceChild(wrappedTextNode, xtextNode);

                            allUsedFonts.AddRange(usedFonts);
                            allUsedFonts = allUsedFonts.Distinct().ToList();
                        }
                    }
                    else
                    {
                        throw new Exception($"Unable to find parent node for text node: '{xtextNode.OuterXml}'");
                    }
                }
            }

        }

        /// <summary>
        /// Segment the text based on unicode script and look for a font to support that script, 
        /// if found, wrap the text in an inline element with font-family attribute
        /// </summary>
        /// <param name="originalText"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        private XmlNode WrapFontFamilyForSpecialLanguages(XmlText originalText, BaseFontFamily defaultFontFamily, EaxsToEaPdfProcessorSettings settings, ref List<string> usedFonts)
        {
            if (originalText == null)
            {
                throw new ArgumentNullException(nameof(originalText));
            }

            if (originalText.OwnerDocument == null)
            {
                throw new Exception($"XmlText {nameof(originalText)} does not have an OwnerDocument");
            }

            var text = originalText.InnerText;
            var offsets = UnicodeHelpers.PartitionTextByUnicodeScript(text, out List<(LogLevel, string)> messages);

            XmlNode ret;

            if (offsets.Count > 0 && offsets.Any(o => settings.AllSupportedScripts.Contains(o.scriptName, StringComparer.OrdinalIgnoreCase)))
            {
                ret = originalText.OwnerDocument.CreateDocumentFragment();
                foreach (var offset in offsets)
                {
                    //TODO:  Look for the base font family of the parent elements and use that same base font family
                    var fonts = settings.GetFontFamily(offset.scriptName, defaultFontFamily);

                    if (fonts == null)
                    {
                        var newElem = originalText.OwnerDocument.CreateTextNode(text[offset.range]);
                        ret.AppendChild(newElem);
                    }
                    else
                    {
                        var newElem = originalText.OwnerDocument.CreateElement(XSL_FO, "inline", XSL_FO_NS);
                        newElem.SetAttribute("font-family", fonts);
                        usedFonts.AddRange(fonts.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        usedFonts = usedFonts.Distinct().ToList();
                        newElem.InnerText = text[offset.range];
                        ret.AppendChild(newElem);
                    }

                }
            }
            else
            {
                ret = originalText;
            }

            return ret;
        }



    }
}
