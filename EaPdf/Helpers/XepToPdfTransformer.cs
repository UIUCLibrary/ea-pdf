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
        const string CLASS_PATH = "C:\\Program Files\\RenderX\\XEP\\lib\\xep.jar;C:\\Program Files\\RenderX\\XEP\\lib\\saxon6.5.5\\saxon.jar;C:\\Program Files\\RenderX\\XEP\\lib\\saxon6.5.5\\saxon-xml-apis.jar;C:\\Program Files\\RenderX\\XEP\\lib\\xt.jar";
        const string MAIN_CLASS = "com.renderx.xep.XSLDriver";

        public XepToPdfTransformer() : base(CLASS_PATH)
        {
        }

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
        public int Transform(string sourceFoFilePath, string configFilePath, string outputPdfFilePath, ref List<(LogLevel level, string message)> messages)
        {
            var args = $"\"-DCONFIG={configFilePath}\" -fo \"{sourceFoFilePath}\" -pdf \"{outputPdfFilePath}\"";

            int status = RunMainClass(MAIN_CLASS, args, ref messages);

            return status;

        }
    }
}
