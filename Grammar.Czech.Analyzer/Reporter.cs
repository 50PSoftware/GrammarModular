using System.Globalization;
using Grammar.Czech.Analyzer.Candidates;

namespace Grammar.Czech.Analyzer
{
    /// <summary>
    /// Writes ranked candidates to CSV, in the same shape a person would fill in by hand while
    /// checking against IJP — this only ever proposes, it never writes a seed.
    /// </summary>
    public static class Reporter
    {
        private const string IjpSearchUrl = "https://prirucka.ujc.cas.cz/?slovo={0}";

        /// <summary>
        /// Writes the ranked candidates to a CSV file.
        /// </summary>
        /// <param name="candidates">The candidates, already ranked.</param>
        /// <param name="corpus">Token occurrence counts, for the frequency column.</param>
        /// <param name="path">Where to write the CSV.</param>
        public static void WriteCsv(
            IReadOnlyList<MatchCandidate> candidates,
            IReadOnlyDictionary<string, int> corpus,
            string path)
        {
            using var writer = new StreamWriter(path, append: false, System.Text.Encoding.UTF8);

            writer.WriteLine("poradi,slovo,kategorie,vzor,rod,zivotnost,cetnost,skore,tvary,ijp,poznamka");

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];

                writer.WriteLine(string.Join(",",
                    (i + 1).ToString(CultureInfo.InvariantCulture),
                    Csv(candidate.Lemma),
                    Csv(candidate.Category.ToString()),
                    Csv(candidate.Pattern),
                    Csv(candidate.Gender?.ToString() ?? ""),
                    Csv(candidate.IsAnimate?.ToString() ?? ""),
                    corpus[candidate.Lemma].ToString(CultureInfo.InvariantCulture),
                    candidate.Score.ToString(CultureInfo.InvariantCulture),
                    Csv(string.Join(" ", candidate.MatchedForms)),
                    Csv(string.Format(CultureInfo.InvariantCulture, IjpSearchUrl, candidate.Lemma)),
                    ""));
            }
        }

        private static string Csv(string value) =>
            value.Contains(',') || value.Contains('"')
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
    }
}
