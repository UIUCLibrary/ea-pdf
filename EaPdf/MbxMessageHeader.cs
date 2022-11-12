using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf
{
    internal class MbxMessageHeader
    {
        public string Header { get; set; } = "";

        public long HeaderOffset { get; set; }

        public DateTime Date { get; set; }

        public long Size { get; set; }

        public uint Keywords { get; set; }

        public ushort Flags { get; set; }

        public uint Uid { get; set; }

        public string ParseResult { get; set; } = "";

        public MbxParser.ParseHeaderResult ParseResultCode { get; set; }
    }
}
