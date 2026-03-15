using Grammar.Core.Helpers;
using Grammar.Czech.Interfaces;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonPrefixDataProvider : IPrefixDataProvider
    {
        private readonly string _prefixPath;
        private Dictionary<string, List<string>> _prefixes;

        public JsonPrefixDataProvider()
        {
            this._prefixPath = "Data.prefixes";
        }

        public Dictionary<string, List<string>> GetPrefixes()
        {
            if (_prefixes == null)
            {
                _prefixes = JsonLoader.LoadDictionaryFromFile<List<string>>(Assembly.GetExecutingAssembly(), _prefixPath, Helpers.JsonHelpers.SerializerOptions);
            }

            return _prefixes;
        }
    }
}
