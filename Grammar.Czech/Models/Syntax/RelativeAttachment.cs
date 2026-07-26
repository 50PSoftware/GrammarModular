using Grammar.Core.Enums;

namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents a relative clause attached to the constituent it modifies.
    /// </summary>
    /// <remarks>
    /// The relative pronoun looks two ways at once: it agrees with the antecedent in gender, number and
    /// animacy, while its case comes from the role it plays inside the relative clause. That is why
    /// <see cref="Case"/> is stated here and everything else is read off the antecedent —
    /// "muž, kterého jsem viděl" is masculine animate singular from muž, accusative from being the object
    /// of viděl.
    /// </remarks>
    public sealed record RelativeAttachment
    {
        /// <summary>
        /// Gets the lemma of the relative pronoun.
        /// </summary>
        public string Pronoun { get; init; } = "který";

        /// <summary>
        /// Gets the case the pronoun takes from its role inside the relative clause.
        /// </summary>
        public Case Case { get; init; } = Case.Nominative;

        /// <summary>
        /// Gets the relative clause itself.
        /// </summary>
        /// <remarks>
        /// A single clause rather than a <see cref="SentenceNode"/>: when the pronoun is the subject, the
        /// predicate has to be made to agree with the antecedent through it, and that has a clear meaning
        /// only for one clause. A relative clause that coordinates internally is not supported yet.
        /// </remarks>
        public CzechClause Clause { get; init; }
    }
}
