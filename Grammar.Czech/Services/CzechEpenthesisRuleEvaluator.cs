using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    public class CzechEpenthesisRuleEvaluator : IEpenthesisRuleEvaluator<CzechWordRequest>
    {
        private readonly IPhonemeRegistry _registry;

        public CzechEpenthesisRuleEvaluator(IPhonemeRegistry registry)
        {
            this._registry = registry;
        }

        public bool ShouldApplyEpenthesis(string stem, string derivationSuffix, CzechWordRequest request)
        {
            if (string.IsNullOrEmpty(stem) || string.IsNullOrEmpty(derivationSuffix))
            {
                return false;
            }

            if (!_registry.IsConsonant(derivationSuffix[0]))
            {
                return false;
            }

            if (!_registry.IsConsonant(stem[^1]))
            {
                return false;
            }

            return EvaluateEpenthesisRules(stem, derivationSuffix, request);
        }

        private bool EvaluateEpenthesisRules(string stem, string derivationSuffix, CzechWordRequest request)
        {
            if (request.WordCategory == Core.Enums.WordCategory.Noun &&
                request.Case == Core.Enums.Case.Genitive &&
                request.Number == Core.Enums.Number.Plural &&
                (derivationSuffix == "k" || derivationSuffix == "g"))
            {
                if (!stem.EndsWith(derivationSuffix))
                {
                    return true;
                }
            }

            if (request.WordCategory == Core.Enums.WordCategory.Noun &&
                request.Case == Core.Enums.Case.Genitive &&
                request.Number == Core.Enums.Number.Plural &&
                request.Gender == Core.Enums.Gender.Neuter &&
                derivationSuffix == "n")
            {
                return true;
            }

            return false;
        }
    }
}