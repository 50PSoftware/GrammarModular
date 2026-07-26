using Grammar.Czech.Services;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies how a numeral written in digits may join a word.
    /// </summary>
    /// <remarks>
    /// The reference is the Internetová jazyková příručka ÚJČ, id=790, 160 and 785: no ending and no hyphen
    /// after a digit, a full stop for ordinals, and a hyphen only after a placeholder letter.
    /// </remarks>
    [TestClass]
    public sealed class NumeralOrthographyTests
    {
        private CzechNumeralOrthographyService service = null!;

        /// <summary>
        /// Creates the test subject.
        /// </summary>
        [TestInitialize]
        public void Setup() => service = new CzechNumeralOrthographyService();

        /// <summary>
        /// Správné zápisy podle IJP id=790: bez mezery, bez spojovníku, bez koncovky.
        /// </summary>
        [DataTestMethod]
        [DataRow("5", DisplayName = "5 – základní číslovka")]
        [DataRow("1953", DisplayName = "1953 – letopočet")]
        [DataRow("5.", DisplayName = "5. – řadová s tečkou")]
        [DataRow("28.", DisplayName = "28. – řadová s tečkou")]
        [DataRow("20krát", DisplayName = "20krát")]
        [DataRow("12procentní", DisplayName = "12procentní")]
        [DataRow("8metrový", DisplayName = "8metrový")]
        [DataRow("300korunová", DisplayName = "300korunová")]
        [DataRow("256členná", DisplayName = "256členná")]
        [DataRow("x-stupňový", DisplayName = "x-stupňový – zástupné písmeno")]
        [DataRow("n-tá", DisplayName = "n-tá – zástupné písmeno")]
        [DataRow("pět", DisplayName = "pět – slovy")]
        public void IsValid_CorrectSpelling_ReturnsTrue(string token)
        {
            Assert.IsTrue(service.IsValid(token, out var reason), $"'{token}' je správný zápis, ale byl odmítnut: {reason}");
            Assert.IsNull(reason);
        }

        /// <summary>
        /// Chybné zápisy podle IJP id=790: koncovka nebo spojovník připojený k číslici.
        /// </summary>
        [DataTestMethod]
        [DataRow("5tý", DisplayName = "*5tý – koncovka u řadové")]
        [DataRow("19tý", DisplayName = "*19tý – koncovka u řadové")]
        [DataRow("8mý", DisplayName = "*8mý – koncovka u řadové")]
        [DataRow("5té", DisplayName = "*5té – koncovka u řadové")]
        [DataRow("10ti", DisplayName = "*10ti – koncovka -ti")]
        [DataRow("8mi", DisplayName = "*8mi – koncovka -mi")]
        [DataRow("12tiprocentní", DisplayName = "*12tiprocentní – koncovka -ti")]
        [DataRow("12-ti-procentní", DisplayName = "*12-ti-procentní – spojovník i koncovka")]
        [DataRow("20-krát", DisplayName = "*20-krát – spojovník")]
        [DataRow("10-ti", DisplayName = "*10-ti – spojovník i koncovka")]
        public void IsValid_IncorrectSpelling_ReturnsFalseWithReason(string token)
        {
            Assert.IsFalse(service.IsValid(token, out var reason), $"'{token}' je chybný zápis, ale prošel.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(reason), "Chybný zápis musí být vysvětlen.");
        }

        /// <summary>
        /// Normalizace opraví chybný zápis na tvar doporučený příručkou.
        /// </summary>
        [DataTestMethod]
        [DataRow("5tý", "5.", DisplayName = "*5tý → 5.")]
        [DataRow("19tý", "19.", DisplayName = "*19tý → 19.")]
        [DataRow("10ti", "10", DisplayName = "*10ti → 10")]
        [DataRow("8mi", "8", DisplayName = "*8mi → 8")]
        [DataRow("12-ti-procentní", "12procentní", DisplayName = "*12-ti-procentní → 12procentní")]
        [DataRow("12tiprocentní", "12procentní", DisplayName = "*12tiprocentní → 12procentní")]
        [DataRow("20-krát", "20krát", DisplayName = "*20-krát → 20krát")]
        public void Normalize_IncorrectSpelling_ReturnsCorrectedToken(string token, string expected)
            => Assert.AreEqual(expected, service.Normalize(token));

        /// <summary>
        /// Správný zápis normalizace nemění.
        /// </summary>
        [DataTestMethod]
        [DataRow("5.", DisplayName = "5. beze změny")]
        [DataRow("20krát", DisplayName = "20krát beze změny")]
        [DataRow("12procentní", DisplayName = "12procentní beze změny")]
        [DataRow("n-tá", DisplayName = "n-tá beze změny")]
        [DataRow("pět", DisplayName = "pět beze změny")]
        public void Normalize_CorrectSpelling_LeavesTokenAlone(string token)
            => Assert.AreEqual(token, service.Normalize(token));

        /// <summary>
        /// Každý odmítnutý zápis se musí dát normalizovat na zápis, který už projde.
        /// </summary>
        [TestMethod]
        public void Normalize_EveryRejectedToken_ProducesValidResult()
        {
            string[] wrong = ["5tý", "19tý", "8mý", "10ti", "8mi", "12tiprocentní", "12-ti-procentní", "20-krát", "10-ti"];

            foreach (var token in wrong)
            {
                var normalized = service.Normalize(token);

                Assert.IsTrue(
                    service.IsValid(normalized, out var reason),
                    $"Normalizace '{token}' vrátila '{normalized}', což je pořád chybné: {reason}");
            }
        }
    }
}
