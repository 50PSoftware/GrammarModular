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

        /// <summary>
        /// Clears every statement, leaving the record as it was built.
        /// </summary>
        public void Forget()
        {
            Tense = null;
            Mood = null;
            Voice = null;
            Aspect = null;
            Person = null;
            Number = null;
            Gender = null;
            IsNegative = null;
            ReflexiveType = null;
            FrameLabel = null;
            DropSubject = null;
        }

        /// <summary>
        /// Describes what the record states, for a session to show what is still in force.
        /// </summary>
        /// <returns>One line per statement, empty when the record says nothing.</returns>
        public IReadOnlyList<string> Describe()
        {
            List<string> lines = [];

            Say("cas", Tense is { } tense ? Terms.Name(tense) : null);
            Say("zpusob", Mood is { } mood ? Terms.Name(mood) : null);
            Say("rod", Voice is { } voice ? Terms.Name(voice) : null);
            Say("vid", Aspect is { } aspect ? Terms.Name(aspect) : null);
            Say("osoba", Person is { } person ? Terms.Name(person) : null);
            Say("cislo", Number is { } number ? Terms.Name(number) : null);
            Say("jmenny-rod", Gender is { } gender ? Terms.Name(gender) : null);
            Say("zapor", IsNegative is { } negative ? (negative ? "ano" : "ne") : null);
            // Zvratnost se zadává částicí a ne názvem typu, tak se tak i vypisuje — 'se' je to, co
            // uživatel napsal, kdežto DerivedReflexive_Se je jméno pro kód.
            Say("zvratne", ReflexiveType switch
            {
                Core.Enums.ReflexiveType.None => "ne",
                Core.Enums.ReflexiveType.DerivedBenefactive_Si or Core.Enums.ReflexiveType.ReflexivumTantum_Si => "si",
                null => null,
                _ => "se",
            });
            Say("ramec", FrameLabel);
            Say("podmet", DropSubject is true ? "vypustit" : null);

            return lines;

            void Say(string name, string? value)
            {
                if (value is not null)
                {
                    lines.Add($"p {name} = {value}");
                }
            }
        }
    }
}
