using Microsoft.Practices.Unity;
using Scraper.Scraper;
using Scraper.Storage;
using Scrapper.Core;

namespace Scraper
{     
    class Program
    {
        private static UnityContainer _container;
		// --
        private static void Initialize()
        {
            _container = new UnityContainer();
            CoreUnityRegistrations.Register(_container);
            CrawlerUnityRegistrations.Register(_container);                        
        }

        static void Main(string[] args)
        {
            Initialize();

            var crawler = _container.Resolve<IOfferUrlCrawler>();            
            crawler.GetAllTeasers();
            crawler.GetFullOffers();
            

             var storage = _container.Resolve<IOfferStorage>();
            
            //storage.Save(offer);
            //var offers = storage.Search("*:*");
            //var teasers = storage.GetTeasers();
        }       
    }
}
