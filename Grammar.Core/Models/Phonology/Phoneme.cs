using Grammar.Core.Enums.PhonologicalFeatures;

namespace Grammar.Core.Models.Phonology
{
    public record Phoneme
    {
        public required string Symbol { get; init; }

        #region Consonants

        public ArticulationPlace? Place { get; init; }
        public ArticulationManner? Manner { get; init; }
        public Voicing? Voicing { get; init; }

        /// <summary>
        /// When set, indicates that this consonant obligatorily assimilates its place
        /// of articulation to match an immediately following consonant at the specified place.
        /// </summary>
        /// <remarks>
        /// Example: Czech /n/ assimilates to [ŋ] before velar consonants (k, g),
        /// making the cluster phonetically homorganic and thus not requiring epenthesis.
        /// </remarks>
        public ArticulationPlace? AssimilatesPlaceBefore { get; init; }

        #endregion Consonants

        #region Vowels

        public VowelBackness? Backness { get; init; }
        public VowelHeight? Height { get; init; }
        public bool? IsRounded { get; init; }

        #endregion Vowels

        #region Alternartions

        public string? PalatalizeTo { get; init; }
        public string? VoicedCounterpart { get; init; }
        public string? VoicelessCounterpart { get; init; }
        public string? ShortCounterpart { get; init; }
        public string? LongCounterpart { get; init; }

        #endregion Alternartions
    }
}
