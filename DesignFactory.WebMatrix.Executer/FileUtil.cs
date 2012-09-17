using System;
using System.Runtime.InteropServices;

namespace DesignFactory.WebMatrix.Executer
{
    static internal class FileUtil
    {
        /// <summary>
        /// Create a guaranteed writeable temporary file with a given file extension.
        /// </summary>
        /// <param name="fileExtension">File extension to use.</param>
        /// <returns></returns>
        static public string GetTempFile(string fileExtension)
        {
            string temp = System.IO.Path.GetTempPath();
            string res = string.Empty;
            while (true)
            {
                res = string.Format("{0}.{1}", Guid.NewGuid().ToString(), fileExtension);
                res = System.IO.Path.Combine(temp, res);
                if (!System.IO.File.Exists(res))
                {
                    try
                    {
                        System.IO.FileStream s = System.IO.File.Create(res);
                        s.Close();
                        break;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return res;
        }

        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        /// <summary>
        /// Create a symbolic link to a file.
        /// </summary>
        /// <param name="symbolicLink">Path to the symbolic link.</param>
        /// <param name="filePath">Path to the file to link to.</param>
        /// <returns>Truel if symbolic link created, false otherwise.</returns>
        static public bool CreateSymbolicLinkToFile(string symbolicLink, string filePath)
        {
            return CreateSymbolicLink(symbolicLink, filePath, 0);
        }
    }
}
