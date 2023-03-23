namespace UIUCLibrary.EaPdf.Helpers.Pdf
{
    public class DPartLeafNode : DPartNode
    {
        public DPartLeafNode(string startDest, string endDest)
        {
            StartNamedDestination = startDest;
            EndNamedDestination = endDest;
        }

        public string StartNamedDestination { get; set; }

        public string EndNamedDestination { get; set; }
    }

}
