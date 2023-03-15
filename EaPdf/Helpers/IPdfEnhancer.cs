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
        /// Add different XMP metadata to specific pages of a PDF file
        /// </summary>
        /// <param name="pdfFilePath">the file path to the PDF to enhance</param>
        /// <param name="pageXmps">Dictionary where the key is a tuple of named destinations for the start page and end part of the DPart (the end page is not used), and the value is the XMP string to associate with that page</param>
        public void AddXmpToPages(Dictionary<(string start, string end), string> pageXmps);

        /// <summary>
        /// Add different XMP metadata to specific Document Parts (DParts) of a PDF file
        /// </summary>
        /// <param name="pageXmps">Dictionary where the key is a tuple of named destinations for the start page and end part of the DPart, and the value is the XMP string to associate with that DPart</param>
        public void AddXmpToDParts(Dictionary<(string start, string end), string> pageXmps);

        /// <summary>
        /// Set the XMP metadata for the entire document
        /// </summary>
        /// <param name="xmp">XMP string for the document</param>
        public void SetDocumentXmp(string xmp);

    }
}
