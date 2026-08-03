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
        /// The starting contents of the dictionary.
        /// </summary>
        public const string Seed = "Data.seed.sql";

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
