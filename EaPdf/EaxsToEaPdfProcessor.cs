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
        public const string FO_XSLT = "eaxs_to_fo.xslt";

        private readonly ILogger _logger;

        public EaxsToEaPdfProcessorSettings Settings { get; }

        /// <summary>
        /// Create a processor for converting email xml archive to an EA-PDF file, initializing the logger and settings
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public EaxsToEaPdfProcessor(ILogger<EaxsToEaPdfProcessor> logger, EaxsToEaPdfProcessorSettings settings)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("MboxProcessor Created");

        }

        public void ConvertEaxsToPdf(string eaxsFilePath)
        {
            var foFilePath = Path.ChangeExtension(eaxsFilePath, ".fo");

            var xsltParams = new Dictionary<string, object>
            {
                { "fo-processor", "xep" }
            };

            List<(LogLevel,string)> messages = new(); ;
            var transformer = new SaxonXsltTransformer();
            var status = transformer.Transform(eaxsFilePath, FO_XSLT, foFilePath, xsltParams, ref messages);


        }
    }
}
