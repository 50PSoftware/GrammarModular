namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents a sentence as a tree of clauses.
    /// </summary>
    /// <remarks>
    /// A <see cref="CzechClause"/> stays what it always was — one clause. This composes clauses into a
    /// sentence, so the pieces that belong to the sentence rather than to any single clause (capitalization,
    /// the final punctuation mark, the commas between clauses) are decided in one place.
    /// </remarks>
    public abstract record SentenceNode
    {
        /// <summary>
        /// Wraps a single clause as a sentence.
        /// </summary>
        /// <param name="clause">The clause to wrap.</param>
        public static implicit operator SentenceNode(CzechClause clause) => new SimpleSentence(clause);
    }

    /// <summary>
    /// Represents a sentence consisting of one clause.
    /// </summary>
    /// <param name="Clause">The clause.</param>
    public sealed record SimpleSentence(CzechClause Clause) : SentenceNode;

    /// <summary>
    /// Represents clauses of equal rank joined by a coordinating conjunction.
    /// </summary>
    /// <param name="Conjunction">The coordinating conjunction placed before every conjunct but the first.</param>
    /// <param name="Conjuncts">The coordinated clauses, in order.</param>
    /// <param name="RequiresComma">
    /// Overrides the conjunction's default comma rule, for the conjunctions where punctuation follows the
    /// relation between the clauses rather than the conjunction itself.
    /// </param>
    /// <remarks>
    /// The conjunction stands outside the clause it precedes, so it does not take that clause's first
    /// position and the clitic cluster is unaffected by it.
    /// <para>
    /// nebo and či are punctuated by the relation, not by the word: no comma when the alternatives are
    /// freely interchangeable ("Podejte nám zprávu písemně nebo telefonicky"), a comma when they exclude
    /// each other ("Pospěšte si, nebo vám ujede vlak"). The data can only carry the commoner of the two,
    /// so the exclusive reading is stated here.
    /// </para>
    /// </remarks>
    public sealed record Coordination(
        string Conjunction,
        IReadOnlyList<SentenceNode> Conjuncts,
        bool? RequiresComma = null) : SentenceNode;

    /// <summary>
    /// Represents a clause attached to a superordinate one by a subordinating conjunction.
    /// </summary>
    /// <param name="Main">The superordinate clause.</param>
    /// <param name="Conjunction">The subordinating conjunction introducing the dependent clause.</param>
    /// <param name="Subordinate">The dependent clause.</param>
    /// <remarks>
    /// The conjunction belongs to the dependent clause and fills its first position, so the clitic cluster
    /// follows the conjunction rather than the verb: "protože se učil".
    /// </remarks>
    public sealed record Subordination(SentenceNode Main, string Conjunction, SentenceNode Subordinate) : SentenceNode;
}
