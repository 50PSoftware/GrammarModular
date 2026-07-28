using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Provides access to interjection data.
    /// </summary>
    public interface IInterjectionDataProvider
    {
        /// <summary>
        /// Gets the Czech interjections loaded from embedded JSON data, keyed by lemma.
        /// </summary>
        /// <returns>The loaded interjection definitions.</returns>
        Dictionary<string, InterjectionData> GetInterjections();
    }
}
