using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Services;
using System.Reflection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies czech phonology service behavior.
    /// </summary>
    [TestClass]
    public class CzechPhonologyServiceTests
    {
        private CzechPhonologyService service;

        /// <summary>
        /// Creates the test subject and its dependencies.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            var registry = new CzechPhonemeRegistry();
            service = new CzechPhonologyService(registry);
        }

        #region Softening

        /// <summary>
        /// Verifies that apply softening should softened.
        /// </summary>
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

        /// <summary>
        /// Verifies that revert softening should revert.
        /// </summary>
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

        /// <summary>
        /// Verifies that apply softening should not change.
        /// </summary>
        [TestMethod]
        public void ApplySoftening_ShouldNotChange()
        {
            Assert.AreEqual("a", service.ApplySoftening("a"));
            Assert.AreEqual("b", service.ApplySoftening("b"));
            Assert.AreEqual("e", service.ApplySoftening("e"));
        }

        /// <summary>
        /// Verifies that revert softening should not change.
        /// </summary>
        [TestMethod]
        public void RevertSoftening_ShouldNotChange()
        {
            Assert.AreEqual("a", service.RevertSoftening("a"));
            Assert.AreEqual("b", service.RevertSoftening("b"));
            Assert.AreEqual("e", service.RevertSoftening("e"));
        }

        /// <summary>
        /// Verifies that apply softening should handle short words.
        /// </summary>
        [TestMethod]
        public void ApplySoftening_ShouldHandleShortWords()
        {
            Assert.AreEqual("hoš", service.ApplySoftening("hoch"));
        }

        /// <summary>
        /// Verifies that revert softening should handle short words.
        /// </summary>
        [TestMethod]
        public void RevertSoftening_ShouldHandleShortWords()
        {
            Assert.AreEqual("hoch", service.RevertSoftening("hoš"));
        }

        /// <summary>
        /// Verifies that apply softening should handle words with softened ending.
        /// </summary>
        [TestMethod]
        public void ApplySoftening_ShouldHandleWordsWithSoftenedEnding()
        {
            Assert.AreEqual("koč", service.ApplySoftening("koc", Enums.Phonology.PalatalizationContext.Second));
            Assert.AreEqual("koš", service.ApplySoftening("koch"));
        }

        #endregion Softening

        #region MobileVowel

        /// <summary>
        /// Verifies that remove mobile vowel pes returns ps.
        /// </summary>
        [TestMethod]
        public void RemoveMobileVowel_Pes_ReturnsPs()
        {
            Assert.AreEqual("ps", service.RemoveMobileE("pes", true));
        }

        /// <summary>
        /// Verifies that remove mobile vowel otec returns otc.
        /// </summary>
        [TestMethod]
        public void RemoveMobileVowel_Otec_ReturnsOtc()
        {
            Assert.AreEqual("otc", service.RemoveMobileE("otec", true));
        }

        /// <summary>
        /// Verifies that remove mobile vowel den returns dn.
        /// </summary>
        [TestMethod]
        public void RemoveMobileVowel_Den_ReturnsDn()
        {
            Assert.AreEqual("dn", service.RemoveMobileE("den", true));
        }

        /// <summary>
        /// Verifies that remove mobile vowel no mobile vowel returns original.
        /// </summary>
        [TestMethod]
        public void RemoveMobileVowel_NoMobileVowel_ReturnsOriginal()
        {
            Assert.AreEqual("hrad", service.RemoveMobileE("hrad", false));
        }

        /// <summary>
        /// Verifies that a stem whose penultimate character is not a mobile e is left untouched
        /// even when the caller claims a mobile vowel is present.
        /// </summary>
        /// <param name="stem">The stem to pass through the removal.</param>
        [TestMethod]
        [DataRow("doktor")]
        [DataRow("Mendominátor")]
        [DataRow("hrad")]
        public void RemoveMobileVowel_StemWithoutMobileE_ReturnsOriginal(string stem)
        {
            Assert.AreEqual(stem, service.RemoveMobileE(stem, true));
        }

        /// <summary>
        /// Verifies that insert mobile vowel ps returns pes.
        /// </summary>
        [TestMethod]
        public void InsertMobileVowel_Ps_ReturnsPes()
        {
            Assert.AreEqual("pes", service.InsertMobileE("ps", 1));
        }

        /// <summary>
        /// Verifies that insert mobile vowel dn returns den.
        /// </summary>
        [TestMethod]
        public void InsertMobileVowel_Dn_ReturnsDen()
        {
            Assert.AreEqual("den", service.InsertMobileE("dn", 1));
        }

        #endregion MobileVowel

        #region Epenthesis Tests

        /// <summary>
        /// Verifies that apply epenthesis studentka gen pl returns studentek.
        /// </summary>
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

        /// <summary>
        /// Verifies that apply epenthesis vzpomínka gen pl returns vzpomínek.
        /// </summary>
        [TestMethod]
        public void ApplyEpenthesis_VzpominkaGenPl_ReturnsVzpominek()
        {
            var request = new CzechWordRequest
            {
                Lemma = "vzpomínka",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Case = Case.Genitive,
                Number = Number.Plural
            };

            var result = service.ApplyEpenthesis(true, "vzpomín", "k");

            Assert.AreEqual("vzpomínek", result);
        }

        /// <summary>
        /// Verifies that apply epenthesis matka gen pl returns matek.
        /// </summary>
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

        /// <summary>
        /// Verifies that apply epenthesis okno gen pl returns oken.
        /// </summary>
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

        /// <summary>
        /// Verifies that shorten vowels.
        /// </summary>
        /// <param name="input">The input text to transform.</param>
        /// <param name="expected">The expected surface form asserted by the test.</param>
        [TestMethod]
        [ShortenVowels]
        public void ShortenVowels(string input, string expected)
        {
            var result = service.ShortenVowel(input);

            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Verifies that lengthen vowels.
        /// </summary>
        /// <param name="input">The input text to transform.</param>
        /// <param name="expected">The expected surface form asserted by the test.</param>
        [TestMethod]
        [LengthenVowel]
        public void LengthenVowels(string input, string expected)
        {
            var result = service.LengthenVowel(input);

            Assert.AreEqual(expected, result);
        }

        #endregion Shorten and Lengthen Vowels

        /// <summary>
        /// Provides shorten vowels attribute behavior.
        /// </summary>
        private class ShortenVowelsAttribute : TestAttributeBase
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
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

        /// <summary>
        /// Provides lengthen vowel attribute behavior.
        /// </summary>
        private class LengthenVowelAttribute : TestAttributeBase
        {
            /// <summary>
            /// Provides data rows for a parameterized MSTest method.
            /// </summary>
            /// <param name="methodInfo">The test method requesting data.</param>
            /// <returns>The test data rows for the requested method.</returns>
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
