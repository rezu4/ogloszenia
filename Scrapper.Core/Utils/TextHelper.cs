using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Scrapper.Core.Utils
{
    public static class TextHelper
    {
        public static string CleanText(string input)
        {
            var result = HttpUtility.HtmlDecode(input);
            result = SanitizeHtml(result);

            return result;
        }

        static string SanitizeHtml(string html)
        {
            string acceptable = "br|p";
            string stringPattern = @"</?(?(?=" + acceptable + @")notag|[a-zA-Z0-9]+)(?:\s[a-zA-Z0-9\-]+=?(?:(["",']?).*?\1?)?)*\s*/?>";
            return Regex.Replace(html, stringPattern, "");
        }
    }
}
