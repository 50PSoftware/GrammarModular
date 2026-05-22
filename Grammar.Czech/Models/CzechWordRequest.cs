using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Specifies comparison degrees for Czech adjective forms.
    /// </summary>
    public enum Degree
    {
        Positive,
        Comparative,
        Superlative
    }

    /// <summary>
    /// Specifies Czech verb classes used for pattern inference.
    /// </summary>
    public enum VerbClass
    { Class1, Class2, Class3, Class4, Class5 }

    /// <summary>
    /// Represents a Czech-specific request for an inflected word form.
    /// </summary>
    public struct CzechWordRequest : IWordRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CzechWordRequest"/> type.
        /// </summary>
        public CzechWordRequest()
        { }

        /// <summary>
        /// Gets or sets the adjective comparison degree.
        /// </summary>
        public Degree? Degree { get; set; }
        /// <summary>
        /// Gets or sets the reflexive type, which determines the reflexive particle (se/si) for the verb phrase.
        /// </summary>
        public ReflexiveType ReflexiveType { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the verb phrase includes an explicit subject.
        /// </summary>
        public bool? HasExplicitSubject { get; set; }
        /// <summary>
        /// Gets or sets the Czech verb class used for pattern inference.
        /// </summary>
        public VerbClass? VerbClass { get; set; }
        /// <summary>
        /// Gets or sets the dictionary form of the word.
        /// </summary>
        public string Lemma { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the requested grammatical gender.
        /// </summary>
        public Gender? Gender { get; set; }
        /// <summary>
        /// Gets or sets the requested grammatical number.
        /// </summary>
        public Number? Number { get; set; }
        /// <summary>
        /// Gets or sets the requested grammatical case.
        /// </summary>
        public Case? Case { get; set; }
        /// <summary>
        /// Gets or sets the requested grammatical person.
        /// </summary>
        public Person? Person { get; set; }
        /// <summary>
        /// Gets or sets the requested grammatical tense.
        /// </summary>
        public Tense? Tense { get; set; }
        /// <summary>
        /// Gets or sets the requested or resolved verb aspect.
        /// </summary>
        public VerbAspect? Aspect { get; set; }
        /// <summary>
        /// Gets or sets the requested grammatical mood.
        /// </summary>
        public Modus? Modus { get; set; }
        /// <summary>
        /// Gets or sets the requested grammatical voice.
        /// </summary>
        public Voice? Voice { get; set; }
        /// <summary>
        /// Gets or sets the lexical category of the requested word.
        /// </summary>
        public WordCategory WordCategory { get; set; }
        /// <summary>
        /// Gets or sets the inflection pattern key.
        /// </summary>
        public string? Pattern { get; set; }
        /// <summary>
        /// Gets or sets optional provider-specific request data.
        /// </summary>
        public string? AdditionalData { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the requested form is negative.
        /// </summary>
        public bool IsNegative { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether the noun is animate.
        /// </summary>
        public bool? IsAnimate { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the pronoun follows a preposition.
        /// </summary>
        public bool IsAfterPreposition { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the word is indeclinable.
        /// </summary>
        public bool? IsIndeclinable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the word occurs only in plural forms.
        /// </summary>
        public bool? IsPluralOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether mobile vowel alternation applies.
        /// </summary>
        public bool? HasMobileE { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether genitive plural shortening applies.
        /// </summary>
        public bool? HasGenitivePluralShortening { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether genitive plural epenthesis aplies (for foreign words - eg. bank)
        /// </summary>
        public bool? HasEpenthesisInGenitivePlural { get; set; }
    }
}
