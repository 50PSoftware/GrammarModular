namespace Grammar.Core.Models.Word
{
    /// <summary>
    /// Represents the internal morphological structure of a word form:
    /// an optional prefix, an inflectional root, and an optional derivational suffix.
    /// </summary>
    public sealed class WordStructure
    {
        /// <summary>Gets or sets the verbal prefix (e.g., "pře" in "přepsat").</summary>
        public string? Prefix { get; set; }

        /// <summary>
        /// Gets or sets the inflectional root used to build inflected forms by appending endings.
        /// Derived from the lemma via <c>ExtractXxxRoot</c> in <c>CzechWordStructureResolver</c>.
        /// </summary>
        public string Root { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the phonologically active derivational suffix that is part of the stem
        /// and affects epenthesis and softening rules during inflection
        /// (e.g., <c>"k"</c> in <c>studentka</c> triggers epenthesis in genitive plural).
        /// <c>null</c> for most words.
        /// </summary>
        public string? DerivationSuffix { get; set; }

        /// <inheritdoc/>
        public override string ToString() => $"{Prefix}{Root}{DerivationSuffix}";
    }
}
