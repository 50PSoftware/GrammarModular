namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents clitic forms keyed by grammatical number and then by person.
    /// </summary>
    /// <remarks>
    /// A person may be absent, which means the paradigm has no form there rather than an empty one:
    /// the Czech past tense takes no third-person auxiliary at all ("dělal", not "dělal je").
    /// </remarks>
    public sealed record PersonClitics
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
    /// Represents the Czech clitics loaded from JSON data.
    /// </summary>
    /// <remarks>
    /// These are clitics, not the word class <em>částice</em>, and the file used to be named for the latter.
    /// What it holds is the klitika tantum of NESČ — the conditional auxiliary, the past-tense auxiliary and
    /// the reflexive — words with no stress of their own that take a fixed rank in the second-position
    /// cluster. The word class částice is asi, prý, ať, snad, jen: uninflected, freely placed, and carrying
    /// meaning rather than grammar, and it lives in ParticleData beside this.
    /// <para>
    /// The two do overlap. NESČ notes that aby and kdyby introducing an autonomous clause get analysed as
    /// částice while still containing the conditional auxiliary that lives here, so a word may legitimately
    /// appear on both sides.
    /// </para>
    /// </remarks>
    public sealed record CliticsData
    {
        /// <summary>
        /// Gets or sets conditional.
        /// </summary>
        public PersonClitics Conditional { get; init; }

        /// <summary>
        /// Gets or sets the past-tense auxiliary forms of "být" that join the clitic cluster.
        /// </summary>
        public PersonClitics PastAuxiliary { get; init; }
        /// <summary>
        /// Gets or sets reflexive.
        /// </summary>
        public ReflexiveClitics Reflexive { get; init; }
    }

    /// <summary>
    /// Represents the reflexive clitic forms grouped by syntactic context.
    /// </summary>
    public sealed record ReflexiveClitics
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
