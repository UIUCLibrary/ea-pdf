using Microsoft.Extensions.Logging;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public interface IPdfEnhancerFactory
    {
        IPdfEnhancer Create(ILogger logger, string inPdfFilePath, string outPdfFilePath);
    }
}
