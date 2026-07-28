using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Services;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies that <see cref="CzechCliticService.GetReflexive"/> maps every
    /// <see cref="ReflexiveType"/> value to the correct Czech particle.
    /// </summary>
    [TestClass]
    public sealed class CzechCliticServiceTests
    {
        private CzechCliticService service = null!;

        /// <summary>
        /// Creates the service under test with its JSON data provider.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            service = new CzechCliticService(new JsonCliticsDataProvider());
        }

        #region Accusative particle — se

        /// <summary>
        /// All reflexive types that require the accusative particle return "se".
        /// </summary>
        [DataTestMethod]
        [DataRow("ReflexivumTantum_Se",   "se", DisplayName = "ReflexivumTantum_Se → se")]
        [DataRow("DerivedReflexive_Se",   "se", DisplayName = "DerivedReflexive_Se → se")]
        [DataRow("Reciprocal_Se",         "se", DisplayName = "Reciprocal_Se → se")]
        [DataRow("DeagentivePassive_Se",  "se", DisplayName = "DeagentivePassive_Se → se")]
        public void GetReflexive_SeType_ReturnsSe(string reflexiveType, string expected)
        {
            var result = service.GetReflexive(Enum.Parse<ReflexiveType>(reflexiveType));

            Assert.AreEqual(expected, result);
        }

        #endregion Accusative particle — se

        #region Dative particle — si

        /// <summary>
        /// All reflexive types that require the dative particle return "si".
        /// </summary>
        [DataTestMethod]
        [DataRow("ReflexivumTantum_Si",   "si", DisplayName = "ReflexivumTantum_Si → si")]
        [DataRow("DerivedBenefactive_Si", "si", DisplayName = "DerivedBenefactive_Si → si")]
        public void GetReflexive_SiType_ReturnsSi(string reflexiveType, string expected)
        {
            var result = service.GetReflexive(Enum.Parse<ReflexiveType>(reflexiveType));

            Assert.AreEqual(expected, result);
        }

        #endregion Dative particle — si

        #region Guard — None

        /// <summary>
        /// Passing <see cref="ReflexiveType.None"/> is a caller error and must throw.
        /// </summary>
        [TestMethod]
        public void GetReflexive_None_ThrowsArgumentOutOfRangeException()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => service.GetReflexive(ReflexiveType.None));
        }

        #endregion Guard — None
    }
}
