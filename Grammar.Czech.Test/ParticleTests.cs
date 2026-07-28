using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the inventory of the word class <em>částice</em>: what each particle does, where it stands,
    /// and how it overlaps with the other uninflected classes.
    /// </summary>
    [TestClass]
    public sealed class ParticleTests
    {
        private static ICzechParticleService service = null!;
        private static ICzechCliticService cliticService = null!;
        private static ICzechAdverbService adverbService = null!;
        private static ICzechConjunctionService conjunctionService = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            service = provider.GetRequiredService<ICzechParticleService>();
            cliticService = provider.GetRequiredService<ICzechCliticService>();
            adverbService = provider.GetRequiredService<ICzechAdverbService>();
            conjunctionService = provider.GetRequiredService<ICzechConjunctionService>();
        }

        #region Funkce částice

        /// <summary>
        /// The types follow the functional-semantic classification NESČ attributes to Nekula in PMČ.
        /// </summary>
        /// <param name="lemma">The particle to look up.</param>
        /// <param name="type">The expected type.</param>
        [DataTestMethod]
        // modální — jistota mluvčího
        [DataRow("asi", "Modal")]
        [DataRow("snad", "Modal")]
        [DataRow("prý", "Modal")]
        [DataRow("možná", "Modal")]
        [DataRow("sotva", "Modal")]
        // intenzifikační — míra vlastnosti
        [DataRow("velmi", "Intensifying")]
        [DataRow("příliš", "Intensifying")]
        [DataRow("zcela", "Intensifying")]
        [DataRow("poněkud", "Intensifying")]
        // vytýkací — výběr z alternativ
        [DataRow("jen", "Focusing")]
        [DataRow("pouze", "Focusing")]
        [DataRow("dokonce", "Focusing")]
        [DataRow("teprve", "Focusing")]
        [DataRow("právě", "Focusing")]
        // modifikační
        [DataRow("přece", "Modifying")]
        [DataRow("prostě", "Modifying")]
        [DataRow("klidně", "Modifying")]
        // odpověďové a negační
        [DataRow("ano", "Response")]
        [DataRow("samozřejmě", "Response")]
        [DataRow("ne", "Negative")]
        [DataRow("nikoli", "Negative")]
        // přací
        [DataRow("ať", "Optative")]
        [DataRow("kéž", "Optative")]
        [DataRow("nechť", "Optative")]
        // strukturující a emocionální
        [DataRow("za prvé", "Structuring")]
        [DataRow("nuže", "Structuring")]
        [DataRow("bohužel", "Emotional")]
        [DataRow("naštěstí", "Emotional")]
        public void GetParticleType_RegisteredParticle_MatchesTheClassification(string lemma, string type)
            => Assert.AreEqual(Enum.Parse<ParticleType>(type), service.GetParticleType(lemma));

        /// <summary>
        /// Every type in the classification is actually populated — an empty group would mean the enum
        /// records a distinction the data does not make.
        /// </summary>
        [TestMethod]
        public void GetParticlesOfType_EveryType_HasEntries()
        {
            foreach (var type in Enum.GetValues<ParticleType>())
            {
                Assert.IsTrue(
                    service.GetParticlesOfType(type).Count > 0,
                    $"Typ {type} nemá v particles.json žádnou částici.");
            }
        }

        #endregion Funkce částice

        #region Postavení

        /// <summary>
        /// The optative particles open their clause; most particles are placed freely.
        /// </summary>
        [TestMethod]
        public void IsClauseInitial_OptativeParticles_OpenTheClause()
        {
            Assert.IsTrue(service.IsClauseInitial("ať"));
            Assert.IsTrue(service.IsClauseInitial("kéž"));
            Assert.IsTrue(service.IsClauseInitial("nechť"));

            Assert.IsFalse(service.IsClauseInitial("jen"));
            Assert.IsFalse(service.IsClauseInitial("asi"));
        }


        /// <summary>
        /// A modifying particle cannot stand in the rheme — the one positional restriction NESČ states of a
        /// whole group, which is why it is derived from the type rather than recorded per word.
        /// </summary>
        [TestMethod]
        public void CanStandInRheme_ModifyingParticles_CannotButOthersCan()
        {
            foreach (var lemma in service.GetParticlesOfType(ParticleType.Modifying))
            {
                Assert.IsFalse(service.CanStandInRheme(lemma), $"Modifikační částice '{lemma}' nesmí být v rématu.");
            }

            Assert.IsTrue(service.CanStandInRheme("jen"));
            Assert.IsTrue(service.CanStandInRheme("asi"));
        }

        #endregion Postavení

        #region Hranice slovního druhu

        /// <summary>
        /// A particle is not a clitic. The two were one file until the clitic layer was renamed, and nothing
        /// in the particle inventory belongs to the second-position cluster.
        /// </summary>
        [TestMethod]
        public void Particles_AreNotClitics()
        {
            foreach (var type in Enum.GetValues<ParticleType>())
            {
                foreach (var lemma in service.GetParticlesOfType(type))
                {
                    Assert.IsFalse(
                        cliticService.IsCliticAuxiliary(lemma),
                        $"'{lemma}' je v particles.json, ale je to klitikon.");
                }
            }
        }

        /// <summary>
        /// The reverse: the clitics are not registered as particles either.
        /// </summary>
        [DataTestMethod]
        [DataRow("bych")]
        [DataRow("bys")]
        [DataRow("by")]
        [DataRow("bychom")]
        [DataRow("byste")]
        [DataRow("jsem")]
        [DataRow("jsi")]
        [DataRow("se")]
        [DataRow("si")]
        public void IsParticle_Clitic_IsNotRegisteredAsAParticle(string lemma)
            => Assert.IsFalse(service.IsParticle(lemma));

        /// <summary>
        /// Homonymy with the adverbs and the conjunctions is expected and is not a fault in the data. NESČ
        /// treats the boundary as drawn by function in context, so one lemma standing in several inventories
        /// records that it has several readings — it is not a duplicate to be removed.
        /// </summary>
        [TestMethod]
        public void Particles_MayAlsoBeRegisteredAsAdverbsOrConjunctions()
        {
            // bohužel: hodnotící částice i příslovce
            Assert.IsTrue(service.IsParticle("bohužel"));
            Assert.IsTrue(adverbService.GetComparativeVariants("bohužel").Count == 0);

            // ať: přací částice i obsahová spojka — NESČ tuhle skupinu výslovně staví na pomezí
            Assert.IsTrue(service.IsParticle("ať"));
            Assert.AreEqual(ConjunctionType.Subordinating, conjunctionService.GetType("ať"));

            // ovšem: odpověďová částice i odporovací spojka
            Assert.IsTrue(service.IsParticle("ovšem"));
        }

        /// <summary>
        /// An unregistered lemma is reported as not being a particle rather than rejected: the class is open
        /// at the edges and a word may be a particle in one context and an adverb in another.
        /// </summary>
        [TestMethod]
        public void IsParticle_UnregisteredLemma_ReturnsFalseWithoutThrowing()
        {
            Assert.IsFalse(service.IsParticle("stůl"));
            Assert.IsFalse(service.IsParticle("běžet"));
        }

        /// <summary>
        /// Asking what an unregistered word does is a different question, and one the data cannot answer.
        /// </summary>
        [TestMethod]
        public void GetParticleType_UnregisteredLemma_Throws()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(() => service.GetParticleType("stůl"));

            StringAssert.Contains(exception.Message, "stůl");
            StringAssert.Contains(exception.Message, "particles.json");
        }

        #endregion Hranice slovního druhu
    }
}
