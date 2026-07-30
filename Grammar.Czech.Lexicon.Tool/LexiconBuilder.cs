using Microsoft.Data.Sqlite;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Creates a lexicon database from the schema and the seed.
    /// </summary>
    internal static class LexiconBuilder
    {
        /// <summary>
        /// Builds a new lexicon database at the supplied path.
        /// </summary>
        /// <param name="path">Where to write the database.</param>
        /// <param name="force">Whether an existing file may be replaced.</param>
        /// <remarks>
        /// The database is the authored source of the dictionary, so building over one already holding
        /// hand-written entries would destroy work rather than regenerate it. That is what force guards.
        /// </remarks>
        public static void Build(string path, bool force)
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

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // The pragmas run first: foreign_keys is per connection and would otherwise not be watching
            // while the seed inserts, which is the one moment the references are actually written.
            Execute(connection, SqlResources.Read(SqlResources.SqliteSettings));
            Execute(connection, SqlResources.Read(SqlResources.Schema));

            using (var transaction = connection.BeginTransaction())
            {
                Execute(connection, SqlResources.Read(SqlResources.Seed), transaction);
                transaction.Commit();
            }
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
