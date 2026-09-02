using Grammar.Core.Models.Semantics;

namespace Grammar.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for reading componential semantic features out of the dictionary.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="IValencyProvider{T}"/> and <see cref="ISemanticRelationProvider"/>
    /// because it answers a different question again: what one sense is, rather than how it relates to
    /// another or what case it assigns.
    /// </remarks>
    public interface ISemanticFeatureProvider
    {
        /// <summary>
        /// Returns every feature recorded for the given sense.
        /// </summary>
        /// <param name="luId">The identifier of the lexical unit — the sense — to read features for.</param>
        /// <returns>The features, or an empty sequence when the sense has none recorded.</returns>
        IEnumerable<SemanticFeature> GetFeatures(long luId);
    }
}
