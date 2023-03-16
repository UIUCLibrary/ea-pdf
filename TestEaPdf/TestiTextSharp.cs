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

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestiTextSharp
    {

        [TestMethod]
        public void TestExtraCatalog()
        {
            string sourcePdf = "C:\\Users\\thabi\\Source\\UIUC\\ea-pdf\\SampleFiles\\Testing\\MozillaThunderbird\\short-test\\in.pdf";
            string destPdf   = "C:\\Users\\thabi\\Source\\UIUC\\ea-pdf\\SampleFiles\\Testing\\MozillaThunderbird\\short-test\\out.pdf";

            var destStrm = new FileStream(destPdf, FileMode.Create);
            var reader = new PdfReader(sourcePdf);
            var stamper = new PdfStamper(reader, destStrm , PdfWriter.VERSION_1_7);

            var newKey = new PdfName("TestExtraCatalog");
            var newStr = new PdfString("Test Extra Catalog");
            reader.Catalog.Put(newKey,newStr);


            stamper.Close();
            destStrm.Close();
            reader.Close();

            var reader2 = new PdfReader(destPdf);

            Assert.IsTrue(reader2.Catalog.Contains(newKey));
            Assert.AreEqual(newStr.ToString(), reader2.Catalog.GetAsString(newKey).ToString());

        }


    }
}
