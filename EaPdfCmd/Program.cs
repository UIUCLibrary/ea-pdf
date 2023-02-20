using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.CommandLine;
using UIUCLibrary.EaPdf;



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

//TODO: Refine how loglevel is used, maybe use the more standard verbosity param https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#the---verbosity-option  
var logLevel = new Option<LogLevel>("--log-level", () => LogLevel.Information, "The logging level to use")
{
    IsRequired=false
};

//create the root command
var rootCommand = new RootCommand("Convert email files to XML");
rootCommand.Add(inFileFolder);
rootCommand.Add(outFolder);
rootCommand.Add(globalId);
rootCommand.Add(addresses);
rootCommand.Add(startAt);

//init the host
var host = Host.CreateDefaultBuilder(args)
    .Build();

//get the logger
var logger = host.Services.GetRequiredService<ILogger<EmailToEaxsProcessor>>();

rootCommand.SetHandler((mboxFilePath, outFolderPath, globalId, accntEmails, startingLocalId) => 
{
    int ret = 0; //Successful return value
    
    //TODO: Get settings from command line options
    var settings = new EmailToEaxsProcessorSettings();

    var emailProc = new EmailToEaxsProcessor(logger, settings);

    try
    {
        var started = DateTime.Now;
        logger.LogInformation($"Processing email in '{mboxFilePath}', output goes to '{outFolderPath}'.");
        var count = emailProc.ConvertMboxToEaxs(mboxFilePath.FullName, outFolderPath.FullName, globalId.ToString(), string.Join(",", accntEmails), startingLocalId);
        logger.LogInformation($"Processed {count} Messages in {DateTime.Now - started}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error converting mbox file to EAXS");
        ret = ex.HResult;
    }


    Task.FromResult(ret);
}, 
inFileFolder, outFolder, globalId, addresses, startAt);

var ret = await rootCommand.InvokeAsync(args);

host.Dispose();

return ret;

