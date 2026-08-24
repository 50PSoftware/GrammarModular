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
        /// <para>
        /// L-participle agreement (l/la/lo/li/ly) collides the same way, and worse: unlike "í", none of
        /// its five endings are themselves rare, so every regular verb's own past tense leaves four or
        /// five sibling forms (existoval, existovalo, existovala, existovali, existovaly...) sitting in
        /// the text ready to corroborate each other as a fake žena- or město-pattern noun paradigm —
        /// "existovalo" (nominative singular, the token itself) with "existoval" as its own genitive
        /// plural, "existovala" as its genitive singular, and so on, entirely explained by verb
        /// agreement and nothing to do with noun declension. Nothing distinguishes this from a real
        /// noun's own paradigm by shape alone — <see cref="NounMatcher"/> tries every pattern blindly by
        /// design — so this only works once <see cref="VerbMatcher"/> can actually reconstruct the
        /// infinitive an l-participle token belongs to and corroborate it as a verb in its own right;
        /// the same shape check that already worked for "í" then applies unchanged.
        /// </para>
        /// <para>
        /// "jít" and its prefixed compounds (přijít, odejít, vyjít, dojít, najít, sejít se...) never get
        /// that corroboration, and never can: their l-participle is suppletive — "-jít" becomes "-šel",
        /// not a chopped-off "-t" — so class 1 is the only class that could try them, and
        /// <see cref="Services.CzechWordStructureResolver.DeriveTrida1"/> refuses on principle rather than
        /// hand back a wrong stem (nést → nél was the standing example of the alternative). "přišel"
        /// therefore corroborates zero verb candidates no matter how <see cref="VerbMatcher"/> reconstructs,
        /// and would slip through the count-based check above the same way it did before l-participle
        /// endings were recognized at all — "přišel"/"přišla"/"přišli" scored as a fake pán-pattern noun
        /// on the same real article. Unlike everywhere else in this class, this one shape is hard-coded
        /// rather than generate-and-test, because there is nothing left to generate and test: the jít
        /// compounds are a small, closed, genuinely irregular family (not a productive ending any other
        /// verb shares), so recognizing "-šel"/"-šla"/"-šlo"/"-šli"/"-šly" directly costs nothing a real
        /// noun would ever pay — no citation form in the twenty noun patterns ends this way.
        /// </para>
        /// </remarks>
        /// <param name="token">The case-folded token under consideration.</param>
        /// <param name="verbCandidateCount">How many verb candidates the same token already produced.</param>
        public static bool ShouldTryAsNoun(string token, int verbCandidateCount)
        {
            if (JitCompoundLParticipleEndings.Any(ending => token.EndsWith(ending, StringComparison.Ordinal)))
            {
                return false;
            }

            return verbCandidateCount == 0
                || !(token.EndsWith("í", StringComparison.Ordinal)
                    || VerbMatcher.LParticipleEndings.Any(ending => token.EndsWith(ending, StringComparison.Ordinal)));
        }

        private static readonly string[] JitCompoundLParticipleEndings = ["šel", "šla", "šlo", "šli", "šly"];

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
        /// Every survivor is first required to be shaped like its own claimed pattern's nominative
        /// singular — a consonant-final lemma under a consonant-final pattern like hrad, or the exact
        /// vowel a vowel-final pattern's own name ends in — via <see cref="HasSelfConsistentEnding"/>,
        /// the same rule <see cref="NounMatcher"/> uses to reattach one when reconstructing. "vrstva",
        /// "vrstvu", "vrstvy" and "vrstvo" all reduce to the same root and all score identically once
        /// any one of them turns up more than once — but žena's own nominative singular ends in "a", so
        /// only "vrstva" is shaped the way its winning pattern says a nominative singular should be; the
        /// other three are real inflected forms of the same word wearing the citation slot they were
        /// never in. This is not only a tie-break: a lone candidate with no sibling to lose to is
        /// rejected the same way if it is not shaped correctly for its own pattern, which is what "jeví"
        /// ("jevit se", not a noun) needs once nothing else in its root group survives to out-score it —
        /// see the next paragraph. Among what remains, ties the length rule alone could not break —
        /// every one of the four vrstva spellings is six letters — collapse to one this way, because the
        /// four case endings a žena-pattern word cycles through (a/u/y/o) are all exactly as long as the
        /// ending they replace; shorter-wins only decides between candidates already shaped correctly
        /// for their own pattern, such as "kandidát" (9) over "kandidáti" (10) once both, standing alone
        /// in front of a consonant-final pattern, pass the shape check equally.
        /// </para>
        /// <para>
        /// A root that is itself already known drops the whole group, not just its weaker members —
        /// useful when the borrowed paradigm belongs to a known word whose own citation form happens to
        /// share the false candidate's shape too closely for <see cref="HasSelfConsistentEnding"/> alone
        /// to reject it. "jeví" itself no longer needs this: reduced to "jev", an ordinary hrad-pattern
        /// noun, "jev" is short enough (three letters) to sit below the default <c>--min-delka</c> on
        /// some articles and never become a competing candidate at all, so nothing was left in the group
        /// to out-score "jeví" until the shape check above started rejecting a shapeless lone survivor
        /// on its own. <paramref name="isKnown"/> still matters for the reverse situation: a candidate
        /// that IS shaped correctly for its pattern but is still someone else's known paradigm restated.
        /// </para>
        /// <para>
        /// The stripped-vowel root is not the only way two candidates turn out to be the same word:
        /// "forem" (genitive plural of "forma", with a vkladné e — <c>form</c> plus an inserted vowel)
        /// ends in a consonant, so <see cref="NounRoot"/> — which only strips a trailing vowel — leaves
        /// it whole, while "forma" strips down to "form"; two different keys for the same word, because
        /// unlike <see cref="Services.CzechWordStructureResolver.ExtractNounRoot"/> this has no lexicon
        /// or heuristic to decide which consonant-final lemmas hide a mobile e, and guessing wrong here
        /// costs a real duplicate escaping instead of a hypothesis quietly failing to corroborate, so
        /// nothing here tries to guess it (see <see cref="NounMatcher"/>'s remarks on why "jev" needed
        /// both readings tried rather than one heuristic trusted). What ties them together instead
        /// needs no phonology: "forem" is one of "forma"'s own matched forms — the corroborating tvary
        /// list already generated for "forma" contains the literal spelling "forem" — so root groups
        /// that share a spelling this way are merged into one before scoring, on the same reasoning as
        /// grouping by root: a candidate whose own lemma appears as an inflected form of another
        /// candidate is that other candidate's paradigm restated, not a second word.
        /// </para>
        /// <para>
        /// A possessive adjective (otcův, matčin — <c>Data/Rules/Adjectives/patterns.json</c>) is
        /// excluded from <see cref="AdjectiveMatcher"/> on purpose (see its own remarks): it is not a
        /// separate lexicon headword, it is derived from the noun it belongs to, so nothing there ever
        /// corroborates one as an adjective in its own right — leaving <see cref="NounMatcher"/> to try
        /// every one of its inflected forms blindly, the same gap l-participle agreement filled before
        /// <see cref="ShouldTryAsNoun"/> learned to recognize it. "papežovo"/"papežova" (papežův,
        /// "the pope's") scored as three separate fake město/turista/žena-pattern nouns on a real
        /// article, entirely explained by "papež" — itself a real candidate two rows above them on the
        /// same run — plus the possessive suffix "-ov" and an adjective agreement ending. Unlike
        /// l-participle, shape alone cannot say which: "-ova" is also the real genitive plural of
        /// "slovo" ("slova"), so <see cref="IsPossessiveAdjectiveDerivative"/> only drops a candidate
        /// once stripping a possessive suffix actually lands on a word already known or already found
        /// elsewhere in the same run — "sl" is neither, so "slova" survives untouched, but "papežov" |
        /// "a" does because "papež" is sitting right there in the same candidate list. This only
        /// catches the ordinary case where the underlying noun's stem does not itself alternate —
        /// "otcův" is the pattern's own name because "otec" loses its mobile e first (otec → otcův, not
        /// otecův); a possessive built on a mobile-e noun would need that same lexicon/heuristic
        /// knowledge <see cref="NounMatcher"/>'s own remarks already flag as unreliable to guess, so it
        /// is left uncaught rather than guessed at.
        /// </para>
        /// </remarks>
        /// <param name="candidates">The candidates to filter, any order.</param>
        /// <param name="isKnown">Whether a given lemma is already known, independent of this candidate list.</param>
        public static IReadOnlyList<MatchCandidate> DropVowelEndingNounDuplicates(
            IEnumerable<MatchCandidate> candidates, Func<string, bool> isKnown)
        {
            var list = candidates.ToList();
            var allLemmas = new HashSet<string>(list.Select(c => c.Lemma));

            bool IsKnownOrFound(string lemma) => isKnown(lemma) || allLemmas.Contains(lemma);

            var nouns = list.Where(c => c.Category == WordCategory.Noun).ToList();
            var groups = MergeByRootAndSharedForms(nouns);

            var result = new List<MatchCandidate>();

            foreach (var group in groups)
            {
                // Checked across the whole merged group, not just the member being scored: "papežov" —
                // the bare root NounMatcher's own reconstruction invents from "papežova" — never itself
                // matches a possessive suffix (nothing follows the "-ov"), so filtering candidates
                // before grouping only removed "papežovo"/"papežova" and left "papežov" to win the
                // group on its own. Every member shares the same underlying word once merged, so one
                // possessive-shaped sibling is enough to condemn the whole group, the same way one
                // already-known root drops it below.
                if (group.Any(c => isKnown(NounRoot(c)) || IsPossessiveAdjectiveDerivative(c.Lemma, IsKnownOrFound)))
                {
                    continue;
                }

                // A candidate whose own trailing letter does not match what its own claimed pattern's
                // nominative singular actually looks like was never shaped like that pattern's citation
                // form to begin with — it only scored because its oblique cases happen to overlap with
                // a different pattern's (see HasSelfConsistentEnding's remarks). Filtered before scoring,
                // not just used to break a tie among the top scorers, so a lone inconsistent survivor
                // with no sibling to lose to — "jeví" ("jevit se", not a noun) tried directly under
                // město, whose nominative singular ends in "o", not "í" — is rejected on its own shape
                // rather than needing a competing "jev" candidate or a known root to catch it. "jev" is
                // three letters, below the default --min-delka, so on some articles no such competitor
                // is ever generated for it to lose to; this closes that gap without depending on one.
                var consistent = group.Where(HasSelfConsistentEnding).ToList();

                if (consistent.Count == 0)
                {
                    continue;
                }

                var best = consistent.Max(c => c.Score);
                var atBest = consistent.Where(c => c.Score == best).ToList();

                // A candidate whose own citation-form spelling was actually written somewhere in the
                // text outranks one that is a pure reconstruction nobody wrote — "varianta" (found
                // standalone) over "varianto" (never seen on its own, only reconstructed from the same
                // oblique forms "varianta" itself corroborates on). Self-consistency alone cannot choose
                // between the two: žena's own "a" and město's own "o" are each exactly what their
                // pattern expects, so both pass, and a same-length tie like this one is exactly what
                // shorter-wins cannot break either — a coincidence shorter-wins already needed a second
                // rule for once ("vrstva" and its siblings), just across two different patterns instead
                // of four spellings of one.
                var attested = atBest.Where(c => c.MatchedForms.Contains(c.Lemma)).ToList();
                var survivors = attested.Count > 0 ? attested : atBest;

                var shortest = survivors.Min(c => c.Lemma.Length);

                result.AddRange(survivors.Where(c => c.Lemma.Length == shortest));
            }

            result.AddRange(list.Where(c => c.Category != WordCategory.Noun));

            return result;
        }

        // Groups noun candidates first by their stripped-vowel root (the common case, no mobile e
        // involved), then merges any two of those groups where one group's lemma turns up as a literal
        // matched form of a candidate in the other — the signal that catches a mobile-e pair like
        // "forem"/"forma" without needing to know the phonology rule that would otherwise connect them.
        private static IEnumerable<IReadOnlyList<MatchCandidate>> MergeByRootAndSharedForms(
            IReadOnlyList<MatchCandidate> nouns)
        {
            var parent = new Dictionary<string, string>();

            string Find(string key)
            {
                while (parent[key] != key)
                {
                    parent[key] = parent[parent[key]];
                    key = parent[key];
                }

                return key;
            }

            void Union(string a, string b)
            {
                var rootA = Find(a);
                var rootB = Find(b);

                if (rootA != rootB)
                {
                    parent[rootA] = rootB;
                }
            }

            foreach (var candidate in nouns)
            {
                var key = NounRoot(candidate);
                parent.TryAdd(key, key);
            }

            foreach (var a in nouns)
            {
                foreach (var b in nouns)
                {
                    if (a.MatchedForms.Contains(b.Lemma))
                    {
                        Union(NounRoot(a), NounRoot(b));
                    }
                }
            }

            return nouns.GroupBy(c => Find(NounRoot(c))).Select(group => (IReadOnlyList<MatchCandidate>)group.ToList());
        }

        // Every literal suffix "otcův"/"matčin" declare in Data/Rules/Adjectives/patterns.json, dash
        // stripped: the possessive marker "ov"/"in" plus whatever adjective-agreement ending follows it
        // (including the bare "ův"/"in" citation form itself). Order does not matter — every candidate
        // lemma matches at most one of these by construction, since they differ in their own trailing
        // letters.
        private static readonly string[] PossessiveAdjectiveSuffixes =
        [
            "ových", "ovými", "ovým", "ova", "ovo", "ovu", "ově", "ovi", "ovy", "ův",
            "iných", "inými", "iným", "ina", "ino", "inu", "ině", "ini", "iny", "in",
        ];

        // A possessive adjective form is indistinguishable from a real noun's own case form by shape
        // alone — "papežova" and "slova" (genitive plural of "slovo") end the same way — so this only
        // fires once stripping the suffix actually lands on a word this run already knows about.
        private static bool IsPossessiveAdjectiveDerivative(string lemma, Func<string, bool> isKnownOrFound)
        {
            foreach (var suffix in PossessiveAdjectiveSuffixes)
            {
                if (lemma.Length > suffix.Length && lemma.EndsWith(suffix, StringComparison.Ordinal)
                    && isKnownOrFound(lemma[..^suffix.Length]))
                {
                    return true;
                }
            }

            return false;
        }

        // Mirrors CzechWordStructureResolver.ExtractNounRoot: a lemma ending in a vowel loses it,
        // everything else is the root as-is. Not calling into the service itself because this only
        // needs the same one-line rule, not the mobile-e/epenthesis machinery that comes with it.
        private static string NounRoot(MatchCandidate candidate) =>
            candidate.Lemma.Length > 1 && !MorphologyHelper.IsConsonant(candidate.Lemma[^1])
                ? candidate.Lemma[..^1]
                : candidate.Lemma;

        // Whether a candidate's own trailing vowel (or absence of one) is the shape its own winning
        // pattern's nominative singular actually has — the same check NounMatcher.NominativeSingularSuffix
        // makes from a pattern's name when it reconstructs one, applied here to judge a candidate that
        // was never reconstructed at all but happens to tie with one that was.
        private static bool HasSelfConsistentEnding(MatchCandidate candidate)
        {
            var expected = PatternNominativeSuffix(candidate.Pattern);

            return expected.Length == 0
                ? candidate.Lemma.Length > 0 && MorphologyHelper.IsConsonant(candidate.Lemma[^1])
                : candidate.Lemma.EndsWith(expected, StringComparison.Ordinal);
        }

        // A pattern name is itself a real word in its own nominative singular — see NounMatcher's copy
        // of this same rule for why that is enough to read the ending straight off the name.
        private static string PatternNominativeSuffix(string patternName) =>
            patternName.Length > 1 && !MorphologyHelper.IsConsonant(patternName[^1])
                ? patternName[^1].ToString()
                : "";
    }
}
