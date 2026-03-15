using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    public class JsonPronounDataProvider : IPronounDataProvider
    {
        private readonly string _pronounPath = "Data.Pronouns.patterns";
        private readonly string _paradigmsPath = "Data.Pronouns.paradigms";
        private readonly Lazy<Dictionary<string, PronounData>> _pronouns;
        private readonly Lazy<Dictionary<string, PronounParadigm>> _paradigms;

        public JsonPronounDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _pronouns = new Lazy<Dictionary<string, PronounData>>(() => JsonLoader.LoadDictionaryFromFile<PronounData>(assembly, _pronounPath, JsonHelpers.SerializerOptions)!);
            _paradigms = new Lazy<Dictionary<string, PronounParadigm>>(() => JsonLoader.LoadDictionaryFromFile<PronounParadigm>(assembly, _paradigmsPath, JsonHelpers.SerializerOptions)!);
        }

        public Dictionary<string, PronounParadigm> GetParadigms() => _paradigms.Value;

        public Dictionary<string, PronounData> GetPronouns() => _pronouns.Value;
    }
}
