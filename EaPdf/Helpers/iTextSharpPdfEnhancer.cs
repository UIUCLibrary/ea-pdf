using Aron.Weiler;
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
        private readonly Stream _out;
        private readonly PdfStamper _stamper;
        private readonly ILogger _logger;

        /// <summary>
        /// Dictionary where either the indirect refererence or the page number can be used to get the page data
        /// </summary>
        private readonly MultiKeyDictionary<PdfIndirectReference, int, PageData> _pages = new(new iTextSharpIndirectReferenceEqualityComparer());

        public iTextSharpPdfEnhancer(ILogger logger, string inPdfFilePath, string outPdfFilePath)
        {
            _logger = logger;
            _reader = new PdfReader(inPdfFilePath);
            _out = new FileStream(outPdfFilePath, FileMode.Create);
            _stamper = new PdfStamper(_reader, _out, PdfWriter.VERSION_1_7);


            //populate the page dictionary
            for (int i = 1; i <= _reader.NumberOfPages; i++)
            {
                var indRef = _reader.GetPageOrigRef(i);
                _pages.Add(indRef, i, new PageData(indRef, i, _reader.GetPageN(i)));
            }
        }

        /// <summary>
        /// Set the Xmp metadata for a single page
        /// <see cref="https://stackoverflow.com/questions/28427100/how-do-i-add-xmp-metadata-to-each-page-of-an-existing-pdf-using-itextsharp"/>
        /// </summary>
        /// <param name="pageXmps">Dictionary with the named destinations for pages and the corresponding Xmp to set for that page</param>
        /// <exception cref="Exception"></exception>
        public void AddXmpToPages(Dictionary<(string start, string end), string> pageXmps)
        {
            foreach (var pageMeta in pageXmps)
            {
                var page = GetPageDataForNamedDestination(pageMeta.Key.start);
                byte[] meta = Encoding.Default.GetBytes(pageMeta.Value);

                if (page != null)
                {
                    PrStream stream = (PrStream)(page.PageDictionary).GetAsStream(PdfName.Metadata);
                    if (stream == null)
                    {
                        // We add the XMP bytes to the writer
                        PdfIndirectObject ind = _stamper.Writer.AddToBody(new PdfStream(meta));
                        // We add a reference to the XMP bytes to the page dictionary
                        page.PageDictionary.Put(PdfName.Metadata, ind.IndirectReference);
                    }
                    else
                    {
                        _logger.LogWarning("Page for message (Named Destination: {NamedDestination}) already had metadata; it was replaced.", pageMeta.Key);
                        byte[] xmpBytes = PdfReader.GetStreamBytes(stream);
                        stream.SetData(meta);
                    }

                }
                else
                {
                    throw new Exception($"Page for message (Named Destination: {pageMeta.Key}) was not found.");
                }
            }
        }

        /// <summary>
        /// Add Xmp metadata to a range of pages represented as a DPart
        /// </summary>
        /// <param name="pageXmps">Dictionary with the named destinations for start and end pages and the corresponding Xmp to set for that page range</param>
        /// <exception cref="Exception"></exception>
        public void AddXmpToDParts(Dictionary<(string start, string end), string> pageXmps)
        {
            var dpartLeafs = new PdfArray(); //collect all the DPart leaf nodes

            //create the indirect references for the new objects
            var dpartRootIndRef=_stamper.Writer.PdfIndirectReference;
            var dpartRootNodeIndRef = _stamper.Writer.PdfIndirectReference;

            //create dpartroot
            var dpartRoot = new PdfDictionary();
            dpartRoot.Put(new PdfName("Type"), new PdfName("DPartRoot"));
            dpartRoot.Put(new PdfName("DPartRootNode"), dpartRootNodeIndRef);

            //create the root node
            var dpartRootNode = new PdfDictionary();
            dpartRootNode.Put(new PdfName("Type"), new PdfName("DPart"));
            dpartRootNode.Put(new PdfName("Parent"), dpartRootIndRef);

            //add DPartRoot to the catalog
            //Note: _stamper.Writer.ExtraCatalog will not work for this
            _reader.Catalog.Put(new PdfName("DPartRoot"), dpartRootIndRef);

            foreach (var pageMeta in pageXmps)
            {
                var pageStart = GetPageDataForNamedDestination(pageMeta.Key.start);
                var pageEnd = GetPageDataForNamedDestination(pageMeta.Key.end);
                byte[] meta = Encoding.Default.GetBytes(pageMeta.Value);

                if (pageStart != null && pageEnd != null)
                {
                    // Add the XMP bytes to the PDF as a stream
                    PdfIndirectObject metaInd = _stamper.Writer.AddToBody(new PdfStream(meta));

                    // Add a DPart leaf node that references the start and end pages and the XMP metadata
                    var dpartLeaf = new PdfDictionary();
                    dpartLeaf.Put(new PdfName("Type"), new PdfName("DPart"));
                    dpartLeaf.Put(new PdfName("Parent"), dpartRootNodeIndRef);
                    dpartLeaf.Put(new PdfName("Start"), pageStart.PageReference);
                    if (pageStart.PageReference != pageEnd.PageReference)
                    {
                        dpartLeaf.Put(new PdfName("End"), pageEnd.PageReference);
                    }
                    dpartLeaf.Put(new PdfName("DPM"), metaInd.IndirectReference);

                    PdfIndirectObject leafInd = _stamper.Writer.AddToBody(dpartLeaf);

                    //add dpart indirect reference to pages dictionary, so there is a two-way linkage
                    for(int i = pageStart.PageNumber; i <= pageEnd.PageNumber; i++)
                    {
                        if (_pages[i] != null)
                            _pages[i]?.PageDictionary.Put(new PdfName("DPart"), leafInd.IndirectReference);
                        else
                            throw new Exception($"Page number {i} not found");
                    }
                    dpartLeafs.Add(leafInd.IndirectReference);
                }
                else
                {
                    throw new Exception($"Start or end page for message (Named Destinations: {pageMeta.Key.start}, {pageMeta.Key.end}) were not found.");
                }
            }

            //QUESTION: for some reason in the examples, Dparts is an array of an array
            var outerArr = new PdfArray();
            outerArr.Add(dpartLeafs);
            dpartRootNode.Put(new PdfName("DParts"), outerArr);

            //add the new objects
            var ref1 = _stamper.Writer.AddToBody(dpartRoot, dpartRootIndRef);
            var ref2 = _stamper.Writer.AddToBody(dpartRootNode, dpartRootNodeIndRef);
        }

        /// <summary>
        /// Set the document level XMP metadata to the given string
        /// </summary>
        /// <param name="xmp"></param>
        public void SetDocumentXmp(string xmp)
        {
            var xmpByt = System.Text.Encoding.Default.GetBytes(xmp);
            _stamper.XmpMetadata = xmpByt;
        }

        /// <summary>
        /// Return the page dictionary, page number, and page indirect reference containing the named destination
        /// </summary>
        /// <param name="name">the name of the destination</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private PageData? GetPageDataForNamedDestination(string name)
        {

            var d1 = _reader.GetNamedDestination(true);
            if (d1.ContainsKey(name))
            {
                PdfArray pageDest = (PdfArray)d1[name] as PdfArray;
                PdfIndirectReference pageRef = pageDest.GetAsIndirectObject(0);
                return _pages[pageRef];
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

                    _out.Close();
                    _out.Dispose();

                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Data about a page, including its number, indirect reference, and page dictionary
        /// </summary>
        private class PageData
        {
            public PageData(PdfIndirectReference indRef, int num, PdfDictionary dict)
            {
                PageReference = indRef;
                PageNumber = num;
                PageDictionary = dict;
            }

            public PdfIndirectReference PageReference { get; }

            public int PageNumber { get; }

            public PdfDictionary PageDictionary { get; }
        }
    }
}
