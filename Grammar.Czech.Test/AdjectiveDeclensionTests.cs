using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Services;
using System.Reflection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies adjective declension behavior.
    /// </summary>
    [TestClass]
    public class AdjectiveDeclensionTests
    {
        private CzechAdjectiveDeclensionService adjectiveDeclensionService;

        /// <summary>
        /// Creates the test subject and its dependencies.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            var nounDataProvider = new JsonNounDataProvider();
            var verbDataProvider = new JsonVerbDataProvider();
            var prefixService = new CzechPrefixService(new JsonPrefixDataProvider());
            var registry = new CzechPhonemeRegistry();
            var phonologyService = new CzechPhonologyService(registry);
            var epenthesisRule = new CzechEpenthesisRuleEvaluator(registry);
            var wordStructureResolver = new CzechWordStructureResolver(verbDataProvider, nounDataProvider, prefixService, phonologyService, registry, epenthesisRule);
            var orthographyService = new CzechOrthographyService(registry);

            var adjectiveDataProvider = new JsonAdjectiveDataProvider();
            adjectiveDeclensionService = new CzechAdjectiveDeclensionService(adjectiveDataProvider, wordStructureResolver, phonologyService, orthographyService);
        }

        /// <summary>
        /// Verifies that GetForm comparative nom sg returns.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="expected">The expected surface form asserted by the test.</param>
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

        /// <summary>
        /// Verifies that GetForm supletive comparative nom sg returns.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="expected">The expected surface form asserted by the test.</param>
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

        /// <summary>
        /// Verifies that GetForm superlative nom sg returns.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="expected">The expected surface form asserted by the test.</param>
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

        /// <summary>
        /// Provides adjective suppletive comparative test cases.
        /// </summary>
        private class AdjectiveSupletiveComparativeTestAttribute : AdjectiveDegreesTestAttribute
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
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

        /// <summary>
        /// Provides adjective comparative test cases.
        /// </summary>
        private class AdjectiveComparativeTestAttribute : AdjectiveDegreesTestAttribute
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
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

        /// <summary>
        /// Provides adjective degrees test attribute behavior.
        /// </summary>
        private class AdjectiveDegreesTestAttribute : TestAttributeBase
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Formats a readable display name for the test case.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <param name="data">The test case data used to build the display name.</param>
            /// <returns>The display name used by the test runner.</returns>
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

        /// <summary>
        /// Provides adjective superlative test cases.
        /// </summary>
        private class AdjectiveSuperlativeTestAttribute : AdjectiveDegreesTestAttribute
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
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
