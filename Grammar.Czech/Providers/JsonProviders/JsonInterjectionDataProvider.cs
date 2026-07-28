using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    /// <summary>
    /// Loads interjection data from embedded JSON resources.
    /// </summary>
    public sealed class JsonInterjectionDataProvider : IInterjectionDataProvider
    {
        private const string InterjectionsPath = "Data.Rules.interjections";

        private readonly Lazy<Dictionary<string, InterjectionData>> _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonInterjectionDataProvider"/> type.
        /// </summary>
        public JsonInterjectionDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();

            _data = new Lazy<Dictionary<string, InterjectionData>>(
                () => JsonLoader.LoadDictionaryFromFile<InterjectionData>(assembly, InterjectionsPath, JsonHelpers.SerializerOptions),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Gets the Czech interjections loaded from embedded JSON data, keyed by lemma.
        /// </summary>
        /// <returns>The loaded interjection definitions.</returns>
        public Dictionary<string, InterjectionData> GetInterjections() => _data.Value;
    }
}
