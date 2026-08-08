using Grammar.Core.Enums;
using Grammar.Czech.Models;

namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents what is to be said, before any decision about how Czech says it.
    /// </summary>
    /// <remarks>
    /// The input to the pipeline. It names the event, the participants and the communicative intent;
    /// everything grammatical — which sense of the verb, which case each participant stands in, whether
    /// the subject is expressed at all, what order any of it comes in — is worked out from here down.
    /// <para>
    /// The predicate is a <see cref="CzechWordRequest"/> rather than a bare lemma because tense, mood
    /// and aspect are part of what is being said, not of how it is said. What is left unset there is
    /// the planner's to fill.
    /// </para>
    /// </remarks>
    public sealed record SentencePlan
    {
        /// <summary>
        /// Gets the word request for the predicate.
        /// </summary>
        public CzechWordRequest Predicate { get; init; }

        /// <summary>
        /// Gets the participants, in the order they were given.
        /// </summary>
        /// <remarks>
        /// The order carries no grammar — Czech word order is pragmatic and comes out of
        /// <see cref="InformationStatus"/> — but it does break ties: which participant gets the
        /// unmarked theme, and which slot an unlabelled participant is matched to.
        /// </remarks>
        public IReadOnlyList<PlannedParticipant> Participants { get; init; } = [];

        /// <summary>
        /// Gets the sense of the verb to use, when the dictionary holds more than one.
        /// </summary>
        public string? FrameLabel { get; init; }

        /// <summary>
        /// Gets the participant to make the subject, or <see langword="null"/> for the unmarked one.
        /// </summary>
        /// <remarks>
        /// This is what selects a diathesis. Naming <see cref="FgdFunctor.PAT"/> asks for the patient to
        /// be the subject, which in Czech is the periphrastic passive — <em>kniha byla dána</em> — and
        /// the planner reaches for the frame that states it rather than recomputing the active one.
        /// <para>
        /// It is stated as a perspective rather than as a voice because that is the communicative fact:
        /// the speaker chooses what the sentence is about, and the voice follows from it.
        /// </para>
        /// </remarks>
        public FgdFunctor? Perspective { get; init; }

        /// <summary>
        /// Gets a value indicating whether a subject pronoun that adds nothing may be dropped.
        /// </summary>
        /// <remarks>
        /// On by default, because Czech drops it by default: the ending already carries the person, and
        /// <em>já čtu</em> against <em>čtu</em> is emphasis rather than the neutral sentence. Turning it
        /// off keeps the pronoun, which is what a contrastive reading needs.
        /// </remarks>
        public bool AllowSubjectDrop { get; init; } = true;

        /// <summary>
        /// Gets the communicative force of the sentence.
        /// </summary>
        public SentenceType SentenceType { get; init; } = SentenceType.Declarative;

        /// <summary>
        /// Gets the punctuation mark that closes the sentence.
        /// </summary>
        public string Terminator { get; init; } = ".";

        /// <summary>
        /// Gets the particle that opens the clause, or null when there is none.
        /// </summary>
        public string? Particle { get; init; }

        /// <summary>
        /// Gets the interjection that opens the sentence, or null when there is none.
        /// </summary>
        public string? Interjection { get; init; }
    }
}
