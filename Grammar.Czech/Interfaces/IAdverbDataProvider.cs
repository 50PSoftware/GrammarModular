using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Provides access to adverb data.
    /// </summary>
    public interface IAdverbDataProvider
    {
        /// <summary>
        /// Gets Czech adverb metadata loaded from embedded JSON data.
        /// </summary>
        /// <returns>The loaded adverb data keyed by lemma.</returns>
        Dictionary<string, AdverbData> GetAdverbs();
    }
}
