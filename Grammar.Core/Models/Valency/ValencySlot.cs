using Grammar.Core.Enums;

namespace Grammar.Core.Models.Valency
{
    /// <summary>
    /// Represents valency slot.
    /// </summary>
    public sealed record ValencySlot
    {
        /// <summary>
        /// Gets or sets the FGD functor represented by the slot.
        /// </summary>
        /// <remarks>
        /// The same vocabulary the clause model uses, so a constituent's functor and a frame's slot match
        /// directly. It replaces the earlier SemanticRole, which named the same things differently and could
        /// not be lined up with a <see cref="Grammar.Core.Enums.FgdFunctor"/> without a translation step.
        /// </remarks>
        public FgdFunctor Functor { get; init; }

        /// <summary>
        /// Gets the syntactic realization required by the valency slot.
        /// </summary>
        public SyntacticRealization Realization { get; init; } = new();

        /// <summary>
        /// Gets a value indicating whether the valency slot must be expressed.
        /// </summary>
        public bool IsObligatory { get; init; }
    }
}
