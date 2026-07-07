using Grammar.Core.Interfaces;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for evaluating Czech case-syncretism rules.
    /// </summary>
    public interface ISyncretismRuleEvaluator<TWord> where TWord : IWordRequest
    {
        /// <summary>
        /// Rozhoduje, zda je tvar totožný s lemmatem bez ohledu na koncovku.
        /// Nominativ je vždy roven lemmatu — v sg. i v pl. u pomnožných jmen.
        /// Používá se dřív, než se vyhledá koncovka, protože vzory nominativ sg.
        /// záměrně nedefinují (spoléhají na tento synkretismus).
        /// </summary>
        /// <param name="request">The word request to analyze or inflect.</param>
        /// <returns>True when the surface form equals the lemma; otherwise, false.</returns>
        bool ShouldUseLemmaForm(TWord request);

        /// <summary>
        /// Rozhoduje, zda je tvar totožný s lemmatem na základě vyhodnocené koncovky.
        /// Synkretismus přímých pádů: akuzativ sg. s nulovou koncovkou se rovná
        /// nominativu sg. (vzory hrad, stroj, kost, píseň). Vrácením lemmatu se
        /// zachová pohyblivé „e" (píseň, žízeň) i kvantitativní alternace (hůl)
        /// bez rekonstrukce kmene. Vyžaduje celý word request — jde o morfologické
        /// rozhodnutí závislé na pádu a čísle, nestačí jen znát koncovku.
        /// </summary>
        /// <param name="request">The word request to analyze or inflect.</param>
        /// <param name="ending">The ending evaluated for the requested case and number.</param>
        /// <returns>True when the surface form equals the lemma; otherwise, false.</returns>
        bool ShouldUseLemmaForm(TWord request, string ending);
    }
}
