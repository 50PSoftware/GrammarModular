namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies the semantic kind of a numeral.
    /// </summary>
    /// <remarks>
    /// The two axes of the Czech classification are kind and definiteness. Only kind is modelled here —
    /// indefiniteness (několik, mnoho, kolik) cuts across every one of these and shows up as a missing
    /// numeric value rather than as a separate member.
    /// </remarks>
    public enum NumeralType
    {
        /// <summary>
        /// Represents the cardinal value, answering "how many": pět, dvacet, několik.
        /// </summary>
        Cardinal,
        /// <summary>
        /// Represents the ordinal value, answering "which in order": pátý, třetí, tisící.
        /// </summary>
        Ordinal,
        /// <summary>
        /// Represents the sortal value, counting kinds of a thing: dvojí, paterý.
        /// </summary>
        Sortal,
        /// <summary>
        /// Represents the set value, counting sets or pairs: dvoje, patery.
        /// </summary>
        Set,
        /// <summary>
        /// Represents the aggregate value, a quantity taken as one whole: patero, desatero.
        /// </summary>
        Aggregate,
        /// <summary>
        /// Represents the multiplicative value, answering "how many times": dvakrát, dvojnásobný.
        /// </summary>
        Multiplicative,
        /// <summary>
        /// Represents the fractional value: polovina, třetina.
        /// </summary>
        Fractional,
        /// <summary>
        /// Represents the group value, a noun naming a group of that size: trojice, pětka.
        /// </summary>
        Group
    }
}
