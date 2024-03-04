using Extensions.Logging.ListOfString;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MimeKit;
using MimeKit.Encodings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using UIUCLibrary.EaPdf;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestEmailToXmlFunctions
    {
        ILogger<EmailToEaxsProcessor>? logger;
        ILoggerFactory? loggerFactory;
        bool validXml = true;
        string outFile = "xml_validation.out";
        readonly List<string> loggedLines = new();
        readonly LogLevel minLogLvl = LogLevel.Trace;

        string testFilesBaseInputDirectory = @"D:\EmailsForTesting\SampleFiles\Testing";

        [TestInitialize]
        public void InitTest()
        {
            //See https://codeburst.io/unit-testing-with-net-core-ilogger-t-e8c16c503a80#767c for how to use ILogger with unit testing
            //See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/ for more info on ILogger in .NET Core

            //log to the testing console standard error; log all message levels: trace, debug, ..., info
            loggerFactory = LoggerFactory.Create(builder => builder.AddConsole(opts => opts.LogToStandardErrorThreshold = minLogLvl).SetMinimumLevel(minLogLvl));

            //using StringListLogger for testing purposes https://www.nuget.org/packages/Extensions.Logging.ListOfString https://github.com/chrisfcarroll/TestBase
            loggerFactory.AddStringListLogger(loggedLines, false, (s, lvl) => { if (lvl >= minLogLvl) return true; else return false; });

            logger = loggerFactory.CreateLogger<EmailToEaxsProcessor>();
            logger.LogDebug("Starting Test");  //all loging done by the test scripts are debug level
        }

        [TestCleanup]
        public void EndTest()
        {
            if (logger != null)
            {
                logger.LogDebug("Information: {informationCount}", StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Information]")).Count());
                logger.LogDebug("Warnings: {warningCount}", StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Warning]")).Count());
                logger.LogDebug("Errors: {errorCount}", StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Error]")).Count());
                logger.LogDebug("Ending Test");
            }
            loggerFactory?.Dispose();
        }

        //The expected error, warning, and message counts were set by running the test scripts as of 2024-02-29

        //Gmail Exports
        [DataRow("Gmail\\Eml\\Inbox", "out_eml_Inbox", "SHA256", false, false, false, false, false, false, 0, 1, 330, DisplayName = "gmail-emls")] //gmail mbox export file
        [DataRow("Gmail\\Eml\\Inbox\\2016-06-23 143920 d3eb274969.eml", "out_eml_d3eb274969", "SHA256", false, false, false, false, false, false, 0, 0, 1, DisplayName = "gmail-emls-2016-06-23-143920-d3eb274969")] //gmail mbox export file with some weirdness 
        [DataRow("Gmail\\Eml\\Inbox\\2016-06-24 002410 57b3136fd3.eml", "out_eml_57b3136fd3", "SHA256", false, false, false, false, false, false, 0, 0, 1, DisplayName = "gmail-emls-2016-06-24-002410-57b3136fd3")] //gmail mbox export file with some html issues
        [DataRow("Gmail\\account.mbox", "out_account", "SHA256", false, false, false, false, false, false, 0, 1, 331, DisplayName = "gmail-mbox")] //gmail mbox export file

        //Mozilla mbox with child mboxes, different combinations of settings
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha1", "SHA1", false, false, false, false, false, false, 0, 0, 384, DisplayName = "moz-dlf-sha1")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256", "SHA256", false, false, false, false, false, false, 0, 0, 384, DisplayName = "moz-dlf-sha256")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext", "SHA256", true, false, false, false, false, false, 0, 0, 475, DisplayName = "moz-dlf-sha256-ext")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext---includeSubs", "SHA256", true, false, false, false, true, false, 9, 11, 852, DisplayName = "moz-dlf-sha256-ext---includeSubs")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext-wrap--includeSubs", "SHA256", true, true, false, false, true, false, 9, 11, 852, DisplayName = "moz-dlf-sha256-ext-wrap--includeSubs")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext-wrap", "SHA256", true, true, false, false, false, false, 0, 0, 475, DisplayName = "moz-dlf-sha256-ext-wrap")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext-wrap-presvEncBin", "SHA256", true, true, true, false, false, false, 0, 0, 475, DisplayName = "moz-dlf-sha256-ext-wrap-presvEncBin")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext-wrap-presvEncBoth", "SHA256", true, true, true, true, false, false, 0, 0, 475, DisplayName = "moz-dlf-sha256-ext-wrap-presvEncBoth")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext-wrap-presvEncText", "SHA256", true, true, false, true, false, false, 0, 0, 475, DisplayName = "moz-dlf-sha256-ext-wrap-presvEncText")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256----includeSubs", "SHA256", false, false, false, false, true, false, 9, 11, 705, DisplayName = "moz-dlf-sha256----includeSubs")]
        //Test maximum output file size
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256----includeSubs-10000000", "SHA256", false, false, false, false, true, false, 9, 11, 705, false, 10000000, DisplayName = "moz-dlf-sha256----includeSubs-10000000")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256---presvEncBin", "SHA256", false, false, true, false, false, false, 0, 0, 384, DisplayName = "moz-dlf-sha256---presvEncBin")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256---presvEncBoth", "SHA256", false, false, true, true, false, false, 0, 0, 384, DisplayName = "moz-dlf-sha256---presvEncBoth")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256---presvEncText", "SHA256", false, false, false, true, false, false, 0, 0, 384, DisplayName = "moz-dlf-sha256---presvEncText")]
        //Mozilla special files
        [DataRow("MozillaThunderbird\\Drafts", "out-moz-drafts", "SHA256", false, false, false, false, false, false, 0, 0, 26, DisplayName = "moz-drafts")]
        [DataRow("MozillaThunderbird\\Inbox", "out-moz-inbox", "SHA256", false, false, false, false, false, false, 0, 0, 21, DisplayName = "moz-inbox")]
        //Mozilla include subs and one file per file
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext--oneper-includeSubs", "SHA256", true, false, false, false, true, true, 9, 11, 852, DisplayName = "moz-dlf-sha256-ext--oneper-includeSubs")]

        //Pine mbox folder
        [DataRow("Pine", "out_pine-ext", "SHA256", true, false, false, false, false, false, 0, 69, 22910, DisplayName = "pine-folder-one-file-ext")]
        [DataRow("Pine", "out-pine", "SHA256", false, false, false, false, false, false, 0, 69, 20799, DisplayName = "pine-folder-one-file")]
        [DataRow("Pine", "out_pine-one", "SHA256", false, false, false, false, false, false, 0, 69, 20799, DisplayName = "pine-folder-one-file-in-subfolder")]
        [DataRow("Pine", "out_pine-many", "SHA256", false, false, false, false, false, true, 0, 69, 20799, DisplayName = "pine-folder-one-file-per-in-subfolder")]
        //Pine mbox files with special properties
        [DataRow("Pine\\sent-mail-aug-2007", "out_pine-sent-mail-aug-2007", "SHA256", false, false, false, false, false, false, 0, 6, 1301, DisplayName = "pine-sent-mail-aug-2007")] //not an mbox file
        [DataRow("Pine\\sent-mail-mar-2000", "out_pine-sent-mail-mar-2000", "SHA256", false, false, false, false, false, false, 0, 1, 100, DisplayName = "pine-sent-mail-mar-2000")] //incomplete message because of unmangled 'From ' line
        [DataRow("Pine\\sent-mail-jul-2006", "out_pine-sent-mail-jul-2006", "SHA256", false, false, false, false, false, false, 0, 0, 466, DisplayName = "pine-sent-mail-jul-2006")] //not an mbox file
        [DataRow("Pine\\sent-mail-jun-2004", "out_pine-sent-mail-jun-2004", "SHA256", false, false, false, false, false, false, 0, 1, 418, DisplayName = "pine-sent-mail-jun-2004")] //not an mbox file
        [DataRow("Pine\\sent-mail-jun-2000", "out_pine-sent-mail-jun-2000", "SHA256", false, false, false, false, false, false, 0, 0, 122, DisplayName = "pine-sent-mail-jun-2000")] //LF and UNKNOWN Eols

        //Weird Emails
        [DataRow("Weird\\missing_ext2.mbox", "out_missing_ext2", "SHA256", true, false, false, false, false, false, 0, 0, 2, DisplayName = "weird-missing-ext2-mbox")] //message from very large mbox seems to be missing external files
        [DataRow("Weird\\missing_ext.mbox", "out_missing_ext", "SHA256", true, false, false, false, false, false, 0, 0, 3, DisplayName = "weird-missing-ext-mbox")] //message from very large mbox seems to be missing external files
        [DataRow("Weird\\rfc822headers2.mbox", "out_rfc822headers2", "SHA256", true, false, false, false, false, false, 0, 0, 2, DisplayName = "weird-rfc822headers2-mbox")] //message from very large mbox which contains txt/rfc822-headers
        [DataRow("Weird\\rfc822headers.mbox", "out_rfc822headers", "SHA256", true, false, false, false, false, false, 0, 0, 2, DisplayName = "weird-rfc822headers-mbox")] //message from very large mbox which contains txt/rfc822-headers
        [DataRow("Weird\\spam_hexa.mbox", "out_hexa", "SHA256", false, false, false, false, false, false, 0, 2, 1, DisplayName = "weird-spam-hexa-mbox")] //weird spam email with 'hexa' encoded content
        [DataRow("Weird\\virus_notif.mbox", "out_notif", "SHA256", false, false, false, false, false, false, 0, 1, 2, DisplayName = "weird-virus-notif-mbox")] //weird virus notification with multipart/report, message/delivery-report. and text/rfc822-headers content types
        [DataRow("Weird\\virus_payload.mbox", "out_virus_payload", "SHA256", true, false, false, false, false, false, 0, 1, 2, DisplayName = "weird-virus-payload-mbox")] //message from very large mbox which contains a virus payload
        [DataRow("Weird\\bad-dates.mbox", "out_bad-dates", "SHA256", true, false, false, false, false, false, 0, 11, 70, DisplayName = "weird-bad-dates-mbox")] //messages with deliberately bad date formats, or missing dates

        //forceParse false, if file does not appear to be an mbox file, it will not be parsed
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext---includeSubs-noforce", "SHA256", true, false, false, false, true, false, 9, 2, 852, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "moz-dlf-sha256-ext---includeSubs-noforce")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext--oneper-includeSubs-noforce", "SHA256", true, false, false, false, true, true, 9, 2, 852, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "moz-dlf-sha256-ext--oneper-includeSubs-noforce")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext-wrap--includeSubs-noforce", "SHA256", true, true, false, false, true, false, 9, 2, 852, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "moz-dlf-sha256-ext-wrap--includeSubs-noforce")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256----includeSubs-noforce", "SHA256", false, false, false, false, true, false, 9, 2, 705, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "moz-dlf-sha256----includeSubs-noforce")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256----includeSubs-10000000-noforce", "SHA256", false, false, false, false, true, false, 9, 2, 705, false, 10000000, false, null, MimeFormat.Mbox, false, DisplayName = "moz-dlf-sha256----includeSubs-10000000-noforce")]

        [DataRow("Pine\\sent-mail-aug-2007", "out_pine-sent-mail-aug-2007-noforce", "SHA256", false, false, false, false, false, false, 0, 6, 1301, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "pine-sent-mail-aug-2007-noforce")] //not an mbox file
        [DataRow("Pine\\sent-mail-mar-2000", "out_pine-sent-mail-mar-2000-noforce", "SHA256", false, false, false, false, false, false, 0, 1, 100, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "pine-sent-mail-mar-2000-noforce")] //incomplete message because of unmangled 'From ' line
        [DataRow("Pine\\sent-mail-jul-2006", "out_pine-sent-mail-jul-2006-noforce", "SHA256", false, false, false, false, false, false, 0, 0, 466, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "pine-sent-mail-jul-2006-noforce")] //not an mbox file
        [DataRow("Pine\\sent-mail-jun-2004", "out_pine-sent-mail-jun-2004-noforce", "SHA256", false, false, false, false, false, false, 0, 1, 418, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "pine-sent-mail-jun-2004-noforce")] //not an mbox file
        [DataRow("Pine\\sent-mail-jun-2000", "out_pine-sent-mail-jun-2000-noforce", "SHA256", false, false, false, false, false, false, 0, 0, 122, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "pine-sent-mail-jun-2000-noforce")] //LF and UNKNOWN Eols

        //folder containing a mixture of mbox, eml, and junk files
        [DataRow("Gmail\\Eml\\Inbox_Mixed", "out_Inbox_Mixed_Force", "SHA256", false, false, false, false, false, false, 8, 8, 273, DisplayName = "mixed-files-force")] //mixture of eml, mbox, and junk files, each file is forced to parse
        [DataRow("Gmail\\Eml\\Inbox_Mixed", "out_Inbox_Mixed_NoForce", "SHA256", false, false, false, false, false, false, 8, 0, 273, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "mixed-files-noforce")] //mixture of eml, mbox, and junk files, invalid file will not be parsed parse
        [DataRow("Gmail\\Eml\\Inbox_Mixed", "out_Inbox_Mixed_OnePer_Force", "SHA256", false, false, false, false, false, true, 8, 8, 273, DisplayName = "mixed-files-oneper-force")] //mixture of eml, mbox, and junk files, each file is forced to parse
        [DataRow("Gmail\\Eml\\Inbox_Mixed", "out_Inbox_Mixed_OnePer_NoForce", "SHA256", false, false, false, false, false, true, 8, 0, 273, false, 0, false, null, MimeFormat.Mbox, false, DisplayName = "mixed-files-oneper-noforce")] //mixture of eml, mbox, and junk files, invalid file will not be parsed parse

        [DataTestMethod]
        public void TestSampleMboxFiles
            (
            string relInPath,
            string relOutPath,
            string hashAlg,
            bool extContent,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc,
            bool includeSubs,
            bool oneFilePerMbox,
            int expectedErrors,
            int expectedWarnings,
            int expectedCounts, //default to check everything
            bool quick = false, //default to no max
            long maxOutFileSize = 0,
            bool xhtml = false, //default to not creating the xhtml content
            string? oneMsgId = null, //just process the one message with this id 
            MimeFormat format = MimeFormat.Mbox,
            bool forceParse = true //default to forcing the parser to parse the file even if it doesn't look like the right format
            )
        {

            testFilesBaseInputDirectory = Path.GetDirectoryName(Path.Combine(testFilesBaseInputDirectory, relInPath)) ?? testFilesBaseInputDirectory;
            string testFileName = Path.GetFileName(relInPath);

            if (logger != null)
            {
                //parse the skips string to get the skipUntilMessageId and skipAfterMessageId
                string? skipUntilMessageId = oneMsgId;
                string? skipAfterMessageId = oneMsgId;

                var settings = new EmailToEaxsProcessorSettings()
                {
                    HashAlgorithmName = hashAlg,
                    SaveAttachmentsAndBinaryContentExternally = extContent,
                    WrapExternalContentInXml = wrapExtInXml,
                    PreserveBinaryAttachmentTransferEncodingIfPossible = preserveBinaryEnc,
                    PreserveTextAttachmentTransferEncoding = preserveTextEnc,
                    IncludeSubFolders = includeSubs,
                    OneFilePerMessageFile = oneFilePerMbox,
                    MaximumXmlFileSize = maxOutFileSize,
                    SaveTextAsXhtml = xhtml,
                    SkipUntilMessageId = skipUntilMessageId,
                    SkipAfterMessageId = skipAfterMessageId,
                    ForceParse = forceParse
                };

                //Also save the sample test files in the test project and automate the folder setup and cleanup
                var sampleFile = Path.Combine(testFilesBaseInputDirectory, testFileName);

                (string expectedOutFolder, string outFolder) = GetOutFolder(sampleFile, relOutPath);

                //clean out the output folder
                if (!string.IsNullOrWhiteSpace(relOutPath) && Directory.Exists(outFolder))
                {
                    Directory.Delete(outFolder, true);
                }

                var eProc = new EmailToEaxsProcessor(logger, settings);

                var validMessageCount = ConvertMessagesAndCheckCounts(eProc, format, sampleFile, outFolder, expectedCounts);

                InputFileType ftype = format switch
                {
                    MimeFormat.Mbox => InputFileType.MboxFile,
                    MimeFormat.Entity => InputFileType.EmlFile,
                    _ => InputFileType.UnknownFile
                };

                List<string> expectedXmlFiles = CheckOutputFolderAndGetXmlFiles(expectedOutFolder, outFolder, sampleFile, includeSubs, oneFilePerMbox, forceParse, ftype, format);

                if (quick) return;

                ValidateXmlFiles(logger, expectedXmlFiles, outFolder, settings, hashAlg, expectedErrors, expectedWarnings, validMessageCount, extContent, wrapExtInXml, preserveBinaryEnc, preserveTextEnc, relInPath, forceParse);

            }
            else
            {
                Assert.Fail("Logger was not initialized");
            }
        }

        //An EML file without the 'From ' header should not work if parsed with the Mbox parser
        [DataRow("Gmail\\true_eml_files\\2016-06-23 135245 d87d0cbbd2.eml", "..\\out_eml_as_mbox_force", "SHA256", false, false, false, false, false, false, 1, 1, 0, true, DisplayName = "parse-eml-as-mbox-force")] //EML file without the mbox 'From ' line, force parse
        [DataRow("Gmail\\true_eml_files\\2016-06-23 135245 d87d0cbbd2.eml", "..\\out_eml_as_mbox", "SHA256", false, false, false, false, false, false, 1, 0, 0, false, DisplayName = "parse-eml-as-mbox")] //EML file without the mbox 'From ' line

        [DataTestMethod]
        public void TestEmlAsMbox(
            string relInPath,
            string relOutPath,
            string hashAlg,
            bool extContent,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc,
            bool includeSub,
            bool oneFilePerMbox,
            int expectedErrors,
            int expectedWarnings,
            int expectedCounts, //default to check everything
            bool forceParse, //default to forcing the parser to parse the file even if it doesn't look like the right format
            bool quick = false, //default to no max
            long maxOutFileSize = 0,
            bool xhtml = false, //default to not creating the xhtml content
            string? oneMsgId = null //just process the one message with this id 
            )
        {
            MimeFormat format = MimeFormat.Mbox;
            TestSampleMboxFiles(relInPath, relOutPath, hashAlg, extContent, wrapExtInXml, preserveBinaryEnc, preserveTextEnc, includeSub, oneFilePerMbox, expectedErrors, expectedWarnings, expectedCounts,
                quick, maxOutFileSize, xhtml, oneMsgId, format, forceParse);

            var wrn = loggedLines.Where(l => l.Contains($"does not appear to be an '{MimeKitHelpers.GetFormatName(format)}' file"));
            Assert.IsNotNull(wrn);
            Assert.AreEqual(1, wrn.Count());

            var wrn2 = loggedLines.Where(l => l.Contains("Failed to find mbox From marker"));
            Assert.IsNotNull(wrn2);
            if (forceParse)
            {
                Assert.AreEqual(1, wrn2.Count());
            }
            else
            {
                Assert.AreEqual(0, wrn2.Count());
            }


        }

        //An mbox file with the 'From ' header should not work if parsed with the Entity parser
        //The Entity parser will correctly parse the first message in the file, but will fail to parse the second message, instead the Epilogue of the first messages body will contain all the text from the remaining messages
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_DLF_Entity_Force", "SHA256", false, false, false, false, false, false, 1, 2, 1, true, DisplayName = "parse-mbox-as-eml-force")] //mbox file with the 'From ' lines separating the messages, force parsing
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_DLF_Entity", "SHA256", false, false, false, false, false, false, 1, 0, 0, false, DisplayName = "parse-mbox-as-eml")] //mbox file with the 'From ' lines separating the messages

        [DataTestMethod]
        public void TestMboxAsEml(
            string relInPath,
            string relOutPath,
            string hashAlg,
            bool extContent,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc,
            bool includeSub,
            bool oneFilePerMbox,
            int expectedErrors,
            int expectedWarnings,
            int expectedCounts, //default to check everything
            bool forceParse, //default to forcing the parser to parse the file even if it doesn't look like the right format
            bool quick = false, //default to no max
            long maxOutFileSize = 0,
            bool xhtml = false, //default to not creating the xhtml content
            string? oneMsgId = null //just process the one message with this id 
            )
        {
            MimeFormat format = MimeFormat.Entity;
            TestSampleMboxFiles(relInPath, relOutPath, hashAlg, extContent, wrapExtInXml, preserveBinaryEnc, preserveTextEnc, includeSub, oneFilePerMbox, expectedErrors, expectedWarnings, expectedCounts,
                quick, maxOutFileSize, xhtml, oneMsgId, format, forceParse);

            var wrn = loggedLines.Where(l => l.Contains($"does not appear to be an '{MimeKitHelpers.GetFormatName(format)}' file"));
            Assert.IsNotNull(wrn);
            Assert.AreEqual(1, wrn.Count());

            var err = loggedLines.Where(l => l.Contains("The file may be corrupt"));
            Assert.IsNotNull(err);
            if (forceParse)
            {
                Assert.AreEqual(1, err.Count());
            }
            else
            {
                Assert.AreEqual(0, err.Count());
            }
        }


        //When the MBOX file has leading whitespace or other junk text before the first 'From ' line, the MimeKit parser will skip over the whitespace and junk and parse the file correctly
        [DataRow("MozillaThunderbird\\Inbox_leading_whitespaces", "out_Mbox_Leading_Junk", "SHA256", false, false, false, false, false, false, 0, 1, 21, DisplayName = "parse-mbox-with-leading-sp")] //mbox file with leading whitespace before the first 'From ' lines separating the messages

        [DataTestMethod]
        public void TestMboxWithLeadingWhitespace(
            string relInPath,
            string relOutPath,
            string hashAlg,
            bool extContent,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc,
            bool includeSub,
            bool oneFilePerMbox,
            int expectedErrors,
            int expectedWarnings,
            int expectedCounts, //default to check everything
            bool quick = false, //default to no max
            long maxOutFileSize = 0,
            bool xhtml = false, //default to not creating the xhtml content
            string? oneMsgId = null //just process the one message with this id 
            )
        {
            MimeFormat format = MimeFormat.Mbox;
            TestSampleMboxFiles(relInPath, relOutPath, hashAlg, extContent, wrapExtInXml, preserveBinaryEnc, preserveTextEnc, includeSub, oneFilePerMbox, expectedErrors, expectedWarnings, expectedCounts,
                quick, maxOutFileSize, xhtml, oneMsgId, format);

            var wrn = loggedLines.Where(l => l.Contains("the leading characters were ignored"));
            Assert.IsNotNull(wrn);
            Assert.AreEqual(1, wrn.Count());


        }

        //When the EML file has leading whitespace before the first headers, the MimeKit parser will fail with a "FormatException: End of stream" and the email converter will treat the whole message,
        //including subsequent headers, as a single body w/o any headers at all.  There will also be warnings about the missing headers
        [DataRow("Gmail\\leading_white_space.eml", "out_Leading_Junk_Force", "SHA256", false, false, false, false, false, false, 1, 4, 1, true, DisplayName = "parse-eml-with-leading-sp-force")] //eml file with leading whitespace before the first header line, force parsing
        [DataRow("Gmail\\leading_white_space.eml", "out_Leading_Junk", "SHA256", false, false, false, false, false, false, 1, 0, 0, false, DisplayName = "parse-eml-with-leading-sp")] //eml file with leading whitespace before the first header line

        [DataTestMethod]
        public void TestEmlWithLeadingWhitespace(
            string relInPath,
            string relOutPath,
            string hashAlg,
            bool extContent,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc,
            bool includeSub,
            bool oneFilePerMbox,
            int expectedErrors,
            int expectedWarnings,
            int expectedCounts, //default to check everything
            bool forceParse, //default to forcing the parser to parse the file even if it doesn't look like the right format
            bool quick = false, //default to no max
            long maxOutFileSize = 0,
            bool xhtml = false, //default to not creating the xhtml content
            string? oneMsgId = null //just process the one message with this id 
            )
        {

            string tempBase = testFilesBaseInputDirectory;

            MimeFormat format = MimeFormat.Entity;
            //Settings.ForceParse = true;
            TestSampleMboxFiles(relInPath, relOutPath, hashAlg, extContent, wrapExtInXml, preserveBinaryEnc, preserveTextEnc, includeSub, oneFilePerMbox, expectedErrors, expectedWarnings, expectedCounts,
                quick, maxOutFileSize, xhtml, oneMsgId, format, forceParse);

            var wrn = loggedLines.Where(l => l.Contains($"does not appear to be an '{MimeKitHelpers.GetFormatName(format)}' file"));
            Assert.IsNotNull(wrn);
            Assert.AreEqual(1, wrn.Count());

            var err = loggedLines.Where(l => l.Contains("FormatException: End of stream"));
            Assert.IsNotNull(err);
            if (forceParse)
            {
                Assert.AreEqual(1, err.Count());
            }
            else
            {
                Assert.AreEqual(0, err.Count());
            }


        }



        //A single normal EML file 
        [DataRow("Gmail\\true_eml_files\\2016-06-23 135245 d87d0cbbd2.eml", "..\\out_eml_one", "SHA256", false, false, false, false, false, false, 0, 0, 1, DisplayName = "gmail-eml")] //EML file 

        //A folder of normal EML files 
        [DataRow("Gmail\\true_eml_files", "out_eml", "SHA256", false, false, false, false, false, false, 0, 0, 10, DisplayName = "gmail-folder-eml")] //Folder of EML files, one output file
        [DataRow("Gmail\\true_eml_files", "out_eml_ext_wrap", "SHA256", true, true, false, false, false, false, 0, 0, 10, DisplayName = "gmail-folder-eml-ext-wrap")] //Folder of EML files, one output file, with external content wrapped in XML
        [DataRow("Gmail\\true_eml_files", "out_eml_one_per", "SHA256", false, false, false, false, false, true, 0, 0, 10, DisplayName = "gmail-folder-eml-one-per")] //Folder of EML files, one output file per input file

        //A folder of normal EML files including sub folders
        [DataRow("Gmail\\true_eml_files", "out_eml_subs", "SHA256", false, false, false, false, true, false, 0, 0, 20, DisplayName = "gmail-folder-subs-eml")] //Folder of EML files, one output file
        [DataRow("Gmail\\true_eml_files", "out_eml_subs_ext_wrap", "SHA256", true, true, false, false, true, false, 0, 0, 20, DisplayName = "gmail-folder-subs-eml-ext-wrap")] //Folder of EML files, one output file, with external content wrapped in XML
        [DataRow("Gmail\\true_eml_files", "out_eml_subs_one_per", "SHA256", false, false, false, false, true, true, 0, 0, 20, DisplayName = "gmail-folder-subs-eml-one-per")] //Folder of EML files, one output file per input file

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Set ForceParse to false and process folder with some junk files, if file does not appear to be an eml file, it will not be parsed
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //A folder of normal EML and junk files 
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_junk_noforce", "SHA256", false, false, false, false, false, false, 1, 0, 10, false, 0, false, null, false, DisplayName = "gmail-folder-eml-junk-noforce")] //Folder of EML files, one output file
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_ext_wrap_junk_noforce", "SHA256", true, true, false, false, false, false, 1, 0, 10, false, 0, false, null, false, DisplayName = "gmail-folder-eml-ext-wrap-junk-noforce")] //Folder of EML files, one output file, with external content wrapped in XML
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_one_per_junk_noforce", "SHA256", false, false, false, false, false, true, 1, 0, 10, false, 0, false, null, false, DisplayName = "gmail-folder-eml-one-per-junk-noforce")] //Folder of EML files, one output file per input file

        //A folder of normal EML files and junk including sub folders
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_subs_junk_noforce", "SHA256", false, false, false, false, true, false, 3, 0, 20, false, 0, false, null, false, DisplayName = "gmail-folder-subs-eml-junk-noforce")] //Folder of EML files, one output file
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_subs_ext_wrap_junk_noforce", "SHA256", true, true, false, false, true, false, 3, 0, 20, false, 0, false, null, false, DisplayName = "gmail-folder-subs-eml-ext-wrap-junk-noforce")] //Folder of EML files, one output file, with external content wrapped in XML
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_subs_one_per_junk_noforce", "SHA256", false, false, false, false, true, true, 3, 0, 20, false, 0, false, null, false, DisplayName = "gmail-folder-subs-eml-one-per-junk-noforce")] //Folder of EML files, one output file per input file

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Set ForceParse to true and process folder with some junk files, if file does not appear to be an eml file, it will be parsed anyway
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //A folder of normal EML and junk files 
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_junk_force", "SHA256", false, false, false, false, false, false, 1, 1, 10, false, 0, false, null, true, DisplayName = "gmail-folder-eml-junk-force")] //Folder of EML files, one output file
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_ext_wrap_junk_force", "SHA256", true, true, false, false, false, false, 1, 1, 10, false, 0, false, null, true, DisplayName = "gmail-folder-eml-ext-wrap-junk-force")] //Folder of EML files, one output file, with external content wrapped in XML
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_one_per_junk_force", "SHA256", false, false, false, false, false, true, 1, 1, 10, false, 0, false, null, true, DisplayName = "gmail-folder-eml-one-per-junk-force")] //Folder of EML files, one output file per input file

        //A folder of normal EML files and junk including sub folders
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_subs_junk_force", "SHA256", false, false, false, false, true, false, 3, 3, 20, false, 0, false, null, true, DisplayName = "gmail-folder-subs-eml-junk-force")] //Folder of EML files, one output file
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_subs_ext_wrap_junk_force", "SHA256", true, true, false, false, true, false, 3, 3, 20, false, 0, false, null, true, DisplayName = "gmail-folder-subs-eml-ext-wrap-junk-force")] //Folder of EML files, one output file, with external content wrapped in XML
        [DataRow("Gmail\\true_eml_files_w_extra_junk", "out_eml_subs_one_per_junk_force", "SHA256", false, false, false, false, true, true, 3, 3, 20, false, 0, false, null, true, DisplayName = "gmail-folder-subs-eml-one-per-junk-force")] //Folder of EML files, one output file per input file


        [DataTestMethod]
        public void TestSampleEmlFile(
            string relInPath,
            string relOutPath,
            string hashAlg,
            bool extContent,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc,
            bool includeSub,
            bool oneFilePerMbox,
            int expectedErrors,
            int expectedWarnings,
            int expectedCounts, //default to check everything
            bool quick = false, //default to no max
            long maxOutFileSize = 0,
            bool xhtml = false, //default to not creating the xhtml content
            string? oneMsgId = null, //just process the one message with this id 
            bool forceParse = true //default to forcing the parser to parse the file even if it doesn't look like the right format
            )
        {
            TestSampleMboxFiles(relInPath, relOutPath, hashAlg, extContent, wrapExtInXml, preserveBinaryEnc, preserveTextEnc, includeSub, oneFilePerMbox, expectedErrors, expectedWarnings, expectedCounts,
                quick, maxOutFileSize, xhtml, oneMsgId, MimeFormat.Entity, forceParse);

        }


        //The expected error, warning, and message counts were set by running the test scripts as of 2024-02-29

        //Gmail Exports
        [DataRow("Gmail\\Eml\\Inbox", "out_Inbox", "SHA256", false, false, false, false, false, false, 0, 75, 330, DisplayName = "xhtml-gmail-emls")] //gmail mbox export file
        [DataRow("Gmail\\Eml\\Inbox\\2016-06-23 143920 d3eb274969.eml", "out_d3eb274969", "SHA256", false, false, false, false, false, false, 0, 1, 1, DisplayName = "xhtml-gmail-emls-2016-06-23-143920-d3eb274969")] //gmail mbox export file with some weirdness
        [DataRow("Gmail\\Eml\\Inbox\\2016-06-24 002410 57b3136fd3.eml", "out_57b3136fd3", "SHA256", false, false, false, false, false, false, 0, 1, 1, DisplayName = "xhtml-gmail-emls-2016-06-24-002410-57b3136fd3")] //gmail mbox export file with some html issues
        [DataRow("Gmail\\account.mbox", "out_account", "SHA256", false, false, false, false, false, false, 0, 75, 331, DisplayName = "xhtml-gmail-mbox")] //gmail mbox export file

        //Mozilla mbox with child mboxes, different combinations of settings
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha1", "SHA1", false, false, false, false, false, false, 0, 9, 384, DisplayName = "xhtml-moz-dlf-sha1")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256", "SHA256", false, false, false, false, false, false, 0, 9, 384, DisplayName = "xhtml-moz-dlf-sha256")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext", "SHA256", true, false, false, false, false, false, 0, 9, 475, DisplayName = "xhtml-moz-dlf-sha256-ext")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext---includeSubs", "SHA256", true, false, false, false, true, false, 9, 53, 852, DisplayName = "xhtml-moz-dlf-sha256-ext---includeSubs")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext-wrap--includeSubs", "SHA256", true, true, false, false, true, false, 9, 53, 852, DisplayName = "xhtml-moz-dlf-sha256-ext-wrap--includeSubs")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext-wrap", "SHA256", true, true, false, false, false, false, 0, 9, 475, DisplayName = "xhtml-moz-dlf-sha256-ext-wrap")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256-ext-wrap-presvEnc", "SHA256", true, true, true, false, false, false, 0, 9, 475, DisplayName = "xhtml-moz-dlf-sha256-ext-wrap-presvEnc")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256----includeSubs", "SHA256", false, false, false, false, true, false, 9, 53, 705, DisplayName = "xhtml-moz-dlf-sha256----includeSubs")]
        //Test maximum output file size
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256----includeSubs-10000000", "SHA256", false, false, false, false, true, false, 9, 53, 705, false, 10000000, DisplayName = "xhtml-moz-dlf-sha256----includeSubs-10000000")]
        [DataRow("MozillaThunderbird\\DLF Distributed Library", "out_dlf-sha256---presvEnc", "SHA256", false, false, true, false, false, false, 0, 9, 384, DisplayName = "xhtml-moz-dlf-sha256---presvEnc")]
        //Mozilla special files
        [DataRow("MozillaThunderbird\\Drafts", "out_drafts", "SHA256", false, false, false, false, false, false, 0, 0, 26, DisplayName = "xhtml-moz-drafts")]
        [DataRow("MozillaThunderbird\\Inbox", "out_inbox", "SHA256", false, false, false, false, false, false, 0, 15, 21, DisplayName = "xhtml-moz-inbox")]


        //Pine mbox folder
        [DataRow("Pine", "out_pine", "SHA256", false, false, false, false, false, false, 0, 583, 20799, DisplayName = "xhtml-pine-folder-one-file")]
        [DataRow("Pine", "out_pine-one", "SHA256", false, false, false, false, false, false, 0, 583, 20799, DisplayName = "xhtml-pine-folder-one-file-in-subfolder")]
        [DataRow("Pine", "out_pine-many", "SHA256", false, false, false, false, false, true, 0, 583, 20799, DisplayName = "xhtml-pine-folder-one-file-per-in-subfolder")]
        //Pine mbox files with special properties
        [DataRow("Pine\\sent-mail-aug-2007", "out_pine-sent-mail-aug-2007", "SHA256", false, false, false, false, false, false, 0, 278, 1301, DisplayName = "xhtml-pine-sent-mail-aug-2007")] //not an mbox file
        [DataRow("Pine\\sent-mail-jul-2006", "out_pine-sent-mail-jul-2006", "SHA256", false, false, false, false, false, false, 0, 1, 466, DisplayName = "xhtml-pine-sent-mail-jul-2006")] //not an mbox file
        [DataRow("Pine\\sent-mail-jun-2004", "out_pine-sent-mail-jun-2004", "SHA256", false, false, false, false, false, false, 0, 1, 418, DisplayName = "xhtml-pine-sent-mail-jun-2004")] //not an mbox file
        [DataRow("Pine\\sent-mail-mar-2000", "out_pine-sent-mail-mar-2000", "SHA256", false, false, false, false, false, false, 0, 1, 100, DisplayName = "xhtml-pine-sent-mail-mar-2000")] //incomplete message because of unmangled 'From ' line
        [DataRow("Pine\\sent-mail-jun-2000", "out_pine-sent-mail-jun-2000", "SHA256", false, false, false, false, false, false, 0, 0, 122, DisplayName = "xhtml-pine-sent-mail-jun-2000")] //LF and UNKNOWN Eols


        //Weird Emails
        [DataRow("Weird\\missing_ext2.mbox", "out_missing_ext2", "SHA256", true, false, false, false, false, false, 0, 0, 2, DisplayName = "xhtml-weird-missing-ext2-mbox")] //message from very large mbox seems to be missing external files
        [DataRow("Weird\\missing_ext.mbox", "out_missing_ext", "SHA256", true, false, false, false, false, false, 0, 0, 3, DisplayName = "xhtml-weird-missing-ext-mbox")] //message from very large mbox seems to be missing external files
        [DataRow("Weird\\rfc822headers2.mbox", "out_rfc822headers2", "SHA256", true, false, false, false, false, false, 0, 0, 2, DisplayName = "xhtml-weird-rfc822headers2-mbox")] //message from very large mbox which contains txt/rfc822-headers
        [DataRow("Weird\\rfc822headers.mbox", "out_rfc822headers", "SHA256", true, false, false, false, false, false, 0, 0, 2, DisplayName = "xhtml-weird-rfc822headers-mbox")] //message from very large mbox which contains txt/rfc822-headers
        [DataRow("Weird\\spam_hexa.mbox", "out_hexa", "SHA256", false, false, false, false, false, false, 0, 4, 1, DisplayName = "xhtml-weird-spam-hexa-mbox")] //weird spam email with 'hexa' encoded content
        [DataRow("Weird\\virus_notif.mbox", "out_notif", "SHA256", false, false, false, false, false, false, 0, 1, 2, DisplayName = "xhtml-weird-virus-notif-mbox")] //weird virus notification with multipart/report, message/delivery-report. and text/rfc822-headers content types
        [DataRow("Weird\\virus_payload.mbox", "out_virus_payload", "SHA256", true, false, false, false, false, false, 0, 2, 2, DisplayName = "xhtml-weird-virus-payload-mbox")] //message from very large mbox which contains a virus payload


        [DataTestMethod]
        public void TestSampleMboxFilesOutputXhtml
            (
            string relInPath,
            string relOutPath,
            string hashAlg,
            bool extContent,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc,
            bool includeSub,
            bool oneFilePerMbox,
            int expectedErrors,
            int expectedWarnings,
            int expectedCounts, //default to check everything
            bool quick = false,
            long maxOutFileSize = 0,
            bool xhtml = true, //default to not creating the xhtml content
            string? oneMsgId = null
            )
        {
            TestSampleMboxFiles(relInPath, relOutPath, hashAlg, extContent, wrapExtInXml, preserveBinaryEnc, preserveTextEnc, includeSub, oneFilePerMbox, expectedErrors, expectedWarnings, expectedCounts,
                quick, maxOutFileSize, xhtml, oneMsgId);
        }

        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_allx", "SHA256", true, false, false, false, false, false, 0, 99600, 248311,
            true, 0, true, DisplayName = "xhtml-gmail-ext-big-mbox")] //very large gmail mbox export file, save external content
        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_splitx", "SHA256", true, false, false, false, false, false, 0, 99600, 248311,
            true, 10000000, true, DisplayName = "xhtml-gmail-ext-big-mbox-10000000")] //very large gmail mbox export file, save external content, split at 10MB

        //messages with specific problems
        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_skipx", "SHA256", true, false, false, false, false, false, 0, 0, 1,
            false, 10000000, true, "d5c1fc93-14b1-48ef-985d-0fbb38878f7e@las1s04mta936.xt.local", DisplayName = "xhtml-skip1-gmail-ext-big-mbox-10000000")] //very large gmail mbox export file, save external content, split at 10MB, just one message
        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_skipx", "SHA256", true, false, false, false, false, false, 0, 1, 1,
            false, 10000000, true, "1E.44.44510.680AEB06@aj.mta1vrest.cc.prd.sparkpost", DisplayName = "xhtml-skip2-gmail-ext-big-mbox-10000000")] //very large gmail mbox export file, save external content, split at 10MB, just one message

        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_skipx", "SHA256", true, false, false, false, false, false, 0, 2, 1,
            false, 0, true, "2010051415093467A03C1ACC$945C3759F1@COMPUTER", DisplayName = "xhtml-xmlns-gmail-ext-big-mbox")] //invalid namespace declaration
        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_skipx", "SHA256", true, false, false, false, false, false, 0, 1, 1,
            false, 0, true, "4313980.1156434894479.JavaMail.root@fac1010", DisplayName = "xhtml-QUOT-gmail-ext-big-mbox")] //undefined character entity &QUOT; -- all upper case
        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_skipx", "SHA256", true, false, false, false, false, false, 0, 0, 1,
            false, 0, true, "ee4ca1ce-f044-4536-85d3-553b088bb1dc@las1s04mta905.xt.local", DisplayName = "xhtml-2head-gmail-ext-big-mbox")] //two head elements
        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_skipx", "SHA256", true, false, false, false, false, false, 0, 1, 1,
            false, 0, true, "A5.52.33171.CC105236@gx.mta2vrest.cc.prd.sparkpost", DisplayName = "xhtml-body-text-gmail-ext-big-mbox")] //text content in the body
        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_skipx", "SHA256", true, false, false, false, false, false, 0, 1, 1,
            false, 0, true, "0.1.BE.4E2.1D77914FC5A11B8.0@omptrans.mail.synchronybank.com", DisplayName = "xhtml-head-xmlns-gmail-ext-big-mbox")] //head element has an invalid xmlns attribute
        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_skipx", "SHA256", true, false, false, false, false, false, 0, 1, 1,
            false, 0, true, "4b3ed4f78bcaae4d31caac7d6c4c22c5884b5b09-20081988-110484621@google.com", DisplayName = "xhtml-head-text-xmlns-gmail-ext-big-mbox")] //head element has text
        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_skipx", "SHA256", true, false, false, false, false, false, 0, 1, 1,
            false, 0, true, "FredricPaulEditor-in-Chief.665xjq1q0.dshl@cmp-subscriptions.p0.com", DisplayName = "xhtml-html-text-xmlns-gmail-ext-big-mbox")] //html element has text



        [DataTestMethod]
        public void TestHugeFilesOutputXhtml
            (
            string relInPath,
            string relOutPath,
            string hashAlg,
            bool extContent,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc,
            bool includeSubs,
            bool oneFilePerMbox,
            int expectedErrors,
            int expectedWarnings,
            int expectedCounts, //default to check everything
            bool quick = false, //if true, it skips the xml validation
            long maxOutFileSize = 0,
            bool xhtml = true, //default to not creating the xhtml content
            string? oneMsgId = null,
            bool forceParse = true //TODO: run the tests with forceParse = false
            )
        {
            TestSampleMboxFiles(relInPath, relOutPath, hashAlg, extContent, wrapExtInXml, preserveBinaryEnc, preserveTextEnc, includeSubs, oneFilePerMbox, expectedErrors, expectedWarnings, expectedCounts,
                quick, maxOutFileSize, xhtml, oneMsgId);

            if (quick) //do the xml validation separately and store results in different outputfile
            {
                testFilesBaseInputDirectory = Path.GetDirectoryName(Path.Combine(testFilesBaseInputDirectory, relInPath)) ?? testFilesBaseInputDirectory;
                string testFileName = Path.GetFileName(relInPath);
                var sampleFile = Path.Combine(testFilesBaseInputDirectory, testFileName);
                (_, string outFolder) = GetOutFolder(sampleFile, relOutPath);

                validXml = ValidateXmlDocuments(includeSubs, oneFilePerMbox, sampleFile, outFolder, forceParse, InputFileType.MboxFile, MimeFormat.Mbox);

                Assert.IsTrue(validXml, "Invalid XML file.");

            }
        }

        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_all", "SHA256", true, false, false, false, false, false, 0, 99600, 248311,
            false, 0, false, DisplayName = "gmail-ext-big-mbox")] //very large gmail mbox export file, save external content
        [DataRow("D:\\EmailsForTesting\\GmailExport_2022-10-08\\All mail Including Spam and Trash-002.mbox", "out_split", "SHA256", true, false, false, false, false, false, 0, 99600, 248311,
            false, 10000000, false, DisplayName = "gmail-ext-big-mbox-10000000")] //very large gmail mbox export file, save external content, split at 10MB

        [DataTestMethod]
        public void TestHugeFiles
            (
            string relInPath,
            string relOutPath,
            string hashAlg,
            bool extContent,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc,
            bool includeSub,
            bool oneFilePerMbox,
            int expectedErrors,
            int expectedWarnings,
            int expectedCounts, //default to check everything
            bool quick = false,
            long maxOutFileSize = 0,
            bool xhtml = false, //default to not creating the xhtml content
            string? oneMsgId = null
            )
        {
            TestSampleMboxFiles(relInPath, relOutPath, hashAlg, extContent, wrapExtInXml, preserveBinaryEnc, preserveTextEnc, includeSub, oneFilePerMbox, expectedErrors, expectedWarnings, expectedCounts,
                quick, maxOutFileSize, xhtml, oneMsgId);
        }

        //Don't change the ouput folders, this is what is being tested
        [DataRow("MozillaThunderbird\\Drafts", "MozillaThunderbird\\folder", false, DisplayName = "path_check_drafts_folder")]
        [DataRow("MozillaThunderbird\\Drafts", "MozillaThunderbird\\\\Drafts.out", true, DisplayName = "path_check_drafts_out")]
        [DataRow("MozillaThunderbird\\Drafts", "MozillaThunderbird\\\\Drafts.out\\test", true, DisplayName = "path_check_drafts_out_test")]
        [DataTestMethod]
        public void TestPathErrorChecks(string inFilePath, string outFolderPath, bool shouldFail)
        {

            if (logger != null)
            {
                var settings = new EmailToEaxsProcessorSettings();
                var eProc = new EmailToEaxsProcessor(logger, settings);

                var inFile = Path.Combine(testFilesBaseInputDirectory, inFilePath);
                var outFolder = Path.Combine(testFilesBaseInputDirectory, outFolderPath);

                try
                {
                    var validMessageCount = eProc.ConvertMboxToEaxs(inFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                    if (shouldFail)
                    {
                        Assert.Fail("Expected an ArgumentException");
                    }
                }
                catch (ArgumentException aex)
                {
                    if (!shouldFail)
                    {
                        Assert.Fail(aex.Message);
                    }
                }
                finally
                {
                    //cleanup
                    if (Directory.Exists(outFolder))
                    {
                        Directory.Delete(outFolder, true);
                    }
                }
            }
        }


        private static long ConvertMessagesAndCheckCounts(EmailToEaxsProcessor eProc, MimeFormat format, string sampleFile, string outFolder, long expectedCounts)
        {
            long validMessageCount = 0;
            if (format == MimeFormat.Mbox && Directory.Exists(sampleFile))
            {
                validMessageCount = eProc.ConvertFolderOfMboxToEaxs(sampleFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                if (expectedCounts == -1)
                    Assert.IsTrue(validMessageCount > 0, "Expected some valid messages");
                else
                    Assert.AreEqual(expectedCounts, validMessageCount, "Expected valid message count does not match");
            }
            else if (format == MimeFormat.Mbox && File.Exists(sampleFile))
            {
                validMessageCount = eProc.ConvertMboxToEaxs(sampleFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                if (expectedCounts == -1)
                    Assert.IsTrue(validMessageCount > 0);
                else
                    Assert.AreEqual(expectedCounts, validMessageCount, "Expected valid message count does not match");
            }
            else if (format == MimeFormat.Entity && Directory.Exists(sampleFile))
            {
                validMessageCount = eProc.ConvertFolderOfEmlToEaxs(sampleFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                if (expectedCounts == -1)
                    Assert.IsTrue(validMessageCount > 0, "Expected some valid messages");
                else
                    Assert.AreEqual(expectedCounts, validMessageCount, "Expected valid message count does not match");
            }
            else if (format == MimeFormat.Entity && File.Exists(sampleFile))
            {
                validMessageCount = eProc.ConvertEmlToEaxs(sampleFile, outFolder, "mailto:thabing@illinois.edu", "thabing@illinois.edu,thabing@uiuc.edu");
                if (expectedCounts == -1)
                    Assert.IsTrue(validMessageCount > 0);
                else
                    Assert.AreEqual(expectedCounts, validMessageCount, "Expected valid message count does not match");
            }
            else
            {
                Assert.Fail($"Sample file or folder '{sampleFile}' does not exist");
            }

            return validMessageCount;
        }

        private List<string> CheckOutputFolderAndGetXmlFiles(string expectedOutFolder, string outFolder, string sampleFile, bool includeSubs, bool oneFilePerMbox, bool forceParse, InputFileType inFileType, MimeFormat mimeFormat)
        {
            //make sure output folders and files exist
            Assert.AreEqual(expectedOutFolder, outFolder);
            Assert.IsTrue(Directory.Exists(outFolder));

            string csvPathStr = Path.Combine(outFolder, Path.ChangeExtension(Path.GetFileName(sampleFile), "csv"));
            Assert.IsTrue(File.Exists(csvPathStr));

            List<string> expectedXmlFiles = Helpers.GetExpectedFiles(includeSubs, oneFilePerMbox, sampleFile, outFolder, forceParse, inFileType, mimeFormat);
            if (forceParse) //if forceParse is true, we expect all the files to be xml files
            {
                Assert.IsTrue(expectedXmlFiles.Count > 0, "Expected at least one xml file");
            }
            else if(expectedXmlFiles.Count == 0)
            {
               logger?.LogDebug("No xml files found in output folder '{outFolder}' for file '{sampleFile}'", outFolder, sampleFile); 
            }

            return expectedXmlFiles;
        }

        private void ValidateXmlFiles(ILogger logger,
            List<string> expectedXmlFiles,
            string outFolder,
            EmailToEaxsProcessorSettings settings,
            string hashAlg,
            long expectedErrors,
            long expectedWarnings,
            long validMessageCount,
            bool extContent,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc,
            string relInPath,
            bool forceParse
            )
        {
            ValidateErrorAndWarningCounts(expectedErrors, expectedWarnings);

            //make sure the xml files are valid
            foreach (var xmlFile in expectedXmlFiles)
            {
                logger.LogDebug("Validating xml file '{xmlFile}'", xmlFile);

                //use an XmlReader to validate with line numbers as soon as the Xml document is loaded
                XmlReaderSettings rdrSettings = new()
                {
                    Schemas = new XmlSchemaSet()
                };
                rdrSettings.Schemas.Add(EmailToEaxsProcessor.XM_NS, EmailToEaxsProcessor.XM_XSD);
                rdrSettings.Schemas.Add(EmailToEaxsProcessor.XM_NS, EmailToEaxsProcessor.XM_CC_XSD);
                rdrSettings.Schemas.Add(EmailToEaxsProcessor.XHTML_NS, EmailToEaxsProcessor.XHTML_XSD);

                rdrSettings.ValidationType = ValidationType.Schema;
                rdrSettings.ValidationEventHandler += XmlValidationEventHandler;
                rdrSettings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;


                validXml = true;
                using var xRdr = XmlReader.Create(xmlFile, rdrSettings);
                var xDoc = new XmlDocument(); //this will validate the XML
                try
                {
                    xDoc.Load(xRdr);
                }
                catch (XmlException xex)
                {
                    validXml = false;
                    Assert.Fail(xex.Message);
                }
                catch (Exception ex)
                {
                    Assert.Fail(ex.Message);
                }
                Assert.IsTrue(validXml, "XML Schema Validation");

                var xmlns = new XmlNamespaceManager(xDoc.NameTable);
                xmlns.AddNamespace(EmailToEaxsProcessor.XM, EmailToEaxsProcessor.XM_NS);

                //make sure the localId values in a each xml file all increase by 1
                ValidateLocalIds(xDoc, xmlns);

                //make sure hash values and sizes are correct for folder-level mbox files
                XmlNodeList? msgFilePropNodes = xDoc.SelectNodes("//xm:Folder/xm:FolderProperties[xm:RelPath]", xmlns);
                if (msgFilePropNodes != null)
                {
                    foreach (XmlElement msgFilePropElem in msgFilePropNodes)
                    {
                        //The FolderProperties/RelPath element contains the relative path to the source mbox file, relative to the file path to the XML file
                        //So we need to get the path to the XML file to use as the base path when combined with the RelPath relative path
                        var outDir = Path.GetDirectoryName(xmlFile);

                        Assert.IsTrue(Directory.Exists(outDir));

                        ValidateHash(msgFilePropElem, xmlns, outDir, settings, hashAlg);
                    }
                }

                //make sure hash values and sizes are correct for message-level eml files
                msgFilePropNodes = xDoc.SelectNodes("//xm:Message/xm:MessageProperties[xm:RelPath]", xmlns);
                if (msgFilePropNodes != null)
                {
                    foreach (XmlElement msgFilePropElem in msgFilePropNodes)
                    {
                        ValidateHash(msgFilePropElem, xmlns, outFolder, settings, hashAlg);
                    }
                }


                //if there are no messages in the file, we can skip the rest of the tests
                if (validMessageCount == 0)
                    return;

                //if there are multiple xml files, need to check each one for content, it might be the case that one file has content and the other doesn't because of an error or warning condition
                var message = xDoc.SelectSingleNode("/xm:Account//xm:Folder/xm:Message", xmlns);
                if (message == null)
                {
                    //if there are no messages in the file make sure there is a warning or error comment
                    var comment = xDoc.SelectSingleNode("/xm:Account//xm:Folder/comment()[starts-with(. , 'WARNING') or starts-with(. , 'ERROR')]", xmlns);
                    Assert.IsNotNull(comment);
                    return;
                }

                if (extContent)
                {
                    var outDir = Path.GetDirectoryName(xmlFile) ?? outFolder; //The ExtBodyContent/RelPath is relative to the XML file, so we need to get the path to the XML file to use as the base path when combined with the RelPath relative path
                    Assert.IsTrue(Directory.Exists(outDir));
                    ValidateExternalContent(xDoc, xmlns, outDir, settings, hashAlg, wrapExtInXml, preserveBinaryEnc, preserveTextEnc);
                }
                else
                {
                    ValidateInternalContent(xDoc, xmlns, preserveBinaryEnc, preserveTextEnc);
                }

                ValidateRfc822Nodes(xDoc, xmlns);

                ValidatePhantomBodies(xDoc, xmlns);

                if (!forceParse)
                {
                    ValidateSkippedFiles(xDoc, xmlns);
                }

                if (relInPath == "MozillaThunderbird\\Drafts")
                {
                    //make sure each message is marked as draft
                    Helpers.CheckThatAllMessagesAreDraft(xDoc, xmlns);
                }

                xRdr.Close();

            }

        }

        //make sure there are no empty folders for skipped files
        private static void ValidateSkippedFiles(XmlDocument xDoc, XmlNamespaceManager xmlns)
        {
            var foldersNoMessages = xDoc.SelectNodes("//xm:Folder[not(xm:Message) and not(xm:Folder)]", xmlns);
            if (foldersNoMessages != null)
            {
                foreach (XmlElement folder in foldersNoMessages)
                {
                    var comments = folder.SelectNodes("comment()[starts-with(. , 'WARNING:') or starts-with(. , 'ERROR:')]", xmlns);
                    if (comments != null)
                    {
                        foreach (XmlNode comment in comments)
                        {
                            Assert.IsFalse(comment.InnerText.Contains("does not appear to be an"));
                        }
                    }
                }
            }
        }

        private static void ValidatePhantomBodies(XmlDocument xDoc, XmlNamespaceManager xmlns)
        {
            //check that any PhantomBody elements are valid
            XmlNodeList? nds = xDoc.SelectNodes("//xm:SingleBody[xm:PhantomBody]", xmlns);
            if (nds != null)
            {
                foreach (XmlElement nd in nds)
                {
                    //check that the content-type is message/external-body or that there are X-Mozilla-* headers
                    var contentType = nd.SelectSingleNode("xm:ContentType", xmlns)?.InnerText;
                    var xMozillaExternal = nd.SelectSingleNode("xm:OtherMimeHeader/xm:Name['X-Mozilla-External-Attachment-URL']", xmlns);
                    Assert.IsTrue((!string.IsNullOrWhiteSpace(contentType) && contentType.Equals("message/external-body", StringComparison.OrdinalIgnoreCase)) || (xMozillaExternal != null));
                }
            }
            XmlNodeList? nds2 = xDoc.SelectNodes($"//xm:SingleBody[translate(xm:ContentType,'{XmlHelpers.UPPER}','{XmlHelpers.LOWER}') = 'message/external-body'] | //xm:SingleBody[translate(xm:OtherMimeHeader/xm:Name,'{XmlHelpers.UPPER}','{XmlHelpers.LOWER}') = 'x-mozilla-external-attachment-url']", xmlns);
            if (nds2 != null)
            {
                foreach (XmlElement nd in nds2)
                {
                    //check that there is a PhantomBody element
                    var phantom = nd.SelectSingleNode("xm:PhantomBody", xmlns);
                    Assert.IsNotNull(phantom);
                }
            }

        }

        private static void ValidateRfc822Nodes(XmlDocument xDoc, XmlNamespaceManager xmlns)
        {
            //If mime type is message/rfc822 or text/rfc822-headers, make sure there is a ChildMessage 
            var rfc822Nds = xDoc.SelectNodes($"//xm:SingleBody[translate(xm:ContentType,'{XmlHelpers.UPPER}','{XmlHelpers.LOWER}') = 'text/rfc822-headers' or translate(xm:ContentType,'{XmlHelpers.UPPER}','{XmlHelpers.LOWER}') = 'message/rfc822']", xmlns);
            if (rfc822Nds != null)
            {
                foreach (XmlElement nd in rfc822Nds)
                {
                    //var id = nd.SelectSingleNode("ancestor::xm:Message/xm:MessageId", xmlns)?.InnerText;
                    //logger.LogDebug($"RFC822 Message Id: {id}");

                    var encoding = nd.SelectSingleNode("xm:TransferEncoding", xmlns)?.InnerText ?? "";
                    if (!new[] { "7bit", "8bit", "binary", "" }.Contains(encoding))
                    {
                        //if the encoding is anything other than 7bit, 8bit, or binary, it is treated as normal BodyContent or ExtBodyContent
                        Assert.IsNotNull(nd.SelectSingleNode("xm:BodyContent | xm:ExtBodyContent", xmlns));
                        continue;
                    }

                    var childNds = nd.SelectNodes("xm:ChildMessage", xmlns);
                    Assert.IsNotNull(childNds);
                    Assert.AreEqual(1, childNds.Count);

                    //if the mime type is text/rfc822-headers, make sure the ChildMessage has no content, it might have a body but it should be empty
                    if (nd.SelectSingleNode("xm:ContentType", xmlns)?.InnerText.ToLower() == "text/rfc822-headers")
                    {
                        if (childNds[0] is XmlElement childMsg)
                        {
                            Assert.IsNull(childMsg.SelectSingleNode("xm:SingleBody/xm:BodyContent", xmlns));
                            Assert.IsNull(childMsg.SelectSingleNode("xm:SingleBody/xm:ExtBodyContent", xmlns));
                            Assert.IsNull(childMsg.SelectSingleNode("xm:SingleBody/xm:ChildMessage", xmlns));
                            Assert.IsNull(childMsg.SelectSingleNode("xm:SingleBody/xm:DeliveryStatus", xmlns));

                            Assert.IsNull(childMsg.SelectSingleNode("xm:MultiBody/xm:MultiBody", xmlns));
                            Assert.IsNull(childMsg.SelectSingleNode("xm:MultiBody/xm:SingleBody", xmlns));

                        }
                    }
                }
            }
        }

        private static void ValidateInternalContent(XmlDocument xDoc, XmlNamespaceManager xmlns, bool preserveBinaryEnc, bool preserveTextEnc)
        {
            //make sure all content is saved in the XML 
            XmlNodeList? nodes = xDoc.SelectNodes("/xm:Account//xm:Folder/xm:Message//xm:SingleBody", xmlns);
            Assert.IsTrue(nodes != null && nodes.Count > 0);
            foreach (XmlElement node in nodes)
            {

                //var id = node.SelectSingleNode("ancestor::xm:Message/xm:MessageId", xmlns)?.InnerText;
                //logger.LogDebug($"RFC822 Message Id: {id}");

                Assert.IsNull(node.SelectSingleNode("xm:ExtBodyContent", xmlns));

                //unless this is a 'text/rfc822-headers', make sure there is a body
                if (node.SelectSingleNode($"ancestor::xm:SingleBody[translate(xm:ContentType,'{XmlHelpers.UPPER}','{XmlHelpers.LOWER}') = 'text/rfc822-headers']", xmlns) == null)
                {
                    Assert.IsNotNull(node.SelectSingleNode("xm:BodyContent | xm:ChildMessage | xm:DeliveryStatus | xm:PhantomBody", xmlns));
                }
                else
                {
                    Assert.IsNull(node.SelectSingleNode("xm:BodyContent | xm:ChildMessage | xm:DeliveryStatus | xm:PhantomBody", xmlns));
                }


                //Test the preserve transfer encoding setting
                var enc = node.SelectSingleNode("xm:BodyContent/xm:TransferEncoding", xmlns)?.InnerText;
                var origEnc = node.SelectSingleNode("xm:TransferEncoding", xmlns)?.InnerText;
                if (preserveBinaryEnc && MimeKitHelpers.BinaryContentEncodings.Contains(origEnc, StringComparer.OrdinalIgnoreCase))
                {
                    if (enc != null)
                        Assert.AreEqual(origEnc, enc);
                }
                else if (preserveTextEnc && MimeKitHelpers.TextContentEncodings.Contains(origEnc, StringComparer.OrdinalIgnoreCase))
                {
                    if (enc != null)
                        Assert.AreEqual(origEnc, enc);
                }
                else
                {
                    //quoted-printable will be used if the content is text but has XML invalid control chars 
                    //otherwise it should be base64
                    if (enc != null)
                    {
                        var contentType = node.SelectSingleNode("xm:ContentType", xmlns)?.InnerText;

                        Assert.IsTrue(enc == "base64" || (enc == "quoted-printable" && contentType != null && contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)));
                    }
                }

            }
        }


        private void ValidateExternalContent(
            XmlDocument xDoc,
            XmlNamespaceManager xmlns,
            string outFolder,
            EmailToEaxsProcessorSettings settings,
            string hashAlg,
            bool wrapExtInXml,
            bool preserveBinaryEnc,
            bool preserveTextEnc)
        {
            //make sure all attachments or binary content are external and that the file exists and hashes match
            //get all SingleBody elements that don't have a child element ChildMessage or child element DeliveryStatus and are attachments or not text/... or message/... mime types
            XmlNodeList? extNodes = xDoc.SelectNodes($"/xm:Account//xm:Folder/xm:Message//xm:SingleBody[not(xm:ChildMessage) and not(xm:DeliveryStatus) and (xm:Disposition='attachment' or (not(starts-with(translate(xm:ContentType,'{XmlHelpers.UPPER}','{XmlHelpers.LOWER}'),'text/')) and not(starts-with(translate(xm:ContentType,'{XmlHelpers.UPPER}','{XmlHelpers.LOWER}'),'message/'))))]", xmlns);

            if (extNodes != null)
            {
                var extdoc = new XmlDocument();
                extdoc.Schemas.Add(EmailToEaxsProcessor.XM_NS, EmailToEaxsProcessor.XM_XSD);
                extdoc.Schemas.Add(EmailToEaxsProcessor.XM_NS, EmailToEaxsProcessor.XM_CC_XSD);
                extdoc.Schemas.Add(EmailToEaxsProcessor.XHTML_NS, EmailToEaxsProcessor.XHTML_XSD);

                foreach (XmlElement singleBodyNd in extNodes)
                {

                    Assert.IsNull(singleBodyNd.SelectSingleNode("xm:BodyContent", xmlns));
                    XmlElement? extNode = singleBodyNd.SelectSingleNode("xm:ExtBodyContent", xmlns) as XmlElement;
                    XmlElement? phantomNode = singleBodyNd.SelectSingleNode("xm:PhantomBody", xmlns) as XmlElement;


                    string? id = null;
                    if (extNode == null)
                    {
                        var msgIdNd = singleBodyNd.SelectSingleNode("ancestor::xm:Message/xm:MessageId", xmlns) as XmlElement;
                        id = msgIdNd?.InnerText;
                    }

                    Assert.IsTrue(extNode != null || phantomNode != null, $"Message Id: {id} should have external body content and/or phantom content");

                    if (extNode == null) //no need for further checks
                        continue;

                    string? extPath = extNode.SelectSingleNode("xm:RelPath", xmlns)?.InnerText;
                    Assert.IsFalse(string.IsNullOrWhiteSpace(extPath));
                    var extFilepath = Path.Combine(outFolder, settings.ExternalContentFolder, extPath);
                    Assert.IsTrue(File.Exists(extFilepath));

                    //get the actual wrapped in xml indicator
                    var xmlWrappedStr = extNode.SelectSingleNode("xm:XMLWrapped", xmlns)?.InnerText ?? "false";
                    bool actualWrapExtInXml = false;
                    if (bool.TryParse(xmlWrappedStr, out bool b))
                        actualWrapExtInXml = b;

                    if (actualWrapExtInXml != wrapExtInXml && wrapExtInXml)
                    {
                        var msg = $"External file: {extFilepath} is not wrapped in xml as expected";
                        logger?.LogDebug("{message}", msg);
                        Assert.Fail(msg); //shouldn't be any exceptions to this
                    }
                    else if (actualWrapExtInXml != wrapExtInXml && !wrapExtInXml)
                    {
                        var msg = $"External file: {extFilepath} is wrapped in xml when it shouldn't be";
                        logger?.LogDebug("{message}", msg);
                        //Assert.Inconclusive(msg); //inconclusive since it might be wrapped in XML because of a virus or other file IO problem
                    }

                    //get the hash and size for the external file
                    string? expectedHash;
                    string? expectedHashAlg;
                    long expectedSize;
                    if (actualWrapExtInXml)
                    {
                        expectedHash = extNode.SelectSingleNode("xm:XMLHash/xm:Value", xmlns)?.InnerText;
                        expectedHashAlg = extNode.SelectSingleNode("xm:XMLHash/xm:Function", xmlns)?.InnerText;
                        expectedSize = long.Parse(extNode.SelectSingleNode("xm:XMLSize", xmlns)?.InnerText ?? "-1");
                    }
                    else
                    {
                        expectedHash = extNode.SelectSingleNode("xm:Hash/xm:Value", xmlns)?.InnerText;
                        expectedHashAlg = extNode.SelectSingleNode("xm:Hash/xm:Function", xmlns)?.InnerText;
                        expectedSize = long.Parse(extNode.SelectSingleNode("xm:Size", xmlns)?.InnerText ?? "-1");
                    }

                    //make sure the hash values match
                    var actualHash = Helpers.CalculateHash(hashAlg, extFilepath);
                    if (string.IsNullOrEmpty(actualHash))
                    {
                        logger?.LogDebug("Unable to calculate the hash for external file: {extFilepath}", extFilepath);
                    }
                    else
                    {
                        Assert.AreEqual(expectedHash, actualHash);
                    }
                    Assert.AreEqual(hashAlg, expectedHashAlg);

                    //make sure the size values match
                    FileInfo fi = new(extFilepath);
                    long actualSize = fi.Length;
                    Assert.AreEqual(expectedSize, actualSize);


                    if (actualWrapExtInXml)
                    {
                        //make sure external content is wrapped in XML
                        var wrapped = extNode.SelectSingleNode("xm:XMLWrapped", xmlns)?.InnerText ?? "false";
                        Assert.IsTrue(wrapped.Equals("true", StringComparison.OrdinalIgnoreCase));

                        Assert.IsTrue(Path.GetExtension(extFilepath) == FilePathHelpers.XML_WRAPPED_EXT);

                        //make sure external file is valid xml
                        validXml = true;
                        extdoc.Load(extFilepath);
                        Assert.IsTrue(extdoc.DocumentElement?.LocalName == "BodyContent");
                        Assert.IsTrue(extdoc.DocumentElement?.NamespaceURI == EmailToEaxsProcessor.XM_NS);
                        extdoc.Validate(XmlValidationEventHandler, extdoc.DocumentElement);
                        Assert.IsTrue(validXml);

                        //Test the preserve transfer encoding setting
                        var enc = extdoc.SelectSingleNode("/xm:BodyContent/xm:TransferEncoding", xmlns)?.InnerText;
                        var origEnc = singleBodyNd.SelectSingleNode("xm:TransferEncoding", xmlns)?.InnerText;
                        if (preserveBinaryEnc && MimeKitHelpers.BinaryContentEncodings.Contains(origEnc))
                        {
                            if (enc != null)
                                Assert.AreEqual(origEnc, enc);
                        }
                        else if (preserveTextEnc && MimeKitHelpers.TextContentEncodings.Contains(origEnc))
                        {
                            if (enc != null)
                                Assert.AreEqual(origEnc, enc);
                        }
                        else
                        {
                            if (enc != null)
                                Assert.AreEqual("base64", enc);
                        }

                        var hash = extdoc.SelectSingleNode("/xm:BodyContent/xm:Hash/xm:Value", xmlns)?.InnerText;
                        var size = extdoc.SelectSingleNode("/xm:BodyContent/xm:Size", xmlns)?.InnerText;

                        //Test that the hash and file size are correct in the wrapped xml
                        IMimeDecoder? decoder = null;
                        switch (enc)
                        {
                            case null:
                            case "":
                                decoder = new MimeKit.Encodings.PassThroughDecoder(ContentEncoding.Default); //don't really care about the encoding since we are just testing the hash and size
                                break;
                            case "base64":
                                decoder = new MimeKit.Encodings.Base64Decoder();
                                break;
                            case "quoted-printable":
                                decoder = new MimeKit.Encodings.QuotedPrintableDecoder();
                                break;
                            case "uuencode":
                                decoder = new MimeKit.Encodings.UUDecoder();
                                break;
                            case "7bit":
                            case "8bit":
                            case "binary":
                                Assert.Fail($"Unexpected transfer encoding: {enc}");
                                break;
                            default:
                                Assert.Fail($"Unknown transfer encoding: {enc}");
                                break;
                        }
                        if (decoder != null)
                        {
                            var inBytes = Encoding.UTF8.GetBytes(extdoc.SelectSingleNode("/xm:BodyContent/xm:Content", xmlns)?.InnerText ?? "");
                            int estCount = decoder.EstimateOutputLength(inBytes.Length);
                            var outBytes = new byte[estCount];
                            int count = decoder.Decode(inBytes, 0, inBytes.Length, outBytes);
                            Assert.AreEqual(int.Parse(size ?? "0"), count);
                            Assert.AreEqual(hash, Helpers.CalculateHash(hashAlg, outBytes, 0, count));
                        }

                    }
                    else
                    {
                        //make sure is not wrapped
                        var wrapped = extNode.SelectSingleNode("xm:XMLWrapped", xmlns)?.InnerText ?? "false";
                        Assert.IsTrue(wrapped.Equals("false", StringComparison.OrdinalIgnoreCase));
                        //Test that file is not XML
                        validXml = true;
                        try
                        {
                            extdoc.Load(extFilepath);
                            //must be well formed XML, so make sure it is not a wrapped email content xml
                            Assert.IsFalse(extdoc.DocumentElement?.LocalName == "BodyContent");
                            Assert.IsFalse(extdoc.DocumentElement?.NamespaceURI == EmailToEaxsProcessor.XM_NS);
                            validXml = false;  //if it gets here, it is XML but not a wrapped email content, so it is invalid
                        }
                        catch (XmlException)
                        {
                            //probably not even XML
                            validXml = false;
                        }
                        catch (IOException ioex)
                        {
                            //file might be a virus or have some other problem, assume it is not valid XML
                            logger?.LogDebug("{message}", ioex.Message);
                            validXml = false;
                        }
                        Assert.IsFalse(validXml);


                    }
                }
            }
        }

        /// <summary>
        /// Make sure we have the expected number of error or warning messages  
        /// Negative numbers mean the count should be greater than the absolute value of the number, i.e. -1 means at least 1 error
        /// </summary>
        /// <param name="expectedErrors"></param>
        /// <param name="expectedWarnings"></param>
        private static void ValidateErrorAndWarningCounts(long expectedErrors, long expectedWarnings)
        {
            if (expectedErrors <= -1)
                Assert.IsTrue(StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Error]")).Count() >= -expectedErrors, "Expected some errors");
            else
                Assert.AreEqual(expectedErrors, StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Error]")).Count(), "Expected error count does not match");

            if (expectedWarnings <= -1)
                Assert.IsTrue(StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Warning]")).Count() >= -expectedWarnings, "Expected some warnings");
            else
                Assert.AreEqual(expectedWarnings, StringListLogger.Instance.LoggedLines.Where(s => s.StartsWith("[Warning]")).Count(), "Expected warning count does not match");
        }

        private static void ValidateHash(XmlElement msgFilePropElem, XmlNamespaceManager xmlns, string outFolder, EmailToEaxsProcessorSettings settings, string hashAlg)
        {
            //get the hash values from the xml
            XmlNode? hashValueNd = msgFilePropElem.SelectSingleNode("xm:Hash/xm:Value", xmlns);
            XmlNode? hashFuncNd = msgFilePropElem.SelectSingleNode("xm:Hash/xm:Function", xmlns);
            XmlNode? sizeNd = msgFilePropElem.SelectSingleNode("xm:Size", xmlns);
            XmlNode? relPath = msgFilePropElem.SelectSingleNode("xm:RelPath", xmlns);
            string absPath = Path.Combine(outFolder, relPath?.InnerText ?? "");

            if (hashFuncNd != null && hashValueNd != null)
            {
                //if there is a hash, make sure hashes match
                Assert.AreEqual(settings.HashAlgorithmName, hashFuncNd?.InnerText);
                Assert.AreEqual(hashAlg, hashFuncNd?.InnerText);

                var expectedHash = Helpers.CalculateHash(hashAlg, absPath);
                Assert.AreEqual(expectedHash, hashValueNd?.InnerText);
            }

            //make sure size match
            FileInfo fi = new(absPath);
            long expectedSize = fi.Length;
            long actualSize = long.Parse(sizeNd?.InnerText ?? "-1");
            Assert.AreEqual(expectedSize, actualSize);
        }

        void XmlValidationEventHandler(object? sender, ValidationEventArgs e)
        {
            validXml = false;
            if (e.Severity == XmlSeverityType.Warning)
            {
                logger?.LogDebug("Line: {lineNumber} -- {message}", e.Exception.LineNumber, e.Message);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                logger?.LogDebug("Line: {lineNumber} -- {message}", e.Exception.LineNumber, e.Message);
            }
        }


        void ValidateLocalIds(XmlDocument xdoc, XmlNamespaceManager xmlns)
        {
            //make sure the localId values in a given xml file all increase by 1
            var localIds = xdoc.SelectNodes("//xm:LocalId", xmlns);
            if (localIds != null && localIds.Count > 0)
            {
                //init the prevId to one less than the first localId
                if (!long.TryParse(localIds[0]?.InnerText, out long prevId))
                {
                    Assert.Fail("localId is not a number");
                }
                prevId--;

                foreach (XmlElement localId in localIds)
                {
                    if (long.TryParse(localId.InnerText, out long id))
                    {
                        Assert.AreEqual(id, prevId + 1);
                        prevId = id;
                    }
                    else
                    {
                        Assert.Fail("localId is not a number");
                    }
                }
            }
            else
            {
                //This is not an error because some files will contain no messages, usually if the source file was not a valid mbox file
                //Assert.Fail("No localIds found");
                logger?.LogDebug("No localIds found");
            }
        }

        /// <summary>
        /// Just validate the XML output files and store the results in a separate output file
        /// </summary>
        /// <param name="oneFilePerMbox"></param>
        /// <param name="sampleFile"></param>
        /// <param name="outFolder"></param>
        /// <returns></returns>
        private bool ValidateXmlDocuments(bool includeSubs, bool oneFilePerMbox, string sampleFile, string outFolder, bool forceParse, InputFileType inFileType, MimeFormat mimeFormat)
        {
            List<string> expectedXmlFiles = Helpers.GetExpectedFiles(includeSubs, oneFilePerMbox, sampleFile, outFolder, forceParse, inFileType, mimeFormat);

            validXml = true;

            foreach (var xmlFile in expectedXmlFiles)
            {
                //use an XmlReader to validate with line numbers as soon as the Xml document is loaded
                XmlReaderSettings rdrSettings = new()
                {
                    Schemas = new XmlSchemaSet()
                };
                rdrSettings.Schemas.Add(EmailToEaxsProcessor.XM_NS, EmailToEaxsProcessor.XM_XSD);
                rdrSettings.Schemas.Add(EmailToEaxsProcessor.XM_NS, EmailToEaxsProcessor.XM_CC_XSD);
                rdrSettings.Schemas.Add(EmailToEaxsProcessor.XHTML_NS, EmailToEaxsProcessor.XHTML_XSD);

                rdrSettings.ValidationType = ValidationType.Schema;
                rdrSettings.ValidationEventHandler += XmlValidationEventHandlerWriteToFile;
                rdrSettings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;

                outFile = Path.ChangeExtension(xmlFile, "val.out");
                File.Delete(outFile);
                using var xRdr = XmlReader.Create(xmlFile, rdrSettings);
                var xDoc = new XmlDocument(); //this will validate the XML
                try
                {
                    logger?.LogInformation("Validating '{xmlFile}'; results are in '{outFile}'.", xmlFile, outFile);
                    File.AppendAllText(outFile, $"*** Validating XML File: {xmlFile} ***\r\n");
                    xDoc.Load(xRdr);
                }
                catch (XmlException xex)
                {
                    File.AppendAllText(outFile, xex.Message + "\r\n");
                    validXml = false;
                }
                catch (Exception ex)
                {
                    File.AppendAllText(outFile, ex.Message + "\r\n");
                }

                xRdr.Close();

                File.AppendAllText(outFile, $"*** File Valid: {validXml} ***\r\n");
            }

            return validXml;

        }

        private void XmlValidationEventHandlerWriteToFile(object? sender, ValidationEventArgs e)
        {
            validXml = false;
            if (e.Severity == XmlSeverityType.Warning)
            {
                File.AppendAllText(outFile, $"Line: {e.Exception.LineNumber} -- {e.Message}\r\n");
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                File.AppendAllText(outFile, $"Line: {e.Exception.LineNumber} -- {e.Message}\r\n");
            }
        }


        private (string expectedOutFolder, string outFolder) GetOutFolder(string sampleFile, string relOutPath)
        {
            var expectedOutFolder = "";
            var outFolder = "";

            if (Directory.Exists(sampleFile))
            {
                //sampleFile is a directory, so the output folder is relative to the sample folder's parent
                expectedOutFolder = Path.Combine(Path.GetDirectoryName(sampleFile) ?? "", relOutPath);
                outFolder = Path.Combine(Path.GetDirectoryName(sampleFile) ?? "", relOutPath);
            }
            else if (File.Exists(sampleFile))
            {
                //sample file is a file, so the output folder is relative to the sample file's parent directory
                expectedOutFolder = Path.Combine(testFilesBaseInputDirectory, relOutPath);
                outFolder = Path.Combine(testFilesBaseInputDirectory, relOutPath);
            }
            else
            {
                Assert.Fail($"Sample file or folder '{sampleFile}' does not exist");
            }

            return (expectedOutFolder, outFolder);

        }

    }
}