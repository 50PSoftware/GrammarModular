using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Provides Czech numeral metadata and paradigms.
    /// </summary>
    public interface INumeralDataProvider
    {
        /// <summary>
        /// Gets Czech numeral entries.
        /// </summary>
        /// <returns>The numeral metadata keyed by lemma.</returns>
        Dictionary<string, NumeralData> GetNumerals();

        /// <summary>
        /// Gets Czech numeral paradigms.
        /// </summary>
        /// <returns>The numeral paradigms keyed by paradigm identifier.</returns>
        Dictionary<string, NumeralParadigm> GetParadigms();
    }
}
