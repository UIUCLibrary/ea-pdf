using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class iTextSharpIndirectReferenceEqualityComparer : IEqualityComparer<PdfIndirectReference>
    {
        public bool Equals(PdfIndirectReference? x, PdfIndirectReference? y)
        {
            if (x == null || y == null)
                return false;

            return x.Number == y.Number && x.Generation == y.Generation;
        }

        public int GetHashCode(PdfIndirectReference obj)
        {
            return obj.Number; // obj.Generation is almost always zero, so ignoring it
        }
    }
}
