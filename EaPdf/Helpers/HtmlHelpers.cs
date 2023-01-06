using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;

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
        public static string ConvertHtmlToXhtmlUsingAngleSharp(string html, ref List<(LogLevel level, string message)> messages)
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
        public static string ConvertHtmlToXhtmlUsingHap(string html, ref List<(LogLevel level, string message)> messages)
        {

            var flags = HtmlNode.ElementsFlags;

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

            RemoveCommentsHap(hdoc);

            var htmlNode = AddHtmlHeadBodyHap(hdoc, ref messages);

            if (htmlNode != null)
            {

                //recursively check each element for namespace declarations and make sure the element and attribute names and prefixes are valid 
                Stack<List<string>> namespacePrefixes = new();
                FixNamesHap(htmlNode, ref namespacePrefixes, ref messages);
                if (namespacePrefixes.Count > 0)
                {
                    messages.Add((LogLevel.Warning, $"There are unclosed tags"));
                }

                ret = htmlNode.OuterHtml;
            }


            if (XmlHelpers.TryReplaceInvalidXMLChars(ref ret, out string msg))
            {
                messages.Add((LogLevel.Warning, msg));
            }


            ret = FixCDataHap(ret);

            return ret;
        }

        /// <summary>
        ///Correct the CDATA sections so that they are valid
        ///     i.e. <title>
        ///           //<![CDATA[
        ///           ...
        ///            //]]>//
        ///          </title >
        ///       this should just be <title><![CDATA[...]]></title>
        ///       The // comments are just useful for the <script> element
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string FixCDataHap(string html)
        {
            var ret = html;
            foreach (var ef in HtmlNode.ElementsFlags.Where(ef => ef.Value.HasFlag(HtmlElementFlag.CData)))
            {
                var name = ef.Key;
                if (name != "script")
                {
                    ret = Regex.Replace(ret, @$"<{name}>\s*//<!\[CDATA\[[\r\n]{{1,2}}", $"<{name}><![CDATA[");
                    ret = Regex.Replace(ret, @$"[\r\n]{{1,2}}//]]>//[\r\n]{{1,2}}</{name}>", @$"]]></{name}>");
                }

            }

            return ret;
        }

        private static void RemoveCommentsHap(HtmlDocument hdoc)
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

        /// <summary>
        /// Add missing elements if needed, html, head, and/or body
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        private static HtmlNode? AddHtmlHeadBodyHap(HtmlDocument hdoc, ref List<(LogLevel level, string message)> messages)
        {
            int msgCount = messages.Count;

            //find the root html tag
            var htmlNode = hdoc.DocumentNode.Descendants().FirstOrDefault(x => x.Name == "html");

            if (htmlNode == null)
            {
                //Add an html element
                var newHtml = hdoc.CreateElement("html");
                var nodes = hdoc.DocumentNode.ChildNodes; //make sure comments are removed before doing this or the DOCTYPE decl could end up improperly nested
                newHtml.AppendChildren(nodes);
                hdoc.DocumentNode.RemoveAllChildren();
                hdoc.DocumentNode.AppendChild(newHtml);
                htmlNode = newHtml;
                messages.Add((LogLevel.Information, "There was no top-level html tag, so one was added"));
            }

            if (htmlNode != null)
            {
                //remove existing xmlns attribute so a new xmlns can be added for xhtml
                htmlNode.Attributes.Remove("xmlns");
                htmlNode.Attributes.Add("xmlns", "http://www.w3.org/1999/xhtml");

                var body = htmlNode.Element("body");
                if (body == null)
                {
                    //Add a body element
                    var newBody = hdoc.CreateElement("body");
                    var nodes = htmlNode.ChildNodes;
                    newBody.AppendChildren(nodes);
                    htmlNode.RemoveAllChildren();
                    htmlNode.AppendChild(newBody);
                    messages.Add((LogLevel.Information, "There was no body tag, so one was added"));
                    body = newBody;
                }

                var head = htmlNode.Element("head");
                if (head == null)
                {
                    //Add a head element
                    var newHead = hdoc.CreateElement("head");

                    //Gather up all the elements that are only allowed in the head and move them there: title, base, meta, link, style
                    //We assume that if there was already a head that it contained the appropriate elements

                    //move title
                    var titles = body.SelectNodes("title");
                    if (titles != null)
                    {
                        foreach (var title in titles)
                        {
                            newHead.AppendChild(title);
                            body.RemoveChild(title);
                        }
                    }

                    //move base
                    var bases = body.SelectNodes("base");
                    if (bases != null)
                    {
                        foreach (var bse in bases)
                        {
                            newHead.AppendChild(bse);
                            body.RemoveChild(bse);
                        }
                    }

                    //move style
                    var styles = body.SelectNodes("style");
                    if (styles != null)
                    {
                        foreach (var style in styles)
                        {
                            newHead.AppendChild(style);
                            body.RemoveChild(style);
                        }
                    }

                    //move meta
                    var metas = body.SelectNodes("meta");
                    if (metas != null)
                    {
                        foreach (var meta in metas)
                        {
                            newHead.AppendChild(meta);
                            body.RemoveChild(meta);
                        }
                    }

                    //move link
                    var links = body.SelectNodes("link");
                    if (links != null)
                    {
                        foreach (var link in links)
                        {
                            newHead.AppendChild(link);
                            body.RemoveChild(link);
                        }
                    }

                    htmlNode.PrependChild(newHead);
                    messages.Add((LogLevel.Information, "There was no head tag, so one was added"));
                    head = newHead;

                }

                //Add a valid root element to the body if needed, body does not allow mixed content, so it needs an element
                var breakText = body.OuterHtml;
                var bodyTextNodes = body.ChildNodes.Where(n => n.NodeType==HtmlNodeType.Text);
                if (bodyTextNodes != null)
                {
                    if (bodyTextNodes.Any(n => !string.IsNullOrWhiteSpace(((HtmlTextNode)n).Text)))
                    {
                        var newDiv = hdoc.CreateElement("div");
                        newDiv.AppendChildren(body.ChildNodes);
                        body.RemoveAllChildren();
                        body.AppendChild(newDiv);
                        messages.Add((LogLevel.Information, "The body element does not allow mixed content, so all content was wrapped in new div element"));
                    }
                }

                //add a meta tag to indicate how the xhtml was generated
                //this also ensures at least one child in the head
                var newMeta = hdoc.CreateElement("meta");
                newMeta.Attributes.Add("name", "generator");
                newMeta.Attributes.Add("content", $"Conversion to XHTML performed by {typeof(HtmlHelpers).Namespace}");
                head.PrependChild(newMeta);


                //see if the htmlNode contains anything besides just a head and body
                //if it does move it to the correct place
                var kids = htmlNode.ChildNodes;
                
                if (kids != null)
                {
                    bool beforeBody = true;

                    List<HtmlNode> toBeRemoved = new();

                    foreach(var kid in kids)
                    {
                        var name = kid.Name;
                        if (name != "head" && name != "body" && kid.NodeType == HtmlNodeType.Element)
                        {
                            if (new string[] { "title", "base", "meta", "link", "style" }.Contains(name))
                            {
                                //Move it to the head
                                head.AppendChild(kid);
                                toBeRemoved.Add(kid);
                                messages.Add((LogLevel.Information, $"Moving {name} to the head"));
                            }
                            else 
                            {
                                //Move it to the Body
                                if (beforeBody)
                                    body.PrependChild(kid);
                                else
                                    body.AppendChild(kid);

                                toBeRemoved.Add(kid);
                                messages.Add((LogLevel.Information, $"Moving {name} to the body"));
                            }
                        }
                        else if (name == "body" && beforeBody)
                        {
                            beforeBody = false;
                        }
                        else if(name == "body" && !beforeBody)
                        {
                            // looks like there are multiple bodies. Rename the second body to div and move it to the bottom of the previous body
                            kid.Name = "div";
                            body.AppendChild(kid);
                            toBeRemoved.Add(kid);
                            messages.Add((LogLevel.Information, $"The html had another body; it was renamed to div and moved to the bottom of the first body."));

                        }
                        else if (kid.NodeType == HtmlNodeType.Text && !beforeBody && !string.IsNullOrWhiteSpace(((HtmlTextNode)kid).Text))
                        {
                            // looks like some straggling text after the body, put it into a div and append it to the body
                            var newDiv = hdoc.CreateElement("div");
                            newDiv.AppendChild(kid);
                            body.AppendChild(newDiv);
                            toBeRemoved.Add(kid);
                            messages.Add((LogLevel.Information, $"Moving trailing text into the body"));
                        }
                    }

                    foreach (var removeMe in toBeRemoved)
                        htmlNode.RemoveChild(removeMe);
                }


                
            }

            if (messages.Count > msgCount)
                messages.Insert(msgCount, (LogLevel.Warning, "Html was not valid; see the following info messages for details"));

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

        public static string ConvertHtmlToXhtml(string html, ref List<(LogLevel level, string message)> messages, WhichParser whichParser)
        {
            if (whichParser == WhichParser.HtmlAgilityPack)
            {
                return ConvertHtmlToXhtmlUsingHap(html, ref messages);
            }
            else if (whichParser == WhichParser.AngleSharp)
            {
                return ConvertHtmlToXhtmlUsingAngleSharp(html, ref messages);
            }
            else if (whichParser == WhichParser.TidyHtml5)
            {
                return ConvertHtmlToXhtmlUsingTidy(html, ref messages);
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
        public static string ConvertHtmlToXhtmlUsingTidy(string html, ref List<(LogLevel level, string message)> messages)
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
