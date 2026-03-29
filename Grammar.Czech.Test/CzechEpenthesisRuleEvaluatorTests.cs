using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Services;
using System.Reflection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Tests for <see cref="CzechEpenthesisRuleEvaluator"/>.
    ///
    /// Organisation:
    ///   1. ReturnsTrue  — heterorganní shluky bez asimilace (epentheze musí nastat)
    ///   2. ReturnsFalse — fonologicky stabilní shluky (epentheze nesmí nastat)
    ///   3. Guard clauses — nesprávný pád, číslo, slovní druh, prázdné vstupy
    /// </summary>
    [TestClass]
    public class CzechEpenthesisRuleEvaluatorTests
    {
        private IEpenthesisRuleEvaluator<CzechWordRequest> _evaluator;

        [TestInitialize]
        public void Setup()
        {
            var registry = new CzechPhonemeRegistry();
            _evaluator = new CzechEpenthesisRuleEvaluator(registry);
        }

        // -------------------------------------------------------------------------

        #region ReturnsTrue — heterorganní shluky bez asimilace

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

        [TestMethod]
        public void ShouldApplyEpenthesis_GenSg_ReturnsFalse()
        {
            // Epentheze nastává pouze v gen.pl., ne v gen.sg.
            var request = BuildRequest("matka", "žena", Gender.Feminine, Case.Genitive, Number.Singular);

            var result = _evaluator.ShouldApplyEpenthesis("mat", "k", request);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldApplyEpenthesis_DatPl_ReturnsFalse()
        {
            var request = BuildRequest("matka", "žena", Gender.Feminine, Case.Dative, Number.Plural);

            var result = _evaluator.ShouldApplyEpenthesis("mat", "k", request);

            Assert.IsFalse(result);
        }

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

        [TestMethod]
        public void ShouldApplyEpenthesis_StemEndsWithVowel_ReturnsFalse()
        {
            var request = BuildRequest("žena", "žena", Gender.Feminine, Case.Genitive, Number.Plural);

            var result = _evaluator.ShouldApplyEpenthesis("žena", "k", request);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldApplyEpenthesis_SuffixStartsWithVowel_ReturnsFalse()
        {
            var request = BuildRequest("matka", "žena", Gender.Feminine, Case.Genitive, Number.Plural);

            var result = _evaluator.ShouldApplyEpenthesis("mat", "ám", request);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldApplyEpenthesis_EmptyStem_ReturnsFalse()
        {
            var result = _evaluator.ShouldApplyEpenthesis(string.Empty, "k", new CzechWordRequest());

            Assert.IsFalse(result);
        }

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
        /// Heterorganní shluky bez obligatorní asimilace — epentheze musí nastat.
        /// Sloupce: stem, suffix, lemma, pattern, gender.
        /// </summary>
        private sealed class EpenthesisReturnsTrueAttribute : TestAttributeBase
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo) =>
            [
                // vzor žena + sufix -ka: shluk C+k (Alveolar/Bilabial → Velar)
                ["mat",     "k", "matka",     "žena",  Gender.Feminine],
                ["student", "k", "studentka", "žena",  Gender.Feminine],
                ["bab",     "k", "babka",     "žena",  Gender.Feminine],
                // vzor město: strukturní sufix — různé typy heterorganních shluků
                ["ok",   "n", "okno",   "město", Gender.Neuter],  // k+n  Velar+Alveolar
                ["jabl", "k", "jablko", "město", Gender.Neuter],  // l+k  Alveolar+Velar
                ["pek",  "l", "peklo",  "město", Gender.Neuter],  // k+l  Velar+Alveolar
                ["vlák", "n", "vlákno", "město", Gender.Neuter],  // k+n  Velar+Alveolar
            ];
        }

        /// <summary>
        /// Fonologicky stabilní shluky — epentheze nesmí nastat.
        /// Homorganní (st) nebo obligatorní place asimilace (nk → [ŋk]).
        /// Sloupce: stem, suffix, lemma, pattern, gender.
        /// </summary>
        private sealed class EpenthesisReturnsFalseAttribute : TestAttributeBase
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo) =>
            [
                // Homorganní: s+t — oba Alveolar
                ["měs", "t", "město", "město", Gender.Neuter],
                // Obligatorní place asimilace: n → [ŋ] před Velar k
                ["ban", "k", "banka", "žena",  Gender.Feminine],
                ["ran", "k", "ranka", "žena",  Gender.Feminine],
            ];
        }

        #endregion Test data attributes
    }
}
