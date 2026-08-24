using Grammar.Core.Enums;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Services;

namespace Grammar.Czech.Analyzer.Candidates
{
    /// <summary>
    /// Tries a token as the nominative singular of every known noun pattern, and additionally tries to
    /// reconstruct a nominative singular from a token shaped like some other case or number of a
    /// pattern, then keeps the ones whose other case/number forms also turn up in the same text.
    /// </summary>
    /// <remarks>
    /// The direct path is generate-and-test, not reverse morphology: it never derives a stem by
    /// stripping an ending from the token to build the hypothesis it tries. It hands the token to
    /// <see cref="CzechNounDeclensionService"/> — the same forward generator the rest of the library
    /// trusts — as if it already were the lemma, for every pattern in turn, and only believes the ones
    /// whose generated paradigm the text backs up. A pattern that does not fit the token's actual shape
    /// does not need to be rejected by hand; it just generates forms nobody wrote, and those score zero
    /// on their own.
    /// <para>
    /// The reconstruction path — <see cref="ReconstructNominatives"/> — is the same trick
    /// <see cref="VerbMatcher"/> uses to recover an infinitive from a present-tense token, applied to
    /// nouns: strip a pattern's own declared ending from the token, and put the pattern's own
    /// nominative-singular shape back where it was. This does not need to be safe on its own, because
    /// nothing here trusts the reconstruction — it is only ever a hypothesis fed through the same
    /// forward generator and the same corroboration threshold as everything else, so a wrong guess
    /// (mobile e, palatalization, epenthesis all make some reconstructions land on the wrong stem)
    /// costs nothing beyond a hypothesis that then fails to corroborate. <c>kandidáti</c> without
    /// <c>kandidát</c> anywhere else in the text now recovers <c>kandidát</c> this way; <c>fyzici</c>
    /// (palatalized k→c) would reconstruct the wrong stem <c>fyzic</c>, but that hypothesis simply
    /// scores worse than the real <c>fyzik</c> wherever the singular also appears, and drops out on its
    /// own if it never corroborates at all — the blind spot standing description used to name here
    /// (<c>psy</c> without <c>pes</c>) is narrower now, not gone: it survives only for the reconstructions
    /// this class does not attempt, and for cases where the reconstructed stem happens to corroborate
    /// as convincingly as the real one would.
    /// </para>
    /// <para>
    /// A related guess cost a real, correctly-reconstructed lemma its evidence: left to
    /// <see cref="CzechWordRequest.HasMobileE"/>'s own default, oblique-case generation falls back to
    /// <c>MorphologyHelper.HasLikelyMobileE(lemma)</c>, which reads "jev" as mobile-e-shaped — the same
    /// single-syllable, vowel-between-consonants pattern as "pes" — and generates "jvu" for what
    /// should be "jevu". "jev" itself still matched (the one slot mobile e never touches), but nothing
    /// else did, so it never reached two. <see cref="GenerateAndCollect"/> now tries both readings
    /// explicitly and keeps whatever either corroborates, for the same reason the reconstruction itself
    /// does not need to be safe: a lemma with no mobile e loses nothing, since both readings coincide
    /// for it, and one that does gets a real chance rather than a coin flip from the ending alone.
    /// </para>
    /// </remarks>
    public sealed class NounMatcher
    {
        private static readonly (string Number, string Case)[] EndingSlots =
        [
            ("singular", "Genitive"), ("singular", "Dative"), ("singular", "Accusative"),
            ("singular", "Vocative"), ("singular", "Locative"), ("singular", "Instrumental"),
            ("plural", "Nominative"), ("plural", "Genitive"), ("plural", "Dative"),
            ("plural", "Accusative"), ("plural", "Vocative"), ("plural", "Locative"), ("plural", "Instrumental"),
        ];

        private readonly CzechNounDeclensionService _declensionService;
        private readonly IReadOnlyDictionary<string, NounPattern> _patterns;

        /// <summary>
        /// Initializes a new instance of the <see cref="NounMatcher"/> type.
        /// </summary>
        /// <param name="declensionService">The declension service to generate forms with.</param>
        /// <param name="dataProvider">The provider holding every known noun pattern.</param>
        public NounMatcher(CzechNounDeclensionService declensionService, INounDataProvider dataProvider)
        {
            _declensionService = declensionService;
            _patterns = dataProvider.GetPatterns();
        }

        /// <summary>
        /// Tries the token — and nominatives reconstructed from it — against every noun pattern, and
        /// returns the ones with at least one corroborating form besides the hypothesis lemma itself.
        /// </summary>
        /// <param name="token">The case-folded token to test as a candidate lemma.</param>
        /// <param name="corpus">Case-folded tokens found in the text, for corroboration.</param>
        /// <param name="properNouns">
        /// Case-folded words the text's own capitalization marks as proper nouns, excluded from
        /// corroboration — see this method's remarks on why a real word's own reconstruction can still
        /// need this even though <c>Program.cs</c> never lets a proper noun seed one.
        /// </param>
        /// <remarks>
        /// A proper noun is kept out of <paramref name="token"/> itself — <c>Program.cs</c> checks it
        /// before ever calling this — but its own inflected spellings stay in <paramref name="corpus"/>
        /// regardless, because <see cref="Tokenizer.CountTokens"/> collects every token it sees without
        /// asking whether it looked like a name. "polskou" (instrumental of the ordinary adjective
        /// "polský", not a name) reconstructs to "polska" the same way any other token does — and
        /// "polska" happens to be the genitive singular of the real proper noun "Polsko", which turns up
        /// in the corpus dozens of times as "polska"/"polsku"/"polsko". None of that is the adjective's
        /// own paradigm; it borrowed a country's declension table by coincidence of spelling. Excluding
        /// <paramref name="properNouns"/> from what counts as a match, not just from what can seed one,
        /// is what a hypothesis this borrowed needs to fail to corroborate on its own.
        /// </remarks>
        public IReadOnlyList<MatchCandidate> Match(
            string token, IReadOnlyDictionary<string, int> corpus, IReadOnlySet<string> properNouns)
        {
            if (!LooksLikeNounCitationForm(token))
            {
                return [];
            }

            var hypotheses = new HashSet<string> { token };

            foreach (var reconstructed in ReconstructNominatives(token))
            {
                hypotheses.Add(reconstructed);
            }

            var results = new List<MatchCandidate>();

            foreach (var lemma in hypotheses)
            {
                foreach (var (patternName, pattern) in _patterns)
                {
                    var (gender, isAnimate) = ParseGender(pattern.Gender);

                    if (gender is null)
                    {
                        continue;
                    }

                    var matchedForms = GenerateAndCollect(lemma, patternName, gender.Value, isAnimate, corpus, properNouns);

                    // A single hit is just the hypothesis matching itself — no corroboration from
                    // elsewhere in the text. Two or more means at least one other case/number form of
                    // the same hypothesis was also found.
                    if (matchedForms.Count >= 2)
                    {
                        results.Add(new MatchCandidate(lemma, WordCategory.Noun, patternName, gender, isAnimate, matchedForms));
                    }
                }
            }

            return results;
        }

        private List<string> GenerateAndCollect(
            string lemma,
            string patternName,
            Gender gender,
            bool? isAnimate,
            IReadOnlyDictionary<string, int> corpus,
            IReadOnlySet<string> properNouns)
        {
            var matchedForms = new List<string>();

            foreach (var number in (Number[]) [Number.Singular, Number.Plural])
            {
                foreach (var @case in (Case[])
                    [Case.Nominative, Case.Genitive, Case.Dative, Case.Accusative,
                     Case.Vocative, Case.Locative, Case.Instrumental])
                {
                    // Left null, HasMobileE falls back to MorphologyHelper.HasLikelyMobileE(lemma) — a
                    // guess from the lemma's shape, and "jev" is shaped exactly like "pes" (single
                    // syllable, vowel between two consonants) without actually having a mobile e: its
                    // genitive is jevu, not the guess's jvu. There is no way to know which a bare
                    // hypothesis is without the lexicon fact that decides it for a real entry, so both
                    // readings are tried and whichever the text actually corroborates wins — the same
                    // "don't decide, generate and test" move as everywhere else here, not a guess of
                    // its own. A lemma that genuinely has no mobile e loses nothing: the true and false
                    // readings coincide for it, so trying both just repeats the one real form twice.
                    foreach (var hasMobileE in (bool?[]) [true, false])
                    {
                        string form;

                        try
                        {
                            form = _declensionService.GetForm(new CzechWordRequest
                            {
                                Lemma = lemma,
                                Pattern = patternName,
                                Gender = gender,
                                IsAnimate = isAnimate,
                                Case = @case,
                                Number = number,
                                HasMobileE = hasMobileE,
                                WordCategory = WordCategory.Noun,
                            }).Form;
                        }
                        catch (InvalidOperationException)
                        {
                            continue;
                        }
                        catch (NotSupportedException)
                        {
                            continue;
                        }

                        var folded = form.ToLowerInvariant();

                        if (corpus.ContainsKey(folded) && !properNouns.Contains(folded) && !matchedForms.Contains(folded))
                        {
                            matchedForms.Add(folded);
                        }
                    }
                }
            }

            return matchedForms;
        }

        // For every pattern and every case/number slot other than the one that is never stored
        // (singular nominative is always the lemma passthrough — see CandidateRanking's remarks on why),
        // strip the declared ending off the token if it fits, and reattach the pattern's own
        // nominative-singular shape — which is not stored anywhere either, but is exactly what the
        // pattern's own name already demonstrates: "předseda" IS a nominative singular of that pattern,
        // so its ending relative to its own consonant/vowel-stripped root is the answer for every word
        // that declines like it.
        private IEnumerable<string> ReconstructNominatives(string token)
        {
            var seen = new HashSet<string>();

            foreach (var (patternName, pattern) in _patterns)
            {
                var nominativeSuffix = NominativeSingularSuffix(patternName);

                foreach (var (numberKey, caseKey) in EndingSlots)
                {
                    if (!pattern.Endings.TryGetValue(numberKey, out var cases)
                        || !cases.TryGetValue(caseKey, out var ending)
                        || ending.Length < 2)
                    {
                        continue;
                    }

                    var suffix = ending[1..]; // "-u" -> "u"; a bare "-" has nothing left to strip

                    if (token.Length <= suffix.Length || !token.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var hypothesis = token[..^suffix.Length] + nominativeSuffix;

                    if (seen.Add(hypothesis))
                    {
                        yield return hypothesis;
                    }
                }
            }
        }

        // None of the twenty pattern names — žena, předseda, turista, růže, píseň, kost, hrad, les,
        // stroj, pán, občan, syn, muž, učitel, král, soudce, město, moře, kuře, stavení — ends in ě or
        // é: every regular noun's own nominative singular ends in a consonant or one of a/e/o/í.
        // "nekonečně" (an adverb) and "nekonečné" (an inflected adjective form) still scored as nouns
        // on a real article, because unlike AdjectiveMatcher and VerbMatcher this class had no shape
        // check of its own — it tries every token under every pattern by design, and a consonant-final
        // pattern like hrad accepts almost any ending, so there was nothing to reject an ě/é-ending
        // token with. This is the one shape no real noun pattern has, so it is the one check that
        // costs nothing to add without the twenty-pattern ambiguity that made a general shape filter
        // too risky to build outright.
        private static bool LooksLikeNounCitationForm(string token) =>
            token.Length < 2 || (token[^1] is not 'ě' and not 'é');

        // A pattern name is itself a real word in its own nominative singular, so its own trailing
        // vowel (or absence of one) already states what to reattach — "žena" -> "a", "hrad" -> nothing.
        // Mirrors CandidateRanking.NounRoot's one-line rule rather than calling ExtractNounRoot itself,
        // for the same reason that one does: this needs the rule, not the mobile-e/epenthesis machinery
        // that comes with the real thing, and the pattern names never carry either.
        private static string NominativeSingularSuffix(string patternName) =>
            patternName.Length > 1 && !MorphologyHelper.IsConsonant(patternName[^1])
                ? patternName[^1].ToString()
                : "";

        // "masculineAnimate"/"masculineInanimate"/"feminine"/"neuter", as Data/Rules/Nouns/patterns.json
        // spells them. Animacy comes bundled with gender here because that is how the patterns
        // themselves are split — pán and hrad are different pattern rows, not one row with a flag.
        private static (Gender? gender, bool? isAnimate) ParseGender(string patternGender) => patternGender switch
        {
            "masculineAnimate" => (Grammar.Core.Enums.Gender.Masculine, true),
            "masculineInanimate" => (Grammar.Core.Enums.Gender.Masculine, false),
            "feminine" => (Grammar.Core.Enums.Gender.Feminine, null),
            "neuter" => (Grammar.Core.Enums.Gender.Neuter, null),
            _ => (null, null),
        };
    }
}
