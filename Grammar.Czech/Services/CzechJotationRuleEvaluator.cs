using Grammar.Core.Enums.PhonologicalFeatures;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Evaluates Czech jotation rule evaluator rules.
    /// </summary>
    public class CzechJotationRuleEvaluator : IJotationRuleEvaluator<CzechWordRequest>
    {
        private readonly IPhonemeRegistry registry;
        private readonly IWordStructureResolver<CzechWordRequest> wordResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechJotationRuleEvaluator"/> type.
        /// </summary>
        public CzechJotationRuleEvaluator(IPhonemeRegistry registry, IWordStructureResolver<CzechWordRequest> structureResolver)
        {
            this.registry = registry;
            this.wordResolver = structureResolver;
        }

        /// <summary>
        /// Determines whether should apply jotation.
        /// </summary>
        /// <param name="request">The Czech word request to process.</param>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="ending">The ending used to choose the morphology rule.</param>
        /// <param name="hasMobileVowelRemoval">True when the rule removes a mobile vowel; otherwise, false.</param>
        /// <returns>True when the condition is met; otherwise, false.</returns>
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
            // The vzor is analyzed as the same word class as the word being inflected. Stated rather
            // than left to the default, which used to be Noun and was right here only by accident.
            var patternRequest = new CzechWordRequest
            {
                Lemma = request.Pattern,
                WordCategory = request.WordCategory,
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
