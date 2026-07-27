using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies adverb forms and their use as clause constituents.
    /// </summary>
    /// <remarks>
    /// NESČ classifies adverbs as uninflected, so the positive degree is the lemma and comparison is the
    /// only morphology under test here.
    /// </remarks>
    [TestClass]
    public sealed class AdverbTests
    {
        private static CzechWordFormComposer composer = null!;
        private static CzechSentenceBuilder builder = null!;
        private static ICzechAdverbService adverbs = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            composer = provider.GetRequiredService<CzechWordFormComposer>();
            builder = provider.GetRequiredService<CzechSentenceBuilder>();
            adverbs = provider.GetRequiredService<ICzechAdverbService>();
        }

        private static CzechWordRequest Adverb(string lemma, Degree? degree = null, bool? preferShort = null) => new()
        {
            Lemma = lemma,
            WordCategory = WordCategory.Adverb,
            Degree = degree,
            PrefersShortForm = preferShort
        };

        #region Forms

        /// <summary>
        /// The positive degree is the lemma, and an adverb that was never registered still passes through —
        /// an uninflected word needs no paradigm to be usable.
        /// </summary>
        /// <param name="lemma">The adverb lemma.</param>
        [DataTestMethod]
        [DataRow("rychle")]
        [DataRow("dobře")]
        [DataRow("dnes")]
        [DataRow("jak")]
        [DataRow("nesmyslně")]
        public void GetFullForm_PositiveDegree_ReturnsTheLemma(string lemma)
        {
            Assert.AreEqual(lemma, composer.GetFullForm(Adverb(lemma)).Form);
        }

        /// <summary>
        /// Regular and irregular comparatives alike come from the data, and the superlative is nej- on top.
        /// </summary>
        /// <param name="lemma">The adverb lemma.</param>
        /// <param name="comparative">The expected comparative.</param>
        /// <param name="superlative">The expected superlative.</param>
        [DataTestMethod]
        // regular -eji/-ěji
        [DataRow("rychle", "rychleji", "nejrychleji")]
        [DataRow("krásně", "krásněji", "nejkrásněji")]
        [DataRow("často", "častěji", "nejčastěji")]
        // palatalization before the suffix
        [DataRow("hladce", "hladčeji", "nejhladčeji")]
        [DataRow("hezky", "hezčeji", "nejhezčeji")]
        // suppletive and otherwise irregular
        [DataRow("dobře", "lépe", "nejlépe")]
        [DataRow("špatně", "hůře", "nejhůře")]
        [DataRow("brzy", "dříve", "nejdříve")]
        [DataRow("dlouho", "déle", "nejdéle")]
        [DataRow("málo", "méně", "nejméně")]
        public void GetFullForm_ComparedDegrees_UseTheRegisteredForms(string lemma, string comparative, string superlative)
        {
            Assert.AreEqual(comparative, composer.GetFullForm(Adverb(lemma, Degree.Comparative)).Form);
            Assert.AreEqual(superlative, composer.GetFullForm(Adverb(lemma, Degree.Superlative)).Form);
        }

        /// <summary>
        /// Several irregulars carry a doublet, the short form being the colloquial one. It is used only
        /// when asked for, and only where usage has one.
        /// </summary>
        /// <param name="lemma">The adverb lemma.</param>
        /// <param name="expected">The expected short comparative.</param>
        [DataTestMethod]
        [DataRow("dobře", "líp")]
        [DataRow("špatně", "hůř")]
        [DataRow("brzy", "dřív")]
        [DataRow("málo", "míň")]
        public void GetFullForm_ShortComparative_UsedOnlyWhenAskedFor(string lemma, string expected)
        {
            Assert.AreEqual(expected, composer.GetFullForm(Adverb(lemma, Degree.Comparative, preferShort: true)).Form);
        }

        /// <summary>
        /// An adverb without a doublet ignores the preference rather than inventing a short form.
        /// </summary>
        [TestMethod]
        public void GetFullForm_ShortComparativeWhereNoneExists_FallsBackToTheLongForm()
        {
            Assert.AreEqual("rychleji", composer.GetFullForm(Adverb("rychle", Degree.Comparative, preferShort: true)).Form);
        }

        #endregion Forms

        #region Comparison is data, not a rule

        /// <summary>
        /// Comparison is irregular often enough that deriving it would produce a plausible wrong word
        /// rather than a failure, so an unregistered comparative is reported.
        /// </summary>
        [TestMethod]
        public void GetFullForm_ComparativeOfUnregisteredAdverb_Throws()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => composer.GetFullForm(Adverb("nesmyslně", Degree.Comparative)));

            StringAssert.Contains(exception.Message, "adverbs.json");
        }

        /// <summary>
        /// An adverb that is not compared at all says so rather than producing "nejdnes".
        /// </summary>
        [TestMethod]
        public void GetFullForm_ComparativeOfNonGradableAdverb_Throws()
        {
            Assert.IsFalse(adverbs.IsComparable("dnes"));
            Assert.ThrowsException<InvalidOperationException>(
                () => composer.GetFullForm(Adverb("dnes", Degree.Comparative)));
        }

        /// <summary>
        /// The two members of a pair formed from one adjective are separate adverbs, and both are registered.
        /// </summary>
        /// <param name="first">The -o member.</param>
        /// <param name="second">The -e/-ě member.</param>
        /// <param name="comparative">The comparative they share.</param>
        [DataTestMethod]
        [DataRow("dlouho", "dlouze", "déle")]
        [DataRow("vysoko", "vysoce", "výše")]
        [DataRow("těžko", "těžce", "tíže")]
        [DataRow("široko", "široce", "šíře")]
        [DataRow("úzko", "úzce", "úže")]
        public void GetFullForm_BothMembersOfAPair_AreRegisteredAndShareTheComparative(
            string first, string second, string comparative)
        {
            Assert.AreEqual(first, composer.GetFullForm(Adverb(first)).Form);
            Assert.AreEqual(second, composer.GetFullForm(Adverb(second)).Form);
            Assert.AreEqual(comparative, composer.GetFullForm(Adverb(first, Degree.Comparative)).Form);
            Assert.AreEqual(comparative, composer.GetFullForm(Adverb(second, Degree.Comparative)).Form);
        }

        /// <summary>
        /// A comparative built a different way is an alternative, not a short variant of the primary —
        /// snadněji is not a clipping of snáze the way hůř is of hůře.
        /// </summary>
        /// <param name="lemma">The adverb lemma.</param>
        /// <param name="alternative">The competing comparative.</param>
        [DataTestMethod]
        [DataRow("snadno", "snadněji")]
        [DataRow("hluboko", "hloub")]
        [DataRow("široko", "šířeji")]
        [DataRow("úzko", "úžeji")]
        public void GetComparativeVariants_CompetingFormation_IsKeptAsAnAlternative(string lemma, string alternative)
        {
            CollectionAssert.Contains(adverbs.GetComparativeVariants(lemma).ToList(), alternative);
        }

        /// <summary>
        /// The clipped doublet and the competing formation are told apart: asking for the short form of
        /// hluboko gives the primary back, because hloub is not a clipping of hlouběji.
        /// </summary>
        [TestMethod]
        public void GetFullForm_CompetingFormationIsNotTreatedAsAShortVariant()
        {
            Assert.AreEqual("hlouběji", composer.GetFullForm(Adverb("hluboko", Degree.Comparative, preferShort: true)).Form);
            Assert.AreEqual("hůř", composer.GetFullForm(Adverb("špatně", Degree.Comparative, preferShort: true)).Form);
        }

        /// <summary>
        /// An unregistered or uncompared adverb has no variants rather than an exception — this is a query.
        /// </summary>
        [TestMethod]
        public void GetComparativeVariants_UnregisteredOrUncompared_IsEmpty()
        {
            Assert.AreEqual(0, adverbs.GetComparativeVariants("nesmyslně").Count);
            Assert.AreEqual(0, adverbs.GetComparativeVariants("dnes").Count);
        }

        #endregion Comparison is data, not a rule

        #region In a clause

        private static CzechWordRequest Verb(string lemma, string pattern, Person person, ReflexiveType reflexive = ReflexiveType.None) => new()
        {
            Lemma = lemma,
            Pattern = pattern,
            WordCategory = WordCategory.Verb,
            Modus = Modus.Indicative,
            Tense = Tense.Present,
            Aspect = VerbAspect.Imperfective,
            Voice = Voice.Active,
            Person = person,
            Number = Number.Singular,
            ReflexiveType = reflexive
        };

        /// <summary>
        /// An adverb of manner fills the MANN functor, which until now had nothing that could realize it.
        /// </summary>
        [TestMethod]
        public void Build_MannerAdverb_FillsTheFunctorThatHadNothingToRealizeIt()
        {
            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá", Person.Third),
                Elements = [ClauseElement.Of(Adverb("rychle"), FgdFunctor.MANN, InformationStatus.New)]
            };

            Assert.AreEqual("Dělá rychle.", builder.Build(clause));
        }

        /// <summary>
        /// The plan that introduced interrogative focus could not build its own example, because the
        /// wh-word is an adverb. It builds now.
        /// </summary>
        [TestMethod]
        public void Build_InterrogativeAdverb_ProducesTheWhQuestionTheSentenceTypePlanCouldNot()
        {
            var clause = new CzechClause
            {
                SentenceType = SentenceType.Interrogative,
                Predicate = Verb("mít", "mít", Person.Second, ReflexiveType.ReflexivumTantum_Se),
                Elements = [ClauseElement.Of(Adverb("jak"), FgdFunctor.MANN, InformationStatus.Interrogative)],
                Terminator = "?"
            };

            Assert.AreEqual("Jak se máš?", builder.Build(clause));
        }

        /// <summary>
        /// A temporal adverb no longer has to be faked as a noun in the nominative.
        /// </summary>
        [TestMethod]
        public void Build_TemporalAdverb_NeedsNoNounWorkaround()
        {
            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá", Person.Third, ReflexiveType.ReflexivumTantum_Se),
                Elements = [ClauseElement.Of(Adverb("dnes"), FgdFunctor.TWHEN, InformationStatus.Given)]
            };

            Assert.AreEqual("Dnes se dělá.", builder.Build(clause));
        }

        #endregion In a clause
    }
}
