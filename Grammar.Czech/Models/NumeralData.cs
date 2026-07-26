using Grammar.Core.Enums;
using Grammar.Czech.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents the competing forms a numeral can take in one case.
    /// </summary>
    /// <remarks>
    /// Several numerals have two standard forms where the norm prefers one — tří over třech, tisíců over
    /// tisíc. <see cref="Default"/> is always the preferred one, so a caller that asks for nothing gets the
    /// form the reference grammar recommends.
    /// </remarks>
    public sealed record NumeralCaseForms
    {
        /// <summary>
        /// Gets the normatively preferred form.
        /// </summary>
        public string? Default { get; init; }

        /// <summary>
        /// Gets the colloquial but still standard doublet: třech, čtyřech.
        /// </summary>
        public string? Colloquial { get; init; }

        /// <summary>
        /// Gets the rare or bookish variant.
        /// </summary>
        public string? Rare { get; init; }

        /// <summary>
        /// Gets the dual form used with paired body parts: s třema rukama, mezi čtyřma očima.
        /// </summary>
        public string? Paired { get; init; }
    }

    /// <summary>
    /// Represents the forms a scale word takes according to what multiplies it.
    /// </summary>
    /// <remarks>
    /// Only sto needs this so far, and it is the reason: two hundred is dvě stě, three and four hundred are
    /// tři and čtyři sta, and five hundred upwards is pět set. The outer key is the multiplier class as a
    /// string — "1", "2", "3" or "5" — because JSON object keys are strings and the classes are ranges
    /// rather than exact values.
    /// </remarks>
    public sealed record NumeralComposite
    {
        /// <summary>
        /// Gets the scale-word forms keyed by multiplier class and then by case.
        /// </summary>
        public Dictionary<string, Dictionary<Case, string>> Forms { get; init; } = new();
    }

    /// <summary>
    /// Represents Czech numeral metadata loaded from JSON data.
    /// </summary>
    public sealed record NumeralData
    {
        /// <summary>
        /// Gets the semantic kind of the numeral.
        /// </summary>
        public NumeralType Type { get; init; }

        /// <summary>
        /// Gets the inflection class, which selects the lookup strategy.
        /// </summary>
        public NumeralMorphology Morphology { get; init; }

        /// <summary>
        /// Gets what the numeral imposes on the noun it counts.
        /// </summary>
        public CardinalAgreement Agreement { get; init; }

        /// <summary>
        /// Gets the numeric value, or <see langword="null"/> for the indefinites (několik, mnoho, kolik).
        /// </summary>
        public decimal? Value { get; init; }

        /// <summary>
        /// Gets the identifier of the shared paradigm table this numeral uses.
        /// </summary>
        public string? ParadigmId { get; init; }

        /// <summary>
        /// Gets the adjective or noun pattern this numeral is declined by, according to its morphology.
        /// </summary>
        public string? DeclensionPattern { get; init; }

        /// <summary>
        /// Gets the lemma to hand the delegated service instead of this numeral's own.
        /// </summary>
        /// <remarks>
        /// The adjective and noun services read the stem off the lemma's ending, so a numeral whose surface
        /// form is not shaped like the pattern it follows has to lend them one that is. The set numerals are
        /// the case in point: dvoje declines exactly like dvojí, but handed dvoje the adjective service
        /// produces dvojeích. The nominative and accusative, where the two genuinely differ, come from
        /// <see cref="Overrides"/>.
        /// </remarks>
        public string? DelegationLemma { get; init; }

        /// <summary>
        /// Gets the gender to decline by when delegating to the noun service.
        /// </summary>
        public Gender? Gender { get; init; }

        /// <summary>
        /// Gets a value indicating whether the numeral is animate when delegating to the noun service.
        /// </summary>
        public bool? IsAnimate { get; init; }

        /// <summary>
        /// Gets the number this numeral is locked to, for the plural-only set numerals (dvoje, patery).
        /// </summary>
        public Number? FixedNumber { get; init; }

        /// <summary>
        /// Gets the cardinal this ordinal is built on, which lets the composer spell out ordinals.
        /// </summary>
        public string? OrdinalOf { get; init; }

        /// <summary>
        /// Gets the forms that override whatever the paradigm or the delegated pattern would produce,
        /// keyed by number ("singular", "plural" or "any") and then by case.
        /// </summary>
        /// <remarks>
        /// This is checked before dispatch, so it doubles as the escape hatch wherever a delegated pattern
        /// gets it wrong — sto is a město but its locative is stu, not stě.
        /// </remarks>
        public Dictionary<string, Dictionary<Case, NumeralCaseForms>>? Overrides { get; init; }

        /// <summary>
        /// Gets the multiplier-dependent forms used when this numeral is a scale word in a compound.
        /// </summary>
        public NumeralComposite? Composite { get; init; }
    }
}
