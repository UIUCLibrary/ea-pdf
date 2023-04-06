using Microsoft.Extensions.Logging;

namespace UIUCLibrary.EaPdf.Helpers
{
    public interface IXsltTransformer
    {
        /// <summary>
        /// Transform the source file into the output file using the xslt file and parameters
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="xsltFilePath"></param>
        /// <param name="outputFilePath"></param>
        /// <param name="xsltParams"></param>
        /// <param name="messages"></param>
        /// <returns>the status code for the transformation, usually the same as returned by the tranformation command line process; 0 usually indicates success</returns>
        int Transform(string sourceFilePath, string xsltFilePath, string outputFilePath, Dictionary<string,object>? xsltParams, ref List<(LogLevel level, string message)> messages);

        string ProcessorVersion { get; }

    }
}
