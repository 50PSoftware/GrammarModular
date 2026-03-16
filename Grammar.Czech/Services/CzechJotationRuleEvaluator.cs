using Grammar.Core.Enums.PhonologicalFeatures;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar.Czech.Services
{
    public class CzechJotationRuleEvaluator : IJotationRuleEvaluator<CzechWordRequest>
    {
        private readonly IPhonemeRegistry _registry;

        public CzechJotationRuleEvaluator(IPhonemeRegistry registry)
        {
            this._registry = registry;
        }

        public bool ShouldApplyJotation(string stem, string ending, bool hasMobileVowelRemoval)
        {
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
            var phoneme = _registry.Get(lastConsonant);
            

            var isLabial = phoneme?.Place == ArticulationPlace.Bilabial || (phoneme?.Place == ArticulationPlace.Labiodental && phoneme.Symbol == "v");
            return isLabial && normalizedEnding.StartsWith("e");
        }
    }
}
