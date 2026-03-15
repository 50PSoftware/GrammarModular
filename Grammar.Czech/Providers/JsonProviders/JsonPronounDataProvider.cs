using Grammar.Core.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonPronounDataProvider : IPronounDataProvider
    {
        private readonly string _pronounPath;
        private readonly string _paradigmsPath;
        private Dictionary<string, PronounData>? _data;

        public JsonPronounDataProvider()
        {
            this._pronounPath = "Data.Pronouns.patterns";
            this._paradigmsPath = "Data.Pronouns.paradigms";
        }

        public Dictionary<string, PronounData> GetPronouns()
        {
            if (_data == null)
            {
                _data = JsonLoader.LoadDictionaryFromFile<PronounData>(Assembly.GetExecutingAssembly(), _pronounPath, Helpers.JsonHelpers.SerializerOptions)!;
            }

            return _data;
        }
    }
}
