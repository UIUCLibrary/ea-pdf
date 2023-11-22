namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public interface IPdfEnhancer : IDisposable
    {

        /// <summary>
        /// Add XMP metadata to specific Document Parts (DParts) of a PDF file; this creates the DPart hierarchy and adds a /DPM key to the DPart dictionaries
        /// If the root DPart has DPM metadata, it will also replace the document level metdata in the Catalog /Metadata key
        /// </summary>
        /// <param name="dparts">Dictionary where the key is a tuple of named destinations for the start page and end part of the DPart, and the value is the XMP string to associate with that DPart</param>
        public void AddXmpToDParts(DPartInternalNode dparts);

        /// <summary>
        /// Add appropriate metadata, extracted from the EAXS XML file, to the PDF file attachments
        /// </summary>
        /// <param name="eaxs"></param>
        public void NormalizeAttachments(List<EmbeddedFile> embeddedFiles);

        /// <summary>
        /// Return a dictionary of the PDF Info metadata
        /// </summary>
        public Dictionary<string, string> PdfInfo { get; }

        /// <summary>
        /// Remove unnecessary or deprecated elements from the PDF file
        /// </summary>
        public void RemoveUnnecessaryElements();
    }
}
