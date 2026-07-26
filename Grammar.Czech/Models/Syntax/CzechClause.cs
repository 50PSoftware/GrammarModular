namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents a single Czech clause as a predicate plus its constituents, before linearization.
    /// </summary>
    /// <remarks>
    /// The clause carries no word order of its own. <see cref="Services.CzechSentenceBuilder"/> derives the
    /// order from <see cref="ClauseElement.Status"/>, so the caller states what is old and what is new rather
    /// than where things go. The order of <see cref="Elements"/> only breaks ties within one status.
    /// </remarks>
    public sealed record CzechClause
    {
        /// <summary>
        /// Gets the word request for the predicate.
        /// </summary>
        public CzechWordRequest Predicate { get; init; }

        /// <summary>
        /// Gets the non-predicate constituents of the clause.
        /// </summary>
        public IReadOnlyList<ClauseElement> Elements { get; init; } = [];

        /// <summary>
        /// Gets the punctuation mark that closes the sentence.
        /// </summary>
        public string Terminator { get; init; } = ".";
    }
}
