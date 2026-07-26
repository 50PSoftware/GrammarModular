namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies how a conjunction joins clauses.
    /// </summary>
    public enum ConjunctionType
    {
        /// <summary>
        /// Joins clauses of equal rank. It stands outside the clause it introduces and does not occupy
        /// the clause's first position.
        /// </summary>
        Coordinating,

        /// <summary>
        /// Attaches a clause to a superordinate one. It belongs to the clause it introduces and occupies
        /// its first position, which the clitic cluster then follows.
        /// </summary>
        Subordinating
    }
}
