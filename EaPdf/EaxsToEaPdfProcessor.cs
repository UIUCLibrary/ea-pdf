using Microsoft.Extensions.Logging;
using System.Xml;
using UIUCLibrary.EaPdf.Helpers;
using UIUCLibrary.EaPdf.Helpers.Pdf;

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
            _logger.LogTrace("{typeName} Created", this.GetType().Name);

        }

        public void ConvertEaxsToPdf(string eaxsFilePath, string pdfFilePath)
        {
            var foFilePath = Path.ChangeExtension(eaxsFilePath, ".fo");

            var eaxsHelpers = new EaxsHelpers(eaxsFilePath);
            //get fonts based on the Unicode scripts used in the text in the EAXS file and the font settings
            var (serifFonts, sansFonts, monoFonts, complex) = eaxsHelpers.GetBaseFontsToUse(Settings);

            var xsltParams = new Dictionary<string, object>
            {
                { "fo-processor-version", _xslfo.ProcessorVersion },
                { "SerifFont", serifFonts },
                { "SansSerifFont", sansFonts },
                { "MonospaceFont", monoFonts }
            };

            List<(LogLevel level, string message)> messages = new();

            //first transform the EAXS to FO using XSLT
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltFoFilePath, foFilePath, xsltParams, null, ref messages);
            foreach (var (level, message) in messages)
            {
                _logger.Log(level, "{message}", message);
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

                string extraCmdLineParams = "";
                if(!complex && _xslfo.ProcessorVersion.StartsWith("FOP"))
                {
                    _logger.LogInformation("Disabling '{version}' complex script support; no complex scripts were detected.", _xslfo.ProcessorVersion);
                    extraCmdLineParams = "-nocs";
                }
                else
                {
                    _logger.LogInformation("Enabling '{version}' complex script support; complex scripts were detected.", _xslfo.ProcessorVersion);
                }

                var status2 = _xslfo.Transform(foFilePath, pdfFilePath, extraCmdLineParams, ref messages);
                foreach (var (level, message) in messages)
                {
                    _logger.Log(level, "{message}", message);
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

            //Do some post processing to add metadata, cleanup attachments, etc.
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

            enhancer.RemoveUnnecessaryElements();

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
                    XmlNodeList? dates = folderPropNode.ParentNode?.SelectNodes(".//xm:Message/xm:OrigDate", xmlns);
                    XmlNodeList? parentFolderNames = folderPropNode.SelectNodes("ancestor::xm:Folder/xm:Name", xmlns);
                    List<string> names = new();
                    if (parentFolderNames != null)
                    {
                        foreach (XmlNode parentFolderName in parentFolderNames)
                        {
                            names.Add(parentFolderName.InnerText);
                        }
                    }
                    AddSourceFile(ret, folderPropNode, dates, names, true, xmlns);
                }
            }

            if (msgPropNodes != null)
            {
                foreach (XmlNode msgPropNode in msgPropNodes)
                {
                    XmlNodeList? dates = msgPropNode.ParentNode?.SelectNodes("xm:OrigDate", xmlns);
                    XmlNodeList? parentFolderNames = msgPropNode.SelectNodes("ancestor::xm:Folder/xm:Name", xmlns);
                    List<string> names = new();
                    if (parentFolderNames != null)
                    {
                        foreach (XmlNode parentFolderName in parentFolderNames)
                        {
                            names.Add(parentFolderName.InnerText);
                        }
                    }
                    names.Add(msgPropNode.SelectSingleNode("../xm:MessageId", xmlns)?.InnerText ?? "");
                    AddSourceFile(ret, msgPropNode, dates, names, false, xmlns);
                }
            }

            var bodyContentNodes = xdoc.SelectNodes("//xm:SingleBody/xm:BodyContent[translate(normalize-space(../@IsAttachment),'TRUE','true') = 'true' or not(starts-with(translate(normalize-space(../xm:ContentType),'TEX','tex'),'text/'))]", xmlns);
            var extBodyContentNodes = xdoc.SelectNodes("//xm:SingleBody/xm:ExtBodyContent", xmlns);

            if (bodyContentNodes != null)
            {
                foreach (XmlNode inlineAttachmentNode in bodyContentNodes)
                {
                    AddAttachmentFile(ret, inlineAttachmentNode, xmlns);
                }
            }

            if (extBodyContentNodes != null)
            {
                foreach (XmlNode extAttachmentNode in extBodyContentNodes)
                {
                    AddAttachmentFile(ret, extAttachmentNode, xmlns);
                }
            }


            //get the attachment files

            return ret;
        }

        private static (string filename, string hash, long size) GetAttachmentFilenameHashSize(XmlNode extAttachmentNode, XmlNamespaceManager xmlns)
        {
            string fileName = extAttachmentNode.SelectSingleNode("(ancestor::*/xm:DispositionFileName | ancestor::*/xm:ContentName)[last()]", xmlns)?.InnerText ?? "";


            string hash = extAttachmentNode.SelectSingleNode("xm:Hash/xm:Value", xmlns)?.InnerText ?? "";
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = Path.ChangeExtension(hash, GetFileExtension(extAttachmentNode.ParentNode, xmlns));
            }
            else
            {
                fileName = Path.ChangeExtension(fileName, GetFileExtension(extAttachmentNode.ParentNode, xmlns));
            }
            long size = long.Parse(extAttachmentNode.SelectSingleNode("xm:Size", xmlns)?.InnerText ?? "-1");
            return (fileName, hash, size);
        }

        private static void AddAttachmentFile(List<EmbeddedFile> ret, XmlNode extAttachmentNode, XmlNamespaceManager xmlns)
        {
            (DateTime earliest, DateTime latest) = GetEarliestLatestMessageDates(extAttachmentNode.SelectNodes($"ancestor::xm:Folder//xm:OrigDate", xmlns));
            (string fileName, string hash, long size) = GetAttachmentFilenameHashSize(extAttachmentNode, xmlns);


            if (!DateTime.TryParse(extAttachmentNode.SelectSingleNode($"ancestor::*/xm:DispositionParam[translate(normalize-space(xm:Name),'{XmlHelpers.UPPER}','{XmlHelpers.LOWER}') = 'creation-date']/xm:Value", xmlns)?.InnerText, out DateTime creDate))
            {
                if (!DateTime.TryParse(extAttachmentNode.SelectSingleNode($"ancestor::*/xm:OrigDate", xmlns)?.InnerText, out creDate))
                {
                    creDate = earliest;
                }
            }
            if (!DateTime.TryParse(extAttachmentNode.SelectSingleNode($"ancestor::*/xm:DispositionParam[translate(normalize-space(xm:Name),'{XmlHelpers.UPPER}','{XmlHelpers.LOWER}') = 'modification-date']/xm:Value", xmlns)?.InnerText, out DateTime modDate))
            {
                if (!DateTime.TryParse(extAttachmentNode.SelectSingleNode($"ancestor::*/xm:OrigDate", xmlns)?.InnerText, out modDate))
                {
                    modDate = latest;
                }
            }



            var mime = extAttachmentNode.ParentNode?.SelectSingleNode("xm:ContentType", xmlns)?.InnerText ?? "";

            ret.Add(new EmbeddedFile()
            {
                OriginalFileName = fileName,
                Relationship = EmbeddedFile.AFRelationship.Supplement,
                Subtype = mime,
                Size = size,
                Hash = hash,
                CreationDate = creDate,
                ModDate = modDate,
                UniqueName = Path.ChangeExtension(hash, GetFileExtension(extAttachmentNode.ParentNode, xmlns)),
                Description = GetAttachmentDescription(extAttachmentNode, xmlns)
            });

        }

        private static string GetAttachmentDescription(XmlNode extAttachmentNode, XmlNamespaceManager xmlns)
        {
            //if the mime header has its own description, use that
            string descr = extAttachmentNode.SelectSingleNode("../xm:Description", xmlns)?.InnerText ?? "";

            string msgId = extAttachmentNode.SelectSingleNode("ancestor::xm:Message/xm:MessageId", xmlns)?.InnerText ?? "[Missing Value]";
            var froms = extAttachmentNode.SelectNodes("ancestor::xm:Message/xm:From/xm:Mailbox", xmlns);


            string from = "[Missing Value]";
            if (froms != null && froms.Count > 0)
            {
                from = froms[0]?.InnerText ?? "";
                if (froms.Count > 1)
                {
                    from += " (and others)";
                }
            }
            var tos = extAttachmentNode.SelectNodes("ancestor::xm:Message/xm:To/xm:Mailbox", xmlns);
            string to = "[Missing Value]";
            if (tos != null && tos.Count > 0)
            {
                to = tos[0]?.InnerText ?? "";
                if (tos.Count > 1)
                {
                    to += " (and others)";
                }
            }
            string subj = extAttachmentNode.SelectSingleNode("ancestor::xm:Message/xm:Subject", xmlns)?.InnerText ?? "[Missing Value]";
            string ret = $"Attachment from Message ID '{msgId}' From {from} To {to} Subject '{subj}'";

            if (!string.IsNullOrWhiteSpace(descr))
            {
                ret += $"\r\n\r\nDescription: {descr}";
            }

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="singleBody"></param>
        /// <param name="xmlns"></param>
        /// <returns></returns>
        private static string GetFileExtension(XmlNode? singleBody, XmlNamespaceManager xmlns)
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
                return Path.GetExtension(dispositionFileName);
            }
            else if (contentName.Contains('.'))
            {
                return Path.GetExtension(contentName);
            }
            else
            {
                return MimeTypeMap.GetExtension(contentType);
            }
        }

        private void AddSourceFile(List<EmbeddedFile> ret, XmlNode propNode, XmlNodeList? origDates, List<string> sourceNames, bool folder, XmlNamespaceManager xmlns)
        {
            var relPath = propNode.SelectSingleNode("xm:RelPath", xmlns)?.InnerText ?? "";
            var fileExt = propNode.SelectSingleNode("xm:FileExt", xmlns)?.InnerText ?? "";

            if (!string.IsNullOrWhiteSpace(fileExt))
            {
                relPath = Path.ChangeExtension(relPath, fileExt);
            }

            var mime = propNode.SelectSingleNode("xm:ContentType", xmlns)?.InnerText ?? "";
            var (earliest, latest) = GetEarliestLatestMessageDates(origDates);
            //get file dates
            if (!DateTime.TryParse(propNode.SelectSingleNode("xm:Created", xmlns)?.InnerText, out DateTime fileCreDate))
            {
                throw new Exception($"File creation date is missing for file '{relPath}'.");
            }
            if (!DateTime.TryParse(propNode.SelectSingleNode("xm:Modified", xmlns)?.InnerText, out DateTime fileModDate))
            {
                throw new Exception($"File modification date is missing for file '{relPath}'.");
            }

            //fileModDate can be earlier than fileCreDate, for example if file has been copied, so swap if this is the case
            if (fileModDate < fileCreDate)
            {
                _logger.LogWarning("File modification date '{fileModDate}' is earlier than file creation date '{fileCreDate}' for file '{relPath}'.\r\nThis sometimes results when the file has been copied or moved to a new medium.", fileModDate, fileCreDate, relPath);
            }

            //if there are no file dates, use the message dates
            if (fileCreDate.Equals(DateTime.MinValue))
            {
                _logger.LogWarning("File creation date is missing for file '{relPath}'.\r\nUsing the earliest message date '{earliest}'.", relPath, earliest);
                fileCreDate = earliest;
            }
            if (fileModDate.Equals(DateTime.MinValue))
            {
                _logger.LogWarning("File modification date is missing for file '{relPath}'.\r\nUsing the latest message date '{latest}'.", relPath, latest);
                fileModDate = latest;
            }

            var hash = propNode.SelectSingleNode("xm:Hash/xm:Value", xmlns)?.InnerText ?? "";

            ret.Add(new EmbeddedFile()
            {
                OriginalFileName = relPath,
                Relationship = EmbeddedFile.AFRelationship.Source,
                Subtype = mime,
                Size = long.Parse(propNode.SelectSingleNode("xm:Size", xmlns)?.InnerText ?? "-1"),
                Hash = hash,
                CreationDate = fileCreDate,
                ModDate = fileModDate,
                UniqueName = Path.ChangeExtension(hash, MimeTypeMap.GetExtension(mime)),
                Description = "Source file for " + (folder ? "folder: " : "message: ") + string.Join(" -> ", sourceNames)
            }); ;

        }


        private static (DateTime earliest, DateTime latest) GetEarliestLatestMessageDates(XmlNodeList? origDates)
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
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltXmpFilePath, xmpFilePath, null, null, ref messages);
            foreach (var (level, message) in messages)
            {
                _logger.Log(level, "{message}", message);
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

            var foProc = _xslfo.ProcessorVersion;
            var pdfaConfLvl = "A"; //PDF/A-3 A, B, or U
            if (foProc.StartsWith("FOP", StringComparison.OrdinalIgnoreCase))
            {
                pdfaConfLvl = "A";
            }

            Dictionary<string, object> parms = new()
            {
                { "producer", GetType().Namespace ?? "UIUCLibrary" },
                {"pdf_a_conf_level", pdfaConfLvl }
            };


            var xmpFilePath = Path.ChangeExtension(eaxsFilePath, ".xmp");

            List<(LogLevel level, string message)> messages = new();
            var status = _xslt.Transform(eaxsFilePath, Settings.XsltRootXmpFilePath, xmpFilePath, parms, null, ref messages);
            foreach (var (level, message) in messages)
            {
                _logger.Log(level, "{message}", message);
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
