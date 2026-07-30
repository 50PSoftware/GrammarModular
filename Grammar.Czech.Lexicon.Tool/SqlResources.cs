using System.Reflection;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Reads the SQL files shipped with the tool.
    /// </summary>
    internal static class SqlResources
    {
        /// <summary>
        /// The portable data definition, shared by every backend.
        /// </summary>
        public const string Schema = "Schema.schema";

        /// <summary>
        /// The SQLite-only settings applied on top of <see cref="Schema"/>.
        /// </summary>
        public const string SqliteSettings = "Schema.schema.sqlite";

        /// <summary>
        /// The starting contents of the dictionary.
        /// </summary>
        public const string Seed = "Data.seed";

        /// <summary>
        /// Reads an embedded SQL file.
        /// </summary>
        /// <param name="name">The resource path without the assembly prefix or the .sql suffix.</param>
        /// <returns>The contents of the file.</returns>
        public static string Read(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resource = $"{assembly.GetName().Name}.{name}.sql";

            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException(
                    $"Zdroj '{resource}' není v sestavení. Dostupné: "
                    + string.Join(", ", assembly.GetManifestResourceNames()) + ".");

            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
