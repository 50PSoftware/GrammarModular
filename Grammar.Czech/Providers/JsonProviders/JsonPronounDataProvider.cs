using Grammar.Core.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonPronounDataProvider : IPronounDataProvider
    {
        private readonly string _pronounPath;
        private readonly string _paradigmsPath;
        private Dictionary<string, PronounData>? _data;

        public JsonPronounDataProvider(string dataPath)
        {
            this._pronounPath = Path.Combine(dataPath, "pronouns.json");
            this._paradigmsPath = Path.Combine(dataPath, "pronouns_paradigms.json");
        }

        public Dictionary<string, PronounData> GetPronouns()
        {
            if (_data == null)
            {
                _data = JsonLoader.LoadDictionaryFromFile<PronounData>(_pronounPath, Helpers.JsonHelpers.SerializerOptions)!;
            }

            return _data;
        }
    }
}
