using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UIUCLibrary.EaPdf;
using UIUCLibrary.EaPdf.Helpers;

namespace EaPdfCmd
{
    public class EmailToEaPdfProcessor : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ICommandLineParams _parms;

        public EmailToEaPdfProcessor(ILogger<EmailToEaPdfProcessor> logger, IConfiguration config, ICommandLineParams parms, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _config = config;
            _parms = parms;
            _appLifetime = appLifetime;

            //TODO: probably don't need these
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);
        }

        public ReturnValue ExitCode { get; private set; } = ReturnValue.OK;
        public List<(LogLevel, string)> Errors { get; private set; } = new List<(LogLevel, string)>();

        /// <summary>
        /// Process the emails in the input file or folder and write the results to the output folder.
        /// </summary>
        /// <returns>status code indicating success or failure</returns>
        private int ProcessEmails()
        {
            var input = _parms.In;
            var output = _parms.Out;
            var gid = _parms.GlobalId;
            var emails = _parms.Email;

            var inputType = MimeKitHelpers.DetermineInputType(input.FullName, out _);


            try
            {
                //Init the email to EAXS processors using the configuration
                var emailProc = new EmailToEaxsProcessor(_logger, _config);

                //Init EAXS to PDF processors using the configuration
                var pdfProc = new EaxsToEaPdfProcessor(_logger, _config);

                //check for compatibility between the settings of the two processors
                string foProc = _config["FoProcessors:Default"] ?? "Fop";
                if (foProc.Equals("Xep", StringComparison.OrdinalIgnoreCase) && emailProc.Settings.WrapExternalContentInXml== false)
                {
                    Errors.Add((LogLevel.Error, $"The 'WrapExternalContentInXml' setting is {emailProc.Settings.WrapExternalContentInXml}, and the 'FoProcessors:Default' setting is '{foProc}'. This is not supported."));
                    ExitCode = ReturnValue.ConfigurationError;
                    return (int)ExitCode;
                }


                long ret = 0;
                switch (inputType)
                {
                    case InputType.MboxFile:
                        ret = emailProc.ConvertMboxToEaxs(input.FullName, output.FullName, gid.ToString(), emails);
                        break;
                    case InputType.MboxFolder:
                        ret = emailProc.ConvertFolderOfMboxToEaxs(input.FullName, output.FullName, gid.ToString(), emails);
                        break;
                    case InputType.EmlFile:
                        ret = emailProc.ConvertEmlToEaxs(input.FullName, output.FullName, gid.ToString(), emails);
                        break;
                    case InputType.EmlFolder:
                        ret = emailProc.ConvertFolderOfEmlToEaxs(input.FullName, output.FullName, gid.ToString(), emails);
                        break;
                    default:
                        Errors.Add((LogLevel.Error, $"Input type '{inputType}' is not supported"));
                        ExitCode = ReturnValue.UnsupportedInputType;
                        return (int)ExitCode;
                }

                if (ret <= 0)
                {
                    Errors.Add((LogLevel.Error, $"Error converting emails to EAXS: {ret}; review the log for details"));
                    ExitCode = ReturnValue.EmailsToEaxsError;
                    return (int)ExitCode;
                }
                else
                {
                    var xmlFile = FilePathHelpers.GetXmlOutputFilePath(output.FullName, input.FullName);
                    var pdfFile = Path.ChangeExtension(xmlFile,".pdf");
                    try
                    {
                        var files = pdfProc.ConvertEaxsToPdf(xmlFile, pdfFile);
                    }
                    catch (Exception ex)
                    {
                        Errors.Add((LogLevel.Error, ex.Message));
                        ExitCode = ReturnValue.EaxsToPdfError;
                        return (int)ExitCode;
                    }
                }
            }
            catch (Exception ex)
            {
                Errors.Add((LogLevel.Error, ex.Message));
                ExitCode = ReturnValue.EmailsToEaxsError;
                return (int)ExitCode;
            }


            ExitCode = ReturnValue.OK;
            return (int)ExitCode;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("StartAsync called");
            Task<int> tskInt = Task.Run<int>(ProcessEmails, cancellationToken);
            return tskInt;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("StopAsync called");
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _logger.LogTrace("OnStarted called.");
            _appLifetime.StopApplication();
        }

        private void OnStopping()
        {
            _logger.LogTrace("OnStopping called.");
        }

        private void OnStopped()
        {
            _logger.LogTrace("OnStopped called.");
        }

    }
}
