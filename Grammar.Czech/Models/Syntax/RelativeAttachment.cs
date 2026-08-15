using Grammar.Core.Enums;

namespace Grammar.Czech.Models.Syntax
{
    /// <summary>
    /// Represents a relative clause attached to the constituent it modifies.
    /// </summary>
    /// <remarks>
    /// The relative pronoun looks two ways at once: it agrees with the antecedent in gender, number and
    /// animacy, while its case comes from the role it plays inside the relative clause. That is why
    /// <see cref="Case"/> is stated here and everything else is read off the antecedent —
    /// "muž, kterého jsem viděl" is masculine animate singular from muž, accusative from being the object
    /// of viděl.
    /// </remarks>
    public sealed record RelativeAttachment
    {
        /// <summary>
        /// Gets the lemma of the word introducing the relative clause.
        /// </summary>
        /// <remarks>
        /// A relative pronoun (který, jenž) or a relative adverb (kde, kdy, kam, jak). It was called Pronoun
        /// while only pronouns were supported; adverbs relativize too — "dům, kde bydlím" — and they behave
        /// differently enough that the name had to stop implying one of the two.
        /// </remarks>
        public string Relativizer { get; init; } = "který";

        /// <summary>
        /// Gets the case the relativizer takes from its role inside the relative clause.
        /// </summary>
        /// <remarks>
        /// Ignored for a relative adverb, which is uninflected and has no role a case could express.
        /// </remarks>
        public Case Case { get; init; } = Case.Nominative;

        /// <summary>
        /// Gets the functor of the participant the relativizer possesses, or <see langword="null"/> when
        /// the relativizer stands for a participant itself.
        /// </summary>
        /// <remarks>
        /// Set for jehož, jejíž and jejichž, which are attributes rather than arguments: in <em>žena,
        /// jejíž dům jsem viděl</em> the pronoun modifies <em>dům</em> and holds no slot of its own, so
        /// <see cref="Case"/> says nothing about it.
        /// <para>
        /// These agree in two directions at once, which is what makes them different from every other
        /// relativizer. Gender and number come from the antecedent and decide <em>which</em> of the three
        /// words it is — masculine or neuter singular jehož, feminine singular jejíž, plural jejichž —
        /// while gender, number and case of the form itself come from the noun possessed. jejíž is the
        /// only one where that second agreement shows: the other two are indeclinable.
        /// </para>
        /// </remarks>
        public FgdFunctor? Possessed { get; init; }

        /// <summary>
        /// Gets the relative clause itself, which may be a sentence in its own right.
        /// </summary>
        /// <remarks>
        /// A <see cref="SentenceNode"/>, so a relative clause can coordinate — "muž, který přišel
        /// a odešel" — or carry a dependent clause of its own. A <see cref="CzechClause"/> converts
        /// implicitly, so stating one clause needs nothing extra.
        /// <para>
        /// Where the pronoun is the subject, the antecedent's agreement reaches the opening clause and
        /// everything coordinated with it, because one relative pronoun is the subject of all of them.
        /// It stops at a subordinator: "muž, který přišel, protože se bál" has a clause with a subject
        /// of its own, and forcing the antecedent's gender onto it would be agreement with the wrong
        /// thing.
        /// </para>
        /// </remarks>
        public SentenceNode Clause { get; init; }
    }
}
