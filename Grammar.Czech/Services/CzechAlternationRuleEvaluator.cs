using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Evaluates Czech alternation rule evaluator rules.
    /// </summary>
    public class CzechAlternationRuleEvaluator : IAlternationRuleEvaluator<CzechWordRequest>
    {
        private readonly IPhonemeRegistry _registry;
        private readonly IValencyProvider<CzechLexicalEntry> _valencyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechAlternationRuleEvaluator"/> type.
        /// </summary>
        public CzechAlternationRuleEvaluator(IPhonemeRegistry registry, IValencyProvider<CzechLexicalEntry> valencyProvider)
        {
            _registry = registry;
            _valencyProvider = valencyProvider;
        }

        #region Public API

        /// <summary>
        /// Determines whether the stem shortens its long vowel before the genitive plural ending.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="request">The Czech word request to process.</param>
        /// <returns>True when the condition is met; otherwise, false.</returns>
        /// <remarks>
        /// Whether a noun shortens at all is lexical, not phonological — kráva gives krav but káva
        /// gives káv — so the decision comes from the entry. The phoneme registry only vetoes vowels
        /// that never shorten in this position, so that a wrong flag cannot invent sfér → *sfer.
        /// </remarks>
        public bool ShouldShortenStem(string stem, CzechWordRequest request)
        {
            if (string.IsNullOrEmpty(stem) || !IsNounGenitivePlural(request))
            {
                return false;
            }

            var shortens = request.HasGenitivePluralShortening
                ?? GetEntryShortening(request)
                ?? false;

            return shortens && ShortensReliably(stem);
        }

        #endregion Public API

        #region Private Rules

        private static bool IsNounGenitivePlural(CzechWordRequest request) =>
            request.WordCategory == WordCategory.Noun
            && request.Case == Case.Genitive
            && request.Number == Number.Plural;

        /// <summary>
        /// Kategorie se předává explicitně ze stejného důvodu jako u kmene: bez ní by stát jako
        /// sloveso vrátil slovesný řádek. Lookup je v provideru cachovaný.
        /// </summary>
        private bool? GetEntryShortening(CzechWordRequest request) =>
            string.IsNullOrEmpty(request.Lemma)
                ? null
                : _valencyProvider.GetEntry(request.Lemma, WordCategory.Noun)?.HasGenitivePluralShortening;

        /// <summary>
        /// Determines whether the vowel that <see cref="ICzechPhonologyService.ShortenVowel"/> would
        /// reach is one that actually shortens in the genitive plural.
        /// </summary>
        /// <remarks>
        /// Only á and í shorten dependably. é, ý and ú/ů keep their length here (sféra → sfér,
        /// rýha → rýh, kúra → kúr), yet the registry gives all of them a short counterpart because
        /// other alternations need it. The scan therefore has to walk the stem the same way
        /// ShortenVowel does, or the veto would judge a different vowel than the one that changes.
        /// </remarks>
        private bool ShortensReliably(string stem)
        {
            for (int i = stem.Length - 1; i >= 0; i--)
            {
                var phoneme = _registry.Get(stem[i]);
                if (phoneme?.ShortCounterpart is not null)
                {
                    return stem[i] is 'á' or 'í';
                }
            }

            return false;
        }

        #endregion Private Rules
    }
}
