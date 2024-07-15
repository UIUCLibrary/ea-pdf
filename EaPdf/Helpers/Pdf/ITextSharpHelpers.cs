using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public static class ITextSharpHelpers
    {

        public static bool EqualsIndRef(this PdfIndirectReference indRef1, PdfIndirectReference indRef2)
        {
            if (indRef1 == null || indRef2 == null)
            {
                return false;
            }
            else
            {
                return indRef1.Number == indRef2.Number && indRef1.Generation == indRef2.Generation;
            }
        }


    }
}
