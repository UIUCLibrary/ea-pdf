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

        /// <summary>
        /// ITextSharp PdfStream Length and RawLength properties don't seem to be reliable, so need to get the actual bytes and count them.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int GetStreamActualSize(PdfStream stream)
        {
            if (stream is not PrStream prs)
            {
                throw new Exception("GetStreamActualSize: Not a PrStream");
            }
            else
            {
                return PdfReader.GetStreamBytes(prs).Length;
            }
        }

        /// <summary>
        /// Because compressed zero-length streams have a non-zero Length value in the stream dictionary, due to headers and padding, 
        /// we need to check the actual size of the stream when the Length value is less than or equal to 8, but greater than zero. Zero will
        /// always be zero for an uncompressed stream, and a compressed stream will have a Length value greater than zero, but less than or equal to 8.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool IsZeroLengthStream(PdfStream stream)
        {
            var size = stream.GetAsNumber(PdfName.LENGTH)?.IntValue ?? throw new Exception("IsZeroLengthStream: Stream dictionary is missing a Length key");

            if (size > 0 && size <= 8) //might be a zero length compressed file
            {
                size = ITextSharpHelpers.GetStreamActualSize(stream);
            }

            return (size == 0);
        }
    }
}
