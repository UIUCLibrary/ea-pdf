using static UIUCLibrary.EaPdf.Helpers.FontHelper;
using System.Text.RegularExpressions;

namespace UIUCLibrary.EaPdf
{
    public  class EaxsToEaPdfProcessorSettings
    {
        public string XsltFoFilePath { get; set; } = "XResources\\eaxs_to_fo.xsl";

        public string XsltXmpFilePath { get; set; } = "XResources\\eaxs_to_xmp.xsl";

        public string XsltRootXmpFilePath { get; set; } = "XResources\\eaxs_to_root_xmp.xsl";

        /// <summary>
        /// The folder where the fonts are located, may contain subfolders of fonts
        /// </summary>
        public string FontsFolder { get; set; } = "Fonts";

        /// <summary>
        /// Regular expressions to match font names to base font styles, serif, sans-serif, and monospace
        /// </summary>
        public Dictionary<Regex, BaseFontFamily> BaseFontMapping { get; set; } = new Dictionary<Regex, BaseFontFamily>()
        {
            {new Regex("sans", RegexOptions.IgnoreCase), BaseFontFamily.SansSerif}, //this must be first so that it matches before serif
            {new Regex("serif", RegexOptions.IgnoreCase), BaseFontFamily.Serif},
            {new Regex("mono", RegexOptions.IgnoreCase), BaseFontFamily.Monospace}
        };


    }
}
  