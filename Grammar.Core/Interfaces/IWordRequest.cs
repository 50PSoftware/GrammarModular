using Grammar.Core.Enums;
using System.Text.Json.Serialization;

namespace Grammar.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for Word Request.
    /// </summary>
    public interface IWordRequest
    {
        /// <summary>
        /// Gets or sets the dictionary form of the word.
        /// </summary>
        public string Lemma { get; }
        /// <summary>
        /// Gets or sets the requested grammatical gender.
        /// </summary>
        public Gender? Gender { get; }
        /// <summary>
        /// Gets or sets the requested grammatical number.
        /// </summary>
        public Number? Number { get; }
        /// <summary>
        /// Gets or sets the requested grammatical case.
        /// </summary>
        public Case? Case { get; }
        /// <summary>
        /// Gets or sets the requested grammatical person.
        /// </summary>
        public Person? Person { get; }
        /// <summary>
        /// Gets or sets the requested grammatical tense.
        /// </summary>
        public Tense? Tense { get; }
        /// <summary>
        /// Gets or sets the requested or resolved verb aspect.
        /// </summary>
        public VerbAspect? Aspect { get; }
        /// <summary>
        /// Gets or sets the requested grammatical mood.
        /// </summary>
        public Modus? Modus { get; }
        /// <summary>
        /// Gets or sets the requested grammatical voice.
        /// </summary>
        public Voice? Voice { get; }

        /// <summary>
        /// Gets or sets the lexical category of the requested word.
        /// </summary>
        /// <remarks>
        /// Nullable like every other category here: an enum's default is one of its members, so a
        /// non-nullable one cannot say that nobody chose. Here that default was <c>Noun</c>.
        /// </remarks>
        [JsonPropertyName("Category")]
        public WordCategory? WordCategory { get; }

        /// <summary>
        /// Gets or sets the inflection pattern key.
        /// </summary>
        public string? Pattern { get; }
        /// <summary>
        /// Gets or sets optional provider-specific request data.
        /// </summary>
        public string? AdditionalData { get; }
        /// <summary>
        /// Gets or sets a value indicating whether the requested form is negative.
        /// </summary>
        public bool IsNegative { get; }
        /// <summary>
        /// Gets or sets a value indicating whether the word is indeclinable.
        /// </summary>
        public bool? IsIndeclinable { get; }
        /// <summary>
        /// Gets or sets a value indicating whether the word occurs only in plural forms.
        /// </summary>
        public bool? IsPluralOnly { get; }
    }
}
