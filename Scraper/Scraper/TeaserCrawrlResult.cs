using System.Collections.Generic;
using Scraper.Storage;

namespace Scraper.Scraper
{
    public class TeaserCrawrlResult
    {
        public List<string> TeaserUrls { get; set; }

        public List<Offer> Teasers { get; set; }
    }
}