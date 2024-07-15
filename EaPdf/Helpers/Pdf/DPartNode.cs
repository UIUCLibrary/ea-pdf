using System.Diagnostics;
using System.Text;
using System.Xml;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    /// <summary>
    /// Represents a DPart tree derived from the EAXS XML files
    /// </summary>
    public class DPartNode
    {
        public const string XmpRootPath = "/x:xmpmeta/rdf:RDF/rdf:Description[1]"; //useful in XMP XPath queries; the first rdf:Description contains the document metadata

        const int MAX_DEPTH = 100;

        /// <summary>
        /// In DPart leaf nodes, this should match an internal-destination id of a content set in the EA-PDF
        /// </summary>
        public string? Id { get; set; }


        /// <summary>
        /// Dictionary corresponding to the DPart DPM metadata PDF Dictionary
        /// </summary>
        public Dictionary<string, string> Dpm { get; set; } = new();

        public string MessageId         {
            get
            {
                return Dpm.GetValueOrDefault("Mail_MessageID") ?? string.Empty;
            }
        }

        /// <summary>
        /// List of the checksums of the attachments for the dpart node (if any), usually only at the message level
        /// Entries correspond to the AttachmentNames list
        /// </summary>
        public List<string> AttachmentChecksums { get; set; } = new();

        /// <summary>
        /// Metadata of the DPart
        /// </summary>
        public XmlDocument? MetadataXml { get; set; }

        /// <summary>
        /// Null if this is the root node
        /// </summary>
        public DPartNode? Parent { get; set; } = null;

        /// <summary>
        /// Get the depth of the node in the DPart tree, useful for debugging
        /// </summary>
        public int Depth
        {
            get
            {
                int ret = 0;
                DPartNode? node = this;
                while (node.Parent != null)
                {
                    ret++;
                    if(ret> MAX_DEPTH)
                    {
                        throw new Exception($"Maximum recursion depth ({MAX_DEPTH}) exceeded in DPartNode.Depth");
                    }
                    node = node.Parent;
                }
                return ret;
            }
        }

        /// <summary>
        /// List will be empty if this is a leaf node
        /// </summary>
        public List<DPartNode> DParts { get; set; } = new();

        public override string? ToString()
        {
            if (!string.IsNullOrEmpty(Id))
            {
                return $"{base.ToString()} (Id: {Id})";
            }
            else
            {
                return base.ToString();
            }
        }

        /// <summary>
        /// Returns the root node of the DPart tree
        /// </summary>
        public DPartNode RootNode
        {
            get
            {
                int depth = 0;
                DPartNode ret = this;
                while (ret.Parent != null)
                {
                    depth++;
                    if (depth > MAX_DEPTH)
                    {
                        throw new Exception($"Maximum recursion depth ({MAX_DEPTH}) exceeded in DPartNode.RootNode");
                    }
                    ret = ret.Parent;
                }
                return ret;
            }
        }

        /// <summary>
        /// Returns a breadth-first ordered list of all leaf nodes starting at this node.  If this node is a leaf node, it will return a list with only this node.
        /// Has a depth parameter to keep track of the depth of the recursion.
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        private List<DPartNode> GetAllLeafNodes(int depth = 0)
        {
            if (depth > MAX_DEPTH)
            {
                throw new Exception($"Maximum recursion depth ({MAX_DEPTH}) exceeded in DPartNode.GetAllLeafNodes()");
            }

            List<DPartNode> ret = new();
            if (DParts.Count == 0)
            {
                ret.Add(this);
            }
            else
            {
                depth++;
                foreach (var child in DParts)
                {
                    ret.AddRange(child.GetAllLeafNodes(depth));
                }
            }
            return ret;
        }

        /// <summary>
        /// Returns a breadth-first ordered list of all leaf nodes starting at this node.  If this node is a leaf node, it will return a list with only this node.
        /// </summary>
        public List<DPartNode> AllLeafNodes
        {
            get
            {
                return GetAllLeafNodes();
            }
        }


        /// <summary>
        /// Returns the first leaf node in the breadth-first order.  If this node is a leaf node, it will return itself.
        /// </summary>
        public DPartNode FirstLeafNode
        {
            get
            {
                return AllLeafNodes.First();
            }
        }

        /// <summary>
        /// Returns the next leaf node in the breadth-first order, or null if this is the last leaf node.
        /// If this node is not itself a leaf node, it will return the first leaf node in its subtree.
        /// </summary>
        /// <returns></returns>
        public DPartNode? NextLeafNode
        {
            get
            {
                if (DParts.Count > 0)
                {
                    return FirstLeafNode;
                }
                var leaves = RootNode.AllLeafNodes;
                var idx = leaves.IndexOf(this) + 1;
                if (idx < leaves.Count)
                {
                    return leaves[idx];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get a string representing the XMP metadata; used in log and error messages
        /// </summary>
        /// <returns></returns>
        public string? GetFirstDpmXmpElement()
        {
            string ret = MetadataXml?.SelectSingleNode("/*/*/*/*")?.OuterXml ?? "";
            return ret;
        }

        /// <summary>
        /// Get or set the XMP metadata as a string
        /// </summary>
        public string MetadataString
        {
            get
            {
                return MetadataXml?.OuterXml ?? "";
            }
            private set
            {
                MetadataXml = new XmlDocument();
                MetadataXml.PreserveWhitespace = true;
                MetadataXml.LoadXml(value);
            }
        }

        /// <summary>
        /// Get an XmlNamespaceManager with the namespaces commonly used in the XMP metadata
        /// </summary>
        public XmlNamespaceManager MetadataNamespaces
        {
            get
            {
                var ret = new XmlNamespaceManager(new NameTable());
                ret.AddNamespace("x", "adobe:ns:meta/");
                ret.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                ret.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
                ret.AddNamespace("dcterms", "http://purl.org/dc/terms/");
                ret.AddNamespace("foaf", "http://xmlns.com/foaf/0.1/");
                ret.AddNamespace("pdf", "http://ns.adobe.com/pdf/1.3/");
                ret.AddNamespace("pdfaid", "http://www.aiim.org/pdfa/ns/id/");
                ret.AddNamespace("pdfmail", "http://www.pdfa.org/eapdf/");
                ret.AddNamespace("pdfmailid", "http://www.pdfa.org/eapdf/ns/id/");
                ret.AddNamespace("pdfmailmeta", "http://www.pdfa.org/eapdf/ns/meta/");
                ret.AddNamespace("pdfx", "http://ns.adobe.com/pdfx/1.3/");
                ret.AddNamespace("xmp", "http://ns.adobe.com/xap/1.0/");

                return ret;
            }
        }

        /// <summary>
        /// Update the value of an element node in the XMP metadata
        /// </summary>
        /// <param name="xpath"></param>
        /// <param name="newVal"></param>
        /// <exception cref="Exception"></exception>
        public void UpdateElementNodeText(string xpath, string newVal)
        {
            if (MetadataXml == null || MetadataNamespaces == null)
            {
                throw new Exception("The XMP is empty or not valid");
            }

            var node = MetadataXml.SelectSingleNode(xpath, MetadataNamespaces);
            if (node != null && node.NodeType == XmlNodeType.Element && !node.ChildNodes.OfType<XmlElement>().Any())
            {
                node.InnerText = newVal;
                var buffer = new StringBuilder();
                var writer = XmlWriter.Create(buffer, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true });
                MetadataXml.Save(writer);
                writer.Close();
                MetadataString = buffer.ToString();
            }
            else
            {
                throw new Exception($"The xpath '{xpath}' was not found in the XMP, or it returned a node which is not an element with only text.");
            }
        }

        /// <summary>
        /// Create a new DPart hierarchy from an XML string
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="xmlString"></param>
        /// <returns>The root DPartNode</returns>
        public static DPartNode CreateFromXmlString(DPartNode parent, string xmlString)
        {
            XmlDocument xdoc = new();
            xdoc.PreserveWhitespace = true;

            xdoc.LoadXml(xmlString);

            return DPartNode.CreateFromXmlDocument(parent, xdoc);
        }

        /// <summary>
        /// Create a new DPart hierarchy from an XML file
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="xmlFilePath"></param>
        /// <returns>The root DPartNode</returns>
        public static DPartNode CreateFromXmlFile(DPartNode parent, string xmlFilePath)
        {
            XmlDocument xdoc = new();
            xdoc.PreserveWhitespace = true;
            xdoc.Load(xmlFilePath);

            return DPartNode.CreateFromXmlDocument(parent, xdoc);
        }

        /// <summary>
        /// Create a new DPart hierarchy from an XMLDocument object
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="xdoc"></param>
        /// <returns>The root DPartNode</returns>
        public static DPartNode CreateFromXmlDocument(DPartNode parent, XmlDocument xdoc)
        {

            if (xdoc.SelectSingleNode("/DPart") is XmlElement xmlDPartNode)
            {
                var dpartNode = ProcessDPart(parent, xmlDPartNode);
                return parent ?? dpartNode;
            }

            return parent;
        }

        /// <summary>
        /// Recursively process a DPart XML element and its children, converting them to DPartNode objects
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="dPartElem"></param>
        /// <returns></returns>
        private static DPartNode ProcessDPart(DPartNode? parentNode, XmlElement dPartElem)
        {
            DPartNode newNode = new()
            {
                Parent = parentNode
            };
            parentNode?.DParts.Add(newNode);

            newNode.Id = dPartElem.Attributes["Id"]?.Value;

            var attachmentCheckSums = dPartElem.GetAttribute("AttachmentCheckSums");
            if(!string.IsNullOrWhiteSpace(attachmentCheckSums))
            {
                newNode.AttachmentChecksums.AddRange(attachmentCheckSums.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            foreach (var DpmAttr in dPartElem.Attributes.Cast<XmlAttribute>().Where(a => a.LocalName.StartsWith("DPM_")))
            {
                var name = DpmAttr.LocalName[4..];
                newNode.Dpm.Add(name, DpmAttr.Value);
            }

            if (dPartElem.SelectSingleNode("metadata") is XmlElement metadata)
            {
                newNode.MetadataXml = new XmlDocument();
                newNode.MetadataXml.PreserveWhitespace = true;
                newNode.MetadataXml.LoadXml(metadata.OuterXml);
            }

            var dPartChildren = dPartElem.SelectNodes("DPart");
            if (dPartChildren != null)
            {
                foreach (XmlElement dPartChildElem in dPartChildren)
                {
                    ProcessDPart(newNode, dPartChildElem);
                }
            }

            return newNode;
        }


    }
}

