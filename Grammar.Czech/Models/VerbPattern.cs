using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    public sealed record VerbPattern
    {
        public VerbAspect Aspect { get; init; }
        public VerbTenseForms Future { get; init; }
        public string? FutureStem { get; init; }
        public string? ImperativeStem { get; init; }
        public string? Infinitive { get; init; }
        public string? InheritsFrom { get; init; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PassiveParticiple { get; init; }
        public string? PassiveStem { get; init; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PastParticiple { get; init; }
        public string? PastStem { get; init; }
        public VerbTenseForms Present { get; init; }
        public string? PresentStem { get; init; }
        public string? Stem { get; init; }
    }
}
