using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Services;
using System.Reflection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies noun declension behavior.
    /// </summary>
    [TestClass]
    public sealed class NounDeclensionTests
    {
        private CzechNounDeclensionService nounDeclensionService;

        /// <summary>
        /// Creates the test subject and its dependencies.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            var registry = new CzechPhonemeRegistry();
            var phonologyService = new CzechPhonologyService(registry);
            var nounDataPrvider = new JsonNounDataProvider();
            var verbDataprovider = new JsonVerbDataProvider();
            var prefixService = new CzechPrefixService(new JsonPrefixDataProvider());
            var epenthesisRule = new CzechEpenthesisRuleEvaluator(registry);
            var wordStructureResolver = new CzechWordStructureResolver(verbDataprovider, nounDataPrvider, prefixService, phonologyService, registry, epenthesisRule);
            var softeningEvaluator = new CzechSofteningRuleEvaluator();
            var epenthesisEvaluator = new CzechEpenthesisRuleEvaluator(registry);
            var jotationEvaluator = new CzechJotationRuleEvaluator(registry, wordStructureResolver);
            var syncretismEvaluator = new CzechSyncretismRuleEvaluator();
            var orthographyService = new CzechOrthographyService(registry);
            var valencyProvider = new JsonValencyProvider();

            nounDeclensionService = new CzechNounDeclensionService(nounDataPrvider, wordStructureResolver, phonologyService, softeningEvaluator, epenthesisEvaluator, jotationEvaluator, syncretismEvaluator, orthographyService, valencyProvider);
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

        /// <summary>
        /// Verifies the -ě digraph realization for žena-pattern nouns in dative and locative singular.
        /// </summary>
        /// <param name="lemma">The dictionary form to inflect.</param>
        /// <param name="expected">The expected dative/locative singular form.</param>
        [TestMethod]
        [ZenaSoftEndingData]
        public void GetForm_ZenaDativeLocativeSg_ReturnsExpected(string lemma, string expected)
        {
            foreach (var @case in new[] { Case.Dative, Case.Locative })
            {
                var request = new CzechWordRequest
                {
                    Lemma = lemma,
                    WordCategory = WordCategory.Noun,
                    Pattern = "žena",
                    Gender = Gender.Feminine,
                    Number = Number.Singular,
                    Case = @case
                };

                var result = nounDeclensionService.GetForm(request).Form;
                Assert.AreEqual(expected, result, $"Pro pád {@case} lemmatu '{lemma}'.");
            }
        }

        /// <summary>
        /// Verifies vocative singular formation for masculine animate nouns.
        /// </summary>
        /// <param name="lemma">The dictionary form to inflect.</param>
        /// <param name="expected">The expected vocative singular form.</param>
        /// <param name="pattern">The declension pattern of the lemma.</param>
        [TestMethod]
        [MasculineVocativeSingularData]
        public void GetForm_MasculineVocativeSg_ReturnsExpected(string lemma, string expected, string pattern)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Noun,
                Pattern = pattern,
                Gender = Gender.Masculine,
                IsAnimate = true,
                Number = Number.Singular,
                Case = Case.Vocative
            };

            var result = nounDeclensionService.GetForm(request).Form;
            Assert.AreEqual(expected, result, $"Vokativ sg. lemmatu '{lemma}'.");
        }

        /// <summary>
        /// Verifies that latinate -or agent nouns keep their full stem across the oblique cases.
        /// </summary>
        /// <param name="lemma">The dictionary form to inflect.</param>
        /// <remarks>
        /// The mobile-e fallback used to match any vowel-consonant-vowel-consonant tail and stripped the
        /// o out of -or, yielding doktrovi and Mendominátre.
        /// </remarks>
        [TestMethod]
        [DataRow("doktor")]
        [DataRow("profesor")]
        [DataRow("Mendominátor")]
        public void GetForm_LatinateOrNounObliqueCases_KeepsFullStem(string lemma)
        {
            var expectedByCase = new Dictionary<Case, string>
            {
                [Case.Genitive] = lemma + "a",
                [Case.Dative] = lemma + "ovi",
                [Case.Accusative] = lemma + "a",
                [Case.Vocative] = lemma + "e",
                [Case.Locative] = lemma + "ovi",
                [Case.Instrumental] = lemma + "em"
            };

            foreach (var (@case, expected) in expectedByCase)
            {
                var request = new CzechWordRequest
                {
                    Lemma = lemma,
                    WordCategory = WordCategory.Noun,
                    Pattern = "pán",
                    Gender = Gender.Masculine,
                    IsAnimate = true,
                    Number = Number.Singular,
                    Case = @case
                };

                var result = nounDeclensionService.GetForm(request).Form;
                Assert.AreEqual(expected, result, $"Pro pád {@case} lemmatu '{lemma}'.");
            }
        }

        /// <summary>
        /// Verifies the metathesized švec stem, which is supplied by the irregulars data rather than derived.
        /// </summary>
        /// <param name="case">The case to inflect into.</param>
        /// <param name="expected">The expected singular form.</param>
        [TestMethod]
        [DataRow(Case.Genitive, "ševce")]
        [DataRow(Case.Dative, "ševci")]
        [DataRow(Case.Vocative, "ševče")]
        [DataRow(Case.Instrumental, "ševcem")]
        public void GetForm_ŠvecSg_UsesMetathesizedStem(Case @case, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = "švec",
                WordCategory = WordCategory.Noun,
                Pattern = "muž",
                Gender = Gender.Masculine,
                IsAnimate = true,
                Number = Number.Singular,
                Case = @case
            };

            var result = nounDeclensionService.GetForm(request).Form;
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Verifies that the irregulars' inheritsFrom overrides the pattern supplied by the caller,
        /// including the endings, not just the rules that run afterwards.
        /// </summary>
        /// <remarks>
        /// Švec inherits from muž. Asking for the hard pán pattern used to yield the hard genitive
        /// ševca, because the pattern was resolved before inheritsFrom was applied.
        /// </remarks>
        [TestMethod]
        public void GetForm_IrregularWithInheritsFrom_OverridesCallerPattern()
        {
            var request = new CzechWordRequest
            {
                Lemma = "švec",
                WordCategory = WordCategory.Noun,
                Pattern = "pán",
                Gender = Gender.Masculine,
                IsAnimate = true,
                Number = Number.Singular,
                Case = Case.Genitive
            };

            var result = nounDeclensionService.GetForm(request).Form;
            Assert.AreEqual("ševce", result);
        }

        /// <summary>
        /// Verifies that GetForm sg nom returns expected.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="pattern">The inflection pattern used to choose the rule.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        /// <param name="isAnimate">The animacy flag supplied by the test data.</param>
        /// <param name="hasMobileE">True when the stem is known to contain a mobile vowel; otherwise, false.</param>
        /// <param name="vals">The test case values supplied by the data attribute.</param>
        [TestMethod]
        [PatternsNounDeclensionData]
        public void GetForm_SgNom_ReturnsExpected(string lemma, string pattern, Gender gender, bool? isAnimate, bool? hasMobileE, string[] vals)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Noun,
                Gender = gender,
                Pattern = pattern,
                IsAnimate = isAnimate,
                HasMobileE = hasMobileE
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

        /// <summary>
        /// Provides žena-pattern dative/locative singular test cases covering the -ě digraph.
        /// </summary>
        private class ZenaSoftEndingDataAttribute : NounDeclensionTestAttribue
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                return new List<object[]>
                {
                    // d/t/n — grapheme stays, ě carries the softening
                    new[] { "žena", "ženě" },
                    new[] { "Jana", "Janě" },
                    new[] { "Marta", "Martě" },
                    new[] { "Linda", "Lindě" },
                    // labials b/p/m and v/f — ě kept
                    new[] { "ryba", "rybě" },
                    new[] { "lampa", "lampě" },
                    new[] { "máma", "mámě" },
                    new[] { "Eva", "Evě" },
                    new[] { "žirafa", "žirafě" },
                    // r → ř, ě → e
                    new[] { "Klára", "Kláře" },
                    // s/z/l — ě → e
                    new[] { "kosa", "kose" },
                    new[] { "koza", "koze" },
                    new[] { "škola", "škole" },
                    // velar 2nd palatalization — k→c, h→z, ch→š, g→z
                    new[] { "ruka", "ruce" },
                    new[] { "noha", "noze" },
                    new[] { "moucha", "mouše" },
                    new[] { "Praha", "Praze" },
                    new[] { "droga", "droze" },
                };
            }
        }

        /// <summary>
        /// Provides vocative singular test cases for masculine animate nouns.
        /// </summary>
        private class MasculineVocativeSingularDataAttribute : NounDeclensionTestAttribue
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                return new List<object[]>
                {
                    // pán — plain -e
                    new[] { "pán", "pane", "pán" },
                    new[] { "student", "studente", "pán" },
                    // latinate -or: vowel before the r, so no palatalization
                    new[] { "doktor", "doktore", "pán" },
                    new[] { "profesor", "profesore", "pán" },
                    new[] { "Mendominátor", "Mendominátore", "pán" },
                    // consonant before the r: 1st palatalization r → ř
                    new[] { "bratr", "bratře", "pán" },
                    new[] { "Petr", "Petře", "pán" },
                    new[] { "ministr", "ministře", "pán" },
                    // velar stems take -u with no palatalization
                    new[] { "voják", "vojáku", "pán" },
                    new[] { "hoch", "hochu", "pán" },
                    new[] { "vrah", "vrahu", "pán" },
                    new[] { "biolog", "biologu", "pán" },
                    // soft and -a patterns
                    new[] { "muž", "muži", "muž" },
                    new[] { "chlapec", "chlapče", "muž" },
                    new[] { "předseda", "předsedo", "předseda" },
                    // soudce keeps the nominative form; *soudče is non-standard per IJP
                    new[] { "soudce", "soudce", "soudce" },
                };
            }
        }

        /// <summary>
        /// Provides noun test cases for the píseň pattern.
        /// </summary>
        private class PisenPatternNounDataTestAttribute : NounDeclensionTestAttribue
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
                    new [] {"píseň", "písně" },
                    new [] {"větev", "větve" },
                    new [] { "třešeň", "třešně" },
                };

                return list;
            }
        }

        /// <summary>
        /// Provides full noun declension pattern test cases.
        /// </summary>
        private class PatternsNounDeclensionDataAttribute : NounDeclensionTestAttribue
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                var dict = new Dictionary<string, (string, Gender, bool?, bool?, string[])>
                {
                    { "student", ("pán", Gender.Masculine, true, null, new[] {"student", "studenta", "studentovi", "studenta", "studente", "studentovi", "studentem",
                                            "studenti", "studentů", "studentům", "studenty", "studenti", "studentech", "studenty"}) },
                    { "studentka", ("žena", Gender.Feminine, null, null, new[] {"studentka", "studentky", "studentce", "studentku", "studentko", "studentce", "studentkou",
                                            "studentky", "studentek", "studentkám", "studentky", "studentky", "studentkách", "studentkami"}) },
                    { "studentík", ("pán", Gender.Masculine, true, null, new [] {"studentík", "studentíka", "studentíkovi", "studentíka", "studentíku", "studentíkovi", "studentíkem",
                                            "studentíci", "studentíků", "studentíkům", "studentíky", "studentíci", "studentících", "studentíky" }) },
                    { "pes", ("pán", Gender.Masculine, true, true, new [] {"pes", "psa", "psovi", "psa", "pse","psovi", "psem",
                                            "psi", "psů", "psům", "psy", "psi", "psech", "psy"}) },
                    { "dům", ("hrad", Gender.Masculine, false, null, new [] { "dům", "domu", "domu", "dům", "dome", "domě", "domem",
                                            "domy", "domů", "domům", "domy", "domy", "domech", "domy"}) },
                    { "kůň", ("muž", Gender.Masculine, true, null, new [] { "kůň", "koně", "koni", "koně", "koni", "koni", "koněm",
                                            "koně", "koní", "koním", "koně", "koně", "koních", "koňmi"}) },
                    { "chlapec", ("muž", Gender.Masculine, true, null, new [] {"chlapec", "chlapce", "chlapci", "chlapce", "chlapče", "chlapci", "chlapcem",
                                            "chlapci", "chlapců", "chlapcům", "chlapce", "chlapci", "chlapcích", "chlapci"}) },
                    { "pán", ("pán", Gender.Masculine, true, null, new [] { "pán", "pána", "pánovi", "pána", "pane", "pánovi", "pánem",
                                            "páni", "pánů", "pánům", "pány", "páni", "pánech", "pány"}) },
                    { "hrad", ("hrad", Gender.Masculine, false, null, new [] {"hrad", "hradu", "hradu", "hrad", "hrade", "hradě", "hradem",
                                            "hrady", "hradů", "hradům", "hrady", "hrady", "hradech", "hrady"}) },
                    { "muž", ("muž", Gender.Masculine, true, null, new [] { "muž", "muže", "muži", "muže", "muži", "muži", "mužem",
                                            "muži", "mužů" ,"mužům", "muže", "muži", "mužích", "muži" })},
                    { "stroj", ("stroj", Gender.Masculine, false, null, new [] { "stroj", "stroje", "stroji", "stroj", "stroji", "stroji", "strojem",
                                            "stroje", "strojů", "strojům", "stroje", "stroje", "strojích", "stroji"}) },
                    { "předseda", ("předseda", Gender.Masculine, true, null, new[] {"předseda", "předsedy", "předsedovi", "předsedu", "předsedo", "předsedovi", "předsedou",
                                            "předsedové", "předsedů", "předsedům", "předsedy", "předsedové", "předsedech", "předsedy"}) },
                    // Vokativ je roven nominativu: IJP uvádí u vzoru soudce tvar "soudce" a *soudče
                    // označuje za nespisovné — vzniká „patrně nevýrazností formy 5. p. totožné s 1. p.“
                    { "soudce", ("soudce", Gender.Masculine, true, null, new[] { "soudce", "soudce", "soudci", "soudce", "soudce", "soudci", "soudcem",
                                            "soudci", "soudců", "soudcům", "soudce", "soudci", "soudcích", "soudci"}) },
                    { "žena", ("žena", Gender.Feminine, null, null, new [] {"žena", "ženy", "ženě", "ženu", "ženo", "ženě", "ženou",
                                            "ženy", "žen", "ženám", "ženy", "ženy", "ženách", "ženami"}) },
                    { "růže", ("růže", Gender.Feminine, null, null, new [] { "růže", "růže", "růži", "růži", "růže", "růži", "růží",
                                            "růže", "růží", "růžím", "růže", "růže", "růžích", "růžemi"}) },
                    { "píseň", ("píseň", Gender.Feminine, null, null, new [] {"píseň", "písně", "písni", "píseň", "písni", "písni", "písní",
                                            "písně", "písní", "písním", "písně", "písně", "písních", "písněmi"}) },
                    { "kost",("kost", Gender.Feminine, null, null, new [] { "kost", "kosti", "kosti", "kost", "kosti", "kosti", "kostí",
                                            "kosti", "kostí", "kostem", "kosti", "kosti", "kostech" ,"kostmi"}) },
                    { "město", ("město", Gender.Neuter, null, null, new [] {"město", "města", "městu", "město", "město", "městě", "městem",
                                            "města", "měst", "městům", "města", "města", "městech", "městy" }) },
                    { "moře", ("moře", Gender.Neuter, null, null, new [] { "moře", "moře", "moři", "moře", "moře", "moři", "mořem",
                                            "moře", "moří", "mořím", "moře", "moře", "mořích", "moři"}) },
                    { "kuře", ("kuře", Gender.Neuter, null, null, new [] {"kuře", "kuřete", "kuřeti", "kuře", "kuře", "kuřeti", "kuřetem",
                                            "kuřata", "kuřat", "kuřatům", "kuřata", "kuřata", "kuřatech", "kuřaty"}) },
                    { "stavení", ("stavení", Gender.Neuter, null, null, new [] {"stavení", "stavení", "stavení", "stavení", "stavení", "stavení", "stavením",
                                            "stavení", "stavení", "stavením", "stavení", "stavení", "staveních", "staveními"}) }
                };

                var data = new List<object[]>();

                foreach (var (k, v) in dict)
                {
                    var lemma = k;
                    var pattern = v.Item1;
                    var gender = v.Item2;
                    var isAnimate = v.Item3;
                    var hasMobileVowel = v.Item4;
                    var vals = v.Item5;
                    data.Add(new object[] { lemma, pattern, gender, isAnimate, hasMobileVowel, vals });
                }

                return data;
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

        /// <summary>
        /// Provides shared display-name behavior for noun declension data attributes.
        /// </summary>
        private class NounDeclensionTestAttribue : TestAttributeBase
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
