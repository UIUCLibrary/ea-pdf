using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.CommandLine;
using UIUCLibrary.EaPdf;

//init the host
var host = Host.CreateDefaultBuilder(args)
    .Build();

//get the logger
var logger = host.Services.GetRequiredService<ILogger<EmailToXmlProcessor>>();


//set up command line options
var inFileFolder = new Option<FileSystemInfo>("--in", "The input email file or folder of email files to process")
{
    IsRequired = true
};
var outFolder = new Option<DirectoryInfo>("--out", "The output folder to write the results to")
{
    IsRequired = true
};
var globalId = new Option<Uri>("--globalId", "The global URI that identifies the email account")
{
    IsRequired = true
};
var addresses = new Option<IEnumerable<string>>("--address", "The email address(es) that belong to the account")
{
    IsRequired = false
};
var startAt = new Option<long>("--startAt", () => 0, "The starting integer to use for local message ids in the output")
{
    IsRequired = false,
};

//create the root command
var rootCommand = new RootCommand("Convert email files to XML");
rootCommand.Add(inFileFolder);
rootCommand.Add(outFolder);
rootCommand.Add(globalId);
rootCommand.Add(addresses);
rootCommand.Add(startAt);

rootCommand.SetHandler((mboxFilePath, outFolderPath, globalId, accntEmails, startingLocalId) => 
{
    int ret = 0; //Successful return value
    
    //TODO: Get settings from command line options
    var settings = new EmailToXmlProcessorSettings();

    var emailProc = new EmailToXmlProcessor(logger, settings);

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

