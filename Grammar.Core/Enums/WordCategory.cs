namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies lexical categories supported by the grammar engine.
    /// </summary>
    public enum WordCategory
    {
        /// <summary>
        /// Represents the noun value.
        /// </summary>
        Noun,
        /// <summary>
        /// Represents the adjective value.
        /// </summary>
        Adjective,
        /// <summary>
        /// Represents the pronoun value.
        /// </summary>
        Pronoun,
        /// <summary>
        /// Represents the numerale value.
        /// </summary>
        Numerale,
        /// <summary>
        /// Represents the verb value.
        /// </summary>
        Verb,
        /// <summary>
        /// Represents the adverb value. Uninflected — comparison is the only morphology it has.
        /// </summary>
        Adverb
    }
}
