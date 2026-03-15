using Grammar.Core.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonVerbDataProvider : IVerbDataProvider
    {
        private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private readonly string _patternPath;
        private readonly string _irregularPath;
        private Dictionary<string, VerbPattern> _patterns;
        private Dictionary<string, VerbPattern> _irregulars;

        public JsonVerbDataProvider()
        {
            this._patternPath = "Data.Verbs.patterns";
            this._irregularPath = "Data.Verbs.irregulars";
        }

        public Dictionary<string, VerbPattern> GetIrregulars()
        {
            if (_irregulars == null)
            {
                _irregulars = JsonLoader.LoadDictionaryFromFile<VerbPattern>(_assembly, _irregularPath, Helpers.JsonHelpers.SerializerOptions);
            }

            return _irregulars;
        }

        public Dictionary<string, VerbPattern> GetPatterns()
        {
            if (_patterns == null)
            {
                _patterns = JsonLoader.LoadDictionaryFromFile<VerbPattern>(_assembly, _patternPath, Helpers.JsonHelpers.SerializerOptions);
            }

            return _patterns;
        }
    }
}
