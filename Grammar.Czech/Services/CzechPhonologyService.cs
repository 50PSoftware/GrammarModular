using Grammar.Core.Interfaces;
using Grammar.Core.Models.Phonology;
using Grammar.Czech.Models;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Grammar.Czech.Services
{
    public class CzechPhonologyService : IPhonologyService<CzechWordRequest>
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

        public string ApplySoftening(string stem)
        {
            if (stem is null)
                throw new ArgumentNullException(nameof(stem));

            if (stem.EndsWith("ch"))
            {
                var palatalizedCH = _registry.Get("ch")?.PalatalizeTo ?? throw new InvalidOperationException("Phoneme 'ch' is missing in registry.");
                return stem[..^2] + palatalizedCH;
            }

            var last = stem[^1..];
            var phoneme = _registry.Get(last);
            return phoneme?.PalatalizeTo is not null
                ? stem[..^1] + phoneme.PalatalizeTo
                : stem;
        }

        public string RevertSoftening(string stem)
        {
            if (stem is null)
                throw new ArgumentNullException(nameof(stem));

            var last = stem[^1..];
            return _reverseMap.TryGetValue(last, out var original)
                ? stem[..^1] + original
                : stem;
        }

        public bool HasMobileVowel(string stem)
        {
            if (string.IsNullOrEmpty(stem) || stem.Length < 2)
            {
                return false;
            }

            var lastTwo = stem[^2..];
            var vowel = lastTwo[0];
            var consonant = lastTwo[1];

            if (vowel != 'e' && vowel != 'ě')
            {
                return false;
            }

            return _registry.IsConsonant(consonant);
        }

        public string InsertMobileVowel(string stem, int position)
        {
            if (position < 0 || position > stem.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be within the bounds of the stem.");
            }

            return stem.Insert(position, "e");
        }

        public string RemoveMobileVowel(string stem)
        {
            if (!HasMobileVowel(stem))
            {
                return stem;
            }

            return stem[..^2] + stem[^1];
        }

        public string ApplyEpenthesis(bool needsEpenthesis, string stem, string suffix)
        {
            if (needsEpenthesis)
            {
                return stem + "e" + suffix;
            }

            return stem + suffix;
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
    }
}
