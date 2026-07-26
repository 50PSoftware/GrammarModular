using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides Czech clitic and reflexive particle lookup operations.
    /// </summary>
    public class CzechParticleService : ICzechParticleService
    {
        private readonly IParticleDataProvider dataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechParticleService"/> type.
        /// </summary>
        public CzechParticleService(IParticleDataProvider dataProvider)
        {
            this.dataProvider = dataProvider;
        }

        /// <summary>
        /// Gets the conditional particle for the requested grammatical number and person.
        /// </summary>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <returns>The matching conditional particle.</returns>
        public string GetConditionalParticle(Number? number, Person? person)
        {
            if (number == null || person == null)
            {
                throw new ArgumentNullException();
            }

            var conditional = dataProvider.GetParticles().Conditional;
            var section = number == Number.Singular ? conditional.Singular : conditional.Plural;

            // Klíče v particles.json jsou názvy členů výčtu (First/Second/Third), ne jejich číselné
            // hodnoty — dictionary klíče se nepřejmenovávají, PropertyNamingPolicy na ně nedopadá.
            return section[person.Value.ToString()];
        }

        /// <summary>
        /// Gets the reflexive particle for the supplied reflexive type.
        /// </summary>
        /// <param name="reflexiveType">The reflexive type that determines whether se or si is returned.</param>
        /// <returns>The reflexive particle for the requested type.</returns>
        public string GetReflexive(ReflexiveType reflexiveType)
        {
            if (reflexiveType == ReflexiveType.None)
                throw new ArgumentOutOfRangeException(nameof(reflexiveType));

            var reflexive = dataProvider.GetParticles().Reflexive;
            return reflexiveType is ReflexiveType.ReflexivumTantum_Si or ReflexiveType.DerivedBenefactive_Si
                ? reflexive.Dative
                : reflexive.Accusative;
        }

        /// <summary>
        /// Determines whether the supplied word is a clitic auxiliary that precedes the reflexive particle
        /// inside the Wackernagel clitic cluster.
        /// </summary>
        /// <param name="word">The single word to classify.</param>
        /// <returns><see langword="true"/> when the word is a clitic auxiliary; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Conditional particles and past-tense auxiliaries — both sit at rank 2, ahead of the reflexive.
        /// The future auxiliary budu/budeš/… is deliberately absent: it carries stress, so it counts as a
        /// first constituent rather than as part of the cluster.
        /// </remarks>
        public bool IsCliticAuxiliary(string word)
        {
            var particles = dataProvider.GetParticles();
            return Contains(particles.Conditional, word) || Contains(particles.PastAuxiliary, word);
        }

        /// <summary>
        /// Gets the past-tense auxiliary for the requested grammatical number and person.
        /// </summary>
        /// <param name="number">The requested grammatical number.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <returns>The auxiliary form, or <see langword="null"/> in the third person, which takes none.</returns>
        public string? GetPastAuxiliary(Number? number, Person? person)
        {
            if (number is null || person is null)
            {
                return null;
            }

            var pastAuxiliary = dataProvider.GetParticles().PastAuxiliary;
            var section = number == Number.Singular ? pastAuxiliary.Singular : pastAuxiliary.Plural;

            // Third person is absent from the data on purpose: the Czech past has no auxiliary there.
            return section.TryGetValue(person.Value.ToString(), out var auxiliary) ? auxiliary : null;
        }

        /// <summary>
        /// Applies the contractions that fuse the second-person auxiliary with a following reflexive.
        /// </summary>
        /// <param name="clitics">The clitic cluster in its canonical order.</param>
        /// <returns>The cluster with jsi se and jsi si contracted to ses and sis.</returns>
        /// <remarks>
        /// Applied to the assembled cluster rather than while building it, so that the two tokens are still
        /// separable while the cluster is being moved into second position.
        /// </remarks>
        public IReadOnlyList<string> ContractCluster(IReadOnlyList<string> clitics)
        {
            var secondPersonSingular = GetPastAuxiliary(Number.Singular, Person.Second);
            var index = clitics.ToList().IndexOf(secondPersonSingular!);

            if (secondPersonSingular is null || index < 0 || index + 1 >= clitics.Count)
            {
                return clitics;
            }

            var reflexive = dataProvider.GetParticles().Reflexive;
            var following = clitics[index + 1];

            var contracted = following == reflexive.Accusative ? "ses"
                : following == reflexive.Dative ? "sis"
                : null;

            if (contracted is null)
            {
                return clitics;
            }

            var result = clitics.ToList();
            result[index] = contracted;
            result.RemoveAt(index + 1);
            return result;
        }

        private static bool Contains(PersonParticles particles, string word) =>
            particles.Singular.Values.Contains(word) || particles.Plural.Values.Contains(word);
    }
}
