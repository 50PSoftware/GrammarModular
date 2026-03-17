using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Services;
using System.Reflection;
using System.Text;

namespace Grammar.Czech.Test
{
    [TestClass]
    public class AdjectiveDeclensionTests
    {
        private CzechAdjectiveDeclensionService adjectiveDeclensionService;

        [TestInitialize]
        public void Setup()
        {
            var nounDataProvider = new JsonNounDataProvider();
            var verbDataProvider = new JsonVerbDataProvider();
            var prefixService = new CzechPrefixService(new JsonPrefixDataProvider());
            var registry = new CzechPhonemeRegistry();
            var phonologyService = new CzechPhonologyService(registry);
            var wordStructureResolver = new CzechWordStructureResolver(verbDataProvider, nounDataProvider, prefixService, phonologyService);
            var ortographyService = new CzechOrtographyService(registry);

            var adjectiveDataProvider = new JsonAdjectiveDataProvider();
            adjectiveDeclensionService = new CzechAdjectiveDeclensionService(adjectiveDataProvider, wordStructureResolver, phonologyService, ortographyService);
        }

        [TestMethod]
        [AdjectiveComparativeTest]
        public void GetForm_ComparativeNomSg_Returns(string lemma, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Adjective,
                Case = Case.Nominative,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Pattern = "mladý",
                Degree = Degree.Comparative,
                IsAnimate = true
            };

            var result = adjectiveDeclensionService.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [TestMethod]
        [AdjectiveSupletiveComparativeTest]
        public void GetForm_SupletiveComparativeNomSg_Returns(string lemma, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Adjective,
                Case = Case.Nominative,
                Number = Number.Singular,
                Gender = Gender.Masculine,
                Pattern = "mladý",
                Degree = Degree.Comparative,
                IsAnimate = true
            };

            var result = adjectiveDeclensionService.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [TestMethod]
        [AdjectiveSuperlativeTest]
        public void GetForm_SuperlativeNomSg_Returns(string lemma, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Adjective,
                Case = Case.Nominative,
                Number = Number.Singular,
                Gender = Gender.Masculine,
                Pattern = "mladý",
                Degree = Degree.Superlative,
                IsAnimate = true
            };

            var result = adjectiveDeclensionService.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        private class AdjectiveSupletiveComparativeTestAttribute : AdjectiveDegreesTestAttribute
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                var list = new List<object[]>
                {
                    new [] {"dobrý", "lepší"},
                    new [] {"malý", "menší"},
                    new [] {"velký","větší"},
                    new [] {"zlý", "horší"},
                    new [] {"špatný", "horší"},
                    new [] {"dlouhý", "delší"},
                };

                return list;
            }
        }

        private class AdjectiveComparativeTestAttribute : AdjectiveDegreesTestAttribute
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                var list = new List<object[]>()
                {
                    new[] { "mladý", "mladší" },
                    new[] { "hezký", "hezčí" },
                    new[] { "starý", "starší" },
                    new[] { "drahý", "dražší" },
                    new[] { "tichý", "tišší" },
                    new[] { "pěkný", "pěknější" },
                    new[] { "jemný", "jemnější" },
                    new[] { "zdravý", "zdravější" },
                    new[] { "teplý", "teplejší" },
                    new[] { "tenký", "tenčí"},
                };

                return list;
            }
        }

        private class AdjectiveDegreesTestAttribute : TestAttributeBase
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                throw new NotImplementedException();
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
                    methodParts[1] = methodParts[1].Insert(0, insert1);

                    var lastIndex = methodParts.Length - 1;
                    methodParts[lastIndex] += insertAfterReturns;

                    return $"{string.Join('_', methodParts)}()";
                }

                return methodInfo.Name;
            }
        }

        private class AdjectiveSuperlativeTestAttribute : AdjectiveDegreesTestAttribute
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                var list = new[]
                {
                    new [] { "mladý", "nejmladší" },
                    new [] { "starý", "nejstarší" },
                    new [] { "hezký", "nejhezčí" },
                    new [] { "malý", "nejmenší" },
                    new [] { "tenký", "nejtenčí" }
                };

                return list;
            }
        }
    }
}
