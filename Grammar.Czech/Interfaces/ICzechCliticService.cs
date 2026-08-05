using Grammar.Core.Enums;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for selecting Czech clitic and reflexive particles.
    /// </summary>
    public interface ICzechCliticService
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
        /// Gets the past-tense auxiliary for the requested grammatical number and person.
        /// </summary>
        /// <param name="number">The requested grammatical number.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <returns>The auxiliary form, or <see langword="null"/> in the third person, which takes none.</returns>
        string? GetPastAuxiliary(Number? number, Person? person);

        /// <summary>
        /// Determines whether the supplied word is a clitic auxiliary that precedes the reflexive particle
        /// inside the Wackernagel clitic cluster.
        /// </summary>
        /// <param name="word">The single word to classify.</param>
        /// <returns><see langword="true"/> when the word is a clitic auxiliary; otherwise, <see langword="false"/>.</returns>
        bool IsCliticAuxiliary(string word);

        /// <summary>
        /// Determines whether the supplied word is a conditional particle.
        /// </summary>
        /// <param name="word">The single word to classify.</param>
        /// <returns><see langword="true"/> for bych, bys, by, bychom and byste; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Narrower than <see cref="IsCliticAuxiliary"/>, which also covers the past-tense auxiliary. The
        /// distinction matters where aby or kdyby has absorbed the conditional auxiliary and the cluster must
        /// not emit it a second time.
        /// </remarks>
        bool IsConditionalParticle(string word);

        /// <summary>
        /// Applies the contractions that fuse the second-person auxiliary with a following reflexive.
        /// </summary>
        /// <param name="clitics">The clitic cluster in its canonical order.</param>
        /// <returns>The cluster with jsi se and jsi si contracted to ses and sis.</returns>
        IReadOnlyList<string> ContractCluster(IReadOnlyList<string> clitics);
    }
}
