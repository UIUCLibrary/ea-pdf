using Microsoft.Extensions.Logging;
using System.Text;

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
            List<(LogLevel level, string message)> tempMessages = new();

            var xparams = "";
            if (xsltParams != null)
            {
                foreach (var kvp in xsltParams)
                {
                    xparams += $"\"{kvp.Key}\"=\"{kvp.Value}\" ";
                }
            }

            var args = $"-s:\"{sourceFilePath}\" -xsl:\"{xsltFilePath}\" -o:\"{outputFilePath}\" {xparams}";


            int status = RunMainClass(MAIN_CLASS, args, ref tempMessages);

            messages.AddRange(ConvertLogLines(tempMessages));

            return status;

        }

        /// <summary>
        /// Convert message lines to the correct log level and granularity 
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private static List<(LogLevel level, string message)> ConvertLogLines(List<(LogLevel level, string message)> messages)
        {
            //Saxon can have one message that crosses multiple lines; a new message does not have leading white space, but additional lines start with spaces

            List<(LogLevel level, string message)> ret = new();

            StringBuilder messageAccumulator = new();
            LogLevel logLevel = LogLevel.None;

            foreach ((LogLevel level, string message) message in messages)
            {
                if (message.message.IndexOfAny(new char[] { ' ', '\t' }) != 0)
                {
                    //start of new message
                    if (messageAccumulator.Length > 0)
                    {
                        AppendMessage(ref logLevel, ref messageAccumulator, ref ret);
                    }

                    if(message.message.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
                    {
                        logLevel = LogLevel.Error;
                    }
                    else if (message.message.StartsWith("Processing terminated", StringComparison.OrdinalIgnoreCase))
                    {
                        logLevel = LogLevel.Critical;
                    }
                    else
                    {
                        logLevel = message.level;
                    }

                    messageAccumulator.AppendLine(message.message);
                }
                else if (message.message.IndexOfAny(new char[] { ' ', '\t' }) == 0)
                {
                    messageAccumulator.AppendLine(message.message);
                }
                else
                {
                    throw new Exception("Could not find start of log message");
                }
            }

            if (messageAccumulator.Length > 0)
            {
                AppendMessage(ref logLevel, ref messageAccumulator, ref ret);
            }

            return ret;
        }
    }
}
