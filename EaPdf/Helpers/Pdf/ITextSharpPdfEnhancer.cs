using Aron.Weiler;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Logging;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;
using System.util;

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

        public Dictionary<string,string> PdfInfo
        {
            get
            {
                return (Dictionary<string, string>)(_reader.Info);
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
                    desc = Regex.Replace(desc, @"^\* \* ", "** ");
                }


                var annotFileSpecList = GetAnnotFileSpecList(embeddedFile.UniqueName);
                foreach (var (annot, filespec, indRef) in annotFileSpecList)
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

            AddFileAttachmentAnnots(embeddedFiles);

            if(PdfInfo.ContainsKey("Producer") && PdfInfo["Producer"].Contains("Apache FOP"))
            {
                //if the PDF was produced by FOP, the AddFileAttachmentAnnots will cause duplicate entries in the file attachments list
                //so delete the /Catalog /Names /EmbeddedFiles entry
                //TODO:  This will leave an orphaned name tree in the document, which will be removed by the RemoveUnusedObjects call in the Dispose method
                var catalog = _reader.Catalog ?? throw new Exception("Catalog not found");
                var names = catalog.GetAsDict(PdfName.Names);
                names?.Remove(PdfName.Embeddedfiles);
            }

        }

        private void AddFileAttachmentAnnots(List<EmbeddedFile> embeddedFiles)
        {
            var catalog = _reader.Catalog ?? throw new Exception("Catalog not found");

            //named destinations via the catalog Names Dests entries
            var names = catalog.GetAsDict(PdfName.Names);
            var destsDict = names?.GetAsDict(PdfName.Dests) ?? throw new Exception("Unable to get the Catalog Names Dests name tree");

            var linkAnnotations = GetLinkAnnotations();

            var annotAppearanceStream = AddAnnotAppearanceStream();

            foreach (var embeddedFile in embeddedFiles)
            {
                var objs = GetObjListFromNameTree(destsDict, "X_" + embeddedFile.Hash);

                var annotFileSpecs = GetAnnotFileSpecList(embeddedFile.UniqueName);
                foreach (var annotFileSpec in annotFileSpecs) //There can be multiple annotations pointing to the same embedded file 
                    //TODO: Take advantage of this to provide unique filenames for duplicate attachments
                {
                    foreach ((PdfObject obj, PdfIndirectReference indRef) in objs)
                    {

                        var linkAnnots = linkAnnotations[indRef.ToString()];
                        foreach (var linkAnnot in linkAnnots)
                        {
                            var d = new PdfDate();

                            //convert the link annotation into a file attachment annotation
                            linkAnnot.Put(PdfName.Subtype, PdfName.Fileattachment);
                            linkAnnot.Put(PdfName.Name, new PdfName("Paperclip"));
                            linkAnnot.Put(PdfName.Nm, new PdfString(embeddedFile.UniqueName, PdfObject.TEXT_UNICODE));
                            linkAnnot.Put(PdfName.Contents, new PdfString(embeddedFile.Description, PdfObject.TEXT_UNICODE));  //TODO: Look if the MIME header has a description
                            //linkAnnot.Put(new PdfName("Subj"), new PdfString("TODO: Subj", PdfObject.TEXT_UNICODE)); //TODO: Look if the MIME header has a subject
                            linkAnnot.Put(PdfName.T, new PdfString($"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name} {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}", PdfObject.TEXT_UNICODE));
                            linkAnnot.Put(PdfName.Creationdate, d);
                            linkAnnot.Put(PdfName.M, d);
                            linkAnnot.Put(PdfName.Fs, annotFileSpec.indRef);
                            var ap = new PdfDictionary();
                            ap.Put(PdfName.N, annotAppearanceStream.IndirectReference);
                            linkAnnot.Put(PdfName.Ap, ap);
                            //linkAnnot.Put(PdfName.P, indRef); //TODO: Need to get the indirect reference to the page object
                            linkAnnot.Remove(PdfName.A);
                            linkAnnot.Remove(PdfName.H);
                            linkAnnot.Remove(PdfName.Structparent);
                        }

                    }
                }

            }
        }

        private PdfIndirectObject AddAnnotAppearanceStream()
        {
            using var memStrm = new MemoryStream();
            using var strmWrtr = new StreamWriter(memStrm);
            //TODO: the graphic is cribbed from a RenderX XEP-produced file; may need to be modified to avoid licensing issues
            strmWrtr.Write("q\r\n1.0 1.0 0.0 rg 0 G 0 i 0.59 w 4 M 1 j 0 J []0 d  0.51 13.63 m 0.51 13.25 0.48 4.38 0.48 3.74 c 0.48 3.29 0.49 1.93 1.38 1.05 c 1.89 0.55 2.59 0.31 3.45 0.32 c 5.46 0.36 6.60 1.61 6.57 3.76 c 6.56 4.66 6.57 10.39 6.57 10.45 c 6.57 10.70 6.36 10.90 6.11 10.90 c 5.86 10.90 5.65 10.70 5.65 10.44 c 5.65 10.21 5.64 4.66 5.65 3.75 c 5.67 2.09 4.95 1.27 3.44 1.24 c 2.83 1.23 2.35 1.39 2.03 1.71 c 1.40 2.32 1.40 3.39 1.40 3.75 c 1.40 4.37 1.43 13.24 1.43 13.63 c 1.43 13.97 1.52 15.65 3.03 15.65 c 3.91 15.65 4.29 15.09 4.29 13.77 c 4.29 13.63 l 4.30 13.30 4.28 9.30 4.27 7.24 c 4.27 7.23 4.27 7.22 4.27 7.21 c 4.28 7.00 4.25 6.32 3.96 6.03 c 3.87 5.94 3.76 5.89 3.60 5.90 c 2.85 5.91 2.86 7.23 2.86 7.24 c 2.84 10.81 l 2.83 11.07 2.63 11.27 2.37 11.27 c 2.12 11.27 1.92 11.06 1.92 10.81 c 1.94 7.24 l 1.93 6.47 2.26 5.00 3.59 4.98 c 3.99 4.97 4.35 5.12 4.62 5.40 c 5.21 6.01 5.20 7.06 5.19 7.24 c 5.19 7.50 5.22 13.24 5.20 13.66 c 5.20 13.77 l 5.21 14.92 4.92 15.61 4.51 16.03 c 4.08 16.45 3.53 16.57 3.03 16.57 c 1.05 16.57 0.52 14.72 0.51 13.63 c h B \r\nQ\r\n");
            strmWrtr.Flush();
            memStrm.Position = 0;

            var annotAppearanceStream = new PdfStream(memStrm, _stamper.Writer);
            annotAppearanceStream.Put(PdfName.TYPE, PdfName.Xobject);
            annotAppearanceStream.Put(PdfName.Subtype, PdfName.Form);
            var bb = new PdfArray();
            bb.Add(new PdfNumber(0));
            bb.Add(new PdfNumber(0));
            bb.Add(new PdfNumber(7));
            bb.Add(new PdfNumber(17));
            annotAppearanceStream.Put(PdfName.Bbox, bb);
            annotAppearanceStream.Put(PdfName.Resources, new PdfDictionary());
            var ret = _stamper.Writer.AddToBody(annotAppearanceStream);
            annotAppearanceStream.WriteLength();

            strmWrtr.Close();
            memStrm.Close();

            return ret;
        }

        private List<(PdfDictionary? annotation, PdfDictionary filespec, PdfIndirectReference indRef)> GetAnnotFileSpecList(string name)
        {
            List<(PdfDictionary? annotation, PdfDictionary filespec, PdfIndirectReference indRef)> ret = new();

            var catalog = _reader.Catalog ?? throw new Exception("Catalog not found");

            //attachments via the catalog Names Embeddedfiles entries
            var names = catalog.GetAsDict(PdfName.Names);
            var embeddedFilesDict = names?.GetAsDict(PdfName.Embeddedfiles);

            //attachments via the page annotations entries
            var annotationAttachments = GetFileAttachmentAnnotations();

            if (embeddedFilesDict != null)
            {
                var t = GetObjFromNameTree(embeddedFilesDict, name);
                if (t?.obj is PdfDictionary fs)
                    ret.Add((null, fs, t.Value.indRef));
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
        Dictionary<string, List<(PdfDictionary? annotation, PdfDictionary filespec, PdfIndirectReference indRef)>>? _fileAttachmentAnnotations;
        private Dictionary<string, List<(PdfDictionary? annotation, PdfDictionary filespec, PdfIndirectReference indRef)>> GetFileAttachmentAnnotations()
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
                            var subtype = annot.GetAsName(PdfName.Subtype);
                            if (subtype == PdfName.Fileattachment)
                            {
                                var fs = annot.GetAsDict(PdfName.Fs) ?? throw new Exception("Unable to retrieve Annotation Filespec dictionary (Fs)");
                                var fsRef = annot.GetAsIndirectObject(PdfName.Fs) ?? throw new Exception("Unable to retrieve Annotation Filespec indirect reference (Fs)");
                                var indRef = fsRef.IndRef;

                                var f = fs.GetAsString(PdfName.F) ?? throw new Exception("Unable to retrieve Filespec Filename (F)");
                                if (_fileAttachmentAnnotations.ContainsKey(f.ToUnicodeString()))
                                {
                                    _fileAttachmentAnnotations[f.ToUnicodeString()].Add((annot, fs, fsRef));
                                }
                                else
                                {
                                    _fileAttachmentAnnotations.Add(f.ToUnicodeString(), (new List<(PdfDictionary? annotation, PdfDictionary filespec, PdfIndirectReference indRef)>() { (annot, fs, fsRef) }));
                                }
                            }
                        }

                    }
                }
                return _fileAttachmentAnnotations;
            }
        }

        Dictionary<string, List<PdfDictionary>>? _linkAnnotations;
        private Dictionary<string, List<PdfDictionary>> GetLinkAnnotations()
        {
            lock (locker)
            {
                if (_linkAnnotations == null)
                {
                    _linkAnnotations = new();

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
                            var subtype = annot.GetAsName(PdfName.Subtype);
                            if (subtype == PdfName.Link)
                            {
                                var a = annot.Get(PdfName.A);
                                if (a == null)
                                    continue;

                                var key = a.ToString();
                                if (_linkAnnotations.ContainsKey(key))
                                {
                                    _linkAnnotations[key].Add(annot);
                                }
                                else
                                {
                                    _linkAnnotations.Add(key, (new List<PdfDictionary>() { annot }));
                                }
                            }
                        }

                    }
                }
                return _linkAnnotations;
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

        /// <summary>
        /// Return the object and indirect reference from a name tree, where the name matches the given string
        /// </summary>
        /// <param name="nameTree"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        private (PdfObject obj, PdfIndirectReference indRef)? GetObjFromNameTree(PdfDictionary nameTree, string name)
        {
            if (nameTree == null)
                throw new ArgumentNullException(nameof(nameTree));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            (PdfObject obj, PdfIndirectReference indRef)? ret = null;

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
                    var indRef = names.GetAsIndirectObject(i + 1);
                    if (string.Compare(name, key, StringComparison.Ordinal) == 0)
                    {
                        return (value, indRef);
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

        /// <summary>
        /// Return a list of objects and indirect references from a name tree, where the name starts with the given string
        /// </summary>
        /// <param name="nameTree"></param>
        /// <param name="nameStartsWith"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        private List<(PdfObject obj, PdfIndirectReference indRef)> GetObjListFromNameTree(PdfDictionary nameTree, string nameStartsWith)
        {
            if (nameTree == null)
                throw new ArgumentNullException(nameof(nameTree));

            if (string.IsNullOrWhiteSpace(nameStartsWith))
                throw new ArgumentNullException(nameof(nameStartsWith));

            List<(PdfObject obj, PdfIndirectReference indRef)> ret = new();

            PdfArray names = nameTree.GetAsArray(PdfName.Names);
            PdfArray kids = nameTree.GetAsArray(PdfName.Kids);
            PdfArray limits = nameTree.GetAsArray(PdfName.Limits);

            string lowerLimit;
            string upperLimit;
            if (limits != null)
            {
                lowerLimit = limits.GetAsString(0).ToString();
                upperLimit = limits.GetAsString(1).ToString();
                //becausing looking for names starting with string, truncate the limits to the length of the nameStartsWith string
                lowerLimit = lowerLimit.Substring(0, Math.Min(lowerLimit.Length, nameStartsWith.Length));
                upperLimit = upperLimit.Substring(0, Math.Min(upperLimit.Length, nameStartsWith.Length));
                int compLower = string.Compare(nameStartsWith, lowerLimit, StringComparison.Ordinal);
                int compUpper = string.Compare(nameStartsWith, upperLimit, StringComparison.Ordinal);
                if (compLower < 0 || compUpper > 0)
                    return ret;
            }

            if (names != null)
            {
                //TODO: this is a linear search, could be improved since the names are sorted

                for (int i = 0; i < names.ArrayList.Count; i += 2)
                {
                    var key = names.GetAsString(i).ToString();
                    var value = names.GetDirectObject(i + 1);
                    var indRef = names.GetAsIndirectObject(i + 1);
                    if (key.StartsWith(nameStartsWith, StringComparison.Ordinal))
                    {
                        ret.Add((value, indRef));
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
                        ret.AddRange(GetObjListFromNameTree((PdfDictionary)nodes, nameStartsWith));
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
