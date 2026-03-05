using Grammar.Core.Interfaces;
using Grammar.Czech.Enums.Phonology;
using Grammar.Czech.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar.Czech.Interfaces
{
    public interface ICzechPhonologyService : IPhonologyService<CzechWordRequest>
    {
        string ApplySoftening(string stem, PalatalizationContext context);
    }
}
