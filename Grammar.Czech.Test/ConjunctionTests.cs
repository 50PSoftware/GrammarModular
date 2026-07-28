using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the conjunction inventory: how each conjunction joins, what relation it marks, where it
    /// stands, and which ones absorb the conditional auxiliary.
    /// </summary>
    [TestClass]
    public sealed class ConjunctionTests
    {
        private static ICzechConjunctionService service = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            service = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })
                              .GetRequiredService<ICzechConjunctionService>();
        }

        #region Typ a sémantický vztah

        /// <summary>
        /// The semantic groups follow the NESČ classification of coordinating conjunctions.
        /// </summary>
        /// <param name="conjunction">The conjunction to look up.</param>
        /// <param name="group">The expected semantic group.</param>
        [DataTestMethod]
        // slučovací
        [DataRow("a", "Copulative")]
        [DataRow("i", "Copulative")]
        [DataRow("ani", "Copulative")]
        // odporovací
        [DataRow("ale", "Adversative")]
        [DataRow("avšak", "Adversative")]
        [DataRow("však", "Adversative")]
        [DataRow("leč", "Adversative")]
        [DataRow("nýbrž", "Adversative")]
        [DataRow("sice", "Adversative")]
        // stupňovací
        [DataRow("ba", "Gradational")]
        // vylučovací
        [DataRow("nebo", "Disjunctive")]
        [DataRow("či", "Disjunctive")]
        // příčinné — souřadicí jen neboť
        [DataRow("neboť", "Causal")]
        // důsledkové
        [DataRow("tedy", "Consecutive")]
        [DataRow("tudíž", "Consecutive")]
        [DataRow("tak", "Consecutive")]
        public void GetSemanticGroup_CoordinatingConjunction_MatchesTheNescClassification(
            string conjunction, string group)
        {
            Assert.AreEqual(ConjunctionType.Coordinating, service.GetType(conjunction));
            Assert.AreEqual(Enum.Parse<ConjunctionSemanticGroup>(group), service.GetSemanticGroup(conjunction));
        }

        /// <summary>
        /// And the same for the subordinating ones, which NESČ groups by the role of the dependent clause.
        /// </summary>
        /// <param name="conjunction">The conjunction to look up.</param>
        /// <param name="group">The expected semantic group.</param>
        [DataTestMethod]
        // časové
        [DataRow("když", "Temporal")]
        [DataRow("než", "Temporal")]
        [DataRow("až", "Temporal")]
        [DataRow("dokud", "Temporal")]
        [DataRow("jakmile", "Temporal")]
        // příčinné
        [DataRow("protože", "Causal")]
        [DataRow("poněvadž", "Causal")]
        [DataRow("jelikož", "Causal")]
        // přípustkové
        [DataRow("ač", "Concessive")]
        [DataRow("ačkoli", "Concessive")]
        [DataRow("přestože", "Concessive")]
        [DataRow("třebaže", "Concessive")]
        // obsahové
        [DataRow("že", "Content")]
        [DataRow("ať", "Content")]
        [DataRow("zda", "Content")]
        // podmínkové a účelové
        [DataRow("jestliže", "Conditional")]
        [DataRow("kdyby", "Conditional")]
        [DataRow("aby", "Purpose")]
        public void GetSemanticGroup_SubordinatingConjunction_MatchesTheNescClassification(
            string conjunction, string group)
        {
            Assert.AreEqual(ConjunctionType.Subordinating, service.GetType(conjunction));
            Assert.AreEqual(Enum.Parse<ConjunctionSemanticGroup>(group), service.GetSemanticGroup(conjunction));
        }

        #endregion Typ a sémantický vztah

        #region Párové spojky

        /// <summary>
        /// The first member of a pair decides which second member follows, so the pairing is recorded on it.
        /// </summary>
        /// <param name="conjunction">The opening member.</param>
        /// <param name="correlate">The expected second member.</param>
        [DataTestMethod]
        [DataRow("buď", "nebo")]
        [DataRow("ani", "ani")]
        [DataRow("nejen", "ale i")]
        [DataRow("nejenže", "ale i")]
        [DataRow("jednak", "jednak")]
        [DataRow("jak", "tak")]
        [DataRow("sice", "ale")]
        public void GetCorrelate_PairedConjunction_ReturnsTheSecondMember(string conjunction, string correlate)
            => Assert.AreEqual(correlate, service.GetCorrelate(conjunction));

        /// <summary>
        /// NESČ is explicit that doubles exist only among the coordinating conjunctions — there is no paired
        /// subordinator — so nothing subordinating carries a correlate.
        /// </summary>
        [TestMethod]
        public void GetCorrelate_SubordinatingConjunction_IsNeverPaired()
        {
            string[] subordinators = ["že", "protože", "když", "aby", "kdyby", "ačkoli", "jestliže", "zda"];

            foreach (var conjunction in subordinators)
            {
                Assert.IsNull(
                    service.GetCorrelate(conjunction),
                    $"Podřadicí spojka '{conjunction}' nemůže být párová.");
            }
        }

        #endregion Párové spojky

        #region Postavení ve větě

        /// <summary>
        /// A subordinating conjunction fills the first position of its clause and a coordinating one does
        /// not, which is what decides where the clitic cluster attaches.
        /// </summary>
        [TestMethod]
        public void OccupiesFirstPosition_FollowsTheConjunctionType()
        {
            Assert.IsTrue(service.OccupiesFirstPosition("protože"));
            Assert.IsTrue(service.OccupiesFirstPosition("aby"));
            Assert.IsFalse(service.OccupiesFirstPosition("a"));
            Assert.IsFalse(service.OccupiesFirstPosition("ale"));
        }

        /// <summary>
        /// však is the one conjunction that cannot open its clause; avšak, its non-enclitic twin, always does.
        /// </summary>
        [TestMethod]
        public void OccupiesSecondPosition_IsTrueForVsakAlone()
        {
            Assert.IsTrue(service.OccupiesSecondPosition("však"));
            Assert.IsFalse(service.OccupiesSecondPosition("avšak"));
            Assert.IsFalse(service.OccupiesSecondPosition("ale"));
        }

        #endregion Postavení ve větě

        #region Fúze s kondicionálem

        /// <summary>
        /// aby and kdyby are the only two conjunctions that absorb the conditional auxiliary.
        /// </summary>
        [TestMethod]
        public void FusesWithConditional_IsTrueForAbyAndKdybyAlone()
        {
            Assert.IsTrue(service.FusesWithConditional("aby"));
            Assert.IsTrue(service.FusesWithConditional("kdyby"));
            Assert.IsFalse(service.FusesWithConditional("že"));
            Assert.IsFalse(service.FusesWithConditional("když"));
            Assert.IsFalse(service.FusesWithConditional("jestliže"));
        }

        /// <summary>
        /// The twelve fused forms are composed from the stem and the conditional particle, not stored.
        /// </summary>
        /// <param name="conjunction">The fusing conjunction.</param>
        /// <param name="number">The grammatical number.</param>
        /// <param name="person">The grammatical person.</param>
        /// <param name="expected">The expected fused form.</param>
        [DataTestMethod]
        [DataRow("aby", "Singular", "First", "abych")]
        [DataRow("aby", "Singular", "Second", "abys")]
        [DataRow("aby", "Singular", "Third", "aby")]
        [DataRow("aby", "Plural", "First", "abychom")]
        [DataRow("aby", "Plural", "Second", "abyste")]
        [DataRow("aby", "Plural", "Third", "aby")]
        [DataRow("kdyby", "Singular", "First", "kdybych")]
        [DataRow("kdyby", "Singular", "Second", "kdybys")]
        [DataRow("kdyby", "Singular", "Third", "kdyby")]
        [DataRow("kdyby", "Plural", "First", "kdybychom")]
        [DataRow("kdyby", "Plural", "Second", "kdybyste")]
        [DataRow("kdyby", "Plural", "Third", "kdyby")]
        public void GetForm_FusingConjunction_ComposesTheParadigm(
            string conjunction, string number, string person, string expected)
        {
            Assert.AreEqual(
                expected,
                service.GetForm(conjunction, Enum.Parse<Number>(number), Enum.Parse<Person>(person)));
        }

        /// <summary>
        /// The widespread *aby jsi and *aby jste cannot arise: what fuses here is the conditional auxiliary,
        /// and the conditional paradigm has no jsi or jste to contribute.
        /// </summary>
        [TestMethod]
        public void GetForm_FusedForms_NeverContainThePastAuxiliary()
        {
            string[] conjunctions = ["aby", "kdyby"];
            Number[] numbers = [Number.Singular, Number.Plural];
            Person[] persons = [Person.First, Person.Second, Person.Third];

            var forms = conjunctions
                .SelectMany(_ => numbers, (conjunction, number) => (conjunction, number))
                .SelectMany(_ => persons, (pair, person) => service.GetForm(pair.conjunction, pair.number, person));

            foreach (var form in forms)
            {
                Assert.IsFalse(form.Contains("js"), $"Tvar '{form}' obsahuje minulé pomocné sloveso.");
            }
        }

        /// <summary>
        /// A conjunction that does not fuse ignores person and number entirely.
        /// </summary>
        [TestMethod]
        public void GetForm_NonFusingConjunction_ReturnsItUnchanged()
        {
            Assert.AreEqual("protože", service.GetForm("protože", Number.Plural, Person.First));
            Assert.AreEqual("a", service.GetForm("a", Number.Singular, Person.Second));
        }

        /// <summary>
        /// With nothing to agree with, the fused conjunction falls back on the third-person form, which is
        /// the lemma itself.
        /// </summary>
        [TestMethod]
        public void GetForm_FusingConjunctionWithoutAgreement_ReturnsTheLemma()
        {
            Assert.AreEqual("aby", service.GetForm("aby", null, null));
            Assert.AreEqual("kdyby", service.GetForm("kdyby", Number.Singular, null));
        }

        #endregion Fúze s kondicionálem

        #region Inventář

        /// <summary>
        /// Conjunctions are a closed class, so an unknown one is reported with the file to add it to.
        /// </summary>
        [TestMethod]
        public void Lookup_UnknownConjunction_Throws()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(() => service.GetType("jakožto"));

            StringAssert.Contains(exception.Message, "jakožto");
            StringAssert.Contains(exception.Message, "conjunctions.json");
        }

        #endregion Inventář

        #region Homonymní spojky

        /// <summary>
        /// Some conjunctions are two words under one spelling, and the readings differ in the type itself.
        /// A bare lookup gives the primary reading; a caller that knows the construction asks for the other.
        /// </summary>
        [TestMethod]
        public void Lookup_At_SubordinatesByDefaultAndCoordinatesWhenAsked()
        {
            // "Řekni mu, ať přijde" — obsahová věta
            Assert.AreEqual(ConjunctionType.Subordinating, service.GetType("ať"));
            Assert.AreEqual(ConjunctionSemanticGroup.Content, service.GetSemanticGroup("ať"));
            Assert.IsTrue(service.OccupiesFirstPosition("ať"));

            // "ať už přijde osobně, nebo vyšle zástupce" — rozštěpená vylučovací spojka
            Assert.AreEqual(ConjunctionType.Coordinating, service.GetType("ať", ConjunctionType.Coordinating));
            Assert.AreEqual(
                ConjunctionSemanticGroup.Disjunctive,
                service.GetSemanticGroup("ať", ConjunctionType.Coordinating));
            Assert.AreEqual("nebo", service.GetCorrelate("ať", ConjunctionType.Coordinating));
        }

        /// <summary>
        /// And the other way round for jak, whose primary reading is the paired coordinator.
        /// </summary>
        [TestMethod]
        public void Lookup_Jak_CoordinatesByDefaultAndSubordinatesWhenAsked()
        {
            // "jak Petr, tak Pavel"
            Assert.AreEqual(ConjunctionType.Coordinating, service.GetType("jak"));
            Assert.AreEqual("tak", service.GetCorrelate("jak"));

            // "udělej to, jak jsem řekl"
            Assert.AreEqual(
                ConjunctionSemanticGroup.Comparison,
                service.GetSemanticGroup("jak", ConjunctionType.Subordinating));
        }

        /// <summary>
        /// The subordinating reading of ať is genuinely unpaired, which is what NESČ says of every
        /// subordinator — the pairing belongs to the coordinating reading and to nothing else.
        /// </summary>
        [TestMethod]
        public void GetCorrelate_SubordinatingReadingOfAHomonym_IsStillNull()
            => Assert.IsNull(service.GetCorrelate("ať", ConjunctionType.Subordinating));

        /// <summary>
        /// Readings come back with the primary one first.
        /// </summary>
        [TestMethod]
        public void GetReadings_ReturnsThePrimaryReadingFirst()
        {
            var readings = service.GetReadings("ať");

            Assert.AreEqual(2, readings.Count);
            Assert.AreEqual(ConjunctionType.Subordinating, readings[0].Type);
            Assert.AreEqual(ConjunctionType.Coordinating, readings[1].Type);

            Assert.AreEqual(1, service.GetReadings("protože").Count);
        }

        /// <summary>
        /// Asking for a reading a conjunction does not have is reported rather than answered with the one it
        /// does have, which would put a subordinator where a coordinator was meant.
        /// </summary>
        [TestMethod]
        public void Lookup_ReadingTheConjunctionDoesNotHave_Throws()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => service.GetType("protože", ConjunctionType.Coordinating));

            StringAssert.Contains(exception.Message, "protože");
            StringAssert.Contains(exception.Message, "alsoReads");
        }

        #endregion Homonymní spojky
    }
}
