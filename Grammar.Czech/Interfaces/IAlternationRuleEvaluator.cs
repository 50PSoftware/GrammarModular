using Grammar.Core.Interfaces;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for evaluating alternation Rule rules.
    /// </summary>
    public interface IAlternationRuleEvaluator<TWord> where TWord : IWordRequest
    {
        /// <summary>
        /// Determines whether the stem shortens its long vowel before the genitive plural ending.
        /// </summary>
        /// <param name="stem">The stem to transform.</param>
        /// <param name="wordRequest">The word request to analyze or inflect.</param>
        /// <returns>True when the condition is met; otherwise, false.</returns>
        bool ShouldShortenStem(string stem, TWord wordRequest);
    }
}
