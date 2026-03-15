using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Jedna sada tvarů pro konkrétní pád zájmena.
    /// Umožňuje rozlišit příklonky a tvary po předložce.
    /// </summary>
    public sealed record PronounCaseForms
    {
        /// <summary>
        /// Neutrální / nejběžnější tvar (fallback).
        /// </summary>
        public string? Default { get; init; }

        /// <summary>
        /// Tvar po předložce (mně, tobě, něm, ně, nim...).
        /// </summary>
        public string? AfterPreposition { get; init; }

        /// <summary>
        /// Příklonka (mi, ti, ho, mu, ji, je...).
        /// </summary>
        public string? Clitic { get; init; }

        /// <summary>
        /// Knižní / alternativní tvar (mne, tebe...).
        /// </summary>
        public string? Rare { get; init; }
    }

    public sealed record PronounData
    {
        /// <summary>
        /// Sémantický typ zájmena (Personal, Possessive, Demonstrative...).
        /// Říká CO zájmeno je.
        /// </summary>
        public PronounType Type { get; init; }

        /// <summary>
        /// Morfologická třída flexe — říká JAK se zájmeno ohýbá.
        /// Nezávislé na Type.
        /// </summary>
        public InflectionClass InflectionClass { get; init; }

        public Person? Person { get; init; }
        public Number? Number { get; init; }
        public Gender? Gender { get; init; }

        /// <summary>
        /// Pro InflectionClass.Substantive — suppletivní lookup tabulka.
        /// </summary>
        public Dictionary<Case, PronounCaseForms>? FixedForms { get; init; }

        /// <summary>
        /// Pro InflectionClass.PronounHard / PronounSoft — klíč do paradigms.json.
        /// </summary>
        public string? ParadigmId { get; init; }

        /// <summary>
        /// Pro InflectionClass.AdjectiveHard / AdjectiveSoft — klíč do adjectives/patterns.json.
        /// </summary>
        public string? DeclensionPattern { get; init; }

        public string? Prefix { get; init; }
    }
}
