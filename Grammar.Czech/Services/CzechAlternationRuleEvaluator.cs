using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar.Czech.Services
{
    public class CzechAlternationRuleEvaluator : IAlternationRuleEvaluator
    {
        private readonly IPhonemeRegistry _registry;

        public CzechAlternationRuleEvaluator(IPhonemeRegistry registry)
        {
            this._registry = registry;
        }

        public bool ShouldShortenVowel(string stem)
        {
            // TODO: If there is 
            var last = stem[^1..];
            var penultimate = stem[^2..]; //second to last

            throw new NotImplementedException();
        }
    }
}
