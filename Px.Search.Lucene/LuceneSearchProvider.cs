using PX.SearchAbstractions;
using System.IO;
using System.Web;
using System.Text;

namespace PX.LuceneProvider48
{
    public class LuceneSearchProvider : IPxSearchProvider
    {
        private string _database;
        private string _language;
        private DirectoryInfo _databaseBaseDirectory;
        public LuceneSearchProvider(string databaseBaseDirectory, string database, string language) {
            _database = database;
            _language = language;
            _databaseBaseDirectory = GetDatabaseBaseDirectory(databaseBaseDirectory);
        }


        public IIndexer GetIndexer()
        {
            string path = GetIndexDirectoryPath();
            return new LuceneIndexer(path, _database);
        }

        public ISearcher GetSearcher()
        {
            string path = GetIndexDirectoryPath();
            return new LuceneSearcher(path);
        }

        /// <summary>
        /// Set the index base directory
        /// </summary>
        /// <param name="indexDirectory">Base directory for all search indexes</param>
        private DirectoryInfo GetDatabaseBaseDirectory(string databaseBaseDirectory)
        {
            if (System.IO.Directory.Exists(databaseBaseDirectory))
            {
                return new DirectoryInfo(databaseBaseDirectory);
            }
            return null;
        }

        /// <summary>
        /// Get path to the specified index directory 
        /// </summary>
        /// <param name="database">database</param>
        /// <param name="language">language</param>
        /// <returns></returns>
        private string GetIndexDirectoryPath()
        {
            StringBuilder dir = new StringBuilder(_databaseBaseDirectory.FullName);

            dir.Append(_database);
            dir.Append(@"\_INDEX\");
            dir.Append(_language);

            return dir.ToString();
        }
    }
}
