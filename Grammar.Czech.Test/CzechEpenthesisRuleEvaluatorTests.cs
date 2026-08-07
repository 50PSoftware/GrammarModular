using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Services;
using System.Reflection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies czech epenthesis rule evaluator behavior.
    /// </summary>
    [TestClass]
    public class CzechEpenthesisRuleEvaluatorTests
    {
        private IEpenthesisRuleEvaluator<CzechWordRequest> _evaluator;

        /// <summary>
        /// Creates the test subject and its dependencies.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            var registry = new CzechPhonemeRegistry();
            _evaluator = new CzechEpenthesisRuleEvaluator(registry);
        }

        // -------------------------------------------------------------------------

        #region ReturnsTrue — heterorganní shluky bez asimilace

        /// <summary>
        /// Determines whether should apply epenthesis heterorganic cluster gen pl returns true.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="suffix">The suffix to attach or evaluate.</param>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="pattern">The inflection pattern used to choose the rule.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        [TestMethod]
        [EpenthesisReturnsTrue]
        public void ShouldApplyEpenthesis_HeterorganicCluster_GenPl_ReturnsTrue(
            string stem, string suffix, string lemma, string pattern, Gender gender)
        {
            // Arrange
            var request = BuildRequest(lemma, pattern, gender, Case.Genitive, Number.Plural);

            // Act
            var result = _evaluator.ShouldApplyEpenthesis(stem, suffix, request);

            // Assert
            Assert.IsTrue(result, $"Očekávána epentheze: {stem}+{suffix} → gen.pl. ({lemma})");
        }

        #endregion ReturnsTrue — heterorganní shluky bez asimilace

        // -------------------------------------------------------------------------

        #region ReturnsFalse — fonologicky stabilní shluky

        /// <summary>
        /// Determines whether should apply epenthesis stable cluster gen pl returns false.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="suffix">The suffix to attach or evaluate.</param>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="pattern">The inflection pattern used to choose the rule.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        [TestMethod]
        [EpenthesisReturnsFalse]
        public void ShouldApplyEpenthesis_StableCluster_GenPl_ReturnsFalse(
            string stem, string suffix, string lemma, string pattern, Gender gender)
        {
            // Arrange
            var request = BuildRequest(lemma, pattern, gender, Case.Genitive, Number.Plural);

            // Act
            var result = _evaluator.ShouldApplyEpenthesis(stem, suffix, request);

            // Assert
            Assert.IsFalse(result, $"Epentheze se nesmí aplikovat: {stem}+{suffix} ({lemma})");
        }

        #endregion ReturnsFalse — fonologicky stabilní shluky

        // -------------------------------------------------------------------------

        #region Guard clauses

        /// <summary>
        /// Determines whether should apply epenthesis gen sg returns false.
        /// </summary>
        [TestMethod]
        public void ShouldApplyEpenthesis_GenSg_ReturnsFalse()
        {
            // Epentheze nastává pouze v gen.pl., ne v gen.sg.
            var request = BuildRequest("matka", "žena", Gender.Feminine, Case.Genitive, Number.Singular);

            var result = _evaluator.ShouldApplyEpenthesis("mat", "k", request);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Determines whether should apply epenthesis dat pl returns false.
        /// </summary>
        [TestMethod]
        public void ShouldApplyEpenthesis_DatPl_ReturnsFalse()
        {
            var request = BuildRequest("matka", "žena", Gender.Feminine, Case.Dative, Number.Plural);

            var result = _evaluator.ShouldApplyEpenthesis("mat", "k", request);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Determines whether should apply epenthesis adjective category returns false.
        /// </summary>
        [TestMethod]
        public void ShouldApplyEpenthesis_AdjectiveCategory_ReturnsFalse()
        {
            var request = new CzechWordRequest
            {
                Lemma = "mladičký",
                WordCategory = WordCategory.Adjective,
                Case = Case.Genitive,
                Number = Number.Plural
            };

            var result = _evaluator.ShouldApplyEpenthesis("mladič", "k", request);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Determines whether should apply epenthesis stem ends with vowel returns false.
        /// </summary>
        [TestMethod]
        public void ShouldApplyEpenthesis_StemEndsWithVowel_ReturnsFalse()
        {
            var request = BuildRequest("žena", "žena", Gender.Feminine, Case.Genitive, Number.Plural);

            var result = _evaluator.ShouldApplyEpenthesis("žena", "k", request);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Determines whether should apply epenthesis suffix starts with vowel returns false.
        /// </summary>
        [TestMethod]
        public void ShouldApplyEpenthesis_SuffixStartsWithVowel_ReturnsFalse()
        {
            var request = BuildRequest("matka", "žena", Gender.Feminine, Case.Genitive, Number.Plural);

            var result = _evaluator.ShouldApplyEpenthesis("mat", "ám", request);

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Determines whether should apply epenthesis empty stem returns false.
        /// </summary>
        [TestMethod]
        public void ShouldApplyEpenthesis_EmptyStem_ReturnsFalse()
        {
            var result = _evaluator.ShouldApplyEpenthesis(string.Empty, "k", new CzechWordRequest());

            Assert.IsFalse(result);
        }

        /// <summary>
        /// Determines whether should apply epenthesis empty suffix returns false.
        /// </summary>
        [TestMethod]
        public void ShouldApplyEpenthesis_EmptySuffix_ReturnsFalse()
        {
            var result = _evaluator.ShouldApplyEpenthesis("mat", string.Empty, new CzechWordRequest());

            Assert.IsFalse(result);
        }

        #endregion Guard clauses

        // -------------------------------------------------------------------------

        #region Helpers

        private static CzechWordRequest BuildRequest(
            string lemma, string pattern, Gender gender, Case @case, Number number) =>
            new()
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Noun,
                Gender = gender,
                Case = @case,
                Number = number
            };

        #endregion Helpers

        // -------------------------------------------------------------------------

        #region Test data attributes

        /// <summary>
        /// Provides epenthesis returns true attribute behavior.
        /// </summary>
        private sealed class EpenthesisReturnsTrueAttribute : TestAttributeBase
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo) =>
            [
                // vzor žena + sufix -ka: shluk C+k (Alveolar/Bilabial → Velar)
                ["mat",     "k", "matka",     "žena",  Gender.Feminine],
                ["student", "k", "studentka", "žena",  Gender.Feminine],
                ["bab",     "k", "babka",     "žena",  Gender.Feminine],
                ["bran", "k", "branka", "žena", Gender.Feminine],
                ["vzpomín", "k", "vzpomínka", "žena", Gender.Feminine],
                ["kres", "b", "kresba", "žena", Gender.Feminine],
                ["sest","r", "sestra", "žena", Gender.Feminine],
                ["leb","k", "lebka", "žena", Gender.Feminine],
                // vzor město: strukturní sufix — různé typy heterorganních shluků
                ["ok",   "n", "okno",   "město", Gender.Neuter],  // k+n  Velar+Alveolar
                ["jabl", "k", "jablko", "město", Gender.Neuter],  // l+k  Alveolar+Velar
                ["pek",  "l", "peklo",  "město", Gender.Neuter],  // k+l  Velar+Alveolar
                ["vlák", "n", "vlákno", "město", Gender.Neuter],  // k+n  Velar+Alveolar
            ];
        }

        /// <summary>
        /// Provides epenthesis returns false attribute behavior.
        /// </summary>
        private sealed class EpenthesisReturnsFalseAttribute : TestAttributeBase
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo) =>
            [
                // Homorganní: s+t — oba Alveolar
                ["měs", "t", "město", "město", Gender.Neuter],
                // Digraf, ne shluk: ch je jeden foném, takže moucha dává much, ne *mouceh.
                ["mouc", "h", "moucha", "žena", Gender.Feminine],
            ];
        }

        #endregion Test data attributes
    }
}
