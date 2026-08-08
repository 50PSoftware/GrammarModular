using Grammar.Czech.Models.Syntax;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Holds the whole sentence as the tool understands it: one clause, or several joined by
    /// conjunctions.
    /// </summary>
    /// <remarks>
    /// A separate type from <see cref="ClauseDraft"/> because a conjunction in the word list is what
    /// splits the sentence, and everything the review does — showing the reading, taking corrections,
    /// reporting what is still open — has to work across all of it rather than inside one clause.
    /// </remarks>
    public sealed class SentenceDraft
    {
        /// <summary>
        /// Gets the clauses, in the order they were entered.
        /// </summary>
        /// <remarks>
        /// The first carries no conjunction; every one after it carries the conjunction that attached
        /// it, which is also what says whether it is coordinated or subordinated.
        /// </remarks>
        public List<ClauseDraft> Clauses { get; } = [];

        /// <summary>
        /// Gets the first clause, which is the one the sentence is about.
        /// </summary>
        public ClauseDraft Main => Clauses[0];

        /// <summary>
        /// Gets the remarks worth showing with the sentence, from every clause.
        /// </summary>
        public IReadOnlyList<string> Notes =>
            [.. Clauses.SelectMany(clause => clause.Notes).Distinct()];

        /// <summary>
        /// Gets the reasons the sentence cannot be built yet, from every clause.
        /// </summary>
        /// <returns>The open questions, empty when the sentence is complete.</returns>
        public IReadOnlyList<string> Gaps() => [.. Clauses.SelectMany(clause => clause.Gaps())];

        /// <summary>
        /// Gets the positions the predicates were entered in, which the review refuses as targets.
        /// </summary>
        public IReadOnlyList<int> PredicatePositions =>
            [.. Clauses.Select(clause => clause.PredicatePosition)];

        /// <summary>
        /// Builds the plan the library works from, with the later clauses hanging off the first.
        /// </summary>
        /// <returns>The plan.</returns>
        /// <exception cref="CliException">Thrown when a clause has not been through the planner.</exception>
        public SentencePlan ToPlan() => Main.ToPlan() with
        {
            Joined =
            [
                .. Clauses.Skip(1).Select(clause => new ClauseLink(
                    clause.Conjunction ?? throw new CliException(
                        $"Klauze se slovesem '{clause.PredicateLemma}' nemá spojku, kterou by se připojila."),
                    clause.ToPlan())),
            ],
        };
    }
}
