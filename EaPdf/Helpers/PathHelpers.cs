using NDepend.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class PathHelpers
    {

        /// <summary>
        /// Determine whether the output path is allowed given the input folder path
        /// The output folder path cannot be the same as or a child folder of the input folder 
        /// The reason is that this could output files inside child mbox folders when the IncludeSubFolders setting is true
        /// </summary>
        /// <param name="absInPath"></param>
        /// <param name="absOutPath"></param>
        /// <returns></returns>
        public static bool IsValidOutputPathForMboxFolder(IAbsoluteDirectoryPath absInPath, IAbsoluteDirectoryPath absOutPath)
        {
            return !(absOutPath.Equals(absInPath) || absOutPath.IsChildOf(absInPath));
        }

        /// <summary>
        /// Determine whether the output path is allowed given the input folder path
        /// The output folder path cannot be the same as or a child folder of the input folder 
        /// The reason is that this could output files inside child mbox folders when the IncludeSubFolders setting is true
        /// </summary>
        /// <param name="absInPath"></param>
        /// <param name="absOutPath"></param>
        /// <returns></returns>
        public static bool IsValidOutputPathForMboxFolder(string absInPath, string absOutPath)
        {
            if (string.IsNullOrWhiteSpace(absInPath))
                throw new ArgumentNullException(nameof(absInPath));
            if (string.IsNullOrWhiteSpace(absOutPath))
                throw new ArgumentNullException(nameof(absOutPath));

            if (!absInPath.IsValidAbsoluteDirectoryPath(out string reason))
                throw new ArgumentException($"'{absInPath}' is not a valid absolute directory path, {reason}");
            if (!absOutPath.IsValidAbsoluteDirectoryPath(out reason))
                throw new ArgumentException($"'{absOutPath}' is not a valid absolute directory path, {reason}");

            return IsValidOutputPathForMboxFolder(absInPath.ToAbsoluteDirectoryPath(), absOutPath.ToAbsoluteDirectoryPath());
        }


        /// <summary>
        /// Determine whether the output path is allowed given the input path
        /// The output folder path cannot be the same as or a child folder of the input file name path taken as a directory path, ignoring extensions
        /// The reason is that this could output files inside child mbox folders when the IncludeSubFolders setting is true
        /// </summary>
        /// <param name="absInPath"></param>
        /// <param name="absOutPath"></param>
        /// <returns></returns>
        public static bool IsValidOutputPathForMboxFile(IAbsoluteDirectoryPath absInPath, IAbsoluteDirectoryPath absOutPath)
        {
            bool ret = true;

            if (absInPath==null)
                throw new ArgumentNullException(nameof(absInPath));
            if (absOutPath==null)
                throw new ArgumentNullException(nameof(absOutPath));

            //treat the paths as a directory paths, minus any extension
            IAbsoluteDirectoryPath inDirPathNoExt = absInPath.ParentDirectoryPath.GetChildDirectoryWithName(Path.GetFileNameWithoutExtension(absInPath.ToString()));
            IAbsoluteDirectoryPath outDirPathNoExt;
            if (absOutPath.HasParentDirectory)
            {
                outDirPathNoExt = absOutPath.ParentDirectoryPath.GetChildDirectoryWithName(Path.GetFileNameWithoutExtension(absOutPath.ToString()));
            }
            else
            {
                //path is just the root of the drive
                outDirPathNoExt = absOutPath;
            }


            if (inDirPathNoExt.Equals(outDirPathNoExt))
            {
                ret = false;
            }
            else {
                int fullInDepth = FolderDepth(inDirPathNoExt);
                int fullOutDepth = FolderDepth(outDirPathNoExt);

                //if the out path is less deep than the parent of the in path, the out folder must be valid
                if (fullOutDepth <= fullInDepth - 1)
                {
                    ret = true;
                }
                else
                {
                    //if there is no common root, the out folder must be valid
                    var paths = new List<IAbsoluteDirectoryPath>() { inDirPathNoExt, outDirPathNoExt };
                    if (!paths.TryGetCommonRootDirectory(out IAbsoluteDirectoryPath commonRoot))
                    {
                        ret = true;
                    }
                    else
                    {
                        ret = IsValidOutputPathForMboxFile(absInPath, outDirPathNoExt.ParentDirectoryPath);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Determine whether the output path is allowed given the input path
        /// The output folder path cannot be the same as or a child folder of the input file name path taken as a directory path, ignoring extensions
        /// The reason is that this could output files inside child mbox folders when the IncludeSubFolders setting is true
        /// </summary>
        /// <param name="absInPath"></param>
        /// <param name="absOutPath"></param>
        /// <returns></returns>
        public static bool IsValidOutputPathForMboxFile(string absInPath, string absOutPath)
        {
            if (string.IsNullOrWhiteSpace(absInPath))
                throw new ArgumentNullException(nameof(absInPath));
            if (string.IsNullOrWhiteSpace(absOutPath))
                throw new ArgumentNullException(nameof(absOutPath));

            if (!absInPath.IsValidAbsoluteFilePath(out string reason))
                throw new ArgumentException($"'{absInPath}' is not a valid file path, {reason}");
            if (!absInPath.IsValidAbsoluteDirectoryPath(out reason))
                throw new ArgumentException($"'{absInPath}' is not a valid absolute directory path, {reason}");
            if (!absOutPath.IsValidAbsoluteDirectoryPath(out reason))
                throw new ArgumentException($"'{absOutPath}' is not a valid absolute directory path, {reason}");

            return IsValidOutputPathForMboxFile(absInPath.ToAbsoluteDirectoryPath(), absOutPath.ToAbsoluteDirectoryPath());
        }

        public static int FolderDepth(IAbsoluteDirectoryPath absPath)
        {
            int ret = 0;

            var parentPath = absPath;
            while (parentPath.HasParentDirectory)
            {
                ret++;
                parentPath = parentPath.ParentDirectoryPath;
            }

            return ret;
        }
    }
}
