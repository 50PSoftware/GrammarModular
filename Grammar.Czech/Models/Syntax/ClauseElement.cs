using Grammar.Core.Enums;

namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents a single non-predicate constituent of a clause together with the information it carries.
    /// </summary>
    /// <remarks>
    /// A constituent is one unit for word order even when it spans several words: second position falls after
    /// the whole phrase, so "Můj bratr se myl" and never "Můj se bratr myl".
    /// </remarks>
    public sealed record ClauseElement
    {
        /// <summary>
        /// Gets the word request for the head of the constituent, which governs agreement.
        /// </summary>
        public CzechWordRequest Word { get; init; }

        /// <summary>
        /// Gets the modifiers that precede the head, in surface order.
        /// </summary>
        /// <remarks>
        /// Each modifier inherits gender, number, case and animacy from the head wherever it leaves them
        /// unset, so an agreeing attribute needs only its lemma and pattern. Anything set explicitly is left
        /// alone, which is what a non-agreeing attribute in another case needs.
        /// </remarks>
        public IReadOnlyList<CzechWordRequest> Modifiers { get; init; } = [];

        /// <summary>
        /// Gets the preposition that opens the constituent, or null when there is none.
        /// </summary>
        /// <remarks>
        /// The builder vocalizes it against the word that follows and marks the head and the modifiers as
        /// standing after a preposition, so the caller does not set <see cref="CzechWordRequest.IsAfterPreposition"/>
        /// itself. A prepositional phrase is one constituent and never gives up a pronoun to the clitic cluster.
        /// </remarks>
        public string? Preposition { get; init; }

        /// <summary>
        /// Gets the FGD functor the constituent fills.
        /// </summary>
        public FgdFunctor Functor { get; init; }

        /// <summary>
        /// Gets the communicative status that decides where the constituent lands relative to the verb.
        /// </summary>
        public InformationStatus Status { get; init; } = InformationStatus.New;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClauseElement"/> type.
        /// </summary>
        /// <param name="word">The word request for the head of the constituent.</param>
        /// <param name="functor">The FGD functor the constituent fills.</param>
        /// <param name="status">The communicative status of the constituent.</param>
        /// <returns>The clause element.</returns>
        public static ClauseElement Of(CzechWordRequest word, FgdFunctor functor, InformationStatus status = InformationStatus.New)
            => new() { Word = word, Functor = functor, Status = status };

        /// <summary>
        /// Initializes a new instance of the <see cref="ClauseElement"/> type with modifiers.
        /// </summary>
        /// <param name="word">The word request for the head of the constituent.</param>
        /// <param name="modifiers">The modifiers preceding the head, in surface order.</param>
        /// <param name="functor">The FGD functor the constituent fills.</param>
        /// <param name="status">The communicative status of the constituent.</param>
        /// <returns>The clause element.</returns>
        public static ClauseElement Of(CzechWordRequest word, IReadOnlyList<CzechWordRequest> modifiers, FgdFunctor functor, InformationStatus status = InformationStatus.New)
            => new() { Word = word, Modifiers = modifiers, Functor = functor, Status = status };

        /// <summary>
        /// Initializes a new instance of the <see cref="ClauseElement"/> type for a prepositional phrase.
        /// </summary>
        /// <param name="preposition">The preposition that opens the constituent.</param>
        /// <param name="word">The word request for the head of the constituent.</param>
        /// <param name="functor">The FGD functor the constituent fills.</param>
        /// <param name="status">The communicative status of the constituent.</param>
        /// <returns>The clause element.</returns>
        public static ClauseElement Of(string preposition, CzechWordRequest word, FgdFunctor functor, InformationStatus status = InformationStatus.New)
            => new() { Preposition = preposition, Word = word, Functor = functor, Status = status };
    }
}
