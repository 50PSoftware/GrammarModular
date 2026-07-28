using Grammar.Core.Enums;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines lookup operations for the Czech word class <em>částice</em>.
    /// </summary>
    /// <remarks>
    /// Lookup only. A particle is uninflected and is not compared, so unlike the other word classes there is
    /// no form to build — the lemma is the word. What the service answers is what the particle does and
    /// where it may stand. For the clitics bych, jsem, se and si, see <see cref="ICzechCliticService"/>.
    /// </remarks>
    public interface ICzechParticleService
    {
        /// <summary>
        /// Determines whether the supplied lemma is registered as a particle.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> when the lemma is in the inventory; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// A false answer means the word is not registered here, not that it cannot be a particle. The same
        /// word may also be an adverb or a conjunction, so this is one reading among several rather than a
        /// classification of the word.
        /// </remarks>
        bool IsParticle(string lemma);

        /// <summary>
        /// Gets the function the supplied particle performs.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns>The particle type.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the lemma is not in the inventory.</exception>
        ParticleType GetParticleType(string lemma);

        /// <summary>
        /// Determines whether the particle opens the clause it belongs to.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> for ať, kéž, nechť and the structuring openers; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown when the lemma is not in the inventory.</exception>
        bool IsClauseInitial(string lemma);


        /// <summary>
        /// Determines whether the particle may stand in the rheme.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="false"/> for a modifying particle; otherwise, <see langword="true"/>.</returns>
        /// <remarks>
        /// The one positional restriction NESČ states outright — <em>modifikační částice v rématu stát
        /// nemohou</em>. A modifying particle bears no stress of its own, so it cannot be what the utterance
        /// is about. Derived from the type rather than recorded per word, because the source states it of the
        /// group.
        /// </remarks>
        bool CanStandInRheme(string lemma);

        /// <summary>
        /// Gets every registered particle of the supplied type.
        /// </summary>
        /// <param name="type">The particle type to select.</param>
        /// <returns>The lemmas registered under that type.</returns>
        IReadOnlyList<string> GetParticlesOfType(ParticleType type);
    }
}
