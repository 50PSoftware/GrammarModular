using Grammar.Core.Models.Semantics;

namespace Grammar.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for reading synonymy and antonymy relations out of the dictionary.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="ISemanticFeatureProvider"/> because a relation is identified by two
    /// senses standing together, the same reason <see cref="IConstructionProvider"/> is kept apart from
    /// <see cref="IValencyProvider{T}"/>.
    /// </remarks>
    public interface ISemanticRelationProvider
    {
        /// <summary>
        /// Returns every relation recorded for the given sense, in either direction of the pair.
        /// </summary>
        /// <param name="luId">The identifier of the lexical unit — the sense — to read relations for.</param>
        /// <returns>The relations, or an empty sequence when the sense has none recorded.</returns>
        /// <remarks>
        /// The relation is symmetric and stored once, so a caller asking about one sense needs both sides
        /// checked — which is exactly what this method does, rather than leaving it to every caller.
        /// </remarks>
        IEnumerable<SemanticRelation> GetRelations(long luId);
    }
}
