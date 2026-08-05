using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents stems, endings, and overrides for a Czech verb pattern.
    /// </summary>
    public sealed record VerbPattern
    {
        /// <summary>
        /// Gets or sets the requested or resolved verb aspect.
        /// </summary>
        public VerbAspect Aspect { get; init; }
        /// <summary>
        /// Gets a value indicating whether the verb forms a passive participle at all.
        /// </summary>
        /// <remarks>
        /// Almost every verb does, including intransitive ones, which use it impersonally — bylo
        /// pracováno, bylo tančeno. The exception is small enough to name: moci has no passive
        /// participle in IJP where pomoci has pomožen, so it cannot be derived and has to be stated.
        /// <para>
        /// A pattern that says nothing forms one, so the endings inherited from a class keep working
        /// and only the verb that refuses has to say so.
        /// </para>
        /// </remarks>
        public bool FormsPassive { get; init; } = true;
        /// <summary>
        /// Gets or sets future.
        /// </summary>
        public VerbTenseForms Future { get; init; }
        /// <summary>
        /// Gets or sets the stem used for future forms.
        /// </summary>
        public string? FutureStem { get; init; }
        /// <summary>
        /// Gets or sets the stem used for imperative forms.
        /// </summary>
        public string? ImperativeStem { get; init; }
        /// <summary>
        /// Gets or sets infinitive.
        /// </summary>
        public string? Infinitive { get; init; }
        /// <summary>
        /// Gets or sets the base pattern key inherited by this pattern.
        /// </summary>
        public string? InheritsFrom { get; init; }
        /// <summary>
        /// Gets or sets passive Participle.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PassiveParticiple { get; init; }
        /// <summary>
        /// Gets or sets the stem used for passive forms.
        /// </summary>
        public string? PassiveStem { get; init; }
        /// <summary>
        /// Gets or sets past Participle.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PastParticiple { get; init; }
        /// <summary>
        /// Gets or sets the stem used for past forms.
        /// </summary>
        public string? PastStem { get; init; }
        /// <summary>
        /// Gets or sets present.
        /// </summary>
        public VerbTenseForms Present { get; init; }
        /// <summary>
        /// Gets or sets the stem used for present forms.
        /// </summary>
        public string? PresentStem { get; init; }
        /// <summary>
        /// Gets or sets the stem used by the pattern.
        /// </summary>
        public string? Stem { get; init; }
    }
}
