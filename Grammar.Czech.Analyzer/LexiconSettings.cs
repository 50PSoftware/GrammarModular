using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Analyzer
{
    /// <summary>
    /// Reads the lexicon path out of the project settings file the other two tools already use.
    /// </summary>
    /// <remarks>
    /// A small, deliberate copy of <c>Grammar.Czech.Cli.LexiconSettings</c> — see that type's remarks
    /// for why: referencing either sibling tool from here to avoid repeating one file name, one key and
    /// an upward search would pull in a whole unrelated .NET tool for three lines of code.
    /// </remarks>
    public static class LexiconSettings
    {
        /// <summary>
        /// The name of the settings file, matching the other two tools.
        /// </summary>
        public const string FileName = "lexikon.json";

        /// <summary>
        /// Finds the lexicon path a settings file names, if there is one.
        /// </summary>
        /// <returns>The path, or <see langword="null"/> when no file names one.</returns>
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
                throw new InvalidOperationException($"Soubor '{path}' nejde přečíst jako JSON: {exception.Message}");
            }

            if (string.IsNullOrWhiteSpace(file?.Database))
            {
                return null;
            }

            return Path.IsPathRooted(file.Database)
                ? file.Database
                : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, file.Database));
        }

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
