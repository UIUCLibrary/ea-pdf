using Microsoft.VisualStudio.TestTools.UnitTesting;
using NDepend.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIUCLibrary.EaPdf;
using iTextSharp.text.pdf;
using System.IO;
using ExCSS;
using System.Threading;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestExCSS
    {

        [TestMethod]
        public void TestParse()
        {


            var parser = new StylesheetParser();


            var badCSS = @"
                h3 {color: yellow;
                @media print {
                    h3 {color: black; }
                    }
                ";

            Stylesheet? stylesheet = null;
            var stylesheetTask = Task.Run<Stylesheet>(() => parser.Parse(badCSS));
            var done = stylesheetTask.Wait(700);
            if (done)
                stylesheet = stylesheetTask.Result;
            else
                //cleanup

            Assert.IsNull(stylesheet);

            var goodCSS = @"
                h3 {color: yellow; }
                @media print {
                    h3 {color: black; }
                    }
                ";

            Stylesheet? stylesheet2 = null;
            var stylesheet2Task = Task.Run<Stylesheet>(() => parser.Parse(goodCSS));
            var done2 = stylesheet2Task.Wait(700);
            if (done2)
                stylesheet2 = stylesheet2Task.Result;
            else
                //cleaup???

                Assert.IsNotNull(stylesheet2);
        }


    }
}
