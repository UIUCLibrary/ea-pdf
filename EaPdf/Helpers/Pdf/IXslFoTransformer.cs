using Microsoft.Extensions.Logging;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    /// <summary>
    /// The XSL-FO processors that can be used
    /// When a new processor is added, add it to this enum 
    /// </summary>
    public enum FoProcessor
    {
        ApacheFop,
        RenderXXep
    }

    public interface IXslFoTransformer
    {

        /// <summary>
        /// Transform the source FO file into the output PDF file using the xslt file and parameters
        /// </summary>
        /// <param name="sourceFoFilePath"></param>
        /// <param name="outputPdfFilePath"></param>
        /// <param name="extraCommandLineParams"></param>
        /// <param name="messages"></param>
        /// <returns>the status code for the transformation, usually the same as returned by the tranformation command line process; 0 usually indicates success</returns>
        int Transform(string sourceFoFilePath, string outputPdfFilePath, string? extraCommandLineParams, ref List<(LogLevel level, string message)> messages);

        string ProcessorVersion { get; }
    }
}
