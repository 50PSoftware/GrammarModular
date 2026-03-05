using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Services;
using System.Reflection;

namespace Grammar.Czech.Test
{
    [TestClass]
    public class CzechPhonologyServiceTests
    {
        private CzechPhonologyService service;

        [TestInitialize]
        public void Setup()
        {
            var registry = new CzechPhonemeRegistry();
            service = new CzechPhonologyService(registry);
        }

        #region Softening

        [TestMethod]
        public void ApplySoftening_ShouldSoftened()
        {
            Assert.AreEqual("č", service.ApplySoftening("c", Enums.Phonology.PalatalizationContext.Second));
            Assert.AreEqual("š", service.ApplySoftening("ch"));
            Assert.AreEqual("z", service.ApplySoftening("h", Enums.Phonology.PalatalizationContext.Second));
            Assert.AreEqual("ď", service.ApplySoftening("d"));
            Assert.AreEqual("ť", service.ApplySoftening("t"));
            Assert.AreEqual("ň", service.ApplySoftening("n"));
        }

        [TestMethod]
        public void RevertSoftening_ShouldRevert()
        {
            Assert.AreEqual("c", service.RevertSoftening("č"));
            Assert.AreEqual("ch", service.RevertSoftening("š"));
            Assert.AreEqual("h", service.RevertSoftening("z"));
            Assert.AreEqual("d", service.RevertSoftening("ď"));
            Assert.AreEqual("t", service.RevertSoftening("ť"));
            Assert.AreEqual("n", service.RevertSoftening("ň"));
        }

        [TestMethod]
        public void ApplySoftening_ShouldNotChange()
        {
            Assert.AreEqual("a", service.ApplySoftening("a"));
            Assert.AreEqual("b", service.ApplySoftening("b"));
            Assert.AreEqual("e", service.ApplySoftening("e"));
        }

        [TestMethod]
        public void RevertSoftening_ShouldNotChange()
        {
            Assert.AreEqual("a", service.RevertSoftening("a"));
            Assert.AreEqual("b", service.RevertSoftening("b"));
            Assert.AreEqual("e", service.RevertSoftening("e"));
        }

        [TestMethod]
        public void ApplySoftening_ShouldHandleShortWords()
        {
            Assert.AreEqual("hoš", service.ApplySoftening("hoch"));
        }

        [TestMethod]
        public void RevertSoftening_ShouldHandleShortWords()
        {
            Assert.AreEqual("hoch", service.RevertSoftening("hoš"));
        }

        [TestMethod]
        public void ApplySoftening_ShouldHandleWordsWithSoftenedEnding()
        {
            Assert.AreEqual("koč", service.ApplySoftening("koc", Enums.Phonology.PalatalizationContext.Second));
            Assert.AreEqual("koš", service.ApplySoftening("koch"));
        }

        #endregion Softening

        #region MobileVowel

        [TestMethod]
        public void RemoveMobileVowel_Pes_ReturnsPs()
        {
            Assert.AreEqual("ps", service.RemoveMobileVowel("pes", true));
        }

        [TestMethod]
        public void RemoveMobileVowel_Otec_ReturnsOtc()
        {
            Assert.AreEqual("otc", service.RemoveMobileVowel("otec", true));
        }

        [TestMethod]
        public void RemoveMobileVowel_Den_ReturnsDn()
        {
            Assert.AreEqual("dn", service.RemoveMobileVowel("den", true));
        }

        [TestMethod]
        public void RemoveMobileVowel_NoMobileVowel_ReturnsOriginal()
        {
            Assert.AreEqual("hrad", service.RemoveMobileVowel("hrad", false));
        }

        [TestMethod]
        public void InsertMobileVowel_Ps_ReturnsPes()
        {
            Assert.AreEqual("pes", service.InsertMobileVowel("ps", 1));
        }

        [TestMethod]
        public void InsertMobileVowel_Dn_ReturnsDen()
        {
            Assert.AreEqual("den", service.InsertMobileVowel("dn", 1));
        }

        #endregion MobileVowel

        #region Epenthesis Tests

        [TestMethod]
        public void ApplyEpenthesis_StudentkaGenPl_ReturnsStudentek()
        {
            var request = new CzechWordRequest
            {
                Lemma = "studentka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Case = Case.Genitive,
                Number = Number.Plural
            };

            var result = service.ApplyEpenthesis(true, "student", "k");

            Assert.AreEqual("studentek", result);
        }

        [TestMethod]
        public void ApplyEpenthesis_MatkaGenPl_ReturnsMatek()
        {
            var request = new CzechWordRequest
            {
                Lemma = "matka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Case = Case.Genitive,
                Number = Number.Plural
            };

            var result = service.ApplyEpenthesis(true, "mat", "k");

            Assert.AreEqual("matek", result);
        }

        [TestMethod]
        public void ApplyEpenthesis_OknoGenPl_ReturnsOken()
        {
            var request = new CzechWordRequest
            {
                Lemma = "okno",
                Pattern = "město",
                WordCategory = WordCategory.Noun,
                Case = Case.Genitive,
                Number = Number.Plural,
                Gender = Gender.Neuter
            };

            var result = service.ApplyEpenthesis(true, "ok", "n");

            Assert.AreEqual("oken", result);
        }

        #endregion Epenthesis Tests

        #region Shorten and Lengthen Vowels

        [TestMethod]
        [ShortenVowels]
        public void ShortenVowels(string input, string expected)
        {
            var result = service.ShortenVowel(input);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        [LengthenVowel]
        public void LengthenVowels(string input, string expected)
        {
            var result = service.LengthenVowel(input);

            Assert.AreEqual(expected, result);
        }

        #endregion Shorten and Lengthen Vowels

        private class ShortenVowelsAttribute : TestAttributeBase
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                var data = new List<string[]>()
                {
                    new[] { "stůl", "stol" },
                    new[] { "pán", "pan" },
                    new[] { "kmen", "kmen" }
                };

                return data;
            }
        }

        private class LengthenVowelAttribute : TestAttributeBase
        {
            public override IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            {
                var data = new List<string[]>()
                {
                    new[] { "stol", "stůl" },
                    new[] { "pan", "pán" },
                    new[] { "dub", "dub" }
                };

                return data;
            }
        }
    }
}
