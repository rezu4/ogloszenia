using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scraper.Web.Models
{
    public class OfferViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Price { get; set; }
        public string Description { get; set; }
        public List<string> Pictures { get; set; }
        public DateTime Date { get; set; }
        public bool Teaser { get; set; }

        public bool PrivateOffer { get; set; }
        public int Attractivenes { get; set; }
        public bool HaveSeen { get; set; }
        public bool Hide { get; set; }
        public string Notes { get; set; }
    }
}