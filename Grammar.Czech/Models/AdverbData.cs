namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents Czech adverb metadata loaded from JSON data.
    /// </summary>
    /// <remarks>
    /// NESČ classifies adverbs as a <em>neohebný</em> word class, so there is no paradigm here: the positive
    /// degree is the lemma itself and comparison is the only morphology an adverb has.
    /// <para>
    /// The comparative is stored rather than derived. Deadjectival adverbs take three different suffixes —
    /// -o, -e/-ě and -y (nízko, krásně, česky) — the choice is not fully predictable, and the same adjective
    /// can yield two adverbs with different comparatives (dlouho and dlouze, vysoko and vysoce). Comparison
    /// itself is no better behaved: the frequent adverbs are irregular (dobře → lépe, špatně → hůře,
    /// brzy → dříve) and the regular -eji/-ěji suffix interacts with palatalization (hladce → hladčeji)
    /// while leaving other stems alone (hustě → hustěji). A rule would be wrong quietly and often.
    /// </para>
    /// </remarks>
    public sealed record AdverbData
    {
        /// <summary>
        /// Gets the comparative form, or null when the adverb is not compared.
        /// </summary>
        public string? Comparative { get; init; }

        /// <summary>
        /// Gets the clipped variant of <see cref="Comparative"/> where usage has one, or null.
        /// </summary>
        /// <remarks>
        /// The same word shortened, with the clipped form the more colloquial of the two: hůře and hůř,
        /// dříve and dřív, výše and výš, lépe and líp. This is not the place for a comparative built a
        /// different way — that goes in <see cref="ComparativeAlternatives"/>.
        /// </remarks>
        public string? ComparativeShort { get; init; }

        /// <summary>
        /// Gets comparatives formed differently from <see cref="Comparative"/> that usage accepts alongside it.
        /// </summary>
        /// <remarks>
        /// Some adverbs have two competing comparatives that are not variants of one word: snadno takes both
        /// snáze and snadněji, hluboko both hloub and hlouběji, široko both šíře and šířeji. Generation picks
        /// the primary; this records that the others are equally correct rather than losing them.
        /// </remarks>
        public IReadOnlyList<string> ComparativeAlternatives { get; init; } = [];
    }
}
