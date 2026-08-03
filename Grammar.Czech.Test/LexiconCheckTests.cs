using Grammar.Czech.Lexicon.Tool;
using System.Text.Json;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Checks the non-destructive validation of what a server is serving.
    /// </summary>
    /// <remarks>
    /// Entries written in the admin exist only in the central database until something pulls them, and
    /// nothing on the server enforces what the validator does. This is the check that answers "is what I
    /// just typed loadable" without waiting for the next real pull — so the two properties that matter
    /// are that it reports honestly and that it leaves nothing behind either way.
    /// </remarks>
    [TestClass]
    public sealed class LexiconCheckTests
    {
        private const int PageSize = 25;

        private static string sourceDatabase = null!;

        /// <summary>
        /// Locates the lexicon that ships beside the test assembly.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            sourceDatabase = Path.Combine(AppContext.BaseDirectory, "grammar.czech.lexicon.db");

            Assert.IsTrue(File.Exists(sourceDatabase), $"Lexikon '{sourceDatabase}' u testů není.");
        }

        /// <summary>
        /// A sound dictionary passes and no file is left behind.
        /// </summary>
        [TestMethod]
        public void Check_SoundLexicon_PassesAndKeepsNothing()
        {
            var before = TemporaryCheckFiles();

            var validation = LexiconPuller.Check(RoundTrip(), _ => { });

            Assert.AreEqual(
                0,
                validation.Errors.Count,
                "Kontrola nahlásila chyby: " + string.Join("; ", validation.Errors));

            AssertNothingLeftBehind(before);
        }

        /// <summary>
        /// A frame whose slots were dropped is reported, and still nothing is left behind.
        /// </summary>
        /// <remarks>
        /// The failing case has to be tested too: a check that only ever passes would pass on a broken
        /// server just as readily. Dropping the slots reproduces the shape the validator exists to
        /// catch — a frame that licenses nothing, which is what an interrupted edit in the admin leaves.
        /// </remarks>
        [TestMethod]
        public void Check_FrameWithoutSlots_ReportsAndKeepsNothing()
        {
            var before = TemporaryCheckFiles();

            var validation = LexiconPuller.Check(WithoutSlots(), _ => { });

            Assert.IsTrue(
                validation.Errors.Count > 0,
                "Rámec bez slotů měl být nahlášen.");

            Assert.IsTrue(
                validation.Errors.Any(error => error.Contains("nemá žádný slot", StringComparison.Ordinal)),
                "Očekával jsem hlášku o rámci bez slotu, přišlo: " + string.Join("; ", validation.Errors));

            AssertNothingLeftBehind(before);
        }

        /// <summary>
        /// A server that answers with nothing does not read as an empty dictionary.
        /// </summary>
        [TestMethod]
        public void Check_NoEntriesAtAll_Throws()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => LexiconPuller.Check(EmptyPages(), _ => { }));

            StringAssert.Contains(exception.Message, "ani jedno heslo");
        }

        // The pages go through JSON, so the check runs on what a server would actually put on the wire
        // rather than on the objects the exporter happened to build.
        private static IEnumerable<LexiconPage> RoundTrip()
        {
            foreach (var page in LexiconJsonExporter.Export(sourceDatabase, PageSize))
            {
                var json = JsonSerializer.Serialize(page, LexiconPage.SerializerOptions);

                yield return JsonSerializer.Deserialize<LexiconPage>(json, LexiconPage.SerializerOptions)!;
            }
        }

        // Both tables have to go. Dropping the slots alone leaves realizations pointing at them, and the
        // foreign key rejects that during the import — earlier than the validator, and a different
        // failure from the one this test is about.
        private static IEnumerable<LexiconPage> WithoutSlots()
            => RoundTrip().Select(page => page.Table is "valency_slot" or "slot_realization"
                ? page with { Rows = [] }
                : page);

        private static IEnumerable<LexiconPage> EmptyPages()
            => LexiconSchema.Tables.Select(table => new LexiconPage
            {
                Table = table.Name,
                Columns = table.Columns,
                Rows = []
            });

        // Only files that appeared are the test's business. The temporary directory is shared, and the
        // tests in this class may run alongside each other, so one of them holding a working file while
        // another takes its snapshot says nothing about either.
        private static void AssertNothingLeftBehind(string[] before)
            => CollectionAssert.AreEquivalent(
                Array.Empty<string>(),
                TemporaryCheckFiles().Except(before).ToArray(),
                "Kontrola po sobě nechala pracovní soubor.");

        private static string[] TemporaryCheckFiles()
            => Directory.GetFiles(Path.GetTempPath(), "lexicon-check-*.db");
    }
}
