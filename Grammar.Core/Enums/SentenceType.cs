namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies the communicative force of a sentence, independent of its punctuation.
    /// </summary>
    /// <remarks>
    /// The force and the closing mark are kept apart on purpose: a rhetorical question is declarative and
    /// still ends in a question mark, so nothing here touches the clause's terminator. The caller sets that.
    /// <para>
    /// Scope: one fronted interrogative element per clause. Multiple wh-words ("Kdo komu co dal?"), echo
    /// questions, and the interrogative discourse particles (copak, viď) are not covered — the last of these
    /// would be data work, since particles.json holds no discourse particles at all.
    /// </para>
    /// </remarks>
    public enum SentenceType
    {
        /// <summary>
        /// A statement. The default; word order follows functional sentence perspective alone.
        /// </summary>
        Declarative,

        /// <summary>
        /// A question. Combine with <see cref="InformationStatus.Interrogative"/> on exactly one element
        /// for a wh-question; leave it unused for a yes/no question, where Czech marks the question by
        /// intonation and punctuation rather than by word order.
        /// </summary>
        Interrogative
    }
}
