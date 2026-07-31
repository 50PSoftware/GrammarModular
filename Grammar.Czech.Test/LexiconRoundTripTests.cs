using Grammar.Czech.Lexicon.Tool;
using System.Text;
using System.Text.Json;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Proves the JSON the API speaks carries the whole dictionary and nothing else.
    /// </summary>
    /// <remarks>
    /// The lexicon is edited centrally and read locally, so every entry crosses the wire as JSON and is
    /// rebuilt into SQLite on the far side. Anything the format cannot carry — a null that comes back as
    /// an empty string, an integer that returns as text, a column quietly dropped — would land in the
    /// local database looking like ordinary data and only surface much later as a word that inflects
    /// wrongly.
    /// <para>
    /// So the test exports the real lexicon, serializes it exactly as the API would, imports it into a
    /// fresh database, and compares the two through the SQL dump. The dump is the right comparison
    /// because it is ordered, covers every column of every table, and prints nulls and numbers
    /// distinguishably — comparing the files byte for byte would instead fail on SQLite's internal page
    /// layout, which carries no meaning.
    /// </para>
    /// </remarks>
    [TestClass]
    public sealed class LexiconRoundTripTests
    {
        // Small on purpose. The shipped lexicon has twelve lemma entries, so a page of five crosses the
        // boundary where a key compared as text stops agreeing with a key compared as a number — the
        // point at which '10' sorts below '5' and rows silently stop arriving.
        private const int PageSize = 5;

        private static string sourceDatabase = null!;
        private static string workingDirectory = null!;

        /// <summary>
        /// Locates the lexicon that ships beside the test assembly.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            sourceDatabase = Path.Combine(AppContext.BaseDirectory, "grammar.czech.lexicon.db");

            Assert.IsTrue(
                File.Exists(sourceDatabase),
                $"Lexikon '{sourceDatabase}' u testů není — nezkopíroval se do výstupu.");

            workingDirectory = Path.Combine(Path.GetTempPath(), $"lexicon-roundtrip-{Guid.NewGuid():N}");
            Directory.CreateDirectory(workingDirectory);
        }

        /// <summary>
        /// Removes the working files.
        /// </summary>
        [ClassCleanup]
        public static void CleanupClass()
        {
            if (Directory.Exists(workingDirectory))
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
        }

        /// <summary>
        /// Exporting and re-importing gives back the database it started from.
        /// </summary>
        [TestMethod]
        public void ExportThenImport_ReproducesTheLexicon()
        {
            var rebuilt = Path.Combine(workingDirectory, "rebuilt.db");
            var validation = LexiconPuller.Pull(RoundTrip(sourceDatabase), rebuilt, _ => { });

            Assert.AreEqual(
                0,
                validation.Errors.Count,
                "Naimportovaný lexikon neprošel kontrolou: " + string.Join("; ", validation.Errors));

            Assert.AreEqual(
                Dump(sourceDatabase),
                Dump(rebuilt),
                "Slovník se po průchodu JSONem změnil.");
        }

        /// <summary>
        /// The export pages the data rather than sending it in one lump, and no row is lost at a boundary.
        /// </summary>
        /// <remarks>
        /// A test that never crossed a page boundary would pass against a paging implementation that only
        /// ever returns the first page, which is the failure this whole mechanism is most likely to have.
        /// </remarks>
        [TestMethod]
        public void Export_PagesTheData_WithoutLosingRows()
        {
            var pages = LexiconJsonExporter.Export(sourceDatabase, PageSize).ToList();
            var lemmaPages = pages.Where(page => page.Table == "lemma_entry").ToList();

            Assert.IsTrue(
                lemmaPages.Count > 1,
                $"Hesla se vešla do jedné stránky ({PageSize} řádků), takže se stránkování netestuje.");

            Assert.IsNull(lemmaPages[^1].NextAfter, "Poslední stránka pořád nabízí pokračování.");

            var keys = lemmaPages
                .SelectMany(page => page.Rows)
                .Select(row => Convert.ToInt64(row[0]))
                .ToList();

            CollectionAssert.AreEquivalent(
                Enumerable.Range(1, keys.Count).Select(id => (long)id).ToArray(),
                keys,
                "Stránkování vynechalo nebo zopakovalo řádek.");
        }

        /// <summary>
        /// Every table of the schema is exported, including the ones that happen to be empty.
        /// </summary>
        [TestMethod]
        public void Export_CoversEveryTable()
            => CollectionAssert.AreEquivalent(
                LexiconSchema.Tables.Select(table => table.Name).ToArray(),
                LexiconJsonExporter.Export(sourceDatabase, PageSize)
                    .Select(page => page.Table)
                    .Distinct()
                    .ToArray());

        /// <summary>
        /// A page whose columns are not the ones the schema expects is refused rather than written.
        /// </summary>
        /// <remarks>
        /// Rows travel as bare arrays, so the column header is the only thing tying a value to a column.
        /// A server that reordered two columns of compatible type would otherwise produce a database that
        /// imports cleanly, validates cleanly, and has the wrong content.
        /// </remarks>
        [TestMethod]
        public void Import_PageWithUnexpectedColumns_Throws()
        {
            var table = LexiconSchema.Get("lexeme");
            var target = Path.Combine(workingDirectory, $"refused-{Guid.NewGuid():N}.db");

            using var importer = LexiconImporter.Create(target, force: true);

            var swapped = new LexiconPage
            {
                Table = table.Name,
                Columns = [table.Columns[0], table.Columns[2], table.Columns[1]],
                Rows = [[1L, "poznámka", "lemma"]]
            };

            var exception = Assert.ThrowsException<InvalidOperationException>(() => importer.Import(swapped));

            StringAssert.Contains(exception.Message, "sloupce");
        }

        /// <summary>
        /// A page naming a table the lexicon does not have is refused.
        /// </summary>
        [TestMethod]
        public void Import_UnknownTable_Throws()
        {
            var target = Path.Combine(workingDirectory, $"unknown-{Guid.NewGuid():N}.db");

            using var importer = LexiconImporter.Create(target, force: true);

            Assert.ThrowsException<InvalidOperationException>(
                () => importer.Import(new LexiconPage { Table = "slovicka" }));
        }

        // Serializes each page and reads it back, so the test exercises the actual JSON rather than
        // handing the importer the objects the exporter happened to build. Without this the round trip
        // would never touch a JsonElement, which is the only form the importer ever sees in production.
        private static IEnumerable<LexiconPage> RoundTrip(string databasePath)
        {
            foreach (var page in LexiconJsonExporter.Export(databasePath, PageSize))
            {
                var json = JsonSerializer.Serialize(page, LexiconPage.SerializerOptions);

                yield return JsonSerializer.Deserialize<LexiconPage>(json, LexiconPage.SerializerOptions)!;
            }
        }

        private static string Dump(string databasePath)
        {
            var file = Path.Combine(workingDirectory, $"dump-{Guid.NewGuid():N}.sql");
            LexiconDumper.Dump(databasePath, file);

            return File.ReadAllText(file, Encoding.UTF8);
        }
    }
}
