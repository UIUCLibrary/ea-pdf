using Microsoft.Extensions.Logging;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public class ITextSharpPdfEnhancerFactory : IPdfEnhancerFactory
    {
        public IPdfEnhancer Create(ILogger logger, string inPdfFilePath, string outPdfFilePath)
        {
            return new ITextSharpPdfEnhancer(logger, inPdfFilePath, outPdfFilePath);
        }
    }
}
