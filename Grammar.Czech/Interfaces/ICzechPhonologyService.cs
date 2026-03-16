using Grammar.Core.Interfaces;
using Grammar.Czech.Enums.Phonology;
using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    public interface ICzechPhonologyService : IPhonologyService<CzechWordRequest>
    {
        string ApplySoftening(string stem, PalatalizationContext context);
        string ApplySoftConsonantBeforeE(string stem);
        string ApplyJotation(string ending);
        string ApplyDTNRule(string stem, string ending);
    }
}
