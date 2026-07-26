using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Integration tests for reflexive particle (se/si) generation across all verb forms.
    /// Exercises both the imperative path in <see cref="CzechVerbConjugationService"/>
    /// and the indicative path in <see cref="CzechWordFormComposer"/>.
    /// </summary>
    [TestClass]
    public sealed class ReflexiveParticleTests
    {
        private static CzechWordFormComposer composer = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            composer = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })
                               .GetRequiredService<CzechWordFormComposer>();
        }

        #region Imperative

        /// <summary>
        /// Imperative forms with reflexive particles.
        /// The particle is appended inside <see cref="CzechVerbConjugationService"/> and
        /// surfaced verbatim by <see cref="CzechWordFormComposer.GetFullForm"/>.
        /// </summary>
        [DataTestMethod]
        // se — accusative types
        [DataRow("dělat",    "dělá",    "Second", "Singular", "ReflexivumTantum_Se",  "dělej se!",   DisplayName = "dělat imp 2sg – ReflexivumTantum_Se")]
        [DataRow("dělat",    "dělá",    "First",  "Plural",   "DerivedReflexive_Se",  "dělejme se!", DisplayName = "dělat imp 1pl – DerivedReflexive_Se")]
        [DataRow("dělat",    "dělá",    "Second", "Plural",   "Reciprocal_Se",        "dělejte se!", DisplayName = "dělat imp 2pl – Reciprocal_Se")]
        [DataRow("nést",     "nese",    "Second", "Singular", "DeagentivePassive_Se", "nes se!",     DisplayName = "nést imp 2sg – DeagentivePassive_Se")]
        [DataRow("tisknout", "trida2",  "Second", "Singular", "DerivedReflexive_Se",  "tiskni se!",  DisplayName = "tisknout imp 2sg – DerivedReflexive_Se")]
        // si — dative types
        [DataRow("dělat",    "dělá",    "Second", "Singular", "ReflexivumTantum_Si",  "dělej si!",   DisplayName = "dělat imp 2sg – ReflexivumTantum_Si")]
        [DataRow("dělat",    "dělá",    "First",  "Plural",   "DerivedBenefactive_Si","dělejme si!", DisplayName = "dělat imp 1pl – DerivedBenefactive_Si")]
        [DataRow("nést",     "nese",    "Second", "Plural",   "DerivedBenefactive_Si","neste si!",   DisplayName = "nést imp 2pl – DerivedBenefactive_Si")]
        public void GetFullForm_ImperativeWithReflexive_ReturnsCorrectPhrase(
            string lemma, string pattern, string person, string number,
            string reflexiveType, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Modus = Modus.Imperative,
                Voice = Voice.Active,
                Person = Enum.Parse<Person>(person),
                Number = Enum.Parse<Number>(number),
                ReflexiveType = Enum.Parse<ReflexiveType>(reflexiveType),
            };

            var result = composer.GetFullForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        #endregion Imperative

        #region Present tense

        /// <summary>
        /// Indicative present forms with reflexive particles.
        /// The particle is appended by <see cref="CzechWordFormComposer.GetFullForm"/>.
        /// </summary>
        [DataTestMethod]
        // se
        [DataRow("dělat",  "dělá",    "Third",  "Singular", "ReflexivumTantum_Se",  "dělá se",    DisplayName = "dělat přít. 3sg – ReflexivumTantum_Se")]
        [DataRow("dělat",  "dělá",    "Third",  "Plural",   "Reciprocal_Se",        "dělají se",  DisplayName = "dělat přít. 3pl – Reciprocal_Se")]
        [DataRow("prosit", "trida4",  "First",  "Singular", "DerivedReflexive_Se",  "prosím se",  DisplayName = "prosit přít. 1sg – DerivedReflexive_Se")]
        [DataRow("nést",   "nese",    "Second", "Singular", "DeagentivePassive_Se", "neseš se",   DisplayName = "nést přít. 2sg – DeagentivePassive_Se")]
        // si
        [DataRow("dělat",  "dělá",    "Third",  "Singular", "DerivedBenefactive_Si","dělá si",    DisplayName = "dělat přít. 3sg – DerivedBenefactive_Si")]
        [DataRow("dělat",  "dělá",    "First",  "Plural",   "ReflexivumTantum_Si",  "děláme si",  DisplayName = "dělat přít. 1pl – ReflexivumTantum_Si")]
        [DataRow("prosit", "trida4",  "Second", "Singular", "DerivedBenefactive_Si","prosíš si",  DisplayName = "prosit přít. 2sg – DerivedBenefactive_Si")]
        public void GetFullForm_PresentWithReflexive_ReturnsCorrectPhrase(
            string lemma, string pattern, string person, string number,
            string reflexiveType, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Tense = Tense.Present,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Person = Enum.Parse<Person>(person),
                Number = Enum.Parse<Number>(number),
                ReflexiveType = Enum.Parse<ReflexiveType>(reflexiveType),
            };

            var result = composer.GetFullForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        #endregion Present tense

        #region Past tense

        /// <summary>
        /// Past tense (l-participle) forms with reflexive particles.
        /// </summary>
        [DataTestMethod]
        // se
        [DataRow("dělat", "dělá",  "Masculine", "Singular", "ReflexivumTantum_Se",  "dělal se",  DisplayName = "dělat min. sg m – ReflexivumTantum_Se")]
        [DataRow("dělat", "dělá",  "Feminine",  "Singular", "DerivedReflexive_Se",  "dělala se", DisplayName = "dělat min. sg f – DerivedReflexive_Se")]
        [DataRow("dělat", "dělá",  "Masculine", "Plural",   "Reciprocal_Se",        "dělali se", DisplayName = "dělat min. pl m – Reciprocal_Se")]
        [DataRow("nést",  "nese",  "Masculine", "Plural",   "DeagentivePassive_Se", "nesli se",  DisplayName = "nést min. pl m – DeagentivePassive_Se")]
        // si
        [DataRow("dělat", "dělá",  "Masculine", "Singular", "DerivedBenefactive_Si","dělal si",  DisplayName = "dělat min. sg m – DerivedBenefactive_Si")]
        [DataRow("dělat", "dělá",  "Feminine",  "Singular", "ReflexivumTantum_Si",  "dělala si", DisplayName = "dělat min. sg f – ReflexivumTantum_Si")]
        [DataRow("nést",  "nese",  "Masculine", "Singular", "DerivedBenefactive_Si","nesl si",   DisplayName = "nést min. sg m – DerivedBenefactive_Si")]
        public void GetFullForm_PastWithReflexive_ReturnsCorrectPhrase(
            string lemma, string pattern, string gender, string number,
            string reflexiveType, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Tense = Tense.Past,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Person = Person.Third,
                Gender = Enum.Parse<Gender>(gender),
                Number = Enum.Parse<Number>(number),
                ReflexiveType = Enum.Parse<ReflexiveType>(reflexiveType),
            };

            var result = composer.GetFullForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        #endregion Past tense

        #region Clitic cluster

        /// <summary>
        /// Conditional forms with reflexive particles. The conditional particle is itself a clitic and
        /// outranks the reflexive in the cluster, so se/si follows it whichever side of the participle it sits on.
        /// </summary>
        [DataTestMethod]
        [DataRow(false, false, "ReflexivumTantum_Se", "dělal bych se", DisplayName = "kondicionál bez podmětu – dělal bych se")]
        [DataRow(true, false, "ReflexivumTantum_Se", "bych se dělal", DisplayName = "kondicionál s podmětem – bych se dělal")]
        [DataRow(false, false, "DerivedBenefactive_Si", "dělal bych si", DisplayName = "kondicionál bez podmětu – dělal bych si")]
        [DataRow(true, false, "DerivedBenefactive_Si", "bych si dělal", DisplayName = "kondicionál s podmětem – bych si dělal")]
        [DataRow(true, true, "ReflexivumTantum_Se", "bych se nedělal", DisplayName = "záporný kondicionál s podmětem – bych se nedělal")]
        [DataRow(false, true, "ReflexivumTantum_Se", "nedělal bych se", DisplayName = "záporný kondicionál bez podmětu – nedělal bych se")]
        public void GetFullForm_ConditionalWithReflexive_PlacesParticleAfterAuxiliary(
            bool explicitSubject, bool isNegative, string reflexiveType, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = "dělat",
                Pattern = "dělá",
                WordCategory = WordCategory.Verb,
                Modus = Modus.Conditional,
                Voice = Voice.Active,
                Person = Person.First,
                Number = Number.Singular,
                Gender = Gender.Masculine,
                IsNegative = isNegative,
                HasPrecedingConstituent = explicitSubject,
                ReflexiveType = Enum.Parse<ReflexiveType>(reflexiveType),
            };

            var result = composer.GetFullForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        /// <summary>
        /// Periphrastic future with reflexive particles. Budu carries stress, so it is a first constituent
        /// rather than part of the cluster: the particle follows it only when no subject is written out.
        /// </summary>
        [DataTestMethod]
        [DataRow(false, "ReflexivumTantum_Se", "budu se dělat", DisplayName = "futurum bez podmětu – budu se dělat")]
        [DataRow(true, "ReflexivumTantum_Se", "se budu dělat", DisplayName = "futurum s podmětem – se budu dělat")]
        [DataRow(false, "DerivedBenefactive_Si", "budu si dělat", DisplayName = "futurum bez podmětu – budu si dělat")]
        public void GetFullForm_FutureWithReflexive_PlacesParticleAfterFirstStressedWord(
            bool explicitSubject, string reflexiveType, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = "dělat",
                Pattern = "dělá",
                WordCategory = WordCategory.Verb,
                Tense = Tense.Future,
                Aspect = VerbAspect.Imperfective,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Person = Person.First,
                Number = Number.Singular,
                HasPrecedingConstituent = explicitSubject,
                ReflexiveType = Enum.Parse<ReflexiveType>(reflexiveType),
            };

            var result = composer.GetFullForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        #endregion Clitic cluster
    }
}
