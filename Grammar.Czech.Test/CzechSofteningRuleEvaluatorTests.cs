using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Services;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies czech softening rule evaluator behavior.
    /// </summary>
    [TestClass]
    public class CzechSofteningRuleEvaluatorTests
    {
        /// <summary>
        /// The resolved stem of the lemma "holka" that every case below is built on.
        /// </summary>
        private const string HolkaStem = "holk";

        private ISofteningRuleEvaluator<CzechWordRequest> softeningRuleEvaluator;

        /// <summary>
        /// Creates the test subject and its dependencies.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            softeningRuleEvaluator = new CzechSofteningRuleEvaluator(new JsonNounDataProvider());
        }

        /// <summary>
        /// Verifies that a rule keyed on the surface shape follows the resolved stem, not the lemma.
        /// </summary>
        /// <remarks>
        /// The lemma "doktor" ends in a vowel plus r, which on its own would not trigger the vocative
        /// palatalization. The stem passed in ends in a consonant plus r, and that is what decides.
        /// No noun currently declines this way — the pair is synthetic and exists to pin the contract,
        /// so a future alternation that reshapes the stem cannot silently bypass the rule.
        /// </remarks>
        [TestMethod]
        public void ShouldApplySoftening_VocativeSg_FollowsStemNotLemma()
        {
            var request = new CzechWordRequest
            {
                Lemma = "doktor",
                Pattern = "pán",
                WordCategory = WordCategory.Noun,
                Number = Number.Singular,
                Case = Case.Vocative
            };

            Assert.IsTrue(softeningRuleEvaluator.ShouldApplySoftening(request, "doktr", out _), "Kmen na souhlásku + r má změkčovat.");
            Assert.AreEqual("-e", softeningRuleEvaluator.GetEndingTransformation(request, "doktr", out _));

            Assert.IsFalse(softeningRuleEvaluator.ShouldApplySoftening(request, "doktor", out _), "Kmen na samohlásku + r měkčit nemá.");
            Assert.IsNull(softeningRuleEvaluator.GetEndingTransformation(request, "doktor", out _));
        }

        /// <summary>
        /// Gets ending transformation should return e for holka dative singular.
        /// </summary>
        [TestMethod]
        public void GetEndingTransformation_ShouldReturnE_ForHolkaDativeSingular()
        {
            var request = new CzechWordRequest
            {
                Lemma = "holka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Number = Number.Singular,
                Case = Case.Dative
            };
            var result = softeningRuleEvaluator.GetEndingTransformation(request, HolkaStem, out _);
            Assert.AreEqual("-e", result);
        }

        /// <summary>
        /// Gets ending transformation should return e for holka locative singular.
        /// </summary>
        [TestMethod]
        public void GetEndingTransformation_ShouldReturnE_ForHolkaLocativeSingular()
        {
            var request = new CzechWordRequest
            {
                Lemma = "holka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Number = Number.Singular,
                Case = Case.Locative
            };
            var result = softeningRuleEvaluator.GetEndingTransformation(request, HolkaStem, out _);
            Assert.AreEqual("-e", result);
        }

        /// <summary>
        /// Determines whether should apply softening should return false for holka genitive plural.
        /// </summary>
        [TestMethod]
        public void ShouldApplySoftening_ShouldReturnFalse_ForHolkaGenitivePlural()
        {
            var request = new CzechWordRequest
            {
                Lemma = "holka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Number = Number.Plural,
                Case = Case.Genitive
            };
            var result = softeningRuleEvaluator.ShouldApplySoftening(request, HolkaStem, out _);
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Gets ending transformation should return null for holka nominative singular.
        /// </summary>
        [TestMethod]
        public void GetEndingTransformation_ShouldReturnNull_ForHolkaNominativeSingular()
        {
            var request = new CzechWordRequest
            {
                Lemma = "holka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Number = Number.Singular,
                Case = Case.Nominative
            };
            var result = softeningRuleEvaluator.GetEndingTransformation(request, HolkaStem, out _);
            Assert.IsNull(result);
        }

        /// <summary>
        /// Determines whether should apply softening should return false for holka nominative singular.
        /// </summary>
        [TestMethod]
        public void ShouldApplySoftening_ShouldReturnFalse_ForHolkaNominativeSingular()
        {
            var request = new CzechWordRequest
            {
                Lemma = "holka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Number = Number.Singular,
                Case = Case.Nominative
            };
            var result = softeningRuleEvaluator.ShouldApplySoftening(request, HolkaStem, out _);
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Determines whether should apply softening should return true for holka dative singular.
        /// </summary>
        [TestMethod]
        public void ShouldApplySoftening_ShouldReturnTrue_ForHolkaDativeSingular()
        {
            var request = new CzechWordRequest
            {
                Lemma = "holka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Number = Number.Singular,
                Case = Case.Dative
            };
            var result = softeningRuleEvaluator.ShouldApplySoftening(request, HolkaStem, out _);
            Assert.IsTrue(result);
        }
    }
}
