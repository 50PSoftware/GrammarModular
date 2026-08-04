using Microsoft.Data.Sqlite;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Writes pages fetched from the API into a lexicon database.
    /// </summary>
    /// <remarks>
    /// The importer holds one open transaction for the whole import. SQLite otherwise commits and fsyncs
    /// once per insert, which at the size the dictionary is heading for is the difference between seconds
    /// and most of an hour. It also means a failed import leaves nothing behind: the transaction is never
    /// committed, and the file it was writing is a temporary one that never replaces the real lexicon.
    /// </remarks>
    public sealed class LexiconImporter : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SqliteTransaction _transaction;
        private readonly Dictionary<string, SqliteCommand> _commands = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);

        private LexiconImporter(SqliteConnection connection)
        {
            _connection = connection;
            _transaction = connection.BeginTransaction();
        }

        /// <summary>
        /// Creates an importer over a new, empty database.
        /// </summary>
        /// <param name="path">Where to write the database.</param>
        /// <param name="force">Whether an existing file may be replaced.</param>
        /// <returns>The importer, which owns the connection.</returns>
        public static LexiconImporter Create(string path, bool force)
            => new(LexiconBuilder.CreateEmpty(path, force));

        /// <summary>
        /// Gets how many rows have been written to each table.
        /// </summary>
        public IReadOnlyDictionary<string, int> Counts => _counts;

        /// <summary>
        /// Writes one page.
        /// </summary>
        /// <param name="page">The page as the API sent it.</param>
        /// <exception cref="InvalidOperationException">
        /// The page names a table the lexicon does not have, or its columns are not the ones that table
        /// carries.
        /// </exception>
        public void Import(LexiconPage page)
        {
            var table = LexiconSchema.Get(page.Table);

            // The header is checked rather than trusted: a reordered column would write values into the
            // wrong ones, and since most are nullable text the result still opens and validates.
            if (!page.Columns.SequenceEqual(table.Columns, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Tabulka '{table.Name}': server poslal jiné sloupce, než schéma očekává.\n"
                    + $"  očekáváno: {string.Join(", ", table.Columns)}\n"
                    + $"  přišlo:    {string.Join(", ", page.Columns)}");
            }

            var command = GetCommand(table);

            foreach (var row in page.Rows)
            {
                if (row.Count != table.Columns.Count)
                {
                    throw new InvalidOperationException(
                        $"Tabulka '{table.Name}': řádek má {row.Count} hodnot, očekáváno {table.Columns.Count}.");
                }

                for (var index = 0; index < row.Count; index++)
                {
                    command.Parameters[index].Value = LexiconPage.ToDatabaseValue(row[index]);
                }

                command.ExecuteNonQuery();
                _counts[table.Name] = _counts.GetValueOrDefault(table.Name) + 1;
            }
        }

        /// <summary>
        /// Commits everything written so far.
        /// </summary>
        public void Complete() => _transaction.Commit();

        /// <summary>
        /// Releases the connection and, if <see cref="Complete"/> was never called, discards the import.
        /// </summary>
        public void Dispose()
        {
            foreach (var command in _commands.Values)
            {
                command.Dispose();
            }

            _transaction.Dispose();
            _connection.Dispose();

            // Microsoft.Data.Sqlite pools connections, and a pooled one keeps the file handle open. On
            // Windows that is enough to make the caller's rename of the finished file fail.
            SqliteConnection.ClearAllPools();
        }

        private SqliteCommand GetCommand(LexiconTable table)
        {
            if (_commands.TryGetValue(table.Name, out var existing))
            {
                return existing;
            }

            var command = _connection.CreateCommand();
            command.Transaction = _transaction;
            command.CommandText =
                $"INSERT INTO {table.Name} ({string.Join(", ", table.Columns)}) "
                + $"VALUES ({string.Join(", ", table.Columns.Select((_, index) => $"@p{index}"))})";

            for (var index = 0; index < table.Columns.Count; index++)
            {
                command.Parameters.Add(new SqliteParameter($"@p{index}", DBNull.Value));
            }

            // Prepared once and reused for every row of the table — the statement is identical and only
            // the values change.
            command.Prepare();
            _commands[table.Name] = command;

            return command;
        }
    }
}
