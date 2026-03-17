using Grammar.Core.Enums.PhonologicalFeatures;
using Grammar.Core.Interfaces;
using Grammar.Czech.Enums.Phonology;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using System.ComponentModel;

namespace Grammar.Czech.Services
{
    public class CzechPhonologyService : ICzechPhonologyService
    {
        private readonly IPhonemeRegistry _registry;
        private readonly IReadOnlyDictionary<string, string> _reverseMap;

        public CzechPhonologyService(IPhonemeRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _reverseMap = _registry.AllPhonemes
                .Where(p => p.PalatalizeTo is not null)
                .ToDictionary(p => p.PalatalizeTo!, p => p.Symbol);
        }

        public string ApplySoftening(string stem) => ApplySoftening(stem, PalatalizationContext.First);

        public string RevertSoftening(string stem)
        {
            if (stem is null)
                throw new ArgumentNullException(nameof(stem));

            var last = stem[^1..];
            return _reverseMap.TryGetValue(last, out var original)
                ? stem[..^1] + original
                : stem;
        }

        public string InsertMobileVowel(string stem, int position)
        {
            if (position < 0 || position > stem.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be within the bounds of the stem.");
            }

            return stem.Insert(position, "e");
        }

        public string RemoveMobileVowel(string stem, bool hasMobileVowel)
        {
            if (!hasMobileVowel)
            {
                return stem;
            }

            return stem[..^2] + stem[^1];
        }

        public string ApplyEpenthesis(bool needsEpenthesis, string stem, string derivationSuffix)
        {
            if (needsEpenthesis)
            {
                return stem + "e" + derivationSuffix;
            }

            return stem + derivationSuffix;
        }

        public string ShortenVowel(string stem)
        {
            for (int i = stem.Length - 1; i >= 0; i--)
            {
                var phoneme = _registry.Get(stem[i]);
                if (phoneme?.ShortCounterpart is not null)
                {
                    return stem[..i] + phoneme.ShortCounterpart + stem[(i + 1)..];
                }
            }

            return stem;
        }

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

        //public string ApplySoftConsonantBeforeE(string stem)
        //{
        //    var last = stem[^1..];
        //    var phoneme = _registry.Get(last);

        //    if (phoneme?.PalatalizeTo is not null)
        //    {
        //        return stem + "ě";
        //    }

        //    return stem + "e";
        //}
    }
}
