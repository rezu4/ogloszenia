using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Scraper.Storage;
using Scraper.Web.Models;

namespace Scraper.Web.App_Start
{
    public static class AutoMapperConfig
    {
        public static void Configure()
        {
            Mapper.CreateMap<Offer, OfferViewModel>();
        }
    }
}