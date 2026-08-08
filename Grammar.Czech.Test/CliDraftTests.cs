using Grammar.Core.Enums;
using Grammar.Czech.Cli;
using Grammar.Czech.Cli.Interaction;
using Grammar.Czech.Cli.Rendering;
using Grammar.Czech.Cli.Sentence;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies what the client application makes of a bare list of lemmas: which word becomes the
    /// predicate, what role each of the others gets, and what it refuses to decide on its own.
    /// </summary>
    [TestClass]
    public sealed class CliDraftTests
    {
        private static IServiceProvider services = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var collection = new ServiceCollection();

            collection.AddCzechGrammarServices();
            collection.AddSingleton<LemmaGuess>();
            collection.AddSingleton<DraftBuilder>();
            collection.AddSingleton<DraftView>();
            collection.AddSingleton<SentenceComposer>();

            services = collection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        }

        private static ClauseDraft Draft(DraftOverrides? overrides, params string[] lemmas) =>
            services.GetRequiredService<DraftBuilder>().Build(lemmas, overrides ?? new DraftOverrides());

        private static string Sentence(DraftOverrides? overrides, params string[] lemmas) =>
            services.GetRequiredService<SentenceComposer>().Compose(Draft(overrides, lemmas));

        /// <summary>
        /// Verifies that the verb becomes the predicate wherever it stands among the lemmas.
        /// </summary>
        [TestMethod]
        public void PredicateIsTheVerbWhereverItStands()
        {
            Assert.AreEqual("číst", Draft(null, "student", "číst", "kniha").PredicateLemma);
            Assert.AreEqual("číst", Draft(null, "číst", "student", "kniha").PredicateLemma);
        }

        /// <summary>
        /// Verifies that the valency frame assigns the roles and governs the cases.
        /// </summary>
        [TestMethod]
        public void FrameAssignsRolesAndCases()
        {
            var draft = Draft(null, "student", "číst", "kniha");

            Assert.AreEqual(FgdFunctor.ACT, draft.Constituents[0].Functor);
            Assert.AreEqual(Case.Nominative, draft.Constituents[0].EffectiveCase);
            Assert.AreEqual(FgdFunctor.PAT, draft.Constituents[1].Functor);
            Assert.AreEqual(Case.Accusative, draft.Constituents[1].EffectiveCase);

            // Pád si request nenese — plyne z rámce a doplňuje ho až builder.
            Assert.IsNull(draft.Constituents[1].Word.Case);
        }

        /// <summary>
        /// Verifies that an animate noun is preferred for the addressee, which is what keeps the two
        /// objects of a transfer verb apart.
        /// </summary>
        [TestMethod]
        public void AnimateNounBecomesTheAddressee()
        {
            Assert.AreEqual("Klára dává ženě knihu.", Sentence(null, "Klára", "dávat", "žena", "kniha"));
        }

        /// <summary>
        /// Verifies that a stated role overrules the one the frame would have assigned.
        /// </summary>
        [TestMethod]
        public void StatedRoleWins()
        {
            var overrides = new DraftOverrides();
            overrides.For("kniha").Functor = FgdFunctor.ACT;

            Assert.AreEqual(FgdFunctor.ACT, Draft(overrides, "student", "číst", "kniha").Constituents[1].Functor);
        }

        /// <summary>
        /// Verifies that a lemma the lexicon does not hold is inferred from its ending and reported as
        /// inferred rather than passed off as known.
        /// </summary>
        [TestMethod]
        public void UnknownLemmaIsInferredAndReported()
        {
            var draft = Draft(null, "Klára", "číst", "kniha");

            Assert.AreEqual(MetadataOrigin.Guess, draft.Constituents[0].Origin);
            Assert.AreEqual("žena", draft.Constituents[0].Word.Pattern);
            Assert.AreEqual(Gender.Feminine, draft.Constituents[0].Word.Gender);
            Assert.IsTrue(draft.Notes.Any(note => note.Contains("Klára")));
        }

        /// <summary>
        /// Verifies that a stated pattern beats both the lexicon and the inference.
        /// </summary>
        [TestMethod]
        public void StatedPatternWins()
        {
            var overrides = new DraftOverrides();
            overrides.For("Ivana").Pattern = "žena";
            overrides.For("Ivana").Gender = Gender.Feminine;

            var draft = Draft(overrides, "Ivana", "číst", "kniha");

            Assert.AreEqual(MetadataOrigin.User, draft.Constituents[0].Origin);
            Assert.AreEqual("žena", draft.Constituents[0].Word.Pattern);
        }

        /// <summary>
        /// Verifies that a preposition standing before a noun becomes that constituent's preposition and
        /// takes the case it governs, and that its semantic group names the free modification.
        /// </summary>
        [TestMethod]
        public void PrepositionOpensTheConstituentAndGovernsIt()
        {
            var draft = Draft(null, "student", "číst", "kniha", "u", "les");
            var adjunct = draft.Constituents[^1];

            Assert.AreEqual("u", adjunct.EffectivePreposition);
            Assert.AreEqual(Case.Genitive, adjunct.EffectiveCase);
            Assert.AreEqual(FgdFunctor.LOC, adjunct.Functor);
        }

        /// <summary>
        /// Verifies that an adjective in front of a noun becomes its agreeing attribute.
        /// </summary>
        [TestMethod]
        public void AdjectiveBecomesAnAttribute()
        {
            Assert.AreEqual("Mladý student čte knihu.", Sentence(null, "mladý", "student", "číst", "kniha"));
        }

        /// <summary>
        /// Verifies that a verb whose senses the dictionary does not rank is left undecided rather than
        /// picked for the user.
        /// </summary>
        [TestMethod]
        public void AmbiguousFrameIsAQuestionRatherThanAChoice()
        {
            var draft = Draft(null, "student", "jít");

            Assert.IsNull(draft.Frame);
            Assert.IsTrue(draft.Gaps().Any(gap => gap.Contains("motion")));

            var overrides = new DraftOverrides { FrameLabel = "motion" };

            Assert.AreEqual(0, Draft(overrides, "student", "jít").Gaps().Count);
        }

        /// <summary>
        /// Verifies that a constituent no frame accounts for and no preposition explains is reported as
        /// an open question instead of being given a role at random.
        /// </summary>
        [TestMethod]
        public void UnexplainedConstituentIsReportedAsAGap()
        {
            var draft = Draft(null, "student", "číst", "kniha", "den");

            Assert.IsNull(draft.Constituents[^1].Functor);
            Assert.IsTrue(draft.Gaps().Any(gap => gap.Contains("den")));
        }

        /// <summary>
        /// Verifies that a clause with no verb in it is refused with an explanation.
        /// </summary>
        [TestMethod]
        public void ClauseWithoutAVerbIsRefused()
        {
            var failure = Assert.ThrowsException<CliException>(() => Draft(null, "student", "kniha"));

            Assert.IsTrue(failure.Message.Contains("sloveso"));
        }

        /// <summary>
        /// Verifies that the predicate's categories are taken from the switches.
        /// </summary>
        [TestMethod]
        public void PredicateCategoriesComeFromTheSwitches()
        {
            var overrides = new DraftOverrides { Tense = Tense.Past, IsNegative = true };

            Assert.AreEqual("Student nečetl knihu.", Sentence(overrides, "student", "číst", "kniha"));
        }

        /// <summary>
        /// Verifies that a pronoun is recognized from the rule data rather than declined as a noun, and
        /// that the tool does not report a closed class as a word the dictionary is missing.
        /// </summary>
        [TestMethod]
        public void PronounComesFromTheRulesRatherThanTheGuess()
        {
            var draft = Draft(null, "já", "číst", "kniha");

            Assert.AreEqual(MetadataOrigin.Rules, draft.Constituents[0].Origin);
            Assert.IsFalse(draft.Notes.Any(note => note.Contains("já")));
            Assert.AreEqual("Já čtu knihu.", Sentence(null, "já", "číst", "kniha"));
        }

        /// <summary>
        /// Verifies that the subject pronoun is kept unless dropping it is asked for — the tool prints
        /// what it was given — and that asking for it produces the neutral Czech sentence.
        /// </summary>
        [TestMethod]
        public void SubjectIsDroppedOnlyWhenAskedFor()
        {
            Assert.AreEqual("Já čtu knihu.", Sentence(null, "já", "číst", "kniha"));

            Assert.AreEqual(
                "Čtu knihu.",
                Sentence(new DraftOverrides { DropSubject = true }, "já", "číst", "kniha"));
        }

        /// <summary>
        /// Verifies that the communicative status decides the word order, which is the whole reason the
        /// draft carries it.
        /// </summary>
        [TestMethod]
        public void StatusDecidesTheWordOrder()
        {
            var overrides = new DraftOverrides();
            overrides.For("kniha").Status = InformationStatus.Given;
            overrides.For("student").Status = InformationStatus.New;

            Assert.AreEqual("Knihu čte student.", Sentence(overrides, "student", "číst", "kniha"));
        }
    }
}
