﻿using MimeKit;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.EaPdf
{
    /// <summary>
    /// Properties that need to be persisted while processing an email message file (mbox or eml)
    /// </summary>
    internal class MessageFileProperties : ICloneable
    {

        public MessageFileProperties()
        {
            //init the hash algorithm to the default values
            HashAlgorithm = EmailToEaxsProcessor.DefaultHashAlgorithm;
            HashAlgorithmName = EmailToEaxsProcessor.HASH_DEFAULT;
        }

        /// <summary>
        /// Create new MessageFileProperties from an existing MessageFileProperties, copy just select properties 
        /// </summary>
        /// <param name="source"></param>
        public MessageFileProperties(MessageFileProperties source) : this()
        {
            this.MessageFilePath = source.MessageFilePath;
            this.OutFilePath = source.OutFilePath;
            this.OutFileNumber = source.OutFileNumber;
            this.AccountEmails = source.AccountEmails;
            this.GlobalId = source.GlobalId;
            this.MessageFormat = source.MessageFormat;
            this.HashAlgorithmName = source.HashAlgorithmName;
            this.HashAlgorithm = HashAlgorithm.Create(HashAlgorithmName) ?? EmailToEaxsProcessor.DefaultHashAlgorithm;
        }

        /// <summary>
        /// Return a shallow copy of the object
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        /// <summary>
        /// If true, the file was skipped during processing
        /// </summary>
        public bool FileWasSkipped { get; set; } = false;

        private string _messageFilepath = "";
        /// <summary>
        /// The full path to the message file being processed
        /// </summary>
        public string MessageFilePath
        {
            get
            {
                return _messageFilepath;
            }
            set
            {
                _messageFilepath = value;
                _fileInfo = null; //clear the cached FileInfo object
                _inputFileType = null; //clear the cached InputType
            }
        }

        /// <summary>
        /// The name of the file, which is just the file name minus the path
        /// </summary>
        public string MessageFileName
        {
            get
            {
                return System.IO.Path.GetFileName(MessageFilePath);
            }
        }

        /// <summary>
        /// Returns the value used for the Folder Name element in the XML
        /// It is based on the MessageFileName for MBOX files or the parent Directory name for EML files
        /// </summary>
        public string XmlFolderName
        {
            get
            {
                if (MessageFormat == MimeFormat.Mbox)
                {
                    return MessageFileName;
                }
                else
                {
                    return Path.GetFileName(Path.GetDirectoryName(MessageFilePath) ?? MessageFileName);
                }
            }
        }

        /// <summary>
        /// The name of the directory containing the message file
        /// </summary>
        public string MessageDirectoryName
        {
            get
            {
                return System.IO.Path.GetDirectoryName(MessageFilePath) ?? "";
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
                Uri globalUri = new(value);

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
            string origFilePath = OriginalOutFilePath; //must get this before incrementing OutFileNumber
            OutFileNumber++;

            OutFilePath = FilePathHelpers.GetFilePathWithIncrementNumber(origFilePath, OutFileNumber);

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
                    ret = FilePathHelpers.GetFilePathWithoutIncrementNumber(OutFilePath);
                }

                return ret;
            }
        }


        /// <summary>
        /// The processor keeps tracks of the different line ending styles used in the message file.
        /// The key is the EOL and the value is the number of occurences of this EOL.
        /// EOLs which do not appear in the file will not appear in the dictionary
        /// </summary>
        public Dictionary<string, int> EolCounts { get; } = new Dictionary<string, int>();

        public void ResetEolCounts()
        {
            EolCounts.Clear();
        }

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
        /// The hash algorithm used for the message file, defaults to MD5
        /// Each message file needs its own since it records state
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
        /// The number of valid messages found in the message file
        /// </summary>
        public int MessageCount { get; set; } = 0;


        public string RelativePath
        {
            get
            {
                var relPath = Path.GetRelativePath(OutDirectoryName, MessageFilePath);
                var ret = new Uri(relPath, UriKind.Relative);
                return ret.ToString().Replace('\\', '/');
            }
        }

        /// <summary>
        /// The file extension of the message file including the leading period
        /// </summary>
        public string Extension
        {
            get
            {
                return Path.GetExtension(MessageFilePath);
            }
        }

        FileInfo? _fileInfo;
        public FileInfo? FileInfo
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(MessageFilePath) && File.Exists(MessageFilePath))
                {
                    _fileInfo ??= new FileInfo(MessageFilePath);
                    return _fileInfo;
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// The format of the message file, either a single EML message or a MBOX file
        /// </summary>
        public MimeFormat MessageFormat { get; set; } = MimeFormat.Mbox;    //This is the same as MBox //TODO: Maybe check the InputFileType to determine this

        /// <summary>
        /// Return a more human-readable name for the MessageFormat
        /// </summary>
        public string MessageFormatName
        {
            get
            {
                return MimeKitHelpers.GetFormatName(MessageFormat);
            }
        }

        /// <summary>
        /// Determine the input file type using the MimeKitHelpers.DetermineInputType function
        /// </summary>
        /// <param name="message">possible warning message associated with determnining the file type</param>
        InputType? _inputFileType;
        public InputType? GetInputFileType(out string message)
        {
            message = "";
            if (_inputFileType == null)
            {
                if (FileInfo != null)
                {
                    _inputFileType = MimeKitHelpers.DetermineInputType(FileInfo.FullName, out message);
                }
            }
            return _inputFileType;
        }

        public bool DoesMimeFormatMatchInputFileType()
        {
            return DoesMimeFormatMatchInputFileType(out _);
        }

        public bool DoesMimeFormatMatchInputFileType(out string message)
        {
            bool ret = false;
            message = "";
            if(FileInfo != null)
            {
                var inputFileType = GetInputFileType(out message);
                ret = MimeKitHelpers.DoesMimeFormatMatchInputFileType( MessageFormat, inputFileType ?? InputType.UnknownFile);
            }
            return ret;
        }


        public bool AlreadySerialized { get; set; } = false;
    }
}
