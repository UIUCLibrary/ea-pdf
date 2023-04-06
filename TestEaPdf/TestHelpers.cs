using System.IO;
using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace UIUCLibrary.TestEaPdf
{
    internal class TestHelpers
    {

        public static string CalculateHash(string algName, string filePath)
        {
            byte[] hash = Array.Empty<byte>();

            using var alg = HashAlgorithm.Create(algName) ?? SHA256.Create(); //Fallback to know hash algorithm

            try
            {
                using var fstream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                hash = alg.ComputeHash(fstream);
            }
            catch
            {
                hash = Array.Empty<byte>();
            }

            return Convert.ToHexString(hash);

        }

        public static List<string> GetExpectedFiles(bool oneFilePerMbox, string sampleFile, string outFolder)
        {
            List<string> expectedXmlFiles = new();

            if (!oneFilePerMbox)
            {
                string xmlPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "xml"));
                Assert.IsTrue(File.Exists(xmlPathStr));
                expectedXmlFiles.Add(xmlPathStr);

                //Output might be split into multiple files
                var files = Directory.GetFiles(outFolder, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_????.xml");
                if (files != null)
                {
                    expectedXmlFiles.AddRange(files.Where(f => Regex.IsMatch(f, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_\\d{{4}}.xml")).ToList());
                }
            }
            else
            {
                if (Directory.Exists(sampleFile))
                {
                    //the input path is a directory, so make sure there is one xml output file for each input file
                    foreach (var file in Directory.GetFiles(sampleFile))
                    {
                        string xmlPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(file), "xml"));
                        Assert.IsTrue(File.Exists(xmlPathStr));
                        expectedXmlFiles.Add(xmlPathStr);

                        //Output might be split into multiple files
                        var files = Directory.GetFiles(outFolder, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_????.xml");
                        if (files != null)
                        {
                            expectedXmlFiles.AddRange(files.Where(f => Regex.IsMatch(f, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_\\d{{4}}.xml")).ToList());
                        }
                    }
                }

            }

            return expectedXmlFiles;
        }

        public static void CheckThatAllMessagesAreDraft(XmlDocument xdoc, XmlNamespaceManager xmlns)
        {
            var messages = xdoc.SelectNodes("/xm:Account/xm:Folder/xm:Message", xmlns);
            if (messages != null)
            {
                foreach (XmlElement message in messages)
                {
                    var draft = message.SelectSingleNode("xm:StatusFlag[normalize-space(text()) = 'Draft']", xmlns);
                    var deleted = message.SelectSingleNode("xm:StatusFlag[normalize-space(text()) = 'Deleted']", xmlns);
                    //if it is deleted, it may not be marked as draft even if it is in the draft folder
                    Assert.IsTrue(draft != null || deleted != null);
                }
            }
        }

    }
}
