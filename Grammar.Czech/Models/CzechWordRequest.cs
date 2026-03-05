namespace Grammar.Czech.Models
{
    using Grammar.Core.Enums;
    using Grammar.Core.Interfaces;

    public enum Degree
    {
        Positive,
        Comparative,
        Superlative
    }

    /// <summary>
    /// Slovesná třída.
    /// </summary>
    public enum VerbClass
    { Class1, Class2, Class3, Class4, Class5 }

    public struct CzechWordRequest : IWordRequest
    {
        public CzechWordRequest()
        { }

        public Degree? Degree { get; set; }
        public bool? HasReflexive { get; set; }
        public bool? HasExplicitSubject { get; set; }
        public VerbClass? VerbClass { get; set; }
        public string Lemma { get; set; } = string.Empty;
        public Gender? Gender { get; set; }
        public Number? Number { get; set; }
        public Case? Case { get; set; }
        public Person? Person { get; set; }
        public Tense? Tense { get; set; }
        public VerbAspect? Aspect { get; set; }
        public Modus? Modus { get; set; }
        public Voice? Voice { get; set; }
        public WordCategory WordCategory { get; set; }
        public string? Pattern { get; set; }
        public string? AdditionalData { get; set; }
        public bool IsNegative { get; set; }
        public bool? IsAnimate { get; set; }
    }
}
