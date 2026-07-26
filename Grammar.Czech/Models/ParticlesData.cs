namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents particle forms keyed by grammatical number and then by person.
    /// </summary>
    /// <remarks>
    /// A person may be absent, which means the paradigm has no form there rather than an empty one:
    /// the Czech past tense takes no third-person auxiliary at all ("dělal", not "dělal je").
    /// </remarks>
    public sealed record PersonParticles
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
        public PersonParticles Conditional { get; init; }

        /// <summary>
        /// Gets or sets the past-tense auxiliary forms of "být" that join the clitic cluster.
        /// </summary>
        public PersonParticles PastAuxiliary { get; init; }
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
