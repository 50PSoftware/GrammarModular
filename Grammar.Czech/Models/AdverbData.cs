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
        /// Gets the adjective this adverb is derived from, or null when it is not deadjectival.
        /// </summary>
        /// <remarks>
        /// Recorded rather than computed, for the same reason as the comparative: which of -o, -e/-ě and -y
        /// an adjective takes is not predictable, and one adjective can yield two adverbs at once
        /// (dlouhý → dlouho and dlouze). Stating the base here gives the mapping in both directions without
        /// a rule that would have to guess.
        /// </remarks>
        public string? DerivedFrom { get; init; }

        /// <summary>
        /// Gets a value indicating whether the adverb can introduce a relative clause.
        /// </summary>
        /// <remarks>
        /// The same words serve as interrogative and relative — kde, kdy, kam, jak — so the flag says the
        /// adverb is available in that role, not that it is always in it.
        /// </remarks>
        public bool IsRelative { get; init; }

        /// <summary>
        /// Gets the comparative form, or null when the adverb is not compared.
        /// </summary>
        /// <remarks>
        /// Most of the stored comparatives are ones the rule in <see cref="Services.CzechAdverbService"/>
        /// could derive, and they are kept on purpose rather than left over. They are the corpus the
        /// derivation is measured against: a test derives every one of them and asserts that the set the
        /// rule misses is exactly the known irregulars. Delete the regular entries and the rule still works,
        /// but nothing is left to show that it works — the evidence goes with them.
        /// <para>
        /// A null here is a claim that the adverb is not compared, which the rule does not override. It is
        /// not the same as being absent from the file, where the rule does apply.
        /// </para>
        /// </remarks>
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
