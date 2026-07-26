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
        /// Covers the phonologically regular triggers: the following word opens with the same consonant or its
        /// voicing counterpart (ve vodě, se sestrou, ze země, ke kamarádovi), or with the cluster mn-
        /// (se mnou, ke mně), or — for v and z — with a sibilant-initial cluster (ve škole, ze zdi).
        /// The lexicalized cases are not covered and would need their own data: ve dvou, ve třech, ke stu.
        /// </remarks>
        public string Vocalize(string preposition, string followingWord)
        {
            if (!_prepositions.TryGetValue(preposition, out var data)
                || data.Vocalized is null
                || string.IsNullOrEmpty(followingWord))
            {
                return preposition;
            }

            var next = followingWord.ToLowerInvariant();
            var final = preposition[^1];

            return RequiresVocalization(final, next) ? data.Vocalized : preposition;
        }

        private static bool RequiresVocalization(char prepositionFinal, string next)
        {
            if (next.StartsWith("mn", StringComparison.Ordinal))
            {
                return true;
            }

            if (SameOrPaired(prepositionFinal, next[0]))
            {
                return true;
            }

            // v and z also vocalize before a cluster that opens with a sibilant: ve škole, ve smyslu, ze zdi.
            return prepositionFinal is 'v' or 'z'
                && next.Length > 1
                && "szšž".Contains(next[0])
                && !IsVowel(next[1]);
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
