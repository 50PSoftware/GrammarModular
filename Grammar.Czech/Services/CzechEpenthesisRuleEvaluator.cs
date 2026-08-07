using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Evaluates Czech epenthesis rule evaluator rules.
    /// </summary>
    public class CzechEpenthesisRuleEvaluator : IEpenthesisRuleEvaluator<CzechWordRequest>
    {
        private readonly IPhonemeRegistry _registry;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechEpenthesisRuleEvaluator"/> type.
        /// </summary>
        public CzechEpenthesisRuleEvaluator(IPhonemeRegistry registry)
        {
            _registry = registry;
        }

        #region Public API

        /// <summary>
        /// Determines whether should apply epenthesis.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="derivationSuffix">The derivational suffix that may require epenthesis.</param>
        /// <param name="request">The Czech word request to process.</param>
        /// <returns>True when the condition is met; otherwise, false.</returns>
        public bool ShouldApplyEpenthesis(string stem, string derivationSuffix, CzechWordRequest request)
        {
            if (request.HasEpenthesisInGenitivePlural.HasValue)
                return IsNounGenitivePlural(request) && request.HasEpenthesisInGenitivePlural.Value;

            if (string.IsNullOrEmpty(stem) || string.IsNullOrEmpty(derivationSuffix))
                return false;

            if (!_registry.IsConsonant(derivationSuffix[0]))
                return false;

            if (!_registry.IsConsonant(stem[^1]))
                return false;

            return IsNounGenitivePlural(request) && IsEpenthesisCluster(stem[^1], derivationSuffix[0]);
        }

        #endregion Public API

        #region Private Rules

        private static bool IsNounGenitivePlural(CzechWordRequest request) =>
            request.WordCategory == WordCategory.Noun
            && request.Case == Case.Genitive
            && request.Number == Number.Plural;

        /// <summary>
        /// Determines whether two adjacent consonants form a cluster that requires epenthesis.
        /// </summary>
        /// <remarks>
        /// A cluster requires epenthesis when it is heterorganic (C1 and C2 have different
        /// places of articulation) and no obligatory place assimilation makes it homorganic.
        /// Homorganic clusters (e.g. st — both alveolar) are phonologically stable.
        /// Assimilating clusters (e.g. nk — n becomes [ŋ] before velar) are also stable.
        /// </remarks>
        private bool IsEpenthesisCluster(char c1, char c2)
        {
            // Digraf není shluk. Bez tohohle se ch rozpadne na c+h, které jsou heterorganní, a
            // moucha dostane epentezi, kterou nemá: gen. pl. je much, ne *mouceh.
            if (_registry.Get($"{c1}{c2}") is not null)
            {
                return false;
            }

            var p1 = _registry.Get(c1);
            var p2 = _registry.Get(c2);
            var p2Czech = p2 as CzechPhoneme;

            //if (p1?.Place is null || p2?.Place is null || p2Czech?.Place is null)
            //    return false;

            if (p2Czech?.TriggersEpenthesisAsC2 == false)
                return false;

            // Homorganní shluk — stejné místo artikulace, snadno vyslovitelný.
            // Příklad: st (Alveolar+Alveolar) → měst, míst — bez epentheze.
            if (p1.Place == p2.Place)
            {
                return p2.Manner is Core.Enums.PhonologicalFeatures.ArticulationManner.Trill
                    or Core.Enums.PhonologicalFeatures.ArticulationManner.LateralApproximant
                    or Core.Enums.PhonologicalFeatures.ArticulationManner.Nasal;
            }

            // Heterorganní shluk bez assimilace → epentheze.
            // Příklady: tk (studentka), kn (okno), kl (peklo), lk (jablko).
            return true;
        }

        #endregion Private Rules
    }
}
