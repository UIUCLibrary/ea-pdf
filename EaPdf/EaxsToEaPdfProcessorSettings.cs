using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf
{
    public  class EaxsToEaPdfProcessorSettings
    {
        public string XsltFoFilePath { get; set; } = "XResources\\eaxs_to_fo.xslt";

        public string XsltXmpFilePath { get; set; } = "XResources\\eaxs_to_xmp.xslt";

        public string XmpSchemaExtension { get; set; } = "XResources\\EaPdfXmpSchema.xmp";

    }
}
