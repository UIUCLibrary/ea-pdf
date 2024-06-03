using MimeKit;
using System.Text.RegularExpressions;
using Wiry.Base32;

namespace UIUCLibrary.EaPdf.Helpers
{
    public static class FilePathHelpers
    {

        #region FileSystemInfo Extensions

        /// <summary>
        /// Determine if one FileSystemInfo is the child of another DirectoryInfo 
        /// </summary>
        /// <param name="child"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsChildOf(this FileSystemInfo child, DirectoryInfo parent)
        {
            if (child == null)
                throw new ArgumentNullException(nameof(child));
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            int parentLen = parent.FullName.Length;
            int childLen = child.FullName.Length;

            if(parentLen >= childLen)
            {
                return false;
            }

            if (child.EqualTo(parent))
            {
                return false;
            }

            char c = child.FullName[parentLen];
            if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
            {
                var comp = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                return child.FullName.IndexOf(parent.FullName, comp) == 0;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Determine if two FileSystemInfo objects are equal, using the normalized FullName properties,
        /// ignoring case on Windows, and ignoring any trailing directory separators
        /// </summary>
        /// <param name="dir1"></param>
        /// <param name="dir2"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool EqualTo(this FileSystemInfo dir1, FileSystemInfo dir2)
        {
            if (dir1 == null)
                throw new ArgumentNullException(nameof(dir1));
            if (dir2 == null)
                throw new ArgumentNullException(nameof(dir2));

            var comp = StringComparison.Ordinal;
            char[] sep = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

            if (OperatingSystem.IsWindows())
            {
                comp = StringComparison.OrdinalIgnoreCase;
            }

            return dir1.FullName.TrimEnd(sep).Equals(dir2.FullName.TrimEnd(sep), comp);
        }

        #endregion

        #region string Extensions for file paths

        /// <summary>
        /// Get the DirectoryInfo for a path string
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static DirectoryInfo ToDirectoryInfo(this string path)
        {
            if (TryGetDirectoryInfo(path, out DirectoryInfo? absPath, out string reason))
            {
                if (absPath != null)
                    return absPath;
                else
                    throw new Exception($"Unable to get DirectoryInfo for path '{path}'");
            }
            else
            {
                throw new ArgumentException($"'{path}' is not a valid absolute directory path, {reason}");
            }
        }

        /// <summary>
        /// Get the FileInfo for a path string
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static FileInfo ToFileInfo(this string path)
        {
            if (TryGetFileInfo(path, out FileInfo? absPath, out string reason))
            {
                if (absPath != null)
                    return absPath;
                else
                    throw new Exception($"Unable to get FileInfo for path '{path}'");
            }
            else
            {
                throw new ArgumentException($"'{path}' is not a valid absolute file path, {reason}");
            }
        }

        /// <summary>
        /// Try to get the absolute file path, return the result and the reason if it fails.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="outPath"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public static bool TryGetFileInfo(this string path, out FileInfo? absoluteFilePath, out string failureReason)
        {
            absoluteFilePath = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                failureReason = "Path is empty.";
                return false;
            }
            if (!path.IsValidAbsoluteFilePath(out failureReason))
            {
                absoluteFilePath = null;
                return false;
            }
            absoluteFilePath = new FileInfo(path);
            return true;
        }

        /// <summary>
        /// Try to get the absolute file path, return the result and the reason if it fails.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="outPath"></param>
        /// <param name="failureReason"></param>
        /// <returns></returns>
        public static bool TryGetDirectoryInfo(this string path, out DirectoryInfo? absoluteDirectoryPath, out string failureReason)
        {
            absoluteDirectoryPath = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                failureReason = "Path is empty.";
                return false;
            }
            if (!path.IsValidAbsoluteDirectoryPath(out failureReason))
            {
                absoluteDirectoryPath = null;
                return false;
            }
            absoluteDirectoryPath = new DirectoryInfo(path);
            return true;
        }



        /// <summary>
        /// Determine if a path is a valid absolute file path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static bool IsValidAbsoluteFilePath(this string path, out string reason)
        {
            bool ret;

            try
            {
                ret = Path.IsPathFullyQualified(path);
                if (ret)
                {
                    var fileName = Path.GetFileName(path);
                    var dirName = Path.GetDirectoryName(path);
                    if (string.IsNullOrWhiteSpace(fileName) || fileName == "." || fileName == "..")
                    {
                        ret = false;
                        reason = "Doesn't appear to be a file path.";
                    }
                    else
                    {
                        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                        {
                            ret = false;
                            reason = "File name contains invalid characters.";
                        }
                        else if (dirName != null && dirName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                        {
                            ret = false;
                            reason = "Directory name contains invalid characters.";
                        }
                        else
                        {
                            ret = true;
                            reason = "";
                        }
                    }
                }
                else
                {
                    ret = false;
                    reason = "Path is not fully qualified.";
                }
            }
            catch (Exception ex)
            {
                ret = false;
                reason = ex.Message;
            }

            return ret;
        }

        /// <summary>
        /// Determine if a path is a valid absolute directory path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static bool IsValidAbsoluteDirectoryPath(this string path, out string reason)
        {
            bool ret;

            try
            {
                ret = Path.IsPathFullyQualified(path);
                if (ret)
                {
                    if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    {
                        ret = false;
                        reason = "Directory name contains invalid characters.";
                    }
                    else
                    {
                        ret = true;
                        reason = "";
                    }
                }
                else
                {
                    ret = false;
                    reason = "Path is not fully qualified.";
                }
            }
            catch (Exception ex)
            {
                ret = false;
                reason = ex.Message;
            }

            return ret;
        }

        #endregion

        #region Misc. File Path Helpers


        public static int FolderDepth(DirectoryInfo absPath)
        {
            int ret = 0;

            var parentPath = absPath;
            while (parentPath.Parent != null)
            {
                ret++;
                parentPath = parentPath.Parent;
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
        /// Read to the end of a stream to ensure the hash is correct
        /// Not really file path related, but useful for file processing
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

        /// <summary>
        /// Shorten the path to just the outer directory name and the file name; useful for logging and error messages
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ShortenedPath(string path)
        {
            var dir = Path.GetFileName(Path.GetDirectoryName(path)) ?? "";
            var file = Path.GetFileName(path);
            var ret = Path.Combine(Path.GetFileName(dir), file);
            if (ret.Length < path.Length)
            {
                ret = $"...\\{ret}";
            }
            return ret;
        }

        /// <summary>
        /// Look for a subfolder name which matches the given file name, ignoring extensions
        /// i.e. Mozilla Thunderbird will append the extension '.sbd' to the folder name
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string? GetSubfolderNameMatchingFileName(string filePath, out string message)
        {
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            string dirName = Path.GetDirectoryName(filePath) ?? "";
            if (string.IsNullOrWhiteSpace(dirName))
                throw new ArgumentException($"The directory name of '{filePath}' is empty; filePath must be the absolute path of a file.");

            string fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException($"The file name of '{filePath}' is empty; filePath must be the absolute path of a file.");


            string? subfolderName = null;

            string[]? subfolders = Directory.GetDirectories(dirName, $"{fileName}.*"); //first look for folders matching the parent name, including extension
            if (subfolders == null || subfolders.Length == 0)
            {
                //if not found, look for names matching the parent without extension
                subfolders = Directory.GetDirectories(dirName, $"{Path.GetFileNameWithoutExtension(fileName)}.*");
            }

            if (subfolders != null)
            {
                try
                {
                    subfolderName = subfolders.SingleOrDefault();
                }
                catch (InvalidOperationException)
                {
                    message = $"There is more than one folder that matches '{fileName}.*'; skipping all subfolders";
                    subfolderName = null;
                }
                catch (Exception ex)
                {
                    message = $"Skipping subfolders. {ex.GetType().Name}: {ex.Message}";
                    subfolderName = null;
                }
            }

            return subfolderName;
        }

        /// <summary>
        /// Get the longest common prefix of a set of strings, assuming the strings are platform-specific directory paths
        /// https://stackoverflow.com/questions/24866683/find-common-parent-path-in-list-of-files-and-directories
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static string GetLongestCommonPrefix(params string[] s)
        {
            //TODO: Use FileSystemInfo instead of string for the paths
            int k = s[0].Length;
            for (int i = 1; i < s.Length; i++)
            {
                k = Math.Min(k, s[i].Length);
                for (int j = 0; j < k; j++)
                {
                    char c1 = OperatingSystem.IsWindows() ? char.ToLowerInvariant(s[i][j]) : s[i][j];
                    char c2 = OperatingSystem.IsWindows() ? char.ToLowerInvariant(s[0][j]) : s[0][j];
                    if (c1 != c2)
                    {
                        k = j;
                        break;
                    }
                }
            }
            return s[0].Substring(0, k);
        }

        #endregion

        #region File Path Manipulation specifically for EA-PDF files

        /// <summary>
        /// Given the output folder path, input file path, and a set of created files, get the output file path for the XML file
        /// </summary>
        /// <param name="fullOutFolderPath"></param>
        /// <param name="fullInFilePath"></param>
        /// <param name="createdFiles">dictionary of previously created files, may be null</param>
        /// <returns></returns>
        public static string GetXmlOutputFilePath(string fullOutFolderPath, string fullInFilePath, Dictionary<string, string>? createdFiles)
        {
            //simplest way to ensure unique output filenames is by appending .xml (not replacing) as the new file extension
            var xmlFilePath = Path.Combine(fullOutFolderPath, Path.GetFileName(fullInFilePath) + ".xml");

            createdFiles?.Add(xmlFilePath, fullInFilePath);

            return xmlFilePath;
        }

        public static string GetXmlOutputFilePath(string fullOutFolderPath, string fullInFilePath)
        {
            return GetXmlOutputFilePath(fullOutFolderPath, fullInFilePath, null);
        }

        public const string XML_WRAPPED_EXT = ".xmlw";

        /// <summary>
        /// Get the file path with the given number appended to the file name
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="incrNumber"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetFilePathWithIncrementNumber(string filePath, int incrNumber)
        {
            if (incrNumber > 9999)
                throw new Exception("No more than 9999 files are supported.");

            string ret = filePath;

            if (incrNumber == 0) //use the original file path w/o the number
            {
                return ret;
            }


            //Update the filepath to add the file number
            string directoryPath = Path.GetDirectoryName(filePath) ?? "";
            string extension = Path.GetExtension(filePath);
            ret = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(filePath) + "_" + incrNumber.ToString("0000") + extension);

            return ret;
        }

        /// <summary>
        /// Get the file path without the increment number at the end
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetFilePathWithoutIncrementNumber(string filePath)
        {

            //strip off the file number from the end
            string ext = Path.GetExtension(filePath);
            string name = Path.GetFileNameWithoutExtension(filePath);
            string dir = Path.GetDirectoryName(filePath) ?? "";

            string ret;
            if (Regex.IsMatch(name, "_\\d\\d\\d\\d$"))
            {
                name = name[..^5];
                ret = Path.Combine(dir, name + ext);
            }
            else
            {
                throw new Exception($"Unexpected filename format '{name}{ext}'.  It should end like '*_nnnn{ext}' with exactly 4 digits with leading zeros if needed");
            }

            return ret;
        }

        /// <summary>
        /// Try to get the file path without the increment number at the end, return the increment number if found, 
        /// else returns 0 with the result set to the original file path
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static int TryGetFilePathWithoutIncrementNumber(string filePath, out string result)
        {
            int ret = 0;
            result = filePath;

            //strip off the file number from the end
            string ext = Path.GetExtension(filePath);
            string name = Path.GetFileNameWithoutExtension(filePath);
            string dir = Path.GetDirectoryName(filePath) ?? "";

            if (Regex.IsMatch(name, "_\\d\\d\\d\\d$"))
            {
                ret = int.Parse(name[^4..]);
                name = name[..^5];
                result = Path.Combine(dir, name + ext);
            }

            return ret;
        }

        /// <summary>
        /// Get new EAXS and PDF file paths with the given number appended to the file name based on the continuation file name
        /// </summary>
        /// <param name="baseEaxsFilePath"></param>
        /// <param name="basePdfFilePath"></param>
        /// <param name="continuationFileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static (string eaxsFilePath, string pdfFilePath) GetDerivedFilePaths(string baseEaxsFilePath, string basePdfFilePath, string continuationFileName)
        {

            if (string.IsNullOrWhiteSpace(baseEaxsFilePath))
                throw new ArgumentNullException(nameof(baseEaxsFilePath));
            if (string.IsNullOrWhiteSpace(basePdfFilePath))
                throw new ArgumentNullException(nameof(basePdfFilePath));

            if (string.IsNullOrWhiteSpace(continuationFileName))
                return (string.Empty, string.Empty);

            string derivedEaxsFilePath = Path.Combine(Path.GetDirectoryName(baseEaxsFilePath) ?? "", continuationFileName);

            //sanity check for base file name matching and increment numbers always increasing by one
            int origEaxsIncrNumber = FilePathHelpers.TryGetFilePathWithoutIncrementNumber(baseEaxsFilePath, out string origEaxsBaseFilename);
            int derivedEaxsIncrNumber = FilePathHelpers.TryGetFilePathWithoutIncrementNumber(derivedEaxsFilePath, out string derivedEaxsBaseFilename);
            int origPdfIncrNumber = FilePathHelpers.TryGetFilePathWithoutIncrementNumber(basePdfFilePath, out string origPdfBaseFilename);

            if (Math.Abs(origEaxsIncrNumber - derivedEaxsIncrNumber) != 1) // +1 or -1 is ok depending on whether the continuation is the next or previous file
            {
                throw new Exception($"The increment number of the EAXS file '{baseEaxsFilePath}' is more than one away from than the increment number of the next EAXS file '{derivedEaxsFilePath}'.");
            }

            if (origEaxsBaseFilename != derivedEaxsBaseFilename)
            {
                throw new Exception($"The base filename of the EAXS file '{baseEaxsFilePath}' is not the same as the base filename of the next EAXS file '{derivedEaxsFilePath}'.");
            }

            if (origPdfIncrNumber != origEaxsIncrNumber)
            {
                throw new Exception($"The increment number of the PDF file '{basePdfFilePath}' is not the same as the increment number of the EAXS file '{baseEaxsFilePath}'.");
            }

            //get the next PDF file name with the same increment number as the EAXS file
            string derivedPdfFilePath = FilePathHelpers.GetFilePathWithIncrementNumber(origPdfBaseFilename, derivedEaxsIncrNumber);

            return (derivedEaxsFilePath, derivedPdfFilePath);
        }


        /// <summary>
        /// Determine whether the output path is allowed given the input folder path
        /// The output folder path cannot be the same as or a child folder of the input folder 
        /// The reason is that this could output files inside child mbox folders when the IncludeSubFolders setting is true
        /// </summary>
        /// <param name="absInPath"></param>
        /// <param name="absOutPath"></param>
        /// <returns></returns>
        public static bool IsValidOutputPathForMboxFolder(DirectoryInfo absInPath, DirectoryInfo absOutPath)
        {
            var ret = !(absOutPath.EqualTo(absInPath) || absOutPath.IsChildOf(absInPath));

            return ret;
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

            return IsValidOutputPathForMboxFolder(absInPath.ToDirectoryInfo(), absOutPath.ToDirectoryInfo());
        }


        /// <summary>
        /// Determine whether the output path is allowed given the input path
        /// The output folder path cannot be the same as or a child folder of the input file name path taken as a directory path, ignoring extensions
        /// The reason is that this could output files inside child mbox folders when the IncludeSubFolders setting is true
        /// </summary>
        /// <param name="absInPath"></param>
        /// <param name="absOutPath"></param>
        /// <returns></returns>
        public static bool IsValidOutputPathForMboxFile(DirectoryInfo absInPath, DirectoryInfo absOutPath)
        {
            if(absInPath == null)
                throw new ArgumentNullException(nameof(absInPath));
            if(absOutPath == null)
                throw new ArgumentNullException(nameof(absOutPath));

            bool ret;

            //treat the paths as a directory paths, minus any extension
            DirectoryInfo inDirPathNoExt = new(Path.Combine(Path.GetDirectoryName(absInPath.FullName) ??  Path.GetPathRoot(absInPath.FullName) ?? "", Path.GetFileNameWithoutExtension(absInPath.Name)));
            DirectoryInfo outDirPathNoExt;
            if (absOutPath.Parent != null)
            {
                outDirPathNoExt = new(Path.Combine(Path.GetDirectoryName(absOutPath.FullName) ?? Path.GetPathRoot(absOutPath.FullName) ?? "", Path.GetFileNameWithoutExtension(absOutPath.Name)));
            }
            else
            {
                //path is just the root of the drive
                outDirPathNoExt = absOutPath;
            }


            if (inDirPathNoExt.EqualTo(outDirPathNoExt))
            {
                ret = false;
            }
            else
            {
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
                    if (string.IsNullOrEmpty(GetLongestCommonPrefix(inDirPathNoExt.FullName, outDirPathNoExt.FullName)))
                    {
                        ret = true;
                    }
                    else
                    {
                        if(outDirPathNoExt.Parent == null)
                            throw new ArgumentNullException(nameof(outDirPathNoExt.Parent));

                        ret = IsValidOutputPathForMboxFile(absInPath, outDirPathNoExt.Parent);
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

            return IsValidOutputPathForMboxFile(absInPath.ToDirectoryInfo(), absOutPath.ToDirectoryInfo());
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
                ext = XML_WRAPPED_EXT; //TODO: what if the original filename already has an xmlw extension?
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


        #endregion

    }
}
