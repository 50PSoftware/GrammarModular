using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the top of the pipeline: the sense of the verb, which participant is the subject,
    /// whether it is expressed at all, and what counts as old information.
    /// </summary>
    /// <remarks>
    /// The planner takes roles as given and the resolver is what works them out, so the two are tested
    /// apart. Everything here that guesses is in <see cref="CzechRoleResolver"/>.
    /// </remarks>
    [TestClass]
    public sealed class SentencePlannerTests
    {
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

            planner = provider.GetRequiredService<CzechSentencePlanner>();
            roles = provider.GetRequiredService<CzechRoleResolver>();
            builder = provider.GetRequiredService<CzechSentenceBuilder>();
        }

        private static CzechWordRequest Verb(string lemma, string pattern) => new()
        {
            Lemma = lemma,
            Pattern = pattern,
            WordCategory = WordCategory.Verb,
            Aspect = VerbAspect.Imperfective
        };

        private static PlannedParticipant Noun(
            string lemma, string pattern, Gender gender, bool animate = false, FgdFunctor? functor = null)
            => PlannedParticipant.Of(
                new CzechWordRequest
                {
                    Lemma = lemma,
                    Pattern = pattern,
                    WordCategory = WordCategory.Noun,
                    Gender = gender,
                    IsAnimate = animate,
                    Number = Number.Singular
                },
                functor);

        private static PlannedParticipant Pronoun(string lemma, FgdFunctor functor) =>
            PlannedParticipant.Of(
                new CzechWordRequest
                {
                    Lemma = lemma,
                    WordCategory = WordCategory.Pronoun,
                    Number = Number.Singular,
                    Gender = Gender.Masculine
                },
                functor);

        private static PlannedParticipant Student(FgdFunctor? functor = null) =>
            Noun("student", "pán", Gender.Masculine, animate: true, functor);

        private static PlannedParticipant Book(FgdFunctor? functor = null) =>
            Noun("kniha", "žena", Gender.Feminine, functor: functor);

        private static string Build(SentencePlan plan) => builder.Build(planner.Plan(plan));

        /// <summary>
        /// What the plan leaves unsaid takes the unmarked value: the present indicative active, and the
        /// first participant as what the sentence is about.
        /// </summary>
        [TestMethod]
        public void UnstatedCategoriesTakeTheUnmarkedValue()
        {
            var sentence = Build(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(FgdFunctor.ACT), Book(FgdFunctor.PAT)]
            });

            Assert.AreEqual("Student čte knihu.", sentence);
        }

        /// <summary>
        /// A subject pronoun that adds nothing is left out, and the agreement it was carrying moves onto
        /// the predicate — which is the whole of what makes the sentence still say who.
        /// </summary>
        [TestMethod]
        public void SubjectPronounIsDropped()
        {
            var plan = new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Pronoun("já", FgdFunctor.ACT), Book(FgdFunctor.PAT)]
            };

            Assert.AreEqual("Čtu knihu.", Build(plan));
            Assert.AreEqual("Já čtu knihu.", Build(plan with { AllowSubjectDrop = false }));
        }

        /// <summary>
        /// A contrasted pronoun is doing work and stays, because dropping it would take the contrast
        /// with it.
        /// </summary>
        [TestMethod]
        public void ContrastedSubjectPronounStays()
        {
            var sentence = Build(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants =
                [
                    Pronoun("já", FgdFunctor.ACT) with { Status = InformationStatus.Contrastive },
                    Book(FgdFunctor.PAT)
                ]
            });

            StringAssert.Contains(sentence, "Já");
        }

        /// <summary>
        /// A noun subject never drops: nothing else in the sentence would name it.
        /// </summary>
        [TestMethod]
        public void NounSubjectIsNotDropped()
        {
            Assert.AreEqual("Student čte knihu.", Build(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(FgdFunctor.ACT), Book(FgdFunctor.PAT)]
            }));
        }

        /// <summary>
        /// Asking for the patient to be the subject selects the passive frame, and with it the whole
        /// remapping: the agent drops to the instrumental and the patient rises to the nominative, which
        /// the predicate then agrees with.
        /// </summary>
        [TestMethod]
        public void PerspectiveOnThePatientSelectsThePassive()
        {
            var sentence = Build(new SentencePlan
            {
                Predicate = Verb("dávat", "trida5"),
                Participants =
                [
                    Student(FgdFunctor.ACT),
                    Noun("žena", "žena", Gender.Feminine, animate: true, FgdFunctor.ADDR),
                    Book(FgdFunctor.PAT)
                ],
                Perspective = FgdFunctor.PAT
            });

            // Ženský rod přísudku je důkaz, že podmětem je kniha a ne student.
            Assert.AreEqual("Kniha je dávána studentem ženě.", sentence);
        }

        /// <summary>
        /// The perspective is also what the sentence is about, so the participant it names becomes the
        /// theme — a passive that left the agent in front would have gained nothing over the active.
        /// </summary>
        [TestMethod]
        public void PerspectiveDecidesTheTheme()
        {
            var sentence = Build(new SentencePlan
            {
                Predicate = Verb("dávat", "trida5"),
                Participants = [Student(FgdFunctor.ACT), Book(FgdFunctor.PAT)],
                Perspective = FgdFunctor.PAT
            });

            StringAssert.StartsWith(sentence, "Kniha");
        }

        /// <summary>
        /// A verb whose senses the dictionary does not rank is an open question, not a coin toss.
        /// </summary>
        [TestMethod]
        public void AmbiguousVerbIsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => Build(new SentencePlan
            {
                Predicate = Verb("jít", "jít"),
                Participants = [Student(FgdFunctor.ACT)]
            }));

            StringAssert.Contains(failure.Message, "motion");
        }

        /// <summary>
        /// An inner participant the verb has no slot for is refused at the top, where the message can
        /// still name what the caller wrote.
        /// </summary>
        [TestMethod]
        public void FunctorOutsideTheFrameIsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => Build(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(FgdFunctor.ACT), Book(FgdFunctor.ADDR)]
            }));

            StringAssert.Contains(failure.Message, "ADDR");
        }

        /// <summary>
        /// The planner takes roles as given; a participant without one is refused rather than guessed
        /// at, and the message names the stage that does the guessing.
        /// </summary>
        [TestMethod]
        public void ParticipantWithoutARoleIsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => Build(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(), Book()]
            }));

            StringAssert.Contains(failure.Message, nameof(CzechRoleResolver));
        }

        /// <summary>
        /// The resolver reads the roles off the frame, so a caller that knows what it wants to say need
        /// not know what the Functional Generative Description calls the parts of it.
        /// </summary>
        [TestMethod]
        public void RoleResolverReadsTheRolesOffTheFrame()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(), Book()]
            });

            Assert.AreEqual(FgdFunctor.ACT, plan.Participants[0].Functor);
            Assert.AreEqual(FgdFunctor.PAT, plan.Participants[1].Functor);
            Assert.AreEqual("Student čte knihu.", Build(plan));
        }

        /// <summary>
        /// The addressee prefers an animate noun, which is what keeps the two objects of a transfer verb
        /// apart without the caller naming either.
        /// </summary>
        [TestMethod]
        public void AnimateNounBecomesTheAddressee()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("dávat", "trida5"),
                Participants =
                [
                    Student(),
                    Noun("žena", "žena", Gender.Feminine, animate: true),
                    Book()
                ]
            });

            Assert.AreEqual("Student dává ženě knihu.", Build(plan));
        }

        /// <summary>
        /// A stated role is never overwritten by the guess.
        /// </summary>
        [TestMethod]
        public void StatedRoleSurvivesTheResolver()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(FgdFunctor.PAT), Book()]
            });

            Assert.AreEqual(FgdFunctor.PAT, plan.Participants[0].Functor);
            Assert.AreEqual(FgdFunctor.ACT, plan.Participants[1].Functor);
        }

        /// <summary>
        /// A participant the frame does not account for and no preposition explains comes back
        /// unresolved rather than plausible.
        /// </summary>
        [TestMethod]
        public void UnexplainedParticipantStaysUnresolved()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(), Book(), Noun("den", "hrad", Gender.Masculine)]
            });

            Assert.AreEqual(1, CzechRoleResolver.Unresolved(plan).Count);
            Assert.AreEqual("den", CzechRoleResolver.Unresolved(plan)[0].Word.Lemma);
        }

        /// <summary>
        /// A preposition names the free modification the frame cannot: the semantic group of the
        /// preposition and its case is what the functor comes from.
        /// </summary>
        [TestMethod]
        public void PrepositionNamesTheFreeModification()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants =
                [
                    Student(),
                    Book(),
                    Noun("les", "les", Gender.Masculine) with { Preposition = "u" }
                ]
            });

            Assert.AreEqual(FgdFunctor.LOC, plan.Participants[2].Functor);
            Assert.AreEqual(Case.Genitive, plan.Participants[2].Word.Case);
        }
    }
}
