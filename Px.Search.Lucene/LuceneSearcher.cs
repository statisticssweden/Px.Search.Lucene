using PX.SearchAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Store;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using PCAxis.Paxiom.Extensions;
using Lucene.Net.Index;

namespace PX.LuceneProvider48
{
    public class LuceneSearcher : ISearcher
    {
        private string _indexDirectory;
        private IndexSearcher _indexSearcher;
        private DateTime _creationTime;
        private static Operator _defaultOperator = Operator.OR;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="indexDirectory">Directory to search</param>
        public LuceneSearcher(string indexDirectory)
        {
            _indexDirectory = indexDirectory;
            _creationTime = DateTime.Now;

            FSDirectory fsDir = FSDirectory.Open(_indexDirectory);

            try
            {
                IndexReader reader = DirectoryReader.Open(fsDir);
                _indexSearcher = new IndexSearcher(reader);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<SearchResultItem> Search(string text, string filter, int resultListLength, out SearchStatusType status)
        {
            if (!System.IO.Directory.Exists(_indexDirectory))
            {
                status = SearchStatusType.NotIndexed;
                return new List<SearchResultItem>();
            }

            List<SearchResultItem> searchResult = new List<SearchResultItem>();
            string[] fields = GetSearchFields(filter);
            Lucene.Net.Util.LuceneVersion luceneVersion = Lucene.Net.Util.LuceneVersion.LUCENE_48;

            QueryParser qp = new MultiFieldQueryParser(luceneVersion,
                                                       fields,
                                                       new StandardAnalyzer(luceneVersion));

            qp.DefaultOperator = _defaultOperator;

            Query q = qp.Parse(text);
            TopDocs topDocs = _indexSearcher.Search(q, resultListLength);
            //hits = topDocs.TotalHits;
            foreach (var d in topDocs.ScoreDocs)
            {
                Document doc = _indexSearcher.Doc(d.Doc);
                searchResult.Add(new SearchResultItem()
                {
                    Path = doc.Get(SearchConstants.SEARCH_FIELD_PATH),
                    Table = doc.Get(SearchConstants.SEARCH_FIELD_TABLE),
                    Title = doc.Get(SearchConstants.SEARCH_FIELD_TITLE),
                    Score = d.Score,
                    Published = GetPublished(doc)
                });
            }

            status = SearchStatusType.Successful;
            return searchResult;
        }

        /// <summary>
        /// Set which operator AND/OR will be used by default when more than one word is specified for a search query
        /// </summary>
        /// <param name="defaultOPerator"></param>
        public void SetDefaultOperator(DefaultOperator defaultOperator)
        {
            if (defaultOperator == DefaultOperator.OR)
            {
                _defaultOperator = Operator.OR;
            }
            else
            {
                _defaultOperator = Operator.AND;
            }
        }

        /// <summary>
        /// The time the Searcher was created
        /// </summary>
        public DateTime CreationTime
        {
            get
            {
                return _creationTime;
            }
        }

        /// <summary>
        /// Get fields in index to search in
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string[] GetSearchFields(string filter)
        {
            string[] fields;


            if (string.IsNullOrEmpty(filter))
            {
                // Default fields
                fields = new[] { SearchConstants.SEARCH_FIELD_SEARCHID,
                                 SearchConstants.SEARCH_FIELD_TITLE,
                                 SearchConstants.SEARCH_FIELD_VALUES,
                                 SearchConstants.SEARCH_FIELD_CODES,
                                 SearchConstants.SEARCH_FIELD_MATRIX,
                                 SearchConstants.SEARCH_FIELD_VARIABLES,
                                 SearchConstants.SEARCH_FIELD_PERIOD,
                                 SearchConstants.SEARCH_FIELD_GROUPINGS,
                                 SearchConstants.SEARCH_FIELD_GROUPINGCODES,
                                 SearchConstants.SEARCH_FIELD_VALUESETS,
                                 SearchConstants.SEARCH_FIELD_VALUESETCODES,
                                 SearchConstants.SEARCH_FIELD_SYNONYMS };
            }
            else
            {
                // Get fields from filter
                fields = filter.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }

            return fields;
        }

        private DateTime GetPublished(Document doc)
        {
            DateTime published = DateTime.MinValue;
            string publishedStr = doc.Get(SearchConstants.SEARCH_FIELD_PUBLISHED);

            if (!string.IsNullOrEmpty(publishedStr))
            {
                if (PxDate.IsPxDate(publishedStr))
                {
                    published = publishedStr.PxDateStringToDateTime();
                }
            }

            return published;
        }
    }
}
