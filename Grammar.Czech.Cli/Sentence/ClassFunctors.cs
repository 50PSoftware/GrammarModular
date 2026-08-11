using Grammar.Core.Enums;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Says what a particle or an interjection contributes to a clause, from what its class already is.
    /// </summary>
    /// <remarks>
    /// Neither is a clause member — Czech grammar calls them <em>bez větněčlenské platnosti</em> — so no
    /// valency frame hands them a role, and without one nothing can be built out of them. Unlike an
    /// adverb, though, this needs no dictionary: the rule data already sorts particles into the nine
    /// groups of Nekula's classification, and the Prague Dependency Treebank sorts the same ground into
    /// functors. Lining the two up is a rule about two classifications, not a list of words, so it
    /// belongs in code — a word list would belong in the dictionary.
    /// <para>
    /// The dictionary still wins where it says anything, the same as for adverbs. This is what applies
    /// when it says nothing, which for particles is everywhere.
    /// </para>
    /// </remarks>
    public static class ClassFunctors
    {
        /// <summary>
        /// Finds the functor a particle of the supplied type carries.
        /// </summary>
        /// <param name="type">The particle type, as the rule data classifies it.</param>
        /// <returns>The functor.</returns>
        public static FgdFunctor Of(ParticleType type) => type switch
        {
            // Jistota mluvčího, že to platí — přesně to, co PDT značí jako modalitu.
            ParticleType.Modal => FgdFunctor.MOD,

            // Přací je taky o postoji mluvčího ke skutečnosti, jen jiným směrem: 'kéž' říká, že to
            // neplatí a mluvčí by chtěl, aby platilo.
            ParticleType.Optative => FgdFunctor.MOD,

            // Vytýkací částice je rematizátor, jak ho PDT definuje: ukazuje na to, o čem věta je.
            ParticleType.Focusing => FgdFunctor.RHEM,

            // Intenzifikační říká, do jaké míry — a na to funktor existoval už dřív, protože totéž
            // dělá 'velmi' i jako příslovce.
            ParticleType.Intensifying => FgdFunctor.EXT,

            // Postojové a modifikační obojí barví výpověď, aniž by měnily, co tvrdí.
            ParticleType.Emotional => FgdFunctor.ATT,
            ParticleType.Modifying => FgdFunctor.ATT,

            // Strukturní váže větu na to, co bylo řečeno před ní — 'nuže', 'dále', 'konečně'.
            ParticleType.Structuring => FgdFunctor.PREC,

            // Odpověďová a negační stojí mimo stavbu věty úplně: 'Ano.' je celá výpověď a nerozvíjí nic.
            _ => FgdFunctor.PARTL,
        };
    }
}
