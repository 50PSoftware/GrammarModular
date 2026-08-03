namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Replaces the local lexicon with a fresh copy pulled from the API.
    /// </summary>
    public static class LexiconPuller
    {
        /// <summary>
        /// Pulls the whole dictionary and, if it validates, puts it in place.
        /// </summary>
        /// <param name="pages">The pages to import, in dependency order.</param>
        /// <param name="destination">Where the finished lexicon belongs.</param>
        /// <param name="report">Receives progress messages.</param>
        /// <returns>The validation report of the imported database.</returns>
        /// <remarks>
        /// The pull writes to a temporary file beside the destination and moves it into place only after
        /// validation passes. Three things follow from that, all of them wanted: a failed or interrupted
        /// pull leaves the working lexicon untouched, a half-written file is never what anything reads,
        /// and the move is a rename within one directory, which the filesystem does atomically.
        /// <para>
        /// Validation is the gate rather than a formality. A paged pull is not a consistent snapshot —
        /// nothing stops the dictionary being edited between the request for one page and the next — and
        /// a lemma that arrived pointing at a lexeme added after its page had been sent shows up here as
        /// a broken foreign key rather than as a lemma that quietly fails to resolve later.
        /// </para>
        /// </remarks>
        public static ValidationReport Pull(
            IEnumerable<LexiconPage> pages,
            string destination,
            Action<string> report)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(destination));

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return Apply(pages, destination + ".incoming", destination, report);
        }

        /// <summary>
        /// Fetches the whole dictionary and validates it without keeping any of it.
        /// </summary>
        /// <param name="pages">The pages to check, in dependency order.</param>
        /// <param name="report">Receives progress messages.</param>
        /// <returns>The validation report of what the server is serving.</returns>
        /// <remarks>
        /// This is how entries written in the admin get checked. They live only in the central database
        /// until something pulls them, and nothing on the server enforces what LexiconValidator does — a
        /// frame with no actor, a slot with no realization at preference 1, a lookup key that no lookup
        /// will match. Running the check answers "is what I just typed loadable" without waiting for the
        /// next real pull and without touching the working lexicon either way.
        /// </remarks>
        public static ValidationReport Check(IEnumerable<LexiconPage> pages, Action<string> report)
            => Apply(
                pages,
                Path.Combine(Path.GetTempPath(), $"lexicon-check-{Guid.NewGuid():N}.db"),
                destination: null,
                report);

        // One path for both, because the only difference is whether the finished file is kept. Building
        // it somewhere else and moving it into place at the end is what makes a failed pull leave the
        // working lexicon untouched, and it costs nothing to also not move it.
        private static ValidationReport Apply(
            IEnumerable<LexiconPage> pages,
            string workingPath,
            string? destination,
            Action<string> report)
        {
            var temporaryPath = workingPath;

            try
            {
                using (var importer = LexiconImporter.Create(temporaryPath, force: true))
                {
                    foreach (var page in pages)
                    {
                        importer.Import(page);
                    }

                    importer.Complete();

                    foreach (var (table, count) in importer.Counts)
                    {
                        report($"  {table,-18} {count,7}");
                    }

                    if (importer.Counts.GetValueOrDefault("lemma_entry") == 0)
                    {
                        throw new InvalidOperationException(
                            "Server nevrátil ani jedno heslo. Lokální lexikon zůstává, jaký byl.");
                    }
                }

                var validation = LexiconValidator.Validate(temporaryPath);

                if (destination is not null && validation.Errors.Count == 0)
                {
                    File.Move(temporaryPath, destination, overwrite: true);
                }

                return validation;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }
}
