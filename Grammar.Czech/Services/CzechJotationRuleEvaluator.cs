using Grammar.Core.Enums.PhonologicalFeatures;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    public class CzechJotationRuleEvaluator : IJotationRuleEvaluator<CzechWordRequest>
    {
        private readonly IPhonemeRegistry registry;
        private readonly IWordStructureResolver<CzechWordRequest> wordResolver;

        public CzechJotationRuleEvaluator(IPhonemeRegistry registry, IWordStructureResolver<CzechWordRequest> structureResolver)
        {
            this.registry = registry;
            this.wordResolver = structureResolver;
        }

        public bool ShouldApplyJotation(CzechWordRequest request, string stem, string ending, bool hasMobileVowelRemoval)
        {
            if (request.Case == Core.Enums.Case.Vocative)
            {
                return false;
            }

            var normalizedEnding = ending.TrimStart('-');
            if (hasMobileVowelRemoval)
            {
                return false;
            }

            if (string.IsNullOrEmpty(stem) || string.IsNullOrEmpty(ending))
            {
                return false;
            }

            var lastConsonant = stem[^1..];
            var phoneme = registry.Get(lastConsonant);

            var isLabial = phoneme?.Place == ArticulationPlace.Bilabial || (phoneme?.Place == ArticulationPlace.Labiodental && phoneme.Symbol == "v");
            var isNasal = phoneme?.Place == ArticulationPlace.Alveolar && phoneme?.Manner == ArticulationManner.Nasal;
            var isDTN = phoneme?.Place == ArticulationPlace.Alveolar && (phoneme?.Manner == ArticulationManner.Nasal || phoneme?.Manner == ArticulationManner.Plosive);
            var patternRequest = new CzechWordRequest
            {
                Lemma = request.Pattern,
                Case = Core.Enums.Case.Nominative,
                Number = Core.Enums.Number.Singular,
                Pattern = request.Pattern,
                Gender = request.Gender,
                IsAnimate = request.IsAnimate
            };

            var patternLastConsonant = wordResolver.AnalyzeStructure(patternRequest).Root[^1..];
            var patternPhoneme = registry.Get(patternLastConsonant);
            var isSoftPattern = patternPhoneme?.Place == ArticulationPlace.Palatal;
            return isLabial && normalizedEnding == "e" || isSoftPattern && isDTN && !isLabial && normalizedEnding.StartsWith("e");
        }
    }
}
