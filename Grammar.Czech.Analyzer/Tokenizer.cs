using System.Text.RegularExpressions;

namespace Grammar.Czech.Analyzer
{
    /// <summary>
    /// Splits raw text into word tokens, counts how often each one occurs, and flags which ones look
    /// like proper nouns.
    /// </summary>
    /// <remarks>
    /// Deliberately not a real Czech tokenizer — no clitic splitting, no abbreviation handling. All
    /// the matcher needs is "which letter-sequences occur, how often, and does this one look like a
    /// name" — a plain letter-run regex plus a light capitalization heuristic is the right amount of
    /// machinery for what reads it next.
    /// </remarks>
    public static partial class Tokenizer
    {
        private static readonly char[] SentenceEnders = ['.', '!', '?'];

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

        /// <summary>
        /// Finds words that look like proper nouns, so the matcher does not propose "Praha" as a
        /// candidate common noun.
        /// </summary>
        /// <remarks>
        /// A word counts as a likely proper noun when it is capitalized somewhere that is not the
        /// start of a sentence, and never appears lowercase anywhere in the text. Sentence-initial
        /// capitalization proves nothing in Czech — every sentence starts that way — so what actually
        /// signals a name is capitalization in a position where ordinary prose would not have it. The
        /// "never lowercase" half guards the case where a genuinely common word only ever happens to
        /// open a sentence in this particular text; without it, "Pes" opening every sentence about a
        /// dog would misfire as a name.
        /// <para>
        /// Sentence boundaries are approximated the same crude way as everywhere else in this project —
        /// . ! ? between two tokens — which is wrong for abbreviations and initials, but wrong in the
        /// direction of caution: it makes fewer words count as sentence-initial, so it undercounts
        /// proper nouns rather than overcounts them.
        /// </para>
        /// </remarks>
        /// <param name="text">The text to scan.</param>
        /// <returns>Case-folded words that look like proper nouns.</returns>
        public static IReadOnlySet<string> FindLikelyProperNouns(string text)
        {
            var capitalizedMidSentence = new HashSet<string>();
            var seenLowercase = new HashSet<string>();
            var sentenceStart = true;
            var previousEnd = 0;

            foreach (Match match in WordPattern().Matches(text))
            {
                if (text[previousEnd..match.Index].IndexOfAny(SentenceEnders) >= 0)
                {
                    sentenceStart = true;
                }

                if (match.Length >= 2)
                {
                    var key = match.Value.ToLowerInvariant();

                    if (char.IsUpper(match.Value[0]))
                    {
                        if (!sentenceStart)
                        {
                            capitalizedMidSentence.Add(key);
                        }
                    }
                    else
                    {
                        seenLowercase.Add(key);
                    }
                }

                previousEnd = match.Index + match.Length;
                sentenceStart = false;
            }

            capitalizedMidSentence.ExceptWith(seenLowercase);

            return capitalizedMidSentence;
        }
    }
}
