using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents Czech conjunction metadata loaded from JSON data.
    /// </summary>
    /// <remarks>
    /// Conjunctions are a closed class, so the data file is the whole inventory the builder accepts and an
    /// unknown conjunction is reported rather than guessed at. Two groups are deliberately absent:
    /// aby and kdyby, which fuse with the conditional auxiliary and inflect for person
    /// (abych, abys, abychom), and však, which is itself a second-position word rather than a clause opener.
    /// Neither can be handled by placing a fixed string in front of a clause.
    /// </remarks>
    public sealed record ConjunctionData
    {
        /// <summary>
        /// Gets how the conjunction joins clauses, which also decides whether it takes the first position
        /// of the clause it introduces.
        /// </summary>
        public ConjunctionType Type { get; init; }

        /// <summary>
        /// Gets a value indicating whether a comma is written before the conjunction.
        /// </summary>
        public bool RequiresComma { get; init; }
    }
}
