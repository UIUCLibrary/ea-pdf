using Microsoft.VisualStudio.TestTools.UnitTesting;
using NDepend.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIUCLibrary.EaPdf;


namespace UIUCLibrary.TestEaPdf
{
    [TestClass]
    public class TestNDependPath
    {

        [TestMethod]
        public void TestIsChildOf()
        {
            var ancestor = @"C:\one\two\three".ToAbsoluteDirectoryPath();
            
            var descendant = @"C:\one\two\three\four\five\six".ToAbsoluteDirectoryPath();
            Assert.IsTrue(descendant.IsChildOf(ancestor));


            descendant = @"C:\one\two\".ToAbsoluteDirectoryPath();
            Assert.IsFalse(descendant.IsChildOf(ancestor));

            descendant = @"C:\one\two\three\".ToAbsoluteDirectoryPath();
            Assert.IsFalse(descendant.IsChildOf(ancestor));
            
            descendant = @"C:\one\TWO\three/four/".ToAbsoluteDirectoryPath();
            Assert.IsTrue(descendant.IsChildOf(ancestor));

        }

        [DataRow(@"C:\one\two\three\file", @"C:\one\two\three\file", false, DisplayName = "path-exact-match")]
        [DataRow(@"C:\one\two\three\file", @"D:\one\two\three\file", true, DisplayName = "path-exact-match-different-drive")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file.out", false, DisplayName = "path-match-except-ext")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\folder.out", true, DisplayName = "path-same-parent-diff-subfolder")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file.out\out", false, DisplayName = "path-match-except-ext-subfolder")]
        [DataRow(@"C:\one\two\three\file.mbox", @"C:\one\two\three\file\one\two\three", false, DisplayName = "path-match-except-ext-deep-subfolder")]
        [DataRow(@"C:\one\two\three\file", @"C:\one\two\file", true, DisplayName = "path-child-less-depth")]
        [DataTestMethod]
        public void TestIsValidOutputPath(string inFile, string outFolder, bool expected)
        {

            var valid = EaPdf.Helpers.PathHelpers.IsValidOutputPathForMboxFile(inFile, outFolder);
            Assert.AreEqual(expected,valid);

        }
    }
}
