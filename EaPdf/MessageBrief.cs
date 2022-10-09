using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace UIUCLibrary.EaPdf
{
    public class MessageBrief
    {
        [Index(0)]
        public long LocalId { get; set; }

        [Index(1)]
        public string From { get; set; } = string.Empty;

        [Index(2)]
        public string To { get; set; } = string.Empty;

        [Index(3)]
        public DateTimeOffset Date { get; set; }

        [Index(4)]
        public string Subject { get; set; } = string.Empty;

        [Index(5)]
        public string MessageID { get; set; } = string.Empty;

        [Index(6)]
        public string Hash { get; set; } = string.Empty;

        [Index(7)]
        public long Errors { get; set; } 
        
        [Name("First Error Message")]
        [Index(8)]
        public string FirstErrorMessage { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{LocalId}: {MessageID} -- {Subject}";
        }

        public static void SaveMessageBriefsToCsvFile(string csvFilePath, IEnumerable<MessageBrief> messageList)
        {
            using var csvStream = new StreamWriter(csvFilePath);
            using var csvWriter = new CsvWriter(csvStream, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(messageList);
        }


    }
}
