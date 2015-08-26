using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using AutoMapper;
using Scraper.Storage;
using Scraper.Web.Models;

namespace Scraper.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IOfferStorage _storage ;

        public HomeController(IOfferStorage storage)
        {
            _storage = storage;
        }

        public ActionResult Index()
        {
            var offers = _storage.Search("*:*");
            var viewModel = new OfferSearchViewModel();
            viewModel.Offers = new List<OfferViewModel>();

            
            Mapper.Map(offers, viewModel.Offers);

            return View(viewModel);
        }

        [System.Web.Mvc.HttpPost]
        public JsonResult Search()
        {
            var offers = _storage.Search("*:*");
            var viewModel = new OfferSearchViewModel();
            viewModel.Offers = new List<OfferViewModel>();


            Mapper.Map(offers, viewModel.Offers);

            return new JsonResult() { Data = viewModel };
        }

        [System.Web.Mvc.HttpPost]
        public JsonResult Save([FromBody] OfferEditDataViewModel offerData)
        {
            var offer = _storage.GetOffer(offerData.OfferId);

            if (offer != null)
            {
                offer.Attractivenes = offerData.Attractivenes;
                offer.HaveSeen = offerData.HaveSeen;
                offer.Hide = offerData.Hide;
                offer.Notes = offerData.Notes;

                _storage.Save(offer);

                var result = new OfferViewModel();
                var savedOffer = _storage.GetOffer(offerData.OfferId);
                Mapper.Map(savedOffer, result);

                return new JsonResult() { Data = result };
            }

            return new JsonResult() {Data = null};
        }
    }
}
