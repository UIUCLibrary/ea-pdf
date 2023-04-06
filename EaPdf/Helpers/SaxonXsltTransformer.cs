using Microsoft.Extensions.Logging;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class SaxonXsltTransformer : JavaRunner, IXsltTransformer
    {
        const string CLASS_PATH = "C:\\Program Files\\SaxonHE11-5J\\saxon-he-11.5.jar";
        const string MAIN_CLASS = "net.sf.saxon.Transform";

        public SaxonXsltTransformer() : base(CLASS_PATH)
        {
        }

        public string ProcessorVersion
        {
            get
            {
                List<(LogLevel level, string message)> messages = new();
                int status = RunMainClass("net.sf.saxon.Version", ref messages);

                if (status == 0)
                    return messages[0].message;
                else
                    return "UNKNOWN";
            }
        }

        /// <summary>
        /// Transform the source file into the output file using the xslt file and parameters
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="xsltFilePath"></param>
        /// <param name="outputFilePath"></param>
        /// <param name="xsltParams"></param>
        /// <param name="messages"></param>
        /// <returns>the status code for the transformation, usually the same as returned by the tranformation command line process; 0 usually indicates success</returns>
        public int Transform(string sourceFilePath, string xsltFilePath, string outputFilePath, Dictionary<string, object>? xsltParams, ref List<(LogLevel level, string message)> messages)
        {
            var xparams = "";
            if (xsltParams != null)
            {
                foreach (var kvp in xsltParams)
                {
                    xparams += $"\"{kvp.Key}\"=\"{kvp.Value}\" ";
                }
            }

            var args = $"-s:\"{sourceFilePath}\" -xsl:\"{xsltFilePath}\" -o:\"{outputFilePath}\" {xparams}";


            int status = RunMainClass(MAIN_CLASS, args, ref messages);

            return status;

        }
    }
}
