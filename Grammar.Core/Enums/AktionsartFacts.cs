namespace Grammar.Core.Enums
{
    /// <summary>
    /// States what follows from a verb's <see cref="Aktionsart"/>.
    /// </summary>
    /// <remarks>
    /// One fact so far, and it is the one the source states of the whole classification: the groups it
    /// lists first are perfective and the rest imperfective. That makes the column checkable — a
    /// semelfactive marked imperfective is a bad row, not an unusual verb.
    /// </remarks>
    public static class AktionsartFacts
    {
        // Written out rather than derived from the member order. The order does carry the answer — the
        // source's rule is "(a)–(r) are perfective, (s)–(y) imperfective" — but reading it off ordinals
        // would make inserting a member somewhere in the middle silently reclassify everything after it.
        private static readonly HashSet<Aktionsart> Perfective =
        [
            Aktionsart.Ingressive, Aktionsart.Evolutive, Aktionsart.Delimitative, Aktionsart.Resultative,
            Aktionsart.Terminative, Aktionsart.Perdurative, Aktionsart.Finitive, Aktionsart.Egressive,
            Aktionsart.Exhaustive, Aktionsart.Total, Aktionsart.Saturative, Aktionsart.Extensive,
            Aktionsart.Cumulative, Aktionsart.Intensive, Aktionsart.Excessive, Aktionsart.Distributive,
            Aktionsart.Attenuative, Aktionsart.Semelfactive, Aktionsart.Momentary,
        ];

        /// <summary>
        /// Gets the aspect a verb of this group must have.
        /// </summary>
        /// <param name="aktionsart">The group to ask about.</param>
        /// <returns>The aspect the group implies.</returns>
        /// <remarks>
        /// Every group implies one, which is why nothing here is nullable: the classification cuts
        /// across the whole verb lexicon and each half of it lies wholly on one side of the aspect
        /// divide. Nový encyklopedický slovník češtiny, heslo ZPŮSOB SLOVESNÉHO DĚJE, states it
        /// outright — <em>slovesa skupin (a)–(r) jsou dok., zatímco slovesa skupin (s)–(y) jsou nedok.</em>
        /// </remarks>
        public static VerbAspect RequiredAspect(Aktionsart aktionsart) =>
            Perfective.Contains(aktionsart) ? VerbAspect.Perfective : VerbAspect.Imperfective;
    }
}
