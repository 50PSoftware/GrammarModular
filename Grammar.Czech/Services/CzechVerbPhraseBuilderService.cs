using Grammar.Core.Enums;
using Grammar.Czech.Enums;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides czech verb phrase builder operations.
    /// </summary>
    public class CzechVerbPhraseBuilderService
    {
        private readonly CzechAuxiliaryVerbService auxVerbService;
        private readonly CzechParticleService particleService;
        private readonly CzechPrefixService prefixService;

        private string BuildConditionalAuxiliary(string verbForm, Number? number, Person? person, bool hasPrecedingConstituent, bool isNegative)
        {
            var particle = particleService.GetConditionalParticle(number, person);
            var negation = isNegative ? prefixService.GetNegativePrefix() : string.Empty;
            return hasPrecedingConstituent ? $"{particle} {negation}{verbForm}" : $"{negation}{verbForm} {particle}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechVerbPhraseBuilderService"/> type.
        /// </summary>
        public CzechVerbPhraseBuilderService(CzechAuxiliaryVerbService auxiliaryService, CzechParticleService particleService, CzechPrefixService prefixService)
        {
            this.auxVerbService = auxiliaryService;
            this.particleService = particleService;
            this.prefixService = prefixService;
        }

        /// <summary>
        /// Builds a Czech conditional verb phrase from a base participle and conditional particle.
        /// </summary>
        /// <param name="verbForm">The finite or participial verb form to combine into a phrase.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <param name="hasPrecedingConstituent">True when some constituent already occupies first position in the clause.</param>
        /// <param name="isNegative">True when the generated phrase should be negated; otherwise, false.</param>
        /// <returns>The assembled conditional verb phrase.</returns>
        public string BuildConditionalPhrase(string verbForm, Number? number, Person? person, bool hasPrecedingConstituent, bool isNegative)
        {
            return BuildConditionalAuxiliary(verbForm, number, person, hasPrecedingConstituent, isNegative);
        }

        /// <summary>
        /// Builds a Czech compound past verb phrase from an l-participle and the auxiliary.
        /// </summary>
        /// <param name="verbForm">The l-participle to combine into a phrase.</param>
        /// <param name="number">The requested grammatical number.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <param name="hasPrecedingConstituent">True when some constituent already occupies first position in the clause.</param>
        /// <param name="isNegative">True when the generated phrase should be negated; otherwise, false.</param>
        /// <returns>The assembled past verb phrase.</returns>
        /// <remarks>
        /// The third person takes no auxiliary, which leaves the bare participle. Negation attaches to the
        /// participle, never to the auxiliary: nedělal jsem, not nejsem dělal.
        /// </remarks>
        public string BuildPastPhrase(string verbForm, Number? number, Person? person, bool hasPrecedingConstituent, bool isNegative)
        {
            var negation = isNegative ? prefixService.GetNegativePrefix() : string.Empty;
            var auxiliary = particleService.GetPastAuxiliary(number, person);

            if (auxiliary is null)
            {
                return $"{negation}{verbForm}";
            }

            return hasPrecedingConstituent
                ? $"{auxiliary} {negation}{verbForm}"
                : $"{negation}{verbForm} {auxiliary}";
        }

        /// <summary>
        /// Builds a Czech passive conditional verb phrase.
        /// </summary>
        /// <param name="verbForm">The finite or participial verb form to combine into a phrase.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <param name="modus">The requested grammatical mood.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        /// <param name="isNegative">True when the generated phrase should be negated; otherwise, false.</param>
        /// <returns>The assembled passive conditional verb phrase.</returns>
        public string BuildPassiveConditionalPhrase(string verbForm, Number? number, Person? person, Modus? modus, Gender? gender, bool isNegative)
        {
            var beForm = auxVerbService.GetBeForm(Tense.Past, number, person, modus, gender, isNegative);
            verbForm = BuildConditionalAuxiliary(verbForm, number, person, true, false);
            return $"{beForm} {verbForm}";
        }

        /// <summary>
        /// Builds a Czech passive verb phrase with the appropriate auxiliary.
        /// </summary>
        /// <param name="verbForm">The finite or participial verb form to combine into a phrase.</param>
        /// <param name="tense">The requested grammatical tense.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <param name="modus">The requested grammatical mood.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        /// <param name="isNegative">True when the generated phrase should be negated; otherwise, false.</param>
        /// <returns>The assembled passive verb phrase.</returns>
        public string BuildPassivePhrase(string verbForm, Tense? tense, Number? number, Person? person, Modus? modus, Gender? gender, bool isNegative)
        {
            var beForm = auxVerbService.GetBeForm(tense, number, person, modus, gender, isNegative);
            return $"{beForm} {verbForm}";
        }

        /// <summary>
        /// Places the Czech reflexive particle inside a verb phrase according to its position in the clitic cluster.
        /// </summary>
        /// <param name="verbForm">The finite or participial verb form to combine into a phrase.</param>
        /// <param name="reflexiveType">The reflexive type that determines whether se or si is placed.</param>
        /// <param name="hasPrecedingConstituent">True when some constituent already occupies first position in the clause.</param>
        /// <returns>The verb phrase with the reflexive particle in place.</returns>
        /// <remarks>
        /// se/si is a second-position (Wackernagel) clitic:
        /// <list type="bullet">
        /// <item>A clitic auxiliary outranks the reflexive in the cluster, so the particle goes straight after it,
        /// wherever it sits: bych se myl, myl bych se.</item>
        /// <item>Otherwise the particle follows the first stressed word — the preceding constituent when the
        /// clause has one, the first word of the phrase when it does not: Já se budu bát, Budu se bát.</item>
        /// </list>
        /// The caller owns the first-position question, because the verb phrase cannot see the clause.
        /// <see cref="CzechSentenceBuilder"/> answers it; a caller that assembles a clause by hand must set the
        /// flag from whether anything precedes the verb, not from whether a subject exists.
        /// Short pronouns (mi, ti, ho, ji) are cluster members too and are not handled yet.
        /// </remarks>
        public string BuildReflexivePhrase(string verbForm, ReflexiveType reflexiveType, bool hasPrecedingConstituent)
        {
            var reflexive = particleService.GetReflexive(reflexiveType);
            var words = verbForm.Split(' ');

            for (int index = 0; index < words.Length; index++)
            {
                if (particleService.IsCliticAuxiliary(words[index]))
                {
                    return Join(words[..(index + 1)].Append(reflexive).Concat(words[(index + 1)..]));
                }
            }

            return hasPrecedingConstituent
                ? $"{reflexive} {verbForm}"
                : Join(words[..1].Append(reflexive).Concat(words[1..]));
        }

        // jsi + se → ses, jsi + si → sis.
        private string Join(IEnumerable<string> words) =>
            string.Join(' ', particleService.ContractCluster(words.ToList()));

        /// <summary>
        /// Builds the periphrastic future phrase for imperfective Czech verbs.
        /// </summary>
        /// <param name="verbForm">The finite or participial verb form to combine into a phrase.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <param name="modus">The requested grammatical mood.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        /// <param name="isNegative">True when the generated phrase should be negated; otherwise, false.</param>
        /// <returns>The assembled synthetic future phrase.</returns>
        public string BuildSynteticFuturePhrase(string verbForm, Number? number, Person? person, Modus? modus, Gender? gender, bool isNegative)
        {
            var beForm = auxVerbService.GetBeForm(Tense.Future, number, person, modus, gender, isNegative);
            return $"{beForm} {verbForm}";
        }
    }
}
