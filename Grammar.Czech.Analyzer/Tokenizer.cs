using System.Text.RegularExpressions;

namespace Grammar.Czech.Analyzer
{
    /// <summary>
    /// Splits raw text into word tokens and counts how often each one occurs.
    /// </summary>
    /// <remarks>
    /// Deliberately not a real Czech tokenizer — no clitic splitting, no sentence boundaries, no
    /// abbreviation handling. All the matcher needs is "which letter-sequences occur, and how often",
    /// so a plain letter-run regex is the right amount of machinery for what reads it next.
    /// </remarks>
    public static partial class Tokenizer
    {
        [GeneratedRegex(@"\p{L}+", RegexOptions.Compiled)]
        private static partial Regex WordPattern();

        /// <summary>
        /// Tokenizes the text and counts occurrences per case-folded word.
        /// </summary>
        /// <param name="text">The text to tokenize.</param>
        /// <returns>Case-folded token to occurrence count, for tokens of at least two letters.</returns>
        public static IReadOnlyDictionary<string, int> CountTokens(string text)
        {
            var counts = new Dictionary<string, int>();

            foreach (Match match in WordPattern().Matches(text))
            {
                if (match.Length < 2)
                {
                    continue;
                }

                var key = match.Value.ToLowerInvariant();
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }

            return counts;
        }
    }
}
