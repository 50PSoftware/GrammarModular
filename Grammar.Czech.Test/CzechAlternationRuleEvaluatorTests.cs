using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.SqliteProviders;
using Grammar.Czech.Services;
using System.Reflection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies czech alternation rule evaluator behavior.
    /// </summary>
    [TestClass]
    public class CzechAlternationRuleEvaluatorTests
    {
        private IAlternationRuleEvaluator<CzechWordRequest> _evaluator;

        /// <summary>
        /// Creates the test subject and its dependencies.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            var registry = new CzechPhonemeRegistry();
            _evaluator = new CzechAlternationRuleEvaluator(registry, new SqliteValencyProvider());
        }

        // -------------------------------------------------------------------------

        #region ReturnsTrue — heslo krátí a kmen má krátitelnou samohlásku

        /// <summary>
        /// Determines whether should shorten stem flagged lemma gen pl returns true.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="pattern">The inflection pattern used to choose the rule.</param>
        [TestMethod]
        [ShorteningReturnsTrue]
        public void ShouldShortenStem_FlaggedLemma_GenPl_ReturnsTrue(string stem, string lemma, string pattern)
        {
            // Arrange
            var request = BuildRequest(lemma, pattern, Case.Genitive, Number.Plural, shortens: true);

            // Act
            var result = _evaluator.ShouldShortenStem(stem, request);

            // Assert
            Assert.IsTrue(result, $"Očekáváno krácení: {lemma} → gen.pl. z kmene {stem}");
        }

        #endregion ReturnsTrue — heslo krátí a kmen má krátitelnou samohlásku

        // -------------------------------------------------------------------------

        #region ReturnsFalse — samohlásky, které v genitivu plurálu nekrátí

        /// <summary>
        /// Determines whether should shorten stem unshortenable vowel returns false.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="pattern">The inflection pattern used to choose the rule.</param>
        [TestMethod]
        [ShorteningReturnsFalse]
        public void ShouldShortenStem_UnshortenableVowel_ReturnsFalse(string stem, string lemma, string pattern)
        {
            // Arrange — vlajka je schválně zapnutá: veto má držet i proti špatně vyplněnému heslu.
            var request = BuildRequest(lemma, pattern, Case.Genitive, Number.Plural, shortens: true);

            // Act
            var result = _evaluator.ShouldShortenStem(stem, request);

            // Assert
            Assert.IsFalse(result, $"Krácení se nečekalo: {lemma} → gen.pl. z kmene {stem}");
        }

        #endregion ReturnsFalse — samohlásky, které v genitivu plurálu nekrátí

        // -------------------------------------------------------------------------

        #region ReturnsFalse — heslo nekrátí

        /// <summary>
        /// Determines whether should shorten stem unflagged lemma returns false.
        /// </summary>
        [TestMethod]
        public void ShouldShortenStem_UnflaggedLemma_ReturnsFalse()
        {
            var request = BuildRequest("káva", "žena", Case.Genitive, Number.Plural, shortens: false);

            Assert.IsFalse(_evaluator.ShouldShortenStem("káv", request));
        }

        /// <summary>
        /// Determines whether should shorten stem unknown lemma returns false.
        /// </summary>
        [TestMethod]
        public void ShouldShortenStem_UnknownLemma_ReturnsFalse()
        {
            // Slovo, které ve slovníku není, a request bez vlajky — bez odpovědi se nekrátí.
            var request = BuildRequest("mendominátora", "žena", Case.Genitive, Number.Plural, shortens: null);

            Assert.IsFalse(_evaluator.ShouldShortenStem("mendominátor", request));
        }

        #endregion ReturnsFalse — heslo nekrátí

        // -------------------------------------------------------------------------

        #region Guard clauses

        /// <summary>
        /// Determines whether should shorten stem gen sg returns false.
        /// </summary>
        [TestMethod]
        public void ShouldShortenStem_GenSg_ReturnsFalse()
        {
            var request = BuildRequest("kráva", "žena", Case.Genitive, Number.Singular, shortens: true);

            Assert.IsFalse(_evaluator.ShouldShortenStem("kráv", request));
        }

        /// <summary>
        /// Determines whether should shorten stem dat pl returns false.
        /// </summary>
        [TestMethod]
        public void ShouldShortenStem_DatPl_ReturnsFalse()
        {
            var request = BuildRequest("kráva", "žena", Case.Dative, Number.Plural, shortens: true);

            Assert.IsFalse(_evaluator.ShouldShortenStem("kráv", request));
        }

        /// <summary>
        /// Determines whether should shorten stem adjective category returns false.
        /// </summary>
        [TestMethod]
        public void ShouldShortenStem_AdjectiveCategory_ReturnsFalse()
        {
            var request = BuildRequest("bílá", "mladý", Case.Genitive, Number.Plural, shortens: true);
            request.WordCategory = WordCategory.Adjective;

            Assert.IsFalse(_evaluator.ShouldShortenStem("bíl", request));
        }

        /// <summary>
        /// Determines whether should shorten stem empty stem returns false.
        /// </summary>
        [TestMethod]
        public void ShouldShortenStem_EmptyStem_ReturnsFalse()
        {
            var request = BuildRequest("kráva", "žena", Case.Genitive, Number.Plural, shortens: true);

            Assert.IsFalse(_evaluator.ShouldShortenStem(string.Empty, request));
        }

        /// <summary>
        /// Determines whether should shorten stem no long vowel returns false.
        /// </summary>
        [TestMethod]
        public void ShouldShortenStem_NoLongVowel_ReturnsFalse()
        {
            var request = BuildRequest("žena", "žena", Case.Genitive, Number.Plural, shortens: true);

            Assert.IsFalse(_evaluator.ShouldShortenStem("žen", request));
        }

        #endregion Guard clauses

        // -------------------------------------------------------------------------

        #region Helpers

        private static CzechWordRequest BuildRequest(
            string lemma, string pattern, Case @case, Number number, bool? shortens) =>
            new()
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Case = @case,
                Number = number,
                HasGenitivePluralShortening = shortens
            };

        #endregion Helpers

        // -------------------------------------------------------------------------

        #region Test data attributes

        /// <summary>
        /// Provides shortening returns true attribute behavior.
        /// </summary>
        private sealed class ShorteningReturnsTrueAttribute : TestAttributeBase
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo) =>
            [
                ["kráv", "kráva", "žena"],
                ["líp",  "lípa",  "žena"],
                ["síl",  "síla",  "žena"],
                ["brán", "brána", "žena"],
            ];
        }

        /// <summary>
        /// Provides shortening returns false attribute behavior.
        /// </summary>
        private sealed class ShorteningReturnsFalseAttribute : TestAttributeBase
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo) =>
            [
                // é, ý a ů mají v registru krátký protějšek kvůli jiným alternacím, ale tady nekrátí.
                ["sfér", "sféra", "žena"],
                ["rýh",  "rýha",  "žena"],
                ["půr",  "půra",  "žena"],
                // ú krátký protějšek nemá vůbec — kúra dává kúr.
                ["kúr",  "kúra",  "žena"],
            ];
        }

        #endregion Test data attributes
    }
}
