using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers
{
    public static class ConfigHelpers
    {

        /// <summary>
        /// Return a string to use in the XMP metadata for the creator tool.
        /// </summary>
        public static string GetNamespaceVersionString(object obj)
        {
                var typ = obj.GetType();
                var product = typ.Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? typ.Namespace ?? typ.Name;
                var ver = typ.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
                int i = ver.IndexOf('+');
                if (i >= 0)
                {
                    ver = ver[..i];
                }

                return $"{product} {ver}";
        }


        /// <summary>
        /// If the source of the configuration is a physical file provider, make the path of the key absolute relative to the provider`s root. 
        /// If the source is not a physical file provider, the path is made absolute relative to the current directory.
        /// If the path is already absolute, leave it as is.
        /// </summary>
        /// <param name="configRoot"></param>
        /// <param name="key"></param>
        /// <returns>the absolute path</returns>
        public static string?  MakeConfigPathAbsolute(IConfiguration config, string key)
        {
            if(config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            string? xsltFoFilePath = config[key];

            if (config is IConfigurationRoot configRoot)
            {
                //need to reverse the order of the providers to get the last one that has the key, later keys override earlier ones
                var reversedProviders = configRoot.Providers.Reverse();
                if (reversedProviders.FirstOrDefault(p => p.TryGet(key, out xsltFoFilePath)) is FileConfigurationProvider fileProvider && xsltFoFilePath != null && !Path.IsPathFullyQualified(xsltFoFilePath))
                {
                    if (fileProvider.Source.FileProvider is PhysicalFileProvider physFileProvider)
                    {
                        var rootDir = physFileProvider.Root;
                        xsltFoFilePath = Path.Combine(rootDir, xsltFoFilePath);
                        fileProvider.Set(key, xsltFoFilePath);
                    }
                }
                else if (reversedProviders.FirstOrDefault(p => p.TryGet(key, out xsltFoFilePath)) is ConfigurationProvider provider && xsltFoFilePath != null && !Path.IsPathFullyQualified(xsltFoFilePath))
                {
                    xsltFoFilePath = Path.Combine(Directory.GetCurrentDirectory(), xsltFoFilePath);
                    provider.Set(key, xsltFoFilePath);
                }
            }

            return xsltFoFilePath;
        }
    }
}
