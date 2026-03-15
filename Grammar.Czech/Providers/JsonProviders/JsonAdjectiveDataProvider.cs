using Grammar.Core.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonAdjectiveDataProvider : IAdjectiveDataProvider
    {
        private readonly string _patternPath;
        private Dictionary<string, AdjectivePattern> _patterns;

        public JsonAdjectiveDataProvider()
        {
            this._patternPath = "Data.Adjectives.patterns";
        }

        public Dictionary<string, AdjectivePattern> GetPatterns()
        {
            if (_patterns == null)
            {
                _patterns = JsonLoader.LoadDictionaryFromFile<AdjectivePattern>(Assembly.GetExecutingAssembly(), _patternPath, Helpers.JsonHelpers.SerializerOptions);
            }

            return _patterns;
        }
    }
}
