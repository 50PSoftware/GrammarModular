using Grammar.Core.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonPrepositionsDataProvider : IPrepositionDataProvider
    {
        private readonly string _prepositionsPath;
        private Dictionary<string, PrepositionData> _data;

        public JsonPrepositionsDataProvider()
        {
            this._prepositionsPath = "Data.prepositions";
        }

        public Dictionary<string, PrepositionData> GetPrepositions()
        {
            if (_data == null)
            {
                _data = JsonLoader.LoadDictionaryFromFile<PrepositionData>(Assembly.GetExecutingAssembly(), _prepositionsPath, Helpers.JsonHelpers.SerializerOptions)!;
            }

            return _data;
        }
    }
}
