using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System.Globalization;

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

        [Name("Source Filename")]
        [Index(9)]
        public string SourceFileName { get; set; } = string.Empty;

        [Name("Destination Filename")]
        [Index(10)]
        public string DestinationFileName { get; set; } = string.Empty;

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
