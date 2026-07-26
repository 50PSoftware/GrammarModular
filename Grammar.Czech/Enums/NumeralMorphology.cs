namespace Grammar.Czech.Enums
{
    /// <summary>
    /// Specifies how a Czech numeral inflects, which is what selects its lookup strategy.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="Grammar.Core.Enums.NumeralType"/> because the two axes are independent:
    /// paterý and dvojí are both sortal yet decline as different adjective patterns, while pět and několik
    /// share a paradigm without sharing a numeric value.
    /// </remarks>
    public enum NumeralMorphology
    {
        /// <summary>
        /// Declines pronominally, after the ten pattern: jeden.
        /// </summary>
        Pronominal,
        /// <summary>
        /// Declines as a dual relic — dvou and dvěma for every gender: dva, oba.
        /// </summary>
        DualRelic,
        /// <summary>
        /// Follows the kost pattern with its own deviations in the genitive and instrumental: tři, čtyři.
        /// </summary>
        ThreeFour,
        /// <summary>
        /// Has the two-form paradigm — bare in the direct cases, -i everywhere else: pět through devadesát,
        /// and the indefinites několik, kolik, tolik, mnoho.
        /// </summary>
        FiveNinetyNine,
        /// <summary>
        /// Declines as a masculine inanimate noun: tisíc (stroj), milion and bilion (hrad).
        /// </summary>
        NounMasculine,
        /// <summary>
        /// Declines as a neuter noun after město: sto, patero.
        /// </summary>
        NounNeuter,
        /// <summary>
        /// Declines as a feminine noun: miliarda and třetina (žena), trojice (růže).
        /// </summary>
        NounFeminine,
        /// <summary>
        /// Declines as a hard adjective after mladý: druhý, pátý, paterý, dvojnásobný.
        /// </summary>
        HardAdjective,
        /// <summary>
        /// Declines as a soft adjective after jarní: první, třetí, tisící, dvojí.
        /// </summary>
        SoftAdjective,
        /// <summary>
        /// Is an adverb and does not decline: dvakrát, trojmo, dvojnásob.
        /// </summary>
        Adverb,
        /// <summary>
        /// Does not decline at all: moc, hodně, dost, pár.
        /// </summary>
        Indeclinable
    }
}
