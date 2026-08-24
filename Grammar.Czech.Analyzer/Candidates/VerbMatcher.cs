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

        private static readonly string[] Trida5PresentEndings = ["ají", "áme", "áte", "ám", "áš", "á"];
        private static readonly string[] Trida4PresentEndings = ["íme", "íte", "ím", "íš", "í"];
        private static readonly string[] Trida3PresentEndings = ["jeme", "jete", "jí", "ji", "ješ", "je"];
        private static readonly string[] Trida2PresentEndings = ["neme", "nete", "nu", "neš", "ne", "nou"];

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
        public IReadOnlyList<MatchCandidate> Match(string token, IReadOnlyDictionary<string, int> corpus)
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
                    var matchedForms = GenerateAndCollect(lemma, pattern, corpus);

                    if (matchedForms.Count >= 2)
                    {
                        results.Add(new MatchCandidate(lemma, WordCategory.Verb, pattern, null, null, matchedForms));
                    }
                }
            }

            return results;
        }

        private List<string> GenerateAndCollect(string lemma, string pattern, IReadOnlyDictionary<string, int> corpus)
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

                if (corpus.ContainsKey(folded) && !matchedForms.Contains(folded))
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
        }
    }
}
