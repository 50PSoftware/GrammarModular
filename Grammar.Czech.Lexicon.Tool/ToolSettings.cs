using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Collects what the tool needs to be told, from the command line, a settings file, or the
    /// environment.
    /// </summary>
    /// <remarks>
    /// Three sources because each answers a different question. An argument is what you want this once;
    /// the file is what this project always wants and belongs in version control; the environment is
    /// what this machine knows and nothing else should. The order follows from that — an argument beats
    /// the file, and the file beats the environment, since a value written down for the project is more
    /// deliberate than one left in a shell.
    /// <para>
    /// It also settles where the token lives. Commit the file with the address and the destination, keep
    /// the token in the environment, and the two never meet: a key absent from the file falls through,
    /// so the file does not have to carry a placeholder that somebody eventually fills in and pushes.
    /// </para>
    /// </remarks>
    public sealed class ToolSettings
    {
        /// <summary>
        /// The name of the settings file, looked for in the working directory and its parents.
        /// </summary>
        public const string FileName = "lexikon.json";

        private ToolSettings(SettingsFile file, string? source, string[] args)
        {
            SettingsPath = source;

            Url = Argument(args, "--url") ?? Blank(file.Url) ?? Variable("LEXICON_API_URL");
            Token = Argument(args, "--token") ?? Blank(file.Token) ?? Variable("LEXICON_API_TOKEN");

            // Jen --db. --out tady schválně není: u build a pull je to cíl lexikonu, ale u dump
            // a export-json je to výstupní soubor nebo adresář. Kdyby ho hltalo tohle, `dump --out
            // vypis.sql` by si tu .sql cestu vzalo za cestu ke slovníku a pokusilo se ji otevřít jako
            // databázi. Co --out znamená, ví jen ten který příkaz.
            DatabasePath = Argument(args, "--db") ?? Relative(Blank(file.Database), source);

            PageSize = int.TryParse(Argument(args, "--page-size"), out var size) && size > 0
                ? size
                : file.PageSize > 0 ? file.PageSize : 5000;
        }

        /// <summary>
        /// Gets the settings file that was used, or <see langword="null"/> when none was found.
        /// </summary>
        public string? SettingsPath { get; }

        /// <summary>
        /// Gets the API address, or <see langword="null"/> when it is configured nowhere.
        /// </summary>
        public string? Url { get; }

        /// <summary>
        /// Gets the bearer token, or <see langword="null"/> when it is configured nowhere.
        /// </summary>
        public string? Token { get; }

        /// <summary>
        /// Gets the lexicon path, or <see langword="null"/> to fall back to searching the repository.
        /// </summary>
        public string? DatabasePath { get; }

        /// <summary>
        /// Gets how many rows to ask for at a time.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Reads the settings for this invocation.
        /// </summary>
        /// <param name="args">The command line.</param>
        /// <returns>The resolved settings.</returns>
        public static ToolSettings Load(string[] args)
        {
            var path = FindSettingsFile();

            if (path is null)
            {
                return new ToolSettings(new SettingsFile(), null, args);
            }

            try
            {
                var file = JsonSerializer.Deserialize<SettingsFile>(
                    File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return new ToolSettings(file ?? new SettingsFile(), path, args);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"Soubor '{path}' nejde přečíst jako JSON: {exception.Message}");
            }
        }

        /// <summary>
        /// Gets the API address, or explains where it could have come from.
        /// </summary>
        /// <exception cref="InvalidOperationException">It is configured nowhere.</exception>
        public string RequireUrl()
            => Url ?? throw new InvalidOperationException(
                "Chybí adresa API. Předej ji přes --url, zapiš do "
                + $"{FileName} jako \"url\", nebo nastav LEXICON_API_URL.");

        // Walks up from the working directory, so the tool can be installed globally and still find the
        // settings of whichever project it is invoked in.
        private static string? FindSettingsFile()
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

        private static string? Argument(string[] args, string name)
        {
            var index = Array.IndexOf(args, name);

            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        // A relative path in the file is relative to the file, not to wherever the tool was invoked. The
        // settings are looked for up the directory tree precisely so they can be used from anywhere
        // below them, and resolving against the working directory would undo that: the same setting
        // would mean a different file in every subdirectory. A path typed as an argument is left alone —
        // that one is relative to the shell it was typed in.
        private static string? Relative(string? value, string? settingsPath)
            => value is null || settingsPath is null || Path.IsPathRooted(value)
                ? value
                : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsPath)!, value));

        // An empty string in the file means "not set here", so that a key left blank falls through to the
        // environment instead of overriding it with nothing.
        private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private static string? Variable(string name) => Blank(Environment.GetEnvironmentVariable(name));

        private sealed class SettingsFile
        {
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("token")]
            public string? Token { get; set; }

            [JsonPropertyName("database")]
            public string? Database { get; set; }

            [JsonPropertyName("pageSize")]
            public int PageSize { get; set; }
        }
    }
}
