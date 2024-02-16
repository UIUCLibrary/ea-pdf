using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using UIUCLibrary.EaPdf;
using UIUCLibrary.EaPdf.Helpers.Pdf;



//set up command line options
var inFileFolder = new Option<FileSystemInfo>("--in", "The input email file or folder of email files to process")
{
    IsRequired = true
};
var outFolder = new Option<DirectoryInfo>("--out", "The output folder to write the results to")
{
    IsRequired = true
};
var globalId = new Option<Uri>("--global-id", "The global URI that identifies the email account")
{
    IsRequired = true
};
var addresses = new Option<IEnumerable<string>>("--address", "The email address(es) that belong to the account")
{
    IsRequired = false
};
var startAt = new Option<long>("--start-at", () => 0, "The starting integer to use for local message ids in the output")
{
    IsRequired = false
};

var foProcessor = new Option<FoProcessor>("--fo", () => FoProcessor.ApacheFop, "The XSL-FO processor to use, defaults to Apache FOP")
{
    IsRequired = false
};


//TODO: Refine how loglevel is used, maybe use the more standard verbosity param https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#the---verbosity-option  
var logLevel = new Option<LogLevel>("--log-level", () => LogLevel.Information, "The logging level to use")
{
    IsRequired = false
};

var xmlConfigFile = new Option<string>("--config", "The XML configuration file to use")
{
    IsRequired = false
};

var fontsFolder = new Option<DirectoryInfo>("--fonts-folder", "The folder containing the fonts to use")
{
    IsRequired = false
};


//create the root command
var rootCommand = new RootCommand("Convert email files to XML")
{
    inFileFolder,
    outFolder,
    globalId,
    addresses,
    startAt,
    xmlConfigFile,
    fontsFolder,
    logLevel
};

//init the host
var hostBldr = Host.CreateDefaultBuilder(args);
hostBldr.ConfigureAppConfiguration((hostingContext, config) =>
{
    var configFilePath = rootCommand.Parse(args).GetValueForOption(xmlConfigFile);
    if (configFilePath != null)
    {
        if (!File.Exists(configFilePath))
        {
            throw new ArgumentException($"Configuration file not found: {configFilePath}");
        }
    }
    var ext = Path.GetExtension(configFilePath) ?? "";
    if (configFilePath != null && (ext.Equals(".xml", StringComparison.OrdinalIgnoreCase) || ext.Equals(".config", StringComparison.OrdinalIgnoreCase)))
    {
        config.AddXmlFile(configFilePath, optional: true);
    }
    else if (configFilePath != null && ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
    {
        config.AddJsonFile(configFilePath, optional: true);
    }
    else if (configFilePath != null)
    {
        throw new ArgumentException("Invalid configuration file; must be an .xml, .config, or .json file with one of these extensions.");
    }
});
var host = hostBldr.Build();

//get the logger
var logger = host.Services.GetRequiredService<ILogger<EmailToEaxsProcessor>>();

//get the configuration
var config = host.Services.GetRequiredService<IConfiguration>();

//set the command handler
rootCommand.SetHandler((mboxFilePath, outFolderPath, globalId, accntEmails, startingLocalId) => 
    Process(mboxFilePath, outFolderPath, globalId, accntEmails, startingLocalId),
    inFileFolder, outFolder, globalId, addresses, startAt);

//run the command
var ret = await rootCommand.InvokeAsync(args);

host.Dispose();

return ret;

/// <summary>Run the command</summary>
Task<int> Process(FileSystemInfo mboxFilePath, DirectoryInfo outFolderPath, Uri globalId, IEnumerable<string> accntEmails, long startingLocalId)
{

    if (!mboxFilePath.Exists)
    {
        logger.LogError("The --in '{mboxFilePath}' file or folder does not exist.", mboxFilePath);
        return Task.FromResult(1);
    }

    if(mboxFilePath is DirectoryInfo dir)
    {
    }

    int ret = 0; //Successful return value


    //Init settings from configuration
    var settings = new EmailToEaxsProcessorSettings(config);

    var emailProc = new EmailToEaxsProcessor(logger, settings);

    try
    {
        var started = DateTime.Now;
        logger.LogInformation("Processing email in '{mboxFilePath}', output goes to '{outFolderPath}'.", mboxFilePath, outFolderPath);
        var count = emailProc.ConvertMboxToEaxs(mboxFilePath.FullName, outFolderPath.FullName, globalId.ToString(), string.Join(",", accntEmails), startingLocalId);
        logger.LogInformation("Processed {count} Messages in {elapsedTime}", count, DateTime.Now - started);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error converting mbox file to EAXS");
        ret = ex.HResult;
    }


    return Task.FromResult(ret);
}
