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
            return section[((int)person).ToString()];
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
    }
}
