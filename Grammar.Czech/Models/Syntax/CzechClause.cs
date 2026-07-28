using Grammar.Core.Enums;

namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents a single Czech clause as a predicate plus its constituents, before linearization.
    /// </summary>
    /// <remarks>
    /// The clause carries no word order of its own. <see cref="Services.CzechSentenceBuilder"/> derives the
    /// order from <see cref="ClauseElement.Status"/>, so the caller states what is old and what is new rather
    /// than where things go. The order of <see cref="Elements"/> only breaks ties within one status.
    /// </remarks>
    public sealed record CzechClause
    {
        /// <summary>
        /// Gets the word request for the predicate.
        /// </summary>
        public CzechWordRequest Predicate { get; init; }

        /// <summary>
        /// Gets the non-predicate constituents of the clause.
        /// </summary>
        public IReadOnlyList<ClauseElement> Elements { get; init; } = [];

        /// <summary>
        /// Gets the valency frame to read the predicate's arguments from, when the verb has more than one.
        /// </summary>
        /// <remarks>
        /// Only needed to disambiguate: jít has a motion frame and a process frame, and they take different
        /// arguments. A verb with a single frame needs nothing here.
        /// </remarks>
        public string? FrameLabel { get; init; }

        /// <summary>
        /// Gets the particle that opens the clause, or null when there is none.
        /// </summary>
        /// <remarks>
        /// Only the clause-initial ones belong here — ať, kéž, nechť, nuže. Like a subordinating conjunction
        /// it fills the first position of its clause, so the clitic cluster follows it: "Ať se umyje". A
        /// particle with scope over a single constituent goes on that constituent instead, in
        /// <see cref="ClauseElement.Particle"/>.
        /// <para>
        /// The builder checks the mood: kéž takes the conditional and ať the imperative, and setting one
        /// against the other is reported rather than rendered.
        /// </para>
        /// </remarks>
        public string? Particle { get; init; }

        /// <summary>
        /// Gets the interjection that opens the sentence, or null when there is none.
        /// </summary>
        /// <remarks>
        /// It stands outside the clause and is set off by a comma — "Ach, Petr se umyl" — so unlike
        /// <see cref="Particle"/> it does not take the clause's first position and the cluster is unaffected.
        /// That is the ÚJČ rule for an interjection that does not stand in for a clause member; one used as
        /// the predicate is a different construction and is not expressed here.
        /// <para>
        /// The terminator is deliberately left alone. An emotional interjection tends towards an exclamation
        /// mark, but tending is not a rule, and <see cref="Terminator"/> is the caller's to set — the same
        /// reason <see cref="SentenceType"/> does not touch it either.
        /// </para>
        /// </remarks>
        public string? Interjection { get; init; }

        /// <summary>
        /// Gets the communicative force of the clause.
        /// </summary>
        /// <remarks>
        /// Governs word order — an interrogative-focus element is fronted — and nothing else. It does not
        /// touch <see cref="Terminator"/>, which the caller sets independently, so a rhetorical question
        /// stays declarative while still ending in a question mark.
        /// </remarks>
        public SentenceType SentenceType { get; init; } = SentenceType.Declarative;

        /// <summary>
        /// Gets the punctuation mark that closes the sentence.
        /// </summary>
        public string Terminator { get; init; } = ".";
    }
}
