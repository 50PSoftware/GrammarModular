using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents pronoun case forms.
    /// </summary>
    public sealed record PronounCaseForms
    {
        /// <summary>
        /// Gets or sets default.
        /// </summary>
        public string? Default { get; init; }

        /// <summary>
        /// Gets or sets after Preposition.
        /// </summary>
        public string? AfterPreposition { get; init; }

        /// <summary>
        /// Gets or sets clitic.
        /// </summary>
        public string? Clitic { get; init; }

        /// <summary>
        /// Gets or sets rare.
        /// </summary>
        public string? Rare { get; init; }
    }

    /// <summary>
    /// Represents Czech pronoun metadata loaded from JSON data.
    /// </summary>
    public sealed record PronounData
    {
        /// <summary>
        /// Gets or sets the pattern type.
        /// </summary>
        public PronounType Type { get; init; }

        /// <summary>
        /// Gets or sets inflection Class.
        /// </summary>
        public InflectionClass InflectionClass { get; init; }

        /// <summary>
        /// Gets or sets the requested grammatical person.
        /// </summary>
        public Person? Person { get; init; }
        /// <summary>
        /// Gets or sets the requested grammatical number.
        /// </summary>
        public Number? Number { get; init; }
        /// <summary>
        /// Gets or sets the requested grammatical gender.
        /// </summary>
        public Gender? Gender { get; init; }

        /// <summary>
        /// Gets or sets fixed Forms.
        /// </summary>
        public Dictionary<Case, PronounCaseForms>? FixedForms { get; init; }

        /// <summary>
        /// Gets or sets paradigm Id.
        /// </summary>
        public string? ParadigmId { get; init; }

        /// <summary>
        /// Gets or sets declension Pattern.
        /// </summary>
        public string? DeclensionPattern { get; init; }

        /// <summary>
        /// Gets or sets the analyzed prefix.
        /// </summary>
        public string? Prefix { get; init; }

        /// <summary>
        /// Gets the further readings the same pronoun has, or an empty list when it has only this one.
        /// </summary>
        /// <remarks>
        /// Several pronouns are two words wearing one spelling, and the readings differ in
        /// <see cref="Type"/> itself rather than only in shade of meaning:
        /// <list type="bullet">
        /// <item>co asks in "co čteš?" and relativizes in "člověk, co přišel";</item>
        /// <item>kdo asks in "kdo přišel?" and relativizes in "ten, kdo přišel";</item>
        /// <item>jaký asks in "jaký je to dům?" and relativizes in "dům, jaký jsem chtěl".</item>
        /// </list>
        /// <para>
        /// This entry is the primary reading — what <see cref="Interfaces.ICzechPronounService.GetPronounType"/>
        /// returns — and the alternatives hang off it. A caller that knows which construction it is building
        /// asks through <see cref="Interfaces.ICzechPronounService.GetReadings"/> instead, which is what the
        /// word-order resolver does: rendering a relative clause wants the relative reading and does not
        /// care that the bare lookup calls the word interrogative.
        /// </para>
        /// <para>
        /// Nesting rather than a list keyed by lemma keeps the primary reading structurally unambiguous, and
        /// a reading in here carries no alternatives of its own. The same shape
        /// <see cref="ConjunctionData.AlsoReads"/> uses, for the same reason.
        /// </para>
        /// <para>
        /// A reading carries its own <see cref="InflectionClass"/>, and states it even where it repeats the
        /// primary one, because omitting it would not mean "the same" but "Substantive" — the first value of
        /// the enum. It is not always the same: interrogative <em>co</em> declines as <em>co, čeho, čemu</em>,
        /// while relative <em>co</em> is indeclinable and expresses its role through a resumptive pronoun
        /// instead (NESČ, <em>Vztažná věta s jmennou hlavou</em>). Two words wearing one spelling can differ
        /// in how they inflect, not only in what they are for.
        /// </para>
        /// </remarks>
        public IReadOnlyList<PronounData> AlsoReads { get; init; } = [];

        /// <summary>
        /// Gets a value indicating whether the relativizer needs a pronominal head rather than a noun.
        /// </summary>
        /// <remarks>
        /// True for relative <em>kdo</em> alone. NESČ lists <em>který</em>, <em>jenž</em>, <em>jaký</em> and
        /// <em>co</em> as the relativizers of a clause with a nominal head and does not include
        /// <em>kdo</em>: it relativizes an entity rather than a property of a noun, so it wants a
        /// demonstrative to lean on — <em>ten, kdo přišel</em>, not <em>člověk, kdo přišel</em>.
        /// <para>
        /// A flag for one word, like <see cref="ConjunctionData.SecondPosition"/> is for <em>však</em>. The
        /// alternative is the lemma written into a service, and a fact about one word belongs in the file
        /// that lists the words.
        /// </para>
        /// </remarks>
        public bool RequiresPronominalHead { get; init; }
    }
}
