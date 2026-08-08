using Grammar.Core.Enums;
using Grammar.Czech.Models;

namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents one participant of a sentence being planned, before it is known what case it stands
    /// in or where in the sentence it goes.
    /// </summary>
    /// <remarks>
    /// The difference from <see cref="ClauseElement"/> is what is allowed to be missing. A clause
    /// element states a functor because a clause has been decided; a participant may leave it unset and
    /// have <see cref="Services.CzechRoleResolver"/> work it out from the verb's frame.
    /// </remarks>
    public sealed record PlannedParticipant
    {
        /// <summary>
        /// Gets the FGD functor the participant fills, or <see langword="null"/> to have it inferred.
        /// </summary>
        /// <remarks>
        /// <see cref="Services.CzechSentencePlanner"/> requires it: planning a sentence out of
        /// participants whose roles are unknown is not planning but guessing, and the guess is a stage
        /// of its own so that it can be inspected and overruled before anything is built.
        /// </remarks>
        public FgdFunctor? Functor { get; init; }

        /// <summary>
        /// Gets the word request for the head of the participant.
        /// </summary>
        public CzechWordRequest Word { get; init; }

        /// <summary>
        /// Gets the proposition filling this participant, or null when a word fills it.
        /// </summary>
        public CzechClause? Content { get; init; }

        /// <summary>
        /// Gets the agreeing attributes preceding the head, in surface order.
        /// </summary>
        public IReadOnlyList<CzechWordRequest> Modifiers { get; init; } = [];

        /// <summary>
        /// Gets the preposition the caller attached, or null to let the frame supply one.
        /// </summary>
        public string? Preposition { get; init; }

        /// <summary>
        /// Gets the communicative status, or <see langword="null"/> to take the unmarked one.
        /// </summary>
        /// <remarks>
        /// Nullable because "not said" and "old information" are different things, and only the first
        /// is the planner's to fill. Stating <see cref="InformationStatus.Given"/> on every participant
        /// is a claim about the discourse; leaving it unset is not.
        /// </remarks>
        public InformationStatus? Status { get; init; }

        /// <summary>
        /// Initializes a participant filled by a word.
        /// </summary>
        /// <param name="word">The word request for the head.</param>
        /// <param name="functor">The functor, or null to have it inferred.</param>
        /// <returns>The participant.</returns>
        public static PlannedParticipant Of(CzechWordRequest word, FgdFunctor? functor = null)
            => new() { Word = word, Functor = functor };

        /// <summary>
        /// Initializes a participant filled by a proposition.
        /// </summary>
        /// <param name="content">The proposition.</param>
        /// <param name="functor">The functor, or null to have it inferred.</param>
        /// <returns>The participant.</returns>
        public static PlannedParticipant Of(CzechClause content, FgdFunctor? functor = null)
            => new() { Content = content, Functor = functor };

        /// <summary>
        /// Converts the participant into the clause element the rest of the pipeline consumes.
        /// </summary>
        /// <returns>The clause element.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the functor is still unknown.</exception>
        public ClauseElement ToElement() => new()
        {
            Word = Word,
            Content = Content,
            Modifiers = Modifiers,
            Preposition = Preposition,
            Functor = Functor ?? throw new InvalidOperationException(
                $"Participant '{Describe()}' nemá funktor. Buď ho zadej, nebo nech doplnit "
                + $"{nameof(Services.CzechRoleResolver)}em."),
            Status = Status ?? InformationStatus.New,
        };

        private string Describe() =>
            Content is not null ? Content.Predicate.Lemma : Word.Lemma;
    }
}
