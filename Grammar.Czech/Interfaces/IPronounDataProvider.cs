using Grammar.Czech.Models;
using Grammar.Czech.Models.Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    public interface IPronounDataProvider
    {
        public Dictionary<string, PronounData> GetPronouns();

        public Dictionary<string, PronounParadigm> GetParadigms();
    }
}
