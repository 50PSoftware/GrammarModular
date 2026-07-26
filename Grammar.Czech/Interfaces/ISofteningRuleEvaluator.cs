using Grammar.Core.Interfaces;
using Grammar.Czech.Enums.Phonology;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for evaluating softening Rule rules.
    /// </summary>
    public interface ISofteningRuleEvaluator<TWord> where TWord : IWordRequest
    {
        /// <summary>
        /// Determines whether Czech softening should apply to the request.
        /// </summary>
        /// <param name="wordRequest">The word request to analyze or inflect.</param>
        /// <param name="stem">The resolved stem the ending will attach to, after mobile-vowel removal and irregular stem overrides.</param>
        /// <param name="context">The palatalization context used to choose the softening target.</param>
        /// <returns>True when the condition is met; otherwise, false.</returns>
        bool ShouldApplySoftening(TWord wordRequest, string stem, out PalatalizationContext context);

        /// <summary>
        /// Gets an ending transformation required by Czech softening rules.
        /// </summary>
        /// <param name="wordRequest">The word request to analyze or inflect.</param>
        /// <param name="stem">The resolved stem the ending will attach to, after mobile-vowel removal and irregular stem overrides.</param>
        /// <param name="applied">The consonant alternation that was applied.</param>
        /// <returns>The transformed ending, or null when no transformation applies.</returns>
        string? GetEndingTransformation(TWord wordRequest, string stem, out bool applied);
    }
}
