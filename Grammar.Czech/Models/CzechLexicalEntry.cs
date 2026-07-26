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
        /// Named to match the hasMobileE key in lexicon.json and <see cref="CzechWordRequest.HasMobileE"/>.
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
    }
}
