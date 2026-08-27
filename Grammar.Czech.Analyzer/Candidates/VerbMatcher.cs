using Grammar.Core.Enums;
using Grammar.Czech.Services;

namespace Grammar.Czech.Analyzer.Candidates
{
    /// <summary>
    /// Tries a token as a verb infinitive across every regular class, and additionally tries to
    /// reconstruct an infinitive from a token shaped like a present-tense form of classes 2 through 5.
    /// </summary>
    /// <remarks>
    /// Mirrors <see cref="NounMatcher"/> — try every regular class (there are only five, same order of
    /// magnitude as the noun patterns) and let the score decide — rather than
    /// <see cref="AdjectiveMatcher"/>'s single guess:
    /// <see cref="CzechVerbConjugationService.GuessVerbClass"/> returns <see langword="null"/> for
    /// class 1 (nést, brát, péct cannot be told from their ending) and is unreliable even where it
    /// does answer (brát ends in -át like a class-5 verb, but is class 1), so a single guess would
    /// either miss verbs outright or seed the search with a wrong class as often as a right one.
    /// <para>
    /// The reconstruction is the reason "the infinitive must appear in the text" is not actually true
    /// for classes 2–5. <see cref="Services.CzechWordStructureResolver"/> derives each of those
    /// classes' present stem from the infinitive by stripping a fixed suffix — no vowel or consonant
    /// alternation happens in that particular derivation, unlike a noun's mobile e — so undoing it for
    /// a token shaped like a present-tense form is exactly as safe as the forward direction already is.
    /// A wrong guess (there is more than one candidate suffix for classes 4 and 5, since the ending
    /// alone cannot say which) costs nothing beyond generating a hypothesis that then fails to
    /// corroborate. Class 1 has no such rule — <c>DeriveTrida1</c> equivalent does not exist because
    /// there is not one to write — so it is only ever tried the way <see cref="NounMatcher"/> tries a
    /// noun: as the literal token itself.
    /// </para>
    /// </remarks>
    public sealed class VerbMatcher
    {
        private static readonly string[] RegularClasses = ["trida1", "trida2", "trida3", "trida4", "trida5"];

        /// <summary>
        /// The five regular verb-class patterns this matcher tries. Exposed so the GUI's pattern
        /// correction combo box offers exactly the patterns a verb candidate could actually be, not a
        /// free-text field that could write a pattern nothing in the lexicon recognizes.
        /// </summary>
        public static IReadOnlyList<string> Patterns => RegularClasses;

        private static readonly string[] Trida5PresentEndings = ["ají", "áme", "áte", "ám", "áš", "á"];
        private static readonly string[] Trida4PresentEndings = ["íme", "íte", "ím", "íš", "í"];
        private static readonly string[] Trida3PresentEndings = ["jeme", "jete", "jí", "ji", "ješ", "je"];
        private static readonly string[] Trida2PresentEndings = ["neme", "nete", "nu", "neš", "ne", "nou"];

        // Minulé příčestí (l/la/lo/li/ly, agreement for gender/number) is not a class-specific shape
        // the way a present-tense ending is — every regular verb forms it the same way regardless of
        // class — so this is tried once, not per class, mirroring CandidateRanking.LParticipleEndings.
        internal static readonly string[] LParticipleEndings = ["li", "ly", "la", "lo", "l"];

        private readonly CzechVerbConjugationService _conjugationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="VerbMatcher"/> type.
        /// </summary>
        /// <param name="conjugationService">The conjugation service to generate forms with.</param>
        public VerbMatcher(CzechVerbConjugationService conjugationService) => _conjugationService = conjugationService;

        /// <summary>
        /// Tries the token — and, for classes 2 through 5, infinitives reconstructed from it — against
        /// every regular verb class, and returns the ones with at least one corroborating form.
        /// </summary>
        /// <param name="token">The case-folded token to test.</param>
        /// <param name="corpus">Case-folded tokens found in the text, for corroboration.</param>
        /// <param name="properNouns">
        /// Case-folded words the text's own capitalization marks as proper nouns, excluded from
        /// corroboration — see <see cref="NounMatcher.Match"/>'s remarks: a reconstruction from an
        /// ordinary token can land on a spelling that happens to be a proper noun's own inflected form,
        /// which stays in <paramref name="corpus"/> regardless of what <c>Program.cs</c> lets seed a
        /// hypothesis, and the same borrowed-paradigm risk applies here as it does for nouns.
        /// </param>
        public IReadOnlyList<MatchCandidate> Match(
            string token, IReadOnlyDictionary<string, int> corpus, IReadOnlySet<string> properNouns)
        {
            var hypotheses = new HashSet<string> { token };

            foreach (var reconstructed in ReconstructInfinitives(token))
            {
                hypotheses.Add(reconstructed);
            }

            var results = new List<MatchCandidate>();

            foreach (var lemma in hypotheses)
            {
                foreach (var pattern in RegularClasses)
                {
                    if (!MatchesInfinitiveShape(lemma, pattern))
                    {
                        continue;
                    }

                    var matchedForms = GenerateAndCollect(lemma, pattern, corpus, properNouns);

                    if (matchedForms.Count >= 2 && HasCorroborationBeyondAmbiguousEnding(matchedForms, pattern))
                    {
                        results.Add(new MatchCandidate(lemma, WordCategory.Verb, pattern, null, null, matchedForms));
                    }
                }
            }

            return DropRedundantSuffixVariants(DropLParticipleNoutAlternativeWhenTPreferredExists(results));
        }

        // ReconstructInfinitives tries both "stem+t" and "stem+nout" from the identical l-participle
        // stem for every candidate — the "+nout" reading exists only for the genuinely irregular class
        // 2 verbs that drop "-nu-" in their own l-participle (vzniknout -> vznikl, stem "vznik", where
        // "vznikt" is not a valid shape for any other class and no sibling ever exists to compete with).
        // For an ordinary verb the l-participle stem already carries the thematic vowel its real class
        // needs, so "stem+t" alone lands on a validly-shaped trida3/4/5 infinitive on its own — every
        // one of those classes' own suffixes ends in "t" — and that sibling is corroborated by the
        // exact same l-participle forms, not a second, independent sighting. "rozvíjenout" tied
        // "rozvíjet" on a real article for this reason, and "umožninout"/"umožnit" the same way, not
        // because either is a real word for two different verbs.
        private static List<MatchCandidate> DropLParticipleNoutAlternativeWhenTPreferredExists(List<MatchCandidate> results)
        {
            var suppressed = new HashSet<int>();

            for (var i = 0; i < results.Count; i++)
            {
                var candidate = results[i];

                if (candidate.Pattern != "trida2" || !candidate.Lemma.EndsWith("nout", StringComparison.Ordinal))
                {
                    continue;
                }

                var tAlternative = candidate.Lemma[..^4] + "t";
                var sibling = results.Find(other => other.Pattern != "trida2" && other.Lemma == tAlternative);

                if (sibling is not null && sibling.Score >= candidate.Score)
                {
                    suppressed.Add(i);
                }
            }

            return results.Where((_, index) => !suppressed.Contains(index)).ToList();
        }

        // DeriveTridaN strips exactly two characters for every one of class 4's four candidate suffixes
        // (it/ít/et/ět) and class 5's two (at/át), so every reconstruction from the same present-tense
        // token shares the identical present stem — and therefore the identical score whenever the
        // evidence is present-tense forms alone, which is the ordinary case. Left alone,
        // "hudebnit"/"hudebnít"/"hudebnet"/"hudebnět" all tied at the same score on a real article, for
        // what a person reads as one guess with four spellings. A tie (or a lead) is resolved toward one
        // preferred spelling per class — "it" for class 4 as the pattern's own canonical example
        // (prosit), "at" for class 5 as the ordinary spelling against the closed handful of real -át
        // verbs — and a variant only survives on its own where it strictly outscores every preferred
        // sibling, which is what a genuine exception (hrát) looks like.
        private static readonly IReadOnlyDictionary<string, string[]> SuffixPreference = new Dictionary<string, string[]>
        {
            ["trida5"] = ["at", "át"],
            ["trida4"] = ["it", "et", "ět", "ít"],
        };

        private static List<MatchCandidate> DropRedundantSuffixVariants(List<MatchCandidate> results)
        {
            var suppressed = new HashSet<int>();

            for (var i = 0; i < results.Count; i++)
            {
                var candidate = results[i];

                if (!SuffixPreference.TryGetValue(candidate.Pattern, out var preference))
                {
                    continue;
                }

                var suffix = preference.FirstOrDefault(s => candidate.Lemma.EndsWith(s, StringComparison.Ordinal));

                if (suffix is null)
                {
                    continue;
                }

                var stem = candidate.Lemma[..^suffix.Length];

                foreach (var preferred in preference.TakeWhile(s => s != suffix))
                {
                    var siblingLemma = stem + preferred;
                    var sibling = results.Find(other => other.Pattern == candidate.Pattern && other.Lemma == siblingLemma);

                    if (sibling is not null && sibling.Score >= candidate.Score)
                    {
                        suppressed.Add(i);
                        break;
                    }
                }
            }

            return results.Where((_, index) => !suppressed.Contains(index)).ToList();
        }

        // Classes 2-5 only ever produce a real paradigm from a lemma shaped like their own infinitive —
        // CzechWordStructureResolver.DeriveTridaN requires the same suffix before it derives anything.
        // Anything else falls through to UnknownInfinitiveFallback, which sets every stem to the bare
        // lemma regardless of class — the same stem every other class's fallback would produce too, so
        // it is not a weak guess, it is a guaranteed source of cross-category collisions: a deverbal
        // noun sharing its source verb's stem (vznik/vzniknout, cesta/cestovat) matches its OWN unrelated
        // shape under every class equally, at a score that has nothing to do with which one is real.
        // Skipping a class whose suffix the lemma does not have removes that class from the search
        // instead of letting it vote on a fallback nothing in the class actually predicts. Class 1 has
        // no suffix to check by definition, so it is exempt — trying it as-is is already the best this
        // matcher can do for it.
        private static bool MatchesInfinitiveShape(string lemma, string pattern) => pattern switch
        {
            "trida5" => lemma.EndsWith("at", StringComparison.Ordinal) || lemma.EndsWith("át", StringComparison.Ordinal),
            "trida4" => lemma.EndsWith("it", StringComparison.Ordinal) || lemma.EndsWith("ít", StringComparison.Ordinal)
                || lemma.EndsWith("et", StringComparison.Ordinal) || lemma.EndsWith("ět", StringComparison.Ordinal),
            "trida3" => lemma.EndsWith("ovat", StringComparison.Ordinal),
            "trida2" => lemma.EndsWith("nout", StringComparison.Ordinal),
            _ => true,
        };

        // Class 4's bare "-í" (3rd person, singular and plural alike) and "-ím" (1st singular) are not
        // just weak evidence, they are the exact shape of a jarní-pattern adjective's own citation form
        // and instrumental singular, and of an í-final noun's own citation form and instrumental
        // singular — konkrétní/konkrétním, prostředí/prostředím. A token that is really one of those
        // reconstructs into a fake trida4 infinitive whose only "corroboration" is that same word's own
        // pair of forms seen twice, not two independent sightings. "změnit" survived this check on a
        // real article because "změnit" itself — the infinitive, not shaped like í or ím at all — also
        // turned up in the text; "prostředit"/"konkrétnit"/"dalšit" and four more did not, and every one
        // of them was invented. Requiring one match outside {í, ím} costs nothing for a genuine class-4
        // verb, since íme/íte/íš, the past participle and the infinitive are all still fair game.
        private static bool HasCorroborationBeyondAmbiguousEnding(IReadOnlyList<string> matchedForms, string pattern) =>
            pattern != "trida4"
            || matchedForms.Any(form => !form.EndsWith("í", StringComparison.Ordinal) && !form.EndsWith("ím", StringComparison.Ordinal));

        private List<string> GenerateAndCollect(
            string lemma, string pattern, IReadOnlyDictionary<string, int> corpus, IReadOnlySet<string> properNouns)
        {
            var matchedForms = new List<string>();

            foreach (var request in VerbForms.Requests(lemma, pattern))
            {
                string form;

                try
                {
                    form = _conjugationService.GetBasicForm(request).Form;
                }
                catch (InvalidOperationException)
                {
                    continue;
                }
                catch (NotSupportedException)
                {
                    continue;
                }
                catch (ArgumentException)
                {
                    continue;
                }

                var folded = form.ToLowerInvariant();

                if (corpus.ContainsKey(folded) && !properNouns.Contains(folded) && !matchedForms.Contains(folded))
                {
                    matchedForms.Add(folded);
                }
            }

            return matchedForms;
        }

        private static IEnumerable<string> ReconstructInfinitives(string token)
        {
            foreach (var ending in Trida5PresentEndings)
            {
                if (token.Length > ending.Length && token.EndsWith(ending, StringComparison.Ordinal))
                {
                    var stem = token[..^ending.Length];
                    yield return stem + "at";
                    yield return stem + "át";
                }
            }

            foreach (var ending in Trida4PresentEndings)
            {
                if (token.Length > ending.Length && token.EndsWith(ending, StringComparison.Ordinal))
                {
                    var stem = token[..^ending.Length];
                    yield return stem + "it";
                    yield return stem + "ít";
                    yield return stem + "et";
                    yield return stem + "ět";
                }
            }

            foreach (var ending in Trida3PresentEndings)
            {
                if (token.Length > ending.Length && token.EndsWith(ending, StringComparison.Ordinal))
                {
                    var stem = token[..^ending.Length];

                    if (stem.EndsWith("u", StringComparison.Ordinal) && stem.Length > 1)
                    {
                        yield return stem[..^1] + "ovat";
                    }
                }
            }

            foreach (var ending in Trida2PresentEndings)
            {
                if (token.Length > ending.Length && token.EndsWith(ending, StringComparison.Ordinal))
                {
                    yield return token[..^ending.Length] + "nout";
                }
            }

            // The infinitive minus its final "t" plus the l-suffix is the l-participle for the ordinary
            // case (existovat -> existoval), and class 2 often drops "nu" the same way it drops it
            // everywhere else in the paradigm (vzniknout -> vznikl, not vzniknul) — trying both suffixes
            // is exactly the same "more than one candidate ending, let the score decide" move already
            // used for classes 4 and 5, not a new kind of guess.
            foreach (var ending in LParticipleEndings)
            {
                if (token.Length > ending.Length && token.EndsWith(ending, StringComparison.Ordinal))
                {
                    var stem = token[..^ending.Length];
                    yield return stem + "t";
                    yield return stem + "nout";
                }
            }
        }
    }
}
