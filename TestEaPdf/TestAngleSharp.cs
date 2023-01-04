using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using AngleSharp.Xhtml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestAngleSharp
    {

        [TestMethod]
        public void TestAngleSharpCustomElementInHead()
        {
            var html = @"<!DOCTYPE html>
<html>
    <head>
        <custom-tag test='123' ></custom-tag>
    </head>
    <body>
        <h1>test</h1>
    </body>
</html>";


            var parser = new HtmlParser(new HtmlParserOptions() { IsAcceptingCustomElementsEverywhere = true });
            var doc = parser.ParseDocument(html);
            var ret = doc.DocumentElement.ToHtml(new HtmlMarkupFormatter());

            Debug.WriteLine("*** Source HTML ***");
            Debug.WriteLine(html);

            Debug.WriteLine("*** After ParseDocument and ToHtml() ***");
            Debug.WriteLine(ret);

        }

    }
}
