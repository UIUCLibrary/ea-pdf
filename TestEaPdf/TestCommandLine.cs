using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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

        [DataRow("--in", @"MBOXes\test_a.mbox", "--out", @"Out\MBOXes_1", "--global-id", "mailto:thabing@illinois.edu", "--config", @"C:\Users\thabi\Source\UIUC\ea-pdf\TestEaPdf\bin\Debug\net6.0\App.config", DisplayName = "MBOX_FILE")]
        [DataTestMethod]
        public void TestCommandLineOptions(params string[] args)
        {

            Task<int> tsk = EaPdfCmd.Program.Main(args);

            int ret = tsk.Result;

            Assert.AreEqual(0, ret);

        }

    }
}
