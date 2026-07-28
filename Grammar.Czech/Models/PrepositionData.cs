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
        /// Only some prepositions have one: v/ve, s/se, z/ze, k/ke, bez/beze, od/ode, nad/nade, pod/pode,
        /// před/přede, přes/přese. Whether it applies depends on the following word and is decided by the
        /// preposition service.
        /// <para>
        /// skrz/skrze is deliberately absent. The two alternate freely — skrz les and skrze les are both
        /// current — so the choice is stylistic rather than conditioned by the following word, and putting
        /// skrze here would make the cluster rules fire on it and produce "skrze silnici" for "skrz silnici".
        /// </para>
        /// </remarks>
        public string? Vocalized { get; init; }
        /// <summary>
        /// Gets the word beginnings that take the vocalized form regardless of what the cluster rules say.
        /// </summary>
        /// <remarks>
        /// Two kinds of entry live here, both out of reach of the cluster rules in the preposition service.
        /// <para>
        /// Lexicalized combinations: the numerals se dvěma, se třemi, se čtyřmi, ve dvou, ve třech, and
        /// beze všeho, ode dneška. Their clusters are only two consonants deep and share nothing with the
        /// preposition, so no rule about sibilants or three-consonant runs reaches them.
        /// </para>
        /// <para>
        /// Clusters opening with d after a one-consonant preposition: ve dne, ze dřeva, ke dnu, se dřevem.
        /// These look regular enough to be a rule and may well become one, but the boundary is not settled —
        /// ze dřeva vocalizes while z Drážďan does not, on the same dr/dř shape — so they are enumerated
        /// until the condition can be checked against the ÚJČ reference rather than guessed at.
        /// </para>
        /// </remarks>
        public List<string> VocalizeBefore { get; init; } = new();
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
