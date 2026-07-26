using Grammar.Core.Helpers;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.Reflection;

namespace Grammar.Czech.Providers.JsonProviders
{
    /// <summary>
    /// Loads numeral data from embedded JSON resources.
    /// </summary>
    public class JsonNumeralDataProvider : INumeralDataProvider
    {
        private readonly string _numeralPath = "Data.Rules.Numerals.patterns";
        private readonly string _paradigmsPath = "Data.Rules.Numerals.paradigms";
        private readonly Lazy<Dictionary<string, NumeralData>> _numerals;
        private readonly Lazy<Dictionary<string, NumeralParadigm>> _paradigms;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNumeralDataProvider"/> type.
        /// </summary>
        public JsonNumeralDataProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            _numerals = new Lazy<Dictionary<string, NumeralData>>(() => JsonLoader.LoadDictionaryFromFile<NumeralData>(assembly, _numeralPath, JsonHelpers.SerializerOptions)!);
            _paradigms = new Lazy<Dictionary<string, NumeralParadigm>>(() => JsonLoader.LoadDictionaryFromFile<NumeralParadigm>(assembly, _paradigmsPath, JsonHelpers.SerializerOptions)!);
        }

        /// <summary>
        /// Gets Czech numeral entries loaded from embedded JSON data.
        /// </summary>
        /// <returns>The loaded numeral metadata keyed by lemma.</returns>
        public Dictionary<string, NumeralData> GetNumerals() => _numerals.Value;

        /// <summary>
        /// Gets Czech numeral paradigms loaded from embedded JSON data.
        /// </summary>
        /// <returns>The loaded numeral paradigms keyed by paradigm identifier.</returns>
        public Dictionary<string, NumeralParadigm> GetParadigms() => _paradigms.Value;
    }
}
