﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EaPdfCmd
{
    public static class Program
    {


        /// <summary>
        /// Gotta start somewhere
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task<int> Main(string[] args)
        {
            //init the host
            var hostBldr = Host.CreateApplicationBuilder(args);

            //load and validate the command line params and the configuration
            var ret = CommandLineHelpers.LoadCmdLineParamsAndConfig(hostBldr, args);
            if(ret != ReturnValue.OK)
            {
                if(ret == ReturnValue.HelpOrVersionRequest)
                {
                    return (int)ReturnValue.OK; //help or version request is always a success
                }
                else
                {
                    return (int)ret;
                }
            }

            //add the email processor as a hosted service
            hostBldr.Services.AddHostedService<EmailToEaPdfProcessor>();

            //build the host
            IHost host = hostBldr.Build();

            //get the processor for later use
            var processor = host.Services.GetService<IHostedService>() as EmailToEaPdfProcessor;

            await host.RunAsync();

            if (processor != null)
            {
                foreach (var (level, msg) in processor.Errors)
                {
                    Console.Error.WriteLine($"{level}: {msg}");
                }
            }

            return (int)(processor?.ExitCode ?? ReturnValue.UnexpectedError);
        }





    }
}
