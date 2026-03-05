using Grammar.Core.Models.Phonology;
using Grammar.Czech.Enums.Phonology;

namespace Grammar.Czech.Models
{
    public sealed record CzechPhoneme : Phoneme
    {
        public Dictionary<PalatalizationContext, string>? PalatalizationTargets { get; init; }
    }
}
