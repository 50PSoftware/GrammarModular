using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Interfaces;

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
        /// Only the conditional particles for now. The past-tense auxiliaries jsem/jsi/jsme/jste belong to the
        /// same cluster rank, but BuildPastForm does not emit them yet; once it does they have to be added here.
        /// The future auxiliary budu/budeš/… is deliberately absent — it carries stress, so it counts as a
        /// first constituent rather than as part of the cluster.
        /// </remarks>
        public bool IsCliticAuxiliary(string word)
        {
            var conditional = dataProvider.GetParticles().Conditional;
            return conditional.Singular.Values.Contains(word)
                || conditional.Plural.Values.Contains(word);
        }
    }
}
