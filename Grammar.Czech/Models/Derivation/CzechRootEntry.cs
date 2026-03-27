namespace Grammar.Czech.Models.Derivation
{
    /// <summary>
    /// Represents a Czech morphological root together with all lexemes that belong to its family.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The outer key in <c>roots.json</c> is the root string (e.g., <c>"mlad"</c>).
    /// The <see cref="Derivations"/> dictionary maps each derived lemma (e.g., <c>"mladík"</c>)
    /// to its morphological metadata.
    /// </para>
    /// <para>
    /// The full suffix of any lexeme is computable as <c>lemma[Root.Length..]</c>
    /// and is never stored redundantly.
    /// </para>
    /// </remarks>
    public sealed record CzechRootEntry
    {
        /// <summary>Gets the morphological root string (e.g., "mlad").</summary>
        public string Root { get; init; } = string.Empty;

        /// <summary>
        /// Gets all lexemes in this root family.
        /// Key = lemma (e.g., "mladík"), value = morphological metadata.
        /// </summary>
        public IReadOnlyDictionary<string, CzechDerivationLink> Derivations { get; init; }
            = new Dictionary<string, CzechDerivationLink>();
    }
}
