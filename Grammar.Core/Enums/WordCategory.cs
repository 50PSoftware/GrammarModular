namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies lexical categories supported by the grammar engine.
    /// </summary>
    /// <remarks>
    /// The ten Czech word classes, in the traditional order. The first five inflect and each has a paradigm
    /// behind it; the last five do not, and for them producing a form means handing back the lemma. They are
    /// members all the same, so that the inventory is complete in the type system and a lexicon entry can be
    /// typed as a preposition or a particle rather than left uncategorized.
    /// <para>
    /// Being a member here is about morphology, not about what the word does in a sentence. The behaviour of
    /// the uninflected classes — case government, comma rules, clause position, clitic ranks — lives in
    /// their own services, which the sentence builder consults directly and which take a lemma rather than a
    /// word request.
    /// </para>
    /// </remarks>
    public enum WordCategory
    {
        /// <summary>
        /// Represents the noun value.
        /// </summary>
        Noun,
        /// <summary>
        /// Represents the adjective value.
        /// </summary>
        Adjective,
        /// <summary>
        /// Represents the pronoun value.
        /// </summary>
        Pronoun,
        /// <summary>
        /// Represents the numerale value.
        /// </summary>
        Numerale,
        /// <summary>
        /// Represents the verb value.
        /// </summary>
        Verb,
        /// <summary>
        /// Represents the adverb value. Uninflected — comparison is the only morphology it has.
        /// </summary>
        Adverb,
        /// <summary>
        /// Represents the preposition value. Uninflected; it governs a case rather than taking one.
        /// </summary>
        Preposition,
        /// <summary>
        /// Represents the conjunction value. Uninflected, with the one exception of aby and kdyby, which
        /// absorb the conditional auxiliary and are built by the conjunction service rather than here.
        /// </summary>
        Conjunction,
        /// <summary>
        /// Represents the particle value — the word class <em>částice</em>, not the clitics.
        /// </summary>
        Particle,
        /// <summary>
        /// Represents the interjection value.
        /// </summary>
        Interjection
    }
}
