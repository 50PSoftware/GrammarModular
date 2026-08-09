using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies způsob slovesného děje: the classification itself, the aspect it implies, and the rows
    /// that carry it.
    /// </summary>
    /// <remarks>
    /// It is not a finer grade of aspect. Aspect is grammatical and every verb has one; this is lexical
    /// and most verbs have none, which is why the interesting assertions are about what stays null.
    /// </remarks>
    [TestClass]
    public sealed class AktionsartTests
    {
        private static IValencyProvider<CzechLexicalEntry> lexicon = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();

            lexicon = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })
                              .GetRequiredService<IValencyProvider<CzechLexicalEntry>>();
        }

        /// <summary>
        /// The classification is the source's whole inventory, taken as it stands.
        /// </summary>
        /// <remarks>
        /// Twenty-six and not twenty-five: the source labels the groups with the Czech alphabet, where
        /// ch is a letter of its own between h and i, so a–y runs one further than it looks.
        /// </remarks>
        [TestMethod]
        public void EveryGroupOfTheSourceIsPresent()
        {
            Assert.AreEqual(26, Enum.GetValues<Aktionsart>().Length);
        }

        /// <summary>
        /// Each group implies an aspect, which is what makes the column checkable rather than
        /// decorative: NESČ states it of the whole classification at once.
        /// </summary>
        [DataTestMethod]
        [DataRow(Aktionsart.Ingressive, VerbAspect.Perfective)]
        [DataRow(Aktionsart.Cumulative, VerbAspect.Perfective)]
        [DataRow(Aktionsart.Semelfactive, VerbAspect.Perfective)]
        [DataRow(Aktionsart.Momentary, VerbAspect.Perfective)]
        [DataRow(Aktionsart.Iterative, VerbAspect.Imperfective)]
        [DataRow(Aktionsart.Frequentative, VerbAspect.Imperfective)]
        [DataRow(Aktionsart.Stative, VerbAspect.Imperfective)]
        [DataRow(Aktionsart.Mutative, VerbAspect.Imperfective)]
        public void EachGroupImpliesItsAspect(Aktionsart aktionsart, VerbAspect expected)
        {
            Assert.AreEqual(expected, AktionsartFacts.RequiredAspect(aktionsart));
        }

        /// <summary>
        /// The divide falls where the source puts it: nineteen groups perfective, seven imperfective.
        /// </summary>
        [TestMethod]
        public void TheDivideFallsWhereTheSourcePutsIt()
        {
            var perfective = Enum.GetValues<Aktionsart>()
                .Count(group => AktionsartFacts.RequiredAspect(group) == VerbAspect.Perfective);

            Assert.AreEqual(19, perfective, "Skupiny (a)–(r) mají být dokonavé.");
            Assert.AreEqual(7, Enum.GetValues<Aktionsart>().Length - perfective, "A (s)–(y) nedokonavé.");
        }

        /// <summary>
        /// The dictionary carries it, and the pair that motivated the column reads as the pair it is.
        /// </summary>
        [DataTestMethod]
        [DataRow("blýsknout", Aktionsart.Semelfactive)]
        [DataRow("blýskat", Aktionsart.Frequentative)]
        [DataRow("zahřmět", Aktionsart.Ingressive)]
        [DataRow("napršet", Aktionsart.Cumulative)]
        [DataRow("nasněžit", Aktionsart.Cumulative)]
        public void DictionaryCarriesTheGroup(string lemma, Aktionsart expected)
        {
            Assert.AreEqual(expected, lexicon.GetEntry(lemma, WordCategory.Verb)?.Aktionsart);
        }

        /// <summary>
        /// A verb the classification says nothing about keeps null, which is unclassified and not
        /// "no group" — most of the lexicon is in this state and the column exists to be sparse.
        /// </summary>
        [DataTestMethod]
        [DataRow("pršet")]
        [DataRow("mrznout")]
        [DataRow("dělat")]
        [DataRow("číst")]
        public void UnclassifiedVerbKeepsNull(string lemma)
        {
            Assert.IsNull(lexicon.GetEntry(lemma, WordCategory.Verb)?.Aktionsart);
        }

        /// <summary>
        /// Every classified row agrees with the aspect its group implies. The lexicon tool checks this
        /// too; here it guards the shipped dictionary rather than whatever is being validated.
        /// </summary>
        [TestMethod]
        public void EveryClassifiedEntryAgreesWithItsAspect()
        {
            string[] classified = ["blýsknout", "blýskat", "zahřmět", "napršet", "nasněžit"];

            foreach (var lemma in classified)
            {
                var entry = lexicon.GetEntry(lemma, WordCategory.Verb);

                Assert.IsNotNull(entry?.Aktionsart, lemma);
                Assert.AreEqual(
                    AktionsartFacts.RequiredAspect(entry.Aktionsart.Value),
                    entry.Aspect,
                    $"{lemma} je {entry.Aktionsart} a ta skupina nese jiný vid.");
            }
        }
    }
}
