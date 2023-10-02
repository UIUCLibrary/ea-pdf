using AngleSharp.Dom;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Pkcs;
using System.Xml;
using System.Xml.Schema;
using UIUCLibrary.EaPdf.Helpers;
using UIUCLibrary.EaPdf.Helpers.Pdf;
using static System.Net.Mime.MediaTypeNames;

namespace UIUCLibrary.EaPdf
{
    public class EaxsToEaPdfProcessor
    {

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

            var eaxsHelpers = new EaxsHelpers(eaxsFilePath);
            //get fonts based on the Unicode scripts used in the text in the EAXS file and the font settings
            var defaultFonts = eaxsHelpers.GetBaseFontsToUse(Settings);

            var xsltParams = new Dictionary<string, object>
            {
                { "fo-processor-version", _xslfo.ProcessorVersion },
                { "SerifFont", defaultFonts.serifFonts },
                { "SansSerifFont", defaultFonts.sansFonts },
                { "MonospaceFont", defaultFonts.monoFonts }
            };

            List<(LogLevel level, string message)> messages = new();

            //first transform the EAXS to FO using XSLT
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltFoFilePath, foFilePath, xsltParams, ref messages);
            foreach (var (level, message) in messages)
            {
                _logger.Log(level, message);
            }

            //if the first transform was successful, transform the FO to PDF using one of the XSL-FO processors
            if (status == 0)
            {
                messages.Clear();

                //do some post processing on the FO file to prevent ligatures and wrap text in font-family
                var foHelper = new XslFoHelpers(foFilePath);
                foHelper.PreventLigatures();
                foHelper.WrapLanguagesInFontFamily(Settings);
                foHelper.SaveFoFile();

                var status2 = _xslfo.Transform(foFilePath, pdfFilePath, ref messages);
                foreach (var (level, message) in messages)
                {
                    _logger.Log(level, message);
                }
                if (status2 != 0)
                {
                    throw new Exception($"FO transformation to PDF failed, status-{status2}; review log details.");
                }
            }
            else
            {
                throw new Exception($"EAXS transformation to FO failed, status-{status}; review log details.");
            }

#if !DEBUG
            //Delete the intermediate FO file
            File.Delete(foFilePath);
#endif

#if DEBUG
            //save intermediate version of the PDF before post processing
            File.Copy(pdfFilePath, Path.ChangeExtension(pdfFilePath, "pre.pdf"), true);
#endif

            //Do some post processing to add metadata
            PostProcessPdf(eaxsFilePath, pdfFilePath);
        }


        private void PostProcessPdf(string eaxsFilePath, string pdfFilePath)
        {
            var tempOutFilePath = Path.ChangeExtension(pdfFilePath, "out.pdf");

            var dparts = GetXmpMetadataForMessages(eaxsFilePath);
            var docXmp = GetRootXmpForAccount(eaxsFilePath);

            //add docXmp to the DPart root node
            dparts.DpmXmpString = docXmp;

            //get list of all embedded files in the PDF
            var embeddedFiles = GetEmbeddedFiles(eaxsFilePath);

            using var enhancer = _enhancerFactory.Create(_logger, pdfFilePath, tempOutFilePath);

            enhancer.AddXmpToDParts(dparts); //Associate XMP with the PDF DPart of the message

            enhancer.NormalizeAttachments(embeddedFiles);

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

        private List<EmbeddedFile> GetEmbeddedFiles(string eaxsFilePath)
        {
            List<EmbeddedFile> ret = new();

            XmlDocument xdoc = new();
            xdoc.Load(eaxsFilePath);

            var xmlns = new XmlNamespaceManager(xdoc.NameTable);
            xmlns.AddNamespace(EmailToEaxsProcessor.XM, EmailToEaxsProcessor.XM_NS);

            //get the source files
            var folderPropNodes = xdoc.SelectNodes("//xm:Folder[xm:Message]/xm:FolderProperties[xm:RelPath]", xmlns);
            var msgPropNodes = xdoc.SelectNodes("//xm:Folder/xm:Message/xm:MessageProperties[xm:RelPath]", xmlns);

            if (folderPropNodes != null)
            {
                foreach (XmlNode folderPropNode in folderPropNodes)
                {
                    AddEmbeddedFile(ret, EmbeddedFile.AFRelationship.Source, folderPropNode, folderPropNode.ParentNode?.SelectNodes("xm:Message/xm:OrigDate", xmlns), xmlns);
                }
            }

            if (msgPropNodes != null)
            {
                foreach (XmlNode msgPropNode in msgPropNodes)
                {
                    AddEmbeddedFile(ret, EmbeddedFile.AFRelationship.Source, msgPropNode, msgPropNode.ParentNode?.SelectNodes("xm:OrigDate", xmlns), xmlns);
                }
            }

            var inlineAttachmentNodes = xdoc.SelectNodes("//xm:SingleBody/xm:BodyContent[translate(normalize-space(../@IsAttachment),'TRUE','true') = 'true' or not(starts-with(translate(normalize-space(../xm:ContentType),'TEX','tex'),'text/'))]", xmlns);
            var extAttachmentNodes = xdoc.SelectNodes("//xm:SingleBody/xm:ExtBodyContent", xmlns);

            if (inlineAttachmentNodes != null)
            {
                foreach (XmlNode inlineAttachmentNode in inlineAttachmentNodes)
                {
                }
            }

            if (extAttachmentNodes != null)
            {
                foreach (XmlNode extAttachmentNode in extAttachmentNodes)
                {
                    string fileName = extAttachmentNode.SelectSingleNode("../xm:DispositionFileName | ../xm:ContentName", xmlns)?.InnerText ?? "";
                    string hash = extAttachmentNode.SelectSingleNode("xm:Hash/xm:Value", xmlns)?.InnerText ?? "";
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        fileName = Path.ChangeExtension(hash, GetFileExtension(extAttachmentNode.ParentNode, xmlns));
                    }
                    long size = long.Parse(extAttachmentNode.SelectSingleNode("xm:Size", xmlns)?.InnerText ?? "-1");

                    var creDate = extAttachmentNode.ParentNode?.SelectSingleNode("xm:DispositionParam[normalize-space(xm:Name) = 'creation-date']", xmlns)?.InnerText;
                    var modDate = extAttachmentNode.ParentNode?.SelectSingleNode("xm:DispositionParam[xm:Name = 'modification-date']", xmlns)?.InnerText;

                }
            }


            //get the attachment files

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="singleBody"></param>
        /// <param name="xmlns"></param>
        /// <returns></returns>
        private string GetFileExtension(XmlNode? singleBody, XmlNamespaceManager xmlns)
        {
            if (singleBody == null)
            {
                throw new ArgumentNullException(nameof(singleBody));
            }
            var contentType = singleBody.SelectSingleNode("xm:ContentType", xmlns)?.InnerText.Trim() ?? "";
            var contentName = singleBody.SelectSingleNode("xm:ContentName", xmlns)?.InnerText.Trim() ?? "";
            var dispositionFileName = singleBody.SelectSingleNode("xm:DispositionFileName", xmlns)?.InnerText.Trim() ?? "";

            if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return "pdf";
            }
            else if (contentType.Equals("text/rtf", StringComparison.OrdinalIgnoreCase))
            {
                return "rtf";
            }
            else if (contentType.Equals("application/rtf", StringComparison.OrdinalIgnoreCase))
            {
                return "rtf";
            }
            else if (dispositionFileName.Contains('.'))
            {
                return dispositionFileName.Substring(dispositionFileName.LastIndexOf('.') + 1);
            }
            else if (contentName.Contains('.'))
            {
                return contentName.Substring(contentName.LastIndexOf('.') + 1);
            }
            else
            {
                return MimeTypeMap.GetExtension(contentType).Substring(1); //skip the leading dot
            }
        }

        private void AddEmbeddedFile(List<EmbeddedFile> ret, EmbeddedFile.AFRelationship relat, XmlNode propNode, XmlNodeList? origDates, XmlNamespaceManager xmlns)
        {
            var relPath = propNode.SelectSingleNode("xm:RelPath", xmlns)?.InnerText ?? "";
            var mime = propNode.SelectSingleNode("xm:ContentType", xmlns)?.InnerText ?? "";
            var (earliest, latest) = GetEarliestLatestMessageDates(origDates);
            //get file dates
            DateTime.TryParse(propNode.SelectSingleNode("xm:Created", xmlns)?.InnerText, out var fileCreDate);
            DateTime.TryParse(propNode.SelectSingleNode("xm:Modified", xmlns)?.InnerText, out var fileModDate);

            //fileModDate can be earlier than fileCreDate, for example if file has been copied, so swap if this is the case
            if (fileModDate < fileCreDate)
            {
                _logger.LogWarning($"File modification date '{fileModDate}' is earlier than file creation date '{fileCreDate}' for file '{relPath}'.\r\nThis sometimes results when the file has been copied or moved to a new medium.");
            }

            //if there are no file dates, use the message dates
            if (fileCreDate.Equals(DateTime.MinValue))
            {
                _logger.LogWarning($"File creation date is missing for file '{relPath}'.\r\nUsing the earliest message date '{earliest}'.");
                fileCreDate = earliest;
            }
            if (fileModDate.Equals(DateTime.MinValue))
            {
                _logger.LogWarning($"File modification date is missing for file '{relPath}'.\r\nUsing the latest message date '{latest}'.");
                fileModDate = latest;
            }

            ret.Add(new EmbeddedFile()
            {
                OriginalFileName = relPath,
                Relationship = relat,
                Subtype = mime,
                Size = long.Parse(propNode.SelectSingleNode("xm:Size", xmlns)?.InnerText ?? "-1"),
                Hash = propNode.SelectSingleNode("xm:Hash/xm:Value", xmlns)?.InnerText ?? "",
                CreationDate = fileCreDate,
                ModDate = fileModDate

            });
        }


        private (DateTime earliest, DateTime latest) GetEarliestLatestMessageDates(XmlNodeList? origDates)
        {
            if (origDates != null && origDates.Count > 0)
            {
                var dates = origDates.Cast<XmlNode>().Select(n => DateTime.Parse(n.InnerText));
                return (dates.Min(), dates.Max());
            }
            else
            {
                var n = DateTime.Now;
                return (n, n);
            }
        }



        /// <summary>
        /// Return the root DPart node for all the folders and messages in the EAXS file.
        /// </summary>
        /// <returns></returns>
        private DPartInternalNode GetXmpMetadataForMessages(string eaxsFilePath)
        {
            DPartInternalNode ret = new();

            var xmpFilePath = Path.ChangeExtension(eaxsFilePath, ".xmp");

            List<(LogLevel level, string message)> messages = new();
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltXmpFilePath, xmpFilePath, null, ref messages);
            foreach (var (level, message) in messages)
            {
                _logger.Log(level, message);
            }

            if (status == 0)
            {
                ret.DParts.Add(DPartNode.Create(ret, xmpFilePath));
            }
            else
            {
                throw new Exception("EAXS transformation to XMP failed; review log details.");
            }

#if !DEBUG
            //Delete the intermediate XMP file
            File.Delete(xmpFilePath);
#endif

            return ret;
        }

        private string GetRootXmpForAccount(string eaxsFilePath)
        {
            string ret;

            Dictionary<string, object> parms = new();

            parms.Add("producer", GetType().Namespace ?? "UIUCLibrary");


            var xmpFilePath = Path.ChangeExtension(eaxsFilePath, ".xmp");

            List<(LogLevel level, string message)> messages = new();
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltRootXmpFilePath, xmpFilePath, parms, ref messages);
            foreach (var (level, message) in messages)
            {
                _logger.Log(level, message);
            }

            if (status == 0)
            {
                ret = File.ReadAllText(xmpFilePath);
            }
            else
            {
                throw new Exception("EAXS transformation to XMP failed; review log details.");
            }

#if !DEBUG
            //Delete the intermediate XMP file
            File.Delete(xmpFilePath);
#endif

            return ret;
        }

    }
}
