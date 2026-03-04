using Grammar.Czech.Models;
using Grammar.Core.Interfaces;
using Grammar.Core.Enums;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Core.Models.Word;

namespace Grammar.Czech.Services
{
    public class CzechNounDeclensionService : IInflectionService<CzechWordRequest>
    {
        private readonly INounDataProvider dataProvider;
        private readonly IWordStructureResolver<CzechWordRequest> wordStructureResolver;
        private readonly IPhonologyService<CzechWordRequest> phonologyService;
        private readonly ISofteningRuleEvaluator<CzechWordRequest> softeningRuleEvaluator;

        public CzechNounDeclensionService(INounDataProvider dataProvider, IWordStructureResolver<CzechWordRequest> wordStructureResolver, IPhonologyService<CzechWordRequest> phonologyService, ISofteningRuleEvaluator<CzechWordRequest> softeningRuleEvaluator)
        {
            this.dataProvider = dataProvider;
            this.wordStructureResolver = wordStructureResolver;
            this.phonologyService = phonologyService;
            this.softeningRuleEvaluator = softeningRuleEvaluator;
        }

        public WordForm GetForm(CzechWordRequest word)
        {
            if (dataProvider.GetPropers().TryGetValue(word.Lemma, out var propers) && propers.IsIndeclinable)
            {
                return new WordForm(word.Lemma);
            }

            if (!dataProvider.GetPatterns().TryGetValue(word.Pattern.ToLower(), out var pattern))
            {
                throw new NotSupportedException($"Noun pattern '{word.Pattern}' not found.");
            }

            if (pattern.IsPluralOnly && word.Number == Number.Singular)
            {
                throw new InvalidOperationException($"{word.Lemma} se nevyskytuje v jednotném čísle.");
            }

            var isBaseWordPattern = word.Lemma == word.Pattern;

            var numberKey = word.Number == Number.Singular ? "singular" : "plural";
            var caseKey = ((int)word.Case).ToString();

            if (dataProvider.GetIrregulars().TryGetValue(word.Lemma.ToLower(), out var irregular))
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

            if (!pattern.Endings.TryGetValue(numberKey, out var caseDict) ||
                !caseDict.TryGetValue(caseKey, out var ending))
                throw new InvalidOperationException($"Ending not found for {numberKey} {caseKey}.");

            // případná výjimka před výpočtem tvaru
            if (pattern.Overrides != null &&
                pattern.Overrides.TryGetValue(numberKey, out var caseOverrides) &&
                caseOverrides.TryGetValue(caseKey, out var overrideForm) &&
                isBaseWordPattern)
            {
                return new WordForm(overrideForm);
            }

            var wordStructure = wordStructureResolver.AnalyzeStructure(word);
            var stem = wordStructure.Root;
            if (!string.IsNullOrEmpty(wordStructure.DerivationSuffix))
            {
                //TODO: Add IEpentheisiRuleValuator.
                //stem = phonologyService.ApplyEpenthesis(stem, wordStructure.DerivationSuffix, word);
            }

            if (!string.IsNullOrEmpty(pattern.Stem) && isBaseWordPattern)
            {
                stem = pattern.Stem!;
            }

            if (softeningRuleEvaluator.ShouldApplySoftening(word))
            {
                stem = phonologyService.ApplySoftening(stem);
            }

            return new WordForm(MorphologyHelper.ApplyFormEnding(stem, softeningRuleEvaluator.GetEndingTransformation(word) ?? ending));
        }

        public (Gender, string, Number, bool) GuessGenderAndPattern(string lemma)
        {
            throw new NotImplementedException();
            var lower = lemma.ToLowerInvariant();

            if (lower.EndsWith("a"))
                return (Gender.Feminine, "žena", Number.Singular, false);

            if (lower.EndsWith("o"))
                return (Gender.Neuter, "město", Number.Singular, false);

            if (lower.EndsWith("í"))
                return (Gender.Neuter, "stavení", Number.Singular, false);

            if (lower.EndsWith("e") || lower.EndsWith("ě"))
                return (Gender.Neuter, "moře", Number.Singular, false);

            if (lower.EndsWith("us") || lower.EndsWith("ec") || lower.EndsWith("tel"))
                return (Gender.Masculine, "muž", Number.Singular, true);

            if (lower.EndsWith("y") || lower.EndsWith("i") || lower.EndsWith("é"))
                return (Gender.Feminine, "žena", Number.Plural, false); // fallback plurál

            // fallback pro souhláskové zakončení
            if ("bcčdďfghjklmnprstvwxyzž".Contains(lower[^1]))
                return (Gender.Masculine, "hrad", Number.Singular, false);

            // default fallback
            return (Gender.Masculine, "muž", Number.Singular, true);
        }
    }
}
