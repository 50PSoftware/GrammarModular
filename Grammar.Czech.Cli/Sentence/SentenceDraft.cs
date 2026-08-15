using Grammar.Core.Enums;
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
        /// Gets every clause of the sentence, the siblings and the relative clauses hanging off their
        /// constituents alike, in the order they were entered.
        /// </summary>
        /// <remarks>
        /// What "the whole sentence" means to anything that reports on it. A relative clause is not in
        /// <see cref="Clauses"/>, so a gap inside one would otherwise go unmentioned and the review would
        /// confirm a sentence it had not finished reading.
        /// </remarks>
        public IEnumerable<ClauseDraft> AllClauses => Clauses.SelectMany(Descend);

        /// <summary>
        /// Gets every relative clause of the sentence with the constituent it hangs off, in the order
        /// they were entered.
        /// </summary>
        /// <remarks>
        /// The two travel together because neither says enough alone: the pronoun's gender and number
        /// come from the constituent and its case from the clause, and every question worth asking about
        /// a relative clause needs both sides of that.
        /// </remarks>
        public IEnumerable<(ConstituentDraft Host, RelativeDraft Relative)> AllRelatives =>
            Clauses.SelectMany(clause => Relatives(clause).SelectMany(pair =>
                new[] { (clause.Constituents[pair.Index], pair.Relative) }
                    .Concat(pair.Relative.Clause.AllRelatives)));

        /// <summary>
        /// Gets the remarks worth showing with the sentence, from every clause.
        /// </summary>
        public IReadOnlyList<string> Notes =>
            [.. AllClauses.SelectMany(clause => clause.Notes).Distinct()];

        /// <summary>
        /// Gets the reasons the sentence cannot be built yet, from every clause.
        /// </summary>
        /// <returns>The open questions, empty when the sentence is complete.</returns>
        public IReadOnlyList<string> Gaps() => [.. AllClauses.SelectMany(clause => clause.Gaps())];

        /// <summary>
        /// Gets the positions the predicates were entered in, which the review refuses as targets.
        /// </summary>
        public IReadOnlyList<int> PredicatePositions =>
            [.. AllClauses.Select(clause => clause.PredicatePosition)];

        /// <summary>
        /// Finds the sentence a clause belongs to: this one, or one hanging inside it.
        /// </summary>
        /// <param name="ordinal">The one-based number of the clause to look for.</param>
        /// <returns>The sentence holding it, or <see langword="null"/> when no clause has that number.</returns>
        /// <remarks>
        /// A relative clause is a sentence of its own, so a clause number alone does not say which list it
        /// lives in. Attaching across that boundary is what needs to know — the clause moves into the
        /// sentence its new parent belongs to, because that is the sentence whose tree will hold it.
        /// </remarks>
        public SentenceDraft? Holding(int ordinal) =>
            Clauses.Any(clause => clause.Ordinal == ordinal)
                ? this
                : Clauses
                    .SelectMany(Relatives)
                    .Select(pair => pair.Relative.Clause.Holding(ordinal))
                    .FirstOrDefault(found => found is not null);

        /// <summary>
        /// Builds the plan the library works from, with each clause hanging off the one it names.
        /// </summary>
        /// <returns>The plan.</returns>
        /// <exception cref="CliException">Thrown when a clause has not been through the planner.</exception>
        public SentencePlan ToPlan() => Main.ToPlan();

        /// <summary>
        /// Assembles the tree out of the clauses' own plans, before any role is worked out.
        /// </summary>
        /// <returns>The assembled plan.</returns>
        /// <exception cref="CliException">Thrown when a clause has no plan or no conjunction.</exception>
        public SentencePlan Assemble() => Assemble(Main, Stated);

        /// <summary>
        /// Assembles the tree again out of what the resolver already worked out.
        /// </summary>
        /// <returns>The assembled plan.</returns>
        /// <exception cref="CliException">Thrown when a clause has no plan or no conjunction.</exception>
        /// <remarks>
        /// The same tree the second time, and it exists because one thing about a relative clause cannot
        /// be known until the first pass is over: a possessive relativizer names the participant it
        /// possesses by functor, and the functor is what resolving works out. Rebuilding rather than
        /// patching keeps that one late value on the same path as everything else.
        /// </remarks>
        public SentencePlan Reassemble() => Assemble(Main, Resolved);

        /// <summary>
        /// Hands each clause back its own part of the resolved tree.
        /// </summary>
        /// <param name="resolved">The resolved plan of the whole sentence.</param>
        public void TakeResolved(SentencePlan resolved) =>
            Distribute(Main, resolved, static (clause, plan) => clause.Resolved = plan);

        /// <summary>
        /// Hands each clause back the completed plan of its own part of the tree.
        /// </summary>
        /// <param name="completed">The completed plan of the whole sentence.</param>
        public void Distribute(SentencePlan completed) =>
            Distribute(Main, completed, static (clause, plan) => clause.Plan = plan);

        // Jeden průchod stromem pro oba směry. Rozdělit ho na dva znamenalo, že se pořadí dá rozejít
        // úpravou jednoho z nich, a rozejde se tiše: hodnoty by se doplnily jinam, než odkud se vzaly.
        // Vztažná věta na členu je přitom klauze, která v Clauses není, takže se musí projít zvlášť.
        private static IEnumerable<(int Index, RelativeDraft Relative)> Relatives(ClauseDraft clause) =>
            clause.Constituents
                .Select((constituent, index) => (Index: index, constituent.Relative))
                .Where(pair => pair.Relative is not null)
                .Select(pair => (pair.Index, pair.Relative!));

        private static IEnumerable<ClauseDraft> Descend(ClauseDraft clause) =>
            new[] { clause }.Concat(
                Relatives(clause).SelectMany(pair => pair.Relative.Clause.AllClauses));

        private SentencePlan Assemble(ClauseDraft clause, Func<ClauseDraft, SentencePlan> source)
        {
            var plan = source(clause);
            var participants = plan.Participants.ToList();

            foreach (var (index, relative) in Relatives(clause))
            {
                participants[index] = participants[index] with
                {
                    Relative = new PlannedRelative
                    {
                        Relativizer = relative.Relativizer,

                        // Vztažné příslovce ani přivlastňovací zájmeno pád nemá; jádro ho u nich ignoruje,
                        // tak ať je to nominativ a ne vymyšlená hodnota, na které by mohlo něco stavět.
                        Case = relative.Case ?? Case.Nominative,
                        Possessed = relative.Possessed,
                        Clause = relative.Clause.Assemble(relative.Clause.Main, source),
                    },
                };
            }

            return plan with
            {
                Participants = participants,
                Joined =
                [
                    .. Children(clause).Select(child => new ClauseLink(
                        child.Conjunction ?? throw new CliException(
                            $"Klauze se slovesem '{child.PredicateLemma}' nemá spojku, kterou by se připojila."),
                        Assemble(child, source))),
                ],
            };
        }

        // Protějšek Assemble, krok za krokem tímtéž stromem: co tam sestoupilo do vztažné věty a co do
        // souřadné klauze, sem sestoupí ve stejném pořadí. Doplnění mění, co v klauzi je, ne kolik jich
        // je ani kde — takže se páruje strukturou, ne hledáním.
        private void Distribute(ClauseDraft clause, SentencePlan plan, Action<ClauseDraft, SentencePlan> assign)
        {
            assign(clause, plan);

            foreach (var (index, relative) in Relatives(clause))
            {
                relative.Clause.Distribute(
                    relative.Clause.Main,
                    plan.Participants[index].Relative?.Clause ?? throw new CliException(
                        $"Vztažná věta u slova '{clause.Constituents[index].Lemma}' se v plánu ztratila."),
                    assign);
            }

            foreach (var (child, link) in Children(clause).Zip(plan.Joined))
            {
                Distribute(child, link.Clause, assign);
            }
        }

        private static SentencePlan Stated(ClauseDraft clause) => clause.Stated
            ?? throw new CliException(
                $"Klauze se slovesem '{clause.PredicateLemma}' nemá rozebraný plán.");

        private static SentencePlan Resolved(ClauseDraft clause) => clause.Resolved
            ?? throw new CliException(
                $"Klauze se slovesem '{clause.PredicateLemma}' neprošla rozdělením rolí.");

        private IEnumerable<ClauseDraft> Children(ClauseDraft parent) =>
            Clauses.Where(clause => clause.ParentOrdinal == parent.Ordinal);
    }
}
