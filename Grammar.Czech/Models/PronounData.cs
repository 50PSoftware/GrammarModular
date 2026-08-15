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
        /// A reading states its <see cref="Type"/> and nothing else, because it is the same word declined the
        /// same way — <em>co, čeho, čemu</em> whether it asks or relativizes. Form lookup therefore always
        /// goes through the primary entry, and repeating the paradigm here would only create somewhere for
        /// the two copies to disagree.
        /// </para>
        /// </remarks>
        public IReadOnlyList<PronounData> AlsoReads { get; init; } = [];
    }
}
