using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the diatheses beyond the passive: the ones with no subject at all.
    /// </summary>
    /// <remarks>
    /// A perspective can only name a diathesis by pointing at the participant that becomes the subject,
    /// which works for the passive and for nothing else. The deagentive and the dispositional have no
    /// subject to point at, so the plan names the diathesis outright and the frame supplies the rest.
    /// </remarks>
    [TestClass]
    public sealed class DiathesisTests
    {
        private static CzechSentenceBuilder builder = null!;
        private static CzechSentencePlanner planner = null!;

        /// <summary>
        /// Builds the service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();

            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            builder = provider.GetRequiredService<CzechSentenceBuilder>();
            planner = provider.GetRequiredService<CzechSentencePlanner>();
        }

        // Plán projde plánovačem a teprve pak stavitelem, stejně jako v SentencePlannerTests.
        private static string Build(SentencePlan plan) => builder.Build(planner.Plan(plan));

        private static SentencePlan Plan(string verb, Diathesis diathesis, params PlannedParticipant[] participants) =>
            new()
            {
                Predicate = new CzechWordRequest
                {
                    Lemma = verb,
                    WordCategory = WordCategory.Verb,
                    Tense = Tense.Past,
                },
                Diathesis = diathesis,
                Participants = participants,
            };

        /// <summary>
        /// The deagentive drops the actor entirely and leaves a subjectless clause with the particle.
        /// </summary>
        /// <remarks>
        /// Not the passive: there the actor is demoted to an instrumental and can still be said. Here it
        /// is gone, which is why the frame carries no slot for it and the participle falls to the neuter
        /// singular — the same path an impersonal verb takes.
        /// </remarks>
        [DataTestMethod]
        [DataRow("pracovat", "Pracovalo se.")]
        [DataRow("mluvit", "Mluvilo se.")]
        public void DeagentiveHasNoActorAtAll(string verb, string expected)
        {
            Assert.AreEqual(expected, Build(Plan(verb, Diathesis.ReflexivePassive)));
        }

        /// <summary>
        /// The dispositional brings the actor back in the dative, and the clause still has no subject.
        /// </summary>
        [TestMethod]
        public void DispositionalPutsTheActorInTheDative()
        {
            var actor = new PlannedParticipant
            {
                Word = new CzechWordRequest
                {
                    Lemma = "student",
                    WordCategory = WordCategory.Noun,
                    Number = Number.Singular,
                },
                Functor = FgdFunctor.ACT,
            };

            Assert.AreEqual(
                "Studentovi se pracovalo.",
                Build(Plan("pracovat", Diathesis.Dispositional, actor)));
        }

        /// <summary>
        /// A plan naming a perspective and a diathesis that disagree is refused rather than resolved.
        /// </summary>
        /// <remarks>
        /// Both say which diathesis to build in, and picking one of two things the caller asked for
        /// would be choosing on their behalf.
        /// </remarks>
        [TestMethod]
        public void PerspectiveAndDiathesisMustAgree()
        {
            var plan = Plan("pracovat", Diathesis.ReflexivePassive) with { Perspective = FgdFunctor.PAT };

            var exception = Assert.ThrowsException<InvalidOperationException>(() => Build(plan));

            StringAssert.Contains(exception.Message, "odporují");
        }

        /// <summary>
        /// The resultative keeps the actor as an ordinary subject — unlike the deagentive and the
        /// dispositional, mít governs a normal nominative actor — but the predicate is built from mít
        /// plus the neuter singular participle rather than the verb's own conjugation.
        /// </summary>
        /// <remarks>
        /// The case comes from the frame seed.025 added for psát/napsat (NESČ, Diateze; MSoČ 2, 2014,
        /// "mám uvařeno"): a plain nominative ACT and no other slot, since the two examples the diathesis
        /// is named from — mám napsáno, má uklizeno — do not express an object.
        /// </remarks>
        [TestMethod]
        public void ResultativeKeepsTheActorAsSubject()
        {
            var actor = new PlannedParticipant
            {
                Word = new CzechWordRequest
                {
                    Lemma = "student",
                    WordCategory = WordCategory.Noun,
                    Number = Number.Singular,
                },
                Functor = FgdFunctor.ACT,
            };

            Assert.AreEqual(
                "Student má napsáno.",
                Build(new SentencePlan
                {
                    Predicate = new CzechWordRequest
                    {
                        Lemma = "napsat",
                        Pattern = "psát",
                        WordCategory = WordCategory.Verb,
                        Tense = Tense.Present,
                    },
                    Diathesis = Diathesis.Resultative,
                    Participants = [actor],
                }));
        }

        /// <summary>
        /// First person past drops the pronoun the same way any other verb does, but mít's own past is
        /// an ordinary l-participle and needs the clitic auxiliary on top of it — "měl jsem", not "měl".
        /// </summary>
        [TestMethod]
        public void ResultativePastFirstPersonTakesTheClitic()
        {
            var actor = new PlannedParticipant
            {
                Word = new CzechWordRequest
                {
                    Lemma = "já",
                    WordCategory = WordCategory.Pronoun,
                    Number = Number.Singular,
                    Gender = Gender.Masculine,
                },
                Functor = FgdFunctor.ACT,
            };

            Assert.AreEqual(
                "Měl jsem napsáno.",
                Build(new SentencePlan
                {
                    Predicate = new CzechWordRequest
                    {
                        Lemma = "napsat",
                        Pattern = "psát",
                        WordCategory = WordCategory.Verb,
                        Tense = Tense.Past,
                    },
                    Diathesis = Diathesis.Resultative,
                    Participants = [actor],
                }));
        }

        /// <summary>
        /// The recipient deobjective promotes the recipient (ADDR) to subject — dostat agrees with
        /// Karel, not with the payer, which is demoted to "od" + genitive and left unstated here since
        /// it is optional (Daneš, Naše řeč 51, 1968: "Karel dostal (od otce) vyhubováno").
        /// </summary>
        [TestMethod]
        public void RecipientDeobjectivePromotesTheRecipientToSubject()
        {
            var recipient = new PlannedParticipant
            {
                Word = new CzechWordRequest
                {
                    Lemma = "Karel",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Masculine,
                    IsAnimate = true,
                    Number = Number.Singular,
                },
                Functor = FgdFunctor.ADDR,
            };

            Assert.AreEqual(
                "Karel dostal zaplaceno.",
                Build(new SentencePlan
                {
                    Predicate = new CzechWordRequest
                    {
                        Lemma = "zaplatit",
                        Pattern = "trida4",
                        WordCategory = WordCategory.Verb,
                        Tense = Tense.Past,
                    },
                    Diathesis = Diathesis.RecipientDeobjective,
                    Participants = [recipient],
                }));
        }

        /// <summary>
        /// First person past drops the pronoun the same way any other verb does, but dostat's own past
        /// is an ordinary l-participle and needs the clitic auxiliary on top of it — this is the shape
        /// Daneš's own title example takes: "Dostal jsem přidáno."
        /// </summary>
        [TestMethod]
        public void RecipientDeobjectivePastFirstPersonTakesTheClitic()
        {
            var recipient = new PlannedParticipant
            {
                Word = new CzechWordRequest
                {
                    Lemma = "já",
                    WordCategory = WordCategory.Pronoun,
                    Number = Number.Singular,
                    Gender = Gender.Masculine,
                },
                Functor = FgdFunctor.ADDR,
            };

            Assert.AreEqual(
                "Dostal jsem zaplaceno.",
                Build(new SentencePlan
                {
                    Predicate = new CzechWordRequest
                    {
                        Lemma = "zaplatit",
                        Pattern = "trida4",
                        WordCategory = WordCategory.Verb,
                        Tense = Tense.Past,
                    },
                    Diathesis = Diathesis.RecipientDeobjective,
                    Participants = [recipient],
                }));
        }

        /// <summary>
        /// Saying nothing still builds the active clause, so the plans written before this keep working.
        /// </summary>
        [TestMethod]
        public void SayingNothingStaysActive()
        {
            var actor = new PlannedParticipant
            {
                Word = new CzechWordRequest
                {
                    Lemma = "student",
                    WordCategory = WordCategory.Noun,
                    Number = Number.Singular,
                },
                Functor = FgdFunctor.ACT,
            };

            Assert.AreEqual(
                "Student pracoval.",
                Build(new SentencePlan
                {
                    Predicate = new CzechWordRequest
                    {
                        Lemma = "pracovat",
                        WordCategory = WordCategory.Verb,
                        Tense = Tense.Past,
                    },
                    Participants = [actor],
                }));
        }
    }
}
