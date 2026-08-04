using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents czech lexical entry.
    /// </summary>
    public sealed record CzechLexicalEntry : LexicalEntry
    {
        /// <summary>
        /// Gets or sets a value indicating whether the noun is animate.
        /// </summary>
        public bool? IsAnimate { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether mobile vowel alternation applies.
        /// </summary>
        /// <remarks>
        /// Named to match the has_mobile_e column and <see cref="CzechWordRequest.HasMobileE"/>.
        /// It was HasMobileVowel, which the camelCase naming policy turned into a key the data never had,
        /// so the lexicon's mobile-vowel flag deserialized to null for every entry.
        /// </remarks>
        public bool? HasMobileE { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether genitive plural shortening applies.
        /// </summary>
        public bool? HasGenitivePluralShortening { get; init; }

        /// <summary>
        /// Gets or sets the requested or resolved verb aspect.
        /// </summary>
        public VerbAspect? Aspect { get; init; }

        /// <summary>
        /// Gets or sets aspect Counterpart.
        /// </summary>
        public string? AspectCounterpart { get; init; }

        /// <summary>
        /// Gets or sets the reflexive type, encoding which particle (se/si) the verb requires.
        /// </summary>
        public ReflexiveType ReflexiveType { get; init; }

        // Lexical facts that used to reach the engine only through CzechWordRequest, which made every
        // caller know that banka takes an epenthetic e in the genitive plural.

        /// <summary>
        /// Gets a value indicating whether genitive plural epenthesis applies, as in banka to bank.
        /// </summary>
        public bool? HasEpenthesisInGenitivePlural { get; init; }

        /// <summary>
        /// Gets a value indicating whether the word is indeclinable.
        /// </summary>
        public bool? IsIndeclinable { get; init; }

        /// <summary>
        /// Gets a value indicating whether the word occurs only in plural forms, as in kamna or lyže.
        /// </summary>
        public bool? IsPluralOnly { get; init; }

        /// <summary>
        /// Gets a value indicating whether the noun denotes something countable.
        /// </summary>
        /// <remarks>
        /// Only the vague quantifiers care: mnoho takes the genitive plural of a countable noun
        /// (mnoho studentů) and the genitive singular of an uncountable one (mnoho práce).
        /// </remarks>
        public bool? IsCountable { get; init; }

        /// <summary>
        /// Gets a value indicating whether the shorter of two competing forms is preferred.
        /// </summary>
        public bool? PrefersShortForm { get; init; }

        /// <summary>
        /// Gets the Czech verb class, which decides the conjugation pattern.
        /// </summary>
        /// <remarks>
        /// <see cref="Services.CzechVerbConjugationService"/> guesses this from the infinitive suffix, and
        /// the suffix is not enough on its own — mazat and dělat both end in -at yet belong to different
        /// classes. A stored class overrules the guess.
        /// </remarks>
        public VerbClass? VerbClass { get; init; }

        /// <summary>
        /// Gets the verb a deverbal noun is derived from, or <see langword="null"/> for a primary noun.
        /// </summary>
        /// <remarks>
        /// příjezd inherits the frame of přijet rather than restating it; the nominal surface rules —
        /// nominative actor to genitive, accusative patient to genitive — apply on top.
        /// </remarks>
        public string? BaseVerbLemma { get; init; }
    }
}
