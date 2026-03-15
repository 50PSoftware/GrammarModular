using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonAdjectiveDataProvider : IAdjectiveDataProvider
    {
        private readonly string _patternPath = "Data.Adjectives.patterns";
        private readonly Lazy<Dictionary<string, AdjectivePattern>> _patterns;

        public JsonAdjectiveDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _patterns = new Lazy<Dictionary<string, AdjectivePattern>>(() => JsonLoader.LoadDictionaryFromFile<AdjectivePattern>(assembly, _patternPath, JsonHelpers.SerializerOptions)!);
        }

        public Dictionary<string, AdjectivePattern> GetPatterns() => _patterns.Value;
    }
}
