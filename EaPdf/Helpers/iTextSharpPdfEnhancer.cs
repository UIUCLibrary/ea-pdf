using Fizzler;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class iTextSharpPdfEnhancer : IPdfEnhancer
    {
        private bool disposedValue;

        private readonly PdfReader _reader;
        private readonly PdfStamper _stamper;
        private readonly ILogger _logger;

        public iTextSharpPdfEnhancer(ILogger logger, string inPdfFilePath, string outPdfFilePath)
        {
            _logger = logger;
            _reader = new PdfReader(inPdfFilePath);
            _stamper = new PdfStamper(_reader, new FileStream(outPdfFilePath, FileMode.Create), PdfWriter.VERSION_1_7);
        }

        //https://stackoverflow.com/questions/28427100/how-do-i-add-xmp-metadata-to-each-page-of-an-existing-pdf-using-itextsharp
        public void AddXmpToPages(Dictionary<string, string> pageXmps)
        {
            foreach (var pageMeta in pageXmps)
            {
                PdfDictionary? page = GetPageDictWithNamedDestination(pageMeta.Key);
                byte[] meta = Encoding.Default.GetBytes(pageMeta.Value);

                if (page != null)
                {
                    PrStream stream = (PrStream)page.GetAsStream(PdfName.Metadata);
                    if (stream == null)
                    {
                        // We add the XMP bytes to the writer
                        PdfIndirectObject ind = _stamper.Writer.AddToBody(new PdfStream(meta));
                        // We add a reference to the XMP bytes to the page dictionary
                        page.Put(PdfName.Metadata, ind.IndirectReference);
                    }
                    else
                    {
                        _logger.LogWarning("Page for message (LocalId: {LocalId}) already had metadata; it was replaced.", pageMeta.Key);
                        byte[] xmpBytes = PdfReader.GetStreamBytes(stream);
                        stream.SetData(meta);
                    }

                }
                else
                {
                    throw new Exception($"Page for message (LocalId: {pageMeta.Key}) was not found.");
                }
            }
        }

        public void SetDocumentXmp(string xmp)
        {
            var xmpByt = System.Text.Encoding.Default.GetBytes(xmp);
            _stamper.XmpMetadata = xmpByt;
        }

        /// <summary>
        /// Return the page containing the named destination
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private PdfDictionary? GetPageDictWithNamedDestination(string name)
        {

            var d1 = _reader.GetNamedDestination(true);
            if (d1.ContainsKey(name))
            {
                PdfArray pageDest = (PdfArray)d1[name] as PdfArray;
                PdfDictionary page = (PdfDictionary)pageDest.GetDirectObject(0);
                return page;
            }
            else
                return null;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _stamper.Close();
                    _reader.Close();    
                    _stamper.Dispose();
                    _reader.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
