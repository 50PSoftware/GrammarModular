namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies grammatical mood values for verb forms.
    /// </summary>
    public enum Modus
    {
        /// <summary>
        /// Represents the conditional value.
        /// </summary>
        Conditional,
        /// <summary>
        /// Represents the imperative value.
        /// </summary>
        Imperative,
        /// <summary>
        /// Represents the conjunctive value.
        /// </summary>
        Conjunctive,
        /// <summary>
        /// Represents the indicative value.
        /// </summary>
        Indicative,
        /// <summary>
        /// Represents the infinitive, the form that states no person, number or tense.
        /// </summary>
        /// <remarks>
        /// Strictly the infinitive is a non-finite form rather than a mood, and it sits here because
        /// this is the field that selects which form of the verb to build. Putting it anywhere else
        /// would mean two things deciding that, and a request could then ask for both at once.
        /// <para>
        /// Appended rather than sorted into place: the value is what a caller may have persisted, so
        /// renumbering the members would silently change what an existing request asks for.
        /// </para>
        /// </remarks>
        Infinitive
    }
}
