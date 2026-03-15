using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonNounDataProvider : INounDataProvider
    {
        private readonly string _patternPath = "Data.Nouns.patterns";
        private readonly string _irregularPath = "Data.Nouns.irregulars";
        private readonly string _properNamesPath = "Data.Nouns.propers";
        private readonly Lazy<Dictionary<string, NounPattern>> _patterns;
        private readonly Lazy<Dictionary<string, NounPattern>> _irregulars;
        private readonly Lazy<Dictionary<string, NounPattern>> _properNames;

        public JsonNounDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _properNames = new Lazy<Dictionary<string, NounPattern>>(() => JsonLoader.LoadDictionaryFromFile<NounPattern>(assembly, _properNamesPath, JsonHelpers.SerializerOptions)!);
            _irregulars = new Lazy<Dictionary<string, NounPattern>>(() => JsonLoader.LoadDictionaryFromFile<NounPattern>(assembly, _irregularPath, JsonHelpers.SerializerOptions)!);
            _patterns = new Lazy<Dictionary<string, NounPattern>>(() =>
            {
                var patterns = JsonLoader.LoadDictionaryFromFile<NounPattern>(assembly, _patternPath, JsonHelpers.SerializerOptions);

                foreach (var kvp in patterns.Where(pattern => !string.IsNullOrEmpty(pattern.Value.InheritsFrom)))
                {
                    var child = kvp.Value;
                    var basePattern = patterns[child.InheritsFrom!];

                    var mergedEngings = new Dictionary<string, IReadOnlyDictionary<string, string>>();

                    foreach (var number in new[] { "singular", "plural" })
                    {
                        var merged = new Dictionary<string, string>(basePattern.Endings[number]);

                        if (child.Endings.TryGetValue(number, out var childCase))
                        {
                            foreach (var pair in childCase)
                            {
                                merged[pair.Key] = pair.Value;
                            }
                        }

                        mergedEngings[number] = merged;
                    }

                    patterns[kvp.Key] = child with { Endings = mergedEngings };
                }

                return patterns;
            });
        }

        public Dictionary<string, NounPattern> GetPatterns() => _patterns.Value;

        public Dictionary<string, NounPattern> GetIrregulars() => _irregulars.Value;

        public Dictionary<string, NounPattern> GetPropers() => _properNames.Value;
    }
}
