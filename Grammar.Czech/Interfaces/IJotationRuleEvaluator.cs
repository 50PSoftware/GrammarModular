using Grammar.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar.Czech.Interfaces
{
    public interface IJotationRuleEvaluator<TWord> where TWord : IWordRequest
    {
        /// <summary>
        /// Rozhoduje, zda aplikovat jotaci (e→ě) na ending.
        /// Vyžaduje celý word request — jotace je morfologické rozhodnutí
        /// závislé na pádu a vzoru, nestačí jen znát hlásky.
        /// </summary>
        bool ShouldApplyJotation(TWord request, string stem, string ending, bool hasMobileWovelRemoval);
    }
}
