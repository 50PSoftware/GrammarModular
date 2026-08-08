using Grammar.Core.Interfaces;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Interfaces;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Recognizes a light verb construction from the words that make it up.
    /// </summary>
    /// <remarks>
    /// A construction cannot be looked up by one lemma, which is exactly what makes it one: neither
    /// <em>mít</em> nor <em>zájem</em> on its own means what <em>mít zájem</em> means, and the argument
    /// the pair takes — <em>o něco</em> — belongs to neither of them alone. So the question this answers
    /// takes both.
    /// <para>
    /// What comes back is an ordinary <see cref="ValencyFrame"/> named after the pattern, so once the
    /// pair is recognized the rest of the pipeline needs to know nothing about constructions at all.
    /// </para>
    /// </remarks>
    public class CzechConstructionService : ICzechConstructionService
    {
        private readonly IConstructionProvider provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechConstructionService"/> type.
        /// </summary>
        /// <param name="provider">The dictionary to read the constructions from.</param>
        public CzechConstructionService(IConstructionProvider provider)
        {
            this.provider = provider;
        }

        /// <summary>
        /// Finds the construction the verb makes with one of the supplied nouns.
        /// </summary>
        /// <param name="verbLemma">The lemma of the verb.</param>
        /// <param name="nounLemmas">The lemmas standing with it, in any order.</param>
        /// <returns>The construction, or <see langword="null"/> when the words make none.</returns>
        public ConstructionTemplate? Find(string verbLemma, IEnumerable<string> nounLemmas)
        {
            var nouns = nounLemmas.ToList();

            return provider.GetConstructions(verbLemma).FirstOrDefault(construction =>
                construction.PredicativeNounLemma is { } noun
                && nouns.Contains(noun, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the frame of the construction registered under the supplied pattern name.
        /// </summary>
        /// <param name="patternName">The name of the pattern.</param>
        /// <returns>The frame, or <see langword="null"/> when nothing is registered under that name.</returns>
        public ValencyFrame? GetFrame(string patternName) =>
            provider.GetConstruction(patternName)?.ToFrame();
    }
}
