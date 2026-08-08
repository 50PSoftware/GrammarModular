using Grammar.Core.Models.Valency;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines the contract for recognizing a light verb construction and reading what it governs.
    /// </summary>
    public interface ICzechConstructionService
    {
        /// <summary>
        /// Finds the construction the verb makes with one of the supplied nouns.
        /// </summary>
        /// <param name="verbLemma">The lemma of the verb.</param>
        /// <param name="nounLemmas">The lemmas standing with it, in any order.</param>
        /// <returns>The construction, or <see langword="null"/> when the words make none.</returns>
        ConstructionTemplate? Find(string verbLemma, IEnumerable<string> nounLemmas);

        /// <summary>
        /// Gets the frame of the construction registered under the supplied pattern name.
        /// </summary>
        /// <param name="patternName">The name of the pattern.</param>
        /// <returns>The frame, or <see langword="null"/> when nothing is registered under that name.</returns>
        ValencyFrame? GetFrame(string patternName);
    }
}
