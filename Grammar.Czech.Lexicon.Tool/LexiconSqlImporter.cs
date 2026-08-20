namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Builds a lexicon database from a portable SQL dump — the counterpart of <see cref="LexiconDumper"/>.
    /// </summary>
    /// <remarks>
    /// The dump this reads is the same format <see cref="LexiconDumper"/> writes and the PHP admin's
    /// export route produces: an empty schema followed by <c>INSERT</c> statements, parents first. It
    /// exists because the admin's export is what a signed-in browser can fetch when <c>pull</c> cannot —
    /// see ExportController.php — and the result still needs to become a usable local database.
    /// </remarks>
    public static class LexiconSqlImporter
    {
        /// <summary>
        /// Creates a database at <paramref name="databasePath"/> and fills it from the dump.
        /// </summary>
        /// <param name="dumpPath">The .sql file to read — schema-less, just the INSERT statements.</param>
        /// <param name="databasePath">Where to write the database.</param>
        /// <param name="force">Whether an existing file at <paramref name="databasePath"/> may be replaced.</param>
        public static void Import(string dumpPath, string databasePath, bool force)
        {
            var sql = File.ReadAllText(dumpPath);

            using var connection = LexiconBuilder.CreateEmpty(databasePath, force);
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();

            // One statement for the whole file, same as LexiconBuilder does with a seed: Microsoft.Data.Sqlite
            // runs a semicolon-separated batch in one call, and a dump is just INSERTs, so there is nothing
            // here to parse or split.
            command.CommandText = sql;
            command.Transaction = transaction;
            command.ExecuteNonQuery();

            transaction.Commit();
        }
    }
}
