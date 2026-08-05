using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies how a particle and an interjection enter the clause: where each stands, what the clause-initial
    /// particle demands of the predicate, and which placements are refused.
    /// </summary>
    [TestClass]
    public sealed class ParticleAndInterjectionClauseTests
    {
        private static CzechSentenceBuilder builder = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            builder = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })
                              .GetRequiredService<CzechSentenceBuilder>();
        }

        private static CzechWordRequest Verb(
            Modus modus = Modus.Indicative,
            Tense tense = Tense.Past,
            ReflexiveType reflexive = ReflexiveType.None) => new()
            {
                Lemma = "dělat",
                Pattern = "dělá",
                WordCategory = WordCategory.Verb,
                Modus = modus,
                Tense = tense,
                Aspect = VerbAspect.Imperfective,
                Voice = Voice.Active,
                Person = Person.Third,
                Number = Number.Singular,
                Gender = Gender.Masculine,
                ReflexiveType = reflexive
            };

        private static ClauseElement Student(InformationStatus status = InformationStatus.Given, string? particle = null) =>
            new()
            {
                Word = new CzechWordRequest
                {
                    Lemma = "student",
                    Pattern = "pán",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Masculine,
                    Number = Number.Singular,
                    IsAnimate = true,
                    Case = Case.Nominative
                },
                Functor = FgdFunctor.ACT,
                Status = status,
                Particle = particle
            };

        #region Citoslovce

        /// <summary>
        /// An interjection stands outside the clause behind its own comma, which is the ÚJČ rule wherever it
        /// does not stand in for a clause member.
        /// </summary>
        [TestMethod]
        public void Build_ClauseWithInterjection_SetsItOffWithAComma()
        {
            var clause = new CzechClause { Predicate = Verb(), Elements = [Student()], Interjection = "ach" };

            Assert.AreEqual("Ach, student dělal.", builder.Build(clause));
        }

        /// <summary>
        /// Standing outside the clause, it leaves first position to the subject, so the cluster attaches
        /// where it would have without it — unlike a clause-initial particle.
        /// </summary>
        [TestMethod]
        public void Build_InterjectionWithClitic_DoesNotTakeFirstPosition()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(reflexive: ReflexiveType.ReflexivumTantum_Se),
                Elements = [Student()],
                Interjection = "hurá"
            };

            Assert.AreEqual("Hurá, student se dělal.", builder.Build(clause));
        }

        /// <summary>
        /// The terminator is left to the caller. An emotional interjection tends towards an exclamation mark,
        /// but tending is not a rule and the clause says what closes it.
        /// </summary>
        [TestMethod]
        public void Build_InterjectionWithExclamation_UsesTheTerminatorTheCallerSet()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements = [Student()],
                Interjection = "hurá",
                Terminator = "!"
            };

            Assert.AreEqual("Hurá, student dělal!", builder.Build(clause));
        }

        #endregion Citoslovce

        #region Větná částice

        /// <summary>
        /// A clause-initial particle fills first position the way a subordinating conjunction does, so the
        /// clitic cluster follows it directly.
        /// </summary>
        [TestMethod]
        public void Build_ClauseInitialParticle_TakesFirstPositionSoTheClusterFollowsIt()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(tense: Tense.Present, reflexive: ReflexiveType.ReflexivumTantum_Se),
                Elements = [Student()],
                Particle = "ať"
            };

            Assert.AreEqual("Ať se student dělá.", builder.Build(clause));
        }

        /// <summary>
        /// The mood is the predicate's business. "Ať přijde" is a plain third-person present — Czech has no
        /// third-person imperative — and NESČ states no mood government for the optative group, so the
        /// builder enforces none and both readings go through.
        /// </summary>
        /// <param name="modus">The mood of the predicate.</param>
        [DataTestMethod]
        [DataRow("Indicative")]
        [DataRow("Conditional")]
        public void Build_OptativeParticle_DoesNotConstrainTheMood(string modus)
        {
            var clause = new CzechClause
            {
                Predicate = Verb(modus: Enum.Parse<Modus>(modus), tense: Tense.Present),
                Elements = [Student()],
                Particle = "kéž"
            };

            StringAssert.StartsWith(builder.Build(clause), "Kéž ");
        }

        /// <summary>
        /// A particle that is not clause-initial does not belong in this slot — it scopes over a constituent
        /// and goes on that constituent instead.
        /// </summary>
        [TestMethod]
        public void Build_NonInitialParticleAsClauseParticle_Throws()
        {
            var clause = new CzechClause { Predicate = Verb(), Elements = [Student()], Particle = "jen" };

            var exception = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(clause));

            StringAssert.Contains(exception.Message, "ClauseElement.Particle");
        }

        #endregion Větná částice

        #region Částice s dosahem na konstituent

        /// <summary>
        /// A focusing particle opens the constituent it singles out.
        /// </summary>
        [TestMethod]
        public void Build_ScopedParticle_OpensItsConstituent()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements = [Student(particle: "jen")]
            };

            Assert.AreEqual("Jen student dělal.", builder.Build(clause));
        }

        /// <summary>
        /// It stands outside the preposition, because its scope is the whole constituent.
        /// </summary>
        [TestMethod]
        public void Build_ScopedParticleOnPrepositionalPhrase_StandsOutsideThePreposition()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements =
                [
                    Student(),
                    new ClauseElement
                    {
                        Preposition = "pro",
                        Word = new CzechWordRequest
                        {
                            Lemma = "student",
                            Pattern = "pán",
                            WordCategory = WordCategory.Noun,
                            Gender = Gender.Masculine,
                            Number = Number.Singular,
                            IsAnimate = true,
                            Case = Case.Accusative
                        },
                        Functor = FgdFunctor.BEN,
                        Status = InformationStatus.New,
                        Particle = "jen"
                    }
                ]
            };

            StringAssert.Contains(builder.Build(clause), "jen pro studenta");
        }

        /// <summary>
        /// A modifying particle cannot stand in the rheme — the one positional restriction NESČ states of a
        /// whole group, and InformationStatus is what makes it checkable.
        /// </summary>
        [TestMethod]
        public void Build_ModifyingParticleInTheRheme_Throws()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements = [Student(status: InformationStatus.New, particle: "prostě")]
            };

            var exception = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(clause));

            StringAssert.Contains(exception.Message, "prostě");
            StringAssert.Contains(exception.Message, "rématu");
        }

        /// <summary>
        /// The same particle outside the rheme is fine, so the restriction is on the position and not on the
        /// word.
        /// </summary>
        [TestMethod]
        public void Build_ModifyingParticleOutsideTheRheme_IsAccepted()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements = [Student(status: InformationStatus.Given, particle: "prostě")]
            };

            Assert.AreEqual("Prostě student dělal.", builder.Build(clause));
        }

        /// <summary>
        /// And a focusing particle stands in the rheme without complaint, which is where singling a new
        /// constituent out usually happens.
        /// </summary>
        [TestMethod]
        public void Build_FocusingParticleInTheRheme_IsAccepted()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements = [Student(), Student(status: InformationStatus.New, particle: "dokonce")]
            };

            StringAssert.Contains(builder.Build(clause), "dokonce student");
        }

        #endregion Částice s dosahem na konstituent
    }
}
