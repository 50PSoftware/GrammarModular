using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Services;
using System.Reflection;

namespace Grammar.Czech.Test
{
    [TestClass]
    public sealed class NounDeclensionTests
    {
        private CzechNounDeclensionService nounDeclensionService;

        [TestInitialize]
        public void Setup()
        {
            var registry = new CzechPhonemeRegistry();
            var phonologyService = new CzechPhonologyService(registry);
            var nounDataPrvider = new JsonNounDataProvider();
            var verbDataprovider = new JsonVerbDataProvider();
            var prefixService = new CzechPrefixService(new JsonPrefixDataProvider());
            var wordStructureResolver = new CzechWordStructureResolver(verbDataprovider, nounDataPrvider, prefixService, phonologyService);
            var softeningEvaluator = new CzechSofteningRuleEvaluator();
            var epenthesisEvaluator = new CzechEpenthesisRuleEvaluator(registry);
            var jotationEvaluator = new CzechJotationRuleEvaluator(registry);

            nounDeclensionService = new CzechNounDeclensionService(nounDataPrvider, wordStructureResolver, phonologyService, softeningEvaluator, epenthesisEvaluator, jotationEvaluator);
        }

        [TestMethod]
        public void GetForm_ZeměGenSg_ReturnsZemě()
        {
            var request = new CzechWordRequest
            {
                Lemma = "země",
                WordCategory = WordCategory.Noun,
                Pattern = "růže",
                Number = Number.Singular,
                Gender = Gender.Feminine,
                Case = Case.Genitive
            };

            var result = nounDeclensionService.GetForm(request);
            Assert.AreEqual("země", result.Form);
        }

        [TestMethod]
        public void GetForm_ZemGenSg_ReturnsZemě()
        {
            var request = new CzechWordRequest
            {
                Lemma = "zem",
                WordCategory = WordCategory.Noun,
                Pattern = "píseň",
                Number = Number.Singular,
                Gender = Gender.Feminine,
                Case = Case.Genitive
            };

            var result = nounDeclensionService.GetForm(request);
            Assert.AreEqual("země", result.Form);
        }

        [TestMethod]
        [PisenPatternNounTest]
        public void GetForm_PíseňPatternGenSgFor_Returns(string lemma, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Noun,
                Pattern = "píseň",
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Case = Case.Genitive
            };

            var result = nounDeclensionService.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        private class PisenPatternNounTestAttribute : TestAttributeBase
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                var list = new List<object[]>
                {
                    new [] {"píseň", "písně" },
                    new [] {"větev", "větve" },
                    new [] { "třešeň", "třešně" },
                };

                return list;
            }

            public override string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
            {
                if (data is not null && data.Length >= 2)
                {
                    string insert1 = data[0]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(insert1))
                    {
                        insert1 = char.ToUpperInvariant(insert1[0]) + insert1[1..];
                    }

                    string insertAfterReturns = data[1]?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(insertAfterReturns))
                    {
                        insertAfterReturns = char.ToUpperInvariant(insertAfterReturns[0]) + insertAfterReturns[1..];
                    }

                    var methodParts = methodInfo.Name.Split('_');
                    methodParts[1] = methodParts[1] += insert1;

                    var lastIndex = methodParts.Length - 1;
                    methodParts[lastIndex] += insertAfterReturns;

                    return $"{string.Join('_', methodParts)}()";
                }

                return methodInfo.Name;
            }
        }
    }
}
