using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.TestEaPdf
{

    [TestClass]
    public class TestNDependPath
    {
        [TestMethod]
        public void TestFolderDepth()
        {
            Assert.AreEqual(2, FilePathHelpers.FolderDepth(new DirectoryInfo(@"C:\test\test")));

            Assert.AreEqual(1, FilePathHelpers.FolderDepth(new DirectoryInfo(@"C:\test")));

            Assert.AreEqual(0, FilePathHelpers.FolderDepth(new DirectoryInfo(@"C:\")));

            Assert.IsTrue(FilePathHelpers.FolderDepth(new DirectoryInfo(@"C:")) > 1); //this evaluates to the current directory on the c: drive

            Assert.IsTrue(FilePathHelpers.FolderDepth(new DirectoryInfo(@"test")) > 1); //this evaluates to file relative to the current directory 

            Assert.AreEqual(0, FilePathHelpers.FolderDepth(new DirectoryInfo(Path.DirectorySeparatorChar.ToString())));
            Assert.AreEqual(0, FilePathHelpers.FolderDepth(new DirectoryInfo(Path.AltDirectorySeparatorChar.ToString())));
        }


        //windows paths
        [DataRow(@"C:\home\user\file.txt", true, true, true, DisplayName = @"WINDOWS_TRYD C:\home\user\file.txt")]
        [DataRow(@"\home\user\file.txt", true, false, false, DisplayName = @"WINDOWS_TRYD \home\user\file.txt")]
        [DataRow(@"C:file.txt", true, false, false, DisplayName = @"WINDOWS_TRYD C:file.txt")]
        [DataRow(@"file.txt", true, false, false, DisplayName = @"WINDOWS_TRYD file.txt")]
        [DataRow(@"home\user\file.txt", true, false, false, DisplayName = @"WINDOWS_TRYD home\user\file.txt")]
        [DataRow(@"C:\home\user\file.txt\", true, false, true, DisplayName = @"WINDOWS_TRYD C:\home\user\file.txt\")]
        [DataRow(@"C:\", true, false, true, DisplayName = @"WINDOWS_TRYD C:\")]
        [DataRow(@"C:", true, false, false, DisplayName = @"WINDOWS_TRYD C:")]
        [DataRow("", true, false, false, DisplayName = "WINDOWS_TRYD String.Empty")]
        [DataRow(@"C:\home\user\.", true, false, true, DisplayName = @"WINDOWS_TRYD C:\home\user\.")]
        [DataRow(@"C:\home\user\..", true, false, true, DisplayName = @"WINDOWS_TRYD C:\home\user\..")]
        [DataRow(@"C:\home\user\.\file.txt", true, true, true, DisplayName = @"WINDOWS_TRYD C:\home\user\.\file.txt")]
        [DataRow(@"C:\home\user\..\file.txt", true, true, true, DisplayName = @"WINDOWS_TRYD C:\home\user\..\file.txt")]
        [DataRow(@"C:\home\user\file.", true, true, true, DisplayName = @"WINDOWS_TRYD C:\home\user\file.")]
        [DataRow(@"C:\home\user\fi|le.txt", true, false, false, DisplayName = @"WINDOWS_TRYD C:\home\user\fi|le.txt")]
        [DataRow(@"C:\home\us|er\file.txt", true, false, false, DisplayName = @"WINDOWS_TRYD C:\home\us|er\file.txt")]
        [DataRow("C:\\home\\us\0er\\file.txt", true, false, false, DisplayName = @"WINDOWS_TRYD C:\home\us\0er\file.txt")]

        // linux paths
        [DataRow(@"/mnt/c/home/user/file.txt", false, true, true, DisplayName = @"LINUX_TRYD /mnt/c/home/user/file.txt")]
        [DataRow(@"file.txt", false, false, false, DisplayName = @"LINUX_TRYD file.txt")]
        [DataRow(@"home/user/file.txt", false, false, false, DisplayName = @"LINUX_TRYD home/user/file.txt")]
        [DataRow(@"/mnt/c/home/user/file.txt/", false, false, true, DisplayName = @"LINUX_TRYD /mnt/c/home/user/file.txt/")]
        [DataRow(@"/", false, false, true, DisplayName = @"LINUX_TRYD /")]
        [DataRow("", false, false, false, DisplayName = "LINUX_TRYD String.Empty")]
        [DataRow(@"/mnt/c/home/user/.", false, false, true, DisplayName = @"LINUX_TRYD /mnt/c/home/user/.")]
        [DataRow(@"/mnt/c/home/user/..", false, false, true, DisplayName = @"LINUX_TRYD /mnt/c/home/user/..")]
        [DataRow(@"/mnt/c/home/user/./file.txt", false, true, true, DisplayName = @"LINUX_TRYD /mnt/c/home/user/./file.txt")]
        [DataRow(@"/mnt/c/home/user/../file.txt", false, true, true, DisplayName = @"LINUX_TRYD /mnt/c/home/user/../file.txt")]
        [DataRow(@"/mnt/c/home/user/file.", false, true, true, DisplayName = @"LINUX_TRYD /mnt/c/home/user/file.")]
        [DataRow(@"/mnt/c/home/user/fi|le.txt", false, true, true, DisplayName = @"LINUX_TRYD /mnt/c/home/user/fi|le.txt")]
        [DataRow(@"/mnt/c/home/us|er/file.txt", false, true, true, DisplayName = @"LINUX_TRYD /mnt/c/home/us|er/file.txt")]
        [DataRow("/mnt/c/home/us\0er/file.txt", false, false, false, DisplayName = @"LINUX_TRYD /mnt/c/home/us\0er/file.txt")]

        [DataTestMethod]
        public void TestTryGetAbsoluteDirectoryPathX(string path, bool isWindows, bool expectedValidFile, bool expectedValidDir)
        {
            if (OperatingSystem.IsWindows() == isWindows)
            {
                var validDir = path.TryGetAbsoluteDirectoryPathX(out DirectoryInfo? absoluteFilePath, out string reason);

                Assert.AreEqual(expectedValidDir, validDir, $"'{path}' {reason}");

                if (validDir)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(reason), $"'{path}' {reason}");
                    Assert.IsNotNull(absoluteFilePath, $"'{path}' {reason}");
                    Assert.IsTrue(path.IsValidAbsoluteDirectoryPathX(out string reasonFile), $"'{path}' {reasonFile}");
                }
                else
                {
                    Assert.IsFalse(string.IsNullOrEmpty(reason), $"'{path}' {reason}");
                    Assert.IsNull(absoluteFilePath, $"'{path}' {reason}");
                    Assert.IsFalse(path.IsValidAbsoluteDirectoryPathX(out string reasonFile), $"'{path}' {reasonFile}");
                }

            }
            else
            {
                Assert.Inconclusive($"'{path}' Test is not for this OS.");
            }
        }


        //windows paths
        [DataRow(@"C:\home\user\file.txt", true, true, true, DisplayName = @"WINDOWS_TRY C:\home\user\file.txt")]
        [DataRow(@"\home\user\file.txt", true, false, false, DisplayName = @"WINDOWS_TRY \home\user\file.txt")]
        [DataRow(@"C:file.txt", true, false, false, DisplayName = @"WINDOWS_TRY C:file.txt")]
        [DataRow(@"file.txt", true, false, false, DisplayName = @"WINDOWS_TRY file.txt")]
        [DataRow(@"home\user\file.txt", true, false, false, DisplayName = @"WINDOWS_TRY home\user\file.txt")]
        [DataRow(@"C:\home\user\file.txt\", true, false, true, DisplayName = @"WINDOWS_TRY C:\home\user\file.txt\")]
        [DataRow(@"C:\", true, false, true, DisplayName = @"WINDOWS_TRY C:\")]
        [DataRow(@"C:", true, false, false, DisplayName = @"WINDOWS_TRY C:")]
        [DataRow("", true, false, false, DisplayName = "WINDOWS_TRY String.Empty")]
        [DataRow(@"C:\home\user\.", true, false, true, DisplayName = @"WINDOWS_TRY C:\home\user\.")]
        [DataRow(@"C:\home\user\..", true, false, true, DisplayName = @"WINDOWS_TRY C:\home\user\..")]
        [DataRow(@"C:\home\user\.\file.txt", true, true, true, DisplayName = @"WINDOWS_TRY C:\home\user\.\file.txt")]
        [DataRow(@"C:\home\user\..\file.txt", true, true, true, DisplayName = @"WINDOWS_TRY C:\home\user\..\file.txt")]
        [DataRow(@"C:\home\user\file.", true, true, true, DisplayName = @"WINDOWS_TRY C:\home\user\file.")]
        [DataRow(@"C:\home\user\fi|le.txt", true, false, false, DisplayName = @"WINDOWS_TRY C:\home\user\fi|le.txt")]
        [DataRow(@"C:\home\us|er\file.txt", true, false, false, DisplayName = @"WINDOWS_TRY C:\home\us|er\file.txt")]
        [DataRow("C:\\home\\us\0er\\file.txt", true, false, false, DisplayName = @"WINDOWS_TRY C:\home\us\0er\file.txt")]

        // linux paths
        [DataRow(@"/mnt/c/home/user/file.txt", false, true, true, DisplayName = @"LINUX_TRY /mnt/c/home/user/file.txt")]
        [DataRow(@"file.txt", false, false, false, DisplayName = @"LINUX_TRY file.txt")]
        [DataRow(@"home/user/file.txt", false, false, false, DisplayName = @"LINUX_TRY home/user/file.txt")]
        [DataRow(@"/mnt/c/home/user/file.txt/", false, false, true, DisplayName = @"LINUX_TRY /mnt/c/home/user/file.txt/")]
        [DataRow(@"/", false, false, true, DisplayName = @"LINUX_TRY /")]
        [DataRow("", false, false, false, DisplayName = "LINUX_TRY String.Empty")]
        [DataRow(@"/mnt/c/home/user/.", false, false, true, DisplayName = @"LINUX_TRY /mnt/c/home/user/.")]
        [DataRow(@"/mnt/c/home/user/..", false, false, true, DisplayName = @"LINUX_TRY /mnt/c/home/user/..")]
        [DataRow(@"/mnt/c/home/user/./file.txt", false, true, true, DisplayName = @"LINUX_TRY /mnt/c/home/user/./file.txt")]
        [DataRow(@"/mnt/c/home/user/../file.txt", false, true, true, DisplayName = @"LINUX_TRY /mnt/c/home/user/../file.txt")]
        [DataRow(@"/mnt/c/home/user/file.", false, true, true, DisplayName = @"LINUX_TRY /mnt/c/home/user/file.")]
        [DataRow(@"/mnt/c/home/user/fi|le.txt", false, true, true, DisplayName = @"LINUX_TRY /mnt/c/home/user/fi|le.txt")]
        [DataRow(@"/mnt/c/home/us|er/file.txt", false, true, true, DisplayName = @"LINUX_TRY /mnt/c/home/us|er/file.txt")]
        [DataRow("/mnt/c/home/us\0er/file.txt", false, false, false, DisplayName = @"LINUX_TRY /mnt/c/home/us\0er/file.txt")]

        [DataTestMethod]
        public void TestTryGetAbsoluteFilePathX(string path, bool isWindows, bool expectedValidFile, bool expectedValidDir)
        {
            if (OperatingSystem.IsWindows() == isWindows)
            {
                var validFile = path.TryGetAbsoluteFilePathX(out FileInfo? absoluteFilePath, out string reason);

                Assert.AreEqual(expectedValidFile, validFile, $"'{path}' {reason}");

                if(validFile)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(reason), $"'{path}' {reason}");
                    Assert.IsNotNull(absoluteFilePath, $"'{path}' {reason}");
                    Assert.IsTrue(path.IsValidAbsoluteFilePathX(out string reasonFile), $"'{path}' {reasonFile}");
                }
                else
                {
                    Assert.IsFalse(string.IsNullOrEmpty(reason), $"'{path}' {reason}");
                    Assert.IsNull(absoluteFilePath, $"'{path}' {reason}");
                    Assert.IsFalse(path.IsValidAbsoluteFilePathX(out string reasonFile), $"'{path}' {reasonFile}");
                }

            }
            else
            {
                Assert.Inconclusive($"'{path}' Test is not for this OS.");
            }
        }


        //windows paths
        [DataRow(@"C:\home\user\file.txt", true, true, true, DisplayName = @"WINDOWS_AB C:\home\user\file.txt")]
        [DataRow(@"\home\user\file.txt", true, false, false, DisplayName = @"WINDOWS_AB \home\user\file.txt")]
        [DataRow(@"C:file.txt", true, false, false, DisplayName = @"WINDOWS_AB C:file.txt")]
        [DataRow(@"file.txt", true, false, false, DisplayName = @"WINDOWS_AB file.txt")]
        [DataRow(@"home\user\file.txt", true, false, false, DisplayName = @"WINDOWS_AB home\user\file.txt")]
        [DataRow(@"C:\home\user\file.txt\", true, false, true, DisplayName = @"WINDOWS_AB C:\home\user\file.txt\")]
        [DataRow(@"C:\", true, false, true, DisplayName = @"WINDOWS_AB C:\")]
        [DataRow(@"C:", true, false, false, DisplayName = @"WINDOWS_AB C:")]
        [DataRow("", true, false, false, DisplayName = "WINDOWS_AB String.Empty")]
        [DataRow(@"C:\home\user\.", true, false, true, DisplayName = @"WINDOWS_AB C:\home\user\.")]
        [DataRow(@"C:\home\user\..", true, false, true, DisplayName = @"WINDOWS_AB C:\home\user\..")]
        [DataRow(@"C:\home\user\.\file.txt", true, true, true, DisplayName = @"WINDOWS_AB C:\home\user\.\file.txt")]
        [DataRow(@"C:\home\user\..\file.txt", true, true, true, DisplayName = @"WINDOWS_AB C:\home\user\..\file.txt")]
        [DataRow(@"C:\home\user\file.", true, true, true, DisplayName = @"WINDOWS_AB C:\home\user\file.")]
        [DataRow(@"C:\home\user\fi|le.txt", true, false, false, DisplayName = @"WINDOWS_AB C:\home\user\fi|le.txt")]
        [DataRow(@"C:\home\us|er\file.txt", true, false, false, DisplayName = @"WINDOWS_AB C:\home\us|er\file.txt")]
        [DataRow("C:\\home\\us\0er\\file.txt", true, false, false, DisplayName = @"WINDOWS_AB C:\home\us\0er\file.txt")]

        // linux paths
        [DataRow(@"/mnt/c/home/user/file.txt", false, true, true, DisplayName = @"LINUX_AB /mnt/c/home/user/file.txt")]
        [DataRow(@"file.txt", false, false, false, DisplayName = @"LINUX_AB file.txt")]
        [DataRow(@"home/user/file.txt", false, false, false, DisplayName = @"LINUX_AB home/user/file.txt")]
        [DataRow(@"/mnt/c/home/user/file.txt/", false, false, true, DisplayName = @"LINUX_AB /mnt/c/home/user/file.txt/")]
        [DataRow(@"/", false, false, true, DisplayName = @"LINUX_AB /")]
        [DataRow("", false, false, false, DisplayName = "LINUX_AB String.Empty")]
        [DataRow(@"/mnt/c/home/user/.", false, false, true, DisplayName = @"LINUX_AB /mnt/c/home/user/.")]
        [DataRow(@"/mnt/c/home/user/..", false, false, true, DisplayName = @"LINUX_AB /mnt/c/home/user/..")]
        [DataRow(@"/mnt/c/home/user/./file.txt", false, true, true, DisplayName = @"LINUX_AB /mnt/c/home/user/./file.txt")]
        [DataRow(@"/mnt/c/home/user/../file.txt", false, true, true, DisplayName = @"LINUX_AB /mnt/c/home/user/../file.txt")]
        [DataRow(@"/mnt/c/home/user/file.", false, true, true, DisplayName = @"LINUX_AB /mnt/c/home/user/file.")]
        [DataRow(@"/mnt/c/home/user/fi|le.txt", false, true, true, DisplayName = @"LINUX_AB /mnt/c/home/user/fi|le.txt")]
        [DataRow(@"/mnt/c/home/us|er/file.txt", false, true, true, DisplayName = @"LINUX_AB /mnt/c/home/us|er/file.txt")]
        [DataRow("/mnt/c/home/us\0er/file.txt", false, false, false, DisplayName = @"LINUX_AB /mnt/c/home/us\0er/file.txt")]

        [DataTestMethod]
        public void TestIsValidAbsolutePath(string path, bool isWindows, bool expectedValidFile, bool expectedValidDir)
        {

            if (OperatingSystem.IsWindows() == isWindows)
            {
                var validFile = path.IsValidAbsoluteFilePathX(out string reasonFile);
                var validDir = path.IsValidAbsoluteDirectoryPathX(out string reasonDir);

                Assert.AreEqual(expectedValidFile, validFile, $"'{path}' {reasonFile}");
                Assert.AreEqual(expectedValidDir, validDir, $"'{path}' {reasonDir}");

                if (validFile)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(reasonFile), $"'{path}' {reasonFile}");
                }
                else
                {
                    Assert.IsFalse(string.IsNullOrEmpty(reasonFile), $"'{path}' {reasonFile}");
                }

                if (validDir)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(reasonDir), $"'{path}' {reasonDir}");
                }
                else
                {
                    Assert.IsFalse(string.IsNullOrEmpty(reasonDir), $"'{path}' {reasonDir}");
                }


            }
            else
            {
                Assert.Inconclusive($"'{path}' Test is not for this OS.");
            }
        }

        [TestMethod]
        public void TestRootDiscover()
        {
            var rootFile = @"C:\test.txt".ToAbsoluteDirectoryPathX();

            var rootParent = rootFile.Parent;

            Assert.IsNotNull(rootParent);

            Assert.IsTrue(Path.GetPathRoot(rootParent.FullName) == "C:\\");

        }

        [TestMethod]
        public void TestIsChildOf()
        {
            var ancestor = @"C:\one\two\three".ToAbsoluteDirectoryPathX();

            var descendant = @"C:\one\two\three\four\five\six".ToAbsoluteDirectoryPathX();
            Assert.IsTrue(descendant.IsChildOf(ancestor));


            descendant = @"C:\one\two\".ToAbsoluteDirectoryPathX();
            Assert.IsFalse(descendant.IsChildOf(ancestor));

            descendant = @"C:\one\two\three\".ToAbsoluteDirectoryPathX();
            Assert.IsFalse(descendant.IsChildOf(ancestor));

            descendant = @"C:\one\TWO\three/four/".ToAbsoluteDirectoryPathX();
            Assert.IsTrue(descendant.IsChildOf(ancestor));

        }

        [DataRow(@"C:\one\two\three\file", @"C:\one\two\three\file", false, DisplayName = "path-exact-match")]
        [DataRow(@"C:\one\two\three\file", @"D:\one\two\three\file", true, DisplayName = "path-exact-match-different-drive")]
        [DataRow(@"C:\one\two\three\file.two.mbox", @"C:\one\two\three\file.out", true, DisplayName = "path-match-in-has-2-ext")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file.mbox.out", true, DisplayName = "path-match-out-has-2-ext")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file.mbox.out", true, DisplayName = "path-match-out-has-2-ext")]
        [DataRow(@"C:\one\two\three\file.two.mbox", @"C:\one\two\three\file.two.out", false, DisplayName = "path-match-has-2-ext-except-ext")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\folder.out", true, DisplayName = "path-same-parent-diff-subfolder")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file.out\out", false, DisplayName = "path-match-except-ext-subfolder")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file\one\two\three", false, DisplayName = "path-match-except-ext-deep-subfolder")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\out\file.out\two\three", true, DisplayName = "path-match-in-subfolder-deep")]
        [DataRow(@"C:\one\two\three\file", @"C:\one\two\file", true, DisplayName = "path-child-less-depth")]
        [DataRow(@"C:\one\file", @"C:\out", true, DisplayName = "path-close-to-root")]
        [DataRow(@"C:\one\file", @"C:\one\file.out", false, DisplayName = "path-close-to-root-not-valid")]
        [DataRow(@"C:\file", @"C:\one", true, DisplayName = "path-in-root-valid")]
        [DataRow(@"C:\file", @"C:\file.out", false, DisplayName = "path-in-root-not-valid")]
        [DataRow(@"C:\file", @"C:\", true, DisplayName = "path-out-is-root")]
        [DataTestMethod]
        public void TestIsValidOutputPath(string inFile, string outFolder, bool expected)
        {

            var valid = EaPdf.Helpers.FilePathHelpers.IsValidOutputPathForMboxFile(inFile, outFolder);
            Assert.AreEqual(expected, valid);

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInvalidFileName()
        {
            _ = EaPdf.Helpers.FilePathHelpers.IsValidOutputPathForMboxFile(@"C:\", @"C:\one\file");

        }
    }
}
