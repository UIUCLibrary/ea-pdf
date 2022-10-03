using Microsoft.VisualStudio.TestTools.UnitTesting;
using NDepend.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
