using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf
{
    public class MbxMessageHeader
    {
        /// <summary>
        /// Enum values taken from the Univerity of Washington IMAP Toolkit C code, specifically mail.h file
        /// </summary>
        [Flags]
        private enum MbxParseFlags
        {
            NONE =     0x0000,
            SEEN =     0x0001,
            DELETED =  0x0002,
            FLAGGED =  0x0004,
            ANSWERED = 0x0008,
            OLD =      0x0010,
            DRAFT =    0x0020,
            EXPUNGED = 0x8000 //internal use
        }

        public string Header { get; set; } = "";

        public long HeaderOffset { get; set; }

        public DateTime Date { get; set; }
        
        public ulong Size { get; set; }

        public uint Keywords { get; set; }

        ushort _flags;
        public ushort Flags {
            get 
            {
                return _flags;
            }
            set 
            {
                //test that the value is a valid combination of flags
                var allFlags = (MbxParseFlags)Enum.GetValues<MbxParseFlags>().Cast<int>().Sum();
                if (!allFlags.HasFlag((MbxParseFlags)value))
                {
                    throw new ArgumentException("Invalid MbxParseFlags value");
                }
                
                _flags = value;
            }
        }

        public uint Uid { get; set; }

        public string ParseResult { get; set; } = "";

        public MbxParser.ParseHeaderResult ParseResultCode { get; set; }

        public bool Seen
        {
            get { 
                return ((MbxParseFlags)Flags).HasFlag(MbxParseFlags.SEEN); 
            }
        }
        public bool Deleted
        {
            get { 
                return ((MbxParseFlags)Flags).HasFlag(MbxParseFlags.DELETED); 
            }
        }
        public bool Flagged
        {
            get { 
                return ((MbxParseFlags)Flags).HasFlag(MbxParseFlags.FLAGGED); 
            }
        }
        public bool Answered
        {
            get { 
                return ((MbxParseFlags)Flags).HasFlag(MbxParseFlags.ANSWERED); 
            }
        }
        public bool Old
        {
            get { 
                return ((MbxParseFlags)Flags).HasFlag(MbxParseFlags.OLD); 
            }
        }
        public bool Draft
        {
            get { 
                return ((MbxParseFlags)Flags).HasFlag(MbxParseFlags.DRAFT); 
            }
        }
        public bool Expunged
        {
            get { 
                return ((MbxParseFlags)Flags).HasFlag(MbxParseFlags.EXPUNGED); 
            }
        }
    }
}
