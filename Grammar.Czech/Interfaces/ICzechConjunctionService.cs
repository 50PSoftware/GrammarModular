using Grammar.Core.Enums;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for resolving Czech conjunction behavior.
    /// </summary>
    /// <remarks>
    /// The lookups take an optional reading. Some conjunctions are two words under one spelling and differ
    /// in type — ať subordinates a content clause and coordinates a split one, jak coordinates the paired
    /// "jak … tak" and subordinates a comparison — so a caller that knows which construction it is building
    /// says so and gets that reading. Omitting it gives the primary reading, which is what a lemma with only
    /// one has anyway.
    /// </remarks>
    public interface ICzechConjunctionService
    {
        /// <summary>
        /// Gets how the supplied conjunction joins clauses.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns>The conjunction type.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        ConjunctionType GetType(string conjunction, ConjunctionType? reading = null);

        /// <summary>
        /// Gets the relation the supplied conjunction establishes between what it joins.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns>The semantic group of the conjunction.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        ConjunctionSemanticGroup GetSemanticGroup(string conjunction, ConjunctionType? reading = null);

        /// <summary>
        /// Determines whether a comma is written before the supplied conjunction.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> when a comma precedes the conjunction; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        bool RequiresComma(string conjunction, ConjunctionType? reading = null);

        /// <summary>
        /// Determines whether the conjunction occupies the first position of the clause it introduces,
        /// which is what the clitic cluster attaches after.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> for a subordinating conjunction; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        bool OccupiesFirstPosition(string conjunction);

        /// <summary>
        /// Determines whether the conjunction stands after the first constituent of its clause rather than
        /// in front of it.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> for však; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        bool OccupiesSecondPosition(string conjunction, ConjunctionType? reading = null);

        /// <summary>
        /// Gets the second member of a paired conjunction.
        /// </summary>
        /// <param name="conjunction">The opening member to look up.</param>
        /// <returns>The second member, or <see langword="null"/> when the conjunction is not paired.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        string? GetCorrelate(string conjunction, ConjunctionType? reading = null);

        /// <summary>
        /// Determines whether the conjunction absorbs the conditional auxiliary and inflects with it.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> for aby and kdyby; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        bool FusesWithConditional(string conjunction);

        /// <summary>
        /// Builds the surface form of the conjunction for the requested grammatical number and person.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <param name="number">The grammatical number of the dependent clause's predicate.</param>
        /// <param name="person">The grammatical person of the dependent clause's predicate.</param>
        /// <returns>
        /// The inflected form for a conjunction that fuses with the conditional auxiliary — abych, abys,
        /// abychom — and the conjunction unchanged for every other one.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        string GetForm(string conjunction, Number? number, Person? person);

        /// <summary>
        /// Gets every reading the supplied conjunction has, the primary one first.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns>The readings registered for it.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        IReadOnlyList<Models.ConjunctionData> GetReadings(string conjunction);

        /// <summary>
        /// Gets the conjunctions that join clauses in the requested way, the least marked one first.
        /// </summary>
        /// <param name="type">Whether the clauses are of equal rank or one depends on the other.</param>
        /// <param name="semanticGroup">The relation between them.</param>
        /// <returns>The matching conjunctions, or an empty sequence when none is registered.</returns>
        /// <remarks>
        /// The inverse of <see cref="GetSemanticGroup"/>, and the reason it is worth having: the semantic
        /// groups are the traditional taxonomy of dependent clauses under different names — Purpose is the
        /// účelová věta, Causal the příčinná, Concessive the přípustková, Conditional the podmínková — so a
        /// caller can say what kind of clause it wants instead of knowing which word expresses it.
        /// <para>
        /// Both readings of a conjunction are searched, so asking for a subordinating comparison finds jak,
        /// whose primary reading is coordinating. Conjunctions matching on their primary reading come first,
        /// and within each group the data's own order is kept — it lists the unmarked choice first, so
        /// taking the first result gives protože for a causal clause rather than ježto.
        /// </para>
        /// </remarks>
        IReadOnlyList<string> GetConjunctionsFor(ConjunctionType type, ConjunctionSemanticGroup semanticGroup);
    }
}
