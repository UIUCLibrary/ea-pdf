using SkiaSharp;
using System.Xml;

namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    //Classes for creating the DPart tree from the EAXS XML files

    public abstract class DPartNode
    {
        public string DpmXmpString { get; set; } = "";

        public XmlDocument? DpmXmpXml 
        {
            get
            {
                if (string.IsNullOrEmpty(DpmXmpString))
                {
                    return null;
                }

                XmlDocument ret = new();
                ret.LoadXml(DpmXmpString);
                return ret;
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
            DPartInternalNode ret = new(); //this is the root node that refers to the first-level folders of the account
            ret.Parent = parent;

            XmlDocument xdoc = new();
            xdoc.PreserveWhitespace = true;
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
