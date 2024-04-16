using Microsoft.Extensions.Logging;

namespace EaPdfCmd
{
    public interface ICommandLineParams
    {
        FileOrDirectoryInfo In { get; set; }

        DirectoryInfo Out { get; set; }

        Uri GlobalId { get; set; }

        FileInfo? Config { get; set; }

        IEnumerable<string>? Email { get; set; }

        LogLevel? LogLevel { get; set; } 

        FoProcessor? FoProcessor { get; set; }
    }
}