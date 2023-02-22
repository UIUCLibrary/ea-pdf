using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers
{
    public interface IFoToPdfTransformer
    {

        /// <summary>
        /// Transform the source file into the output file using the xslt file and parameters
        /// </summary>
        /// <param name="sourceFoFilePath"></param>
        /// <param name="outputPdfFilePath"></param>
        /// <param name="messages"></param>
        /// <returns>the status code for the transformation, usually the same as returned by the tranformation command line process; 0 usually indicates success</returns>
        int Transform(string sourceFoFilePath, string configFilePath, string outputPdfFilePath, ref List<(LogLevel level, string message)> messages);

    }
}
