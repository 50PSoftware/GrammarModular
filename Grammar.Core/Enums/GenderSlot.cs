namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies the gender slots a paradigm table is keyed by.
    /// </summary>
    /// <remarks>
    /// Wider than <see cref="Gender"/> on purpose: animacy splits the masculine in the singular, while the
    /// plural collapses feminine, neuter and masculine inanimate onto shared forms. Pronouns, numerals and any
    /// other paradigm stored as a nested table use these slots as their middle key.
    /// </remarks>
    public enum GenderSlot
    {
        /// <summary>
        /// Represents the masculine animate value.
        /// </summary>
        MasculineAnimate,
        /// <summary>
        /// Represents the masculine inanimate value.
        /// </summary>
        MasculineInanimate,
        /// <summary>
        /// Represents the feminine value.
        /// </summary>
        Feminine,
        /// <summary>
        /// Represents the neuter value.
        /// </summary>
        Neuter,
        /// <summary>
        /// Represents the shared slot used where genders collapse onto one form, typically in the plural.
        /// </summary>
        Other
    }
}
