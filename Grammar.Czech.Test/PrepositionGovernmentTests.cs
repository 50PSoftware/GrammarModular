using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies that preposition metadata is actually loaded and case government reads back from it.
    /// </summary>
    [TestClass]
    public sealed class PrepositionGovernmentTests
    {
        private static ICzechPrepositionService service = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            service = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })
                              .GetRequiredService<ICzechPrepositionService>();
        }

        /// <summary>
        /// Case government per preposition, which also proves the variants deserialize at all.
        /// </summary>
        /// <param name="preposition">The preposition to look up.</param>
        /// <param name="case">The case it is expected to govern.</param>
        [DataTestMethod]
        [DataRow("v", "Locative")]
        // v also governs the accusative, in temporal use: v pondělí, v deset hodin
        [DataRow("v", "Accusative")]
        [DataRow("na", "Locative")]
        [DataRow("na", "Accusative")]
        [DataRow("do", "Genitive")]
        [DataRow("z", "Genitive")]
        [DataRow("k", "Dative")]
        [DataRow("s", "Instrumental")]
        [DataRow("kvůli", "Dative")]
        public void IsAllowed_GovernedCase_ReturnsTrue(string preposition, string @case)
        {
            Assert.IsTrue(service.IsAllowed(preposition, Enum.Parse<Case>(@case)));
        }

        /// <summary>
        /// A case the preposition does not govern is rejected.
        /// </summary>
        [TestMethod]
        public void IsAllowed_UngovernedCase_ReturnsFalse()
        {
            Assert.IsFalse(service.IsAllowed("v", Case.Instrumental));
            Assert.IsFalse(service.IsAllowed("do", Case.Dative));
        }

        /// <summary>
        /// The semantic group distinguishes the two variants of na.
        /// </summary>
        [TestMethod]
        public void GetSemanticGroup_TwoCaseVariants_AreDistinguished()
        {
            Assert.AreEqual(PrepositionSemanticGroup.Direction, service.GetSemanticGroup("na", Case.Accusative));
            Assert.AreEqual(PrepositionSemanticGroup.Location, service.GetSemanticGroup("na", Case.Locative));
        }
    }
}
