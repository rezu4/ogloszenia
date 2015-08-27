using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using CsQuery;
using CsQuery.ExtensionMethods;
using Scraper.Storage;
using Scrapper.Core.Utils;

namespace Scraper.Scraper
{
    public class OfferUrlCrawler : IOfferUrlCrawler
    {
        private readonly IOfferStorage _storage;

        public OfferUrlCrawler(IOfferStorage storage)
        {
            _storage = storage;
        }

        public void GetFullOffers()
        {
            var teasers = _storage.GetTeasers();

            var count = 0;
            var startTime = DateTime.Now;

            foreach (var teaser in teasers)
            {
                var fullOffer = GetFullOffer(teaser);
                _storage.Save(fullOffer);

                count++;
                var elapsed = DateTime.Now - startTime;
                var speed = (int) (count*1000/elapsed.TotalMilliseconds);
                var timeLeft = (int) ((teasers.Count - count)/(speed + 0.001));


                Console.Out.WriteLine("downloaded full offers {0}, speed {1}/s, complete in {2}:{3} [m:s]", count, speed,
                    (int) (timeLeft/60), timeLeft%60);
            }
        }

        private Offer GetFullOffer(Offer teaser)
        {
            var client = new HtmlClient();

            var html = client.GetHtml(teaser.Url);


            var dom = new CQ(html);

            var header = dom[".wspolny_naglowek_tytul"][0].InnerHTML;
            if (header.Contains("PRYWATNA"))
            {
                teaser.PrivateOffer = true;
            }
            
            var fullDescription = dom[".pokaz_ogloszenie_tresc"];
            for (var i = 0; i < fullDescription.Length; i++)
            {
                teaser.Description =  TextHelper.CleanText(fullDescription.RenderSelection());
            }

            var kontakt = dom["ul.pokaz_ogloszenie"][0].OuterHTML;

            Regex rgx = new Regex("<script.+script>", RegexOptions.Singleline);
            Match match = rgx.Match(kontakt);
            if (match.Success)
                kontakt = rgx.Replace(kontakt, "");

            teaser.Description += kontakt;

            var pictureEls = dom["img.pokaz_ogloszenie_obrazek"];

            var pictures = new List<string>();

            foreach (var picture in pictureEls)
            {
                var pictureUrl = "http://ogloszenia.przemysl.pl/" + picture.ParentNode.Attributes["href"];
                pictures.Add(pictureUrl);
            }

            if (pictures.Count > 0)
            {
                teaser.Pictures = pictures;
            }

            teaser.Teaser = false;

            return teaser;
        }

        public void GetAllTeasers()
        {
            var mainPage = "http://ogloszenia.przemysl.pl/mieszkanie.sprzedam";
            //var mainPage = "http://ogloszenia.przemysl.pl/dzialke.sprzedam";
            var urls = new List<string>();
            urls.Add(mainPage);

            var i = 0;

            while (i < urls.Count)
            {
                var curUrl = urls[i];
                var result = GetTeasers(curUrl);

                if (result != null)
                {
                    Console.Out.WriteLine("downloaded {0} teaers, {1} links", result.Teasers.Count, result.TeaserUrls.Count);
                    if (result.TeaserUrls != null)
                    {
                        foreach (var teaserUrl in result.TeaserUrls)
                        {
                            if (!urls.Contains(teaserUrl))
                            {
                                urls.Add(teaserUrl);
                            }
                        }
                    }

                    foreach (var teaser in result.Teasers)
                    {
                        _storage.Save(teaser);
                    }
                }

                i++;
            }
        }

        private TeaserCrawrlResult GetTeasers(string url)
        {
            var result = new TeaserCrawrlResult();

            var client = new HtmlClient();

            var html = client.GetHtml(url);


            var dom = new CQ(html);

            result.TeaserUrls = GetUrls(dom).ToList();
            result.Teasers = GetTeasers(dom);

            return result;
        }

        private List<Offer> GetTeasers(CQ dom)
        {
            var result = new List<Offer>();

            var offerEls = dom["h4.lista_ogloszen_w_kategorii"];

            foreach (var el in offerEls)
            {
                var oferElDom = new CQ(el.OuterHTML);
                //el.OuterHTML
                var aEl = oferElDom["a.lista_ogloszen_link"];
                if (aEl.Length == 0)
                {
                    continue;
                }

                var offer = new Offer();
                offer.Teaser = true;
                offer.Title = HttpUtility.HtmlDecode(aEl[0].InnerText);
                offer.Id = aEl[0].Attributes["href"];
                offer.Url = "http://ogloszenia.przemysl.pl/" + offer.Id;

                var pictures = new List<string>();

                var spans = oferElDom["span:not(.stopka_ogloszenia) span"];

                foreach (var span in spans)
                {
                    var txt = TextHelper.CleanText(span.InnerHTML);
                    if (!string.IsNullOrEmpty(txt))
                    {
                        offer.Description = txt;
                        break;
                    }
                }

                var imgs = oferElDom["span:not(.stopka_ogloszenia) span img"];

                foreach (var img in imgs)
                {

                    var pictureUrl = img.Attributes["src"];
                    if (offer.Pictures == null)
                    {
                        offer.Pictures = new List<string>();
                    }

                    pictureUrl = "http://ogloszenia.przemysl.pl/" + pictureUrl;
                    offer.Pictures.Add(pictureUrl);
                }

                var price = oferElDom["span.stopka_ogloszenia span"];

                if (price.Length > 0)
                {
                    offer.Price = HttpUtility.HtmlDecode(price[0].InnerText);
                }

                var dateEl = oferElDom["span.stopka_ogloszenia"];

                if (dateEl.Length > 0)
                {
                    var strDate = dateEl[0].InnerText;

                    var rx = new Regex("([0-9]{4})-([0-9]{2})-([0-9]{2})");
                    var m = rx.Match(strDate);
                    if (m.Success)
                    {
                        offer.Date = DateTime.ParseExact(m.Groups[0].Value,
                                        "yyyy-MM-dd",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None);                        
                    }

                }

                result.Add(offer);
            }

            return result;
        }

        private IEnumerable<string> GetUrls(CQ dom)
        {
            var urls = new HashSet<string>();
            var links = dom[".lista_stron a.lista_stron"];

            foreach (var link in links)
            {
                var href = "http://ogloszenia.przemysl.pl/" + link.Attributes["href"];
                urls.Add(href);
            }

            return urls;
        }
    }
}