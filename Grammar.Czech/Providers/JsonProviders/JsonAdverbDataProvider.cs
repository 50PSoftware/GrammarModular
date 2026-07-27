using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    /// <summary>
    /// Loads adverb data from embedded JSON resources.
    /// </summary>
    public class JsonAdverbDataProvider : IAdverbDataProvider
    {
        private const string AdverbsPath = "Data.Rules.adverbs";

        private readonly Lazy<Dictionary<string, AdverbData>> _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonAdverbDataProvider"/> type.
        /// </summary>
        public JsonAdverbDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _data = new Lazy<Dictionary<string, AdverbData>>(
                () => JsonLoader.LoadDictionaryFromFile<AdverbData>(assembly, AdverbsPath, JsonHelpers.SerializerOptions)!,
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Gets Czech adverb metadata loaded from embedded JSON data.
        /// </summary>
        /// <returns>The loaded adverb data keyed by lemma.</returns>
        public Dictionary<string, AdverbData> GetAdverbs() => _data.Value;
    }
}
