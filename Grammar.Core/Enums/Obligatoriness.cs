namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies how strongly a valency slot is required by its frame.
    /// </summary>
    /// <remarks>
    /// FGD grades this rather than treating it as a yes or no. The middle value is what a plain boolean
    /// could not express: the addressee of dát is present in the meaning of every use of the verb and is
    /// recoverable from context when unsaid (dal to = dal to jemu), which is a different thing from an
    /// optional directional that simply is not part of the event.
    /// </remarks>
    public enum Obligatoriness
    {
        /// <summary>
        /// The slot has to be expressed on the surface; omitting it yields an ungrammatical clause.
        /// </summary>
        Obligatory = 1,

        /// <summary>
        /// The slot belongs to the meaning of the predicate and may stay unexpressed when context supplies it.
        /// </summary>
        Typical,

        /// <summary>
        /// The slot may be filled or left out freely.
        /// </summary>
        Optional
    }
}
