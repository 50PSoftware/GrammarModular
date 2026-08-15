using Grammar.Core.Enums;

namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents a relative clause hanging off a participant, stated as a plan rather than as a
    /// finished clause.
    /// </summary>
    /// <remarks>
    /// The plan-level counterpart of <see cref="RelativeAttachment"/>, which carries a
    /// <see cref="SentenceNode"/> because by then everything has been decided. Here the clause is a
    /// <see cref="SentencePlan"/>, so what happens inside a relative clause is what happens anywhere
    /// else: roles are worked out from the verb's frame, the sense of the verb is chosen, the subject
    /// may be dropped, and it may itself be a complex sentence.
    /// </remarks>
    public sealed record PlannedRelative
    {
        /// <summary>
        /// Gets the lemma of the word introducing the relative clause.
        /// </summary>
        /// <remarks>
        /// A relative pronoun (který, jenž) or a relative adverb (kde, kdy, kam, jak).
        /// </remarks>
        public string Relativizer { get; init; } = "který";

        /// <summary>
        /// Gets the case the relativizer takes from its role inside the relative clause.
        /// </summary>
        /// <remarks>
        /// Stated rather than derived, because it is the one thing about the pronoun that does not come
        /// from the antecedent: it looks two ways at once. Ignored for a relative adverb.
        /// </remarks>
        public Case Case { get; init; } = Case.Nominative;

        /// <summary>
        /// Gets the functor of the participant the relativizer possesses, or <see langword="null"/> when
        /// the relativizer stands for a participant itself.
        /// </summary>
        /// <remarks>
        /// The plan-level counterpart of <see cref="RelativeAttachment.Possessed"/>. Naming the participant
        /// by functor rather than by position keeps it addressable the way everything else in a plan is,
        /// and the participant it names keeps its own role and its own case: the pronoun is its attribute,
        /// not its replacement. <see cref="Case"/> is therefore unused when this is set, and
        /// <see cref="Services.CzechRoleResolver"/> reserves nothing.
        /// </remarks>
        public FgdFunctor? Possessed { get; init; }

        /// <summary>
        /// Gets the plan of the relative clause.
        /// </summary>
        public SentencePlan Clause { get; init; } = new();
    }
}
