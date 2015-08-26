using System.Collections.Generic;

namespace Scraper.Storage
{
    public interface IOfferStorage
    {
        void Delete(string id);
        bool ClearAll();
        Offer GetOffer(string id);
        void Save(Offer offer);
        IEnumerable<Offer> Search(string input);
        List<Offer> GetTeasers();
    }
}