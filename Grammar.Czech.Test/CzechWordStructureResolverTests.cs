using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Services;

namespace Grammar.Czech.Test
{
    [TestClass]
    public class CzechWordStructureResolverTests
    {
        private IWordStructureResolver<CzechWordRequest> resolver;
        private IVerbDataProvider verbDataProvider;
        private CzechPrefixService prefixService;
        private IPhonologyService<CzechWordRequest> phonologyService;
        private IVerbStructureResolver<CzechWordRequest> verbResolver;

        [TestInitialize]
        public void Setup()
        {
            verbDataProvider = new JsonVerbDataProvider();
            var perfixProvider = new JsonPrefixDataProvider();
            var nounDataProvider = new JsonNounDataProvider();
            prefixService = new CzechPrefixService(perfixProvider);
            var registry = new CzechPhonemeRegistry();
            phonologyService = new CzechPhonologyService(registry);
            var rootProvider = new JsonRootProvider();
            resolver = new CzechWordStructureResolver(verbDataProvider, nounDataProvider, prefixService, phonologyService, rootProvider);
            verbResolver = new CzechWordStructureResolver(verbDataProvider, nounDataProvider, prefixService, phonologyService, rootProvider);
        }

        #region Nouns

        [TestMethod]
        public void AnalyzeNoun_SimplePattern_ExtractsRootCorrectly()
        {
            var request = new CzechWordRequest
            {
                Lemma = "žena",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Case = Case.Nominative
            };

            var result = resolver.AnalyzeStructure(request);

            Assert.AreEqual("žen", result.Root);
            Assert.IsNull(result.Prefix);
        }

        [TestMethod]
        public void AnalyzeNoun_KostPattern_ExtractsRootCorrectly()
        {
            var request = new CzechWordRequest
            {
                Lemma = "radost",
                Pattern = "kost",
                WordCategory = WordCategory.Noun,
                Case = Case.Nominative
            };
            var result = resolver.AnalyzeStructure(request);
            Assert.AreEqual("radost", result.Root);
            Assert.IsNull(result.Prefix);
        }

        [TestMethod]
        public void AnalyzeNoun_WithKaSuffix_HandlesDerivationalSuffix()
        {
            var request = new CzechWordRequest
            {
                Lemma = "studentka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Case = Case.Nominative,
                Number = Number.Plural
            };

            var result = resolver.AnalyzeStructure(request);

            Assert.AreEqual("student", result.Root);
            Assert.AreEqual("k", result.DerivationSuffix);
        }

        [TestMethod]
        public void AnalyzeNoun_WithKaSuffix_HandlesGenitivePluralDerivationalSuffix()
        {
            var request = new CzechWordRequest
            {
                Lemma = "studentka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Case = Case.Genitive,
                Number = Number.Plural
            };

            var result = resolver.AnalyzeStructure(request);

            Assert.AreEqual("student", result.Root);
            Assert.AreEqual("k", result.DerivationSuffix);
        }

        [TestMethod]
        public void AnalyzeNoun_Pes_Nominative_KeepsMobileE()
        {
            var request = new CzechWordRequest
            {
                Lemma = "pes",
                Pattern = "pán",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Case = Case.Nominative,
                Number = Number.Singular
            };

            var result = resolver.AnalyzeStructure(request);

            Assert.AreEqual("pes", result.Root);
        }

        [TestMethod]
        public void AnalyzeNoun_Pes_Genitive_RemovesMobileE()
        {
            var request = new CzechWordRequest
            {
                Lemma = "pes",
                Pattern = "pán",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Case = Case.Genitive,
                Number = Number.Singular
            };

            var result = resolver.AnalyzeStructure(request);

            Assert.AreEqual("ps", result.Root);
        }

        [TestMethod]
        public void AnalyzeNoun_Otec_Genitive_RemovesMobileE()
        {
            var request = new CzechWordRequest
            {
                Lemma = "otec",
                Pattern = "muž",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Case = Case.Genitive,
                Number = Number.Singular
            };

            var result = resolver.AnalyzeStructure(request);

            Assert.AreEqual("otc", result.Root);
        }

        #endregion Nouns

        #region Verbs

        [TestMethod]
        public void AnalyzeVerb_WithPrefix_ExtractsPrefixAndStem()
        {
            var request = new CzechWordRequest
            {
                Lemma = "donést",
                Pattern = "nese",
                WordCategory = WordCategory.Verb,
                Tense = Tense.Present
            };

            var result = verbResolver.AnalyzeVerbStructure(request);

            Assert.AreEqual("do", result.Prefix);
            Assert.AreEqual("nes", result.PresentStem);
        }

        [TestMethod]
        public void AnalyzeVerb_IrregularBýt_UsesPresentStem()
        {
            var request = new CzechWordRequest
            {
                Lemma = "být",
                Pattern = "být",
                WordCategory = WordCategory.Verb,
                Tense = Tense.Present
            };

            var result = verbResolver.AnalyzeVerbStructure(request);

            Assert.AreEqual("js", result.PresentStem);
        }

        #endregion Verbs

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AnalyzeStructure_EmptyLemma_ThrowsException()
        {
            var request = new CzechWordRequest { Lemma = "", Pattern = "žena" };
            resolver.AnalyzeStructure(request);
        }
    }
}
