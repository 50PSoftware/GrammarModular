using Grammar.Core.Enums;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for resolving Czech conjunction behavior.
    /// </summary>
    public interface ICzechConjunctionService
    {
        /// <summary>
        /// Gets how the supplied conjunction joins clauses.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns>The conjunction type.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        ConjunctionType GetType(string conjunction);

        /// <summary>
        /// Determines whether a comma is written before the supplied conjunction.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> when a comma precedes the conjunction; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        bool RequiresComma(string conjunction);

        /// <summary>
        /// Determines whether the conjunction occupies the first position of the clause it introduces,
        /// which is what the clitic cluster attaches after.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> for a subordinating conjunction; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        bool OccupiesFirstPosition(string conjunction);
    }
}
