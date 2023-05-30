using AngleSharp.Css.Dom;
using AngleSharp.Css.Parser;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml;

namespace UIUCLibrary.EaPdf.Helpers
{

    internal class HtmlHelpers
    {
        //Add any needed non-standard character entities here; note that character entities are case-sensitive
        public static Dictionary<string, int> ExtraCharacterEntities = new() { { "QUOT", 0x22 } };

        /// <summary>
        /// Use the HTML Agility Pack to parse the HTML and return it as valid XHTML
        /// </summary>
        /// <param name="html"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        /// <returns></returns>
        public static string ConvertHtmlToXhtml(string html, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {

            //TODO: There are html files in the wild that have multiple <html> nested tags.  Might get a better result if we could combine them somehow
            //      For example see C:\Users\thabi\Source\UIUC\ea-pdf\SampleFiles\Testing\MozillaThunderbird\Inbox ID: 000001c8c9bf$e47a3590$04000100@KLINE

            string ret = html;

            if (string.IsNullOrWhiteSpace(html))
            {
                messages.Add((LogLevel.Warning, "The html content is empty"));
                return ret;
            }


            //replace named character entities with the characters they represent
            ret = FixCharacterEntities(ret);

            //load the html string into the HtmlAgilityPack parser
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

            //HAP seems to correct some doubly encoded character entities (inside attributes?), i.e. &amp;reg; becomes &reg;
            //so get the html back out of the HAP document and re-DeEntitize it, and then try to reload it; this seems to get rid of the problem
            try
            {
                ret = hdoc.DocumentNode.OuterHtml;
                ret = FixCharacterEntities(ret);
                hdoc.LoadHtml(ret);
            }
            catch (Exception ex)
            {
                messages.Add((LogLevel.Warning, $"Unable to reload html document node from outer html: {ex.Message}"));
            }

            CorrectEmptyNamespaceDeclarations(hdoc, ref messages, ignoreHtmlIssues);

            //rebalance the xhtml so it contains a single root html just one head and one body child
            //with the appropriate elements created and moved as needed
            var htmlNode = MakeHtmlMinimallyValid(hdoc, ref messages, ignoreHtmlIssues);

            //move styles to inline with the elements
            ConvertToInlineCssUsingAngleSharpCss(htmlNode, ref messages, ignoreHtmlIssues);

            //Normalize style properties and remove any inline style properties that are not supported by the HTML
            NormalizeStyleProperties(htmlNode, ref messages, ignoreHtmlIssues);

            //The rgba function is not supported by the XSL-FO processor, so convert it to rgb
            //This must occur after the inlining process, because that will move rgba values from stylesheets to inline style attributes
            ConvertStylesRgbaToRgb(hdoc, ref messages, ignoreHtmlIssues);

            //Fix improperly nested lists
            FixImproprerlyNestedLists(htmlNode, ref messages, ignoreHtmlIssues);

            //Fix non-unique id attributes -- make sure to do this after inlining the styles, because the inlining will require the ids to match
            //The XSL-FO processor will complain if ids are not unique
            FixNonUniqueIdValues(hdoc, ref messages, ignoreHtmlIssues);

            ConvertRelativeUrlsToAbsolute(hdoc, ref messages, ignoreHtmlIssues);

            //If an element has the "display: none" style set, delete it from the xhtml.  make sure this is after the inlining process
            RemoveDisplayNone(hdoc, ref messages, ignoreHtmlIssues);

            ret = htmlNode.OuterHtml;

            if (XmlHelpers.TryReplaceInvalidXMLChars(ref ret, out string msg))
            {
                messages.Add((LogLevel.Warning, msg));
            }

            //HAP does not correctly encode the CDATA sections, so we need to clean up their mess
            ret = FixCData(ret);

            return ret;
        }

        const string RGBA_REGEX = @"rgba\(\s*(?<r>[^,]+?)\s*,\s*(?<g>[^,]+?)\s*,\s*(?<b>[^,]+?)\s*,\s*(?<a>[^,]+?)\s*\)";

        private static void ConvertStylesRgbaToRgb(HtmlDocument hdoc, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            var styleNodes = hdoc.DocumentNode.SelectNodes("//*/@style[contains(translate(.,'RGBA','rgba'), 'rgba')]");
            if (styleNodes == null || styleNodes.Count == 0)
            {
                return;
            }

            foreach (var node in styleNodes)
            {
                var style = node.Attributes["style"];
                var styleValue = style.Value;
                var msgs = new List<(LogLevel level, string message)>();
                var newStyle = Regex.Replace(styleValue, RGBA_REGEX, new MatchEvaluator(delegate (Match match) { return ConvertRgbaToRgb(match, ref msgs); }), RegexOptions.IgnoreCase);
                messages.AddRange(msgs);
                style.Value = newStyle;
            }
        }

        private static string ConvertRgbaToRgb(Match match, ref List<(LogLevel level, string message)> messages)
        {
            string rgb = match.Value;

            (string r, string g, string b, float a) = ParseRgbaMatch(match, ref messages);

            if (string.IsNullOrWhiteSpace(r) || string.IsNullOrWhiteSpace(g) || string.IsNullOrWhiteSpace(b))
            {
                messages.Add((LogLevel.Warning, $"The color '{rgb}' value does not appear to be valid; leaving it as is."));
            }
            else
            {
                rgb = $"rgb({r},{g},{b})";
                if (a < 1.0)
                {
                    messages.Add((LogLevel.Warning, $"For color '{match.Value}' the alpha value '{a}' is less than 1.  XSL FO does not support transparency; the alpha channel was dropped."));
                }
                messages.Add((LogLevel.Debug, $"Color '{match.Value}' was converted to color '{rgb}'; the alpha channel was dropped."));
            }

            return rgb;
        }

        private static (string r, string g, string b, float a) ParseRgbaMatch(Match match, ref List<(LogLevel level, string message)> messages)
        {
            var r = match.Groups["r"].Value;
            var g = match.Groups["g"].Value;
            var b = match.Groups["b"].Value;
            var a = match.Groups["a"].Value;

            if (!Regex.IsMatch(r, "\\d{1,3}%?"))
            {
                messages.Add((LogLevel.Warning, $"For '{match.Value}' the red value '{r}' is not a valid percentage or integer value"));
            }
            if (!Regex.IsMatch(g, "\\d{1,3}%?"))
            {
                messages.Add((LogLevel.Warning, $"For '{match.Value}' the green value '{g}' is not a valid percentage or integer value"));
            }
            if (!Regex.IsMatch(b, "\\d{1,3}%?"))
            {
                messages.Add((LogLevel.Warning, $"For '{match.Value}' the blue value '{b}' is not a valid percentage or integer value"));
            }
            float alpha = 1.0f;
            if (float.TryParse(a, out alpha))
            {
                if (alpha < 0 || alpha > 1)
                {
                    messages.Add((LogLevel.Warning, $"For '{match.Value}' the alpha value '{a}' is not between 0 and 1"));
                }

            }
            return (r, g, b, alpha);
        }

        /// <summary>
        /// An empty declaration like 'xmlns:ns=""' is not valid xhtml, so we need to change it to 'xmlns:ns="http://example.edu/empty-namespace-decl"'
        /// An empty declaration like 'xmlns=""' is not valid xhtml, so we need get rid of it
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        private static void CorrectEmptyNamespaceDeclarations(HtmlDocument hdoc, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            var xmlnsNodes = hdoc.DocumentNode.SelectNodes("//*/@*[starts-with(local-name(),'xmlns')]");
            if (xmlnsNodes != null)
            {
                foreach (var xmlnsNode in xmlnsNodes)
                {
                    var xmlns = xmlnsNode.Attributes.Where(a => a.Name.StartsWith("xmlns") && string.IsNullOrWhiteSpace(a.Value));
                    if (xmlns != null)
                    {
                        List<HtmlAttribute> toBeRemoved = new();
                        foreach (var attr in xmlns)
                        {
                            var orig = $"{attr.Name}='{attr.Value}'";
                            if (attr.Name == "xmlns")
                            {
                                // get rid of the xmlns unless it defines a prefix
                                toBeRemoved.Add(attr);
                                messages.Add((LogLevel.Warning, $"Empty namespace declaration \"{orig}\" was removed."));
                            }
                            else
                            {
                                //keep the namespace, but give it a dummy value
                                attr.Value = "http://example.edu/empty-namespace-decl";
                                messages.Add((LogLevel.Warning, $"Empty namespace declaration \"{orig}\" was changed to \"{attr.Name}='{attr.Value}'\"."));
                            }
                        }
                        foreach (var attr in toBeRemoved)
                        {
                            attr.Remove();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// IF an element has a style of 'display:none' remove it it from the html altogether
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        private static void RemoveDisplayNone(HtmlDocument hdoc, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            var displayNoneNodes = hdoc.DocumentNode.QuerySelectorAll("*[style*='display:none']");

            List<HtmlNode> toRemove = new();
            foreach (var node in displayNoneNodes)
            {
                toRemove.Add(node);
            }

            toRemove.ForEach(h => h.Remove());

        }

        private static void ConvertRelativeUrlsToAbsolute(HtmlDocument hdoc, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            var baseNode = hdoc.DocumentNode.SelectSingleNode("/html/head/base");

            if (baseNode == null)
                return;

            var baseHref = baseNode.Attributes["href"];

            if (baseHref == null)
                return;

            var href = baseHref.Value;

            if (string.IsNullOrWhiteSpace(href))
                return;

            var baseUri = new Uri(href, UriKind.RelativeOrAbsolute);

            //get all the img src attributes
            var imgNodes = hdoc.DocumentNode.QuerySelectorAll("img");
            if (imgNodes != null)
            {
                foreach (var imgNode in imgNodes)
                {
                    var src = imgNode.Attributes["src"];
                    if (src != null)
                    {
                        var uriSrc = new Uri(src.Value, UriKind.RelativeOrAbsolute);
                        if (!uriSrc.IsAbsoluteUri)
                        {
                            //combine with base to create an absolute uri
                            if (Uri.TryCreate(baseUri, uriSrc, out Uri? newUri))
                            {
                                src.Value = newUri.ToString();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// look at all id attributes and make sure they are unique within the xhtml
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        private static void FixNonUniqueIdValues(HtmlDocument hdoc, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {

            var nodes = hdoc.DocumentNode.QuerySelectorAll("*[id]"); //Note: the Xpath equivalent "//*[@id]" doesn't seem to work for some reason
            Dictionary<string, int> idCnts = new();

            if (nodes != null)
            {
                foreach (var nodeWithId in nodes)
                {
                    var currId = nodeWithId.Id;
                    if (!string.IsNullOrWhiteSpace(currId))
                    {
                        var newId = MakeIdUnique(currId, idCnts);
                        if (newId != currId)
                        {
                            if (!ignoreHtmlIssues)
                                messages.Add((LogLevel.Information, $"Non-unique id value '{currId}' was changed to '{newId}'"));
                            nodeWithId.Id = newId;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// if the given id value has already been used, add a counter to make it unique 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="idCnts"></param>
        /// <returns></returns>
        private static string MakeIdUnique(string id, Dictionary<string, int> idCnts)
        {
            //TODO: This could break internal links if the id is used as a fragment in an href attribute
            //      Might want to look hrefs like '#id' and change them to '#id_1' if id is changed to id_1
            if (idCnts.TryGetValue(id, out var cnt))
            {
                //this is a duplicate
                idCnts[id] = cnt + 1;
                id = $"{id}_{cnt + 1}";
                return MakeIdUnique(id, idCnts); //recursion to ensure that a renamed id hasn't also already been used
            }
            else
            {
                idCnts[id] = cnt + 1;
                return id;
            }
        }

        /// <summary>
        /// Look for lists which are not properly nested and correct them
        /// </summary>
        /// <param name="htmlNode"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        private static void FixImproprerlyNestedLists(HtmlNode htmlNode, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            List<(HtmlNode insert, HtmlNode delete)> toBeReplaced = new();
            var nodes = htmlNode.QuerySelectorAll("ul > * , ol > *");
            if (nodes != null)
            {
                foreach (var li in nodes)
                {
                    if (li.Name != "li")
                    {
                        // this is an improperly nested list item, so wrap it in an li
                        // this works well if the element is another list, ul or ol, but if the other item is a font, span, etc., it will be wrapped in an li
                        // and the nested items will be lost
                        //FUTURE: Not sure what a good generic solution would be???  Maybe the solution belongs in the XSLT instead of here
                        var newLi = htmlNode.OwnerDocument.CreateElement("li");
                        toBeReplaced.Add((newLi, li));
                        if (!ignoreHtmlIssues)
                            messages.Add((LogLevel.Information, $"List Node '{li.ParentNode.XPath}' had improperly nested elements; these were wrapped in <li> elements"));
                    }
                }
                toBeReplaced.ForEach(x => x.delete.ParentNode.InsertBefore(x.insert, x.delete));
                toBeReplaced.ForEach(x => x.insert.AppendChild(x.delete.CloneNode(true)));
                toBeReplaced.ForEach(x => x.delete.ParentNode.RemoveChild(x.delete));
            }
        }

        /// <summary>
        /// Normalize all style attribute property values, and also remove unsupported properties
        /// </summary>
        /// <param name="htmlNode"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        /// <returns></returns>
        private static void NormalizeStyleProperties(HtmlNode htmlNode, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            var allElements = htmlNode.SelectNodes("//*");
            if (allElements != null)
            {
                var cssParser = new AngleSharp.Css.Parser.CssParser();

                //loop through all the elements, use AngleSharp.Css to parse all the style attributes, and replace the style value with a new one normalized by the AngleSharp.Css parser
                foreach (var elem in allElements)
                {
                    var style = elem.GetAttributes("style").SingleOrDefault();
                    if (style != null && !string.IsNullOrWhiteSpace(style.Value))
                    {
                        string? newStyle = null;
                        //elem.Name may be invalid causing the parser to not parse correctly, so normalize it
                        var normalizedName = XmlConvert.EncodeLocalName(elem.Name);
                        try
                        {
                            var sSheet = cssParser.ParseStyleSheet($"{normalizedName} {{{style.Value}}}");
                            newStyle = ((ICssStyleRule)sSheet.Rules.Single()).Style.CssText;
                        }
                        catch (Exception ex)
                        {
                            messages.Add((LogLevel.Critical, $"ParseStyleSheet '{normalizedName} {{{style.Value}}}'; {ex.Message}"));
                        }

                        if (!string.IsNullOrWhiteSpace(newStyle))
                        {
                            style.Value = newStyle;
                        }
                        else
                        {
                            if (!ignoreHtmlIssues)
                                messages.Add((LogLevel.Information, $"Invalid style attribute '{style.Value}' was removed from node '{elem.XPath}'"));
                            style.Remove();
                        }
                    }
                }
            }
        }



        public static string FixCharacterEntities(string htmlStr)
        {
            //TODO: probably need to be a bit selective to avoid decoding CDATA sections

            //replace named character entities with the characters

            //DeEntitize also replaces standard xml entities, so I need to double-encode them to prevent this
            htmlStr = htmlStr.Replace("&amp;", "&amp;amp;");
            htmlStr = htmlStr.Replace("&lt;", "&amp;lt;");
            htmlStr = htmlStr.Replace("&gt;", "&amp;gt;");
            htmlStr = htmlStr.Replace("&quot;", "&amp;quot;");
            htmlStr = htmlStr.Replace("&apos;", "&amp;apos;");

            htmlStr = HtmlEntity.DeEntitize(htmlStr);

            return htmlStr;
        }

        /// <summary>
        /// The HAP seems to over encode CDATA sections
        ///Correct the CDATA sections so that they are valid
        ///     i.e. <title>
        ///           //<![CDATA[
        ///           ...
        ///            //]]>//
        ///          </title >
        ///       this should just be <title><![CDATA[...]]></title>
        ///       The // comments are only useful for the <script> element
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string FixCData(string html)
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

        /// <summary>
        /// Remove any comments <!-- ... --> from the HTML
        /// They are not needed for rendering
        /// </summary>
        /// <param name="hdoc"></param>
        private static void RemoveComments(HtmlDocument hdoc)
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
        /// Return the root html element, if it is missing create it and return it
        /// The xhtml xmlns declaration is also added
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        /// <returns></returns>
        private static HtmlNode GetOrCreateHtml(HtmlDocument hdoc, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            //find, or if missing create, the root html tag
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
                if (!ignoreHtmlIssues)
                    messages.Add((LogLevel.Information, "There was no top-level html tag, so one was added"));
            }

            //remove existing xmlns attribute so a new xmlns can be added for xhtml
            htmlNode.Attributes.Remove("xmlns");
            htmlNode.Attributes.Add("xmlns", "http://www.w3.org/1999/xhtml");

            return htmlNode;
        }

        /// <summary>
        /// Return the html body element, if it is missing create it and return it
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="htmlNode"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        /// <returns></returns>
        private static HtmlNode GetOrCreateBody(HtmlDocument hdoc, HtmlNode htmlNode, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            var body = htmlNode.Element("body");
            if (body == null)
            {
                //Add a body element
                var newBody = hdoc.CreateElement("body");
                var nodes = htmlNode.ChildNodes;
                newBody.AppendChildren(nodes);
                htmlNode.RemoveAllChildren();
                htmlNode.AppendChild(newBody);
                if (!ignoreHtmlIssues)
                    messages.Add((LogLevel.Information, "There was no body tag, so one was added"));
                body = newBody;
            }

            return body;
        }

        /// <summary>
        /// Return the html head element, if it is missing create it and return it
        /// This will also make sure that the head element is the first child of the html element
        /// and that it contains all the appropriate child elements
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="htmlNode"></param>
        /// <param name="body"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        /// <returns></returns>
        private static HtmlNode GetOrCreateHead(HtmlDocument hdoc, HtmlNode htmlNode, HtmlNode body, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            //determine if there are multiple heads
            var heads = htmlNode.SelectNodes("head");
            if (heads != null && heads.Count > 1)
            {
                //need to merge them into a single head
                var mainHead = heads[0];
                for (int i = 1; i < heads.Count; i++)
                {
                    var otherHead = heads[i];
                    mainHead.AppendChildren(otherHead.ChildNodes); //move all the children of the head to the main head
                    otherHead.RemoveAllChildren();
                    otherHead.Remove();
                }
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
                if (!ignoreHtmlIssues)
                    messages.Add((LogLevel.Information, "There was no head tag, so one was added"));
                head = newHead;


            }

            return head;

        }
        /// <summary>
        /// The xhtml body element does not allow mixed content, if the are non-whitespace text nodes in the body
        /// a new div element will be added to enclose the whole body
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="body"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        private static void AddRootDivToBodyIfNeeded(HtmlDocument hdoc, HtmlNode body, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            //Add a valid root element to the body if needed, body does not allow mixed content, so it needs an element
            var breakText = body.OuterHtml;
            var bodyTextNodes = body.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Text);
            if (bodyTextNodes != null)
            {
                if (bodyTextNodes.Any(n => !XmlHelpers.IsValidXmlWhitespace(((HtmlTextNode)n).Text)))
                {
                    var newDiv = hdoc.CreateElement("div");
                    newDiv.AppendChildren(body.ChildNodes);
                    body.RemoveAllChildren();
                    body.AppendChild(newDiv);
                    if (!ignoreHtmlIssues)
                        messages.Add((LogLevel.Information, "The body element does not allow mixed content, so all content was wrapped in new div element"));
                }
            }
        }

        private static void RemoveTextFromHead(HtmlDocument hdoc, HtmlNode head, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            var bodyTextNodes = head.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Text);
            foreach (HtmlTextNode node in bodyTextNodes.Cast<HtmlTextNode>())
            {
                if (!XmlHelpers.IsValidXmlWhitespace(node.Text))
                {
                    var newText = Regex.Replace(node.Text, "[^ \t\r\n]", " ");
                    if (!ignoreHtmlIssues)
                        messages.Add((LogLevel.Information, $"The head element does not allow mixed content, so text content '{node.Text}' was converted to all spaces '{newText}'."));
                    node.Text = newText;
                }
            }
        }


        /// <summary>
        /// This will add a <meta name='generator' content='Conversion to XHTML performed by UIUCLibrary.EaPdf.Helpers' /> element to the head 
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="head"></param>
        private static void AddGeneratorMetaToHead(HtmlDocument hdoc, HtmlNode head)
        {
            //add a meta tag to indicate how the xhtml was generated
            //this also ensures at least one child in the head
            var newMeta = hdoc.CreateElement("meta");
            newMeta.Attributes.Add("name", "generator");
            newMeta.Attributes.Add("content", $"Conversion to XHTML performed by {typeof(HtmlHelpers).Namespace}");
            head.PrependChild(newMeta);
        }

        /// <summary>
        /// Any left-over or dangling nodes or non-whitespace text which are outside of the head or body will be moved into the head or body as appropriate
        /// This includes dealing with multiple body elements
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="htmlNode"></param>
        /// <param name="head"></param>
        /// <param name="body"></param>
        /// <param name="messages"></param>
        /// <param name="ignoreHtmlIssues"></param>
        private static void MoveDanglingNodesToHeadOrBody(HtmlDocument hdoc, HtmlNode htmlNode, HtmlNode head, HtmlNode body, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            //see if the htmlNode contains anything besides just a head and body
            //if it does move it to the correct place in the head or to the start or end of the body
            var kids = htmlNode.ChildNodes;

            if (kids != null)
            {
                bool beforeBody = true;

                List<HtmlNode> toBeRemoved = new();

                foreach (var kid in kids)
                {
                    var name = kid.Name;
                    if (name != "head" && name != "body" && kid.NodeType == HtmlNodeType.Element)
                    {
                        if (new string[] { "title", "base", "meta", "link", "style" }.Contains(name))
                        {
                            //Move it to the head
                            head.AppendChild(kid);
                            toBeRemoved.Add(kid);
                            if (!ignoreHtmlIssues)
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
                            if (!ignoreHtmlIssues)
                                messages.Add((LogLevel.Information, $"Moving {name} to the body"));
                        }
                    }
                    else if (name == "body" && beforeBody)
                    {
                        beforeBody = false;
                    }
                    else if (name == "body" && !beforeBody)
                    {
                        // looks like there are multiple bodies. Rename the second body to div and move it to the bottom of the previous body
                        kid.Name = "div";
                        body.AppendChild(kid);
                        toBeRemoved.Add(kid);
                        if (!ignoreHtmlIssues)
                            messages.Add((LogLevel.Information, $"The html had another body; it was renamed to div and moved to the bottom of the first body."));

                    }
                    else if (kid.NodeType == HtmlNodeType.Text && !beforeBody && !XmlHelpers.IsValidXmlWhitespace(((HtmlTextNode)kid).Text))
                    {
                        // looks like some straggling text after the body, put it into a div and append it to the body
                        var newDiv = hdoc.CreateElement("div");
                        newDiv.AppendChild(kid);
                        body.AppendChild(newDiv);
                        toBeRemoved.Add(kid);
                        if (!ignoreHtmlIssues)
                            messages.Add((LogLevel.Information, $"Moving trailing text into the body"));
                    }
                }

                foreach (var removeMe in toBeRemoved)
                    htmlNode.RemoveChild(removeMe);
            }
        }

        /// <summary>
        /// Add missing elements if needed, html, head, and/or body
        /// </summary>
        /// <param name="hdoc"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        private static HtmlNode MakeHtmlMinimallyValid(HtmlDocument hdoc, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            int msgCount = messages.Count;

            //remove comments, this will include any DOCTYPE declaration
            RemoveComments(hdoc);

            var htmlNode = GetOrCreateHtml(hdoc, ref messages, ignoreHtmlIssues);

            var body = GetOrCreateBody(hdoc, htmlNode, ref messages, ignoreHtmlIssues);

            var head = GetOrCreateHead(hdoc, htmlNode, body, ref messages, ignoreHtmlIssues);

            //remove non-whitespace text from the head
            RemoveTextFromHead(hdoc, head, ref messages, ignoreHtmlIssues);

            AddRootDivToBodyIfNeeded(hdoc, body, ref messages, ignoreHtmlIssues);

            AddGeneratorMetaToHead(hdoc, head);

            MoveDanglingNodesToHeadOrBody(hdoc, htmlNode, head, body, ref messages, ignoreHtmlIssues);

            //recursively check each element for namespace declarations and make sure the element and attribute names and prefixes are valid 
            Stack<List<string>> namespacePrefixes = new();
            FixElementAndAttributeNames(htmlNode, ref namespacePrefixes, ref messages, ignoreHtmlIssues);
            if (namespacePrefixes.Count > 0 && !ignoreHtmlIssues)
            {
                messages.Add((LogLevel.Warning, $"There are unclosed tags"));
            }


            if (!ignoreHtmlIssues && messages.Count > msgCount)
                messages.Insert(msgCount, (LogLevel.Warning, "Html was not valid; see the following info messages for details "));

            return htmlNode;
        }

        /// <summary>
        /// Check that namespace prefixes have been previously declared using xmlns, if not escape the name to make it valid 
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="namespacePrefixes"></param>
        private static void FixElementAndAttributeNames(HtmlNode elem, ref Stack<List<string>> namespacePrefixes, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
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
                    FixElementAndAttributeNames(child, ref namespacePrefixes, ref messages, ignoreHtmlIssues);
                }
            }

            namespacePrefixes.Pop();
        }

        private static void ConvertToInlineCssUsingAngleSharpCss(HtmlNode htmlNode, ref List<(LogLevel level, string message)> messages, bool ignoreHtmlIssues)
        {
            //get all the style elements
            var styles = htmlNode.SelectNodes("//style");

            string allStyles = "";
            if (styles != null)
            {
                //merge into one CSS string
                foreach (var style in styles)
                {
                    allStyles += style.InnerText + " ;\r\n/* NEXT STYLE */\r\n";
                }

                var cssParser = new CssParser(); //AngleSharp.Css parser
                var sSheetTask = cssParser.ParseStyleSheet(allStyles);

                foreach (ICssStyleRule rule in sSheetTask.Rules.Where(r => r is ICssStyleRule sr && sr.Selector != null).OrderByDescending(sr => ((ICssStyleRule)sr).Selector.Specificity).Cast<ICssStyleRule>())
                {
                    //FUTURE: Add support for ListSelectors, so that the separate specificity of its selectors can be determined.
                    //FUTURE: Add support for the shorthand property "all"
                    //FUTURE: Add support for the !important modifier.

                    var selectorText = rule.Selector.Text;
                    var styleText = rule.Style.CssText;
                    var specificity = rule.Selector.Specificity;

                    if (string.IsNullOrWhiteSpace(styleText))
                        continue; //skip this rule and move to the next

                    IEnumerable<HtmlNode>? nodes = null;
                    try
                    {
                        nodes = htmlNode.QuerySelectorAll(selectorText);
                    }
                    catch (Exception ex)
                    {
                        messages.Add((LogLevel.Debug, $"Parsing selector '{selectorText}'; {ex.Message}"));
                        nodes = null;
                    }

                    if (nodes != null)
                    {
                        foreach (var node in nodes)
                        {
                            var styleAttr = node.GetAttributes("style").SingleOrDefault();
                            if (styleAttr == null)
                            {
                                var newStyle = ((ICssStyleRule)cssParser.ParseStyleSheet($"dummy {{{styleText}}}").Rules.Single()).Style;
                                var newStyleText = string.Join("; ", newStyle.Select(p => p.Name + ":" + p.Value)) + "; ";

                                node.Attributes.Add("style", newStyleText);
                                messages.Add((LogLevel.Debug, $"Node: {node.XPath}, Inlining style:  {selectorText} {{{styleText}}}, new style attribute"));
                            }
                            else
                            {
                                var currentStyle = ((ICssStyleRule)cssParser.ParseStyleSheet($"dummy {{{styleAttr.Value}}}").Rules.Single()).Style;

                                var currentStyleText = string.Join("; ", currentStyle.Select(p => p.Name + ":" + p.Value)) + "; ";

                                foreach (var newProperty in rule.Style)
                                {
                                    //since we are iterating rules from most specific to the least specific, we can't overwrite an existing property because it originated from a more specific rule
                                    if (!currentStyle.Contains(newProperty, new AngleSharpCssPropertyComparer()))
                                    {
                                        currentStyleText += newProperty.Name + ":" + newProperty.Value + "; ";
                                        messages.Add((LogLevel.Debug, $"Node: {node.XPath}, Inlining style: {selectorText} {{{styleText}}}, modify style attribute"));
                                    }
                                }

                                styleAttr.Value = currentStyleText.Trim().Trim(';').Trim(); //get rid of any leading or trailing semi-colons and whitespace

                            }
                        }
                    }
                }
            }
        }
    }

    class AngleSharpCssPropertyComparer : IEqualityComparer<ICssProperty>
    {
        public bool Equals(ICssProperty? x, ICssProperty? y)
        {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (x is null || y is null)
                return false;

            return x.Name == y.Name;
        }

        public int GetHashCode([DisallowNull] ICssProperty obj)
        {
            //Check whether the object is null
            if (obj is null) return 0;


            //Calculate the hash code for the product.
            return obj.Name.GetHashCode();
        }
    }

}
