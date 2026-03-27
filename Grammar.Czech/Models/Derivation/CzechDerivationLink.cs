using Grammar.Core.Enums;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Models.Derivation
{
    /// <summary>
    /// Stores the morphological metadata for a single Czech lexeme that belongs to a root family.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The full derivational suffix is not stored — it is computed at runtime as
    /// <c>lemma[rootKey.Length..]</c> whenever needed.
    /// Only <see cref="DerivationSuffix"/> is stored, and only when it is phonologically
    /// active during inflection (i.e., it triggers epenthesis or softening in
    /// <c>CzechWordStructureResolver</c>).
    /// </para>
    /// <para>
    /// Derivation type classification (Feminative, Diminutive, …) and phonological
    /// alternation instructions belong to <c>IDerivationService</c>, not to this record.
    /// </para>
    /// </remarks>
    public sealed record CzechDerivationLink
    {
        /// <summary>Gets the part of speech of this lexeme.</summary>
        [JsonPropertyName("pos")]
        public WordCategory PartOfSpeech { get; init; }

        /// <summary>
        /// Gets the inflectional pattern key (e.g., "pán", "žena", "mladý").
        /// Must match a key in <c>patterns.json</c>.
        /// </summary>
        [JsonPropertyName("pattern")]
        public string? Pattern { get; init; }

        /// <summary>Gets the grammatical gender, or <c>null</c> for adjectives.</summary>
        [JsonPropertyName("gender")]
        public Gender? Gender { get; init; }

        /// <summary>Gets a value indicating whether the lexeme is animate, or <c>null</c> if not applicable.</summary>
        [JsonPropertyName("isAnimate")]
        public bool? IsAnimate { get; init; }

        /// <summary>Gets a value indicating whether the lexeme has a mobile vowel (e.g., "pes").</summary>
        [JsonPropertyName("hasMobileVowel")]
        public bool? HasMobileVowel { get; init; }

        /// <summary>
        /// Gets the phonologically active suffix used during inflection stem assembly,
        /// or <c>null</c> when no such suffix exists.
        /// </summary>
        /// <remarks>
        /// Only set when the suffix affects inflection rules.
        /// For example, <c>"k"</c> in <c>studentka</c> triggers epenthesis in
        /// genitive plural ("studentek"). Most lexemes leave this <c>null</c>.
        /// </remarks>
        [JsonPropertyName("derivationSuffix")]
        public string? DerivationSuffix { get; init; }
    }
}
