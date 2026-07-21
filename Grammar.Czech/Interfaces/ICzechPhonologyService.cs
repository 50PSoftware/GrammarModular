using Grammar.Core.Interfaces;
using Grammar.Czech.Enums.Phonology;
using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines Czech-specific phonology operations.
    /// </summary>
    public interface ICzechPhonologyService : IPhonologyService<CzechWordRequest>
    {
        /// <summary>
        /// Applies Czech consonant softening for the supplied palatalization context.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="context">The palatalization context used to choose the softening target.</param>
        /// <returns>The stem after context-sensitive consonant softening has been applied.</returns>
        string ApplySoftening(string stem, PalatalizationContext context);

        /// <summary>
        /// Applies orthographic consonant softening before a soft ending, leaving d/t/n unchanged.
        /// Their palatalization is purely phonetic and is carried by the following <c>ě</c> digraph,
        /// so the grapheme must stay (<c>dě/tě/ně</c>, not <c>ďe/ťe/ňe</c>). All other consonants
        /// (r→ř, velar 2nd palatalization, …) are softened via <see cref="ApplySoftening(string, PalatalizationContext)"/>.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="context">The palatalization context used to choose the softening target.</param>
        /// <returns>The stem after orthographic consonant softening has been applied.</returns>
        string ApplyOrthographicSoftening(string stem, PalatalizationContext context);
    }
}
