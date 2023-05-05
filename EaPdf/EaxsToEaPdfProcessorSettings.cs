namespace UIUCLibrary.EaPdf
{
    public  class EaxsToEaPdfProcessorSettings
    {
        public string XsltFoFilePath { get; set; } = "XResources\\eaxs_to_fo.xsl";

        public string XsltXmpFilePath { get; set; } = "XResources\\eaxs_to_xmp.xsl";

        public string XsltRootXmpFilePath { get; set; } = "XResources\\eaxs_to_root_xmp.xsl";

    }
}
