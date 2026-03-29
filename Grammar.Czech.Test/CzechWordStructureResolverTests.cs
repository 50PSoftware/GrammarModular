using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Services;
using System.Reflection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Tests for <see cref="CzechWordStructureResolver"/> — noun root and derivation suffix extraction.
    ///
    /// Organisation:
    ///   1. vzor žena  — základní substantiva a derivovaná na -ka
    ///   2. vzor město — strukturní sufix (jablko, okno, peklo) + stabilní shluky (město)
    ///   3. vzor kost  — konsonantický kmen bez sufixu
    ///   4. Mobile vowel (pes, otec)
    ///   5. Verbs
    ///   6. Guard clauses
    /// </summary>
    [TestClass]
    public class CzechWordStructureResolverTests
    {
        private IWordStructureResolver<CzechWordRequest> _resolver;
        private IVerbStructureResolver<CzechWordRequest> _verbResolver;

        [TestInitialize]
        public void Setup()
        {
            var registry = new CzechPhonemeRegistry();
            var phonologyService = new CzechPhonologyService(registry);
            var nounDataProvider = new JsonNounDataProvider();
            var verbDataProvider = new JsonVerbDataProvider();
            var prefixService = new CzechPrefixService(new JsonPrefixDataProvider());
            var epenthesisRule = new CzechEpenthesisRuleEvaluator(registry);

            var sut = new CzechWordStructureResolver(
                verbDataProvider, nounDataProvider, prefixService, phonologyService, registry, epenthesisRule);

            _resolver = sut;
            _verbResolver = sut;
        }

        // -------------------------------------------------------------------------

        #region vzor žena

        [TestMethod]
        public void AnalyzeNoun_ZenaPattern_ExtractsRoot()
        {
            // Arrange
            var request = BuildNounRequest("žena", "žena", Gender.Feminine, Case.Nominative, Number.Singular);

            // Act
            var result = _resolver.AnalyzeStructure(request);

            // Assert
            Assert.AreEqual("žen", result.Root);
            Assert.IsNull(result.DerivationSuffix);
        }

        [TestMethod]
        [ZenaKaSuffixData]
        public void AnalyzeNoun_ZenaPatternKaSuffix_ExtractsRootAndSuffix(
            string lemma, string expectedRoot, string expectedSuffix)
        {
            // Arrange
            var request = BuildNounRequest(lemma, "žena", Gender.Feminine, Case.Genitive, Number.Plural);

            // Act
            var result = _resolver.AnalyzeStructure(request);

            // Assert
            Assert.AreEqual(expectedRoot, result.Root, $"Root pro {lemma}");
            Assert.AreEqual(expectedSuffix, result.DerivationSuffix, $"DerivationSuffix pro {lemma}");
        }

        #endregion vzor žena

        // -------------------------------------------------------------------------

        #region vzor město — strukturní sufix

        [TestMethod]
        [MestoEpenthesisData]
        public void AnalyzeNoun_MestoPattern_EpenthesisCluster_ExtractsRootAndSuffix(
            string lemma, string expectedRoot, string expectedSuffix)
        {
            // Arrange
            var request = BuildNounRequest(lemma, "město", Gender.Neuter, Case.Genitive, Number.Plural);

            // Act
            var result = _resolver.AnalyzeStructure(request);

            // Assert
            Assert.AreEqual(expectedRoot, result.Root, $"Root pro {lemma}");
            Assert.AreEqual(expectedSuffix, result.DerivationSuffix, $"DerivationSuffix pro {lemma}");
        }

        [TestMethod]
        public void AnalyzeNoun_MestoPattern_StableCluster_RootUnchanged()
        {
            // Homorganní shluk s+t — evaluátor vrátí false → root nesmí být zkrácen.
            // Arrange
            var request = BuildNounRequest("město", "město", Gender.Neuter, Case.Genitive, Number.Plural);

            // Act
            var result = _resolver.AnalyzeStructure(request);

            // Assert
            Assert.AreEqual("měst", result.Root);
        }

        #endregion vzor město — strukturní sufix

        // -------------------------------------------------------------------------

        #region vzor kost

        [TestMethod]
        public void AnalyzeNoun_KostPattern_ExtractsRoot()
        {
            // Arrange
            var request = BuildNounRequest("radost", "kost", Gender.Feminine, Case.Nominative, Number.Singular);

            // Act
            var result = _resolver.AnalyzeStructure(request);

            // Assert
            Assert.AreEqual("radost", result.Root);
            Assert.IsNull(result.DerivationSuffix);
        }

        #endregion vzor kost

        // -------------------------------------------------------------------------

        #region Mobile vowel

        [TestMethod]
        public void AnalyzeNoun_Pes_NomSg_KeepsMobileVowel()
        {
            // Arrange
            var request = BuildNounRequest("pes", "pán", Gender.Masculine, Case.Nominative, Number.Singular);

            // Act
            var result = _resolver.AnalyzeStructure(request);

            // Assert
            Assert.AreEqual("pes", result.Root);
        }

        [TestMethod]
        public void AnalyzeNoun_Pes_GenSg_RemovesMobileVowel()
        {
            // Arrange
            var request = BuildNounRequest("pes", "pán", Gender.Masculine, Case.Genitive, Number.Singular, true);

            // Act
            var result = _resolver.AnalyzeStructure(request);

            // Assert
            Assert.AreEqual("ps", result.Root);
        }

        [TestMethod]
        public void AnalyzeNoun_Otec_GenSg_RemovesMobileVowel()
        {
            // Arrange
            var request = BuildNounRequest("otec", "muž", Gender.Masculine, Case.Genitive, Number.Singular, true);

            // Act
            var result = _resolver.AnalyzeStructure(request);

            // Assert
            Assert.AreEqual("otc", result.Root);
        }

        #endregion Mobile vowel

        // -------------------------------------------------------------------------

        #region Verbs

        [TestMethod]
        public void AnalyzeVerb_WithPrefix_ExtractsPrefixAndStem()
        {
            // Arrange
            var request = new CzechWordRequest
            {
                Lemma = "donést",
                Pattern = "nese",
                WordCategory = WordCategory.Verb,
                Tense = Tense.Present
            };

            // Act
            var result = _verbResolver.AnalyzeVerbStructure(request);

            // Assert
            Assert.AreEqual("do", result.Prefix);
            Assert.AreEqual("nes", result.PresentStem);
        }

        [TestMethod]
        public void AnalyzeVerb_IrregularByt_UsesPresentStem()
        {
            // Arrange
            var request = new CzechWordRequest
            {
                Lemma = "být",
                Pattern = "být",
                WordCategory = WordCategory.Verb,
                Tense = Tense.Present
            };

            // Act
            var result = _verbResolver.AnalyzeVerbStructure(request);

            // Assert
            Assert.AreEqual("js", result.PresentStem);
        }

        #endregion Verbs

        // -------------------------------------------------------------------------

        #region Guard clauses

        [TestMethod]
        public void AnalyzeStructure_EmptyLemma_ThrowsArgumentException()
        {
            var request = new CzechWordRequest { Lemma = "", Pattern = "žena" };

            Assert.ThrowsException<ArgumentException>(() => _resolver.AnalyzeStructure(request));
        }

        [TestMethod]
        public void AnalyzeStructure_EmptyPattern_ThrowsArgumentException()
        {
            var request = new CzechWordRequest { Lemma = "žena", Pattern = "" };

            Assert.ThrowsException<ArgumentException>(() => _resolver.AnalyzeStructure(request));
        }

        #endregion Guard clauses

        // -------------------------------------------------------------------------

        #region Helpers

        private static CzechWordRequest BuildNounRequest(
            string lemma, string pattern, Gender gender, Case @case, Number number, bool? hasMobileVowel = null) =>
            new()
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Noun,
                Gender = gender,
                Case = @case,
                Number = number,
                HasMobileVowel = hasMobileVowel
            };

        #endregion Helpers

        // -------------------------------------------------------------------------

        #region Test data attributes

        /// <summary>
        /// Vzor žena + sufix -ka: lemma, očekávaný root, očekávaný derivační sufix.
        /// </summary>
        private sealed class ZenaKaSuffixDataAttribute : TestAttributeBase
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo) =>
            [
                ["studentka", "student", "k"],
                ["matka",     "mat",     "k"],
                ["babka",     "bab",     "k"],
            ];
        }

        /// <summary>
        /// Vzor město — strukturní sufix tvoří heterorganní shluk → epentheze.
        /// Sloupce: lemma, očekávaný root, očekávaný derivační sufix.
        /// </summary>
        private sealed class MestoEpenthesisDataAttribute : TestAttributeBase
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo) =>
            [
                ["jablko", "jabl", "k"],  // l+k  Alveolar+Velar
                ["okno",   "ok",   "n"],  // k+n  Velar+Alveolar
                ["peklo",  "pek",  "l"],  // k+l  Velar+Alveolar
                ["vlákno", "vlák", "n"],  // k+n  Velar+Alveolar
            ];
        }

        #endregion Test data attributes
    }
}
