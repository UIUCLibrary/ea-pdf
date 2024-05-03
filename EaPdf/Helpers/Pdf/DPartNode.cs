using Org.BouncyCastle.Asn1.Pkcs;
using SkiaSharp;
using System.Text;
using System.Xml;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    //Classes for creating the DPart tree from the EAXS XML files

    public abstract class DPartNode
    {
        private object locker = new object();

        private string _dpmXmpString = "";
        public string DpmXmpString 
        {
            get 
            {
                return _dpmXmpString; 
            }
            set 
            {
                lock (locker) 
                {
                    _dpmXmpString = value;
                    _xmlDoc = null;
                    _xmlns = null;
                }
            }
        }

        public void UpdateElementNodeText(string xpath, string newVal)
        {
            if(DpmXmpXml == null || XmlNamespaces == null)
            {
                throw new Exception("The XMP is empty or not valid");
            }

            var node = DpmXmpXml.SelectSingleNode(xpath, XmlNamespaces);
            if (node != null && node.NodeType == XmlNodeType.Element && !node.ChildNodes.OfType<XmlElement>().Any())
            {
                node.InnerText = newVal;
                var buffer = new StringBuilder();
                var writer = XmlWriter.Create(buffer, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true });
                DpmXmpXml.Save(writer);
                writer.Close();
                DpmXmpString = buffer.ToString();
            }
            else
            {
                throw new Exception($"The xpath '{xpath}' was not found in the XMP, or it returned a node which is not an element with only text.");
            }
        }

        XmlDocument? _xmlDoc = null;
        XmlNamespaceManager? _xmlns = null;
        public XmlDocument? DpmXmpXml
        {
            get
            {
                if (string.IsNullOrEmpty(DpmXmpString))
                {
                    return null;
                }

                lock (locker)
                {
                    if (_xmlDoc == null)
                    {
                        _xmlDoc = new XmlDocument();
                        _xmlDoc.LoadXml(DpmXmpString);

                        _xmlns = new XmlNamespaceManager(_xmlDoc.NameTable);
                        _xmlns.AddNamespace("x", "adobe:ns:meta/");
                        _xmlns.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                        _xmlns.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
                        _xmlns.AddNamespace("dcterms", "http://purl.org/dc/terms/");
                        _xmlns.AddNamespace("foaf", "http://xmlns.com/foaf/0.1/");
                        _xmlns.AddNamespace("pdf", "http://ns.adobe.com/pdf/1.3/");
                        _xmlns.AddNamespace("pdfaid", "http://www.aiim.org/pdfa/ns/id/");
                        _xmlns.AddNamespace("pdfmail", "http://www.pdfa.org/eapdf/");
                        _xmlns.AddNamespace("pdfmailid", "http://www.pdfa.org/eapdf/ns/id/");
                        _xmlns.AddNamespace("pdfmailmeta", "http://www.pdfa.org/eapdf/ns/meta/");
                        _xmlns.AddNamespace("pdfx", "http://ns.adobe.com/pdfx/1.3/");
                        _xmlns.AddNamespace("xmp", "http://ns.adobe.com/xap/1.0/");
                    }
                    return _xmlDoc;
                }
            }
        }

        public XmlNamespaceManager? XmlNamespaces
        {
            get
            {
                if (DpmXmpXml == null)
                {
                    return null;
                }
                return _xmlns;
            }
        }

        public string? GetFirstDpmXmpElement()
        {
            string ret = DpmXmpXml?.SelectSingleNode("/*/*/*/*")?.OuterXml ?? "";
            return ret;
        }

        public DPartNode? Parent { get; set; } = null;

        public static DPartInternalNode Create(DPartNode parent, string xmlFilePath)
        {
            DPartInternalNode ret = new()
            {
                Parent = parent //this is the root node that refers to the first-level folders of the account
            };

            XmlDocument xdoc = new()
            {
                PreserveWhitespace = true
            };
            xdoc.Load(xmlFilePath);

            var xmlFolderNodes = xdoc.SelectNodes("/root/folder");
            if (xmlFolderNodes != null)
            {
                foreach (XmlElement xmlFolderElem in xmlFolderNodes)
                {
                    ProcessFolder(ret, xmlFolderElem);
                }
            }

            return ret;
        }

        private static void ProcessFolder(DPartInternalNode parentNode, XmlElement xmlFolderElem)
        {
            parentNode.Name = xmlFolderElem.GetAttribute("Name");
            var xmlMsgNodes = xmlFolderElem.SelectNodes("message");
            if (xmlMsgNodes != null)
            {
                foreach (XmlElement xmlMsgElem in xmlMsgNodes)
                {
                    DPartLeafNode msgNode = new(xmlMsgElem.GetAttribute("NamedDestination"), xmlMsgElem.GetAttribute("NamedDestinationEnd"))
                    {
                        DpmXmpString = xmlMsgElem.InnerXml,
                        Parent = parentNode
                    };
                    parentNode.DParts.Add(msgNode);
                }
            }

            var xmlFldrNodes = xmlFolderElem.SelectNodes("folder");
            if (xmlFldrNodes != null)
            {
                foreach (XmlElement xmlFldrElem in xmlFldrNodes)
                {
                    DPartInternalNode fldrNode = new()
                    {
                        Name = xmlFldrElem.GetAttribute("Name"),
                        Parent = parentNode
                    };
                    parentNode.DParts.Add(fldrNode);
                    ProcessFolder(fldrNode, xmlFldrElem);
                }
            }

        }

    }

}
