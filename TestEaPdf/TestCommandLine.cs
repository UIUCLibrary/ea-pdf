using CommandLine;
using EaPdfCmd;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UIUCLibrary.EaPdf.Helpers;
using System.Collections.Generic;
using System.Linq;
using MimeKit;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestCommandLine
    {
        //capture the console output here
        private StringBuilder ConsoleOutput { get; set; } = new StringBuilder();

        string testFilesBaseInputDirectory = @"D:\EmailsForTesting\CommandLineTests";

        string startingDir = Directory.GetCurrentDirectory();

        [TestInitialize]
        public void Init()
        {
            //Redirect the console output to a string builder
            Console.SetOut(new StringWriter(this.ConsoleOutput));
            this.ConsoleOutput.Clear();

            Directory.SetCurrentDirectory(testFilesBaseInputDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            //Restore the console output and write the output to the console
            var stdOut = new StreamWriter(Console.OpenStandardOutput());
            stdOut.AutoFlush = true;
            Console.SetOut(stdOut);
            Debug.Write(this.ConsoleOutput.ToString());

            Directory.SetCurrentDirectory(startingDir);
        }

        //make sure the in and out arguments are [0],[1] and [2],[3], repectively
        [DataRow("--in", @"mboxes\test_a.mbox", "--out", @"out\mboxes_file",        "--global-id", "mailto:thabing@illinois.edu", DisplayName = "MBOX_FILE")]
        [DataRow("--in", @"mboxes",             "--out", @"out\mboxes_folder",      "--global-id", "mailto:thabing@illinois.edu", DisplayName = "MBOX_FOLDER")]
        [DataRow("--in", @"emls\test_a.eml",    "--out", @"out\emls_file",          "--global-id", "mailto:thabing@illinois.edu", DisplayName = "EML_FILE")]
        [DataRow("--in", @"emls",               "--out", @"out\emls_folder",        "--global-id", "mailto:thabing@illinois.edu", DisplayName = "EML_FOLDER")]

        [DataRow("--in", @"mboxes\test_a.mbox", "--out", @"out\mboxes_file_m",      "--global-id", "mailto:thabing@illinois.edu", "-m", "True", DisplayName = "MBOX_FILE_MULTI")]
        [DataRow("--in", @"mboxes",             "--out", @"out\mboxes_folder_m",    "--global-id", "mailto:thabing@illinois.edu", "-m", "True", DisplayName = "MBOX_FOLDER_MULTI")]
        [DataRow("--in", @"emls\test_a.eml",    "--out", @"out\emls_file_m",        "--global-id", "mailto:thabing@illinois.edu", "-m", "True", DisplayName = "EML_FILE_MULTI")]
        [DataRow("--in", @"emls",               "--out", @"out\emls_folder_m",      "--global-id", "mailto:thabing@illinois.edu", "-m", "True", DisplayName = "EML_FOLDER_MULTI")]

        [DataRow("--in", @"mboxes\test_a.mbox", "--out", @"out\mboxes_file_s",      "--global-id", "mailto:thabing@illinois.edu", "-s", "True", DisplayName = "MBOX_FILE_SUB")]
        [DataRow("--in", @"mboxes",             "--out", @"out\mboxes_folder_s",    "--global-id", "mailto:thabing@illinois.edu", "-s", "True", DisplayName = "MBOX_FOLDER_SUB")]
        [DataRow("--in", @"emls\test_a.eml",    "--out", @"out\emls_file_s",        "--global-id", "mailto:thabing@illinois.edu", "-s", "True", DisplayName = "EML_FILE_SUB")]
        [DataRow("--in", @"emls",               "--out", @"out\emls_folder_s",      "--global-id", "mailto:thabing@illinois.edu", "-s", "True", DisplayName = "EML_FOLDER_SUB")]

        [DataRow("--in", @"mboxes\test_a.mbox", "--out", @"out\mboxes_file_s_m",    "--global-id", "mailto:thabing@illinois.edu", "-s", "True", "-m", "True", DisplayName = "MBOX_FILE_SUB_MULTI")]
        [DataRow("--in", @"mboxes",             "--out", @"out\mboxes_folder_s_m",  "--global-id", "mailto:thabing@illinois.edu", "-s", "True", "-m", "True", DisplayName = "MBOX_FOLDER_SUB_MULTI")]
        [DataRow("--in", @"emls\test_a.eml",    "--out", @"out\emls_file_s_m",      "--global-id", "mailto:thabing@illinois.edu", "-s", "True", "-m", "True", DisplayName = "EML_FILE_SUB_MULTI")]
        [DataRow("--in", @"emls",               "--out", @"out\emls_folder_s_m",    "--global-id", "mailto:thabing@illinois.edu", "-s", "True", "-m", "True", DisplayName = "EML_FOLDER_SUB_MULTI")]

        [DataTestMethod]
        public void TestCommandLineOptions(params string[] args)
        {
            var inFilePath = Path.GetFullPath(args[1]);
            var outFilePath = Path.GetFullPath(args[3]);

            int dashM = args.ToList().IndexOf("-m");
            bool allowMultipleSourceFilesPerOutputFile = dashM > 0 && args[dashM + 1].Equals("True", StringComparison.OrdinalIgnoreCase);

            int dashS = args.ToList().IndexOf("-s");
            bool includeSubFolders = dashS > 0 && args[dashS + 1].Equals("True", StringComparison.OrdinalIgnoreCase);

            if (Directory.Exists(outFilePath))
                Directory.Delete(outFilePath, true);


            Task<int> tsk = EaPdfCmd.Program.Main(args);
            int ret = tsk.Result;

            Assert.AreEqual(0, ret, "Result: " + ((ReturnValue)ret).ToString());

            InputType type = MimeKitHelpers.DetermineInputType(inFilePath, includeSubFolders, out string message);

            if (type.IsMixedFolder() || type.IsUnknownFolder() || type == InputType.UnknownFile)
            {
                Assert.Fail($"Unsupported input type: {type}");
            }

            var mimeType = type.HasFlag(InputType.MboxFile) ? MimeFormat.Mbox : MimeFormat.Entity;

            var xmlFilePaths = Helpers.GetExpectedFiles(includeSubFolders, !allowMultipleSourceFilesPerOutputFile, inFilePath, outFilePath, false, type, mimeType);

            List<FileInfo> xmlFiles = xmlFilePaths.Select(f => new FileInfo(f)).ToList();


            foreach (FileInfo xmlFile in xmlFiles)
            {
                var pdfFile = new FileInfo(Path.ChangeExtension(xmlFile.FullName, ".pdf"));

                Assert.IsTrue(xmlFile.Exists);
                Assert.IsTrue(pdfFile.Exists);

                //make sure files were created within the last 1 minutes
                var fileAge = DateTime.Now.Subtract(xmlFile.LastWriteTime);
                Assert.IsTrue(fileAge.TotalMinutes < 5);

                fileAge = DateTime.Now.Subtract(pdfFile.LastWriteTime);
                Assert.IsTrue(fileAge.TotalMinutes < 5);
            }


            //FUTURE: other checks???
        }

        [TestMethod]
        public void TestSwitches()
        {
            string[] args = new string[] { "--in", "d:\\EmailsForTesting\\CommandLineTests\\emls", "--out", "d:\\EmailsForTesting\\CommandLineTests\\out\\", "-g", "mailto:thabing@gmail.com" };

            //Parse the command line arguments
            var argParser = new Parser(with =>
            {
                with.IgnoreUnknownArguments = false;
                with.CaseInsensitiveEnumValues = true;
                with.CaseSensitive = false;
                with.HelpWriter = null;
            });

            var argResults = argParser.ParseArguments<CommandLineParams>(args);

            Assert.IsNotNull(argResults);
            Assert.IsTrue(argResults.Tag == ParserResultType.Parsed);
            Assert.IsNull(argResults.Value.IncludeSubFolders);
            Assert.IsNull(argResults.Value.AllowMultipleSourceFilesPerOutputFile);

            args = new string[] { "--in", "d:\\EmailsForTesting\\CommandLineTests\\emls", "--out", "d:\\EmailsForTesting\\CommandLineTests\\out\\", "-g", "mailto:thabing@gmail.com", "-s", "true", "-m", "true" };
            argResults = argParser.ParseArguments<CommandLineParams>(args);
            Assert.IsNotNull(argResults);
            Assert.IsTrue(argResults.Tag == ParserResultType.Parsed);
            Assert.IsTrue(argResults.Value.IncludeSubFolders?.ToBoolean());
            Assert.IsTrue(argResults.Value.AllowMultipleSourceFilesPerOutputFile?.ToBoolean());

            args = new string[] { "--in", "d:\\EmailsForTesting\\CommandLineTests\\emls", "--out", "d:\\EmailsForTesting\\CommandLineTests\\out\\", "-g", "mailto:thabing@gmail.com", "-s", "false", "-m", "false" };
            argResults = argParser.ParseArguments<CommandLineParams>(args);
            Assert.IsNotNull(argResults);
            Assert.IsTrue(argResults.Tag == ParserResultType.Parsed);
            Assert.IsFalse(argResults.Value.IncludeSubFolders?.ToBoolean());
            Assert.IsFalse(argResults.Value.AllowMultipleSourceFilesPerOutputFile?.ToBoolean());

        }
    }
}
