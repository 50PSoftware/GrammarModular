using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides Czech clitic and reflexive particle lookup operations.
    /// </summary>
    public class CzechCliticService : ICzechCliticService
    {
        private readonly ICliticDataProvider dataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechCliticService"/> type.
        /// </summary>
        public CzechCliticService(ICliticDataProvider dataProvider)
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

            var conditional = dataProvider.GetClitics().Conditional;
            var section = number == Number.Singular ? conditional.Singular : conditional.Plural;

            // Klíče v clitics.json jsou názvy členů výčtu (First/Second/Third), ne jejich číselné
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

            var reflexive = dataProvider.GetClitics().Reflexive;
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
            var clitics = dataProvider.GetClitics();
            return Contains(clitics.Conditional, word) || Contains(clitics.PastAuxiliary, word);
        }

        /// <summary>
        /// Determines whether the supplied word is a conditional particle.
        /// </summary>
        /// <param name="word">The single word to classify.</param>
        /// <returns><see langword="true"/> for bych, bys, by, bychom and byste; otherwise, <see langword="false"/>.</returns>
        public bool IsConditionalParticle(string word)
            => Contains(dataProvider.GetClitics().Conditional, word);

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

            var pastAuxiliary = dataProvider.GetClitics().PastAuxiliary;
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
            var pastSecond = GetPastAuxiliary(Number.Singular, Person.Second);
            var conditionalSecond = GetConditionalParticle(Number.Singular, Person.Second);

            var index = clitics.ToList().FindIndex(word => word == pastSecond || word == conditionalSecond);

            if (index < 0 || index + 1 >= clitics.Count)
            {
                return clitics;
            }

            var reflexive = dataProvider.GetClitics().Reflexive;
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

            // The conditional keeps its particle and only sheds the -s onto the reflexive: by ses, by sis.
            // The past auxiliary disappears into it entirely: ses, sis.
            if (clitics[index] == conditionalSecond)
            {
                result.Insert(index, GetConditionalParticle(Number.Singular, Person.Third));
            }

            return result;
        }

        private static bool Contains(PersonClitics clitics, string word) =>
            clitics.Singular.Values.Contains(word) || clitics.Plural.Values.Contains(word);
    }
}
