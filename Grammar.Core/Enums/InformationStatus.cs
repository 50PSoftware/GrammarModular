namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies the communicative status of a clause element for functional sentence perspective.
    /// </summary>
    /// <remarks>
    /// Czech word order is pragmatic rather than syntactic — the case system identifies the clause members,
    /// so linear order is free to express what is old and what is new. Given material opens the clause,
    /// new material closes it.
    /// </remarks>
    public enum InformationStatus
    {
        /// <summary>
        /// Old or contextually recoverable material. Belongs to the theme, before the verb.
        /// </summary>
        Given,

        /// <summary>
        /// New or communicatively most important material. Belongs to the rheme, after the verb.
        /// </summary>
        New,

        /// <summary>
        /// Material set against an alternative. Fronted ahead of the theme.
        /// </summary>
        Contrastive
    }
}
