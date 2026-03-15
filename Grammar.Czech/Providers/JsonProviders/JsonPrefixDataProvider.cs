using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonPrefixDataProvider : IPrefixDataProvider
    {
        private readonly string _prefixPath = "Data.prefixes";
        private readonly Lazy<Dictionary<string, List<string>>> _prefixes;

        public JsonPrefixDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _prefixes = new Lazy<Dictionary<string, List<string>>>(() => JsonLoader.LoadDictionaryFromFile<List<string>>(assembly, _prefixPath, JsonHelpers.SerializerOptions)!);
        }

        public Dictionary<string, List<string>> GetPrefixes() => _prefixes.Value;
    }
}
