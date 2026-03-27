using Grammar.Core.Enums;

namespace Grammar.Core.Models.Derivation
{
    /// <summary>
    /// Represents a language-agnostic link from a root to one of its derived lexemes.
    /// </summary>
    /// <remarks>
    /// This record intentionally contains only fields that every language-specific
    /// subtype can provide. Czech-specific metadata (pattern, gender, animacy, mobile
    /// vowel, inflection suffix) lives in <c>CzechDerivationLink</c>.
    /// <para>
    /// The derivational suffix and semantic type (Feminative, Diminutive, …) are
    /// absent here because they are either computable from data or belong to
    /// <c>IDerivationService</c>, not to the data layer.
    /// </para>
    /// </remarks>
    public record DerivationLink
    {
        /// <summary>Gets the lemma of the derived word (e.g., "mladík").</summary>
        public string Lemma { get; init; } = string.Empty;

        /// <summary>Gets the part of speech of the derived word.</summary>
        public WordCategory PartOfSpeech { get; init; }
    }
}
