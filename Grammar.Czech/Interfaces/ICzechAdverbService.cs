using Grammar.Core.Models.Word;
using Grammar.Czech.Models;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for producing Czech adverb forms.
    /// </summary>
    public interface ICzechAdverbService
    {
        /// <summary>
        /// Builds the requested adverb form.
        /// </summary>
        /// <param name="request">The Czech word request to process.</param>
        /// <returns>The generated adverb form.</returns>
        WordForm GetForm(CzechWordRequest request);

        /// <summary>
        /// Determines whether the adverb is compared.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns><see langword="true"/> when a comparative is registered for the lemma; otherwise, <see langword="false"/>.</returns>
        bool IsComparable(string lemma);

        /// <summary>
        /// Gets every comparative usage accepts for the adverb, the generated one first.
        /// </summary>
        /// <param name="lemma">The dictionary form to look up.</param>
        /// <returns>The comparatives, or an empty sequence when the adverb is unregistered or not compared.</returns>
        /// <remarks>
        /// Generation has to pick one form, but several adverbs have more than one correct comparative —
        /// snadno takes snáze and snadněji alike. This is how a caller sees the others.
        /// </remarks>
        IReadOnlyList<string> GetComparativeVariants(string lemma);

        /// <summary>
        /// Gets the adverbs derived from the supplied adjective.
        /// </summary>
        /// <param name="adjectiveLemma">The adjective lemma.</param>
        /// <returns>The adverbs derived from it, or an empty sequence when none is registered.</returns>
        /// <remarks>
        /// More than one can come back: dlouhý yields dlouho and dlouze, vysoký yields vysoko and vysoce.
        /// Which of them a sentence wants is a lexical choice, not something the pair itself settles.
        /// </remarks>
        IReadOnlyList<string> GetAdverbsFor(string adjectiveLemma);
    }
}
