using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers
{
    public interface IPdfEnhancer : IDisposable
    {
        /// <summary>
        /// Add XMP metadata to specific pages of a PDF file
        /// </summary>
        /// <param name="pdfFilePath">the file path to the PDF to enhance</param>
        /// <param name="pageXmps">Dictionary where the key is a named destination on a page, and the value is the XMP string to associate with that page</param>
        public void AddXmpToPages(Dictionary<string, string> pageXmps);

        /// <summary>
        /// Set the XMP metadata for the entire document
        /// </summary>
        /// <param name="xmp">XMP string for the document</param>
        public void SetDocumentXmp(string xmp);

    }
}
