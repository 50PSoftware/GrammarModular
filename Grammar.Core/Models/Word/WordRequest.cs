using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using System.Text.Json.Serialization;

namespace Grammar.Core.Models.Word
{
    /// <summary>
    /// Represents a language-neutral request for an inflected word form.
    /// </summary>
    public struct WordRequest : IWordRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WordRequest"/> type.
        /// </summary>
        public WordRequest()
        { }

        /// <summary>
        /// Gets or sets the dictionary form of the word.
        /// </summary>
        [JsonPropertyName("Lemma")]
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
        [JsonPropertyName("Category")]
        public WordCategory? WordCategory { get; set; }

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
        /// Gets or sets a value indicating whether the word is indeclinable.
        /// </summary>
        public bool? IsIndeclinable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the word occurs only in plural forms.
        /// </summary>
        public bool? IsPluralOnly { get; set; }
    }
}
