using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SMLDC.Simulator.Utilities
{
    public class MyFileIO
    {
       


        public static string ReturnAbsoluteOrRelativePath(string inputPath)
        {
            string filePath = "";
            if (Path.IsPathRooted(inputPath))
                //Absolute. Path is inputPath
                filePath = inputPath;
            else
                //Relative. Path is ../../../InputPath
                //Because the directory it starts at needs to go back to 4 levels.
                //from project/bin/debug/.netcoreapp3
                filePath = Path.GetFullPath($"../../../{inputPath}");

            DoesFileExist(filePath);

            return filePath;
        }

        public static void DoesFileExist(string filePath)
        {
            try
            {
                File.Exists(filePath);
            }
            catch (System.Exception e)
            {
                throw new Exception($"File does not exist. \nERROR: {e.Message}");
            }
        }


        public static string GetShortFileName(string filePath)
        {
            while(filePath.EndsWith("\\") || filePath.EndsWith("/"))
            {
                filePath = filePath.Substring(0, filePath.Length - 1);
            }
            int ndx_slash = filePath.LastIndexOf("/");
            int ndx_backslash = filePath.LastIndexOf("\\");
            int ndx_last = Math.Max(ndx_slash, ndx_backslash);
            if (ndx_last >= 0)
            {
                return filePath.Substring(ndx_last + 1);
            }
            else { return filePath; }
        }



        public static List<string> RecursiveDirSearch(string sDir, string[] allowedExtensions, string[] excludeDirs)
        {
            //Console.WriteLine("DirSearch..(" + sDir + ")");
            List<string> files = new List<string>();
            DirSearch_ex3(sDir, allowedExtensions, files, excludeDirs);
            return files;
        }


        // idee gebaseerd op : https://stackoverflow.com/a/929277
        public static void DirSearch_ex3(string sDir, string[] allowedExtensions, List<string> files, string[] excludeDirs)
        {
            //Console.WriteLine("DirSearch..(" + sDir + ")");
            try
            {
               // Console.WriteLine(sDir);

                foreach (string f in Directory.GetFiles(sDir))
                {
                    if(allowedExtensions.Any (f.ToLower().EndsWith ))
                    {
                       // Console.WriteLine(f);
                        files.Add(f);
                    }
                }

                foreach (string d in Directory.GetDirectories(sDir))
                {
                    if (!excludeDirs.Any(d.ToLower().Contains))
                    {
                        DirSearch_ex3(d, allowedExtensions, files, excludeDirs);
                    }
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }
    

        // https://stackoverflow.com/a/30082323
        public static string[] GetFiles(string folder, string[] allowedExtensions, SearchOption searchoption = SearchOption.TopDirectoryOnly)
        {
            //var allowedExtensions = new[] { ".doc", ".docx", ".pdf", ".ppt", ".pptx", ".xls", ".xslx" };
            return Directory
                .GetFiles(folder, "*.*", searchoption)
                .Where(file => allowedExtensions.Any(file.ToLower().EndsWith))
                .ToArray();
        }
    }
}
