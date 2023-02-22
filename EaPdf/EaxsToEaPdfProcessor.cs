using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using PdfTemplating.XslFO.ApacheFOP.Serverless;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.EaPdf
{
    public class EaxsToEaPdfProcessor
    {

        private readonly ILogger _logger;
        private readonly IXsltTransformer _xslt;
        private readonly IXslFoTransformer _xslfo;

        public EaxsToEaPdfProcessorSettings Settings { get; }

        /// <summary>
        /// Create a processor for converting email xml archive to an EA-PDF file, initializing the logger, converters, and settings
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public EaxsToEaPdfProcessor(ILogger<EaxsToEaPdfProcessor> logger, IXsltTransformer xslt, IXslFoTransformer xslfo, EaxsToEaPdfProcessorSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _xslt = xslt ?? throw new ArgumentNullException(nameof(xslt));
            _xslfo = xslfo ?? throw new ArgumentNullException(nameof(xslfo));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("MboxProcessor Created");

        }

        public void ConvertEaxsToPdf(string eaxsFilePath)
        {
            var foFilePath = Path.ChangeExtension(eaxsFilePath, ".fo");
            var pdfFilePath = Path.ChangeExtension(eaxsFilePath, ".pdf");

            var xsltParams = new Dictionary<string, object>
            {
                { "fo-processor-version", _xslfo.ProcessorVersion }
            };

            List<(LogLevel,string)> messages = new();
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltFilePath, foFilePath, xsltParams, ref messages);


        }
    }
}
