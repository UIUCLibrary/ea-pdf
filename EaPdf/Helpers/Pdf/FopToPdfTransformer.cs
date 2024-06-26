using Microsoft.Extensions.Logging;
using System.Text;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public class FopToPdfTransformer : JavaRunner, IXslFoTransformer
    {
        const string CLASS_PATH = "C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\build\\fop-2.9.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\build\\fop-core-2.9.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\build\\fop-events-2.9.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\build\\fop-util-2.9.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-anim-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-awt-util-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-bridge-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-codec-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-constants-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-css-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-dom-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-ext-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-extension-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-gvt-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-i18n-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-parser-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-script-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-shared-resources-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-svg-dom-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-svggen-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-transcoder-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-util-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\batik-xml-1.17.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\commons-io-2.11.0.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\commons-logging-1.0.4.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\fontbox-2.0.27.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\xml-apis-1.4.01.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\xml-apis-ext-1.3.04.jar;C:\\Program Files\\Apache FOP\\fop-2.9\\fop\\lib\\xmlgraphics-commons-2.9.jar";
        const string MAIN_CLASS = "org.apache.fop.cli.Main";

        public FopToPdfTransformer(string classPath, string configFilePath) : base(classPath)
        {
            ConfigFilePath = configFilePath;
        }

        public FopToPdfTransformer(string configFilePath) : this(CLASS_PATH, configFilePath)
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
                if (!ret.StartsWith("FOP",StringComparison.OrdinalIgnoreCase))
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
        /// <returns>the status code for the transformation, usually the same as returned by the transformation command line process; 0 usually indicates success</returns>
        public int Transform(string sourceFoFilePath, string outputPdfFilePath, string? extraCommandLineParams, ref List<(LogLevel level, string message)> messages)
        {
            List<(LogLevel level, string message)> tempMessages = new();

            string config = "";
            if (!string.IsNullOrWhiteSpace(ConfigFilePath))
            {
                config = $"-c \"{ConfigFilePath}\"";
            }
            //-q option to suppress output except warnings and errors; unfortunately doesn't seem to make a difference
            var args = $" -q {extraCommandLineParams} {config} -fo \"{sourceFoFilePath}\" -pdf \"{outputPdfFilePath}\"";

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

            //FOP has one log message per multiple lines which could be info, warn, or error
            //The first line of the message is the date and time, the second line is the log level, and the rest of the lines are the message
            //Debug and Trace messages are always on their own line

            List<(LogLevel level, string message)> ret = new();

            StringBuilder messageAccumulator = new();
            LogLevel logLevel = LogLevel.None;

            foreach ((LogLevel level, string message) message in messages)
            {
                //Date Format:  Jul 19, 2023 11:55:07 AM or Jul 19, 2023 1:55:07 AM (one-digit hour)
                if (message.level == LogLevel.Trace || message.level == LogLevel.Debug || message.message == "USAGE" || (message.message.Length >=24 && DateTime.TryParseExact(message.message[..24], "MMM dd, yyyy h:mm:ss tt", null, System.Globalization.DateTimeStyles.AssumeLocal | System.Globalization.DateTimeStyles.AllowTrailingWhite, out DateTime dateTime)))
                {
                    //start of new message
                    if (messageAccumulator.Length > 0)
                    {
                        AppendMessage(ref logLevel, ref messageAccumulator, ref ret);
                    }

                    messageAccumulator.AppendLine(message.message);

                    if (message.level == LogLevel.Trace || message.level == LogLevel.Debug) //these are always on their own line
                        logLevel = message.level;

                }
                else if(messageAccumulator.Length > 0)
                {
                    if (logLevel == LogLevel.None)
                    {
                        if (message.message.StartsWith("INFO:"))
                            logLevel = LogLevel.Information;
                        else if (message.message.StartsWith("WARN:") || message.message.StartsWith("WARNING:"))
                            logLevel = LogLevel.Warning;
                        else if (message.message.StartsWith("ERROR:") || message.message.StartsWith("SEVERE:"))
                            logLevel = LogLevel.Error;
                        else if (message.message.StartsWith("FATAL:"))
                            logLevel = LogLevel.Critical;
                        else
                            logLevel = LogLevel.Critical; //default to critical since the first line of the message doesn't indicate the log level
                    }

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

            if(messageAccumulator.Length > 0)
            {
                AppendMessage(ref logLevel, ref messageAccumulator, ref ret);
            }

            //remove the USAGE message which is not useful
            ret.RemoveAll(m => m.message.StartsWith("USAGE\r\n") && m.level==LogLevel.Critical);

            return ret;
        }

    }
}
