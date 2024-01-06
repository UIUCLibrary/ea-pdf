using System.IO;
using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace UIUCLibrary.TestEaPdf
{
    internal class Helpers
    {
        const string VERAPDF_PATH = "C:\\Users\\thabi\\verapdf\\verapdf.bat";

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

        public static string CalculateHash(string algName, byte[] byts)
        {
            using var alg = HashAlgorithm.Create(algName) ?? SHA256.Create(); //Fallback to know hash algorithm

            byte[] hash;
            try
            {
                hash = alg.ComputeHash(byts);
            }
            catch
            {
                hash = Array.Empty<byte>();
            }

            return Convert.ToHexString(hash);

        }

        public static string CalculateHash(string algName, byte[] byts, int offset, int count)
        {
            using var alg = HashAlgorithm.Create(algName) ?? SHA256.Create(); //Fallback to know hash algorithm

            byte[] hash;
            try
            {
                hash = alg.ComputeHash(byts, offset, count);
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

        public static int RunCmd(string cmdExec, string arguments, string workingDir, out string stdOut, out string stdErr)
        {
            var psi = new ProcessStartInfo
            {
                FileName = cmdExec,
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            if (!string.IsNullOrWhiteSpace(workingDir))
                psi.WorkingDirectory = workingDir;

            using var proc = new Process
            {
                StartInfo = psi
            };
            proc.Start();

            StringBuilder stdOutBldr = new();

            proc.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) stdOutBldr.AppendLine(e.Data);
            };

            proc.BeginOutputReadLine();

            StringBuilder stdErrBldr = new();

            proc.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data)) stdErrBldr.AppendLine(e.Data);
            };

            proc.BeginErrorReadLine();

            proc.WaitForExit();

            stdOut = stdOutBldr.ToString();
            stdErr = stdErrBldr.ToString();

            return proc.ExitCode;

        }

        public static void ValidatePdfAUsingVeraPdf(string pdfFilePath)
        {
            string args = $"\"{pdfFilePath}\"";
            string workDir = Path.GetDirectoryName(pdfFilePath) ?? ".";

            int ret = RunCmd(VERAPDF_PATH, args, workDir, out string stdOut, out string stdErr);

            if (ret != 0)
            {
                Debug.WriteLine(stdOut);
                Debug.WriteLine(stdErr);
            }

            Assert.AreEqual(0, ret);
            Assert.IsFalse(string.IsNullOrWhiteSpace(stdOut));
            Assert.IsTrue(string.IsNullOrWhiteSpace(stdErr));

            XmlDocument xresult = new();
            xresult.LoadXml(stdOut);

            XmlElement? reports = (XmlElement?)xresult.SelectSingleNode("/report/batchSummary/validationReports");

            if (reports != null)
            {
                var nonCompliant = reports.GetAttribute("nonCompliant");
                var failedJobs = reports.GetAttribute("failedJobs");

                Assert.AreEqual("0", nonCompliant);
                Assert.AreEqual("0", failedJobs);
            }

            var validationReports = xresult.SelectNodes("/report/jobs/job/validationReport");

            if (validationReports != null)
            {
                foreach (XmlElement validationReport in validationReports)
                {
                    var profileName = validationReport.GetAttribute("profileName");
                    var isCompliant = validationReport.GetAttribute("isCompliant");

                    Debug.WriteLine($"{profileName}, compliant: {isCompliant}");

                    Assert.IsTrue(profileName.StartsWith("PDF/A-3U", StringComparison.OrdinalIgnoreCase) || profileName.StartsWith("PDF/A-3A", StringComparison.OrdinalIgnoreCase));
                    Assert.AreEqual("true", isCompliant.ToLowerInvariant());
                }
            }

        }


    }
}
