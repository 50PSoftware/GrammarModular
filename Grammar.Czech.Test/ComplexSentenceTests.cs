using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies clause combination: comma placement, and where the clitic cluster lands in a dependent clause.
    /// </summary>
    [TestClass]
    public sealed class ComplexSentenceTests
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

        private static CzechWordRequest Verb(string lemma, string pattern, ReflexiveType reflexive = ReflexiveType.None) => new()
        {
            Lemma = lemma,
            Pattern = pattern,
            WordCategory = WordCategory.Verb,
            Modus = Modus.Indicative,
            Tense = Tense.Past,
            Aspect = VerbAspect.Imperfective,
            Voice = Voice.Active,
            Person = Person.Third,
            Number = Number.Singular,
            Gender = Gender.Masculine,
            ReflexiveType = reflexive
        };

        private static ClauseElement Petr() =>
            ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = "student",
                    Pattern = "pán",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Masculine,
                    Number = Number.Singular,
                    IsAnimate = true,
                    Case = Case.Nominative
                },
                FgdFunctor.ACT,
                InformationStatus.Given);

        private static CzechClause Clause(CzechWordRequest predicate, params ClauseElement[] elements) =>
            new() { Predicate = predicate, Elements = elements };

        #region Coordination

        /// <summary>
        /// No comma before a, i, ani, nebo, či.
        /// </summary>
        /// <param name="conjunction">The coordinating conjunction.</param>
        [DataTestMethod]
        [DataRow("a")]
        [DataRow("i")]
        [DataRow("nebo")]
        [DataRow("či")]
        public void Build_CoordinationWithoutComma_JoinsWithASpace(string conjunction)
        {
            var sentence = new Coordination(conjunction,
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ]);

            Assert.AreEqual($"Student dělal {conjunction} dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// Comma before ale, avšak, nýbrž, neboť.
        /// </summary>
        /// <param name="conjunction">The coordinating conjunction.</param>
        [DataTestMethod]
        [DataRow("ale")]
        [DataRow("avšak")]
        [DataRow("nýbrž")]
        [DataRow("neboť")]
        public void Build_CoordinationWithComma_PutsACommaBeforeTheConjunction(string conjunction)
        {
            var sentence = new Coordination(conjunction,
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ]);

            Assert.AreEqual($"Student dělal, {conjunction} dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// A coordinating conjunction stands outside the clause it precedes, so that clause keeps its own
        /// first position and the cluster follows the verb rather than the conjunction.
        /// </summary>
        [TestMethod]
        public void Build_CoordinatedReflexiveClause_LeavesFirstPositionToTheClause()
        {
            var sentence = new Coordination("a",
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se))
            ]);

            Assert.AreEqual("Student dělal a dělal se.", builder.Build(sentence));
        }

        /// <summary>
        /// More than two conjuncts repeat the conjunction between each pair.
        /// </summary>
        [TestMethod]
        public void Build_ThreeConjuncts_RepeatsTheConjunction()
        {
            var sentence = new Coordination("a",
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá")),
                Clause(Verb("dělat", "dělá"))
            ]);

            Assert.AreEqual("Student dělal a dělal a dělal.", builder.Build(sentence));
        }

        #endregion Coordination

        #region Subordination

        /// <summary>
        /// A dependent clause is preceded by a comma and gets neither a capital nor a full stop of its own.
        /// </summary>
        [TestMethod]
        public void Build_Subordination_PunctuatesTheBoundaryOnly()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "protože",
                Clause(Verb("dělat", "dělá")));

            Assert.AreEqual("Student dělal, protože dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// The subordinating conjunction fills the first position of its clause, so the cluster follows the
        /// conjunction rather than the verb.
        /// </summary>
        [TestMethod]
        public void Build_SubordinateReflexiveClause_PlacesClusterAfterTheConjunction()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "protože",
                Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se)));

            Assert.AreEqual("Student dělal, protože se dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// With first position taken by the conjunction, the cluster precedes the subject of the
        /// dependent clause.
        /// </summary>
        [TestMethod]
        public void Build_SubordinateClauseWithSubject_PutsTheClusterAheadOfTheSubject()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá")),
                "protože",
                Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se), Petr()));

            Assert.AreEqual("Dělal, protože se student dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// The past auxiliary travels with the cluster into the position after the conjunction.
        /// </summary>
        [TestMethod]
        public void Build_SubordinateClauseWithAuxiliary_MovesTheWholeClusterAfterTheConjunction()
        {
            var predicate = Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se);
            predicate.Person = Person.First;

            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "že",
                Clause(predicate));

            Assert.AreEqual("Student dělal, že jsem se dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// Nesting composes: a coordination can sit inside a subordination.
        /// </summary>
        [TestMethod]
        public void Build_CoordinationInsideSubordination_Composes()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "protože",
                new Coordination("a",
                [
                    Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se)),
                    Clause(Verb("dělat", "dělá"))
                ]));

            Assert.AreEqual("Student dělal, protože se dělal a dělal.", builder.Build(sentence));
        }

        #endregion Subordination

        #region Inventory

        /// <summary>
        /// Conjunctions are a closed class, so an unknown one is reported instead of guessed at.
        /// </summary>
        [TestMethod]
        public void Build_UnknownConjunction_Throws()
        {
            var sentence = new Coordination("jakožto",
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ]);

            var exception = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(sentence));
            StringAssert.Contains(exception.Message, "jakožto");
        }

        /// <summary>
        /// aby and kdyby are deliberately absent: they fuse with the conditional auxiliary and inflect for
        /// person, which placing a fixed string in front of a clause cannot express.
        /// </summary>
        [TestMethod]
        public void Build_AbyConjunction_IsReportedAsUnsupported()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "aby",
                Clause(Verb("dělat", "dělá")));

            var exception = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(sentence));
            StringAssert.Contains(exception.Message, "abych");
        }

        #endregion Inventory
    }
}
