using Grammar.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar.Czech.Interfaces
{
    public interface IEpenthesisRuleEvaluator<TWord> where TWord : IWordRequest
    {
        bool ShouldApplyEpenthesis(string stem, string derivationSuffix, TWord wordRequest);
    }
}
