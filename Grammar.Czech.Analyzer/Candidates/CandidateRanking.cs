using Grammar.Core.Enums;
using Grammar.Czech.Helpers;

namespace Grammar.Czech.Analyzer.Candidates
{
    /// <summary>
    /// Thins out the raw candidate list before it reaches a person.
    /// </summary>
    /// <remarks>
    /// Generate-and-test is deliberately permissive — it tries every pattern and lets the score sort
    /// out which one fits — but that means a short or frequent token which happens to have any
    /// corroborating form at all leaves one row per pattern that did not contradict it. Nothing about
    /// that is wrong, but a row that lost to a better-scoring sibling for the same word is not a
    /// second opinion worth reading, it is the same guess restated. This keeps only the
    /// best-corroborated reading(s) per word, and caps how many tied ones get shown.
    /// </remarks>
    public static class CandidateRanking
    {
        /// <summary>
        /// Keeps, per distinct lemma, only the candidates tied for that lemma's highest score, capped
        /// to at most <paramref name="maxPerWord"/> of them.
        /// </summary>
        /// <param name="candidates">The raw candidates, any order.</param>
        /// <param name="maxPerWord">How many tied top candidates to keep per word.</param>
        /// <returns>The thinned candidates, ranked by score, then by lemma's corpus frequency is left to the caller.</returns>
        public static IReadOnlyList<MatchCandidate> Thin(IEnumerable<MatchCandidate> candidates, int maxPerWord)
        {
            var result = new List<MatchCandidate>();

            foreach (var group in candidates.GroupBy(candidate => (candidate.Lemma, candidate.Category)))
            {
                var best = group.Max(candidate => candidate.Score);

                result.AddRange(group
                    .Where(candidate => candidate.Score == best)
                    // Same lemma, same pattern shows up more than once when several source tokens
                    // normalize to the one hypothesis (AdjectiveMatcher folding mladá/mladé to mladý) —
                    // that is one piece of evidence restated, not a second pattern worth a second row.
                    .DistinctBy(candidate => candidate.Pattern)
                    .OrderBy(candidate => candidate.Pattern, StringComparer.Ordinal)
                    .Take(maxPerWord));
            }

            return result;
        }

        /// <summary>
        /// Drops a noun candidate whose lemma ends in a vowel when a same-pattern candidate for the
        /// consonant-stripped spelling exists and scores at least as well.
        /// </summary>
        /// <remarks>
        /// Traced to a specific mechanism, not a guess: no noun pattern declares an ending for
        /// nominative singular — <c>Data/Rules/Nouns/patterns.json</c> never has one — so
        /// <see cref="Services.CzechNounDeclensionService.GetForm"/> returns a hypothesis lemma
        /// unchanged for that one slot regardless of shape, while every other case runs it through
        /// <see cref="Services.CzechWordStructureResolver.ExtractNounRoot"/>, which strips a trailing
        /// vowel unconditionally. A wrong-shaped hypothesis like "zápasí" (really the verb form
        /// "zápasí", not a noun) therefore generates the exact oblique-case paradigm the real noun
        /// "zápas" would — zápasu, zápase, zápasem, zápasy... — because both reduce to the same root
        /// the moment a suffix is appended, and only the pass-through nominative singular differs. The
        /// result is not a low-confidence guess to filter by score; it is a wrong-spelling duplicate of
        /// a real finding, which is why this checks for the sibling directly rather than raising the
        /// corroboration threshold. Compared across every pattern, not just the one that scored this
        /// candidate: <see cref="Thin"/> has not run yet when this does, so "zápasí" and "zápas" can
        /// each be carrying their own best-scoring pattern (město vs hrad) with nothing in common but
        /// the stripped root — and it is the root, not the specific pattern, that says one is a
        /// respelling of the other.
        /// </remarks>
        /// <param name="candidates">The candidates to filter, any order.</param>
        public static IReadOnlyList<MatchCandidate> DropVowelEndingNounDuplicates(IEnumerable<MatchCandidate> candidates)
        {
            var list = candidates.ToList();
            var suppressed = new HashSet<int>();

            for (var i = 0; i < list.Count; i++)
            {
                var candidate = list[i];

                if (candidate.Category != WordCategory.Noun
                    || candidate.Lemma.Length < 2
                    || MorphologyHelper.IsConsonant(candidate.Lemma[^1]))
                {
                    continue;
                }

                var stripped = candidate.Lemma[..^1];
                var bestSiblingScore = list
                    .Where(other => other.Category == WordCategory.Noun && other.Lemma == stripped)
                    .Select(other => (int?)other.Score)
                    .DefaultIfEmpty(null)
                    .Max();

                if (bestSiblingScore is { } score && score >= candidate.Score)
                {
                    suppressed.Add(i);
                }
            }

            return list.Where((_, index) => !suppressed.Contains(index)).ToList();
        }
    }
}
