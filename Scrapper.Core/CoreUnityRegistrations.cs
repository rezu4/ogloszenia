using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Scraper.Storage;

namespace Scrapper.Core
{
    public static class CoreUnityRegistrations
    {
        public static void Register(IUnityContainer container)
        {
            container.RegisterType<IOfferStorage, OfferStorage>();
        }
    }
}
