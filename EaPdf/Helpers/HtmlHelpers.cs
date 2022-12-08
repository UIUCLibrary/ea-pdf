using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace UIUCLibrary.EaPdf.Helpers
{
    internal class HtmlHelpers
    {
        /// <summary>
        /// Use the HTML Agility Pack to parse the HTML and return it as valid XHTML
        /// </summary>
        /// <param name="html"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static string ConvertHtmltoXhtml(string html, out string msg)
        {
            string ret = html;

            if (string.IsNullOrWhiteSpace(html))
            {
                msg = "The html content is empty";
                return ret;
            }

            var hdoc = new HtmlDocument();
            hdoc.OptionFixNestedTags = true;
            hdoc.OptionOutputAsXml = true;
            hdoc.OptionReadEncoding = false;
            hdoc.OptionXmlForceOriginalComment = false;
            hdoc.BackwardCompatibility = false;
            hdoc.LoadHtml(html);


            //find the root html tag
            var htmlNode = hdoc.DocumentNode.Descendants().FirstOrDefault(x => x.Name == "html");

            if (htmlNode == null)
            {
                ret = hdoc.DocumentNode.InnerHtml;
                msg = "No html node was found";
            }
            else
            {
                if (htmlNode.Attributes.Contains("xmlns"))
                {
                    //remove existing xmlns attribute so a new xmlns can be added for xhtml
                    htmlNode.Attributes.Remove("xmlns");
                }
                htmlNode.Attributes.Add("xmlns", "http://www.w3.org/1999/xhtml");
                ret = htmlNode.OuterHtml;
                msg = "";
            }

            //replace named character entities with the characters

            //DeEntitize also replaces standard xml entities, so I need to double-encode them to prevent this
            ret = ret.Replace("&amp;", "&amp;amp;");
            ret = ret.Replace("&lt;", "&amp;lt;");
            ret = ret.Replace("&gt;", "&amp;gt;");
            ret = ret.Replace("&quot;", "&amp;quot;");
            ret = ret.Replace("&apos;", "&amp;apos;");
            ret = HtmlEntity.DeEntitize(ret);

            return ret;
        }



    }
}
