using Grammar.Core.Enums.PhonologicalFeatures;
using Grammar.Core.Interfaces;
using Grammar.Czech.Enums.Phonology;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides czech phonology operations.
    /// </summary>
    public class CzechPhonologyService : ICzechPhonologyService
    {
        private readonly IPhonemeRegistry _registry;
        private readonly IReadOnlyDictionary<string, string> _reverseMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechPhonologyService"/> type.
        /// </summary>
        public CzechPhonologyService(IPhonemeRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _reverseMap = _registry.AllPhonemes
                .Where(p => p.PalatalizeTo is not null)
                .ToDictionary(p => p.PalatalizeTo!, p => p.Symbol);
        }

        /// <summary>
        /// Applies Czech consonant softening for the supplied palatalization context.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <returns>The stem after context-sensitive consonant softening has been applied.</returns>
        public string ApplySoftening(string stem) => ApplySoftening(stem, PalatalizationContext.First);

        /// <summary>
        /// Reverts the final softened consonant to its unsoftened form when a reverse mapping exists.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <returns>The stem after softening has been reverted when possible.</returns>
        public string RevertSoftening(string stem)
        {
            if (stem is null)
                throw new ArgumentNullException(nameof(stem));

            var last = stem[^1..];
            return _reverseMap.TryGetValue(last, out var original)
                ? stem[..^1] + original
                : stem;
        }

        /// <summary>
        /// Inserts a mobile vowel into the supplied stem when the morphology rule requires it.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="position">The zero-based position where the mobile vowel should be inserted.</param>
        /// <returns>The stem with the mobile vowel inserted at the requested position.</returns>
        public string InsertMobileE(string stem, int position)
        {
            if (position < 0 || position > stem.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be within the bounds of the stem.");
            }

            return stem.Insert(position, "e");
        }

        /// <summary>
        /// Removes a mobile vowel from the supplied stem when the morphology rule requires it.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="hasMobileVowel">True when the stem is known to contain a mobile vowel; otherwise, false.</param>
        /// <returns>The stem with its mobile vowel removed when applicable.</returns>
        public string RemoveMobileE(string stem, bool hasMobileVowel)
        {
            if (!hasMobileVowel)
            {
                return stem;
            }

            // Only ever delete an actual e/ě. Without this guard a wrong flag silently removes whatever
            // letter happens to sit in that slot (doktor → doktr, Mendominátor → Mendominátr).
            if (stem.Length < 2 || (stem[^2] != 'e' && stem[^2] != 'ě'))
            {
                return stem;
            }

            return stem[..^2] + stem[^1];
        }

        /// <summary>
        /// Adds or removes epenthetic material between a stem and suffix.
        /// </summary>
        /// <param name="needsEpenthesis">True when epenthesis should be applied; otherwise, false.</param>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="derivationSuffix">The derivational suffix that may require epenthesis.</param>
        /// <returns>The stem combined with the suffix, including epenthesis when requested.</returns>
        public string ApplyEpenthesis(bool needsEpenthesis, string stem, string derivationSuffix)
        {
            if (needsEpenthesis)
            {
                return stem + "e" + derivationSuffix;
            }

            return stem + derivationSuffix;
        }

        /// <summary>
        /// Shortens the final long vowel in the supplied stem when possible.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <returns>The stem with the final long vowel shortened when a short counterpart exists.</returns>
        public string ShortenVowel(string stem)
        {
            for (int i = stem.Length - 1; i >= 0; i--)
            {
                // Diftong se zkouší dřív než jeho druhý znak, jinak by se ou rozpadlo a zkrátil by
                // se jen ten kus, který zbyde.
                if (i > 0 && _registry.Get(stem[(i - 1)..(i + 1)])?.ShortCounterpart is { } diphthong)
                {
                    return stem[..(i - 1)] + diphthong + stem[(i + 1)..];
                }

                var phoneme = _registry.Get(stem[i]);
                if (phoneme?.ShortCounterpart is not null)
                {
                    return stem[..i] + phoneme.ShortCounterpart + stem[(i + 1)..];
                }
            }

            return stem;
        }

        /// <summary>
        /// Lengthens the final short vowel in the supplied stem when possible.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <returns>The stem with the final short vowel lengthened when a long counterpart exists.</returns>
        public string LengthenVowel(string stem)
        {
            for (int i = stem.Length - 1; i >= 0; i--)
            {
                var phoneme = _registry.Get(stem[i]);
                if (phoneme?.LongCounterpart is not null)
                {
                    return stem[..i] + phoneme.LongCounterpart + stem[(i + 1)..];
                }
            }

            return stem;
        }

        /// <summary>
        /// Applies Czech consonant softening for the supplied palatalization context.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="context">The palatalization context that selects the softening target.</param>
        /// <returns>The stem after context-sensitive consonant softening has been applied.</returns>
        public string ApplySoftening(string stem, PalatalizationContext context)
        {
            if (stem is null)
            {
                throw new ArgumentNullException(nameof(stem));
            }

            if (stem.EndsWith("ch"))
            {
                var czechPhoneme = _registry.Get("ch") as CzechPhoneme;
                var target = czechPhoneme?.PalatalizationTargets?.GetValueOrDefault(context) ?? _registry.Get("ch")?.PalatalizeTo ?? throw new InvalidOperationException("Phoneme 'ch' missing palatalization target.");

                return stem[..^2] + target;
            }

            var last = stem[^1..];
            var phoneme = _registry.Get(last);
            var czechP = phoneme as CzechPhoneme;
            var result = czechP?.PalatalizationTargets?.GetValueOrDefault(context) ?? phoneme?.PalatalizeTo;
            return result is not null ? stem[..^1] + result : stem;
        }

        /// <summary>
        /// Applies orthographic consonant softening before a soft ending, leaving d/t/n unchanged.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="context">The palatalization context used to choose the softening target.</param>
        /// <returns>The stem after orthographic consonant softening has been applied.</returns>
        public string ApplyOrthographicSoftening(string stem, PalatalizationContext context)
        {
            if (stem is null)
            {
                throw new ArgumentNullException(nameof(stem));
            }

            var phoneme = _registry.Get(stem[^1..]);

            // d/t/n palatalization is phonetic only; the ě digraph carries it, so keep the grapheme.
            var isDTN = phoneme?.Place == ArticulationPlace.Alveolar
                && (phoneme.Manner == ArticulationManner.Plosive
                    || phoneme.Manner == ArticulationManner.Nasal);

            return isDTN ? stem : ApplySoftening(stem, context);
        }
    }
}
