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
        /// Gets the diathesis to build the clause in, or <see langword="null"/> to let
        /// <see cref="Perspective"/> decide.
        /// </summary>
        /// <remarks>
        /// Czech has five diatheses and a perspective can only name two of them. Saying PAT is the
        /// subject asks for the periphrastic passive and says everything there is to say about it; the
        /// deagentive <em>pracovalo se</em> and the dispositional <em>pracovalo se mi dobře</em> have no
        /// subject at all, so there is no participant to point at and the diathesis has to be named
        /// outright.
        /// <para>
        /// Stated separately rather than folded into <see cref="Perspective"/> so that the plans that
        /// worked before keep working: a perspective still selects the passive on its own, and this is
        /// only consulted where it says something. Naming both a perspective and a diathesis that
        /// disagree is refused rather than silently resolved.
        /// </para>
        /// <para>
        /// It is not a voice. The deagentive and the dispositional are built on an active verb form with
        /// the reflexive particle, so <see cref="Grammar.Core.Enums.Voice"/> stays active for both; what
        /// changes is which frame the arguments come from.
        /// </para>
        /// </remarks>
        public Diathesis? Diathesis { get; init; }

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

        /// <summary>
        /// Gets the clauses joined to this one, in the order they follow it.
        /// </summary>
        /// <remarks>
        /// Whether each one is coordinated or subordinated is not stated here: the conjunction says it,
        /// and conjunctions are a closed class the rule data enumerates. Naming both would let a caller
        /// write a contradiction the grammar has no reading for.
        /// </remarks>
        public IReadOnlyList<ClauseLink> Joined { get; init; } = [];
    }

    /// <summary>
    /// Represents one clause joined to another by a conjunction.
    /// </summary>
    /// <param name="Conjunction">The conjunction joining them, which also says how.</param>
    /// <param name="Clause">The clause being joined, itself a whole plan.</param>
    /// <param name="RequiresComma">
    /// Overrides the conjunction's default comma rule, for the conjunctions punctuated by the relation
    /// between the clauses rather than by the word — nebo and či.
    /// </param>
    /// <param name="Paired">
    /// Asks for the split construction: buď … nebo, ani … ani, nejen … ale i. Asked for rather than
    /// inferred, because the same conjunction serves both and only the caller knows which was meant.
    /// </param>
    public sealed record ClauseLink(
        string Conjunction,
        SentencePlan Clause,
        bool? RequiresComma = null,
        bool Paired = false);
}
