using Microsoft.Extensions.Logging;

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

                return messages[0].message;
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
        public int Transform(string sourceFoFilePath, string outputPdfFilePath, ref List<(LogLevel level, string message)> messages)
        {
            var args = $"-c \"{ConfigFilePath}\" -fo \"{sourceFoFilePath}\" -pdf \"{outputPdfFilePath}\"";

            int status = RunExecutableJar(JarFilePath, args, ref messages);

            return status;
        }
    }
}
