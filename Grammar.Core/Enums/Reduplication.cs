namespace Grammar.Core.Enums
{
    /// <summary>
    /// Represents whether a word form is repeated when used.
    /// </summary>
    public enum Reduplication
    {
        /// <summary>
        /// Not recorded as repeating.
        /// </summary>
        /// <remarks>
        /// The absence of a record, not a claim that the word cannot repeat. Czech interjections reduplicate
        /// freely and NESČ names only a few cases outright, so this is what an unmarked entry gets.
        /// </remarks>
        None,

        /// <summary>
        /// Repeats where the speaker wants it, and stands on its own otherwise — oj oj oj, ťuk(y) ťuk, no no.
        /// </summary>
        Optional,

        /// <summary>
        /// Does not occur unrepeated: bubu, never *bu.
        /// </summary>
        Required
    }
}
