using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides lookup for the Czech word class <em>částice</em>: what a particle does and where it stands.
    /// </summary>
    /// <remarks>
    /// There is no morphology here and there never will be. A particle is uninflected and is not compared,
    /// so the lemma is the form — the whole service is the inventory and what it records about each entry.
    /// <para>
    /// The inventory is open at the edges rather than closed. NESČ carries three competing classifications
    /// of the class and observes that its boundaries with the adverbs and the conjunctions are drawn by
    /// function in context, not by the word: klidně is a particle in "Klidně seď" and an adverb in "Seď
    /// klidně, nevrť se". So an unregistered lemma is not rejected the way an unknown conjunction is — it
    /// simply is not a particle as far as this data goes, which <see cref="IsParticle"/> reports and the
    /// typed lookups treat as a caller error.
    /// </para>
    /// </remarks>
    public class CzechParticleService : ICzechParticleService
    {
        private readonly Dictionary<string, ParticleData> _particles;
        private readonly Lazy<ILookup<ParticleType, string>> _byType;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechParticleService"/> type.
        /// </summary>
        public CzechParticleService(IParticleDataProvider dataProvider)
        {
            _particles = dataProvider.GetParticles();

            _byType = new Lazy<ILookup<ParticleType, string>>(
                () => _particles.ToLookup(entry => entry.Value.Type, entry => entry.Key),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Determines whether the supplied lemma is registered as a particle.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> when the lemma is in the inventory; otherwise, <see langword="false"/>.</returns>
        public bool IsParticle(string lemma) => _particles.ContainsKey(lemma);

        /// <summary>
        /// Gets the function the supplied particle performs.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns>The particle type.</returns>
        public ParticleType GetParticleType(string lemma) => Lookup(lemma).Type;

        /// <summary>
        /// Determines whether the particle opens the clause it belongs to.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> for ať, kéž, nechť and the structuring openers; otherwise, <see langword="false"/>.</returns>
        public bool IsClauseInitial(string lemma) => Lookup(lemma).IsClauseInitial;


        /// <summary>
        /// Determines whether the particle may stand in the rheme.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="false"/> for a modifying particle; otherwise, <see langword="true"/>.</returns>
        public bool CanStandInRheme(string lemma) => Lookup(lemma).Type != ParticleType.Modifying;

        /// <summary>
        /// Gets every registered particle of the supplied type.
        /// </summary>
        /// <param name="type">The particle type to select.</param>
        /// <returns>The lemmas registered under that type.</returns>
        public IReadOnlyList<string> GetParticlesOfType(ParticleType type) => _byType.Value[type].ToList();

        // Asking what an unregistered word does is a different mistake from asking whether it is a particle
        // at all, so the typed lookups report it and IsParticle answers plainly.
        private ParticleData Lookup(string lemma)
        {
            if (_particles.TryGetValue(lemma, out var data))
            {
                return data;
            }

            throw new InvalidOperationException(
                $"'{lemma}' není v seznamu částic. Ověř to přes IsParticle, nebo ji doplň do particles.json.");
        }
    }
}
