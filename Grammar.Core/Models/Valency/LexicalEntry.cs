using Grammar.Core.Enums;

namespace Grammar.Core.Models.Valency
{
    /// <summary>
    /// Represents lexical entry.
    /// </summary>
    public record LexicalEntry
    {
        /// <summary>
        /// Gets or sets the dictionary form of the word.
        /// </summary>
        public string Lemma { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets category.
        /// </summary>
        public WordCategory Category { get; init; }

        /// <summary>
        /// Gets or sets the requested grammatical gender.
        /// </summary>
        public Gender? Gender { get; init; }

        /// <summary>
        /// Gets or sets the inflection pattern key.
        /// </summary>
        public string? Pattern { get; init; }
    }
}
