using Grammar.Core.Enums;

namespace Grammar.Core.Models.Valency
{
    /// <summary>
    /// Represents a light verb construction: a verb that contributes little more than tense, a noun
    /// that carries the meaning, and the arguments the two license together.
    /// </summary>
    /// <remarks>
    /// It exists because the valency of such a predicate is not the verb's. <em>Mít</em> governs an
    /// accusative and nothing else, yet <em>mít zájem</em> governs <em>o</em> with the accusative as
    /// well — the argument belongs to <em>zájem</em>, and no frame of <em>mít</em> can be made to
    /// account for it without claiming that every use of the verb takes it.
    /// <para>
    /// The slots are ordinary <see cref="ValencySlot"/> values, so once the construction is recognized
    /// everything downstream treats it as a frame like any other. One of them is the noun itself, under
    /// <see cref="FgdFunctor.CPHR"/>: it is part of the predicate rather than a participant, but it is
    /// still a word that has to be declined and placed.
    /// </para>
    /// </remarks>
    public sealed record ConstructionTemplate
    {
        /// <summary>
        /// Gets the name of the pattern, which is unique and is what a caller names to ask for it.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the lemma of the light verb.
        /// </summary>
        public string LightVerbLemma { get; init; } = string.Empty;

        /// <summary>
        /// Gets the lemma of the noun carrying the meaning, or <see langword="null"/> for a pattern that
        /// is not built on one.
        /// </summary>
        public string? PredicativeNounLemma { get; init; }

        /// <summary>
        /// Gets the slots the construction licenses, the noun among them.
        /// </summary>
        public IReadOnlyList<ValencySlot> Slots { get; init; } = [];

        /// <summary>
        /// Converts the construction into the frame the rest of the pipeline reads.
        /// </summary>
        /// <returns>The frame.</returns>
        /// <remarks>
        /// Named after the pattern, so a clause can point at it the same way it points at a sense of an
        /// ordinary verb, and nothing below has to learn a second way of asking what governs what.
        /// </remarks>
        public ValencyFrame ToFrame() => new()
        {
            VerbLemma = LightVerbLemma,
            FrameLabel = Name,
            Kind = ValencyKind.LightVerb,
            Diathesis = Diathesis.Active,
            Slots = Slots,
        };
    }
}
