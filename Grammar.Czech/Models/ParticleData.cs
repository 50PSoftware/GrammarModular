using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents Czech particle metadata loaded from JSON data.
    /// </summary>
    /// <remarks>
    /// The word class <em>částice</em>, not the clitics in <see cref="CliticsData"/>. A particle is
    /// uninflected and has no clause-member status, so there is no paradigm here and nothing to derive: the
    /// entry records what the word does, not what forms it takes.
    /// <para>
    /// Homonymy with adverbs and conjunctions is normal rather than a fault in the data, and NESČ treats it
    /// as one of the defining difficulties of the class. klidně is a particle in "Klidně seď" and an adverb
    /// in "Seď klidně, nevrť se"; tedy and bohužel are registered here and in the conjunction and adverb
    /// files respectively; ať is here and among the conjunctions. Each service is an independent lookup keyed
    /// by the lemma, so a word standing in several of them is exactly right and must not be "fixed".
    /// </para>
    /// </remarks>
    public sealed record ParticleData
    {
        /// <summary>
        /// Gets the function the particle performs.
        /// </summary>
        public ParticleType Type { get; init; }

        /// <summary>
        /// Gets a value indicating whether the particle opens the clause.
        /// </summary>
        /// <remarks>
        /// True for the optative ať, kéž, nechť and for the structuring openers nuže, inu. Most particles
        /// are placed freely and take whatever position their scope calls for, so this is the exception
        /// rather than a category.
        /// </remarks>
        public bool IsClauseInitial { get; init; }

        /// <summary>
        /// Gets the mood the particle calls for in the clause it opens, or null when it calls for none.
        /// </summary>
        /// <remarks>
        /// Recorded rather than ruled. "Kéž by přišel" takes the conditional and "Ať přijde" the imperative
        /// or a plain present, but NESČ does not state the government outright — it only observes that the
        /// optative group shades into the conjunctions — so this is what the attested usage shows for each
        /// word, not a generalization over the group.
        /// </remarks>
        public Modus? RequiresModus { get; init; }
    }
}
