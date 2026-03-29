using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Evaluates whether vowel epenthesis should be applied when assembling an inflected noun form.
    /// </summary>
    /// <remarks>
    /// Epenthesis (insertion of a fleeting -e-) occurs in genitive plural when the stem would
    /// otherwise end in a phonologically difficult consonant cluster. The difficulty is determined
    /// by whether the cluster is heterorganic (different places of articulation) and whether
    /// obligatory place assimilation applies — both facts sourced from <see cref="IPhonemeRegistry"/>.
    /// </remarks>
    public class CzechEpenthesisRuleEvaluator : IEpenthesisRuleEvaluator<CzechWordRequest>
    {
        private readonly IPhonemeRegistry _registry;

        /// <summary>
        /// Initializes a new instance of <see cref="CzechEpenthesisRuleEvaluator"/>.
        /// </summary>
        public CzechEpenthesisRuleEvaluator(IPhonemeRegistry registry)
        {
            _registry = registry;
        }

        #region Public API

        /// <inheritdoc/>
        public bool ShouldApplyEpenthesis(string stem, string derivationSuffix, CzechWordRequest request)
        {
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
            var p1 = _registry.Get(c1);
            var p2 = _registry.Get(c2);

            if (p1?.Place is null || p2?.Place is null)
                return false;

            // Homorganní shluk — stejné místo artikulace, snadno vyslovitelný.
            // Příklad: st (Alveolar+Alveolar) → měst, míst — bez epentheze.
            if (p1.Place == p2.Place)
                return false;

            // Obligatorní assimilace místa — C1 se foneticky přizpůsobí C2,
            // shluk se stane homorganním. Příklad: nk → [ŋk] — banka → bank.
            if (p1.AssimilatesPlaceBefore == p2.Place)
                return false;

            // Heterorganní shluk bez assimilace → epentheze.
            // Příklady: tk (studentka), kn (okno), kl (peklo), lk (jablko).
            return true;
        }

        #endregion Private Rules
    }
}
