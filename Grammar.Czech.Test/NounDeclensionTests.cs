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
            var ortographyService = new CzechOrtographyService(registry);

            nounDeclensionService = new CzechNounDeclensionService(nounDataPrvider, wordStructureResolver, phonologyService, softeningEvaluator, epenthesisEvaluator, jotationEvaluator, ortographyService);
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
        [PisenPatternNounDataTest]
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

        [TestMethod]
        [PatternsNounDeclensionData]
        public void GetForm_SgNom_ReturnsExpected(string lemma, string pattern, Gender gender, bool? isAnimate, string[] vals)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Noun,
                Gender = gender,
                Pattern = pattern,
                IsAnimate = isAnimate
            };

            for (int index = 0; index < vals.Length; index++)
            {
                var caseNum = index < 7 ? index : index - 7;
                request.Case = Enum.GetValues<Case>()[caseNum];
                request.Number = index < 7 ? Number.Singular : Number.Plural;

                var result = nounDeclensionService.GetForm(request).Form;
                var expected = vals[index];
                Assert.AreEqual(expected, result, $"Pro pád {request.Case?.ToString()} se očekávalo: {expected}.");
            }
        }

        private class PisenPatternNounDataTestAttribute : NounDeclensionTestAttribue
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
        }

        private class PatternsNounDeclensionDataAttribute : NounDeclensionTestAttribue
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                var dict = new Dictionary<string, (string, Gender, bool?, string[])>
                {
                    { "student", ("pán", Gender.Masculine, true, new[] {"student", "studenta", "studentovi", "studenta", "studente", "studentovi", "studentem",
                                            "studenti", "studentů", "studentům", "studenty", "studenti", "studentech", "studenty"}) },
                    { "studentka", ("žena", Gender.Feminine, null, new[] {"studentka", "studentky", "studentce", "studentku", "studentko", "studentce", "studentkou",
                                            "studentky", "studentek", "studentkám", "studentky", "studentky", "studentkách", "studentkami"}) },
                    { "studentík", ("pán", Gender.Masculine, true, new [] {"studentík", "studentíka", "studentíkovi", "studentíka", "studentíku", "studentíkovi", "studentíkem",
                                            "studentíci", "studentíků", "studentíkům", "studentíky", "studentíci", "studentících", "studentíky" }) },
                    {"žena", ("žena", Gender.Feminine, null, new [] {"žena", "ženy", "ženě", "ženu", "ženo", "ženě","ženou",
                                            "ženy", "žen", "ženám", "ženy", "ženy", "ženách", "ženami"}) },
                    { "pes", ("pán", Gender.Masculine, true, new [] {"pes", "psa", "psovi", "psa", "pse","psovi", "psem",
                                            "psi", "psů", "psům", "psy", "psi", "psech", "psy"}) },
                    { "dům", ("hrad", Gender.Masculine, false, new [] { "dům", "domu", "domu", "dům", "dome", "domě", "domem",
                                            "domy", "domů", "domům", "domy", "domy", "domech", "domy"}) },
                    { "kůň", ("muž", Gender.Masculine, true, new [] { "kůň", "koně", "koni", "koně", "koni", "koni", "koněm",
                                            "koně", "koní", "koním", "koně", "koně", "koních", "koňmi"}) }
                };

                var data = new List<object[]>();

                foreach (var (k,v) in dict)
                {
                    var lemma = k;
                    var pattern = v.Item1;
                    var gender = v.Item2;
                    var isAnimate = v.Item3;
                    var vals = v.Item4;
                    data.Add(new object[] { lemma, pattern, gender, isAnimate, vals });
                }

                return data;
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

                    var expectedData = data[4] as string[] ?? null;
                    var expectedDataString = string.Empty;
                    if (expectedData is not null)
                    {
                        expectedDataString = string.Join(',', expectedData);
                    }

                    var methodParts = methodInfo.Name.Split('_');
                    methodParts[1] = methodParts[1] += insert1;

                    return $"{string.Join('_', methodParts)}({expectedDataString})";
                }

                return base.GetDisplayName(methodInfo, data);
            }
        }

        private class NounDeclensionTestAttribue : TestAttributeBase
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
