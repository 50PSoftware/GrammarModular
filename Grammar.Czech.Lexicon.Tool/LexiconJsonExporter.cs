using Microsoft.Data.Sqlite;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Reads a lexicon database out as the same paged JSON the API serves.
    /// </summary>
    /// <remarks>
    /// It exists for two reasons that are worth more than the code it costs. It seeds the central MySQL
    /// copy from the lexicon that is here today, and it lets the importer be tested end to end without a
    /// running server: exporting and re-importing has to give back the database it started from, and any
    /// value the wire format cannot carry shows up as a difference.
    /// </remarks>
    public static class LexiconJsonExporter
    {
        /// <summary>
        /// Reads every table of the lexicon, a page at a time.
        /// </summary>
        /// <param name="databasePath">The database to read.</param>
        /// <param name="pageSize">How many rows a page holds.</param>
        /// <returns>The pages, tables in dependency order.</returns>
        public static IEnumerable<LexiconPage> Export(string databasePath, int pageSize)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            try
            {
                foreach (var table in LexiconSchema.Tables)
                {
                    string? after = null;

                    do
                    {
                        var page = ReadPage(connection, table, after, pageSize);
                        after = page.NextAfter;

                        // A table with no rows still yields one empty page, so that a consumer sees every
                        // table named rather than having to know which ones to expect.
                        yield return page;
                    }
                    while (after is not null);
                }
            }
            finally
            {
                // Disposing returns the connection to the pool, which keeps the file open. Callers move
                // and delete these files straight afterwards.
                SqliteConnection.ClearAllPools();
            }
        }

        private static LexiconPage ReadPage(
            SqliteConnection connection,
            LexiconTable table,
            string? after,
            int pageSize)
        {
            using var command = connection.CreateCommand();

            // Keyset paging with the key compared in its own type, so the filter agrees with the ORDER
            // BY. Ordering numerically while filtering as text loses '10' behind '5' — silently.
            var filter = after is null ? string.Empty : $"WHERE {table.KeyColumn} > @after ";

            command.CommandText =
                $"SELECT {string.Join(", ", table.Columns)} FROM {table.Name} "
                + filter
                + $"ORDER BY {table.KeyColumn} LIMIT {pageSize}";

            if (after is not null)
            {
                command.Parameters.AddWithValue("@after", table.ToKeyValue(after));
            }

            using var reader = command.ExecuteReader();

            var rows = new List<IReadOnlyList<object?>>();
            string? lastKey = null;

            while (reader.Read())
            {
                var row = new object?[table.Columns.Count];

                for (var index = 0; index < row.Length; index++)
                {
                    row[index] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                }

                lastKey = row[0]?.ToString();
                rows.Add(row);
            }

            return new LexiconPage
            {
                Table = table.Name,
                Columns = table.Columns,
                Rows = rows,

                // A short page is the last one. Asking for one more row to be sure would cost a query per
                // table for the sake of a boundary case that only wastes one empty request.
                NextAfter = rows.Count == pageSize ? lastKey : null
            };
        }
    }
}
