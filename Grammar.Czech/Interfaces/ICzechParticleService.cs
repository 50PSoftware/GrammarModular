using Grammar.Core.Enums;
using Grammar.Czech.Enums;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for selecting Czech clitic and reflexive particles.
    /// </summary>
    public interface ICzechParticleService
    {
        /// <summary>
        /// Gets the conditional particle for the requested grammatical number and person.
        /// </summary>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <returns>The matching conditional particle.</returns>
        string GetConditionalParticle(Number? number, Person? person);

        /// <summary>
        /// Gets the reflexive particle for the supplied reflexive type.
        /// </summary>
        /// <param name="reflexiveType">The reflexive type that determines whether se or si is returned.</param>
        /// <returns>The reflexive particle for the requested type.</returns>
        string GetReflexive(ReflexiveType reflexiveType);

        /// <summary>
        /// Determines whether the supplied word is a clitic auxiliary that precedes the reflexive particle
        /// inside the Wackernagel clitic cluster.
        /// </summary>
        /// <param name="word">The single word to classify.</param>
        /// <returns><see langword="true"/> when the word is a clitic auxiliary; otherwise, <see langword="false"/>.</returns>
        bool IsCliticAuxiliary(string word);
    }
}
