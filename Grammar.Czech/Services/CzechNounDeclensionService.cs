using Grammar.Core.Enums;
using Grammar.Core.Exceptions;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Word;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Builds Czech noun forms from declension patterns, irregular overrides, stem alternations, softening, epenthesis, and jotation rules.
    /// </summary>
    public class CzechNounDeclensionService : IInflectionService<CzechWordRequest>
    {
        private readonly INounDataProvider _dataProvider;
        private readonly IWordStructureResolver<CzechWordRequest> _wordStructureResolver;
        private readonly ICzechPhonologyService _phonologyService;
        private readonly ISofteningRuleEvaluator<CzechWordRequest> _softeningRuleEvaluator;
        private readonly IEpenthesisRuleEvaluator<CzechWordRequest> _epenthesisRuleEvaluator;
        private readonly IJotationRuleEvaluator<CzechWordRequest> _jotationRuleEvaluator;
        private readonly ISyncretismRuleEvaluator<CzechWordRequest> _syncretismRuleEvaluator;
        private readonly ICzechOrthographyService _orthographyService;
        private readonly IValencyProvider<CzechLexicalEntry> _valencyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechNounDeclensionService"/> type.
        /// </summary>
        public CzechNounDeclensionService(INounDataProvider dataProvider, IWordStructureResolver<CzechWordRequest> wordStructureResolver, ICzechPhonologyService phonologyService, ISofteningRuleEvaluator<CzechWordRequest> softeningRuleEvaluator, IEpenthesisRuleEvaluator<CzechWordRequest> epenthesisRuleEvaluator, IJotationRuleEvaluator<CzechWordRequest> jotationRuleEvaluator, ISyncretismRuleEvaluator<CzechWordRequest> syncretismRuleEvaluator, ICzechOrthographyService orthographyService, IValencyProvider<CzechLexicalEntry> valencyProvider)
        {
            this._dataProvider = dataProvider;
            this._wordStructureResolver = wordStructureResolver;
            this._phonologyService = phonologyService;
            this._softeningRuleEvaluator = softeningRuleEvaluator;
            this._epenthesisRuleEvaluator = epenthesisRuleEvaluator;
            this._jotationRuleEvaluator = jotationRuleEvaluator;
            this._syncretismRuleEvaluator = syncretismRuleEvaluator;
            this._orthographyService = orthographyService;
            this._valencyProvider = valencyProvider;
        }

        /// <summary>
        /// Builds the requested inflected form.
        /// </summary>
        /// <param name="word">The Czech word request containing the lemma and requested grammatical categories.</param>
        /// <returns>The generated noun form.</returns>
        public WordForm GetForm(CzechWordRequest word)
        {
            if (_dataProvider.GetPropers().TryGetValue(word.Lemma, out var propers) && word.IsIndeclinable.HasValue && word.IsIndeclinable.Value)
            {
                return new WordForm(word.Lemma);
            }

            if (word.IsPluralOnly.HasValue && word.IsPluralOnly.Value && word.Number == Number.Singular)
            {
                throw new InvalidOperationException($"{word.Lemma} se nevyskytuje v jednotném čísle.");
            }

            // Synkretismus přímých pádů (nominativ = lemma) — před vyhledáním koncovky.
            if (_syncretismRuleEvaluator.ShouldUseLemmaForm(word))
            {
                return new WordForm(word.Lemma);
            }

            // Vyhodnoceno proti vzoru, který zadal volající — InheritsFrom níže ho může přepsat.
            var isBaseWordPattern = word.Lemma == word.Pattern;

            var numberKey = word.Number == Number.Singular ? "singular" : "plural";
            var caseKey = word.Case.ToString();

            NounPattern? irregular = null;

            // Nepravidelná slova se řeší před výběrem vzoru: InheritsFrom musí ovlivnit i koncovky,
            // ne jen pravidla, která se ptají na word.Pattern později.
            if (_dataProvider.GetIrregulars().TryGetValue(word.Lemma.ToLower(), out irregular))
            {
                if (irregular.Overrides != null &&
                    irregular.Overrides.TryGetValue(numberKey, out var cases) &&
                    cases.TryGetValue(caseKey, out var irregularForm))
                {
                    return new WordForm(irregularForm);
                }

                if (!string.IsNullOrEmpty(irregular.InheritsFrom))
                {
                    word.Pattern = irregular.InheritsFrom;
                }
            }

            if (!_dataProvider.GetPatterns().TryGetValue(word.Pattern.ToLower(), out var pattern))
            {
                throw new NotSupportedException($"Noun pattern '{word.Pattern}' not found.");
            }

            if (!pattern.Endings.TryGetValue(numberKey, out var caseDict) ||
                !caseDict.TryGetValue(caseKey, out var ending))
                throw new InvalidOperationException($"Ending not found for {numberKey} {caseKey}.");

            // Synkretismus přímých pádů: Ak. sg. s nulovou koncovkou = Nom. sg. (= lemma).
            if (_syncretismRuleEvaluator.ShouldUseLemmaForm(word, ending))
            {
                return new WordForm(word.Lemma);
            }

            // případná výjimka před výpočtem tvaru
            if (pattern.Overrides != null &&
                pattern.Overrides.TryGetValue(numberKey, out var caseOverrides) &&
                caseOverrides.TryGetValue(caseKey, out var overrideForm) &&
                isBaseWordPattern)
            {
                return new WordForm(overrideForm);
            }

            var wordStructure = _wordStructureResolver.AnalyzeStructure(word);
            var stem = wordStructure.Root;
            if (!string.IsNullOrEmpty(wordStructure.DerivationSuffix))
            {
                stem = _phonologyService.ApplyEpenthesis(_epenthesisRuleEvaluator.ShouldApplyEpenthesis(stem, wordStructure.DerivationSuffix, word), stem, wordStructure.DerivationSuffix);
            }

            if (!string.IsNullOrEmpty(irregular?.Stem))
            {
                stem = irregular.Stem;
            }

            if (!string.IsNullOrEmpty(pattern.Stem) && isBaseWordPattern)
            {
                stem = pattern.Stem!;
            }

            // Softening rules see the resolved stem, not just the lemma: the ending attaches to the stem
            // and the two diverge whenever an alternation fired above (nůž → noz-, dům → dom-, otec → otc-).
            var resolvedStem = stem;

            if (_softeningRuleEvaluator.ShouldApplySoftening(word, resolvedStem, out var palatalizationContext))
            {
                stem = _phonologyService.ApplyOrthographicSoftening(stem, palatalizationContext);
            }

            var hasMobileERemoval = word.HasMobileE ?? MorphologyHelper.HasLikelyMobileE(word.Lemma);

            var finalEnding = _softeningRuleEvaluator.GetEndingTransformation(word, resolvedStem, out var endingTransformationApplied) ?? ending;

            if (_jotationRuleEvaluator.ShouldApplyJotation(word, stem, finalEnding, hasMobileERemoval))
            {
                finalEnding = _orthographyService.ApplyJotationOrthography(finalEnding);
            }
            else if (!endingTransformationApplied)
            {
                finalEnding = _orthographyService.NormalizeEndingOrthography(stem, finalEnding, word.Pattern);
            }

            return new WordForm(MorphologyHelper.ApplyFormEnding(stem, finalEnding));
        }

        /// <summary>
        /// Resolves noun gender, declension pattern, animacy, and mobile vowel metadata from the lexicon.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The gender, pattern, default number, animacy, and mobile-vowel metadata for the lemma.</returns>
        public (Gender gender, string pattern, Number number, bool isAnimate, bool? hasMobileVowel) ResolveGenderAndPattern(string lemma)
        {
            var entry = _valencyProvider.GetEntry(lemma)
                ?? throw new LemmaNotFoundException(lemma);

            var gender = entry.Gender
                ?? throw new LemmaNotFoundException(lemma,
                    $"Lemma '{lemma}' found in lexicon but Gender is null.");

            var pattern = entry.Pattern
                ?? throw new LemmaNotFoundException(lemma,
                    $"Lemma '{lemma}' found in lexicon but Pattern is null.");

            return (gender, pattern, Number.Singular, entry.IsAnimate ?? false, entry.HasMobileE);
        }
    }
}
