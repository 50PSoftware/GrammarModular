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
    }
}
