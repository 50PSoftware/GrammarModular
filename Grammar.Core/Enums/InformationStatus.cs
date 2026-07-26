namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies the communicative status of a clause element for functional sentence perspective.
    /// </summary>
    /// <remarks>
    /// Czech word order is pragmatic rather than syntactic — the case system identifies the clause members,
    /// so linear order is free to express what is old and what is new. Given material opens the clause,
    /// new material closes it, and contrastive given material comes first of all, because NESČ puts
    /// contrastive contextually-engaged nodes at a lower degree of communicative dynamism than
    /// non-contrastive ones.
    /// <para>
    /// Word order is only one exponent of this. NESČ is explicit that intonation carries it too, and that
    /// two readings distinguished by prosody alone are two different sentences rather than variants of one.
    /// Nothing here models that, so a status set on an element decides its position and nothing more.
    /// </para>
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
