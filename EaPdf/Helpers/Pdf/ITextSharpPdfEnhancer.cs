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

        /// <summary>
        /// Set the AFRelationship, use the actual file names, and set the size, mod date, and creation date for each attachment
        /// </summary>
        /// <param name="embeddedFiles"></param>
        /// <exception cref="Exception"></exception>
        public void NormalizeAttachments(List<EmbeddedFile> embeddedFiles)
        {
            foreach (var embeddedFileGrp in embeddedFiles.GroupBy(e => e.UniqueName)) //The embeddedFiles list can contain duplicates, so group by PdfName and use the first one in the group or use the group to create a new PdfName
            {
                var embeddedFile = embeddedFileGrp.First();

                var fileName = embeddedFile.UniqueName;
                var desc = embeddedFile.Description;
                var fileNames = embeddedFileGrp.Select(e => e.OriginalFileName).Distinct().ToList();
                var descs = embeddedFileGrp.Select(e => e.Description).Distinct().ToList();

                //For duplicates only use the original filename if all the duplicates use the same name; otherwise use the Pdfname, and put something in the description
                if (fileNames.Count == 1)
                {
                    fileName = fileNames[0];
                }
                else if (fileNames.Count > 1)
                {
                    desc = $"* {desc} *[File occurs {fileNames.Count} times with different filenames: '{string.Join("', '", fileNames)}']";
                }

                if (descs.Count > 1)
                {
                    desc = $"* {desc} *[File is attached to multiple messages; description is for the first.]";
                }


                var annotFileSpecList = GetAnnotFileSpecList(embeddedFile.UniqueName);
                foreach (var (annot, filespec) in annotFileSpecList)
                {
                    //Update the values of the /Contents and /T entries in the annotation dictionary for the file attachment annotation
                    //Also add a /NM entry to the annotation dictionary, to maintain the linkage to the original file spec
                    if (annot != null)
                    {
                        var d = new PdfDate();
                        annot.Put(PdfName.Nm, new PdfString(embeddedFile.UniqueName, PdfObject.TEXT_UNICODE));
                        annot.Put(PdfName.Contents, new PdfString(desc, PdfObject.TEXT_UNICODE));
                        annot.Put(new PdfName("Subj"), new PdfString(fileName, PdfObject.TEXT_UNICODE));
                        annot.Put(PdfName.T, new PdfString($"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name} {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}", PdfObject.TEXT_UNICODE));
                        annot.Put(PdfName.Creationdate, d);
                        annot.Put(PdfName.M, d);
                    }

                    filespec.Put(new PdfName("AFRelationship"), new PdfName(embeddedFile.Relationship.ToString()));

                    filespec.Put(new PdfName("F"), new PdfString(fileName, PdfObject.TEXT_UNICODE));
                    filespec.Put(new PdfName("UF"), new PdfString(fileName, PdfObject.TEXT_UNICODE));
                    if (!string.IsNullOrWhiteSpace(desc))
                        filespec.Put(new PdfName("Desc"), new PdfString(desc, PdfObject.TEXT_UNICODE));

                    var efDict = filespec.GetAsDict(PdfName.EF) ?? throw new Exception($"Filespec EmbeddedFile (EF) for attachment '{embeddedFile.UniqueName}' not found");

                    var efStream = efDict.GetAsStream(PdfName.F) ?? throw new Exception($"Filespec EmbeddedFile stream for attachment '{embeddedFile.UniqueName}' not found");

                    efStream.Put(new PdfName("Subtype"), new PdfName(embeddedFile.Subtype));

                    var paramsDict = efStream.GetAsDict(PdfName.Params);

                    if (paramsDict == null)
                    {
                        paramsDict = new PdfDictionary();
                        paramsDict.Put(PdfName.Size, new PdfNumber(embeddedFile.Size));
                        efStream.Put(PdfName.Params, paramsDict);
                    }
                    var size = paramsDict.GetAsNumber(PdfName.Size) ?? throw new Exception($"Filespec EmbeddedFile stream Params Size for attachment '{embeddedFile.UniqueName}' not found");
                    if ((long)size.FloatValue != embeddedFile.Size) throw new Exception($"Filespec EmbeddedFile stream Params Size for attachment '{embeddedFile.UniqueName}' does not match");


                    if (embeddedFile.ModDate != null) paramsDict.Put(new PdfName("ModDate"), new PdfDate(embeddedFile.ModDate ?? DateTime.Now));
                    if (embeddedFile.CreationDate != null) paramsDict.Put(new PdfName("CreationDate"), new PdfDate(embeddedFile.ModDate ?? DateTime.Now));
                }


            }

        }

        private List<(PdfDictionary? annotation, PdfDictionary filespec)> GetAnnotFileSpecList(string name)
        {
            List<(PdfDictionary? annotation, PdfDictionary filespec)> ret = new();

            var catalog = _reader.Catalog ?? throw new Exception("Catalog not found");

            //attachments via the catalog Names Embeddedfiles entries
            var names = catalog.GetAsDict(PdfName.Names);
            var embeddedFilesDict = names?.GetAsDict(PdfName.Embeddedfiles);

            //attachments via the page annotations entries
            var annotationAttachments = GetFileAttachmentAnnotations();

            if (embeddedFilesDict != null)
            {
                var fs = (PdfDictionary?)GetObjFromNameTree(embeddedFilesDict, name);
                if (fs != null)
                    ret.Add((null, fs));
            }

            if (annotationAttachments.ContainsKey(name))
            {
                ret.AddRange(annotationAttachments[name]);
            }

            if (ret == null)
                throw new Exception($"Filespec for attachment '{name}' not found");

            return ret;
        }

        object locker = new object();
        Dictionary<string, List<(PdfDictionary? annotation, PdfDictionary filespec)>>? _fileAttachmentAnnotations;
        private Dictionary<string, List<(PdfDictionary? annotation, PdfDictionary filespec)>> GetFileAttachmentAnnotations()
        {
            lock (locker)
            {
                if (_fileAttachmentAnnotations == null)
                {
                    _fileAttachmentAnnotations = new();

                    int pageCount = _reader.NumberOfPages;
                    for (int i = 1; i <= pageCount; i++)
                    {
                        PdfDictionary pageDict = _reader.GetPageN(i) ?? throw new Exception($"Unable to get page {i}");
                        PdfArray annots = pageDict.GetAsArray(PdfName.Annots);

                        if (annots == null)
                            continue;

                        for (int j = 0; j < annots.Size; j++)
                        {
                            PdfDictionary annot = annots.GetAsDict(j) ?? throw new Exception("Unable to retrieve Annot dictionary");
                            var name = annot.GetAsName(PdfName.Subtype);
                            if (name == PdfName.Fileattachment)
                            {
                                var fs = annot.GetAsDict(PdfName.Fs) ?? throw new Exception("Unable to retrieve Annotation Filespec dictionary (Fs)");
                                var f = fs.GetAsString(PdfName.F) ?? throw new Exception("Unable to retrieve Filespec Filename (F)");
                                if (_fileAttachmentAnnotations.ContainsKey(f.ToUnicodeString()))
                                {
                                    _fileAttachmentAnnotations[f.ToUnicodeString()].Add((annot, fs));
                                }
                                else
                                {
                                    _fileAttachmentAnnotations.Add(f.ToUnicodeString(), (new List<(PdfDictionary? annotation, PdfDictionary filespec)>() { (annot, fs) }));
                                }
                            }
                        }

                    }
                }
                return _fileAttachmentAnnotations;
            }
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
                if (dpartNode.Parent == null)
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

        private PdfObject? GetObjFromNameTree(PdfDictionary nameTree, string name)
        {
            if (nameTree == null)
                throw new ArgumentNullException(nameof(nameTree));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            PdfObject? ret = null;

            PdfArray names = nameTree.GetAsArray(PdfName.Names);
            PdfArray kids = nameTree.GetAsArray(PdfName.Kids);
            PdfArray limits = nameTree.GetAsArray(PdfName.Limits);

            string lowerLimit;
            string upperLimit;
            if (limits != null)
            {
                lowerLimit = limits.GetAsString(0).ToString();
                upperLimit = limits.GetAsString(1).ToString();
                int compLower = string.Compare(name, lowerLimit, StringComparison.Ordinal);
                int compUpper = string.Compare(name, upperLimit, StringComparison.Ordinal);
                if (compLower < 0 || compUpper > 0)
                    return null;
            }

            if (names != null)
            {
                //TODO: this is a linear search, could be improved since the names are sorted

                for (int i = 0; i < names.ArrayList.Count; i += 2)
                {
                    var key = names.GetAsString(i).ToString();
                    var value = names.GetDirectObject(i + 1);
                    if (string.Compare(name, key, StringComparison.Ordinal) == 0)
                    {
                        return value;
                    }

                }
            }
            else if (kids != null)
            {
                for (int i = 0; i < kids.Size; i += 1)
                {
                    PdfObject nodes = (PdfDictionary)kids.GetDirectObject(i);
                    if (nodes.IsDictionary())
                    {
                        ret = GetObjFromNameTree((PdfDictionary)nodes, name);
                        if (ret != null)
                            break;
                    }
                    else
                    {
                        throw new Exception("Invalid kids");
                    }
                }
            }
            else
            {
                throw new Exception("Invalid name tree");
            }


            return ret;
        }
    }
}
