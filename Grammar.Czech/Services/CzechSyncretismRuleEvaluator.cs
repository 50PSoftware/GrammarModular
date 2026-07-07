using Grammar.Core.Enums;
using Grammar.Czech.Helpers;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Evaluates Czech case-syncretism rules that make an inflected form identical to the lemma.
    /// </summary>
    public class CzechSyncretismRuleEvaluator : ISyncretismRuleEvaluator<CzechWordRequest>
    {
        /// <summary>
        /// Determines whether the requested form equals the lemma regardless of the ending.
        /// </summary>
        /// <param name="request">The word request to analyze or inflect.</param>
        /// <returns>True when the surface form equals the lemma; otherwise, false.</returns>
        public bool ShouldUseLemmaForm(CzechWordRequest request)
        {
            // Nominativ je vždy totožný s lemmatem — v sg. i v pl. u pomnožných jmen.
            // Vzory nominativ sg. nedefinují, proto se rozhoduje ještě před vyhledáním koncovky.
            return request.Case == Case.Nominative
                && (request.Number == Number.Singular
                    || (request.IsPluralOnly.HasValue && request.IsPluralOnly.Value && request.Number == Number.Plural));
        }

        /// <summary>
        /// Determines whether the requested form is syncretic with the nominative singular (the lemma).
        /// </summary>
        /// <param name="request">The word request to analyze or inflect.</param>
        /// <param name="ending">The ending evaluated for the requested case and number.</param>
        /// <returns>True when the surface form equals the lemma; otherwise, false.</returns>
        public bool ShouldUseLemmaForm(CzechWordRequest request, string ending)
        {
            // Akuzativ sg. s nulovou koncovkou = nominativ sg. (synkretismus přímých pádů).
            // Vzory hrad, stroj, kost, píseň: neživotná maskulina a souhláskové feminina
            // mají Ak. sg. = Nom. sg., takže lemma je správný tvar včetně pohyblivého „e"
            // a kvantitativních alternací.
            return request.Case == Case.Accusative
                && request.Number == Number.Singular
                && MorphologyHelper.IsEnding(ending)
                && string.IsNullOrEmpty(ending.Replace("-", ""));
        }
    }
}
