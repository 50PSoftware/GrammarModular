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
    /// <remarks>
    /// An unlisted preposition passes through rather than being reported, and that leniency is deliberate.
    /// NESČ splits the word class: the original prepositions are a closed set, while the secondary ones —
    /// derived and compound, the kvůli and v rámci type — form an open class that cannot be enumerated. So
    /// an absent preposition is not evidence of a bad request the way an absent pronoun is, and government
    /// is checked only where the data has something to check against.
    /// </remarks>
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
        /// Vocalization is not a settled rule — the ÚJČ reference is explicit that usage decides and the
        /// forms vary — so this follows the conditions it states, in its order:
        /// <list type="bullet">
        /// <item>never before a vowel: k ústavě, s ementálem, v okolí, z Asie;</item>
        /// <item>always before the same consonant: ke kořenům, se sestrou, ve vejci, ze země;</item>
        /// <item>before a similar consonant — s before z/ž/š, z before s/š/ž, v before f, k before g:
        /// se ženou, ze stromu, ve Francii, ke gauči;</item>
        /// <item>three or more consonants: ke středu, se vstupem, ve skladišti, ze vzpomínek;</item>
        /// <item>two consonants whose second is r, ř or l do <em>not</em> vocalize — s prací, z Prahy,
        /// v trávě, k bráně — except for the clusters tř, dř, sl, zr and zl, which do: ve třech, ze dřeva,
        /// ke slibu, se zrádcem, ze zlata;</item>
        /// <item>the second consonant of the cluster repeating the preposition's: ve dveřích, ve svém,
        /// ke skoku;</item>
        /// <item>for v and z, a sibilant-initial cluster: ve škole, ve smyslu.</item>
        /// </list>
        /// Syllabic prepositions mostly do not vocalize at all — bez zákona, not beze zákona — and are
        /// excluded from the cluster conditions. They vocalize only before forms of "všechen" and "já"
        /// (beze všeho, nade vše, přede všemi, pode mnou, beze mne), which is a rule rather than a list,
        /// and in a few settled cases registered per preposition: ode dveří, ode dneška, beze studu.
        /// <para>
        /// Two things stay out of reach. The archaic ku survives only in fixed expressions before a labial
        /// (ku příkladu, ku prospěchu) and in ratios, so it is not generated. And "s sebou" resists the
        /// same-consonant rule; it is registered in <see cref="PrepositionData.DoNotVocalizeBefore"/>
        /// rather than ruled, with the caveat that reflexive "se sebou" — spokojený sám se sebou — is a
        /// different construction the service cannot tell apart from the string alone.
        /// </para>
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

        // The clusters that vocalize even though their second member is r, ř or l, which otherwise blocks it.
        // A closed list stated by the ÚJČ reference: ve třech, ze dřeva, ke slibu, se zrádcem, ze zlata.
        private static readonly string[] VocalizingClusters = ["tř", "dř", "sl", "zr", "zl"];

        private bool RequiresVocalization(PrepositionData data, string preposition, string next)
        {
            // Settled spellings that override the rules the other way: s sebou keeps its bare preposition
            // despite the same consonant.
            if (data.DoNotVocalizeBefore.Any(word => next.StartsWith(word, StringComparison.Ordinal)))
            {
                return false;
            }

            // Forms of "já" and "všechen" vocalize whatever the preposition, syllabic or not: se mnou,
            // ke mně, pode mnou, beze všeho, nade vše, přede všemi.
            if (next.StartsWith("mn", StringComparison.Ordinal) || next.StartsWith("vš", StringComparison.Ordinal))
            {
                return true;
            }

            // Settled combinations no cluster rule reaches, listed in the data for this preposition.
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

            var leading = LeadingConsonants(next);

            // Nothing before a vowel: k ústavě, v okolí. A single consonant is no harder: k řece, v zimě.
            if (leading < 2)
            {
                return false;
            }

            // Three consonants running are awkward enough on their own: ke středu, ve skladišti.
            if (leading >= 3)
            {
                return true;
            }

            // The second consonant of the cluster repeating the preposition's: ve dveřích, ke skoku.
            if (next[1] == final)
            {
                return true;
            }

            if (VocalizingClusters.Any(cluster => next.StartsWith(cluster, StringComparison.Ordinal)))
            {
                return true;
            }

            // A two-consonant cluster closing on r, ř or l is easy to say and blocks vocalization:
            // s prací, z Prahy, v trávě, k bráně.
            if (next[1] is 'r' or 'ř' or 'l')
            {
                return false;
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

        // The same consonant, its voicing counterpart, or the sibilant series. All three come out of the
        // phoneme registry rather than a local table, which is how two descriptions drift apart.
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
