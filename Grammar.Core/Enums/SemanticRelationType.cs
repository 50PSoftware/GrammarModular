namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies the kind of relation <c>semantic_relation</c> states between two senses.
    /// </summary>
    public enum SemanticRelationType
    {
        /// <summary>The two senses are close enough in meaning to substitute for each other in some context.</summary>
        Synonym,

        /// <summary>The two senses are opposites. See <see cref="AntonymSubtype"/> for how.</summary>
        Antonym
    }
}
