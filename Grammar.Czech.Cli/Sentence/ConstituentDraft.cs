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
        /// Gets or sets the communicative status that decides where the constituent lands.
        /// </summary>
        public InformationStatus Status { get; set; } = InformationStatus.New;

        /// <summary>
        /// Gets or sets the preposition the user attached, or <see langword="null"/> when there is none.
        /// </summary>
        /// <remarks>
        /// Only what the user wrote. A preposition the valency frame supplies is left to the builder,
        /// which is also where it gets vocalized.
        /// </remarks>
        public string? Preposition { get; set; }

        /// <summary>
        /// Gets the agreeing attributes preceding the head, in surface order.
        /// </summary>
        public List<CzechWordRequest> Modifiers { get; } = [];

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
