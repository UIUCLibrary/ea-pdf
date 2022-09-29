using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf
{
    /// <summary>
    /// Properties that need to be persisted while processing an email mbox file
    /// </summary>
    internal class MboxProperties
    {

        public MboxProperties()
        {
            //init the hash algorithm
            HashAlgorithm = SHA256.Create();
            HashAlgorithmName = "SHA256";
        }

        public string MboxFilePath { get; set; } = "";

        public string MboxName
        {
            get
            {
                return System.IO.Path.GetFileName(MboxFilePath);
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

        public string OutFilePath { get; set; } = "";

        public string OutDirectoryName
        {
            get
            {
                return System.IO.Path.GetDirectoryName(OutFilePath) ?? "";
            }
        }


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
                var ret = MimeMessageProperties.EOL_TYPE_UNK;
                var max = 0;
                foreach (var kvp in EolCounts.Where(c=>c.Key!=MimeMessageProperties.EOL_TYPE_UNK))
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

        /// <summary>
        /// The hash algorithm used for the mbox, defaults to SHA256
        /// Each mbox file needs its own since it records state
        /// </summary>
        public HashAlgorithm HashAlgorithm { get; private set; }

        /// <summary>
        /// The name of the hash algorithm to use
        /// </summary>
        public string HashAlgorithmName { get; private set; }

        public string TrySetHashAlgorithm(string hashName)
        {
            if (hashName == HashAlgorithmName)
            {
                //its already set correctly
                return HashAlgorithmName;
            }

            var alg = HashAlgorithm.Create(hashName);
            if (alg != null)
            {
                HashAlgorithm.Dispose(); //dispose of the previous algorithm
                
                HashAlgorithm = alg;
                HashAlgorithmName = hashName;
                return HashAlgorithmName;
            }
            else
            {
                //couldn't be changed to this algoritm so just leaving it as is
                return HashAlgorithmName;
            }
        }

        public int MessageCount { get; set; } = 0;
    }
}
