namespace Grammar.Czech.Enums
{
    /// <summary>
    /// Specifies how a compound numeral from twenty-one to ninety-nine is written and construed.
    /// </summary>
    /// <remarks>
    /// The Internetová jazyková příručka (id=792) admits three treatments, all standard, and calls the
    /// genitive-plural one the more natural. The choice changes both the surface form and the case of the
    /// counted noun, which is why it is one setting rather than two.
    /// </remarks>
    public enum CompoundVariant
    {
        /// <summary>
        /// Written apart, governing the genitive plural: dvacet jedna žáků bylo. The příručka's preference.
        /// </summary>
        Preferred,

        /// <summary>
        /// Written apart, agreeing with the last member: dvacet jeden žák byl, dvacet čtyři žáci byli.
        /// </summary>
        AgreeingLastMember,

        /// <summary>
        /// Contracted into one word, governing the genitive plural: čtyřiadvacet žáků bylo.
        /// </summary>
        Contracted
    }
}
