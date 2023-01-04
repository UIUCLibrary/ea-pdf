using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Events;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Dom.Events;
using AngleSharp.Html.Parser;
using AngleSharp.Text;
using AngleSharp.Xhtml;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace UIUCLibrary.EaPdf.Helpers
{
    internal class HtmlHelpers
    {

        /// <summary>
        /// Use AngleSharp to parse html and return it as <em>well-formed</em> xhtml
        /// Note that it may not be valid xhtml if the source html document is not valid
        /// </summary>
        /// <param name="html">the input html string</param>
        /// <returns></returns>
        public static string ConvertHtmlToXhtmlUsingAngleSharp(string html, bool removeComments, ref List<(LogLevel level, string message)> messages)
        {

            var opts = new HtmlParserOptions()
            {
                IsAcceptingCustomElementsEverywhere = true,
                IsEmbedded = true,
                OnCreated = CreatedAngleSharp
            };

            var parser = new HtmlParser(opts);
            parser.Error += ParseErrorAngleSharp;


            //NOTE: AngleSharp conforms strictly to the HTML5 spec, meaning that custom tags occuring in the head section (common in old html emails) can get moved to the body
            //      If the custom tags are empty, it can cause  even worse problems because AngleSharp interprets them as having content which can cause things which should be
            //      kept in the head to be moved to the body with all the body content nested inside these custom tags
            var doc = parser.ParseDocument(html);

            var formatter = new EaPdfXhtmlMarkupFormatter(true, true);
            doc.DocumentElement.SetAttribute("xmlns", "http://www.w3.org/1999/xhtml");
            var ret = doc.DocumentElement.ToHtml(formatter);
            messages.AddRange(formatter.ConversionLog);

            return ret.Trim();
        }

        public static void ParseErrorAngleSharp(object sender, Event evt)
        {
            //TODO: What do I even want to do with these events
        }

        public static void CreatedAngleSharp(IElement element, TextPosition pos)
        {
            //TODO:  Not sure what to do with this
        }

        /// <summary>
        /// Use the HTML Agility Pack to parse the HTML and return it as valid XHTML
        /// </summary>
        /// <param name="html"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string ConvertHtmlToXhtmlUsingHap(string html, bool removeComments, ref List<(LogLevel level, string message)> messages)
        {
            string ret = html;

            if (string.IsNullOrWhiteSpace(html))
            {
                messages.Add((LogLevel.Warning, "The html content is empty"));
                return ret;
            }

            //replace named character entities with the characters

            //DeEntitize also replaces standard xml entities, so I need to double-encode them to prevent this
            ret = ret.Replace("&amp;", "&amp;amp;");
            ret = ret.Replace("&lt;", "&amp;lt;");
            ret = ret.Replace("&gt;", "&amp;gt;");
            ret = ret.Replace("&quot;", "&amp;quot;");
            ret = ret.Replace("&apos;", "&amp;apos;");
            ret = HtmlEntity.DeEntitize(ret);


            var hdoc = new HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionOutputAsXml = true,
                OptionReadEncoding = false,
                OptionXmlForceOriginalComment = false,
                BackwardCompatibility = false,
                OptionPreserveXmlNamespaces = true
            };
            hdoc.LoadHtml(ret);

            if (removeComments) //this seems to get rid of DOCTYPE declarations too
                RemoveCommentsHap(hdoc, ref messages);

            var htmlNode = AddXHtmlHeadBodyHap(hdoc, ref messages);

            if (htmlNode != null)
            {

                //recursively check each element for namespace declarations and make sure the element and attribute names and prefixes are valid 
                Stack<List<string>> namespacePrefixes = new();
                FixNamesHap(htmlNode, ref namespacePrefixes, ref messages);
                if (namespacePrefixes.Count > 0)
                {
                    messages.Add((LogLevel.Warning, $"There are unclosed tags"));
                }

                //TODO: Make sure the html has a head and a body, and also make sure the elements that are only allowed in the head are moved there if needed

                ret = htmlNode.OuterHtml;
            }


            if (XmlHelpers.TryReplaceInvalidXMLChars(ref ret, out string msg))
            {
                messages.Add((LogLevel.Warning, msg));
            }

            return ret;
        }

        private static void RemoveCommentsHap(HtmlDocument hdoc, ref List<(LogLevel level, string message)> messages)
        {
            var nodes = hdoc.DocumentNode.SelectNodes("//comment()");
            if (nodes != null)
            {
                foreach (HtmlNode comment in nodes)
                {
                    comment.ParentNode.RemoveChild(comment);
                }
            }
        }

        private static HtmlNode? AddXHtmlHeadBodyHap(HtmlDocument hdoc, ref List<(LogLevel level, string message)> messages)
        {
            //find the root html tag
            var htmlNode = hdoc.DocumentNode.Descendants().FirstOrDefault(x => x.Name == "html");

            if (htmlNode == null)
            {
                //Add an html element
                var el = hdoc.CreateElement("html");
                el.Attributes.Add("xmlns", "http://www.w3.org/1999/xhtml");
                var nodes = hdoc.DocumentNode.ChildNodes; //TODO: DOCTYPE decl must be removed before this
                el.AppendChildren(nodes);
                hdoc.DocumentNode.RemoveAllChildren();
                hdoc.DocumentNode.AppendChild(el);
                htmlNode = el;
                messages.Add((LogLevel.Warning, "There was no top-level html tag, so one was added"));
            }
            else
            {
                //remove existing xmlns attribute so a new xmlns can be added for xhtml
                htmlNode.Attributes.Remove("xmlns");
                htmlNode.Attributes.Add("xmlns", "http://www.w3.org/1999/xhtml");
            }

            return htmlNode;
        }

        /// <summary>
        /// Check that namespace prefixes have been previously declared using xmlns, if not escape the name to make it valid 
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="namespacePrefixes"></param>
        private static void FixNamesHap(HtmlNode elem, ref Stack<List<string>> namespacePrefixes, ref List<(LogLevel level, string message)> messages)
        {
            //collect the namespace prefixes on the stack
            namespacePrefixes.Push(new List<string>());
            foreach (var attr in elem.Attributes)
            {
                if (attr.Name.StartsWith("xmlns:", StringComparison.OrdinalIgnoreCase))
                {
                    namespacePrefixes.Peek().Add(attr.Name[6..]);
                }
            }

            //check the element name
            var origName = elem.Name;
            if (origName.Contains(':'))
            {
                var prefix = origName[..origName.IndexOf(':')];
                var localName = origName[(origName.IndexOf(':') + 1)..];
                //make sure prefix is in the stack of prefixes
                if (!namespacePrefixes.Any(s => s.Any(p => p == prefix)))
                {
                    //the prefix hasn't been declared, so escape the whole name
                    elem.Name = XmlConvert.EncodeLocalName(origName);
                    messages.Add((LogLevel.Warning, $"Element name '{origName}' prefix was not in scope, so the name was escaped to '{elem.Name}'"));
                }
                else
                {
                    //TODO: Do I need to accomodate invalid prefix strings; this could be a challange if I need to correct xmlns attributes
                    elem.Name = $"{prefix}:{XmlConvert.EncodeLocalName(localName)}";
                    if (origName != elem.Name)
                        messages.Add((LogLevel.Warning, $"Element name '{origName}' contained invalid characters, so the name was escaped to '{elem.Name}'"));
                }
            }
            else
            {
                elem.Name = XmlConvert.EncodeLocalName(origName);
                if (origName != elem.Name)
                    messages.Add((LogLevel.Warning, $"Element name '{origName}' contained invalid characters, so the name was escaped to '{elem.Name}'"));
            }

            //check the attribute names
            foreach (var attr in elem.Attributes.Where(a => !a.Name.StartsWith("xml", StringComparison.OrdinalIgnoreCase)))
            {
                var origAttrName = attr.Name;
                if (origAttrName.Contains(':'))
                {
                    var prefix = origAttrName[..origAttrName.IndexOf(':')];
                    var localName = origAttrName[(origAttrName.IndexOf(':') + 1)..];
                    //make sure prefix is in the stack of prefixes
                    if (!namespacePrefixes.Any(s => s.Any(p => p == prefix)))
                    {
                        //the prefix hasn't been declared, so escape it
                        attr.Name = XmlConvert.EncodeLocalName(origAttrName);
                        messages.Add((LogLevel.Warning, $"Attribute name '{origAttrName}' prefix was not in scope, so the name was escaped to '{attr.Name}'"));
                    }
                    else
                    {
                        //TODO: Do I need to accomodate invalid prefix strings; this could be a challange if I need to correct xmlns attributes
                        attr.Name = $"{prefix}:{XmlConvert.EncodeLocalName(localName)}";
                        if (origAttrName != attr.Name)
                            messages.Add((LogLevel.Warning, $"Attribute name '{origAttrName}' contained invalid characters, so the name was escaped to '{attr.Name}'"));
                    }
                }
                else
                {
                    attr.Name = XmlConvert.EncodeLocalName(origAttrName);
                    if (origAttrName != attr.Name)
                        messages.Add((LogLevel.Warning, $"Attribute name '{origAttrName}' contained invalid characters, so the name was escaped to '{attr.Name}'"));
                }
            }

            foreach (var child in elem.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Element)
                {
                    FixNamesHap(child, ref namespacePrefixes, ref messages);
                }
            }

            namespacePrefixes.Pop();
        }

        public static string ConvertHtmlToXhtml(string html, bool removeComments, ref List<(LogLevel level, string message)> messages, WhichParser whichParser)
        {
            if (whichParser == WhichParser.HtmlAgilityPack)
            {
                return ConvertHtmlToXhtmlUsingHap(html, removeComments, ref messages);
            }
            else if (whichParser == WhichParser.AngleSharp)
            {
                return ConvertHtmlToXhtmlUsingAngleSharp(html, removeComments, ref messages);
            }
            else if (whichParser == WhichParser.TidyHtml5)
            {
                return ConvertHtmlToXhtmlUsingTidy(html, removeComments, ref messages);
            }
            else
            {
                throw new Exception($"Unexpected Html Parser {whichParser}");
            }
        }

        /// <summary>
        /// Use the Tidy HTML5 Managed to parse the HTML and return it as valid XHTML
        /// </summary>
        /// <param name="html"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string ConvertHtmlToXhtmlUsingTidy(string html, bool removeComments, ref List<(LogLevel level, string message)> messages)
        {
            throw new NotImplementedException("Tidy is not yet supported");
        }


        public enum WhichParser
        {
            HtmlAgilityPack,
            AngleSharp,
            TidyHtml5
        }
    }
}
