using Grammar.Core.Enums;

namespace Grammar.Core.Models.Semantics
{
    /// <summary>
    /// Represents one componential feature of a sense.
    /// </summary>
    /// <remarks>
    /// Bound to a lexical unit — a sense — rather than to a headword, because a feature belongs to one
    /// meaning: kohoutek the tap and kohoutek the bird are different senses of the same lemma and have
    /// nothing in common to hold a shared feature.
    /// </remarks>
    public sealed record SemanticFeature
    {
        /// <summary>
        /// Gets the identifier of the lexical unit — the sense — this feature describes.
        /// </summary>
        public long LuId { get; init; }

        /// <summary>
        /// Gets the name of the feature (for example "animate", "furniture", "concrete").
        /// </summary>
        public string FeatureName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the value of the feature, in the form <see cref="ValueKind"/> says to read it in.
        /// </summary>
        public string FeatureValue { get; init; } = string.Empty;

        /// <summary>
        /// Gets how to read <see cref="FeatureValue"/>.
        /// </summary>
        public SemanticValueKind ValueKind { get; init; }
    }
}
