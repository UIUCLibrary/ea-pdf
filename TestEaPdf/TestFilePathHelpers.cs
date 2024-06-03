using AngleSharp.Dom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using UIUCLibrary.EaPdf.Helpers;

namespace UIUCLibrary.TestEaPdf
{


    [TestClass]
    public class TestFilePathHelpers
    {
        [DataRow(@"C:\one\two\three\..\four\five\.\six\", @"C:\one\two\four\five\six\", true, DisplayName = @"WINDOWS_NORM C:\one\two\three\..\four\five\.\six\")]
        [DataRow(@"C:\one\two\three\four\five\six\..", @"C:\one\two\three\four\five", true, DisplayName = @"WINDOWS_NORM C:\one\two\three\four\five\six\..")]
        [DataRow(@"C:\one\two\three\four\five\six\..\", @"C:\one\two\three\four\five\", true, DisplayName = @"WINDOWS_NORM C:\one\two\three\four\five\six\..\")]
        [DataRow(@"C:\one\\\two\three\four//five\six\..\\", @"C:\one\two\three\four\five\", true, DisplayName = @"WINDOWS_NORM C:\one\\\two\three\four//five\six\..\\")]

        [DataRow(@"/one/two/three/../four/five/./six/", @"/one/two/four/five/six/", false, DisplayName = @"LINUX_NORM /one/two/three/../four/five/./six/")]
        [DataRow(@"/one/two/three/four/five/six/..", @"/one/two/three/four/five", false, DisplayName = @"LINUX_NORM /one/two/three/four/five/six/..")]
        [DataRow(@"/one/two/three/four/five/six/../", @"/one/two/three/four/five/", false, DisplayName = @"LINUX_NORM /one/two/three/four/five/six/../")]
        [DataRow(@"/one///two/three/four//five/six/..//", @"/one/two/three/four/five/", false, DisplayName = @"LINUX_NORM /one///two/three/four//five/six/..//")]

        [DataTestMethod]
        public void TestPathNormalization(string path, string normPath, bool isWindows)
        {
            if (OperatingSystem.IsWindows() == isWindows)
            {

                var dirInfo = new DirectoryInfo(path);

                Assert.AreEqual(normPath, dirInfo.FullName);
            }
            else
            {
                Assert.Inconclusive($"'{path}' Test is not for this OS.");
            }
        }



        [DataRow(@"C:\test\test", 2, true, DisplayName = @"WINDOWS_DEPTH C:\test\test")]
        [DataRow(@"C:\test", 1, true, DisplayName = @"WINDOWS_DEPTH C:\test")]
        [DataRow(@"C:\test\test\", 2, true, DisplayName = @"WINDOWS_DEPTH C:\test\test\")]
        [DataRow(@"C:\test\", 1, true, DisplayName = @"WINDOWS_DEPTH C:\test\")]
        [DataRow(@"C:/test/test", 2, true, DisplayName = @"WINDOWS_DEPTH C:/test/test")]
        [DataRow(@"C:/test", 1, true, DisplayName = @"WINDOWS_DEPTH C:/test")]
        [DataRow(@"C:/test/test/", 2, true, DisplayName = @"WINDOWS_DEPTH C:/test/test/")]
        [DataRow(@"C:/test/", 1, true, DisplayName = @"WINDOWS_DEPTH C:/test/")]
        [DataRow(@"C:\", 0, true, DisplayName = @"WINDOWS_DEPTH C:\")]
        [DataRow(@"C:/", 0, true, DisplayName = @"WINDOWS_DEPTH C:/")]
        [DataRow(@"/", 0, true, DisplayName = @"WINDOWS_DEPTH /")]
        [DataRow(@"\", 0, true, DisplayName = @"WINDOWS_DEPTH \")]
        [DataRow(@"C:", -1, true, DisplayName = @"WINDOWS_DEPTH C:")]
        [DataRow(@"test", -1, true, DisplayName = @"WINDOWS_DEPTH test")]

        [DataRow(@"/test/test", 2, false, DisplayName = @"LINUX_DEPTH /test/test")]
        [DataRow(@"/test", 1, false, DisplayName = @"LINUX_DEPTH /test")]
        [DataRow(@"/test/test/", 2, false, DisplayName = @"LINUX_DEPTH /test/test/")]
        [DataRow(@"/test/", 1, false, DisplayName = @"LINUX_DEPTH /test/")]
        [DataRow(@"/", 0, false, DisplayName = @"LINUX_DEPTH /")]
        [DataRow(@"\", -1, false, DisplayName = @"LINUX_DEPTH \")] //in linux this is a relative path to a file named '\'
        [DataRow(@"test", -1, false, DisplayName = @"LINUX_DEPTH test")]

        [DataTestMethod]
        public void TestFolderDepth(string path, int expectedDepth, bool isWindows)
        {
            if (OperatingSystem.IsWindows() == isWindows)
            {
                if (expectedDepth >= 0)
                {
                    Assert.AreEqual(expectedDepth, FilePathHelpers.FolderDepth(new DirectoryInfo(path)));
                }
                else
                {
                    //if expectedDepth is negative, then the depth is greater than the absolute value of expectedDepth
                    //used to test relative paths
                    Assert.IsTrue(FilePathHelpers.FolderDepth(new DirectoryInfo(path)) > Math.Abs(expectedDepth));
                }
            }
            else
            {
                Assert.Inconclusive($"'{path}' Test is not for this OS.");
            }
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
                var validDir = path.TryGetDirectoryInfo(out DirectoryInfo? absoluteFilePath, out string reason);

                Assert.AreEqual(expectedValidDir, validDir, $"'{path}' {reason}");

                if (validDir)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(reason), $"'{path}' {reason}");
                    Assert.IsNotNull(absoluteFilePath, $"'{path}' {reason}");
                    Assert.IsTrue(path.IsValidAbsoluteDirectoryPath(out string reasonFile), $"'{path}' {reasonFile}");
                }
                else
                {
                    Assert.IsFalse(string.IsNullOrEmpty(reason), $"'{path}' {reason}");
                    Assert.IsNull(absoluteFilePath, $"'{path}' {reason}");
                    Assert.IsFalse(path.IsValidAbsoluteDirectoryPath(out string reasonFile), $"'{path}' {reasonFile}");
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
                var validFile = path.TryGetFileInfo(out FileInfo? absoluteFilePath, out string reason);

                Assert.AreEqual(expectedValidFile, validFile, $"'{path}' {reason}");

                if (validFile)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(reason), $"'{path}' {reason}");
                    Assert.IsNotNull(absoluteFilePath, $"'{path}' {reason}");
                    Assert.IsTrue(path.IsValidAbsoluteFilePath(out string reasonFile), $"'{path}' {reasonFile}");
                }
                else
                {
                    Assert.IsFalse(string.IsNullOrEmpty(reason), $"'{path}' {reason}");
                    Assert.IsNull(absoluteFilePath, $"'{path}' {reason}");
                    Assert.IsFalse(path.IsValidAbsoluteFilePath(out string reasonFile), $"'{path}' {reasonFile}");
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
                var validFile = path.IsValidAbsoluteFilePath(out string reasonFile);
                var validDir = path.IsValidAbsoluteDirectoryPath(out string reasonDir);

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
            if (OperatingSystem.IsWindows())
            {
                var rootFile = @"C:\test.txt".ToDirectoryInfo();

                var rootParent = rootFile.Parent;

                Assert.IsNotNull(rootParent);

                Assert.IsTrue(Path.GetPathRoot(rootParent.FullName) == "C:\\");

            }
            else
            {
                var rootFile = @"/test.txt".ToDirectoryInfo();

                var rootParent = rootFile.Parent;

                Assert.IsNotNull(rootParent);

                Assert.IsTrue(Path.GetPathRoot(rootParent.FullName) == "/");
            }

        }

        [DataRow(@"C:\one\two\three", @"C:\one\two\three\four\five\six", true, true, DisplayName = @"WINDOWS_CHILD C:\one\two\three\four\five\six")]
        [DataRow(@"C:\one\two\three", @"C:/one/two/three/four/five/six", true, true, DisplayName = @"WINDOWS_CHILD C:/one/two/three/four/five/six")]
        [DataRow(@"C:\one\two\three", @"C:\one\two\", false, true, DisplayName = @"WINDOWS_CHILD C:\one\two\")]
        [DataRow(@"C:/one/two/three", @"C:/one/two/", false, true, DisplayName = @"WINDOWS_CHILD C:/one/two/")]
        [DataRow(@"C:\one\two\three", @"C:\one\two\three\", false, true, DisplayName = @"WINDOWS_CHILD C:\one\two\three\")]
        [DataRow(@"C:\one\two\three", @"C:\one\TWO\three/four/", true, true, DisplayName = @"WINDOWS_CHILD C:\one\TWO\three/four/")]

        [DataRow(@"/one/two/three", @"/one/two/three/four/five/six", true, false, DisplayName = @"LINUX_CHILD C:\one\two\three\four\five\six")]
        [DataRow(@"/one/two/three", @"/one/two/", false, false, DisplayName = @"LINUX_CHILD C:\one\two\")]
        [DataRow(@"/one/two/three", @"/one/two/three/", false, false, DisplayName = @"LINUX_CHILD C:\one\two\three\")]
        [DataRow(@"/one/two/three", @"/one/TWO/three/four/", false, false, DisplayName = @"LINUX_CHILD C:\one\TWO\three/four/")]

        [DataTestMethod]
        public void TestIsChildOf(string ancestorPath, string descendantPath, bool expectedIsDesc, bool isWindows)
        {
            if (OperatingSystem.IsWindows() == isWindows)
            {
                var ancestor = ancestorPath.ToDirectoryInfo();
                var descendant = descendantPath.ToDirectoryInfo();
                Assert.AreEqual(expectedIsDesc, descendant.IsChildOf(ancestor));

            }
            else
            {
                Assert.Inconclusive($"'{ancestorPath}' '{descendantPath}' Test is not for this OS.");
            }
        }

        //TODO: Make Linux versions of all these tests

        [DataRow(@"C:\one\two\three\file", @"C:\one\two\three\file", false, true, DisplayName = "WINDOWS_VALID path-exact-match")]
        [DataRow(@"C:\one\two\three\file", @"D:\one\two\three\file", true, true, DisplayName = "WINDOWS_VALID path-exact-match-different-drive")]
        [DataRow(@"C:\one\two\three\file.two.mbox", @"C:\one\two\three\file.out", true, true, DisplayName = "WINDOWS_VALID path-match-in-has-2-ext")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file.mbox.out", true, true, DisplayName = "WINDOWS_VALID path-match-out-has-2-ext")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file.mbox.out", true, true, DisplayName = "WINDOWS_VALID path-match-out-has-2-ext")]
        [DataRow(@"C:\one\two\three\file.two.mbox", @"C:\one\two\three\file.two.out", false, true, DisplayName = "WINDOWS_VALID path-match-has-2-ext-except-ext")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\folder.out", true, true, DisplayName = "WINDOWS_VALID path-same-parent-diff-subfolder")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file.out\out", false, true, DisplayName = "WINDOWS_VALID path-match-except-ext-subfolder")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file\one\two\three", false, true, DisplayName = "WINDOWS_VALID path-match-except-ext-deep-subfolder")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\out\file.out\two\three", true, true, DisplayName = "WINDOWS_VALID path-match-in-subfolder-deep")]
        [DataRow(@"C:\one\two\three\file", @"C:\one\two\file", true, true, DisplayName = "WINDOWS_VALID path-child-less-depth")]
        [DataRow(@"C:\one\file", @"C:\out", true, true, DisplayName = "WINDOWS_VALID path-close-to-root")]
        [DataRow(@"C:\one\file", @"C:\one\file.out", false, true, DisplayName = "WINDOWS_VALID path-close-to-root-not-valid")]
        [DataRow(@"C:\file", @"C:\one", true, true, DisplayName = "WINDOWS_VALID path-in-root-valid")]
        [DataRow(@"C:\file", @"C:\file.out", false, true, DisplayName = "WINDOWS_VALID path-in-root-not-valid")]
        [DataRow(@"C:\file", @"C:\", true, true, DisplayName = "WINDOWS_VALID path-out-is-root")]

        [DataRow(@"/one/two/three/file", @"/one/two/three/file", false, false, DisplayName = "LINUX_VALID path-exact-match")]
        [DataRow(@"/mnt/c/one/two/three/file", @"/mnt/d/one/two/three/file", true, false, DisplayName = "LINUX_VALID path-exact-match-different-drive")]
        [DataRow(@"/one/two/three/file.two.mbox", @"/one/two/three/file.out", true, false, DisplayName = "LINUX_VALID path-match-in-has-2-ext")]
        [DataRow(@"/one/two/three/file.mbox", @"/one/two/three/file.mbox.out", true, false, DisplayName = "LINUX_VALID path-match-out-has-2-ext")]
        [DataRow(@"/one/two/three/file.mbox", @"/one/two/three/file.mbox.out", true, false, DisplayName = "LINUX_VALID path-match-out-has-2-ext")]
        [DataRow(@"/one/two/three/file.two.mbox", @"/one/two/three/file.two.out", false, false, DisplayName = "LINUX_VALID path-match-has-2-ext-except-ext")]
        [DataRow(@"/one/two/three/file.mbox", @"/one/two/three/folder.out", true, false, DisplayName = "LINUX_VALID path-same-parent-diff-subfolder")]
        [DataRow(@"/one/two/three/file.mbox", @"/one/two/three/file.out/out", false, false, DisplayName = "LINUX_VALID path-match-except-ext-subfolder")]
        [DataRow(@"/one/two/three/file.mbox", @"/one/two/three/file/one/two/three", false, false, DisplayName = "LINUX_VALID path-match-except-ext-deep-subfolder")]
        [DataRow(@"/one/two/three/file.mbox", @"/one/two/three/out/file.out/two/three", true, false, DisplayName = "LINUX_VALID path-match-in-subfolder-deep")]
        [DataRow(@"/one/two/three/file", @"/one/two/file", true, false, DisplayName = "LINUX_VALID path-child-less-depth")]
        [DataRow(@"/one/file", @"/out", true, false, DisplayName = "LINUX_VALID path-close-to-root")]
        [DataRow(@"/one/file", @"/one/file.out", false, false, DisplayName = "LINUX_VALID path-close-to-root-not-valid")]
        [DataRow(@"/file", @"/one", true, false, DisplayName = "LINUX_VALID path-in-root-valid")]
        [DataRow(@"/file", @"/file.out", false, false, DisplayName = "LINUX_VALID path-in-root-not-valid")]
        [DataRow(@"/file", @"/", true, false, DisplayName = "LINUX_VALID path-out-is-root")]

        [DataTestMethod]
        public void TestIsValidOutputPath(string inFile, string outFolder, bool expected, bool isWindows)
        {
            if (OperatingSystem.IsWindows() == isWindows)
            {
                var valid = EaPdf.Helpers.FilePathHelpers.IsValidOutputPathForMboxFile(inFile, outFolder);
                Assert.AreEqual(expected, valid);
            }
            else
            {
                Assert.Inconclusive($"'{inFile}' '{outFolder}' Test is not for this OS.");
            }


        }


        [DataRow(@"C:\", @"C:\one\file", true, DisplayName = "WINDOWS_INVALID path-in-is-root1")]
        [DataRow(@"C:/", @"C:/one/file", true, DisplayName = "WINDOWS_INVALID path-in-is-root2")]
        [DataRow(@"/", @"/one/file", true, DisplayName = "WINDOWS_INVALID path-in-is-root3")]

        [DataRow(@"/", @"/one/file", false, DisplayName = "LINUX_INVALID path-in-is-root")]

        [DataTestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInvalidFileName(string inFile, string outFolder, bool isWindows)
        {
            if (OperatingSystem.IsWindows() == isWindows) 
            {
                _ = EaPdf.Helpers.FilePathHelpers.IsValidOutputPathForMboxFile(inFile, outFolder);
            }
            else
            {
                Assert.Inconclusive($"'{inFile}' '{outFolder}' Test is not for this OS.");
            }

        }
    }
}
