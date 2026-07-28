using Grammar.Core.Enums;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for resolving Czech conjunction behavior.
    /// </summary>
    public interface ICzechConjunctionService
    {
        /// <summary>
        /// Gets how the supplied conjunction joins clauses.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns>The conjunction type.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        ConjunctionType GetType(string conjunction);

        /// <summary>
        /// Gets the relation the supplied conjunction establishes between what it joins.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns>The semantic group of the conjunction.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        ConjunctionSemanticGroup GetSemanticGroup(string conjunction);

        /// <summary>
        /// Determines whether a comma is written before the supplied conjunction.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> when a comma precedes the conjunction; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        bool RequiresComma(string conjunction);

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
        bool OccupiesSecondPosition(string conjunction);

        /// <summary>
        /// Gets the second member of a paired conjunction.
        /// </summary>
        /// <param name="conjunction">The opening member to look up.</param>
        /// <returns>The second member, or <see langword="null"/> when the conjunction is not paired.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the conjunction is not in the inventory.</exception>
        string? GetCorrelate(string conjunction);

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
    }
}
