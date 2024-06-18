using iTextSharp.text.pdf;
using System.Text;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public interface IPdfEnhancer : IDisposable
    {

        /// <summary>
        /// Add Document Parts (DParts) hierarchy to a PDF file; this creates the DPart hierarchy and adds a /DPM key to the DPart dictionaries
        /// If the root DPart has DPM metadata, it will also replace the document level metdata in the Catalog /Metadata key
        /// </summary>
        /// <param name="dparts">The root DPart node containing all the metadata for the folders and messages in the file</param>
        public void AddDPartHierarchy(DPartNode dparts);

        /// <summary>
        /// Add appropriate metadata, extracted from the EAXS XML file, to the PDF file attachments
        /// </summary>
        /// <param name="eaxs"></param>
        public void NormalizeAttachments(List<EmbeddedFile> embeddedFiles);

        /// <summary>
        /// Set the PageMode and ViewerPreferences based on the conformance level and whether the PDF has attachments
        /// </summary>
        /// <param name="conformance"></param>
        /// <param name="hasAttachments"></param>
        public void SetViewerPreferences(PdfMailIdConformance conformance, bool hasAttachments);

        /// <summary>
        /// Return a dictionary of the PDF Info metadata
        /// </summary>
        public Dictionary<string, string> PdfInfo { get; }

        /// <summary>
        /// Remove unnecessary or deprecated elements from the PDF file
        /// </summary>
        public void RemoveUnnecessaryElements();

        /// <summary>
        /// VeraPDF does not like the link GotoR action pointing to a filespec dictionary, just put the external filename there
        /// </summary>
        public void FixGotoRLinks();

        /// <summary>
        /// Get the version of the PDF processor used to enhance the PDF
        /// </summary>
        string ProcessorVersion { get; }

    }
}
