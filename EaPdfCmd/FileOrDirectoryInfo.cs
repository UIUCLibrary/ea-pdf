using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaPdfCmd
{


    /// <summary>
    /// Represents a file or directory on the file system. 
    /// </summary>
    public class FileOrDirectoryInfo : FileSystemInfo
    {
        private readonly FileSystemInfo value;

        public FileOrDirectoryInfo(string path)
        {
            OriginalPath = path;

            if (Directory.Exists(path))
            {
                value = new DirectoryInfo(path);
            }
            else if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                     path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                value = new DirectoryInfo(path);
            }
            else
            {
                value = new FileInfo(path);
            }

            FullPath = value.FullName;
        }

        public override bool Exists
        {
            get
            {
                return value.Exists;
            }
        }

        public override string Name
        {
            get
            {
                return value.Name;
            }
        }

        public override void Delete()
        {
            value.Delete();
        }

        public bool IsDirectory
        {
            get
            {
                return value is DirectoryInfo;
            }
        }

        public bool IsFile
        {
            get
            {
                return value is FileInfo;
            }
        }

    }
}
