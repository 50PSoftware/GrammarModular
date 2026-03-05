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
            const string dataPath = "Data";
            var nounDataProvider = new JsonNounDataProvider(dataPath);
            var verbDataProvider = new JsonVerbDataProvider(dataPath);
            var prefixService = new CzechPrefixService(new JsonPrefixDataProvider(dataPath));
            var phonologyService = new CzechPhonologyService(new CzechPhonemeRegistry());
            var wordStructureResolver = new CzechWordStructureResolver(verbDataProvider, nounDataProvider, prefixService, phonologyService);

            var adjectiveDataProvider = new JsonAdjectiveDataProvider(dataPath);
            adjectiveDeclensionService = new CzechAdjectiveDeclensionService(adjectiveDataProvider, wordStructureResolver, phonologyService);
        }

        [TestMethod]
        [AdjectiveComparativeTest]
        public void GetForm_ComparativeNomSg_Returns(string lemma, string expectation)
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
            Assert.AreEqual(expectation, result.Form);
        }

        private class AdjectiveComparativeTestAttribute : AdjectiveDegreesTestAttribute
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                var list = new List<object[]>()
                {
                    new [] {"mladý", "mladší" },
                    new [] { "hezký", "hezčí" },
                    new [] { "starý", "starší" },
                    new [] { "drahý", "dražší" },
                    new [] { "tichý", "tišší" },
                    new [] { "pěkný", "pěknější" },
                    new [] { "jemný", "jemnější" },
                    new [] { "zdravý", "zdravější" }
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
                if (data is not null)
                {
                    var sb = new StringBuilder();
                    string insert1 = string.Empty;
                    string insertAfterReturns = string.Empty;
                    foreach (var d in data)
                    {
                        sb.AppendFormat("{0}", string.Join(d.ToString()));
                        if (d is object[] stringArray)
                        {
                            insert1 = (string)stringArray[0];
                            insertAfterReturns = (string)stringArray[1];
                        }
                    }

                    var methodParts = methodInfo.Name.Split('_');
                    methodParts[1] = methodParts[1].Insert(0, insert1);

                    var lastIndex = methodParts.Length - 1;
                    var lastSegmentLength = methodParts[lastIndex].Length;
                    methodParts[lastIndex] = methodParts[lastIndex].Insert(lastSegmentLength, insertAfterReturns);

                    var methodName = string.Join('_', methodParts);

                    return string.Format("{0}({1})", methodName, sb.ToString());
                }

                return methodInfo.Name;
            }
        }

        private class AdjectiveSuperlativeTestAttribue : AdjectiveDegreesTestAttribute
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                var list = new[]
                {
                    new [] { "mladý", "nejmladší" },
                    new [] { "starý", "nejstarší" },
                    new [] { "hezký", "nejhezčí" },
                };

                return list;
            }
        }
    }
}
