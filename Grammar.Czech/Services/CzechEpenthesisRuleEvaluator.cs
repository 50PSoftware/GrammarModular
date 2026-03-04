using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar.Czech.Services
{
    public class CzechEpenthesisRuleEvaluator : IEpenthesisRuleEvaluator<CzechWordRequest>
    {
        private readonly IPhonemeRegistry _registry;
        public CzechEpenthesisRuleEvaluator(IPhonemeRegistry registry)
        {
            this._registry = registry;
        }
        public bool ShouldApplyEpenthesis(string stem, string suffix, CzechWordRequest request)
        {
            if (string.IsNullOrEmpty(stem) || string.IsNullOrEmpty(suffix))
            {
                return false;
            }

            if (!_registry.IsConsonant(suffix[0]))
            {
                return false;
            }

            if (!_registry.IsConsonant(stem[^1]))
            {
                return false;
            }

            return EvaluateEpenthesisRules(stem, suffix, request);
        }

        private bool EvaluateEpenthesisRules(string stem, string suffix, CzechWordRequest request)
        {
            if (request.WordCategory == Core.Enums.WordCategory.Noun &&
                request.Case == Core.Enums.Case.Genitive &&
                request.Number == Core.Enums.Number.Plural &&
                (suffix == "k" || suffix == "g"))
            {
                if (!stem.EndsWith(suffix))
                {
                    return true;
                }
            }

            if (request.WordCategory == Core.Enums.WordCategory.Noun &&
                request.Case == Core.Enums.Case.Genitive &&
                request.Number == Core.Enums.Number.Plural &&
                request.Gender == Core.Enums.Gender.Neuter &&
                suffix == "n")
            {
                return true;
            }

            return false;
        }
    }
}
