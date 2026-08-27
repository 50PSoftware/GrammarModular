using Grammar.Czech.Lexicon.Tool;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies how <see cref="ToolSettings"/> resolves the proposals-queue path — the same
    /// argument/file/environment order the tool already uses for its other settings, now unified with
    /// <c>gramatika</c> and <c>rozbor</c> so all three agree on one queue.
    /// </summary>
    [TestClass]
    public sealed class ToolSettingsTests
    {
        /// <summary>
        /// The proposals path is taken from the "navrhy" key in <c>lexikon.json</c>, resolved relative
        /// to the file rather than the working directory — the same rule <c>database</c> already follows.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void ProposalsPathComesFromTheSettingsFile()
        {
            var root = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), $"lexikon-nastaveni-{Guid.NewGuid():N}"));
            var previous = Directory.GetCurrentDirectory();

            try
            {
                File.WriteAllText(
                    Path.Combine(root.FullName, ToolSettings.FileName),
                    """{ "database": "slovnik/lexikon.db", "navrhy": "navrhy.json" }""");

                Directory.SetCurrentDirectory(root.FullName);

                var settings = ToolSettings.Load([]);

                Assert.AreEqual(Path.Combine(root.FullName, "navrhy.json"), settings.ProposalsPath);
            }
            finally
            {
                Directory.SetCurrentDirectory(previous);
                root.Delete(recursive: true);
            }
        }

        /// <summary>
        /// A value written down in <c>lexikon.json</c> wins over the environment variable when both are
        /// set — a project's own deliberate setting outranks whatever a shell happens to have.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void ProposalsPathPrefersSettingsFileOverEnvironmentVariable()
        {
            var root = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), $"lexikon-nastaveni-{Guid.NewGuid():N}"));
            var previous = Directory.GetCurrentDirectory();
            var previousEnv = Environment.GetEnvironmentVariable(ToolSettings.ProposalsPathVariable);

            try
            {
                File.WriteAllText(
                    Path.Combine(root.FullName, ToolSettings.FileName),
                    """{ "navrhy": "z-projektu.json" }""");

                Directory.SetCurrentDirectory(root.FullName);
                Environment.SetEnvironmentVariable(ToolSettings.ProposalsPathVariable, "ze-shellu.json");

                var settings = ToolSettings.Load([]);

                Assert.AreEqual(Path.Combine(root.FullName, "z-projektu.json"), settings.ProposalsPath);
            }
            finally
            {
                Directory.SetCurrentDirectory(previous);
                Environment.SetEnvironmentVariable(ToolSettings.ProposalsPathVariable, previousEnv);
                root.Delete(recursive: true);
            }
        }

        /// <summary>
        /// Without a settings file, the environment variable is still honoured — the same variable
        /// <c>gramatika</c>'s <see cref="Cli.Sentence.WordProposals"/> reads.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void ProposalsPathFallsBackToEnvironmentVariable()
        {
            var root = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), $"lexikon-nastaveni-{Guid.NewGuid():N}"));
            var previous = Directory.GetCurrentDirectory();
            var previousEnv = Environment.GetEnvironmentVariable(ToolSettings.ProposalsPathVariable);

            try
            {
                Directory.SetCurrentDirectory(root.FullName);
                Environment.SetEnvironmentVariable(ToolSettings.ProposalsPathVariable, "ze-shellu.json");

                var settings = ToolSettings.Load([]);

                Assert.AreEqual("ze-shellu.json", settings.ProposalsPath);
            }
            finally
            {
                Directory.SetCurrentDirectory(previous);
                Environment.SetEnvironmentVariable(ToolSettings.ProposalsPathVariable, previousEnv);
                root.Delete(recursive: true);
            }
        }

        /// <summary>
        /// An explicit "--soubor" argument wins over both the settings file and the environment
        /// variable — "just this once" always outranks a standing default.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void ProposalsPathArgumentWinsOverSettingsFileAndEnvironmentVariable()
        {
            var root = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), $"lexikon-nastaveni-{Guid.NewGuid():N}"));
            var previous = Directory.GetCurrentDirectory();
            var previousEnv = Environment.GetEnvironmentVariable(ToolSettings.ProposalsPathVariable);

            try
            {
                File.WriteAllText(
                    Path.Combine(root.FullName, ToolSettings.FileName),
                    """{ "navrhy": "z-projektu.json" }""");

                Directory.SetCurrentDirectory(root.FullName);
                Environment.SetEnvironmentVariable(ToolSettings.ProposalsPathVariable, "ze-shellu.json");

                var settings = ToolSettings.Load(["navrhy", "--soubor", "z-prikazove-radky.json"]);

                Assert.AreEqual("z-prikazove-radky.json", settings.ProposalsPath);
            }
            finally
            {
                Directory.SetCurrentDirectory(previous);
                Environment.SetEnvironmentVariable(ToolSettings.ProposalsPathVariable, previousEnv);
                root.Delete(recursive: true);
            }
        }
    }
}
