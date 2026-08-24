using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Services;

namespace Grammar.Czech.Analyzer.Candidates
{
    /// <summary>
    /// Tries a token as the citation form (masculine nominative singular) of an adjective, and keeps
    /// it when other case/gender/number forms of the same hypothesis also turn up in the text.
    /// </summary>
    /// <remarks>
    /// Reuses <see cref="CzechAdjectiveDeclensionService.GuessAdjectivePattern"/> rather than trying
    /// every adjective pattern the way <see cref="NounMatcher"/> tries every noun pattern — that
    /// heuristic already exists for exactly this (an unknown lemma, guessed from its ending), so there
    /// is no reason to duplicate it. Possessive patterns (otcův, matčin) are out of scope on purpose:
    /// the guess never returns them, and a possessive is not really a separate lexicon headword —
    /// it is derived from the noun it belongs to.
    /// <para>
    /// The hard pattern (mladý) is the one adjective pattern whose citation form actually varies by
    /// gender — mladý/mladá/mladé — where the soft pattern (jarní) already reads the same in every
    /// gender's nominative singular. Left alone, a token read straight off the text would treat
    /// "celá" and "celé" as two more lemmas beside "celý", each independently corroborated by the
    /// same underlying word. <see cref="ToCitationForm"/> folds the gender ending back to -ý before
    /// anything is generated, so all three tokens produce the one hypothesis and its combined
    /// evidence, instead of three competing entries a person has to recognize as the same word by eye.
    /// </para>
    /// <para>
    /// <see cref="CzechAdjectiveDeclensionService.GuessAdjectivePattern"/> falls back to "mladý" for
    /// any ending it does not recognize — a reasonable default for its own callers, who already know
    /// they have an adjective and just need a pattern for it, but fatal for this matcher, which does
    /// not know that yet and would otherwise hand every non-adjective token in the text a "mladý"
    /// hypothesis for free. "novin" (genitive plural of noviny) scored as high as a real find on a
    /// live article this way — nothing about the guess rejects a token that is not shaped like an
    /// adjective at all. <see cref="LooksLikeAdjectiveCitationForm"/> checks the one thing the guess
    /// does not: whether the token actually ends the way a citation form has to, before the guess and
    /// the fallback it hides behind ever run.
    /// </para>
    /// </remarks>
    public sealed class AdjectiveMatcher
    {
        private static readonly (Gender Gender, bool? IsAnimate)[] GenderSlots =
        [
            (Grammar.Core.Enums.Gender.Masculine, true),
            (Grammar.Core.Enums.Gender.Masculine, false),
            (Grammar.Core.Enums.Gender.Feminine, null),
            (Grammar.Core.Enums.Gender.Neuter, null),
        ];

        private readonly CzechAdjectiveDeclensionService _declensionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdjectiveMatcher"/> type.
        /// </summary>
        /// <param name="declensionService">The declension service to guess a pattern and generate forms with.</param>
        public AdjectiveMatcher(CzechAdjectiveDeclensionService declensionService) => _declensionService = declensionService;

        /// <summary>
        /// Tries the token as an adjective lemma and returns a candidate when at least one other
        /// generated form was also found in the text.
        /// </summary>
        /// <param name="token">The case-folded token to test as a candidate lemma.</param>
        /// <param name="corpus">Case-folded tokens found in the text, for corroboration.</param>
        /// <param name="properNouns">
        /// Case-folded words the text's own capitalization marks as proper nouns, excluded from
        /// corroboration — see <see cref="NounMatcher.Match"/>'s remarks: an adjective's own case forms
        /// can coincide with a proper noun's the same way a noun reconstruction can borrow one.
        /// </param>
        public MatchCandidate? Match(
            string token, IReadOnlyDictionary<string, int> corpus, IReadOnlySet<string> properNouns)
        {
            if (!LooksLikeAdjectiveCitationForm(token))
            {
                return null;
            }

            var pattern = _declensionService.GuessAdjectivePattern(token);
            var lemma = pattern == "mladý" ? ToCitationForm(token) : token;
            var matchedForms = new List<string>();

            foreach (var number in (Number[]) [Number.Singular, Number.Plural])
            {
                foreach (var @case in (Case[])
                    [Case.Nominative, Case.Genitive, Case.Dative, Case.Accusative,
                     Case.Vocative, Case.Locative, Case.Instrumental])
                {
                    foreach (var (gender, isAnimate) in GenderSlots)
                    {
                        string form;

                        try
                        {
                            form = _declensionService.GetForm(new CzechWordRequest
                            {
                                Lemma = lemma,
                                Pattern = pattern,
                                Gender = gender,
                                IsAnimate = isAnimate,
                                Case = @case,
                                Number = number,
                                Degree = Degree.Positive,
                                WordCategory = WordCategory.Adjective,
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

            return matchedForms.Count >= 2
                ? new MatchCandidate(lemma, WordCategory.Adjective, pattern, null, null, matchedForms)
                : null;
        }

        // mladá/mladé -> mladý. jarní's nominative singular is "-í" in every gender already, so this
        // is only ever called for the hard pattern's guess, where the gender ending genuinely differs.
        private static string ToCitationForm(string token) => token.Length >= 2 && token[^1] is 'á' or 'é'
            ? token[..^1] + "ý"
            : token;

        // The four endings a real adjective's nominative singular can end in, across all genders and
        // both patterns (mladý/mladá/mladé, jarní). Anything else is not a citation form to begin with,
        // and letting GuessAdjectivePattern's "mladý" fallback run on it anyway is how "novin" scored.
        //
        // -ání/-ení/-ění is excluded even though it ends in í: that is specifically how a verbal noun
        // is formed from an infinitive (dýchat -> dýchání, čtení, dělění), not how a soft adjective is
        // — jarní/letní/národní attach -ní straight to a root, with no theme vowel in front of it. A
        // real jarní-pattern adjective ending in exactly that three-letter sequence does not turn up in
        // Czech; a neuter deverbal noun scoring as an adjective on a real article did.
        private static bool LooksLikeAdjectiveCitationForm(string token) =>
            token.Length >= 2
            && token[^1] is 'ý' or 'á' or 'é' or 'í'
            && !token.EndsWith("ání", StringComparison.Ordinal)
            && !token.EndsWith("ení", StringComparison.Ordinal)
            && !token.EndsWith("ění", StringComparison.Ordinal);
    }
}
