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
    }
}
