using CommandLine;
using EmailValidation;
using Microsoft.Extensions.Logging;
using System.Reflection;
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

        [Option('s', "include-sub-folders", Required = false, HelpText = "Include sub-folders.")]
        public TrueFalse? IncludeSubFolders { get; set; }

        [Option('m', "allow-multiple-source-files", Required = false, HelpText = "Allow multiple source files in a single PDF.")]
        public TrueFalse? AllowMultipleSourceFilesPerOutputFile { get; set; }

        /// <summary>
        /// Validate whether the command line parameters are valid.
        /// </summary>
        /// <param name="messages"></param>
        /// <returns>true if it is valid; else false</returns>
        public bool IsValid(out string messages)
        {

            var ret = new StringBuilder();

            if(In.Name.Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                ret.AppendLine($"Required option '{GetOptionName(nameof(In))}' is missing. Did you forget the double dash in front of the long name?");
            }   
            else if (!In.Exists)
            {
                ret.AppendLine($"The '{GetOptionName(nameof(In))}' {(In.IsFile ? "file" : "folder")} '{FilePathHelpers.ShortenedPath(In.FullName)}' does not exist.");
            }
            else if (In.IsDirectory && !FilePathHelpers.IsValidOutputPathForMboxFolder(In.FullName, Out.FullName))
            {
                ret.AppendLine($"The '{GetOptionName(nameof(Out))}' folder '{FilePathHelpers.ShortenedPath(Out.FullName)}' cannot be the same as or a child of the {GetOptionName(nameof(In))} folder '{FilePathHelpers.ShortenedPath(In.FullName)}'.");
            }
            else if (In.IsFile && !FilePathHelpers.IsValidOutputPathForMboxFile(In.FullName, Out.FullName))
            {
                ret.AppendLine($"The '{GetOptionName(nameof(Out))}' folder '{FilePathHelpers.ShortenedPath(Out.FullName)}' cannot be the same as or a child of the '{GetOptionName(nameof(In))}' file '{FilePathHelpers.ShortenedPath(In.FullName)}' taken as a folder name, ignoring any extensions.");
            }
            else
            {
                var inType = MimeKitHelpers.DetermineInputType(In.FullName, out _);

                if (inType == InputType.UnknownFile)
                {
                    ret.AppendLine($"The '{GetOptionName(nameof(In))}' file '{FilePathHelpers.ShortenedPath(In.FullName)}' is not a supported email file; it must be an MBOX or EML file.");
                }
                else if (inType.IsUnknownFolder())
                {
                    ret.AppendLine($"The '{GetOptionName(nameof(In))}' folder '{FilePathHelpers.ShortenedPath(In.FullName)}' does not contain supported email files; it must contain some MBOX or EML files.");
                }
                else if (inType.IsMixedFolder())
                {
                    ret.AppendLine($"The '{GetOptionName(nameof(In))}' folder '{FilePathHelpers.ShortenedPath(In.FullName)}' contains both MBOX files and EML files, mixing file types is not currently supported.");
                }
            }

            //look for common error if using short param name and forget double dash
            if (Out.Name.Equals("ut", StringComparison.OrdinalIgnoreCase))
            {
                ret.AppendLine($"Required option '{GetOptionName(nameof(Out))}' is missing. Did you forget the double dash in front of the long name?");
            }


            //global id must be an absolute URI
            if (!GlobalId.IsAbsoluteUri)
            {
                ret.AppendLine($"The '{GetOptionName(nameof(GlobalId))}' value '{GlobalId}' is not a valid absolute URI.");
            }

            //config file must exist
            if(Config != null)
            {
                if (Config.Name.Equals("onfig", StringComparison.OrdinalIgnoreCase))
                {
                    ret.AppendLine($"Required option '{GetOptionName(nameof(Config))}' is missing. Did you forget the double dash in front of the long name?");
                }
                else if (!Config.Exists)
                {
                    ret.AppendLine($"The '{GetOptionName(nameof(Config))}' file '{FilePathHelpers.ShortenedPath(Config.FullName)}' does not exist.");
                }
            }

            //validate email addresses
            if (Email != null)
            {
                foreach (var email in Email)
                {
                    if (!EmailValidator.Validate(email))
                    {
                        ret.AppendLine($"The '{GetOptionName(nameof(Email))}' value '{email}' is not a valid email address.");
                    }
                }
            }   

            messages = ret.ToString();
            return string.IsNullOrWhiteSpace(messages);
        }

        public static string GetOptionName(string propertyName)
        {

            if(propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            string optionName = propertyName;
            PropertyInfo? prop = typeof(CommandLineParams).GetProperties().SingleOrDefault(p => p.Name == propertyName);

            if (prop != null)
            {
                var attr = prop.GetCustomAttribute<OptionAttribute>();
                if (attr != null)
                {
                    optionName = $"{attr.ShortName}, {attr.LongName}";
                }
            }
            return optionName;
        }

    }
}
