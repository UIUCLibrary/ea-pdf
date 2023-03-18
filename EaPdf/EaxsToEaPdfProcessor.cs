using System.Xml;
using Microsoft.Extensions.Logging;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.EaPdf
{
    public class EaxsToEaPdfProcessor
    {

        public const string EABCC = "eabcc";
        public const string EABCC_NS = "http://emailarchivesgrant.library.illinois.edu/ns/";

        private readonly ILogger _logger;
        private readonly IXsltTransformer _xslt;
        private readonly IXslFoTransformer _xslfo;
        private readonly IPdfEnhancerFactory _enhancerFactory;

        public EaxsToEaPdfProcessorSettings Settings { get; }

        /// <summary>
        /// Create a processor for converting email xml archive to an EA-PDF file, initializing the logger, converters, and settings
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public EaxsToEaPdfProcessor(ILogger<EaxsToEaPdfProcessor> logger, IXsltTransformer xslt, IXslFoTransformer xslfo, IPdfEnhancerFactory enhancerFactory, EaxsToEaPdfProcessorSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _xslt = xslt ?? throw new ArgumentNullException(nameof(xslt));
            _xslfo = xslfo ?? throw new ArgumentNullException(nameof(xslfo));
            _enhancerFactory = enhancerFactory ?? throw new ArgumentNullException(nameof(enhancerFactory));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace($"{this.GetType().Name} Created");

        }

        public void ConvertEaxsToPdf(string eaxsFilePath, string pdfFilePath)
        {
            var foFilePath = Path.ChangeExtension(eaxsFilePath, ".fo");

            var xsltParams = new Dictionary<string, object>
            {
                { "fo-processor-version", _xslfo.ProcessorVersion }
            };

            List<(LogLevel level, string message)> messages = new();
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltFoFilePath, foFilePath, xsltParams, ref messages);
            foreach (var (level, message) in messages)
            {
                _logger.Log(level, message);
            }

            if (status == 0)
            {
                messages.Clear();
                var status2 = _xslfo.Transform(foFilePath, pdfFilePath, ref messages);
                foreach (var (level, message) in messages)
                {
                    _logger.Log(level, message);
                }
                if (status2 != 0)
                {
                    throw new Exception("FO transformation to PDF failed; review log details.");
                }
            }
            else
            {
                throw new Exception("EAXS transformation to FO failed; review log details.");
            }

            //Delete the intermediate FO file
            File.Delete(foFilePath);

            //Do some post processing to add metadata
            AddXmp(eaxsFilePath, pdfFilePath);
        }


        private void AddXmp(string eaxsFilePath, string pdfFilePath)
        {
            var tempOutFilePath = Path.ChangeExtension(pdfFilePath, "out.pdf");

            var pageXmps = GetXmpMetadataForMessages(eaxsFilePath);
            var docXmp = GetDocumentXmp(eaxsFilePath);

            using var enhancer = _enhancerFactory.Create(_logger, pdfFilePath, tempOutFilePath);

            enhancer.SetDocumentXmp(docXmp);
            // enhancer.AddXmpToPages(pageXmps); //Associate XMP with first PDF page of the message
            enhancer.AddXmpToDParts(pageXmps); //Associate XMP with the PDF DPart of the message

            //dispose of the enhancer to make sure files are closed
            enhancer.Dispose();

            //if all is well, move the temp file over the top of the original
            var pdfFi = new FileInfo(pdfFilePath);
            var tempFi = new FileInfo(tempOutFilePath);

            if (tempFi.Exists && tempFi.Length >= pdfFi.Length)
            {
                File.Move(tempOutFilePath, pdfFilePath, true);
            }
        }


        /// <summary>
        /// Return a dictionary of XMP metadata for all the messages in the EAXS file.
        /// The key is start and end named destinations for the message and the value is the XMP metadata for that message.
        /// </summary>
        /// <returns></returns>
        private Dictionary<(string start, string end), string> GetXmpMetadataForMessages(string eaxsFilePath)
        {
            Dictionary<(string start, string end), string> ret = new();

            var xmpFilePath = Path.ChangeExtension(eaxsFilePath, ".xmp");

            List<(LogLevel level, string message)> messages = new();
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltXmpFilePath, xmpFilePath, null, ref messages);
            foreach (var (level, message) in messages)
            {
                _logger.Log(level, message);
            }

            if (status == 0)
            {
                XmlDocument xdoc = new();
                xdoc.Load(xmpFilePath);
                var xmlns = new XmlNamespaceManager(xdoc.NameTable);
                xmlns.AddNamespace(EABCC, EABCC_NS);
                var nodes = xdoc.SelectNodes("//eabcc:message", xmlns);
                if (nodes != null)
                {
                    foreach (XmlElement node in nodes)
                    {
                        var start = node.GetAttribute("NamedDestination");
                        var end = node.GetAttribute("NamedDestinationEnd");
                        var meta = node.InnerXml;
                        ret.Add((start, end), meta);
                    }
                }
                else
                {
                    throw new Exception("EAXS transformation to XMP was corrupt; review log details.");
                }
            }
            else
            {
                throw new Exception("EAXS transformation to XMP failed; review log details.");
            }

            //Delete the intermediate FO file
            File.Delete(xmpFilePath);

            return ret;
        }

        private string GetDocumentXmp(string eaxsFilePath)
        {
            //open the eaxs and get values from it
            var xdoc = new XmlDocument();
            xdoc.Load(eaxsFilePath);
            var xmlns = new XmlNamespaceManager(xdoc.NameTable);
            xmlns.AddNamespace(EmailToEaxsProcessor.XM, EmailToEaxsProcessor.XM_NS);

            var globalId = xdoc.SelectSingleNode("/xm:Account/xm:GlobalId", xmlns)?.InnerText;
            var accounts = xdoc.SelectNodes("/xm:Account/xm:EmailAddress", xmlns);
            var folder = xdoc.SelectSingleNode("/xm:Account/xm:Folder/xm:Name", xmlns)?.InnerText;
            var accntStrs = new List<string>();
            if (accounts != null)
                foreach (XmlElement account in accounts)
                {
                    accntStrs.Add(account.InnerText);
                }

            var xmp = File.ReadAllText(Settings.XmpSchemaExtension);

            //Fill in the missing placeholders
            xmp = xmp.Replace("$description$", $"PDF Email Archive for Account '{string.Join(", ", accntStrs)}' for Folder '{folder}'");
            xmp = xmp.Replace("$global-id$", globalId);
            xmp = xmp.Replace("$datetime-string$", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"));
            xmp = xmp.Replace("$producer$", GetType().Namespace);

            return xmp;
        }

    }
}
