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
        public MatchCandidate? Match(string token, IReadOnlyDictionary<string, int> corpus)
        {
            var pattern = _declensionService.GuessAdjectivePattern(token);
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
                                Lemma = token,
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

                        if (corpus.ContainsKey(folded) && !matchedForms.Contains(folded))
                        {
                            matchedForms.Add(folded);
                        }
                    }
                }
            }

            return matchedForms.Count >= 2
                ? new MatchCandidate(token, WordCategory.Adjective, pattern, null, null, matchedForms)
                : null;
        }
    }
}
