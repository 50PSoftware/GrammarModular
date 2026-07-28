using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Provides access to clitic data.
    /// </summary>
    public interface ICliticDataProvider
    {
        /// <summary>
        /// Gets Czech clitic data loaded from embedded JSON data.
        /// </summary>
        /// <returns>The loaded Czech clitic definitions.</returns>
        CliticsData GetClitics();
    }
}
