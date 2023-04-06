namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public interface IPdfEnhancer : IDisposable
    {
        /// <summary>
        /// Add XMP metadata to specific pages of a PDF file; this adds a /Metdata key to the page dictionaries
        /// </summary>
        /// <param name="dparts">Dictionary where the key is a tuple of named destinations for the start page and end part of the DPart (the end page is not used by this method), and the value is the XMP string to associate with that page</param>
        public void AddXmpToPages(DPartInternalNode dparts);

        /// <summary>
        /// Add XMP metadata to specific Document Parts (DParts) of a PDF file; this creates the DPart hierarchy and adds a /DPM key to the DPart dictionaries
        /// </summary>
        /// <param name="dparts">Dictionary where the key is a tuple of named destinations for the start page and end part of the DPart, and the value is the XMP string to associate with that DPart</param>
        public void AddXmpToDParts(DPartInternalNode dparts);

        /// <summary>
        /// Set the XMP metadata for the entire document
        /// </summary>
        /// <param name="xmp">XMP string for the document</param>
        public void SetDocumentXmp(string xmp);

    }
}
