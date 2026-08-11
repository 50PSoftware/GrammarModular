using Grammar.Core.Enums;
using Grammar.Czech.Models;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Collects what the user stated about one word, whether on the command line or in the review.
    /// </summary>
    /// <remarks>
    /// Everything is nullable for the same reason it is on <c>CzechWordRequest</c>: unset has to be
    /// distinguishable from stated, because unset is what the lexicon and the guess are allowed to fill.
    /// </remarks>
    public sealed class WordOverride
    {
        /// <summary>
        /// Gets or sets the FGD functor the constituent fills.
        /// </summary>
        public FgdFunctor? Functor { get; set; }

        /// <summary>
        /// Gets or sets the communicative status of the constituent.
        /// </summary>
        public InformationStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the case, which overrules the one the valency frame would govern.
        /// </summary>
        public Case? Case { get; set; }

        /// <summary>
        /// Gets or sets the gender.
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// Gets or sets the grammatical number.
        /// </summary>
        public Number? Number { get; set; }

        /// <summary>
        /// Gets or sets the declension pattern.
        /// </summary>
        public string? Pattern { get; set; }

        /// <summary>
        /// Gets or sets animacy.
        /// </summary>
        public bool? IsAnimate { get; set; }

        /// <summary>
        /// Gets or sets the word class stated for the word.
        /// </summary>
        /// <remarks>
        /// The escape hatch for everything the tool works out from the closed-class data and from the
        /// ending. Those answer well enough to be worth having and not well enough to be the last word:
        /// forty-nine words are both an adverb and a particle, and no rule decides between them.
        /// </remarks>
        public WordCategory? WordCategory { get; set; }

        /// <summary>
        /// Gets or sets the degree of comparison stated for the word.
        /// </summary>
        /// <remarks>
        /// Adjectives and adverbs compare; the tool otherwise builds every one of them in the positive,
        /// because nothing in a list of lemmas says <em>rychleji</em> rather than <em>rychle</em>.
        /// </remarks>
        public Degree? Degree { get; set; }

        /// <summary>
        /// Gets or sets the preposition opening the constituent.
        /// </summary>
        public string? Preposition { get; set; }

        /// <summary>
        /// Gets the agreeing attributes to put in front of the head, in the order they were given.
        /// </summary>
        public List<string> Modifiers { get; } = [];

        /// <summary>
        /// Gets a value indicating whether anything at all was stated.
        /// </summary>
        public bool IsEmpty =>
            Functor is null && Status is null && Case is null && Gender is null && Number is null
            && Pattern is null && IsAnimate is null && Preposition is null && Modifiers.Count == 0
            && WordCategory is null && Degree is null;

        /// <summary>
        /// Gets a value indicating whether the morphological identity of the word was stated, as opposed
        /// to only its role in the clause.
        /// </summary>
        /// <remarks>
        /// The distinction drives what the review reports as the source of the metadata: naming the
        /// functor says nothing about where the vzor came from.
        /// </remarks>
        public bool StatesMorphology =>
            Gender is not null || Number is not null || Pattern is not null || IsAnimate is not null
            || WordCategory is not null || Degree is not null;
    }
}
