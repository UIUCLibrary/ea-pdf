using MimeKit;
using NDepend.Path;
using Wiry.Base32;

namespace UIUCLibrary.EaPdf.Helpers
{
    public class FilePathHelpers
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
            bool ret;

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
                    if (!paths.TryGetCommonRootDirectory(out _))
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

        /// <summary>
        /// Get a random file name that does not exist in the given directory
        /// </summary>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static string GetRandomFilePath(string folderPath)
        {
            //get random file name that doesn't already exist
            string randomFilePath;
            do
            {
                randomFilePath = Path.Combine(folderPath, Path.GetRandomFileName());
            } while (File.Exists(randomFilePath));

            return randomFilePath;
        }

        /// <summary>
        /// Get a file path based on a hash with the appropriate filename extension
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="part"></param>
        /// <param name="folderPath"></param>
        /// <param name="wrapInXml"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GetOutputFilePathBasedOnHash(byte[] hash, MimePart part, string folderPath, bool wrapInXml)
        {
            string hashStr;
            if (hash != null)
                hashStr = Base32Encoding.ZBase32.GetString(hash, 0, hash.Length); // uses z-base-32 encoding for file names, https://en.wikipedia.org/wiki/Base32
            else
                throw new ArgumentNullException(nameof(hash));

            //for convenience get an extension to use with the derived filename
            string ext;
            if (wrapInXml)
            {
                ext = ".xml";
            }
            else
            {
                ext = Path.GetExtension(part.FileName); //try to use the extension of the mime header file name
                if (string.IsNullOrWhiteSpace(ext) || ext.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    //if file doesn't have an extension, or if the extension is not valid, should try to use the content type to get the extension
                    if (!MimeTypes.TryGetExtension(part.ContentType.MimeType, out ext))
                    {
                        ext = MimeTypeMap.DefaultExtension; //if no extension found, use the default .bin
                    }
                }
            }

            var hashFilePath = Path.Combine(folderPath, hashStr[..2], Path.ChangeExtension(hashStr, ext));

            return hashFilePath;
        }

        /// <summary>
        /// Read to the end of a stream to ensure the hash is correct
        /// </summary>
        /// <param name="stream"></param>
        public static void ReadToEnd(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            const int bufSz = 4096 * 2;  // 8K

            int i;
            byte[] buffer = new byte[bufSz];
            do
            {
                i = stream.Read(buffer, 0, bufSz);
            } while (i > 0);
        }


    }
}
