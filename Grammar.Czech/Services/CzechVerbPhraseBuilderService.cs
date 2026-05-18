using Grammar.Core.Enums;

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

        private string BuildConditionalAuxiliary(string verbForm, Number? number, Person? person, bool explicitSubject, bool isNegative)
        {
            var particle = particleService.GetConditionalParticle(number, person);
            var negation = isNegative ? prefixService.GetNegativePrefix() : string.Empty;
            return explicitSubject ? $"{particle} {negation}{verbForm}" : $"{negation}{verbForm} {particle}";
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
        /// <param name="explicitSubject">The optional explicit subject to place before the verb phrase.</param>
        /// <param name="isNegative">True when the generated phrase should be negated; otherwise, false.</param>
        /// <returns>The assembled conditional verb phrase.</returns>
        public string BuildConditionalPhrase(string verbForm, Number? number, Person? person, bool explicitSubject, bool isNegative)
        {
            return BuildConditionalAuxiliary(verbForm, number, person, explicitSubject, isNegative);
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
        /// Adds the appropriate Czech reflexive particle to a verb phrase.
        /// </summary>
        /// <param name="verbForm">The finite or participial verb form to combine into a phrase.</param>
        /// <param name="reflexiveType">The reflexive type that determines whether se or si is appended.</param>
        /// <returns>The verb phrase with the reflexive particle appended.</returns>
        public string BuildReflexivePhrase(string verbForm, ReflexiveType reflexiveType)
        {
            var reflexive = particleService.GetReflexive(reflexiveType);
            return $"{verbForm} {reflexive}";
        }

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
