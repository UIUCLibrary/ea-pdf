using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public interface IPdfEnhancerFactory
    {
        IPdfEnhancer Create(ILogger logger, string inPdfFilePath, string outPdfFilePath);
    }
}
