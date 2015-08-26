using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Scraper
{
    public static class FileHelper
    {
        public static string GetAssemblyRootPath()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            var dirPath = Path.GetDirectoryName(path);
            var curPath = dirPath;
            

            while (true)
            {
                var parentDiPath = Directory.GetParent(curPath).FullName;
                var dataDirPath = Path.Combine(parentDiPath, "data");
                if (Directory.Exists(dataDirPath))
                {
                    return dataDirPath;
                }

                curPath = parentDiPath;
            }

            return null;
        }
    }
}
