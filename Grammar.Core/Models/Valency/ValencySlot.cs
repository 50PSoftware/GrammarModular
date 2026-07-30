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
        /// Gets the surface forms the slot may take, ordered as stored.
        /// </summary>
        /// <remarks>
        /// Use <see cref="PreferredRealization"/> to generate. A slot with no realization at all is a data
        /// error rather than a slot that cannot surface.
        /// </remarks>
        public IReadOnlyList<SlotRealization> Realizations { get; init; } = [];

        /// <summary>
        /// Gets the position the slot takes in the canonical ordering of the frame, counting from one.
        /// </summary>
        /// <remarks>
        /// Czech word order is pragmatic, so this is the order to fall back to when information structure
        /// says nothing — not a rule about where the constituent has to stand.
        /// </remarks>
        public int CanonicalOrder { get; init; }

        /// <summary>
        /// Gets how strongly the frame requires the slot.
        /// </summary>
        public Obligatoriness Obligatoriness { get; init; } = Obligatoriness.Optional;

        /// <summary>
        /// Gets a value indicating whether the slot may stay unexpressed when the context supplies it.
        /// </summary>
        public bool CanDropContextual { get; init; }

        /// <summary>
        /// Gets a value indicating whether the slot may stay unexpressed on a generic reading.
        /// </summary>
        public bool CanDropGeneric { get; init; }

        /// <summary>
        /// Gets the functor this slot is coreferential with in a controlled infinitive, or
        /// <see langword="null"/> when the slot controls nothing.
        /// </summary>
        /// <remarks>
        /// Chce přijít has one actor holding both clauses together; the infinitive has no subject of its
        /// own to inflect for, so the controlling functor has to be recorded rather than inferred.
        /// </remarks>
        public FgdFunctor? ControlTarget { get; init; }

        /// <summary>
        /// Gets the surface form to use when generating, which is the lowest-ranked realization.
        /// </summary>
        public SlotRealization? PreferredRealization
            => Realizations.Count == 0
                ? null
                : Realizations.MinBy(realization => realization.Preference);

        /// <summary>
        /// Gets a value indicating whether the valency slot must be expressed.
        /// </summary>
        [Obsolete("Use Obligatoriness. A boolean cannot express the Typical grade, which is what tells a "
            + "slot that is merely unsaid apart from one that is not part of the event.")]
        public bool IsObligatory => Obligatoriness == Obligatoriness.Obligatory;
    }
}
