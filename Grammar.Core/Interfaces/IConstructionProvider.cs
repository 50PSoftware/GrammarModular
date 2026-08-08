using Grammar.Core.Models.Valency;

namespace Grammar.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for reading light verb constructions out of the dictionary.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="IValencyProvider{T}"/> because it answers a different question. A
    /// valency frame is looked up by one lemma; a construction is identified by two standing together,
    /// and neither of them on its own means what the pair does.
    /// </remarks>
    public interface IConstructionProvider
    {
        /// <summary>
        /// Returns the constructions registered for the given light verb.
        /// </summary>
        /// <param name="verbLemma">The infinitive form of the verb (case-insensitive).</param>
        /// <returns>The constructions, or an empty sequence when the verb heads none.</returns>
        IEnumerable<ConstructionTemplate> GetConstructions(string verbLemma);

        /// <summary>
        /// Returns the construction registered under the given pattern name.
        /// </summary>
        /// <param name="patternName">The name of the pattern.</param>
        /// <returns>The construction, or <see langword="null"/> when nothing is registered under it.</returns>
        ConstructionTemplate? GetConstruction(string patternName);
    }
}
