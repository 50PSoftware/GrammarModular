using Grammar.Core.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonNounDataProvider : INounDataProvider
    {
        private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private readonly string _patternPath;
        private readonly string _irregularPath;
        private readonly string _properNamesPath;
        private Dictionary<string, NounPattern> _patterns;
        private Dictionary<string, NounPattern> _irregulars;
        private Dictionary<string, NounPattern> _properNames;

        public JsonNounDataProvider()
        {
            this._patternPath = "Data.Nouns.patterns";
            this._irregularPath = "Data.Nouns.irregulars";
            this._properNamesPath = "Data.Nouns.propers";
        }

        public Dictionary<string, NounPattern> GetPatterns()
        {
            if (_patterns == null)
            {
                var patterns = JsonLoader.LoadDictionaryFromFile<NounPattern>(_assembly, _patternPath, Helpers.JsonHelpers.SerializerOptions);

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

                _patterns = patterns;
            }

            return _patterns;
        }

        public Dictionary<string, NounPattern> GetIrregulars()
        {
            if (_irregulars == null)
            {
                _irregulars = JsonLoader.LoadDictionaryFromFile<NounPattern>(_assembly, _irregularPath, Helpers.JsonHelpers.SerializerOptions);
            }

            return _irregulars;
        }

        public Dictionary<string, NounPattern> GetPropers()
        {
            if (_properNames == null)
            {
                _properNames = JsonLoader.LoadDictionaryFromFile<NounPattern>(_assembly, _properNamesPath, Helpers.JsonHelpers.SerializerOptions);
            }

            return _properNames;
        }
    }
}
