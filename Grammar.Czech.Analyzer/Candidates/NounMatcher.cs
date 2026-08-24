using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Services;

namespace Grammar.Czech.Analyzer.Candidates
{
    /// <summary>
    /// Tries a token as the nominative singular of every known noun pattern, and keeps the ones whose
    /// other case/number forms also turn up in the same text.
    /// </summary>
    /// <remarks>
    /// This is generate-and-test, not reverse morphology: it never derives a stem by stripping an
    /// ending from the token. It hands the token to <see cref="CzechNounDeclensionService"/> — the
    /// same forward generator the rest of the library trusts — as if it already were the lemma, for
    /// every pattern in turn, and only believes the ones whose generated paradigm the text backs up.
    /// A pattern that does not fit the token's actual shape does not need to be rejected by hand; it
    /// just generates forms nobody wrote, and those score zero on their own.
    /// <para>
    /// The consequence is a known blind spot, not a bug: a word is only found this way if its
    /// nominative singular — the citation form — appears in the text somewhere. <c>psy</c> without
    /// <c>pes</c> anywhere nearby will not surface <c>pes</c>, because nothing here strips the
    /// accusative ending and reinserts the mobile e that <c>pes</c> needs. That reconstruction is
    /// exactly the reverse-phonology work the project chose not to build; the corroboration this
    /// class adds is scoped to the words simple enough to appear in their own base form.
    /// </para>
    /// </remarks>
    public sealed class NounMatcher
    {
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
        /// Tries the token against every noun pattern and returns the ones with at least one
        /// corroborating form besides the token itself.
        /// </summary>
        /// <param name="token">The case-folded token to test as a candidate lemma.</param>
        /// <param name="corpus">Case-folded tokens found in the text, for corroboration.</param>
        public IReadOnlyList<MatchCandidate> Match(string token, IReadOnlyDictionary<string, int> corpus)
        {
            var results = new List<MatchCandidate>();

            foreach (var (patternName, pattern) in _patterns)
            {
                var (gender, isAnimate) = ParseGender(pattern.Gender);

                if (gender is null)
                {
                    continue;
                }

                var matchedForms = GenerateAndCollect(token, patternName, gender.Value, isAnimate, corpus);

                // A single hit is just the token matching its own hypothesis (nominative singular) —
                // no corroboration from elsewhere in the text. Two or more means at least one other
                // case/number form of the same hypothesis was also found.
                if (matchedForms.Count >= 2)
                {
                    results.Add(new MatchCandidate(token, WordCategory.Noun, patternName, gender, isAnimate, matchedForms));
                }
            }

            return results;
        }

        private List<string> GenerateAndCollect(
            string token,
            string patternName,
            Gender gender,
            bool? isAnimate,
            IReadOnlyDictionary<string, int> corpus)
        {
            var matchedForms = new List<string>();

            foreach (var number in (Number[]) [Number.Singular, Number.Plural])
            {
                foreach (var @case in (Case[])
                    [Case.Nominative, Case.Genitive, Case.Dative, Case.Accusative,
                     Case.Vocative, Case.Locative, Case.Instrumental])
                {
                    string form;

                    try
                    {
                        form = _declensionService.GetForm(new CzechWordRequest
                        {
                            Lemma = token,
                            Pattern = patternName,
                            Gender = gender,
                            IsAnimate = isAnimate,
                            Case = @case,
                            Number = number,
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

                    if (corpus.ContainsKey(folded) && !matchedForms.Contains(folded))
                    {
                        matchedForms.Add(folded);
                    }
                }
            }

            return matchedForms;
        }

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
