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
        bool ShouldApplyJotation(string stem, string ending, bool hasMobileWovelRemoval);
    }
}
