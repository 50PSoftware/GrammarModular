using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies looking a conjunction up by the kind of clause it introduces, rather than the other way round.
    /// </summary>
    /// <remarks>
    /// The semantic groups are the traditional taxonomy of dependent clauses under other names, so this is
    /// what lets a caller ask for an účelová věta without knowing that aby is the word for it.
    /// </remarks>
    [TestClass]
    public sealed class ClauseTypeLookupTests
    {
        private static ICzechConjunctionService conjunctions = null!;
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

            conjunctions = provider.GetRequiredService<ICzechConjunctionService>();
            builder = provider.GetRequiredService<CzechSentenceBuilder>();
        }

        /// <summary>
        /// Every traditional kind of dependent clause resolves to the conjunction that introduces it, with
        /// the unmarked choice first.
        /// </summary>
        /// <param name="semanticGroup">The relation, which is the clause kind under another name.</param>
        /// <param name="expected">The conjunction expected first.</param>
        [DataTestMethod]
        [DataRow("Purpose", "aby", DisplayName = "účelová – aby")]
        [DataRow("Causal", "protože", DisplayName = "příčinná – protože")]
        [DataRow("Conditional", "jestliže", DisplayName = "podmínková – jestliže")]
        [DataRow("Concessive", "ač", DisplayName = "přípustková – ač")]
        [DataRow("Temporal", "když", DisplayName = "časová – když")]
        [DataRow("Consecutive", "takže", DisplayName = "důsledková – takže")]
        [DataRow("Comparison", "jako", DisplayName = "srovnávací – jako")]
        [DataRow("Content", "že", DisplayName = "obsahová – že")]
        public void GetConjunctionsFor_DependentClauseKind_ReturnsTheUnmarkedConjunctionFirst(
            string semanticGroup, string expected)
        {
            var found = conjunctions.GetConjunctionsFor(
                ConjunctionType.Subordinating,
                Enum.Parse<ConjunctionSemanticGroup>(semanticGroup));

            Assert.IsTrue(found.Count > 0, $"Pro {semanticGroup} se nenašla žádná spojka.");
            Assert.AreEqual(expected, found[0]);
        }

        /// <summary>
        /// A relation with several conjunctions returns all of them, not just the first.
        /// </summary>
        [TestMethod]
        public void GetConjunctionsFor_RelationWithSeveralConjunctions_ReturnsThemAll()
        {
            var causal = conjunctions.GetConjunctionsFor(ConjunctionType.Subordinating, ConjunctionSemanticGroup.Causal).ToList();

            CollectionAssert.Contains(causal, "protože");
            CollectionAssert.Contains(causal, "poněvadž");
            CollectionAssert.Contains(causal, "jelikož");
            CollectionAssert.Contains(causal, "ježto");
        }

        /// <summary>
        /// Both readings are searched, so a conjunction whose primary reading is the other type is still
        /// found — and comes after those that are this type to begin with.
        /// </summary>
        [TestMethod]
        public void GetConjunctionsFor_SearchesAlternativeReadingsAndRanksThemLast()
        {
            var comparison = conjunctions.GetConjunctionsFor(ConjunctionType.Subordinating, ConjunctionSemanticGroup.Comparison).ToList();

            CollectionAssert.Contains(comparison, "jako", "Primární čtení.");
            CollectionAssert.Contains(comparison, "jak", "Jak je primárně souřadicí, srovnávací je až druhé čtení.");

            Assert.IsTrue(
                comparison.IndexOf("jako") < comparison.IndexOf("jak"),
                "Primární čtení má předcházet druhotnému.");
        }

        /// <summary>
        /// Coordination is looked up the same way.
        /// </summary>
        [TestMethod]
        public void GetConjunctionsFor_CoordinatingRelation_Resolves()
        {
            var adversative = conjunctions.GetConjunctionsFor(ConjunctionType.Coordinating, ConjunctionSemanticGroup.Adversative);
            var disjunctive = conjunctions.GetConjunctionsFor(ConjunctionType.Coordinating, ConjunctionSemanticGroup.Disjunctive);

            Assert.AreEqual("ale", adversative[0]);
            Assert.AreEqual("nebo", disjunctive[0]);
        }

        /// <summary>
        /// A combination nothing expresses comes back empty rather than throwing — this is a query.
        /// </summary>
        [TestMethod]
        public void GetConjunctionsFor_UnexpressedCombination_IsEmpty()
        {
            Assert.AreEqual(0, conjunctions.GetConjunctionsFor(ConjunctionType.Coordinating, ConjunctionSemanticGroup.Purpose).Count);
        }

        /// <summary>
        /// The whole point, end to end: name the kind of clause, get a sentence, without naming the word.
        /// </summary>
        [TestMethod]
        public void Build_ClauseChosenByItsKind_ProducesTheSentence()
        {
            CzechWordRequest Verb() => new()
            {
                Lemma = "dělat",
                Pattern = "dělá",
                WordCategory = WordCategory.Verb,
                Modus = Modus.Indicative,
                Tense = Tense.Past,
                Aspect = VerbAspect.Imperfective,
                Voice = Voice.Active,
                Person = Person.Third,
                Number = Number.Singular,
                Gender = Gender.Masculine
            };

            var student = ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = "student",
                    Pattern = "pán",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Masculine,
                    IsAnimate = true,
                    Number = Number.Singular,
                    Case = Case.Nominative
                },
                FgdFunctor.ACT,
                InformationStatus.Given);

            var causal = conjunctions.GetConjunctionsFor(ConjunctionType.Subordinating, ConjunctionSemanticGroup.Causal)[0];

            var sentence = new Subordination(
                new CzechClause { Predicate = Verb(), Elements = [student] },
                causal,
                new CzechClause { Predicate = Verb() });

            Assert.AreEqual("Student dělal, protože dělal.", builder.Build(sentence));
        }
    }
}
