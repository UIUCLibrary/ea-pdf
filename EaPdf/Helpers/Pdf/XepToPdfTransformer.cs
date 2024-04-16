using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Ocsp;
using System.Text;
using System.Text.RegularExpressions;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public class XepToPdfTransformer : JavaRunner, IXslFoTransformer
    {
        const string CLASS_PATH = "C:\\Program Files\\RenderX\\XEP\\lib\\xep.jar;C:\\Program Files\\RenderX\\XEP\\lib\\saxon6.5.5\\saxon.jar;C:\\Program Files\\RenderX\\XEP\\lib\\saxon6.5.5\\saxon-xml-apis.jar;C:\\Program Files\\RenderX\\XEP\\lib\\xt.jar";
        const string MAIN_CLASS = "com.renderx.xep.XSLDriver";

        public XepToPdfTransformer(string classPath, string configFilePath) : base(classPath)
        {
            ConfigFilePath = configFilePath;
        }

        public XepToPdfTransformer(string configFilePath) : this(CLASS_PATH, configFilePath)
        {
        }

        public string ConfigFilePath { get; set; }

        public string ProcessorVersion
        {
            get
            {
                var args = "-version";
                List<(LogLevel level, string message)> messages = new();
                _ = RunMainClass(MAIN_CLASS, args, ref messages);

                string ret = messages[0].message;
                if (!ret.StartsWith("XEP", StringComparison.OrdinalIgnoreCase))
                {
                    ret = "UNKNOWN VERSION";
                }

                return ret;
            }
        }

        /// <summary>
        /// Transform the source file into the output file using the xslt file and parameters
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="xsltFilePath"></param>
        /// <param name="outputFilePath"></param>
        /// <param name="xsltParams"></param>
        /// <param name="extraCommandLineParams"></param>
        /// <param name="messages"></param>
        /// <returns>the status code for the transformation, usually the same as returned by the tranformation command line process; 0 usually indicates success</returns>
        public int Transform(string sourceFoFilePath, string outputPdfFilePath, string? extraCommandLineParams, ref List<(LogLevel level, string message)> messages)
        {
            List<(LogLevel level, string message)> tempMessages = new();

            string config = "";
            if (!string.IsNullOrWhiteSpace(ConfigFilePath))
            {
                config = $"\"-DCONFIG={ConfigFilePath}\" ";
            }

            //-quiet option to suppress output except warnings and errors
            var args = $"{config} -quiet {extraCommandLineParams} -fo \"{sourceFoFilePath}\" -pdf \"{outputPdfFilePath}\"";

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
            //In quiet mode, XEP has one message per line which should all be errors or warnings
            //Except for exceptions, which are multiple lines with the first line being "...something.somethingException" and subsequent lines beginning with tab and being the stack trace
            //Debug and Trace messages are always just one line

            List<(LogLevel level, string message)> ret = new();

            StringBuilder messageAccumulator = new();
            LogLevel logLevel = LogLevel.None;

            foreach ((LogLevel level, string message) message in messages)
            {
                if (message.level == LogLevel.Trace || message.level==LogLevel.Debug)
                {
                    StartNewMessage(message.message, message.level, ref logLevel, ref messageAccumulator, ref ret);
                }
                else if (!message.message.StartsWith('\t') && message.message.StartsWith("[warning]"))
                {
                    StartNewMessage(message.message, LogLevel.Warning, ref logLevel, ref messageAccumulator, ref ret);
                }
                else if (!message.message.StartsWith('\t') && 
                    (message.message.StartsWith("[error]") || message.message.StartsWith("error:") || 
                    message.message.StartsWith("Parse error:") || message.message.StartsWith("Formatter initialization failed:"))
                    )
                {
                    StartNewMessage(message.message, LogLevel.Error, ref logLevel, ref messageAccumulator, ref ret);
                }
                else if (!message.message.StartsWith('\t') && 
                    (Regex.IsMatch(message.message, @"^[\w\.]+\.[\w]+Exception\s*$") || Regex.IsMatch(message.message, @"^[\w\.]+\.[\w]+Exception:"))
                    )
                {
                    StartNewMessage(message.message, LogLevel.Error, ref logLevel, ref messageAccumulator, ref ret);
                }
                else if (message.message.StartsWith('\t'))
                {
                    //continuation of previous message
                    messageAccumulator.AppendLine(message.message);
                }
                else
                {
                    //ret.AddRange(messages);
                    //logLevel= LogLevel.Critical;
                    //messageAccumulator.AppendLine("Could not find start of log message");
                    //break;
                    throw new Exception("Could not find start of log message");
                }
            }

            if (messageAccumulator.Length > 0)
            {
                AppendMessage(ref logLevel, ref messageAccumulator, ref ret);
            }
            return ret;
        }

        private static void StartNewMessage(string message, LogLevel newLevel, ref LogLevel logLevel, ref StringBuilder messageAccumulator, ref List<(LogLevel level, string message)>  ret)
        {
            if (messageAccumulator.Length > 0)
            {
                AppendMessage(ref logLevel, ref messageAccumulator, ref ret);
            }
            logLevel = newLevel; // LogLevel.Error;
            messageAccumulator.AppendLine(message);

        }
    }
}
