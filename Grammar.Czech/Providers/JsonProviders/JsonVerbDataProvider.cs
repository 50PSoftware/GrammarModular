using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonVerbDataProvider : IVerbDataProvider
    {
        private readonly string _patternPath = "Data.Verbs.patterns";
        private readonly string _irregularPath = "Data.Verbs.irregulars";
        private readonly Lazy<Dictionary<string, VerbPattern>> _patterns;
        private readonly Lazy<Dictionary<string, VerbPattern>> _irregulars;

        public JsonVerbDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _irregulars = new Lazy<Dictionary<string, VerbPattern>>(() => JsonLoader.LoadDictionaryFromFile<VerbPattern>(assembly, _irregularPath, JsonHelpers.SerializerOptions)!);
            _patterns = new Lazy<Dictionary<string, VerbPattern>>(() => JsonLoader.LoadDictionaryFromFile<VerbPattern>(assembly, _patternPath, JsonHelpers.SerializerOptions)!);
        }

        public Dictionary<string, VerbPattern> GetIrregulars() => _irregulars.Value;

        public Dictionary<string, VerbPattern> GetPatterns() => _patterns.Value;
    }
}
