using Grammar.Core.Models.Phonology;
using Grammar.Czech.Enums.Phonology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar.Czech.Models
{
    sealed record CzechPhoneme : Phoneme
    {
        public Dictionary<PalatalizationContext, string>? PalatalizationTargets { get; init; }
    }
}
