using Grammar.Czech.Models.Syntax;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Holds the whole sentence as the tool understands it: one clause, or several joined by
    /// conjunctions into a tree.
    /// </summary>
    /// <remarks>
    /// A separate type from <see cref="ClauseDraft"/> because a conjunction in the word list is what
    /// splits the sentence, and everything the review does — showing the reading, taking corrections,
    /// reporting what is still open — has to work across all of it rather than inside one clause.
    /// <para>
    /// The clauses are kept as a flat list in the order they were entered, with each one naming the
    /// clause it hangs off. That is what the user typed and what the review shows; the tree is derived
    /// from it when the plan is built, which keeps a correction to one attachment from having to move
    /// anything else.
    /// </para>
    /// </remarks>
    public sealed class SentenceDraft
    {
        /// <summary>
        /// Gets the clauses, in the order they were entered.
        /// </summary>
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
        /// Builds the plan the library works from, with each clause hanging off the one it names.
        /// </summary>
        /// <returns>The plan.</returns>
        /// <exception cref="CliException">Thrown when a clause has not been through the planner.</exception>
        public SentencePlan ToPlan() => Main.ToPlan();

        /// <summary>
        /// Assembles the tree out of the clauses' own plans, before anything is defaulted.
        /// </summary>
        /// <returns>The assembled plan.</returns>
        /// <exception cref="CliException">Thrown when a clause has no plan or no conjunction.</exception>
        public SentencePlan Assemble() => Assemble(Main);

        /// <summary>
        /// Hands each clause back the completed plan of its own part of the tree.
        /// </summary>
        /// <param name="completed">The completed plan of the whole sentence.</param>
        /// <remarks>
        /// Walks the same order <see cref="Assemble"/> built, which is what makes the pairing safe:
        /// completing a plan changes what is in each clause, never how many there are or where.
        /// </remarks>
        public void Distribute(SentencePlan completed) => Distribute(Main, completed);

        private SentencePlan Assemble(ClauseDraft clause) => Resolved(clause) with
        {
            Joined =
            [
                .. Children(clause).Select(child => new ClauseLink(
                    child.Conjunction ?? throw new CliException(
                        $"Klauze se slovesem '{child.PredicateLemma}' nemá spojku, kterou by se připojila."),
                    Assemble(child))),
            ],
        };

        private void Distribute(ClauseDraft clause, SentencePlan plan)
        {
            clause.Plan = plan;

            foreach (var (child, link) in Children(clause).Zip(plan.Joined))
            {
                Distribute(child, link.Clause);
            }
        }

        private static SentencePlan Resolved(ClauseDraft clause) => clause.Resolved
            ?? throw new CliException(
                $"Klauze se slovesem '{clause.PredicateLemma}' nemá rozebraný plán.");

        private IEnumerable<ClauseDraft> Children(ClauseDraft parent) =>
            Clauses.Where(clause => clause.ParentOrdinal == parent.Ordinal);
    }
}
