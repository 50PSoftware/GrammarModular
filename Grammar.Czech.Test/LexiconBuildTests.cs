using Grammar.Czech.Lexicon.Tool;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Builds the lexicon from the schema and the seed files and checks the result.
    /// </summary>
    /// <remarks>
    /// Every other test here reads the database that is committed to the repository, which means a seed
    /// file can be broken while the whole suite stays green — the built artefact still holds whatever
    /// was correct when it was last built. That gap is not hypothetical: a fix to seed.001.sql was lost
    /// when the file was overwritten, and nothing noticed, because the committed database still had the
    /// fix baked in and `build` is not something the tests ran.
    /// <para>
    /// So this one starts from the sources. It is the only test that would fail on a seed file nobody
    /// can apply.
    /// </para>
    /// </remarks>
    [TestClass]
    public sealed class LexiconBuildTests
    {
        /// <summary>
        /// The schema and every seed block apply cleanly and validate.
        /// </summary>
        [TestMethod]
        public void Build_FromSources_ValidatesWithoutErrors()
        {
            var path = Path.Combine(Path.GetTempPath(), $"lexicon-build-{Guid.NewGuid():N}.db");

            try
            {
                LexiconBuilder.Build(path, force: true);

                var validation = LexiconValidator.Validate(path);

                Assert.AreEqual(
                    0,
                    validation.Errors.Count,
                    "Lexikon postavený ze zdrojů neprošel kontrolou:\n  "
                    + string.Join("\n  ", validation.Errors));
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        /// <summary>
        /// What the sources build is what the repository ships.
        /// </summary>
        /// <remarks>
        /// The database is committed because everything else reads it, and a committed artefact drifts
        /// from the sources it was built from unless something says otherwise. Comparing through the SQL
        /// dump rather than byte for byte, since SQLite's page layout carries no meaning.
        /// </remarks>
        [TestMethod]
        public void Build_FromSources_MatchesTheCommittedDatabase()
        {
            var built = Path.Combine(Path.GetTempPath(), $"lexicon-build-{Guid.NewGuid():N}.db");
            var shipped = Path.Combine(AppContext.BaseDirectory, "grammar.czech.lexicon.db");

            Assert.IsTrue(File.Exists(shipped), $"Lexikon '{shipped}' u testů není.");

            try
            {
                LexiconBuilder.Build(built, force: true);

                Assert.AreEqual(
                    Dump(shipped),
                    Dump(built),
                    "Committnutý lexikon neodpovídá seed souborům — spusť `lexikon build --force`.");
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                foreach (var file in Directory.GetFiles(
                    Path.GetTempPath(), Path.GetFileNameWithoutExtension(built) + "*"))
                {
                    File.Delete(file);
                }
            }
        }

        private static string Dump(string databasePath)
        {
            var file = Path.Combine(Path.GetTempPath(), $"lexicon-dump-{Guid.NewGuid():N}.sql");

            try
            {
                LexiconDumper.Dump(databasePath, file);

                return File.ReadAllText(file);
            }
            finally
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }
    }
}
