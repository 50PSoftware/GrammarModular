using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Provides Czech numeral operations.
    /// </summary>
    public interface ICzechNumeralService
    {
        /// <summary>
        /// Attempts to resolve a numeral form for the supplied grammatical options.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="grammaticalCase">The grammatical case requested for the generated form.</param>
        /// <param name="gender">The grammatical gender requested for the generated form.</param>
        /// <param name="number">The grammatical number requested for the generated form.</param>
        /// <param name="isAnimate">True when the masculine form is animate; otherwise, false.</param>
        /// <param name="options">The options selecting between competing standard forms.</param>
        /// <returns>The matching form when the numeral has one; otherwise, null.</returns>
        string? TryGetForm(
            string lemma,
            Case grammaticalCase,
            Gender? gender,
            Number? number,
            bool? isAnimate,
            NumeralFormOptions? options);

        /// <summary>
        /// Gets the semantic kind of the numeral.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The numeral type, or <see langword="null"/> when the lemma is unknown.</returns>
        NumeralType? GetNumeralType(string lemma);

        /// <summary>
        /// Gets the inflection class of the numeral.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The numeral morphology, or <see langword="null"/> when the lemma is unknown.</returns>
        NumeralMorphology? GetMorphology(string lemma);

        /// <summary>
        /// Gets what the numeral imposes on the noun it counts.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The agreement the numeral governs with, or None when the lemma is unknown.</returns>
        CardinalAgreement GetAgreement(string lemma);

        /// <summary>
        /// Gets what a numeric value imposes on the noun it counts.
        /// </summary>
        /// <param name="value">The value counted with.</param>
        /// <returns>The agreement the counted noun follows.</returns>
        /// <remarks>
        /// The single source of truth for deriving agreement from a number, shared by the lemma lookup, the
        /// composer, and anything counting with digits rather than words.
        /// </remarks>
        CardinalAgreement GetAgreementForValue(decimal value);

        /// <summary>
        /// Gets the numeric value of the numeral.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The value, or <see langword="null"/> for indefinites and unknown lemmas.</returns>
        decimal? GetValue(string lemma);

        /// <summary>
        /// Determines whether the lemma is a known numeral.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns><see langword="true"/> when the numeral is known; otherwise, <see langword="false"/>.</returns>
        bool IsNumeral(string lemma);

        /// <summary>
        /// Gets the raw data entry for the numeral.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The numeral data, or <see langword="null"/> when the lemma is unknown.</returns>
        NumeralData? GetData(string lemma);
    }
}
