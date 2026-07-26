using Grammar.Core.Enums;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for resolving Czech preposition case government and semantic groups.
    /// </summary>
    public interface ICzechPrepositionService
    {
        /// <summary>
        /// Gets the grammatical cases allowed by the supplied preposition.
        /// </summary>
        /// <param name="preposition">The preposition text to look up.</param>
        /// <returns>The grammatical cases governed by the preposition, or an empty sequence when it is unknown.</returns>
        IEnumerable<Case> GetAllowedCases(string preposition);

        /// <summary>
        /// Gets the semantic group for a preposition and governed case.
        /// </summary>
        /// <param name="preposition">The preposition text to look up.</param>
        /// <param name="kase">The grammatical case governed by the preposition.</param>
        /// <returns>The semantic group for the matching preposition variant, or <see langword="null"/> when no variant matches.</returns>
        PrepositionSemanticGroup? GetSemanticGroup(string preposition, Case kase);

        /// <summary>
        /// Gets the surface form of the preposition before the supplied word, vocalized where required.
        /// </summary>
        /// <param name="preposition">The preposition text to look up.</param>
        /// <param name="followingWord">The word that immediately follows the preposition.</param>
        /// <returns>The vocalized variant when the following word requires it; otherwise, the preposition unchanged.</returns>
        string Vocalize(string preposition, string followingWord);

        /// <summary>
        /// Determines whether the supplied preposition can govern the requested case.
        /// </summary>
        /// <param name="preposition">The preposition text to look up.</param>
        /// <param name="kase">The grammatical case governed by the preposition.</param>
        /// <returns><see langword="true"/> when the case is allowed for the preposition; otherwise, <see langword="false"/>.</returns>
        bool IsAllowed(string preposition, Case kase);
    }
}
