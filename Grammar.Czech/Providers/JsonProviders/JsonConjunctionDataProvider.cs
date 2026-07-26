using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    /// <summary>
    /// Loads conjunction data from embedded JSON resources.
    /// </summary>
    public class JsonConjunctionDataProvider : IConjunctionDataProvider
    {
        private const string ConjunctionsPath = "Data.Rules.conjunctions";

        private readonly Lazy<Dictionary<string, ConjunctionData>> _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonConjunctionDataProvider"/> type.
        /// </summary>
        public JsonConjunctionDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _data = new Lazy<Dictionary<string, ConjunctionData>>(
                () => JsonLoader.LoadDictionaryFromFile<ConjunctionData>(assembly, ConjunctionsPath, JsonHelpers.SerializerOptions)!,
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Gets Czech conjunction metadata loaded from embedded JSON data.
        /// </summary>
        /// <returns>The loaded conjunction data keyed by conjunction form.</returns>
        public Dictionary<string, ConjunctionData> GetConjunctions() => _data.Value;
    }
}
