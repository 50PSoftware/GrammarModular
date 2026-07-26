namespace Grammar.Czech.Enums
{
    /// <summary>
    /// Specifies what a numeral imposes on the noun it counts, and through it on the predicate.
    /// </summary>
    /// <remarks>
    /// This is the one place Czech runs agreement backwards. Every other attribute takes its categories from
    /// its head; a cardinal from five up instead forces the head into the genitive plural and the predicate
    /// into the neuter singular — pět žáků bylo, against tři žáci byli.
    /// <para>
    /// It is stored per lemma rather than derived from the numeric value, because několik and mnoho have no
    /// value at all and still behave like five. Source: Internetová jazyková příručka ÚJČ, id=792.
    /// </para>
    /// </remarks>
    public enum CardinalAgreement
    {
        /// <summary>
        /// Imposes nothing; the numeral is an ordinary agreeing attribute. Ordinals and multiplicatives.
        /// </summary>
        None,
        /// <summary>
        /// The counted noun agrees in the singular: jeden student.
        /// </summary>
        AgreesSingular,
        /// <summary>
        /// The counted noun agrees in the plural: dva studenti, tři ženy, oba muži.
        /// </summary>
        AgreesPlural,
        /// <summary>
        /// The counted noun stands in the genitive plural in the direct cases and agrees in the plural
        /// elsewhere: pět studentů, but o pěti studentech. Covers five through ninety-nine and the
        /// indefinites několik, kolik, tolik, and mnoho with a countable noun.
        /// </summary>
        GenitivePluralInDirectCases,
        /// <summary>
        /// The counted noun stands in the genitive plural in every case, because the numeral is itself a
        /// noun: sto lidí, tisíc korun, milion obyvatel.
        /// </summary>
        AlwaysGenitivePlural,
        /// <summary>
        /// The counted noun stands in the genitive singular: mnoho práce, 1,5 metru.
        /// </summary>
        GenitiveSingular
    }
}
