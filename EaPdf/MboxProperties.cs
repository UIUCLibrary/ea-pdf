using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf
{
    /// <summary>
    /// Properties that need to be persisted while processing an email mbox file
    /// </summary>
    internal class MboxProperties
    {

        /// <summary>
        /// Default to the SHA256 hash algorithm
        /// </summary>
        public MboxProperties()
        {
            //init the hash algorithm
            HashAlgorithm = SHA256.Create();
            HashAlgorithmName = "SHA256";
        }

        /// <summary>
        /// Create new MboxProperties from an existing MBoxProperties 
        /// </summary>
        /// <param name="source"></param>
        public MboxProperties(MboxProperties source) : this()
        {
            this.MboxFilePath = source.MboxFilePath;
            this.OutFilePath = source.OutFilePath;
            this.OutFileNumber = source.OutFileNumber;
            this.AccountEmails = source.AccountEmails;
            this.GlobalId = source.GlobalId;
        }

        /// <summary>
        /// The full path to the mbox file being processed
        /// </summary>
        public string MboxFilePath { get; set; } = "";

        /// <summary>
        /// The name of the mbox file, which is just the file name minus the path
        /// </summary>
        public string MboxName
        {
            get
            {
                return System.IO.Path.GetFileName(MboxFilePath);
            }
        }

        /// <summary>
        /// The name of the directory containing the mbox file
        /// </summary>
        public string MboxDirectoryName
        {
            get
            {
                return System.IO.Path.GetDirectoryName(MboxFilePath) ?? "";
            }
        }

        string _globalId = "";
        /// <summary>
        /// Globally unique, permanent, absolute URI with no fragment conforming to the canonical form specified in RFC2396 as amended by RFC2732. 
        /// </summary>
        public string GlobalId
        {
            get
            {
                return _globalId;
            }
            set
            {
                //To check for validity, try to create the Uri from the string
                Uri globalUri = new Uri(value);

                if (!globalUri.IsAbsoluteUri)
                    throw new UriFormatException("");
                if (!string.IsNullOrWhiteSpace(globalUri.Fragment))
                    throw new UriFormatException("");

                _globalId = globalUri.ToString();
            }

        }

        /// <summary>
        /// Comma-separated list email addresses associated with the account
        /// </summary>
        public string AccountEmails { get; set; } = "";

        /// <summary>
        /// Return the AccountEmails as a enumerable of strings
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetAccountEmails()
        {
            return AccountEmails.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public int OutFileNumber { get; private set; } = 0;
        public int IncrementOutFileNumber()
        {
            var origOutFilePath = OriginalOutFilePath;

            OutFileNumber++;

            if (OutFileNumber > 9999)
                throw new Exception("No more than 9999 file are supported.");
            
            //Update the OutFilePath to add the file number
            var ext = Path.GetExtension(OutFilePath);
            OutFilePath = Path.Combine(OutDirectoryName, Path.GetFileNameWithoutExtension(origOutFilePath) + "_" + OutFileNumber.ToString("0000") + ext);
            
            return OutFileNumber;
        }


        /// <summary>
        /// The path to the output XML file.
        /// Note that the other output files, such as the CSV or external attachments, will be in the same directory
        /// </summary>
        public string OutFilePath { get; set; } = "";

        /// <summary>
        /// The name of the directory containing the output XML file
        /// </summary>
        public string OutDirectoryName
        {
            get
            {
                return System.IO.Path.GetDirectoryName(OutFilePath) ?? "";
            }
        }

        /// <summary>
        /// Return the original OutFilePath prior to any FileNumber increments
        /// </summary>
        public string OriginalOutFilePath 
        {
            get
            {
                var ret = OutFilePath;

                if (OutFileNumber > 0)
                {
                    //strip off the file number from the end
                    var ext = Path.GetExtension(OutFilePath);
                    var name = Path.GetFileNameWithoutExtension(OutFilePath);
                    if (Regex.IsMatch(name,"_\\d\\d\\d\\d$"))
                    {
                        name = name[..^5];
                    }
                    else
                    {
                        throw new Exception($"Unexpected filename format '{name}'.  It should end like '*_nnnn'.");
                    }
                    ret = Path.Combine(OutDirectoryName, name + ext);
                }

                return ret;
            }
        }


        /// <summary>
        /// The processor keeps tracks of the different line ending styles used in the mbox file.
        /// The key is the EOL and the value is the number of occurences of this EOL.
        /// EOLs which do not appear in the file will not appear in the dictionary
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
                foreach (var kvp in EolCounts.Where(c => c.Key != MimeMessageProperties.EOL_TYPE_UNK))
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

        /// <summary>
        /// Try to change to a different hash algorithm
        /// If unable to change, whatever the previous value was will remain
        /// </summary>
        /// <param name="hashName">The name of the hash algorithm from the list used by the HashAlgorithm.Create(String) function </param>
        /// <returns>The name of the newly set hash algorithm</returns>
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

        /// <summary>
        /// The number of valid messages found in the mbox
        /// </summary>
        public int MessageCount { get; set; } = 0;

    }
}
