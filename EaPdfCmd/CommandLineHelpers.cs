using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pastel;
using System.Text;
using UIUCLibrary.EaPdf.Helpers;

namespace EaPdfCmd
{

    public enum ReturnValue
    {
        OK = 0, //to conform with normal rules for command line return values
        HelpOrVersionRequest,
        ArgumentError,
        FileNotFound,
        FolderNotFound,
        UnsupportedInputType,
        EmailsToEaxsError,
        EaxsToPdfError,
        UnexpectedError,
        ConfigurationError
    }

    public enum FoProcessor
    {
        Fop,
        Xep
    }

    public enum XsltProcessor
    {
        Saxon
    }

    /// <summary>
    /// Just so that commandlineparser display the acceptable values in the help text for bools
    /// </summary>
    public enum TrueFalse
    {
        True = 1,
        False = 0
    }

    public static class CommandLineHelpers
    {
        const int DefaultMaximumLength = 80;
        const int DefaultIndent = 2;

        /// <summary>
        /// So you can use the TrueFalse enum as a boolean
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool ToBoolean(this TrueFalse value)
        {
            switch (value)
            {
                case TrueFalse.True: return true;
                case TrueFalse.False: return false;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public static ReturnValue LoadCmdLineParamsAndConfig(IHostApplicationBuilder hostBldr, string[] args)
        {
            var cmdLineParams = ParseCommandLineParams(args);

            if (cmdLineParams == null)
            {
                if (args.Contains("--help") || args.Contains("--version"))
                {
                    //normal exit for help/version
                    return ReturnValue.HelpOrVersionRequest;
                }
                else
                {
                    return ReturnValue.ArgumentError;
                }
            }

            //add the command line params to the service collection
            hostBldr.Services.AddSingleton<ICommandLineParams>(sp => cmdLineParams);

            //logging just for the startup
            var startupLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole(opts => opts.LogToStandardErrorThreshold = LogLevel.Trace).SetMinimumLevel(cmdLineParams.LogLevel ?? LogLevel.Information));
            var logger = startupLoggerFactory.CreateLogger("Startup");
            logger.LogTrace("Starting Main");


            //look for appsettings.json or App.Config in the app directory
            var appPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(appPath))
            {
                logger.LogTrace($"Loading configuration from '{appPath}'");
                hostBldr.Configuration.AddJsonFile(appPath);
            }
            else
            {
                appPath = Path.Combine(AppContext.BaseDirectory, "App.config");
                if (File.Exists(appPath))
                {
                    logger.LogTrace($"Loading configuration from '{appPath}'");
                    hostBldr.Configuration.AddXmlFile(appPath);
                }
            }

            char[] seps = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            //only look for appsettings.json or App.Config in the current directory if the app directory is different from the current directory
            if (!AppContext.BaseDirectory.TrimEnd(seps).Equals(Environment.CurrentDirectory.TrimEnd(seps), Path.DirectorySeparatorChar == Path.AltDirectorySeparatorChar ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
            {
                //look for appsettings.json or App.config in the current directory
                appPath = "appsettings.json";
                if (File.Exists(appPath))
                {
                    logger.LogTrace($"Loading configuration from '{appPath}'");
                    hostBldr.Configuration.AddJsonFile(appPath);
                }
                else
                {
                    appPath = "App.Config";
                    if (File.Exists(appPath))
                    {
                        logger.LogTrace($"Loading configuration from '{appPath}'");
                        hostBldr.Configuration.AddXmlFile(appPath);
                    }
                }
            }

            //look for config file using --config option
            var configFilePath = hostBldr.Configuration.GetValue<string>("config");

            if (configFilePath != null)
            {
                if (!File.Exists(configFilePath))
                {
                    logger.LogError($"Configuration file not found: '{configFilePath}'");
                }
            }
            var ext = Path.GetExtension(configFilePath) ?? "";
            if (configFilePath != null && (ext.Equals(".xml", StringComparison.OrdinalIgnoreCase) || ext.Equals(".config", StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogTrace($"Loading configuration from '{configFilePath}'");
                hostBldr.Configuration.AddXmlFile(configFilePath, optional: true);
            }
            else if (configFilePath != null && ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogTrace($"Loading configuration from '{configFilePath}'");
                hostBldr.Configuration.AddJsonFile(configFilePath, optional: true);
            }
            else if (configFilePath != null)
            {
                logger.LogError($"Unable to load configuration from '{configFilePath}'; invalid configuration file; must be an .xml, .config, or .json file with one of these extensions.");
            }

            hostBldr.Configuration.AddCommandLine(args);

            ConvertCommandLineParmsIntoConfig(hostBldr, cmdLineParams);

            FixConfigPaths(hostBldr.Configuration);

            var validionResult = ValidateConfiguration(hostBldr.Configuration, logger);

            return validionResult;

        }

        /// <summary>
        /// Some command line arguments will override configuration settings
        /// </summary>
        /// <param name="hostBldr"></param>
        /// <param name="cmdLineParams"></param>
        private static void ConvertCommandLineParmsIntoConfig(IHostApplicationBuilder hostBldr, ICommandLineParams cmdLineParams)
        {
            //set some config values from the command line
            var parms = new Dictionary<string, string?>();

            //set the Logging:LogLevel:Default from the command line
            if (cmdLineParams.LogLevel != null)
            {
                parms.Add("Logging:LogLevel:Default", cmdLineParams.LogLevel.ToString());
            }

            //set the FoProcessor:Default from the command line
            if (cmdLineParams.FoProcessor != null)
            {
                parms.Add("FoProcessors:Default", cmdLineParams.FoProcessor.ToString());
            }

            ////set the EmailToEaxsProcessorSettings:IncludeSubFolders from the command line
            //if (cmdLineParams.IncludeSubFolders != null)
            //{
            //    parms.Add("EmailToEaxsProcessorSettings:IncludeSubFolders", cmdLineParams.IncludeSubFolders.ToString());
            //}

            ////set the EmailToEaxsProcessorSettings:OneFilePerMessageFile from the command line
            //if (cmdLineParams.OneFilePerMessageFile != null)
            //{
            //    parms.Add("EmailToEaxsProcessorSettings:OneFilePerMessageFile", cmdLineParams.OneFilePerMessageFile.ToString());
            //}

            hostBldr.Configuration.AddInMemoryCollection(parms);
        }

        /// <summary>
        /// Turn relative paths in the configuration into absolute paths, either based on the current directory or the directory of the configuration file
        /// </summary>
        /// <param name="config"></param>
        private static void FixConfigPaths(IConfiguration config)
        {
            //convert the relative paths to absolute paths, if needed
            _ = ConfigHelpers.MakeConfigPathAbsolute(config, "EaxsToEaPdfProcessorSettings:XsltFoFilePath");
            _ = ConfigHelpers.MakeConfigPathAbsolute(config, "EaxsToEaPdfProcessorSettings:XsltXmpFilePath");
            _ = ConfigHelpers.MakeConfigPathAbsolute(config, "EaxsToEaPdfProcessorSettings:XsltRootXmpFilePath");

            _ = ConfigHelpers.MakeConfigPathAbsolute(config, "FoProcessors:Fop:ConfigFilePath");
            _ = ConfigHelpers.MakeConfigPathAbsolute(config, "FoProcessors:Xep:ConfigFilePath");

        }

        private static ReturnValue ValidateConfiguration(IConfiguration config, ILogger logger)
        {
            //check that files exist
            if (!FileExists(config, "EaxsToEaPdfProcessorSettings:XsltFoFilePath", logger))
            {
                return ReturnValue.FileNotFound;
            }
            if (!FileExists(config, "EaxsToEaPdfProcessorSettings:XsltXmpFilePath", logger))
            {
                return ReturnValue.FileNotFound;
            }
            if (!FileExists(config, "EaxsToEaPdfProcessorSettings:XsltRootXmpFilePath", logger))
            {
                return ReturnValue.FileNotFound;
            }
            if (!FileExists(config, "FoProcessors:Fop:JarFilePath", logger))
            {
                return ReturnValue.FileNotFound;
            }
            if (!FileExists(config, "FoProcessors:Fop:ConfigFilePath", logger))
            {
                return ReturnValue.FileNotFound;
            }
            if (!FileExists(config, "FoProcessors:Xep:ConfigFilePath", logger))
            {
                return ReturnValue.FileNotFound;
            }

            var classPath = config["XsltProcessors:Saxon:ClassPath"];
            if (!string.IsNullOrWhiteSpace(classPath))
            {
                foreach (string path in classPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!File.Exists(path))
                    {
                        logger.LogError($"File '{path}' not found for 'XsltProcessors:Saxon:ClassPath':'{classPath}' ");
                        return ReturnValue.FileNotFound;
                    }
                }
            }

            classPath = config["FoProcessors:Xep:ClassPath"];
            if (!string.IsNullOrWhiteSpace(classPath))
            {
                foreach (string path in classPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!File.Exists(path))
                    {
                        logger.LogError($"File '{path}' not found for 'XsltProcessors:Saxon:ClassPath':'{classPath}' ");
                        return ReturnValue.FileNotFound;
                    }
                }
            }

            //check for valid FO processor
            var foProcStr = config["FoProcessors:Default"] ?? "";
            if (!string.IsNullOrWhiteSpace(foProcStr) && !Enum.TryParse<FoProcessor>(foProcStr, true, out _))
            {
                logger.LogError($"Invalid value for 'FoProcessors:Default':'{foProcStr}'; must be one of these values: {string.Join(", ", Enum.GetNames(typeof(FoProcessor)))}");
                return ReturnValue.ConfigurationError;
            }

            //check for valid XSLT processor
            var xsltProcStr = config["XsltProcessors:Default"] ?? "";
            if (!string.IsNullOrWhiteSpace(xsltProcStr) && !Enum.TryParse<XsltProcessor>(xsltProcStr, true, out _))
            {
                logger.LogError($"Invalid value for 'XsltProcessors:Default':'{xsltProcStr}'; must be one of these values: {string.Join(", ", Enum.GetNames(typeof(XsltProcessor)))}");
                return ReturnValue.ConfigurationError;
            }

            //check for invalid configuration combinations
            var wrap = config.GetValue<bool?>("EmailToEaxsProcessorSettings:WrapExternalContentInXml");
            if (wrap != null && wrap == false && foProcStr.Equals("Xep", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError($"The 'EmailToEaxsProcessorSettings:WrapExternalContentInXml' setting is {wrap}, and the 'FoProcessors:Default' setting is '{foProcStr}'. This is not supported.");
                return ReturnValue.ConfigurationError;
            }

            //prevent some defaults from being changed
            var preserveBinary = config.GetValue<bool?>("EmailToEaxsProcessorSettings:PreserveBinaryAttachmentTransferEncodingIfPossible");
            if (preserveBinary != null && preserveBinary == true)
            {
                logger.LogError($"The 'EmailToEaxsProcessorSettings:PreserveBinaryAttachmentTransferEncodingIfPossible' setting is {preserveBinary}. This is not currently supported.");
                return ReturnValue.ConfigurationError;
            }

            var preserveText = config.GetValue<bool?>("EmailToEaxsProcessorSettings:PreserveTextAttachmentTransferEncoding");
            if (preserveText != null & preserveText == true)
            {
                logger.LogError($"The 'EmailToEaxsProcessorSettings:PreserveTextAttachmentTransferEncoding' setting is {preserveText}. This is not currently supported.");
                return ReturnValue.ConfigurationError;
            }

            var includeSubFolders = config.GetValue<bool?>("EmailToEaxsProcessorSettings:IncludeSubFolders");
            if (includeSubFolders != null && includeSubFolders == true)
            {
                logger.LogError($"The 'EmailToEaxsProcessorSettings:IncludeSubFolders' setting is {includeSubFolders}. This is not currently supported.");
                return ReturnValue.ConfigurationError;
            }

            var oneFilePerMessageFile = config.GetValue<bool?>("EmailToEaxsProcessorSettings:OneFilePerMessageFile");
            if (oneFilePerMessageFile != null && oneFilePerMessageFile == true)
            {
                logger.LogError($"The 'EmailToEaxsProcessorSettings:OneFilePerMessageFile' setting is {oneFilePerMessageFile}. This is not currently supported.");
                return ReturnValue.ConfigurationError;
            }

            var maxFileSize = config.GetValue<int?>("EmailToEaxsProcessorSettings:MaxFileSize");
            if (maxFileSize != null && maxFileSize > 0)
            {
                logger.LogError($"The 'EmailToEaxsProcessorSettings:MaxFileSize' setting is {maxFileSize}. This is not currently supported.");
                return ReturnValue.ConfigurationError;
            }

            return ReturnValue.OK;
        }

        private static bool FileExists(IConfiguration config, string key, ILogger logger)
        {
            var path = config[key];
            if (string.IsNullOrWhiteSpace(path) || File.Exists(path))
                return true;

            logger.LogError($"File not found for '{key}':'{path}' ");
            return false;
        }

        private static bool FolderExists(IConfiguration config, string key, ILogger logger)
        {
            var path = config[key];
            if (string.IsNullOrWhiteSpace(path) || Directory.Exists(path))
                return true;

            logger.LogError($"File not found for '{key}':'{path}' ");
            return false;
        }

        /// <summary>
        /// Parse the command line arguments and return the parsed parameters, or null if there was an error
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static ICommandLineParams? ParseCommandLineParams(string[] args)
        {

            //Parse the command line arguments
            var argParser = new Parser(with =>
            {
                with.IgnoreUnknownArguments = false;
                with.CaseInsensitiveEnumValues = true;
                with.CaseSensitive = false;
                with.HelpWriter = null;
            });
            var argResults = argParser.ParseArguments<CommandLineParams>(args);

            //Validate the command line arguments
            if (!ValidateCommandLineArgs(argResults))
            {
                return null;
            }
            return argResults.Value;
        }

        /// <summary>
        /// Make sure the command line arguments are valid and print help if they are not
        /// </summary>
        /// <param name="argResults"></param>
        /// <returns>true if the arguments are valid; else false</returns>
        private static bool ValidateCommandLineArgs(ParserResult<CommandLineParams> argResults)
        {
            int maxWidth = DefaultMaximumLength;
            int indent = DefaultIndent;

            //don't colorize the help text if the output is redirected
            if(Console.IsErrorRedirected)
            {
                Pastel.ConsoleExtensions.Disable();
            }

            string errHdr = SentenceBuilder.Create().ErrorsHeadingText();
            int colorLength = "".Pastel(ConsoleColor.Red).Length;

            string myErrors = "";
            if (argResults.Tag == ParserResultType.NotParsed || !argResults.Value.IsValid(out myErrors))
            {

                var argErrors = GetArgErrors(argResults);
                if (!string.IsNullOrWhiteSpace(argErrors))
                    argErrors = ColorizeEachLine(TextWrapper.WrapAndIndentText(argErrors, indent, maxWidth - colorLength - indent), ConsoleColor.Red);
                if (!string.IsNullOrWhiteSpace(myErrors))
                    myErrors = ColorizeEachLine(TextWrapper.WrapAndIndentText(myErrors, indent, maxWidth - colorLength - indent), ConsoleColor.Red);

                HelpText helpText = HelpText.AutoBuild(argResults,
                    e =>
                    {
                        e.AutoHelp = true;
                        e.AutoVersion = true;
                        e.AddEnumValuesToHelpText = true;
                        e.Heading = "";
                        e.Copyright = "";
                        e.AddDashesToOption = true;
                        if (!string.IsNullOrWhiteSpace(argErrors))
                        {
                            e.AddPreOptionsLine(errHdr.Pastel(ConsoleColor.Red));
                            e.AddPreOptionsLine(argErrors);
                        }
                        return e;
                    },
                    x =>
                    {
                        return x;
                    });

                if (argResults.Tag == ParserResultType.Parsed && !string.IsNullOrWhiteSpace(myErrors))
                {
                    helpText.Heading = "";
                    helpText.Copyright = "";
                    helpText.AddPreOptionsLine(errHdr.Pastel(ConsoleColor.Red));
                    helpText.AddPreOptionsLine(myErrors);
                }

                //Remove the Git Commit ID from the HeadingInfo version
                string heading = HeadingInfo.Default.ToString();
                int plus = heading.LastIndexOf("+");
                if(plus > 0)
                {
                    heading = heading[..plus];
                }

                if (argResults.Errors.IsHelp() || argResults.Errors.IsVersion())
                {
                    //The parser handles the help/version, go to stdout and report this is a success
                    Console.Out.WriteLine(heading);
                    if (!argResults.Errors.IsVersion())
                    {
                        Console.Out.WriteLine(CopyrightInfo.Default);
                        Console.Out.WriteLine(helpText);
                    }
                    return true;
                }
                else
                {
                    //Otherwise, there was an argument parsing error, go stderr and report this is a failure
                    Console.Error.WriteLine(heading);
                    Console.Error.WriteLine(CopyrightInfo.Default);
                    Console.Error.WriteLine(helpText);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Very kludgy way to get the parser errors, by parsing the help text string
        /// Assumes format like:
        /// 
        /// ERROR(S):
        ///   error 1
        ///   error 2
        ///   [blank line]
        /// 
        /// </summary>
        /// <param name="argResults"></param>
        /// <returns></returns>
        private static string GetArgErrors(ParserResult<CommandLineParams> argResults)
        {
            string tag = SentenceBuilder.Create().ErrorsHeadingText();
            string hlpTxt = HelpText.AutoBuild(argResults,
                e =>
                {
                    e.Heading = "";
                    e.Copyright = "";
                    return HelpText.DefaultParsingErrorsHandler(argResults, e);
                },
                x =>
                {
                    return x;
                });

            if (string.IsNullOrWhiteSpace(tag))
            {
                return "";
            }

            var bldr = new StringBuilder();

            var rdr = new StringReader(hlpTxt);
            string? hlpLine = null;
            do
            {
                hlpLine = rdr.ReadLine();
                if (hlpLine == tag)
                {
                    string? errLine = null;
                    do
                    {
                        errLine = rdr.ReadLine();
                        bldr.AppendLine(errLine?.Trim());
                    } while (!string.IsNullOrWhiteSpace(errLine));

                    hlpLine = null;
                }
            } while (hlpLine != null);



            return bldr.ToString().Trim();
        }

        /// <summary>
        /// Colorize each line separately, so word wrapping doesn't mess up the color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private static string ColorizeEachLine(string text, ConsoleColor color)
        {
            var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var bldr = new StringBuilder();
            foreach (var line in lines)
            {
                bldr.AppendLine(line.Pastel(color));
            }
            return bldr.ToString();
        }

    }
}
