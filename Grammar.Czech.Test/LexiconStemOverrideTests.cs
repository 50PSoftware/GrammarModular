using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Providers.SqliteProviders;
using Grammar.Czech.Services;
using Microsoft.Data.Sqlite;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Checks that a stem written on a dictionary entry beats the one its pattern would derive.
    /// </summary>
    /// <remarks>
    /// The stems used to live only in the embedded irregulars.json, where correcting one verb meant a
    /// rebuild and a release of the library. They are now columns on lemma_entry as well, and the point
    /// of the whole change is that the column wins.
    /// <para>
    /// The stems written here are deliberately not the real Czech forms — vid where the language says
    /// viděl. A test that overrode a stem with the form the pattern already derives would pass whether
    /// or not the column was read at all.
    /// </para>
    /// </remarks>
    [TestClass]
    public sealed class LexiconStemOverrideTests
    {
        private static string databasePath = null!;
        private static SqliteValencyProvider provider = null!;
        private static CzechVerbConjugationService service = null!;

        /// <summary>
        /// Copies the shipped lexicon and writes stems onto two of its verbs.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            databasePath = Path.Combine(Path.GetTempPath(), $"kmeny-{Guid.NewGuid():N}.db");
            File.Copy(
                Path.Combine(AppContext.BaseDirectory, SqliteValencyProvider.DefaultFileName),
                databasePath);

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = """
                    UPDATE lemma_entry SET past_stem = 'vid' WHERE lemma = 'vidět';
                    UPDATE lemma_entry SET past_stem = 'uzř' WHERE lemma = 'uvidět';
                    """;
                command.ExecuteNonQuery();
            }

            SqliteConnection.ClearAllPools();

            provider = new SqliteValencyProvider(databasePath);
            service = BuildService(provider);
        }

        /// <summary>
        /// Releases the copy.
        /// </summary>
        [ClassCleanup]
        public static void CleanupClass()
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }

        /// <summary>
        /// The provider reads the stem columns back.
        /// </summary>
        [TestMethod]
        public void GetEntry_EntryWithStem_ReadsItBack()
        {
            var entry = provider.GetEntry("vidět", WordCategory.Verb);

            Assert.IsNotNull(entry);
            Assert.AreEqual("vid", entry.PastStem);
            Assert.IsTrue(entry.HasStems);
        }

        /// <summary>
        /// An entry that writes no stem says so, which is what lets conjugation skip the override.
        /// </summary>
        [TestMethod]
        public void HasStems_EntryWithoutStems_IsFalse()
        {
            var entry = provider.GetEntry("dávat", WordCategory.Verb);

            Assert.IsNotNull(entry);
            Assert.IsFalse(entry.HasStems);
        }

        /// <summary>
        /// The past tense is built on the stem from the lexicon, not on the one trida4 derives.
        /// </summary>
        [DataTestMethod]
        [DataRow("Masculine", "Singular", "vidl", DisplayName = "vidět – min. sg m z lexikonu")]
        [DataRow("Feminine", "Singular", "vidla", DisplayName = "vidět – min. sg f z lexikonu")]
        [DataRow("Masculine", "Plural", "vidli", DisplayName = "vidět – min. pl m z lexikonu")]
        public void GetBasicForm_PastWithLexiconStem_UsesIt(string gender, string number, string expected)
        {
            var result = service.GetBasicForm(new CzechWordRequest
            {
                Lemma = "vidět",
                Pattern = "trida4",
                WordCategory = WordCategory.Verb,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Tense = Tense.Past,
                Aspect = VerbAspect.Imperfective,
                Gender = Enum.Parse<Gender>(gender),
                Number = Enum.Parse<Number>(number),
            });

            Assert.AreEqual(expected, result.Form);
        }

        /// <summary>
        /// A verb the lexicon writes no stem for conjugates exactly as before.
        /// </summary>
        /// <remarks>
        /// The regression that matters: the override runs on every verb, and one that quietly rebuilt
        /// the structure for words with nothing to override would move forms all over the dictionary.
        /// </remarks>
        [DataTestMethod]
        [DataRow("dělat", "dělá", "dělal", DisplayName = "dělat – mimo lexikon")]
        [DataRow("dávat", "trida5", "dával", DisplayName = "dávat – v lexikonu, bez kmene")]
        [DataRow("nést", "nese", "nesl", DisplayName = "nést – nepravidelný vzor beze změny")]
        public void GetBasicForm_PastWithoutLexiconStem_IsUnchanged(
            string lemma, string pattern, string expected)
        {
            var result = service.GetBasicForm(new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Tense = Tense.Past,
                Aspect = VerbAspect.Imperfective,
                Gender = Gender.Masculine,
                Number = Number.Singular,
            });

            Assert.AreEqual(expected, result.Form);
        }

        /// <summary>
        /// A stem written for a prefixed lemma carries the prefix and does not get a second one.
        /// </summary>
        /// <remarks>
        /// uvidět is analyzed as u + vidět, and the builder prepends the prefix to every stem. The stem
        /// from the lexicon is written for the entry as it stands, so prepending to it as well would
        /// give uuzřl.
        /// </remarks>
        [TestMethod]
        public void GetBasicForm_PastWithLexiconStemOnPrefixedLemma_DoesNotRepeatThePrefix()
        {
            var result = service.GetBasicForm(new CzechWordRequest
            {
                Lemma = "uvidět",
                Pattern = "trida4",
                WordCategory = WordCategory.Verb,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Tense = Tense.Past,
                Aspect = VerbAspect.Perfective,
                Gender = Gender.Masculine,
                Number = Number.Singular,
            });

            Assert.AreEqual("uzřl", result.Form);
        }

        /// <summary>
        /// The stems the entry leaves alone keep the prefix the override discards.
        /// </summary>
        /// <remarks>
        /// The other half of the same mechanism: uvidět writes only a past stem, so its present has to
        /// come out of the pattern with the u still on it.
        /// </remarks>
        [TestMethod]
        public void GetBasicForm_PresentOfPrefixedLemmaWithPastStem_KeepsThePrefix()
        {
            var result = service.GetBasicForm(new CzechWordRequest
            {
                Lemma = "uvidět",
                Pattern = "trida4",
                WordCategory = WordCategory.Verb,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Tense = Tense.Present,
                Aspect = VerbAspect.Perfective,
                Person = Person.Third,
                Number = Number.Singular,
            });

            Assert.AreEqual("uvidí", result.Form);
        }

        private static CzechVerbConjugationService BuildService(SqliteValencyProvider valencyProvider)
        {
            var verbDataProvider = new JsonVerbDataProvider();
            var registry = new CzechPhonemeRegistry();
            var prefixService = new CzechPrefixService(new JsonPrefixDataProvider());

            var structureResolver = new CzechWordStructureResolver(
                verbDataProvider,
                new JsonNounDataProvider(),
                prefixService,
                new CzechPhonologyService(registry),
                registry,
                new CzechEpenthesisRuleEvaluator(registry));

            return new CzechVerbConjugationService(
                verbDataProvider,
                structureResolver,
                prefixService,
                registry,
                valencyProvider);
        }
    }
}
