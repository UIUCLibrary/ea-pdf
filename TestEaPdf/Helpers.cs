using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.TestEaPdf
{
    internal class Helpers
    {
        const string VERAPDF_PATH = "C:\\Users\\thabi\\verapdf_1.24\\verapdf.bat";

        public static string CalculateHash(string algName, string filePath)
        {
            byte[] hash = Array.Empty<byte>();

            using var alg = HashAlgorithm.Create(algName) ?? SHA256.Create(); //Fallback to know hash algorithm

            try
            {
                using var fstream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                hash = alg.ComputeHash(fstream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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


        public static List<string> GetExpectedFiles(bool includeSubs, bool oneFilePerMbox, string sampleFile, string outFolder, bool forceParse, InputFileType inFileType, MimeFormat mimeFormat)
        {
            List<string> expectedXmlFiles = new();

            if (File.Exists(sampleFile))
            {
                expectedXmlFiles.AddRange(GetExpectedFilesForFile(includeSubs, oneFilePerMbox, sampleFile, outFolder, forceParse, mimeFormat));
            }
            else if (Directory.Exists(sampleFile))
            {
                expectedXmlFiles.AddRange(GetExpectedFilesForDirectory(includeSubs, oneFilePerMbox, sampleFile, outFolder, forceParse, mimeFormat));
            }
            else
            {
                Assert.Fail($"Sample file or directory not found: {sampleFile}");
            }

            return expectedXmlFiles;

        }

        private static List<string> GetExpectedFilesForDirectory(bool includeSubs, bool oneFilePerMbox, string sampleDir, string outFolder, bool forceParse, MimeFormat mimeFormat)
        {
            List<string> expectedXmlFiles = new();


            if (oneFilePerMbox)
            {
                var files = Directory.GetFiles(sampleDir, "*", new EnumerationOptions() { RecurseSubdirectories = includeSubs });
                foreach (var file in files)
                {
                    expectedXmlFiles.AddRange(GetExpectedFilesForFile(includeSubs, oneFilePerMbox, file, outFolder, forceParse, mimeFormat));
                }
            }
            else
            {
                expectedXmlFiles.AddRange(GetExpectedFilesForFile(includeSubs, oneFilePerMbox, sampleDir, outFolder, forceParse, mimeFormat));
            }


            return expectedXmlFiles;
        }


        private static List<string> GetExpectedFilesForFile(bool includeSubs, bool oneFilePerMbox, string sampleFile, string outFolder, bool forceParse, MimeFormat mimeFormat)
        {
            List<string> expectedXmlFiles = new();

            string xmlPathStr = FilePathHelpers.GetXmlOutputFilePath(outFolder, sampleFile); 

            var inFileType = (InputFileType)MimeKitHelpers.DetermineInputType(sampleFile, out _);

            if (forceParse || MimeKitHelpers.DoesMimeFormatMatchInputFileType(mimeFormat, inFileType)) //If we are forcing the parse or if the file type matches what is expected, we expect the xml file to be created
            {
                Assert.IsTrue(File.Exists(xmlPathStr));
                expectedXmlFiles.Add(xmlPathStr);

                //Output might be split into multiple files
                var files = Directory.GetFiles(outFolder, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_????.xml");
                if (files != null)
                {
                    expectedXmlFiles.AddRange(files.Where(f => Regex.IsMatch(f, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_\\d{{4}}.xml")).ToList());
                }

                if (includeSubs && oneFilePerMbox)
                {
                    //The directory may be named something other than the mbox file, so we need to find the directory
                    //i.e. if the mbox file is "sample.mbox" or just "sample", the directory may be "sample.sbd"

                    var baseDirName = Path.GetFileNameWithoutExtension(sampleFile);
                    var baseDir = Path.GetDirectoryName(sampleFile);
                    var sampleSubDir = string.Empty;
                    if (baseDir != null)
                    {
                        var subDirs = Directory.GetDirectories(baseDir) ?? Array.Empty<string>();
                        sampleSubDir = subDirs.Where(d => Path.GetFileNameWithoutExtension(d).Equals(baseDirName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();
                    }

                    if (Directory.Exists(sampleSubDir))
                    {
                        //the input path is a directory, so make sure there is one xml output file for each input file
                        foreach (var file in Directory.GetFiles(sampleSubDir))
                        {
                            //TODO: If forceParse is false, do not include files that are not mbox or eml
                            if (forceParse || (InputFileType)MimeKitHelpers.DetermineInputType(file, includeSubs, out _) == inFileType)
                            {
                                xmlPathStr = Path.Combine(outFolder, Path.GetFileName(sampleSubDir), $"{Path.GetFileName(file)}.xml");
                                Assert.IsTrue(File.Exists(xmlPathStr));
                                expectedXmlFiles.Add(xmlPathStr);

                                //Output might be split into multiple files
                                files = Directory.GetFiles(outFolder, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_????.xml");
                                if (files != null)
                                {
                                    expectedXmlFiles.AddRange(files.Where(f => Regex.IsMatch(f, $"{Path.GetFileNameWithoutExtension(xmlPathStr)}_\\d{{4}}.xml")).ToList());
                                }
                            }
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

            Assert.AreEqual(0, ret, "VeraPdf validation failed; see output for details;");
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
