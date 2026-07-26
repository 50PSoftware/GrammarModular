using Grammar.Czech.Helpers;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies morphology helper behavior.
    /// </summary>
    [TestClass]
    public sealed class MorphologyHelperTests
    {
        /// <summary>
        /// Verifies that lemmas with a productive mobile-e suffix are recognized.
        /// </summary>
        /// <param name="lemma">The dictionary form to classify.</param>
        [TestMethod]
        [DataRow("otec")]
        [DataRow("chlapec")]
        [DataRow("domek")]
        [DataRow("větev")]
        [DataRow("švec")]
        public void HasLikelyMobileE_ProductiveSuffix_ReturnsTrue(string lemma)
        {
            Assert.IsTrue(MorphologyHelper.HasLikelyMobileE(lemma));
        }

        /// <summary>
        /// Verifies that latinate -or nouns and other vowel-consonant tails are not treated as mobile-e stems.
        /// </summary>
        /// <param name="lemma">The dictionary form to classify.</param>
        [TestMethod]
        [DataRow("doktor")]
        [DataRow("profesor")]
        [DataRow("Mendominátor")]
        [DataRow("sever")]
        [DataRow("kmen")]
        [DataRow("hrad")]
        [DataRow("ec")]
        public void HasLikelyMobileE_NoProductiveSuffix_ReturnsFalse(string lemma)
        {
            Assert.IsFalse(MorphologyHelper.HasLikelyMobileE(lemma));
        }
    }
}
