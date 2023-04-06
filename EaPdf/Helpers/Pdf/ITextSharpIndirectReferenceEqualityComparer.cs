using iTextSharp.text.pdf;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public class ITextSharpIndirectReferenceEqualityComparer : IEqualityComparer<PdfIndirectReference>
    {
        public bool Equals(PdfIndirectReference? x, PdfIndirectReference? y)
        {
            if (x == null || y == null)
                return false;

            return x.Number == y.Number && x.Generation == y.Generation;
        }

        public int GetHashCode(PdfIndirectReference obj)
        {
            return HashCode.Combine(obj.Number, obj.Generation);
        }
    }
}
