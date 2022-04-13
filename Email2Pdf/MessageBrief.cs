using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;

namespace Email2Pdf
{
    internal class MessageBrief
    {
        public long LocalId { get; set; }

        public string From { get; set; } = string.Empty;

        public string To { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string MessageID { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;

        public long Errors { get; set; } 

        [Name("First Error Message")]
        public string FirstErrorMessage { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{LocalId}: {MessageID} -- {Subject}";
        }
    }
}
