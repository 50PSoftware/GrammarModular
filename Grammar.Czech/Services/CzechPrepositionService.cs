using Grammar.Core.Enums;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechPrepositionService"/> type.
        /// </summary>
        public CzechPrepositionService(IPrepositionDataProvider dataProvider)
        {
            _prepositions = dataProvider.GetPrepositions();
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
        /// Genuinely lexicalized forms stay out of reach of any rule: ve třech, ke stu.
        /// </remarks>
        public string Vocalize(string preposition, string followingWord)
        {
            if (!_prepositions.TryGetValue(preposition, out var data)
                || data.Vocalized is null
                || string.IsNullOrEmpty(followingWord))
            {
                return preposition;
            }

            return RequiresVocalization(preposition, followingWord.ToLowerInvariant()) ? data.Vocalized : preposition;
        }

        private static bool RequiresVocalization(string preposition, string next)
        {
            if (next.StartsWith("mn", StringComparison.Ordinal))
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
            return final is 'v' or 'z' && "szšž".Contains(next[0]);
        }

        private static int LeadingConsonants(string word)
        {
            var count = 0;
            while (count < word.Length && !IsVowel(word[count]))
            {
                count++;
            }

            return count;
        }

        // Voicing pairs, plus the sibilant series that s and z share with š and ž.
        private static bool SameOrPaired(char prepositionFinal, char next) => prepositionFinal switch
        {
            'v' => next is 'v' or 'f',
            'k' => next is 'k' or 'g',
            's' or 'z' => next is 's' or 'z' or 'š' or 'ž',
            'd' => next is 'd' or 't',
            _ => prepositionFinal == next
        };

        private static bool IsVowel(char c) => "aáeéěiíyýoóuúů".Contains(c);

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
