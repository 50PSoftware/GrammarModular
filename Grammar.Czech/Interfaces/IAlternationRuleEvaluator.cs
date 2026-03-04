using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar.Czech.Interfaces
{
    public interface IAlternationRuleEvaluator
    {
        bool ShouldShortenWovel(string stem);
    }
}
