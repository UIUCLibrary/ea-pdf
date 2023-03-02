using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Fizzler;
using iTextSharp.text.pdf;
using iTextSharp.text.xml.xmp;
using Microsoft.Extensions.Logging;
using PdfTemplating.XslFO.ApacheFOP.Serverless;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.EaPdf
{
    public class EaxsToEaPdfProcessor
    {

        private readonly ILogger _logger;
        private readonly IXsltTransformer _xslt;
        private readonly IXslFoTransformer _xslfo;

        public EaxsToEaPdfProcessorSettings Settings { get; }

        /// <summary>
        /// Create a processor for converting email xml archive to an EA-PDF file, initializing the logger, converters, and settings
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public EaxsToEaPdfProcessor(ILogger<EaxsToEaPdfProcessor> logger, IXsltTransformer xslt, IXslFoTransformer xslfo, EaxsToEaPdfProcessorSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _xslt = xslt ?? throw new ArgumentNullException(nameof(xslt));
            _xslfo = xslfo ?? throw new ArgumentNullException(nameof(xslfo));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("MboxProcessor Created");

        }

        public void ConvertEaxsToPdf(string eaxsFilePath)
        {
            var foFilePath = Path.ChangeExtension(eaxsFilePath, ".fo");
            var pdfFilePath = Path.ChangeExtension(eaxsFilePath, ".pdf");

            var xsltParams = new Dictionary<string, object>
            {
                { "fo-processor-version", _xslfo.ProcessorVersion }
            };

            List<(LogLevel level, string message)> messages = new();
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltFilePath, foFilePath, xsltParams, ref messages);
            foreach (var m in messages)
            {
                _logger.Log(m.level, m.message);
            }

            if (status == 0)
            {
                messages.Clear();
                var status2 = _xslfo.Transform(foFilePath, pdfFilePath, ref messages);
                foreach (var m in messages)
                {
                    _logger.Log(m.level, m.message);
                }
                if (status2 != 0)
                {
                    throw new Exception("XSLT transformation failed; review log details.");
                }
            }
            else
            {
                throw new Exception("FO transformation to PDF failed; review log details.");
            }

            //Delete the intermediate FO file
            File.Delete(foFilePath);

            //Do some post processing to add metadata
            AddXmpToPage(pdfFilePath);
        }


        //https://stackoverflow.com/questions/28427100/how-do-i-add-xmp-metadata-to-each-page-of-an-existing-pdf-using-itextsharp
        private void AddXmpToPage(string pdfFilePath)
        {
            var outFilePath = Path.ChangeExtension(pdfFilePath, "out.pdf");

            using PdfReader reader = new PdfReader(pdfFilePath);
            using PdfStamper stamper = new PdfStamper(reader, new FileStream(outFilePath, FileMode.Create),PdfWriter.VERSION_1_7);

            PdfDictionary? page = GetPageDictWithNamedDestination(reader, "MESSAGE_4");

            if (page != null)
            {
                PrStream stream = (PrStream)page.GetAsStream(PdfName.Metadata);
                if(stream == null)
                {
                    // We create some XMP bytes
                    MemoryStream memStrm = new MemoryStream();
                    XmpWriter xmp = new XmpWriter(memStrm, new PdfDictionary(), PdfWriter.PDFA1B);
                    //TODO: Add the metadata
                    xmp.Close();

                    byte[] bts = memStrm.ToArray();
                    string s = System.Text.Encoding.Default.GetString(bts);


                    // We add the XMP bytes to the writer
                    PdfIndirectObject ind = stamper.Writer.AddToBody(new PdfStream(memStrm.ToArray()));
                    // We add a reference to the XMP bytes to the page dictionary
                    page.Put(PdfName.Metadata, ind.IndirectReference);
                }
                else
                {
                    byte[] xmpBytes = PdfReader.GetStreamBytes(stream);
                    //TODO: Modify the XMP metadata
                    stream.SetData(xmpBytes);
                }

                stamper.Close();
                reader.Close();
            }
            else
            {
                throw new Exception("");
            }
        }

        /// <summary>
        /// Return the page containing the named destination
        /// Assume PDF version 1.2 or later
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private PdfDictionary? GetPageDictWithNamedDestination(PdfReader reader, string name)
        {

            var d1 = reader.GetNamedDestination(true);
            if (d1.ContainsKey(name))
            {
                PdfArray pageDest = (PdfArray)d1[name] as PdfArray;
                PdfDictionary page = (PdfDictionary)pageDest.GetDirectObject(0);
                return page;
            }
            else
                return null;
        }

        /// <summary>
        /// Return a dictionary of XMP metadata for all the messages in the EAXS file.
        /// The key is the message LocalId and the value is the XMP metadata for that message.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string,string> GetXmpMetadataForMessage(string eaxsFilePath)
        {
            Dictionary<string, string> ret = new();



            return ret;
        }

    }
}
