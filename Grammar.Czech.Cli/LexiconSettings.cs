using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Cli
{
    /// <summary>
    /// Reads the lexicon path out of the project settings file the lexicon tool already uses.
    /// </summary>
    /// <remarks>
    /// The two tools work on the same dictionary and there is no reason for a project to have to say
    /// where it is twice. <c>lexikon.json</c> is where a project writes down what it always wants, it is
    /// looked for by walking up from the working directory, and the path in it is taken relative to the
    /// file rather than to the working directory — otherwise the same setting would mean a different
    /// file in every subdirectory.
    /// <para>
    /// Only the <c>database</c> key is read. The address and the token belong to the tool that talks to
    /// the API, and this application never does; a key it does not understand is simply ignored, so the
    /// same file serves both.
    /// </para>
    /// <para>
    /// This is a small copy of what <c>ToolSettings</c> does, and it is a copy on purpose: the two are
    /// separate .NET tools and referencing the lexicon tool from here would pull the whole of it —
    /// importer, API client, seeds — into a package that generates sentences. What is duplicated is the
    /// file name, one key and the upward search, and both sides state the same rule.
    /// </para>
    /// </remarks>
    public static class LexiconSettings
    {
        /// <summary>
        /// The name of the settings file, matching the lexicon tool.
        /// </summary>
        public const string FileName = "lexikon.json";

        /// <summary>
        /// Finds the lexicon path a settings file names, if there is one.
        /// </summary>
        /// <returns>The path, or <see langword="null"/> when no file names one.</returns>
        /// <exception cref="CliException">Thrown when the settings file cannot be read.</exception>
        public static string? DatabasePath()
        {
            if (FindFile() is not { } path)
            {
                return null;
            }

            SettingsFile? file;

            try
            {
                file = JsonSerializer.Deserialize<SettingsFile>(
                    File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException exception)
            {
                throw new CliException($"Soubor '{path}' nejde přečíst jako JSON: {exception.Message}");
            }

            // Prázdný klíč znamená „tady nenastaveno" a propadne dál na proměnnou prostředí, místo aby
            // ji přebil ničím.
            if (string.IsNullOrWhiteSpace(file?.Database))
            {
                return null;
            }

            return Path.IsPathRooted(file.Database)
                ? file.Database
                : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, file.Database));
        }

        // Nahoru od pracovního adresáře, takže globálně nainstalovaný nástroj najde nastavení toho
        // projektu, ve kterém je zrovna spuštěný.
        private static string? FindFile()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, FileName);

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private sealed class SettingsFile
        {
            [JsonPropertyName("database")]
            public string? Database { get; set; }
        }
    }
}
