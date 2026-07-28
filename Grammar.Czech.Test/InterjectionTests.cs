using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the interjection inventory: what each one does, which can carry a clause, and how the
    /// punctuation follows from the use rather than from the word.
    /// </summary>
    [TestClass]
    public sealed class InterjectionTests
    {
        private static ICzechInterjectionService service = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            service = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })
                              .GetRequiredService<ICzechInterjectionService>();
        }

        #region Typy

        /// <summary>
        /// The four types of NESČ, by what the interjection does in the discourse.
        /// </summary>
        /// <param name="lemma">The interjection to look up.</param>
        /// <param name="type">The expected type.</param>
        [DataTestMethod]
        // emocionální — stav mluvčího
        [DataRow("ach", "Emotional")]
        [DataRow("au", "Emotional")]
        [DataRow("fuj", "Emotional")]
        [DataRow("brr", "Emotional")]
        [DataRow("ouvej", "Emotional")]
        [DataRow("hurá", "Emotional")]
        // kontaktová — navázání a udržení kontaktu
        [DataRow("hej", "Phatic")]
        [DataRow("haló", "Phatic")]
        [DataRow("ahoj", "Phatic")]
        [DataRow("pa", "Phatic")]
        [DataRow("čao", "Phatic")]
        // apelová — působení na adresáta
        [DataRow("psst", "Conative")]
        [DataRow("prr", "Conative")]
        [DataRow("kuk", "Conative")]
        [DataRow("hop", "Conative")]
        [DataRow("aha", "Conative")]
        // zvukomalebná — napodobení zvuku
        [DataRow("ťuk", "Onomatopoeic")]
        [DataRow("bum", "Onomatopoeic")]
        [DataRow("žbluňk", "Onomatopoeic")]
        [DataRow("tik", "Onomatopoeic")]
        [DataRow("bim", "Onomatopoeic")]
        public void GetInterjectionType_RegisteredInterjection_MatchesTheClassification(string lemma, string type)
            => Assert.AreEqual(Enum.Parse<InterjectionType>(type), service.GetInterjectionType(lemma));

        /// <summary>
        /// Every type is populated, so the enum records no distinction the data fails to make.
        /// </summary>
        [TestMethod]
        public void GetInterjectionsOfType_EveryType_HasEntries()
        {
            foreach (var type in Enum.GetValues<InterjectionType>())
            {
                Assert.IsTrue(
                    service.GetInterjectionsOfType(type).Count > 0,
                    $"Typ {type} nemá v interjections.json žádné citoslovce.");
            }
        }

        #endregion Typy

        #region Přísudkové užití a interpunkce

        /// <summary>
        /// The interjections that take objects and adjuncts as a verb would.
        /// </summary>
        [DataTestMethod]
        [DataRow("buch")]
        [DataRow("prásk")]
        [DataRow("žbluňk")]
        [DataRow("dup")]
        [DataRow("ťuk")]
        // hop je apelové, a přísudkové přesto — proto se to zapisuje po slovech, ne odvozuje z typu
        [DataRow("hop")]
        public void CanBePredicate_PredicativeInterjection_IsTrue(string lemma)
            => Assert.IsTrue(service.CanBePredicate(lemma));

        /// <summary>
        /// And the ones that only ever stand outside the clause.
        /// </summary>
        [DataTestMethod]
        [DataRow("ach")]
        [DataRow("hurá")]
        [DataRow("ahoj")]
        [DataRow("psst")]
        [DataRow("mňau")]
        public void CanBePredicate_NonPredicativeInterjection_IsFalse(string lemma)
            => Assert.IsFalse(service.CanBePredicate(lemma));

        /// <summary>
        /// The ÚJČ rule: a comma sets the interjection off unless it stands in for a clause member. The same
        /// word is punctuated both ways, so the use decides it and no entry in the data could have.
        /// </summary>
        [TestMethod]
        public void RequiresComma_TurnsOnTheUseNotTheWord()
        {
            // "Palicí buch ho po hlavě" — přísudek, bez čárky
            Assert.IsFalse(service.RequiresComma("buch", asPredicate: true));

            // totéž slovo mimo větný člen — s čárkou
            Assert.IsTrue(service.RequiresComma("buch", asPredicate: false));

            // "Kamarádi, hurá, vyhráli jsme"
            Assert.IsTrue(service.RequiresComma("hurá", asPredicate: false));
        }

        /// <summary>
        /// Asking for predicative punctuation on a word not recorded as predicative is a contradiction, and
        /// is reported rather than resolved silently.
        /// </summary>
        [TestMethod]
        public void RequiresComma_NonPredicativeInterjectionAsPredicate_Throws()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => service.RequiresComma("hurá", asPredicate: true));

            StringAssert.Contains(exception.Message, "hurá");
            StringAssert.Contains(exception.Message, "interjections.json");
        }

        #endregion Přísudkové užití a interpunkce

        #region Reduplikace a slovotvorba

        /// <summary>
        /// Reduplication is recorded only where the source names it: optional for oj and ťuk, obligatory for
        /// bubu, and unmarked everywhere else.
        /// </summary>
        [TestMethod]
        public void GetReduplication_RecordsOnlyWhatTheSourceNames()
        {
            Assert.AreEqual(Reduplication.Optional, service.GetReduplication("oj"));
            Assert.AreEqual(Reduplication.Optional, service.GetReduplication("ťuk"));
            Assert.AreEqual(Reduplication.Required, service.GetReduplication("bubu"));

            // Neoznačené neznamená "neopakuje se", jen "není to zapsané".
            Assert.AreEqual(Reduplication.None, service.GetReduplication("ahoj"));
        }

        /// <summary>
        /// The onomatopoeic interjections feed word formation directly, without passing through another word
        /// class on the way.
        /// </summary>
        /// <param name="lemma">The interjection.</param>
        /// <param name="verb">The verb formed from it.</param>
        [DataTestMethod]
        [DataRow("žbluňk", "žbluňknout")]
        [DataRow("ťuk", "ťuknout")]
        [DataRow("buch", "buchnout")]
        [DataRow("prásk", "prásknout")]
        [DataRow("cink", "cinknout")]
        public void GetDerivedVerb_OnomatopoeicInterjection_ReturnsTheVerb(string lemma, string verb)
            => Assert.AreEqual(verb, service.GetDerivedVerb(lemma));

        /// <summary>
        /// Every interjection that can be a predicate is one that names an event, so all but the two whose
        /// verb Czech does not form carry a derived verb too.
        /// </summary>
        [TestMethod]
        public void GetDerivedVerb_PredicativeInterjections_MostlyDeriveAVerb()
        {
            var predicative = Enum.GetValues<InterjectionType>()
                .SelectMany(service.GetInterjectionsOfType)
                .Where(service.CanBePredicate)
                .ToList();

            var withoutVerb = predicative.Where(lemma => service.GetDerivedVerb(lemma) is null).ToList();

            Assert.IsTrue(
                withoutVerb.Count <= 1,
                $"Přísudková citoslovce bez odvozeného slovesa: {string.Join(", ", withoutVerb)}.");
        }

        #endregion Reduplikace a slovotvorba

        #region Otevřenost třídy

        /// <summary>
        /// Onomatopoeia is coined on the spot, so an unregistered lemma is a gap in the data rather than a
        /// mistake by the caller — the opposite of the closed conjunction inventory.
        /// </summary>
        [TestMethod]
        public void IsInterjection_UnregisteredLemma_ReturnsFalseWithoutThrowing()
        {
            Assert.IsFalse(service.IsInterjection("šplouchtink"));
            Assert.IsFalse(service.CanBePredicate("šplouchtink"));
        }

        /// <summary>
        /// But asking what an unregistered word does is a question the data cannot answer.
        /// </summary>
        [TestMethod]
        public void GetInterjectionType_UnregisteredLemma_Throws()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => service.GetInterjectionType("šplouchtink"));

            StringAssert.Contains(exception.Message, "šplouchtink");
        }

        #endregion Otevřenost třídy
    }
}
