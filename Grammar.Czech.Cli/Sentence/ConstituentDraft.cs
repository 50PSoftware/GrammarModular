using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Holds one non-predicate constituent as the tool understands it, before the clause is built.
    /// </summary>
    /// <remarks>
    /// Mutable, unlike <see cref="ClauseElement"/>, because the review loop edits it in place and
    /// re-renders after every change. It converts to a clause element only when the sentence is built.
    /// </remarks>
    public sealed class ConstituentDraft
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstituentDraft"/> type.
        /// </summary>
        /// <param name="position">The one-based position the lemma was entered in.</param>
        /// <param name="lemma">The lemma as the user wrote it.</param>
        /// <param name="word">The word request resolved so far.</param>
        /// <param name="origin">Where the metadata came from.</param>
        public ConstituentDraft(int position, string lemma, CzechWordRequest word, MetadataOrigin origin)
        {
            Position = position;
            Lemma = lemma;
            Word = word;
            Origin = origin;
        }

        /// <summary>
        /// Gets the one-based position the lemma was entered in, which is how the user addresses it.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Gets the lemma as the user wrote it.
        /// </summary>
        public string Lemma { get; }

        /// <summary>
        /// Gets or sets the word request for the head of the constituent.
        /// </summary>
        public CzechWordRequest Word { get; set; }

        /// <summary>
        /// Gets or sets the FGD functor, or <see langword="null"/> while it is still undecided.
        /// </summary>
        public FgdFunctor? Functor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the role was invented from word order rather than
        /// read off a valency frame.
        /// </summary>
        /// <remarks>
        /// The difference between the dictionary saying what this argument is and the tool proposing it,
        /// which is the same distinction <see cref="Origin"/> draws for the morphology. A guessed role
        /// carries a guessed case with it, so it is the one line of the review worth reading twice.
        /// </remarks>
        public bool FunctorIsGuessed { get; set; }

        /// <summary>
        /// Gets or sets the communicative status that decides where the constituent lands.
        /// </summary>
        public InformationStatus Status { get; set; } = InformationStatus.New;

        /// <summary>
        /// Gets or sets a value indicating whether the user stated the status rather than the planner
        /// filling it.
        /// </summary>
        /// <remarks>
        /// The two have to be told apart on the way into the plan: an unstated status is the planner's
        /// to decide, and passing the tool's own default down would take that decision away from it.
        /// </remarks>
        public bool HasStatedStatus { get; set; }

        /// <summary>
        /// Gets or sets the preposition the user attached, or <see langword="null"/> when there is none.
        /// </summary>
        /// <remarks>
        /// Only what the user wrote. A preposition the valency frame supplies is left to the builder,
        /// which is also where it gets vocalized.
        /// </remarks>
        public string? Preposition { get; set; }

        /// <summary>
        /// Gets or sets the cases <see cref="Preposition"/> may govern, when there is one.
        /// </summary>
        /// <remarks>
        /// Carried so that an unanswered constituent can be reported as the question it actually is. A
        /// preposition with one rection settles the case and with it the role — <em>do</em> is always
        /// genitive — while <em>v</em> governs two and they mean different things, so what is open there
        /// is the case and not the role.
        /// </remarks>
        public IReadOnlyList<Case> PrepositionCases { get; set; } = [];

        /// <summary>
        /// Gets the agreeing attributes preceding the head, in surface order.
        /// </summary>
        public List<CzechWordRequest> Modifiers { get; } = [];

        /// <summary>
        /// Gets or sets the relative clause modifying this constituent, or <see langword="null"/> when
        /// there is none.
        /// </summary>
        /// <remarks>
        /// The counterpart of <see cref="PlannedParticipant.Relative"/>, and the reason a relative clause
        /// is not among <see cref="SentenceDraft.Clauses"/>: it hangs off a thing, not off the sentence.
        /// </remarks>
        public RelativeDraft? Relative { get; set; }

        /// <summary>
        /// Gets or sets where the head's metadata came from.
        /// </summary>
        public MetadataOrigin Origin { get; set; }

        /// <summary>
        /// Gets or sets the case the valency frame governs, kept for display only.
        /// </summary>
        /// <remarks>
        /// The builder fills the real one. This copy exists so the review table can show a case that the
        /// request itself does not carry — the whole point of a frame is that the caller does not state it.
        /// </remarks>
        public Case? FrameCase { get; set; }

        /// <summary>
        /// Gets or sets the preposition the valency frame governs, kept for display only.
        /// </summary>
        public string? FramePreposition { get; set; }

        /// <summary>
        /// Gets the case the constituent will actually stand in, whoever decided it.
        /// </summary>
        /// <remarks>
        /// Mirrors the builder: a case stated on the request wins, and it also stops the frame's
        /// preposition from being applied, so the two have to be read together.
        /// </remarks>
        public Case? EffectiveCase => Word.Case ?? FrameCase;

        /// <summary>
        /// Gets the preposition the constituent will actually open with.
        /// </summary>
        public string? EffectivePreposition => Preposition ?? (Word.Case is null ? FramePreposition : null);

        /// <summary>
        /// Converts the draft into the clause element the sentence builder consumes.
        /// </summary>
        /// <returns>The clause element.</returns>
        /// <exception cref="CliException">Thrown when the functor is still undecided.</exception>
        public ClauseElement ToElement()
        {
            if (Functor is not { } functor)
            {
                throw new CliException($"U slova '{Lemma}' není rozhodnutá role.");
            }

            return new ClauseElement
            {
                Word = Word,
                Modifiers = Modifiers,
                Preposition = Preposition,
                Functor = functor,
                Status = Status,
            };
        }
    }
}
