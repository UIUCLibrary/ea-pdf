using Aron.Weiler;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Logging;
using System.Text;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public class ITextSharpPdfEnhancer : IPdfEnhancer
    {
        private bool disposedValue;

        private readonly PdfReader _reader;
        private readonly Stream _out;
        private readonly PdfStamper _stamper;
        private readonly ILogger _logger;

        /// <summary>
        /// Dictionary where either the indirect refererence or the page number can be used to get the page data
        /// </summary>
        private readonly MultiKeyDictionary<PdfIndirectReference, int, PageData> _pages = new(new ITextSharpIndirectReferenceEqualityComparer());

        public ITextSharpPdfEnhancer(ILogger logger, string inPdfFilePath, string outPdfFilePath)
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

        public void NormalizeAttachments(List<EmbeddedFile> embeddedFiles)
        {
            var catalog = _reader.Catalog;
        }

        /// <summary>
        /// Add Xmp metadata to a DPart range of pages for each message
        /// </summary>
        /// <param name="dparts">The root DPart node containing all the metadata for the folders and messages in the file</param>
        /// <exception cref="Exception"></exception>
        public void AddXmpToDParts(DPartInternalNode dparts)
        {
            //get reference to new DPartRoot object
            var dpartRootIndRef = _stamper.Writer.PdfIndirectReference;

            //create the DPart hierarchy
            var dpartRootNode = AddDPartNode(dparts, dpartRootIndRef);

            //create the DPartRoot object pointing to the root of the dpart hierarchy
            var newDPartRootDict = new PdfDictionary();
            newDPartRootDict.Put(new PdfName("Type"), new PdfName("DPartRoot"));
            newDPartRootDict.Put(new PdfName("DPartRootNode"), dpartRootNode);
            var newDPartRootIndObj = _stamper.Writer.AddToBody(newDPartRootDict, dpartRootIndRef) ?? throw new Exception("New DPartRoot object was not created");

            if (newDPartRootIndObj.IndirectReference.Number != dpartRootIndRef.Number || newDPartRootIndObj.IndirectReference.Generation != dpartRootIndRef.Generation)
                throw new Exception("New indirect reference unexpectedly assigned");

            //add DPartRoot to catalog
            //Note: _stamper.Writer.ExtraCatalog will not work for this
            _reader.Catalog.Put(new PdfName("DPartRoot"), dpartRootIndRef);
        }

        /// <summary>
        /// Recursively create the DPart hierarchy
        /// </summary>
        /// <param name="dpartNode"></param>
        /// <param name="parentIndRef">Indirect reference to parent DPart node</param>
        /// <param name="metaIndRef">Indirect reference to XMP metadata to associate with DPart node</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        private PdfIndirectReference AddDPartNode(DPartNode dpartNode, PdfIndirectReference parentIndRef)
        {
            if (dpartNode == null)
                throw new ArgumentNullException(nameof(dpartNode));

            var newIndRef = _stamper.Writer.PdfIndirectReference; //get reference to new DPart object

            var newDPartDict = new PdfDictionary();
            newDPartDict.Put(new PdfName("Type"), new PdfName("DPart"));
            newDPartDict.Put(new PdfName("Parent"), parentIndRef);

            if (!string.IsNullOrWhiteSpace(dpartNode.DpmXmpString))
            {
                byte[] metaBytes = Encoding.UTF8.GetBytes(dpartNode.DpmXmpString);
                var newStrm = new PdfStream(metaBytes);
                newStrm.Put(new PdfName("Type"), new PdfName("Metadata"));
                newStrm.Put(new PdfName("Subtype"), new PdfName("XML"));
                PdfIndirectObject metaIndObj = _stamper.Writer.AddToBody(newStrm);
                PdfDictionary metaDict = new PdfDictionary();
                metaDict.Put(new PdfName("Metadata"), metaIndObj.IndirectReference);
                newDPartDict.Put(new PdfName("DPM"), metaDict);
                //Maybe use this instead: newDPartDict.Put(new PdfName("Metadata"), metaIndObj.IndirectReference); 
                if(dpartNode.Parent == null)
                {
                    //this is the root DPart node, so replace the catalog metadata with this
                    _reader.Catalog.Remove(PdfName.Metadata);
                    _reader.Catalog.Put(PdfName.Metadata, metaIndObj.IndirectReference);
                }
            }

            if (dpartNode is DPartInternalNode dpartInternal)
            {
                PdfArray dpartsArr = new();
                foreach (var child in dpartInternal.DParts)
                {
                    var childIndRef = AddDPartNode(child, newIndRef);
                    dpartsArr.Add(childIndRef);
                }

                //QUESTION: DPart seems to require that the DParts array is inside another array, what else can be inside the outer array?
                var outerArr = new PdfArray();
                outerArr.Add(dpartsArr);
                newDPartDict.Put(new PdfName("DParts"), outerArr);
            }
            else if (dpartNode is DPartLeafNode dpartLeafNode)
            {
                var pageStart = GetPageDataForNamedDestination(dpartLeafNode.StartNamedDestination);
                var pageEnd = GetPageDataForNamedDestination(dpartLeafNode.EndNamedDestination);

                if (pageStart != null && pageEnd != null)
                {
                    newDPartDict.Put(new PdfName("Start"), pageStart.PageReference);
                    if (pageStart.PageReference != pageEnd.PageReference)
                    {
                        newDPartDict.Put(new PdfName("End"), pageEnd.PageReference);
                    }

                    //add dpart indirect reference to pages dictionary, so there is a two-way linkage
                    for (int i = pageStart.PageNumber; i <= pageEnd.PageNumber; i++)
                    {
                        if (_pages[i] != null)
                            _pages[i]?.PageDictionary.Put(new PdfName("DPart"), newIndRef);
                        else
                            throw new Exception($"Page number {i} not found");
                    }

                }
                else
                {
                    throw new Exception($"Start or end page for message (Named Destinations: {dpartLeafNode.StartNamedDestination}, {dpartLeafNode.EndNamedDestination}) were not found.");
                }
            }
            else
            {
                throw new Exception($"Unexpected PdfDpartNode subclass '{dpartNode.GetType().Name}'");
            }

            PdfIndirectObject leafIndObj = _stamper.Writer.AddToBody(newDPartDict, newIndRef) ?? throw new Exception("New DPartRoot object was not created");

            if (leafIndObj.IndirectReference.Number != newIndRef.Number || leafIndObj.IndirectReference.Generation != newIndRef.Generation)
                throw new Exception("New indirect reference unexpectedly assigned");

            return leafIndObj.IndirectReference;
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
                    _reader.RemoveUnusedObjects(); //this gets rid of orphaned XMP metadata objects, maybe among others

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
