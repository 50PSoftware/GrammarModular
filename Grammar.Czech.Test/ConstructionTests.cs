using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Providers.SqliteProviders;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies light verb constructions: a predicate whose meaning sits in a noun, and whose valency
    /// is therefore the noun's rather than the verb's.
    /// </summary>
    [TestClass]
    public sealed class ConstructionTests
    {
        private static ICzechConstructionService constructions = null!;
        private static CzechSentencePlanner planner = null!;
        private static CzechRoleResolver roles = null!;
        private static CzechSentenceBuilder builder = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            constructions = provider.GetRequiredService<ICzechConstructionService>();
            planner = provider.GetRequiredService<CzechSentencePlanner>();
            roles = provider.GetRequiredService<CzechRoleResolver>();
            builder = provider.GetRequiredService<CzechSentenceBuilder>();
        }

        private static PlannedParticipant Noun(string lemma, string? preposition = null) => new()
        {
            Word = new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Noun,
                Number = Number.Singular
            },
            Preposition = preposition,
        };

        private static string Build(string verb, string pattern, params PlannedParticipant[] participants)
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = new CzechWordRequest
                {
                    Lemma = verb,
                    Pattern = pattern,
                    WordCategory = WordCategory.Verb,
                    Aspect = VerbAspect.Imperfective
                },
                Participants = participants,
            });

            Assert.AreEqual(0, CzechRoleResolver.Unresolved(plan).Count,
                "Nezařazené: " + string.Join(", ", CzechRoleResolver.Unresolved(plan).Select(p => p.Word.Lemma)));

            return builder.Build(planner.Plan(plan));
        }

        /// <summary>
        /// The pair is recognized from both words together, which is the whole of what makes it a
        /// construction: neither lemma on its own means what they mean standing side by side.
        /// </summary>
        [TestMethod]
        public void ConstructionIsRecognizedFromBothWords()
        {
            var found = constructions.Find("mít", ["student", "zájem", "kniha"]);

            Assert.IsNotNull(found);
            Assert.AreEqual("LVC.mít.zájem", found.Name);
            CollectionAssert.AreEqual(
                new[] { FgdFunctor.ACT, FgdFunctor.CPHR, FgdFunctor.PAT },
                found.Slots.Select(slot => slot.Functor).ToArray());

            Assert.IsNull(constructions.Find("mít", ["student", "kniha"]));
        }

        /// <summary>
        /// The construction governs what the verb alone does not — this is the reason it is recorded at
        /// all, since no frame of <em>mít</em> licenses an <em>o</em>.
        /// </summary>
        [TestMethod]
        public void ConstructionGovernsWhatTheVerbAloneDoesNot()
        {
            Assert.AreEqual(
                "Student má zájem o knihu.",
                Build("mít", "mít", Noun("student"), Noun("zájem"), Noun("kniha", "o")));
        }

        /// <summary>
        /// Each construction brings its own government, so the preposition and case come from the
        /// pattern rather than from anything the verb or the preposition would suggest on its own.
        /// </summary>
        [DataTestMethod]
        [DataRow("dávat", "trida5", "pozor", "na", "Student dává pozor na knihu.")]
        [DataRow("mít", "mít", "strach", "z", "Student má strach z knihy.")]
        public void EachConstructionBringsItsOwnGovernment(
            string verb, string pattern, string noun, string preposition, string expected)
        {
            Assert.AreEqual(
                expected,
                Build(verb, pattern, Noun("student"), Noun(noun), Noun("kniha", preposition)));
        }

        /// <summary>
        /// The verb keeps its own frame where the noun is absent, so the construction does not leak into
        /// ordinary uses of the same verb.
        /// </summary>
        [TestMethod]
        public void VerbWithoutTheNounKeepsItsOwnFrame()
        {
            Assert.AreEqual("Student má knihu.", Build("mít", "mít", Noun("student"), Noun("kniha")));
        }

        /// <summary>
        /// The argument the noun licenses is optional, as the pattern says: the construction stands on
        /// its own.
        /// </summary>
        [TestMethod]
        public void ArgumentOfTheConstructionIsOptional()
        {
            Assert.AreEqual("Student má zájem.", Build("mít", "mít", Noun("student"), Noun("zájem")));
        }

        /// <summary>
        /// The light verb is still a verb: tense and agreement are its, and only the meaning is the
        /// noun's.
        /// </summary>
        [TestMethod]
        public void LightVerbStillCarriesTheTense()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = new CzechWordRequest
                {
                    Lemma = "mít",
                    Pattern = "mít",
                    WordCategory = WordCategory.Verb,
                    Aspect = VerbAspect.Imperfective,
                    Tense = Tense.Past
                },
                Participants = [Noun("student"), Noun("zájem"), Noun("kniha", "o")],
            });

            Assert.AreEqual("Student měl zájem o knihu.", builder.Build(planner.Plan(plan)));
        }

        /// <summary>
        /// A construction can be named the way a sense of a verb is named, which is what lets the stages
        /// below the planner — which never see the words standing around the verb — work with it at all.
        /// </summary>
        [TestMethod]
        public void ConstructionIsAddressableByName()
        {
            Assert.IsNotNull(constructions.GetFrame("LVC.mít.zájem"));
            Assert.IsNull(constructions.GetFrame("LVC.mít.nic"));
        }

        /// <summary>
        /// A template that licenses nothing is an error rather than an empty frame: it would otherwise
        /// behave exactly as though the verb had been used on its own, which is the reading the row
        /// exists to rule out.
        /// </summary>
        [TestMethod]
        public void EmptyTemplateIsRefused()
        {
            Assert.ThrowsException<InvalidOperationException>(
                () => ConstructionSlots.Parse("""{"slots":[]}""", "LVC.test"));

            Assert.ThrowsException<InvalidOperationException>(
                () => ConstructionSlots.Parse("nic takového", "LVC.test"));
        }

        /// <summary>
        /// A functor the vocabulary does not hold is named in the refusal, since a silent fallback would
        /// be a slot quietly governing the wrong thing.
        /// </summary>
        [TestMethod]
        public void UnknownFunctorInATemplateIsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(
                () => ConstructionSlots.Parse("""{"slots":[{"functor":"NIC","forms":[]}]}""", "LVC.test"));

            StringAssert.Contains(failure.Message, "NIC");
        }
    }
}
