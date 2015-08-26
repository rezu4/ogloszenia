using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Scrapper.Core.Utils;

namespace Scraper
{
    public class HtmlClient
    {
        public string GetHtml(string url)
        {
            var result = string.Empty;

            if (!TryGetFromCache(url, out result))
            {
                using (var wc = new WebClient())
                {
                    var src = wc.DownloadData(url);
                    Encoding enc = Encoding.GetEncoding("iso-8859-2");
                    UTF8Encoding utf8 = new UTF8Encoding();                  
                    byte[] dst = Encoding.Convert(enc, utf8, src);
                    result = utf8.GetString(dst);
                    
                    SaveCache(url, result);
                }
            }
            
            return result;
        }

        public bool TryGetFromCache(string url, out string html)
        {
            var filePath = GetCacheFileName(url);

            if (File.Exists(filePath))
            {
                var fi = new FileInfo(filePath);
                var lastWriteAt = DateTime.UtcNow - fi.LastWriteTimeUtc;
                if (lastWriteAt.TotalMinutes < 10)
                {
                    
                    html = File.ReadAllText(filePath);
                    return true;
                }
            }

            html = null;
            return false;
        }

        private void SaveCache(string url, string html)
        {
            var filePath = GetCacheFileName(url);
            File.WriteAllText(filePath, html);
        }

        private string GetCacheFileName(string url)
        {
            var mainDir = Path.Combine(FileHelper.GetAssemblyRootPath(), "http_cache");

            if (!Directory.Exists(mainDir))
            {
                Directory.CreateDirectory(mainDir);
            }

            var file = String.Format("{0}.html", CalculateMD5Hash(url));
            return Path.Combine(mainDir, file);
        }

        public string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
