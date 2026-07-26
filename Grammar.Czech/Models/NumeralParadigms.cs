using Grammar.Core.Enums;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Specifies the number slots a numeral paradigm table is keyed by.
    /// </summary>
    /// <remarks>
    /// <see cref="Any"/> exists because most numerals do not distinguish number at all: dva is inherently
    /// plural, pět has one set of forms full stop. Storing those under a real number would force every
    /// caller to guess which one the data happened to use.
    /// </remarks>
    public enum NumberSlot
    {
        /// <summary>
        /// Represents the singular value.
        /// </summary>
        Singular,
        /// <summary>
        /// Represents the plural value.
        /// </summary>
        Plural,
        /// <summary>
        /// Represents forms shared by every number.
        /// </summary>
        Any
    }

    /// <summary>
    /// Represents a numeral paradigm as a nested table of number, gender slot, and case.
    /// </summary>
    public sealed record NumeralParadigm
    {
        /// <summary>
        /// Gets the forms grouped by number slot, gender slot, and case.
        /// </summary>
        public Dictionary<NumberSlot, Dictionary<GenderSlot, Dictionary<Case, string>>> Slots { get; init; } = new();
    }
}
