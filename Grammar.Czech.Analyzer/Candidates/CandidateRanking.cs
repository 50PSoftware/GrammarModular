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
        /// Decides whether a token should be tried as a noun at all, given how many verb candidates the
        /// same token already produced.
        /// </summary>
        /// <remarks>
        /// "í" is the one noun-citation ending that collides with a verb's own shape — class 3's plural
        /// "jí" and most of class 4's singular — so a token shaped like a citation form (na)declares
        /// nothing about which one it really is. Real í-ending nouns (stavení, rozhodčí) are a small,
        /// closed set that no real verb reconstructs from, so this is not a score comparison — "změní"
        /// out-corroborated the "změnit" already found for the same token on real text frequency, not a
        /// weaker guess — it is a shape one: a token whose route to a noun candidate is generate-and-test
        /// blindly trying every pattern should not get to compete once something specific to its own
        /// ending already explained it.
        /// </remarks>
        /// <param name="token">The case-folded token under consideration.</param>
        /// <param name="verbCandidateCount">How many verb candidates the same token already produced.</param>
        public static bool ShouldTryAsNoun(string token, int verbCandidateCount) =>
            !(token.EndsWith("í", StringComparison.Ordinal) && verbCandidateCount > 0);

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
        /// <para>
        /// A tie within the root group is resolved toward the shorter lemma — "kandidát" (9) over
        /// "kandidáti" (10) — once <see cref="NounMatcher"/> started reconstructing a nominative
        /// singular from a plural token: the reconstruction and the token-as-its-own-lemma reading
        /// share the identical paradigm and therefore the identical score whenever <c>kandidát</c>
        /// itself never appears in the text, and a genuine nominative singular is never longer than an
        /// inflected form built on top of it. This is narrower than the class-4 verb tie-break — no
        /// single spelling is "the ordinary one" across every pattern the way "-at" was for class 5 —
        /// but shorter-wins needs no per-pattern table and happens to be exactly right for a suffix
        /// stripped down to the bare root.
        /// </para>
        /// <para>
        /// A root that is itself already known drops the whole group, not just its weaker members —
        /// "jeví" (the verb jevit se, not a noun at all) reduces to "jev", an ordinary hrad-pattern
        /// noun already on file. "jev" never competes as a candidate — it is not a gap — so nothing in
        /// the group above would have caught this without asking <paramref name="isKnown"/> directly:
        /// the borrowed paradigm (jevu, jevy, jevů, all real) belongs to the known word, and every
        /// candidate sharing its root is retelling that word's paradigm, not finding a new one.
        /// </para>
        /// </remarks>
        /// <param name="candidates">The candidates to filter, any order.</param>
        /// <param name="isKnown">Whether a given lemma is already known, independent of this candidate list.</param>
        public static IReadOnlyList<MatchCandidate> DropVowelEndingNounDuplicates(
            IEnumerable<MatchCandidate> candidates, Func<string, bool> isKnown)
        {
            var list = candidates.ToList();
            var result = new List<MatchCandidate>();

            foreach (var group in list.Where(c => c.Category == WordCategory.Noun).GroupBy(NounRoot))
            {
                if (isKnown(group.Key))
                {
                    continue;
                }

                var best = group.Max(c => c.Score);
                var atBest = group.Where(c => c.Score == best).ToList();
                var shortest = atBest.Min(c => c.Lemma.Length);

                result.AddRange(atBest.Where(c => c.Lemma.Length == shortest));
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
