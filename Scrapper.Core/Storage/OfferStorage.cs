using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Scrapper.Core.Utils;
using Version = Lucene.Net.Util.Version;

namespace Scraper.Storage
{
    public class OfferStorage : IOfferStorage
    {
        private FSDirectory _directoryTemp;

        private FSDirectory _directory
        {
            get
            {
                var mainDir = FileHelper.GetAssemblyRootPath();
                var luceneDir = Path.Combine(mainDir, "lucene");

                if (!System.IO.Directory.Exists(luceneDir))
                {
                    System.IO.Directory.CreateDirectory(luceneDir);
                }

                _directoryTemp = FSDirectory.Open(new DirectoryInfo(luceneDir));
                if (IndexWriter.IsLocked(_directoryTemp))
                {
                    IndexWriter.Unlock(_directoryTemp);
                }

                var lockFilePath = Path.Combine(luceneDir, "write.lock");
                if (File.Exists(lockFilePath))
                {
                    File.Delete(lockFilePath);
                }
                return _directoryTemp;
            }
        }

        public void Delete(string id)
        {
            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                var searchQuery = new TermQuery(new Term("Id", id));
                writer.DeleteDocuments(searchQuery);
                analyzer.Close();
                writer.Dispose();
            }
        }

        public Offer GetOffer(string id)
        {
            var offersMatch =  Search("Id:" + id).ToList();

            if (offersMatch.Count == 1)
            {
                return offersMatch[0];
            }

            return null;
        }

        private void Optimize()
        {
            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                analyzer.Close();
                writer.Optimize();
                writer.Dispose();
            }
        }

        public bool ClearAll()
        {
            try
            {
                var analyzer = new StandardAnalyzer(Version.LUCENE_30);
                using (var writer = new IndexWriter(_directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    writer.DeleteAll();
                    analyzer.Close();
                    writer.Dispose();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;

        }

        public void Save(Offer offer)
        {
            var analyzer = new StandardAnalyzer(Version.LUCENE_30);
            using (var writer = new IndexWriter(_directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                Save(offer, writer);

                // close handles
                analyzer.Close();
                writer.Dispose();
            }
        }

        private void Save(Offer offer, IndexWriter writer)
        {
            // remove older index entry
            var searchQuery = new TermQuery(new Term("Id", offer.Id));
            writer.DeleteDocuments(searchQuery);

            // add new index entry
            var doc = new Document();
            
            // add lucene fields mapped to db fields
            doc.Add(new Field("Id", offer.Id, Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Title", offer.Title, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("Url", offer.Url, Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Price", offer.Price, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("Description", offer.Description, Field.Store.YES, Field.Index.ANALYZED));
            var strDate = DateTools.DateToString(offer.Date, DateTools.Resolution.DAY);
            doc.Add(new Field("Date", strDate, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("Teaser", offer.Teaser?"1":"0", Field.Store.YES, Field.Index.ANALYZED));           
            doc.Add(new Field("PrivateOffer", offer.PrivateOffer?"1":"0", Field.Store.YES, Field.Index.ANALYZED));           
            
            if (offer.Pictures != null)
            {
                foreach (var picture in offer.Pictures)
                {
                    doc.Add(new Field("Pictures", picture, Field.Store.YES, Field.Index.NOT_ANALYZED));
                }
            }
                        
            doc.Add(new Field("Attractivenes", offer.Attractivenes.ToString(CultureInfo.InvariantCulture), Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("HaveSeen", offer.HaveSeen?"1":"0", Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("Hide", offer.Hide ? "1" : "0", Field.Store.YES, Field.Index.ANALYZED));

            if (!string.IsNullOrEmpty(offer.Notes))
            {
                doc.Add(new Field("Notes", offer.Notes, Field.Store.YES, Field.Index.ANALYZED));
            }

            // add entry to index
            writer.AddDocument(doc);
        }

        private Offer MapOffer(Document doc)
        {
            int attractivenes;
            var strAttractivenes = doc.Get("Attractivenes");
            int.TryParse(strAttractivenes, out attractivenes);
            return new Offer
            {
                Id = doc.Get("Id"),
                Title = doc.Get("Title"),
                Url = doc.Get("Url"),
                Price = doc.Get("Price"),
                Description = TextHelper.CleanText(doc.Get("Description")),
                Pictures = doc.GetValues("Pictures").ToList(),
                Date = DateTools.StringToDate(doc.Get("Date")),                
                Teaser = doc.Get("Teaser")=="1",
                PrivateOffer = doc.Get("PrivateOffer") == "1",
                Attractivenes = attractivenes,                
                HaveSeen = doc.Get("HaveSeen") == "1",
                Hide = doc.Get("Hide") == "1",
                Notes = doc.Get("Notes")
            };
        }
      
        private IEnumerable<Offer> MapOffers(IEnumerable<ScoreDoc> hits,
            IndexSearcher searcher)
        {
            return hits.Select(hit => MapOffer(searcher.Doc(hit.Doc))).ToList();
        }

        private Query ParseQuery(string searchQuery, QueryParser parser)
        {
            Query query;
            try
            {
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
            }
            return query;
        }

        private IEnumerable<Offer> SearchInternal(string searchQuery)
        {
            // validation
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", "")))
            {
                searchQuery = "*";
            }

            // set up lucene searcher
            using (var searcher = new IndexSearcher(_directory, false))
            {
                const int hitsLimit = 1000;
                var analyzer = new StandardAnalyzer(Version.LUCENE_30);

                var sort = new Sort(new SortField("Date", SortField.LONG, true));

                var parser = new MultiFieldQueryParser
                    (Version.LUCENE_30, new[] {"Id", "Title", "Url", "Price", "Description"}, analyzer);
                var query = ParseQuery(searchQuery, parser);
                var hits = searcher.Search(query, null, hitsLimit, sort).ScoreDocs;
                var results = MapOffers(hits, searcher);
                analyzer.Close();
                searcher.Dispose();
                return results;
            }
        }


        public IEnumerable<Offer> Search(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                input = "*:*";
            }

            input += " NOT Description:kamienic*  NOT Description:dwufunkc* NOT Description:dworskiego*  NOT Description:borelowskiego* NOT Title:kamienic* NOT Title:dwufunkc* NOT Title:dworskiego* NOT Title:borelowskiego* ";
            // do remontu
                        
            var result =  SearchInternal(input);

            return result;
        }

        public List<Offer> GetTeasers()
        {
            return Search("Teaser:1").ToList();
        }
    }
}
