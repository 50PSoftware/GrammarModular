using Grammar.Core.Enums;
using Grammar.Core.Enums.PhonologicalFeatures;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Phonology;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides Czech preposition case government and semantic group lookup operations.
    /// </summary>
    public class CzechPrepositionService : ICzechPrepositionService
    {
        private readonly Dictionary<string, PrepositionData> _prepositions;
        private readonly IPhonemeRegistry _phonemes;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechPrepositionService"/> type.
        /// </summary>
        public CzechPrepositionService(IPrepositionDataProvider dataProvider, IPhonemeRegistry phonemes)
        {
            _prepositions = dataProvider.GetPrepositions();
            _phonemes = phonemes;
        }

        /// <summary>
        /// Gets the grammatical cases allowed by the supplied preposition.
        /// </summary>
        /// <param name="preposition">The preposition text to look up.</param>
        /// <returns>The grammatical cases governed by the preposition, or an empty sequence when it is unknown.</returns>
        public IEnumerable<Case> GetAllowedCases(string preposition)
        {
            if (_prepositions.TryGetValue(preposition, out var data))
            {
                return data.Variants.Select(v => v.Case).Distinct();
            }

            return Enumerable.Empty<Case>();
        }

        /// <summary>
        /// Gets the semantic group for a preposition and governed case.
        /// </summary>
        /// <param name="preposition">The preposition text to look up.</param>
        /// <param name="case">The grammatical case governed by the preposition.</param>
        /// <returns>The semantic group for the matching preposition variant, or <see langword="null"/> when no variant matches.</returns>
        public PrepositionSemanticGroup? GetSemanticGroup(string preposition, Case @case)
        {
            if (_prepositions.TryGetValue(preposition, out var data))
            {
                return data.Variants.FirstOrDefault(v => v.Case == @case)?.SemanticGroup;
            }

            return null;
        }

        /// <summary>
        /// Gets the surface form of the preposition before the supplied word, vocalized where required.
        /// </summary>
        /// <param name="preposition">The preposition text to look up.</param>
        /// <param name="followingWord">The word that immediately follows the preposition.</param>
        /// <returns>The vocalized variant when the following word requires it; otherwise, the preposition unchanged.</returns>
        /// <remarks>
        /// Vocalization is not a settled rule — the IJP is explicit that usage decides and the forms vary —
        /// so this covers the tendencies it states and no more:
        /// <list type="bullet">
        /// <item>before mn-, whatever the preposition: se mnou, ke mně, beze mne, ode mne;</item>
        /// <item>the same consonant or its voicing counterpart: ve vodě, se sestrou, ze země, ke gauči;</item>
        /// <item>the second consonant of the cluster repeats the preposition's: ve dveřích, ve svém,
        /// ke skoku, ve dvou;</item>
        /// <item>three or more consonants: ke středu, se vstupem, ve skladišti, ze vzpomínek;</item>
        /// <item>for v and z, a sibilant-initial cluster: ve škole, ve smyslu.</item>
        /// </list>
        /// Syllabic prepositions are excluded from all of it except mn-, because they mostly do not vocalize
        /// even before the same consonant: bez zákona, not beze zákona.
        /// Genuinely lexicalized forms are out of reach of any rule and are listed per preposition in
        /// <see cref="PrepositionData.VocalizeBefore"/> instead: se dvěma, se třemi, se čtyřmi.
        /// </remarks>
        public string Vocalize(string preposition, string followingWord)
        {
            if (!_prepositions.TryGetValue(preposition, out var data)
                || data.Vocalized is null
                || string.IsNullOrEmpty(followingWord))
            {
                return preposition;
            }

            return RequiresVocalization(data, preposition, followingWord.ToLowerInvariant()) ? data.Vocalized : preposition;
        }

        private bool RequiresVocalization(PrepositionData data, string preposition, string next)
        {
            if (next.StartsWith("mn", StringComparison.Ordinal))
            {
                return true;
            }

            // Lexicalized combinations no cluster rule reaches, listed in the data for this preposition.
            if (data.VocalizeBefore.Any(prefix => next.StartsWith(prefix, StringComparison.Ordinal)))
            {
                return true;
            }

            // A syllabic preposition already has its vowel and keeps it: bez zákona, od dveří.
            if (preposition.Any(IsVowel))
            {
                return false;
            }

            var final = preposition[^1];

            if (SameOrPaired(final, next[0]))
            {
                return true;
            }

            if (LeadingConsonants(next) < 2)
            {
                return false;
            }

            // The second consonant of the cluster repeating the preposition's: ve dveřích, ke skoku, ve dvou.
            if (next[1] == final)
            {
                return true;
            }

            // Three consonants running are awkward enough on their own: ke středu, ve skladišti.
            if (LeadingConsonants(next) >= 3)
            {
                return true;
            }

            // v and z also vocalize before a cluster that opens with a sibilant: ve škole, ve smyslu.
            var opening = _phonemes.Get(next[0]);

            return final is 'v' or 'z' && opening is not null && IsSibilant(opening);
        }

        private int LeadingConsonants(string word)
        {
            var count = 0;
            while (count < word.Length && !IsVowel(word[count]))
            {
                count++;
            }

            return count;
        }

        // The same consonant, its voicing counterpart, or the sibilant series that s and z share with š and
        // ž. All three come out of the phoneme registry rather than a table here: the voicing pairs v/f, k/g,
        // s/z and d/t are already stated once on Phoneme, and a sibilant is just a fricative articulated at
        // the alveolar or palatal place, which the features say directly. Restating them locally is how the
        // two descriptions drift apart.
        private bool SameOrPaired(char prepositionFinal, char next)
        {
            if (prepositionFinal == next)
            {
                return true;
            }

            var final = _phonemes.Get(prepositionFinal);
            var following = _phonemes.Get(next);

            if (final is null || following is null)
            {
                return false;
            }

            return final.VoicedCounterpart == following.Symbol
                || final.VoicelessCounterpart == following.Symbol
                || (IsSibilant(final) && IsSibilant(following));
        }

        private static bool IsSibilant(Phoneme phoneme) =>
            phoneme.Manner == ArticulationManner.Fricative
            && phoneme.Place is ArticulationPlace.Alveolar or ArticulationPlace.Palatal;

        private bool IsVowel(char c) => _phonemes.IsVowel(c);

        /// <summary>
        /// Determines whether the supplied preposition can govern the requested case.
        /// </summary>
        /// <param name="preposition">The preposition text to look up.</param>
        /// <param name="case">The grammatical case governed by the preposition.</param>
        /// <returns><see langword="true"/> when the case is allowed for the preposition; otherwise, <see langword="false"/>.</returns>
        public bool IsAllowed(string preposition, Case @case)
        {
            return GetAllowedCases(preposition).Contains(@case);
        }
    }
}
