using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Provides access to conjunction data.
    /// </summary>
    public interface IConjunctionDataProvider
    {
        /// <summary>
        /// Gets Czech conjunction metadata loaded from embedded JSON data.
        /// </summary>
        /// <returns>The loaded conjunction data keyed by conjunction form.</returns>
        Dictionary<string, ConjunctionData> GetConjunctions();
    }
}
