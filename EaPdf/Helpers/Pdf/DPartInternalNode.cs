namespace UIUCLibrary.EaPdf.Helpers.Pdf
{

    public class DPartInternalNode : DPartNode
    {
        public List<DPartNode> DParts { get; set; } = new List<DPartNode>();

        public string? Name { get; set; }

        public List<DPartLeafNode> GetLeafNodeChildren()
        {
            List<DPartLeafNode> ret = new();
            foreach (DPartNode node in DParts)
            {
                if (node is DPartLeafNode leafNode)
                {
                    ret.Add(leafNode);
                }
            }

            return ret;
        }

        public List<DPartInternalNode> GetInternalNodeChildren()
        {
            List<DPartInternalNode> ret = new();
            foreach (DPartNode node in DParts)
            {
                if (node is DPartInternalNode intNode)
                {
                    ret.Add(intNode);
                }
            }

            return ret;
        }


        public List<DPartLeafNode> GetAllLeafNodesAsFlattenedList()
        {
            List<DPartLeafNode> ret = GetLeafNodeChildren();

            foreach (DPartInternalNode node in GetInternalNodeChildren())
            {
                ret.AddRange(node.GetAllLeafNodesAsFlattenedList());
            }

            return ret;
        }
    }

}
