using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CommandLine;
using System.Text;
using UIUCLibrary.EaPdf.Helpers;

namespace EaPdfCmd
{
    public class CommandLineParams : ICommandLineParams
    {
        public CommandLineParams()
        {
            //Set required properties to some defaults, just to avoid compiler warnings about nulls
            In = new FileOrDirectoryInfo(".");
            Out = new DirectoryInfo(".");
            GlobalId = new Uri("http://example.com/");
        }

        [Option('i', "in", SetName = "file", Required = true, HelpText = "Input file or folder.")]
        public FileOrDirectoryInfo In { get; set; }

        [Option('o', "out", Required = true, HelpText = "Output folder.")]
        public DirectoryInfo Out { get; set; }

        [Option('g', "global-id", Required = true, HelpText = "Globally unique, permanent, absolute URI identifying the email archive.")]
        public Uri GlobalId { get; set; }

        [Option('c', "config", Required = false, HelpText = "Configuration file.")]
        public FileInfo? Config { get; set; }

        [Option('e', "email", Required = false, HelpText = "Email address(es) associated with the archive, repeatable.")]
        public IEnumerable<string>? Email { get; set; }

        [Option('l', "log-level", Required = false, HelpText = "Default log level.")]
        public LogLevel? LogLevel { get; set; }

        [Option('f', "fo-processor", Required = false, HelpText = "Which XSL-FO processor to use.")]
        public FoProcessor? FoProcessor { get; set; }

        /// <summary>
        /// Validate whether the command line parameters are valid.
        /// </summary>
        /// <param name="messages"></param>
        /// <returns>true if it is valid; else false</returns>
        public bool IsValid(out string messages)
        {
            var ret = new StringBuilder();

            if (!In.Exists)
            {
                ret.AppendLine($"The --in '{ShortenedPath(In.FullName)}' file or folder does not exist.");
            }
            else if (In.IsDirectory && !FilePathHelpers.IsValidOutputPathForMboxFolder(In.FullName, Out.FullName))
            {
                ret.AppendLine($"The --out '{ShortenedPath(Out.FullName)}' folder is not valid given the --in '{ShortenedPath(In.FullName)}' folder ; it cannot be the same as or a child of the input folder");
            }
            else if (In.IsFile && !FilePathHelpers.IsValidOutputPathForMboxFile(In.FullName, Out.FullName))
            {
                ret.AppendLine($"The --out '{ShortenedPath(Out.FullName)}' folder is not valid given the --in '{ShortenedPath(In.FullName)}' file; it cannot be the same as or a child of the input file taken as a folder name, ignoring any extensions");
            }

            if(In.Exists)
            {
                var inType = MimeKitHelpers.DetermineInputType(In.FullName, out _);

                if (inType == InputType.UnknownFile)
                {
                    ret.AppendLine($"The --in '{ShortenedPath(In.FullName)}' file is not a valid email archive file.");
                }
                else if (inType == InputType.UnknownFolder)
                {
                    ret.AppendLine($"The --in '{ShortenedPath(In.FullName)}' folder is not a valid email archive folder.");
                }
                else if (inType == InputType.MixedFolder)
                {
                    ret.AppendLine($"The --in '{ShortenedPath(In.FullName)}' folder contains both mbox files and eml, which is not currently supported.");
                }
            }

            if (!GlobalId.IsAbsoluteUri)
            {
                ret.AppendLine($"The --global-id '{GlobalId}' is not a valid absolute URI.");
            }

            if (Config != null && !Config.Exists)
            {
                ret.AppendLine($"The --config '{ShortenedPath(Config.FullName)}' file does not exist.");
            }

            messages = ret.ToString();
            return string.IsNullOrWhiteSpace(messages);
        }

        /// <summary>
        /// Shorten the path to just the outer directory name and the file name.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string ShortenedPath(string path)
        {
            var dir = Path.GetFileName(Path.GetDirectoryName(path)) ?? ".";
            var file = Path.GetFileName(path);
            var ret = Path.Combine(Path.GetFileName(dir), file);
            if(ret.Length < path.Length)
            {
                ret = $"...\\{ret}";
            }
            return ret;
        }
    }

    class CommandLineParamsValidationException : Exception
    {
        public CommandLineParamsValidationException(string message) : base(message) { }
    }
}
