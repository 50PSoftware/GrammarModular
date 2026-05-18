namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents conditional particle forms grouped by grammatical number.
    /// </summary>
    public sealed record ConditionalParticles
    {
        /// <summary>
        /// Gets or sets plural.
        /// </summary>
        public IReadOnlyDictionary<string, string> Plural { get; init; }
        /// <summary>
        /// Gets or sets singular.
        /// </summary>
        public IReadOnlyDictionary<string, string> Singular { get; init; }
    }

    /// <summary>
    /// Represents Czech particle forms loaded from JSON data.
    /// </summary>
    public sealed record ParticlesData
    {
        /// <summary>
        /// Gets or sets conditional.
        /// </summary>
        public ConditionalParticles Conditional { get; init; }
        /// <summary>
        /// Gets or sets reflexive.
        /// </summary>
        public ReflexiveParticles Reflexive { get; init; }
    }

    /// <summary>
    /// Represents reflexive particle forms grouped by syntactic context.
    /// </summary>
    public sealed record ReflexiveParticles
    {
        /// <summary>
        /// Gets or sets dative.
        /// </summary>
        public string Dative { get; init; }
        /// <summary>
        /// Gets or sets accusative.
        /// </summary>
        public string Accusative { get; init; }
    }
}
