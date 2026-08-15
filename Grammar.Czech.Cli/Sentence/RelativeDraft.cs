using Grammar.Core.Enums;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Holds one relative clause as the tool understands it, together with the word that introduces it.
    /// </summary>
    /// <remarks>
    /// A relative clause is not a sibling of the clause it stands next to — it says something about a
    /// thing rather than about the event — so it hangs off a <see cref="ConstituentDraft"/> and not off
    /// <see cref="SentenceDraft.Clauses"/>. What hangs there is a whole <see cref="SentenceDraft"/>,
    /// because a relative clause may coordinate and carry dependent clauses of its own, exactly as
    /// <see cref="Models.Syntax.PlannedRelative"/> allows.
    /// </remarks>
    public sealed class RelativeDraft
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeDraft"/> type.
        /// </summary>
        /// <param name="ordinal">The one-based number of this relative clause in the sentence.</param>
        /// <param name="relativizer">The lemma introducing the clause, as the user wrote it.</param>
        /// <param name="position">The one-based position the relativizer was entered in.</param>
        /// <param name="clause">The clause itself.</param>
        public RelativeDraft(int ordinal, string relativizer, int position, SentenceDraft clause)
        {
            Ordinal = ordinal;
            Relativizer = relativizer;
            Position = position;
            Clause = clause;
        }

        /// <summary>
        /// Gets the one-based number of this relative clause in the sentence, which is how the review
        /// addresses it.
        /// </summary>
        /// <remarks>
        /// Numbered separately from <see cref="ClauseDraft.Ordinal"/> because the two are addressed by
        /// different switches — <c>--pripojit</c> moves a sibling clause, <c>--vztazna</c> moves a
        /// relative one — and sharing one sequence would make each switch skip numbers belonging to the
        /// other.
        /// </remarks>
        public int Ordinal { get; }

        /// <summary>
        /// Gets the lemma introducing the clause: a relative pronoun (který, jenž) or a relative adverb
        /// (kde, kdy).
        /// </summary>
        public string Relativizer { get; }

        /// <summary>
        /// Gets the one-based position the relativizer was entered in.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Gets or sets the case the relative pronoun stands in, or <see langword="null"/> for a relative
        /// adverb, which has none.
        /// </summary>
        /// <remarks>
        /// Worked out from the frame of the relative clause's own verb — the pronoun takes the first slot
        /// nothing else claims — and overridden by <c>--pad</c> on the relativizer. It is the one thing
        /// about the pronoun that does not come from the antecedent, because it looks two ways at once.
        /// </remarks>
        public Case? Case { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the case was worked out from the frame rather than
        /// stated by the user.
        /// </summary>
        public bool CaseIsDerived { get; set; }

        /// <summary>
        /// Gets the clause itself, which is a sentence in its own right.
        /// </summary>
        public SentenceDraft Clause { get; }
    }
}
