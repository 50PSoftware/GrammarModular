using Grammar.Core.Enums;
using Grammar.Czech.Enums.Phonology;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Evaluates Czech Softening Rule Evaluator rules.
    /// </summary>
    public class CzechSofteningRuleEvaluator : ISofteningRuleEvaluator<CzechWordRequest>
    {
        private readonly List<SofteningRule> rules = new()
        {
            new("žena", WordCategory.Noun, Number.Singular, Case.Dative, IsVelarStem, EndingTransformation: "-e", Context: PalatalizationContext.Second),
            new("žena", WordCategory.Noun, Number.Singular, Case.Locative, IsVelarStem, EndingTransformation: "-e", Context: PalatalizationContext.Second),

            new("žena", WordCategory.Noun, Number.Singular, Case.Dative, req => !req.Lemma.EndsWith("ka") && req.Lemma != "žena"),
            new("žena", WordCategory.Noun, Number.Singular, Case.Locative, req => !req.Lemma.EndsWith("ka") && req.Lemma != "žena"),

            new("muž", WordCategory.Noun, Number.Singular, Case.Vocative,
                req => req.Lemma?.EndsWith("ec") == true, EndingTransformation: "-e"),

            new("pán", WordCategory.Noun, Number.Plural, Case.Nominative,
    req => req.Lemma.EndsWith("k"),
    Context: PalatalizationContext.Second),
            new("pán", WordCategory.Noun, Number.Plural, Case.Nominative,
    req => req.Lemma.EndsWith("ch"),
    Context: PalatalizationContext.First),
            new("pán", WordCategory.Noun, Number.Plural, Case.Vocative,
    req => req.Lemma.EndsWith("k"),
    Context: PalatalizationContext.Second),
            new("pán", WordCategory.Noun, Number.Plural, Case.Vocative,
    req => req.Lemma.EndsWith("ch"),
    Context: PalatalizationContext.First),
            new("pán", WordCategory.Noun, Number.Plural, Case.Locative,
    req => req.Lemma.EndsWith("k"),
    EndingTransformation: "-ích", Context: PalatalizationContext.Second),
            new("pán", WordCategory.Noun, Number.Plural, Case.Locative,
    req => req.Lemma.EndsWith("ch"),
    EndingTransformation: "-ích", Context: PalatalizationContext.First),
            new("pán", WordCategory.Noun, Number.Singular, Case.Vocative, req => req.Lemma.EndsWith("k") || req.Lemma.EndsWith("ch"), EndingTransformation: "-u", ApplySoftening: false)
        };

        /// <summary>
        /// Gets the ending transformation associated with the matching softening rule.
        /// </summary>
        /// <param name="wordRequest">The word request to analyze or inflect.</param>
        /// <param name="applied">The consonant alternation that was applied.</param>
        /// <returns>The ending transformation from the matching rule, or <see langword="null"/> when no transformation applies.</returns>
        public string? GetEndingTransformation(CzechWordRequest wordRequest, out bool applied)
        {
            var rule = GetMatchingRule(wordRequest);
            applied = rule?.EndingTransformation is not null;
            return rule?.EndingTransformation;
        }

        // Feminine žena velar stems undergoing 2nd palatalization in dat/loc sg: k→c, h→z, g→z.
        // ch (moucha) is excluded — it stays on the general 1st-palatalization path (ch→š → mouše).
        private static bool IsVelarStem(CzechWordRequest req) =>
            req.Lemma.EndsWith("ka")
            || (req.Lemma.EndsWith("ha") && !req.Lemma.EndsWith("cha"))
            || req.Lemma.EndsWith("ga");

        private SofteningRule? GetMatchingRule(CzechWordRequest wordRequest)
        {
            return rules.FirstOrDefault(rule =>
                (rule.Pattern == null || rule.Pattern == wordRequest.Pattern) &&
                (rule.Category == null || rule.Category == wordRequest.WordCategory) &&
                (rule.Number == null || rule.Number == wordRequest.Number) &&
                (rule.Case == null || rule.Case == wordRequest.Case) &&
                (rule.CustomPredicate == null || rule.CustomPredicate(wordRequest))
            );
        }

        /// <summary>
        /// Determines whether a matching rule requires consonant softening.
        /// </summary>
        /// <param name="request">The Czech word request to process.</param>
        /// <param name="context">The palatalization context used to choose the softening target.</param>
        /// <returns><see langword="true"/> when softening should be applied; otherwise, <see langword="false"/>.</returns>
        public bool ShouldApplySoftening(CzechWordRequest request, out PalatalizationContext context)
        {
            var rule = GetMatchingRule(request);
            context = rule?.Context ?? PalatalizationContext.First;
            return rule?.ApplySoftening ?? false;
        }
    }
}
