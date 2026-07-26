using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents Czech preposition metadata loaded from JSON data.
    /// </summary>
    public sealed record PrepositionData
    {
        /// <summary>
        /// Gets the preposition lemma or surface form.
        /// </summary>
        public string Preposition { get; init; } = "";
        /// <summary>
        /// Gets the origin category of the preposition.
        /// </summary>
        public PrepositionOriginType OriginType { get; init; }
        /// <summary>
        /// Gets the vocalized variant used before an awkward consonant cluster, or null when the preposition has none.
        /// </summary>
        /// <remarks>
        /// Only the syllabic prepositions have one: v/ve, s/se, z/ze, k/ke, od/ode, bez/beze.
        /// Whether it applies depends on the following word and is decided by the preposition service.
        /// </remarks>
        public string? Vocalized { get; init; }
        /// <summary>
        /// Gets the case and semantic variants supported by the preposition.
        /// </summary>
        public List<PrepositionVariant> Variants { get; init; } = new();
    }

    /// <summary>
    /// Represents one surface variant of a Czech preposition.
    /// </summary>
    public sealed record PrepositionVariant
    {
        /// <summary>
        /// Gets or sets the requested grammatical case.
        /// </summary>
        public Case Case { get; init; }
        /// <summary>
        /// Gets the semantic group represented by the preposition variant.
        /// </summary>
        public PrepositionSemanticGroup SemanticGroup { get; init; }
    }
}
