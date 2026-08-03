using System.Reflection;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Reads the data files compiled into the tool.
    /// </summary>
    /// <remarks>
    /// Everything the tool has to read at run time is embedded rather than copied next to the assembly.
    /// That covers the SQL, and also the one PHP file the tests compare against: nothing in a .NET build
    /// output has any use for PHP source, and the file only needs to be readable, not present.
    /// </remarks>
    public static class ToolResources
    {
        /// <summary>
        /// The portable data definition, shared by every backend.
        /// </summary>
        public const string Schema = "Schema.schema.sql";

        /// <summary>
        /// The SQLite-only settings applied on top of <see cref="Schema"/>.
        /// </summary>
        public const string SqliteSettings = "Schema.schema.sqlite.sql";

        /// <summary>
        /// The MySQL variant of <see cref="Schema"/>, used by the central editable copy.
        /// </summary>
        /// <remarks>
        /// A separate file rather than a delta, because a table cannot be altered into existence and the
        /// types differ from the first line. The two are held together by a test comparing the tables,
        /// columns and constrained values they declare.
        /// </remarks>
        public const string MysqlSchema = "Schema.schema.mysql.sql";

        /// <summary>
        /// Lists the seed files in the order they have to be applied.
        /// </summary>
        /// <returns>Resource names, lowest number first.</returns>
        /// <remarks>
        /// The dictionary is written in numbered blocks — seed.000.sql, seed.001.sql — because each one
        /// continues the identifiers of the last and none of them can be replayed on its own. Ordering is
        /// therefore not cosmetic: applying 001 before 000 fails on a foreign key, which is the good case.
        /// <para>
        /// The number is in the name rather than the base file being special-cased, so that sorting the
        /// names ordinally is the whole of the rule. A base called seed.sql would sort <em>after</em>
        /// seed.001.sql, since '0' precedes 's'.
        /// </para>
        /// <para>
        /// These are the bootstrap corpus, not a running log. Once the central database is live, entries
        /// are written in the admin and pulled from there; a seed file added after that point is a second
        /// source of truth that every pull silently discards.
        /// </para>
        /// </remarks>
        public static IReadOnlyList<string> SeedFiles()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var prefix = $"{assembly.GetName().Name}.Data.seed.";

            return assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(prefix, StringComparison.Ordinal)
                    && name.EndsWith(".sql", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .Select(name => name[(assembly.GetName().Name!.Length + 1)..])
                .ToList();
        }

        /// <summary>
        /// The server's copy of the column map and the permitted values.
        /// </summary>
        /// <remarks>
        /// Embedded so that PhpSchemaParityTests can check it against <see cref="LexiconSchema"/> and the
        /// C# enums. The admin builds its forms from this list and the provider parses what comes back
        /// case-sensitively, so the two drifting apart is a value nobody can enter or one that saves and
        /// then throws on the first lookup that touches it.
        /// </remarks>
        public const string PhpSchemaTables = "Php.schema-tables.php";

        /// <summary>
        /// Reads an embedded file.
        /// </summary>
        /// <param name="name">The resource path below the assembly name, extension included.</param>
        /// <returns>The contents of the file.</returns>
        public static string Read(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resource = $"{assembly.GetName().Name}.{name}";

            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException(
                    $"Zdroj '{resource}' není v sestavení. Dostupné: "
                    + string.Join(", ", assembly.GetManifestResourceNames()) + ".");

            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
