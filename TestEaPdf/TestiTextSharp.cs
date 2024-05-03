using iTextSharp.text.pdf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestiTextSharp
    {

        [TestMethod]
        public void TestExtraCatalog()
        {
            string sourcePdf = "D:\\EmailsForTesting\\SampleFiles\\Testing\\PDFs\\in.pdf";
            string destPdf   = "D:\\EmailsForTesting\\SampleFiles\\Testing\\PDFs\\out.pdf";

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

        [TestMethod]
        public void TestGetSetDocumentInfo()
        {
            string sourcePdf = "D:\\EmailsForTesting\\SampleFiles\\Testing\\PDFs\\in.pdf";
            string destPdf = "D:\\EmailsForTesting\\SampleFiles\\Testing\\PDFs\\out.pdf";

            var destStrm = new FileStream(destPdf, FileMode.Create);
            var reader = new PdfReader(sourcePdf);
            var stamper = new PdfStamper(reader, destStrm, PdfWriter.VERSION_1_7);

            var info = reader.Info;

            var newKey = "TestInfo";
            var newVal = "Test Info 123";

            info.Add(newKey, newVal);

            stamper.MoreInfo = info;

            stamper.Close();
            destStrm.Close();
            reader.Close();

            var reader2 = new PdfReader(destPdf);
            var info2 = reader2.Info;

            Assert.IsTrue(info2.ContainsKey(newKey));
            Assert.AreEqual(newVal, info2[newKey]);

        }


    }
}
