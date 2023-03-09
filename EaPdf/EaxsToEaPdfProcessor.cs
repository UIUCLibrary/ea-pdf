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
            foreach (var m in messages)
            {
                _logger.Log(m.level, m.message);
            }

            if (status == 0)
            {
                messages.Clear();
                var status2 = _xslfo.Transform(foFilePath, pdfFilePath, ref messages);
                foreach (var m in messages)
                {
                    _logger.Log(m.level, m.message);
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
            AddXmpToPage(eaxsFilePath, pdfFilePath);
        }


        private void AddXmpToPage(string eaxsFilePath, string pdfFilePath)
        {
            var outFilePath = Path.ChangeExtension(pdfFilePath, "out.pdf");

            var pageXmps = GetXmpMetadataForMessages(eaxsFilePath);
            var docXmp = GetDocumentXmp(eaxsFilePath);

            using var enhancer = _enhancerFactory.Create(_logger, pdfFilePath, outFilePath);

            enhancer.SetDocumentXmp(docXmp);
            enhancer.AddXmpToPages(pageXmps);


        }


        /// <summary>
        /// Return a dictionary of XMP metadata for all the messages in the EAXS file.
        /// The key is the message LocalId and the value is the XMP metadata for that message.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetXmpMetadataForMessages(string eaxsFilePath)
        {
            Dictionary<string, string> ret = new();

            var xmpFilePath = Path.ChangeExtension(eaxsFilePath, ".xmp");

            List<(LogLevel level, string message)> messages = new();
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltXmpFilePath, xmpFilePath, null, ref messages);
            foreach (var m in messages)
            {
                _logger.Log(m.level, m.message);
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
                        var id = node.GetAttribute("NamedDestination");
                        var meta = node.InnerXml;
                        ret.Add(id, meta);
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
            var xmp = File.ReadAllText(Settings.XmpSchemaExtension);

            //Fill in the missing placeholders
            xmp = xmp.Replace("$description$", "PDF Email Archive for Account 'ACCOUNT' for Folder 'FOLDER'");
            xmp = xmp.Replace("$global-id$", "GLOBAL_ID");
            xmp = xmp.Replace("$datetime-string$", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"));
            xmp = xmp.Replace("$producer$", GetType().Namespace);

            return xmp;
        }

    }
}
