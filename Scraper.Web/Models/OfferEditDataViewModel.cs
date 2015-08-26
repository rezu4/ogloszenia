using System.Runtime.Serialization;

namespace Scraper.Web.Models
{
    [DataContract]
    public class OfferEditDataViewModel
    {
        [DataMember(Name = "offerId")]
        public string OfferId { get; set; } 

        [DataMember(Name = "attractivenes")]
        public int Attractivenes { get; set; } 

        [DataMember(Name = "haveSeen")]
        public bool HaveSeen { get; set; } 

        [DataMember(Name = "hide")]
        public bool Hide { get; set; } 

        [DataMember(Name = "notes")]
        public string Notes { get; set; } 
    }
}