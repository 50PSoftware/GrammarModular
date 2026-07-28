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
        /// What is left once the rules have had their turn: the numerals se dvěma, se čtyřmi, ve dvou,
        /// ve čtyřech, and the settled ode dveří, ode dneška, beze studu. Their clusters are two consonants
        /// deep, close on something other than r, ř or l, and share nothing with the preposition, so none of
        /// the cluster conditions reaches them.
        /// <para>
        /// The tř, dř, sl, zr and zl clusters used to be listed here and are not any more — the ÚJČ
        /// reference states them as a closed exception to the r/ř/l condition, so they are a rule and the
        /// service applies them. Forms of "všechen" left for the same reason: beze všeho and nade vše hold
        /// for every syllabic preposition rather than for the ones that happened to be enumerated.
        /// </para>
        /// </remarks>
        public List<string> VocalizeBefore { get; init; } = new();
        /// <summary>
        /// Gets the words before which the preposition keeps its bare form whatever the rules say.
        /// </summary>
        /// <remarks>
        /// The rules run one way; this is the other. "s sebou" keeps the bare s even though the following
        /// word opens on the preposition's own consonant, which would otherwise vocalize it without
        /// exception — brát s sebou, vzít s sebou, jídlo s sebou.
        /// <para>
        /// It is worth being honest about the limit: reflexive "se sebou" — spokojený sám se sebou — is a
        /// different construction with the same two words, and nothing in the string tells them apart. The
        /// prepositional reading is the one the ÚJČ reference singles out and the far more frequent one, so
        /// that is what the service produces.
        /// </para>
        /// </remarks>
        public List<string> DoNotVocalizeBefore { get; init; } = new();
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
