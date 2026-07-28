using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Provides access to particle data.
    /// </summary>
    public interface IParticleDataProvider
    {
        /// <summary>
        /// Gets the Czech particles loaded from embedded JSON data, keyed by lemma.
        /// </summary>
        /// <returns>The loaded particle definitions.</returns>
        Dictionary<string, ParticleData> GetParticles();
    }
}
