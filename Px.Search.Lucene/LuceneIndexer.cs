using PCAxis.Paxiom;
using PX.SearchAbstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Index;
using PCAxis.Menu;
using Lucene.Net.Documents;
using PCAxis.Web.Core.Enums;
using PCAxis.Paxiom.Extensions;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Miscellaneous;

namespace PX.LuceneProvider48
{
    public class LuceneIndexer : IIndexer
    {
        private const int DefaultMaxFieldLength = 10000;
        private string _indexDirectory;
        private string _database;
        private IndexWriter _writer;
        private bool _running;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="indexDirectory">Index directory</param>
        /// <param name="database">Database id</param>
        public LuceneIndexer(string indexDirectory, string database)
        {
            _indexDirectory = indexDirectory;
            _database = database;

        }
        public void AddPaxiomDocument(string database, string id, string path, string table, string title, DateTime published, PXMeta meta)
        {
            Document doc = GetDocument(database, id, path, table, title, published, meta);

            _writer.AddDocument(doc);
        }

        public void UpdatePaxiomDocument(string database, string id, string path, string table, string title, DateTime published, PXMeta meta)
        {
            Document doc = GetDocument(database, id, path, table, title, published, meta);
            _writer.UpdateDocument(new Term(SearchConstants.SEARCH_FIELD_DOCID, doc.Get(SearchConstants.SEARCH_FIELD_DOCID)), doc);
        }

        public void Create(bool createIndex)
        {
            _writer = CreateIndexWriter();
            /*
            if (createIndex)
            {
                _writer.SetMaxFieldLength(int.MaxValue);
            }*/
        }

        public void Dispose()
        {
            if (_running) {
                _writer.Rollback(); 
            }
            _writer.Dispose();
        }

        public void End()
        {
            _running = false;
        }

        /// <summary>
        /// Get Document object representing the table
        /// </summary>
        /// <param name="database">Database id</param>
        /// <param name="id">Id of document (table)</param>
        /// <param name="path">Path to table within database</param>
        /// <param name="path">Table</param>
        /// <param name="meta">PXMeta object</param>
        /// <returns>Document object representing the table</returns>
        private Document GetDocument(string database, string id, string path, string table, string title, DateTime published, PXMeta meta)
        {
            Document doc = new Document();

            if (meta != null)
            {
                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(table) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(meta.Title) || string.IsNullOrEmpty(meta.Matrix) || meta.Variables.Count == 0)
                {
                    return doc;
                }

                doc.Add(new StringField(SearchConstants.SEARCH_FIELD_DOCID, id, Field.Store.YES)); // Used as id when updating a document - NOT searchable!!!
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_SEARCHID, id, Field.Store.NO)); // Used for finding a document by id - will be used for generating URL from just the tableid - Searchable!!!
                doc.Add(new StoredField(SearchConstants.SEARCH_FIELD_PATH, path));
                doc.Add(new StoredField(SearchConstants.SEARCH_FIELD_TABLE, table));
                doc.Add(new StringField(SearchConstants.SEARCH_FIELD_DATABASE, database, Field.Store.YES));
                doc.Add(new StringField(SearchConstants.SEARCH_FIELD_PUBLISHED, published.DateTimeToPxDateString(), Field.Store.YES));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_MATRIX, meta.Matrix, Field.Store.YES));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_TITLE, title, Field.Store.YES));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_VARIABLES, string.Join(" ", (from v in meta.Variables select v.Name).ToArray()), Field.Store.NO));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_PERIOD, meta.GetTimeValues(), Field.Store.NO));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_VALUES, meta.GetAllValues(), Field.Store.NO));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_CODES, meta.GetAllCodes(), Field.Store.NO));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_GROUPINGS, meta.GetAllGroupings(), Field.Store.NO));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_GROUPINGCODES, meta.GetAllGroupingCodes(), Field.Store.NO));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_VALUESETS, meta.GetAllValuesets(), Field.Store.NO));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_VALUESETCODES, meta.GetAllValuesetCodes(), Field.Store.NO));
                doc.Add(new TextField(SearchConstants.SEARCH_FIELD_TABLEID, meta.TableID == null ? meta.Matrix : meta.TableID, Field.Store.YES));
                if (!string.IsNullOrEmpty(meta.Synonyms))
                {
                    doc.Add(new TextField(SearchConstants.SEARCH_FIELD_SYNONYMS, meta.Synonyms, Field.Store.NO));
                }

            }

            return doc;
        }

        /// <summary>
        /// Get Lucene.Net IndexWriter object 
        /// </summary>
        /// <returns>IndexWriter object. If the Index directory is locked, null is returned</returns>
        private IndexWriter CreateIndexWriter()
        {
            FSDirectory fsDir = FSDirectory.Open(_indexDirectory);

            if (IndexWriter.IsLocked(fsDir))
            {
                return null;
            }
            Lucene.Net.Util.LuceneVersion luceneVersion = Lucene.Net.Util.LuceneVersion.LUCENE_48;

            Analyzer analyzer = new StandardAnalyzer(luceneVersion);

            IndexWriterConfig config = new IndexWriterConfig(luceneVersion, analyzer)
            {
                OpenMode = OpenMode.CREATE_OR_APPEND // Creates a new index if one does not exist, otherwise it opens the index and documents will be appended.
            };
            IndexWriter writer = new IndexWriter(fsDir, config);

            //IndexWriter writer = new IndexWriter(fsDir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), createIndex, IndexWriter.MaxFieldLength.LIMITED);
            return writer;
        }
    }
}
