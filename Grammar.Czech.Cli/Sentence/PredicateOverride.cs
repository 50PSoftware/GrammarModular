using Grammar.Core.Enums;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Collects what the user stated about one predicate.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="WordOverride"/> because a predicate is not a constituent: it takes
    /// tense and mood where a constituent takes case and role, and the two vocabularies never meet.
    /// <para>
    /// One of these stands for the whole sentence and one for each clause that was singled out. A
    /// clause's own value wins where it has one, so <c>--cas minuly --cas 2=pritomny</c> reads as it
    /// looks: the past throughout, except in the second clause.
    /// </para>
    /// </remarks>
    public sealed class PredicateOverride
    {
        /// <summary>
        /// Gets or sets the tense.
        /// </summary>
        public Tense? Tense { get; set; }

        /// <summary>
        /// Gets or sets the mood.
        /// </summary>
        public Modus? Mood { get; set; }

        /// <summary>
        /// Gets or sets the voice.
        /// </summary>
        public Voice? Voice { get; set; }

        /// <summary>
        /// Gets or sets the aspect.
        /// </summary>
        public VerbAspect? Aspect { get; set; }

        /// <summary>
        /// Gets or sets the person, for a clause with no subject to agree with.
        /// </summary>
        public Person? Person { get; set; }

        /// <summary>
        /// Gets or sets the number, for a clause with no subject to agree with.
        /// </summary>
        public Number? Number { get; set; }

        /// <summary>
        /// Gets or sets the gender, for a clause with no subject to agree with.
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the predicate is negated.
        /// </summary>
        public bool? IsNegative { get; set; }

        /// <summary>
        /// Gets or sets the reflexive type.
        /// </summary>
        public ReflexiveType? ReflexiveType { get; set; }

        /// <summary>
        /// Gets or sets the valency frame to read the arguments from.
        /// </summary>
        public string? FrameLabel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a subject pronoun that adds nothing may be dropped.
        /// </summary>
        public bool? DropSubject { get; set; }

        /// <summary>
        /// Combines this record with the one standing for the whole sentence.
        /// </summary>
        /// <param name="wider">What was said about every clause.</param>
        /// <returns>The combined record, with this one winning wherever it says anything.</returns>
        public PredicateOverride Over(PredicateOverride wider) => new()
        {
            Tense = Tense ?? wider.Tense,
            Mood = Mood ?? wider.Mood,
            Voice = Voice ?? wider.Voice,
            Aspect = Aspect ?? wider.Aspect,
            Person = Person ?? wider.Person,
            Number = Number ?? wider.Number,
            Gender = Gender ?? wider.Gender,
            IsNegative = IsNegative ?? wider.IsNegative,
            ReflexiveType = ReflexiveType ?? wider.ReflexiveType,
            FrameLabel = FrameLabel ?? wider.FrameLabel,
            DropSubject = DropSubject ?? wider.DropSubject,
        };
    }
}
