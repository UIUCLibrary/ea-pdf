using Microsoft.Extensions.Logging;
using NDepend.Path;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class XepToPdfTransformer : JavaRunner, IXslFoTransformer
    {
        public const string CLASS_PATH = "C:\\Program Files\\RenderX\\XEP\\lib\\xep.jar;C:\\Program Files\\RenderX\\XEP\\lib\\saxon6.5.5\\saxon.jar;C:\\Program Files\\RenderX\\XEP\\lib\\saxon6.5.5\\saxon-xml-apis.jar;C:\\Program Files\\RenderX\\XEP\\lib\\xt.jar";
        public const string MAIN_CLASS = "com.renderx.xep.XSLDriver";

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

                int status = RunMainClass(MAIN_CLASS, args, ref messages);

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
            var args = $"\"-DCONFIG={ConfigFilePath}\" -fo \"{sourceFoFilePath}\" -pdf \"{outputPdfFilePath}\"";

            int status = RunMainClass(MAIN_CLASS, args, ref messages);

            return status;

        }
    }
}
