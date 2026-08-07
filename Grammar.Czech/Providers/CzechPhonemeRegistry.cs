using Grammar.Core.Enums.PhonologicalFeatures;
using Grammar.Core.Extensions;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Phonology;
using Grammar.Czech.Enums.Phonology;
using Grammar.Czech.Models;

namespace Grammar.Czech.Providers
{
    /// <summary>
    /// Registers Czech phonemes and exposes phoneme lookup operations.
    /// </summary>
    public sealed class CzechPhonemeRegistry : IPhonemeRegistry
    {
        private static readonly Dictionary<string, Phoneme> _phonemes = BuildRegistry();

        private static Dictionary<string, Phoneme> BuildRegistry() => new()
        {
            ["p"] = new CzechPhoneme { Symbol = "p", Place = ArticulationPlace.Bilabial, Manner = ArticulationManner.Plosive, Voicing = Voicing.Voiceless, VoicedCounterpart = "b", TriggersEpenthesisAsC2 = false },
            ["b"] = new CzechPhoneme { Symbol = "b", Place = ArticulationPlace.Bilabial, Manner = ArticulationManner.Plosive, Voicing = Voicing.Voiced, VoicelessCounterpart = "p", TriggersEpenthesisAsC2 = true },
            ["f"] = new CzechPhoneme { Symbol = "f", Place = ArticulationPlace.Labiodental, Manner = ArticulationManner.Fricative, Voicing = Voicing.Voiceless, TriggersEpenthesisAsC2 = false, VoicedCounterpart = "v" },
            ["v"] = new Phoneme { Symbol = "v", Place = ArticulationPlace.Labiodental, Manner = ArticulationManner.Fricative, Voicing = Voicing.Voiced, VoicelessCounterpart = "f" },
            ["m"] = new Phoneme { Symbol = "m", Place = ArticulationPlace.Bilabial, Manner = ArticulationManner.Nasal },
            ["t"] = new CzechPhoneme { Symbol = "t", Place = ArticulationPlace.Alveolar, Manner = ArticulationManner.Plosive, Voicing = Voicing.Voiceless, VoicedCounterpart = "d", PalatalizeTo = "ť", TriggersEpenthesisAsC2 = false },
            ["d"] = new CzechPhoneme { Symbol = "d", Place = ArticulationPlace.Alveolar, Manner = ArticulationManner.Plosive, Voicing = Voicing.Voiced, VoicelessCounterpart = "t", TriggersEpenthesisAsC2 = false, PalatalizeTo = "ď" },
            ["s"] = new CzechPhoneme { Symbol = "s", Place = ArticulationPlace.Alveolar, Manner = ArticulationManner.Fricative, Voicing = Voicing.Voiceless, VoicedCounterpart = "z", TriggersEpenthesisAsC2 = false },
            ["z"] = new CzechPhoneme { Symbol = "z", Place = ArticulationPlace.Alveolar, Manner = ArticulationManner.Fricative, Voicing = Voicing.Voiced, VoicelessCounterpart = "s", TriggersEpenthesisAsC2 = false },
            ["n"] = new Phoneme { Symbol = "n", Place = ArticulationPlace.Alveolar, Manner = ArticulationManner.Nasal, PalatalizeTo = "ň", AssimilatesPlaceBefore = ArticulationPlace.Velar },
            ["c"] = new Phoneme { Symbol = "c", Place = ArticulationPlace.Alveolar, Manner = ArticulationManner.Affricate, Voicing = Voicing.Voiceless, PalatalizeTo = "č" },
            ["č"] = new Phoneme { Symbol = "č", Place = ArticulationPlace.Palatal, Manner = ArticulationManner.Affricate, Voicing = Voicing.Voiceless },
            ["ť"] = new Phoneme { Symbol = "ť", Place = ArticulationPlace.Palatal, Manner = ArticulationManner.Plosive, Voicing = Voicing.Voiceless },
            ["ď"] = new Phoneme { Symbol = "ď", Place = ArticulationPlace.Palatal, Manner = ArticulationManner.Plosive, Voicing = Voicing.Voiced },
            ["š"] = new CzechPhoneme { Symbol = "š", Place = ArticulationPlace.Palatal, Manner = ArticulationManner.Fricative, Voicing = Voicing.Voiceless, VoicedCounterpart = "ž", TriggersEpenthesisAsC2 = false },
            ["ž"] = new Phoneme { Symbol = "ž", Place = ArticulationPlace.Palatal, Manner = ArticulationManner.Fricative, Voicing = Voicing.Voiced, VoicelessCounterpart = "š" },
            ["ň"] = new Phoneme { Symbol = "ň", Place = ArticulationPlace.Palatal, Manner = ArticulationManner.Nasal },
            ["k"] = new CzechPhoneme
            {
                Symbol = "k",
                Place = ArticulationPlace.Velar,
                Manner = ArticulationManner.Plosive,
                Voicing = Voicing.Voiceless,
                VoicedCounterpart = "g",
                TriggersEpenthesisAsC2 = true,
                PalatalizeTo = "c",
                PalatalizationTargets = new() {
                { PalatalizationContext.First, "č"},
                { PalatalizationContext.Second, "c" } }
            },
            ["g"] = new CzechPhoneme
            {
                Symbol = "g",
                Place = ArticulationPlace.Velar,
                Manner = ArticulationManner.Plosive,
                Voicing = Voicing.Voiced,
                VoicelessCounterpart = "k",
                // 2nd palatalization g→z (droga→droze). No PalatalizeTo: the reverse map is keyed
                // by PalatalizeTo and "z" is already taken by h, so a base target would collide.
                PalatalizationTargets = new() {
                { PalatalizationContext.First, "ž" },
                { PalatalizationContext.Second, "z" } }
            },
            ["ch"] = new CzechPhoneme
            {
                Symbol = "ch",
                Place = ArticulationPlace.Velar,
                Manner = ArticulationManner.Fricative,
                Voicing = Voicing.Voiceless,
                VoicedCounterpart = "h",
                TriggersEpenthesisAsC2 = false,
                PalatalizeTo = "š",
                PalatalizationTargets = new Dictionary<PalatalizationContext, string>() {
                { PalatalizationContext.First, "š" },
                { PalatalizationContext.Second, "s" } }
            },
            ["h"] = new CzechPhoneme
            {
                Symbol = "h",
                Place = ArticulationPlace.Velar,
                Manner = ArticulationManner.Fricative,
                Voicing = Voicing.Voiced,
                VoicelessCounterpart = "ch",
                PalatalizeTo = "z",
                PalatalizationTargets = new() {
                { PalatalizationContext.First, "ž" },
                { PalatalizationContext.Second, "z" } }
            },
            ["j"] = new Phoneme { Symbol = "j", Place = ArticulationPlace.Palatal, Manner = ArticulationManner.Approximant },
            ["ř"] = new Phoneme { Symbol = "ř", Place = ArticulationPlace.Alveolar, Manner = ArticulationManner.Trill },
            ["r"] = new Phoneme { Symbol = "r", Place = ArticulationPlace.Alveolar, Manner = ArticulationManner.Trill, PalatalizeTo = "ř" },
            ["l"] = new Phoneme { Symbol = "l", Place = ArticulationPlace.Alveolar, Manner = ArticulationManner.LateralApproximant },
            ["a"] = new Phoneme { Symbol = "a", Backness = VowelBackness.Central, Height = VowelHeight.Open, IsRounded = false, LongCounterpart = "á" },
            ["e"] = new Phoneme { Symbol = "e", Backness = VowelBackness.Front, Height = VowelHeight.Mid, IsRounded = false, LongCounterpart = "é" },
            ["i"] = new Phoneme { Symbol = "i", Backness = VowelBackness.Front, Height = VowelHeight.Close, IsRounded = false, LongCounterpart = "í" },
            ["y"] = new Phoneme { Symbol = "y", Backness = VowelBackness.Front, Height = VowelHeight.Close, IsRounded = false, LongCounterpart = "ý" },
            ["o"] = new Phoneme { Symbol = "o", Backness = VowelBackness.Back, Height = VowelHeight.Mid, IsRounded = true, LongCounterpart = "ů" },
            ["u"] = new Phoneme { Symbol = "u", Backness = VowelBackness.Back, Height = VowelHeight.Close, IsRounded = true },
            ["á"] = new Phoneme { Symbol = "á", Backness = VowelBackness.Central, Height = VowelHeight.Open, IsRounded = false, ShortCounterpart = "a" },
            ["é"] = new Phoneme { Symbol = "é", Backness = VowelBackness.Front, Height = VowelHeight.Mid, IsRounded = false, ShortCounterpart = "e" },
            ["í"] = new Phoneme { Symbol = "í", Backness = VowelBackness.Front, Height = VowelHeight.Close, IsRounded = false, ShortCounterpart = "i" },
            ["ý"] = new Phoneme { Symbol = "ý", Backness = VowelBackness.Front, Height = VowelHeight.Close, IsRounded = false, ShortCounterpart = "y" },
            ["ó"] = new Phoneme { Symbol = "ó", Backness = VowelBackness.Back, Height = VowelHeight.Mid, IsRounded = true },
            ["ú"] = new Phoneme { Symbol = "ú", Backness = VowelBackness.Back, Height = VowelHeight.Close, IsRounded = true },
            ["ů"] = new Phoneme { Symbol = "ů", Backness = VowelBackness.Back, Height = VowelHeight.Close, IsRounded = true, ShortCounterpart = "o" },
            // Krátký protějšek u, ne o: ou je dlouhý člen kvantitativní dvojice u/ou (houba → hub,
            // plul → plout). Že jde o jeden foném, a ne o spojení o+u, stojí právě na téhle
            // alternaci — viz heslo DIFTONG v Novém encyklopedickém slovníku češtiny.
            ["ou"] = new Phoneme { Symbol = "ou", Backness = VowelBackness.Back, ShortCounterpart = "u" },
            ["eu"] = new Phoneme { Symbol = "eu", Backness = VowelBackness.Front },
            ["au"] = new Phoneme { Symbol = "au", Backness = VowelBackness.Central },
            ["ě"] = new Phoneme { Symbol = "ě", Backness = VowelBackness.Front, Height = VowelHeight.Mid, IsRounded = false },
        };

        /// <summary>
        /// Gets all phonemes registered by the Czech phoneme registry.
        /// </summary>
        public IReadOnlyCollection<Phoneme> AllPhonemes => _phonemes.Values;

        /// <summary>
        /// Gets the phoneme matching the supplied symbol.
        /// </summary>
        /// <param name="symbol">The phoneme symbol to look up.</param>
        /// <returns>The matching phoneme, or <see langword="null"/> when the symbol is not registered.</returns>
        public Phoneme? Get(string symbol)
        {
            return _phonemes.GetValueOrDefault(symbol);
        }

        /// <summary>
        /// Determines whether the supplied character is a vowel.
        /// </summary>
        /// <param name="c">The character to classify.</param>
        /// <returns><see langword="true"/> when the character is a vowel; otherwise, <see langword="false"/>.</returns>
        public bool IsVowel(char c)
        {
            return Get(c.ToString())?.IsVowel() ?? false;
        }

        /// <summary>
        /// Determines whether the supplied character is a consonant.
        /// </summary>
        /// <param name="c">The character to classify.</param>
        /// <returns><see langword="true"/> when the character is a consonant; otherwise, <see langword="false"/>.</returns>
        public bool IsConsonant(char c)
        {
            return Get(c.ToString())?.IsConsonant() ?? false;
        }

        /// <summary>
        /// Determines whether the supplied character is a front vowel.
        /// </summary>
        /// <param name="c">The character to classify.</param>
        /// <returns><see langword="true"/> when the character is a front vowel; otherwise, <see langword="false"/>.</returns>
        public bool IsFrontVowel(char c)
        {
            return Get(c.ToString())?.Backness == VowelBackness.Front;
        }
    }
}
