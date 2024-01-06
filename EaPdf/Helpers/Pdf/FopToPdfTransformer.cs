using Microsoft.Extensions.Logging;
using System.Text;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public class FopToPdfTransformer : JavaRunner, IXslFoTransformer
    {
        public const string JAR_FILE = "C:\\Program Files\\Apache FOP\\fop-2.8\\fop\\build\\fop.jar";

        public FopToPdfTransformer(string jarFilePath, string configFilePath)
        {
            JarFilePath = jarFilePath;
            ConfigFilePath = configFilePath;
        }

        public FopToPdfTransformer(string configFilePath) : this(JAR_FILE, configFilePath)
        {
        }

        public string ConfigFilePath { get; set; }

        public string JarFilePath { get; }

        public string ProcessorVersion
        {
            get
            {
                var args = "-version";
                List<(LogLevel level, string message)> messages = new();
                _ = RunExecutableJar(JarFilePath, args, ref messages);

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

            //-q option to suppress output except warnings and errors; unfortunately doesn't seem to make a difference
            var args = $" -q {extraCommandLineParams} -c \"{ConfigFilePath}\" -fo \"{sourceFoFilePath}\" -pdf \"{outputPdfFilePath}\"";

            int status = RunExecutableJar(JarFilePath, args, ref tempMessages);

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

            List<(LogLevel level, string message)> ret = new();

            StringBuilder messageAccumulator = new();
            LogLevel logLevel = LogLevel.None;

            foreach ((LogLevel level, string message) message in messages)
            {
                //Date Format:  Jul 19, 2023 11:55:07 AM or Jul 19, 2023 1:55:07 AM (one-digit hour)
                if (message.message == "USAGE" || (message.message.Length >=24 && DateTime.TryParseExact(message.message[..24], "MMM dd, yyyy h:mm:ss tt", null, System.Globalization.DateTimeStyles.AssumeLocal | System.Globalization.DateTimeStyles.AllowTrailingWhite, out DateTime dateTime)))
                {
                    //start of new message
                    if (messageAccumulator.Length > 0)
                    {
                        AppendMessage(ref logLevel, ref messageAccumulator, ref ret);
                    }

                    messageAccumulator.AppendLine(message.message);
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
