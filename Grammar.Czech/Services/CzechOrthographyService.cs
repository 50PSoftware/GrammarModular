using Grammar.Core.Enums.PhonologicalFeatures;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides Czech orthography adjustments for endings and jotation.
    /// </summary>
    public class CzechOrthographyService : ICzechOrthographyService
    {
        private readonly IPhonemeRegistry _registry;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechOrthographyService"/> type.
        /// </summary>
        public CzechOrthographyService(IPhonemeRegistry registry)
        {
            this._registry = registry;
        }

        /// <summary>
        /// Applies Czech orthographic changes caused by jotation.
        /// </summary>
        /// <param name="ending">The ending used to choose the morphology rule.</param>
        /// <returns>The ending with initial e changed to e-with-caron when jotation applies.</returns>
        public string ApplyJotationOrthography(string ending)
        {
            var normalized = ending.TrimStart('-');
            var dashPrefix = ending.Length - normalized.Length;

            if (!normalized.StartsWith('e'))
                return ending;

            return ending[..dashPrefix] + 'ě' + normalized[1..];
        }

        /// <summary>
        /// Normalizes ending spelling against the final stem consonant.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="ending">The ending used to choose the morphology rule.</param>
        /// <param name="pattern">The declension pattern, used to distinguish hard vs. soft paradigms.</param>
        /// <returns>The normalized ending spelling for the supplied stem.</returns>
        public string NormalizeEndingOrthography(string stem, string ending, string pattern)
        {
            if (string.IsNullOrEmpty(stem) || string.IsNullOrEmpty(ending))
                return ending;

            var normalizedEnding = ending.TrimStart('-');
            var dashPrefix = ending.Length - normalizedEnding.Length;

            // Pouze ě→e reverze — e→ě patří výhradně do JSON dat
            if (!normalizedEnding.StartsWith('ě'))
                return ending;

            var phoneme = _registry.Get(stem[^1..]);

            var isDTN = phoneme?.Place == ArticulationPlace.Alveolar
                && (phoneme.Manner == ArticulationManner.Nasal
                    || phoneme.Manner == ArticulationManner.Plosive);

            // Bilabiála (b/p/m) — ě se drží vždy (žena→mámě, hrad→domě, píseň→země).
            var isBilabial = phoneme?.Place == ArticulationPlace.Bilabial;

            // Labiodentála (v/f) — ě se drží jen v tvrdém skloňování (žena→Evě, žirafě);
            // v měkkém vzoru je koncové -e měkké (píseň→větve).
            var isHardLabiodental = phoneme?.Place == ArticulationPlace.Labiodental
                && !SoftPatterns.Contains(pattern);

            // ě se drží po DTN (ňe/ďe/ťe → dě/tě/ně) a po labiále dle výše.
            // Kdekoliv jinde je ortografická chyba → normalizuj na e.
            if (!isDTN && !isBilabial && !isHardLabiodental)
                return ending[..dashPrefix] + 'e' + normalizedEnding[1..];

            return ending;
        }

        // Měkké skloňovací vzory — koncové -e po labiále zůstává měkké (bez ě).
        private static readonly HashSet<string> SoftPatterns = new()
        {
            "píseň", "růže", "moře", "kuře", "stroj", "muž", "učitel", "soudce", "stavení"
        };
    }
}
