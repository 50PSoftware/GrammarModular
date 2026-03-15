using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonPrepositionsDataProvider : IPrepositionDataProvider
    {
        private readonly string _prepositionsPath = "Data.prepositions";
        private readonly Lazy<Dictionary<string, PrepositionData>> _data;

        public JsonPrepositionsDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _data = new Lazy<Dictionary<string, PrepositionData>>(() => JsonLoader.LoadDictionaryFromFile<PrepositionData>(assembly, _prepositionsPath, JsonHelpers.SerializerOptions)!);
        }

        public Dictionary<string, PrepositionData> GetPrepositions() => _data.Value;
    }
}
