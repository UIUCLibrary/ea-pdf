using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf
{
    /// <summary>
    /// Properties that need to be persisted while processing an email mbox file
    /// </summary>
    internal class MboxProperties
    {

        public string MboxFilePath { get; set; } = "";

        public string MboxName
        {
            get
            {
                return System.IO.Path.GetFileNameWithoutExtension(MboxFilePath);
            }
        }

        public string MboxDirectoryName
        {
            get
            {
                return System.IO.Path.GetDirectoryName(MboxFilePath) ?? "";
            }
        }

        public string AccountId { get; set; } = "";

        public string OutFilePath { get; set; }="";
        
        public string OutDirectoryName
        {
            get
            {
                return System.IO.Path.GetDirectoryName(OutFilePath) ?? "";
            }
        }


        public bool IncludeSubFolders { get; set; } = true;

        /// <summary>
        /// The processor keeps tracks of the different line ending styles used in the mbox file.
        /// </summary>
        public Dictionary<string, int> EolCounts { get; } = new Dictionary<string, int>();

        /// <summary>
        /// This will return the most common line ending style used in the file.
        /// </summary>
        public string MostCommonEol
        {
            get
            {
                var ret = "";
                var max = 0;
                foreach (var kvp in EolCounts)
                {
                    if (kvp.Value > max)
                    {
                        max = kvp.Value;
                        ret = kvp.Key;
                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// If there are different line endings in the file this will return true
        /// Otherwise, it returns false
        /// </summary>
        public bool UsesDifferentEols
        {
            get
            {
                return EolCounts.Count > 1;
            }
        }

    }
}
