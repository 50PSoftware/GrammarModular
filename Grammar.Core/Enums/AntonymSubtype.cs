namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies how two antonymous senses oppose each other.
    /// </summary>
    /// <remarks>
    /// A distinction a flat WordNet-style antonym link cannot make, and one componential features alone
    /// do not settle automatically either.
    /// </remarks>
    public enum AntonymSubtype
    {
        /// <summary>Asserting one sense denies the other and there is nothing between them (živý/mrtvý).</summary>
        Complementary,

        /// <summary>The two sit at opposite ends of a continuous scale (horký/studený).</summary>
        Scalar,

        /// <summary>The same relation seen from each participant's side (manžel/manželka).</summary>
        Converse
    }
}
