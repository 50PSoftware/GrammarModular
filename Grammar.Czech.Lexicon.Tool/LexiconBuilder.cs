using Microsoft.Data.Sqlite;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Creates lexicon databases — empty ones for an import to fill, or seeded ones to start from.
    /// </summary>
    public static class LexiconBuilder
    {
        /// <summary>
        /// Builds a new lexicon database holding the seed entries.
        /// </summary>
        /// <param name="path">Where to write the database.</param>
        /// <param name="force">Whether an existing file may be replaced.</param>
        public static void Build(string path, bool force)
        {
            using var connection = CreateEmpty(path, force);
            using var transaction = connection.BeginTransaction();

            // One transaction across every block: a later file's frames hang off an earlier file's
            // lexemes, so a half-applied set is a database with dangling references.
            foreach (var seed in ToolResources.SeedFiles())
            {
                Execute(connection, ToolResources.Read(seed), transaction);
            }

            transaction.Commit();
        }

        /// <summary>
        /// Creates a database holding the schema and nothing else, and returns it open.
        /// </summary>
        /// <param name="path">Where to write the database.</param>
        /// <param name="force">Whether an existing file may be replaced.</param>
        /// <returns>The open connection, which the caller owns.</returns>
        /// <exception cref="InvalidOperationException">The file exists and force was not given.</exception>
        /// <remarks>
        /// Overwriting is guarded because a lexicon may hold hand-written entries that exist nowhere else,
        /// and rebuilding over them destroys work rather than regenerating it. An import writes to a
        /// temporary file and only replaces the real one once it has been validated, so it passes force.
        /// </remarks>
        public static SqliteConnection CreateEmpty(string path, bool force)
        {
            if (File.Exists(path))
            {
                if (!force)
                {
                    throw new InvalidOperationException(
                        $"Lexikon '{path}' už existuje. Build ho přepíše celý — pokud v něm jsou ručně "
                        + "psaná hesla, přijdeš o ně. Přidej --force, jestli to je záměr.");
                }

                File.Delete(path);
            }

            var directory = Path.GetDirectoryName(Path.GetFullPath(path));

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            var connection = new SqliteConnection(connectionString);
            connection.Open();

            // The settings run first: page_size only takes effect before the first table exists, and
            // foreign_keys is per connection, so it has to be on before anything writes a reference.
            Execute(connection, ToolResources.Read(ToolResources.SqliteSettings));
            Execute(connection, ToolResources.Read(ToolResources.Schema));

            return connection;
        }

        private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;
            command.ExecuteNonQuery();
        }
    }
}
