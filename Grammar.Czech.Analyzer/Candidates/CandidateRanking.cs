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
        /// Keeps only the best-scoring noun candidate(s) among those that reduce to the same root once
        /// a trailing vowel is stripped.
        /// </summary>
        /// <remarks>
        /// Traced to a specific mechanism, not a guess: no noun pattern declares an ending for
        /// nominative singular — <c>Data/Rules/Nouns/patterns.json</c> never has one — so
        /// <see cref="Services.CzechNounDeclensionService.GetForm"/> returns a hypothesis lemma
        /// unchanged for that one slot regardless of shape, while every other case runs it through
        /// <see cref="Services.CzechWordStructureResolver.ExtractNounRoot"/>, which strips a trailing
        /// vowel unconditionally. A wrong-shaped hypothesis like "zápasí" (really the verb form, not a
        /// noun) therefore generates the exact oblique-case paradigm the real noun "zápas" would —
        /// zápasu, zápase, zápasem... — because both reduce to the same root the moment a suffix is
        /// appended, and only the pass-through nominative singular differs.
        /// <para>
        /// Grouping by the stripped root rather than checking one exact spelling against another is
        /// what catches "změní" against "změna" too: "zápas" happens to end in a consonant, so
        /// stripping one vowel off "zápasí" lands on it exactly, but "změna" has a vowel ending of its
        /// own — stripping "a" and stripping "í" both land on "změn", and it is that shared root, not
        /// either spelling, that says one is a wrong-category duplicate of the other. Every noun
        /// candidate reduces to a root the same way (or is left alone, unchanged, if it already ends in
        /// a consonant), so one grouping rule covers both shapes of the coincidence.
        /// </para>
        /// <para>
        /// Compared across every pattern, not just the one that scored a given candidate: <see cref="Thin"/>
        /// has not run yet when this does, so two candidates sharing a root can each be carrying their
        /// own best-scoring pattern with nothing else in common.
        /// </para>
        /// </remarks>
        /// <param name="candidates">The candidates to filter, any order.</param>
        public static IReadOnlyList<MatchCandidate> DropVowelEndingNounDuplicates(IEnumerable<MatchCandidate> candidates)
        {
            var list = candidates.ToList();
            var result = new List<MatchCandidate>();

            foreach (var group in list.Where(c => c.Category == WordCategory.Noun).GroupBy(NounRoot))
            {
                var best = group.Max(c => c.Score);
                result.AddRange(group.Where(c => c.Score == best));
            }

            result.AddRange(list.Where(c => c.Category != WordCategory.Noun));

            return result;
        }

        // Mirrors CzechWordStructureResolver.ExtractNounRoot: a lemma ending in a vowel loses it,
        // everything else is the root as-is. Not calling into the service itself because this only
        // needs the same one-line rule, not the mobile-e/epenthesis machinery that comes with it.
        private static string NounRoot(MatchCandidate candidate) =>
            candidate.Lemma.Length > 1 && !MorphologyHelper.IsConsonant(candidate.Lemma[^1])
                ? candidate.Lemma[..^1]
                : candidate.Lemma;
    }
}
