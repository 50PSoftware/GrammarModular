using Grammar.Core.Enums;
using Grammar.Czech.Enums.Phonology;
using Grammar.Czech.Helpers;
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

            new("žena", WordCategory.Noun, Number.Singular, Case.Dative, (req, _) => !req.Lemma.EndsWith("ka") && req.Lemma != "žena"),
            new("žena", WordCategory.Noun, Number.Singular, Case.Locative, (req, _) => !req.Lemma.EndsWith("ka") && req.Lemma != "žena"),

            // Keyed off the lemma on purpose: the mobile e is already gone from the stem (otec → otc-),
            // so there is no -ec left to match by the time the stem reaches us.
            new("muž", WordCategory.Noun, Number.Singular, Case.Vocative,
                (req, _) => req.Lemma?.EndsWith("ec", StringComparison.Ordinal) == true, EndingTransformation: "-e"),

            new("pán", WordCategory.Noun, Number.Plural, Case.Nominative,
    EndsWithK,
    Context: PalatalizationContext.Second),
            new("pán", WordCategory.Noun, Number.Plural, Case.Nominative,
    EndsWithCh,
    Context: PalatalizationContext.First),
            new("pán", WordCategory.Noun, Number.Plural, Case.Vocative,
    EndsWithK,
    Context: PalatalizationContext.Second),
            new("pán", WordCategory.Noun, Number.Plural, Case.Vocative,
    EndsWithCh,
    Context: PalatalizationContext.First),
            new("pán", WordCategory.Noun, Number.Plural, Case.Locative,
    EndsWithK,
    EndingTransformation: "-ích", Context: PalatalizationContext.Second),
            new("pán", WordCategory.Noun, Number.Plural, Case.Locative,
    EndsWithCh,
    EndingTransformation: "-ích", Context: PalatalizationContext.First),

            new("pán", WordCategory.Noun, Number.Singular, Case.Vocative, IsConsonantRStem,
    EndingTransformation: "-e", Context: PalatalizationContext.First),
            new("pán", WordCategory.Noun, Number.Singular, Case.Vocative, IsVelarVocativeStem, EndingTransformation: "-u", ApplySoftening: false)
        };

        /// <summary>
        /// Gets the ending transformation associated with the matching softening rule.
        /// </summary>
        /// <param name="wordRequest">The word request to analyze or inflect.</param>
        /// <param name="stem">The resolved stem the ending will attach to.</param>
        /// <param name="applied">The consonant alternation that was applied.</param>
        /// <returns>The ending transformation from the matching rule, or <see langword="null"/> when no transformation applies.</returns>
        public string? GetEndingTransformation(CzechWordRequest wordRequest, string stem, out bool applied)
        {
            var rule = GetMatchingRule(wordRequest, stem);
            applied = rule?.EndingTransformation is not null;
            return rule?.EndingTransformation;
        }

        // These read the stem, not the lemma: the ending attaches to the stem, and the two diverge
        // whenever an alternation fires (nůž → noz-, dům → dom-, domek → domk-).
        // Ordinal throughout: cs-CZ collation treats a trailing "ch" as a single unit, so a
        // culture-aware EndsWith("h") is false for hoch and EndsWith("ch") is unreliable.
        private static bool EndsWithK(CzechWordRequest req, string stem) => stem.EndsWith("k", StringComparison.Ordinal);

        private static bool EndsWithCh(CzechWordRequest req, string stem) => stem.EndsWith("ch", StringComparison.Ordinal);

        // Velar stems take -u in the vocative sg. with no palatalization. All four velars, per IJP:
        // "jejichž tvarotvorný základ končí na -k, -g, -h, -ch, mají koncovku -u" — vojáku, biologu,
        // vrahu, hochu. The g was missing and gave biologe.
        private static bool IsVelarVocativeStem(CzechWordRequest req, string stem) =>
            EndsWithK(req, stem)
            || EndsWithCh(req, stem)
            || stem.EndsWith("h", StringComparison.Ordinal)
            || stem.EndsWith("g", StringComparison.Ordinal);

        // A consonant before the final r means syllabic r, which palatalizes in the vocative sg.:
        // bratr → bratře, Petr → Petře, ministr → ministře. A vowel before the r keeps the plain
        // -e, which is what latinate agent nouns need: doktore, profesore, Mendominátore.
        private static bool IsConsonantRStem(CzechWordRequest req, string stem) =>
            stem.Length > 1
            && stem.EndsWith("r", StringComparison.Ordinal)
            && MorphologyHelper.IsConsonant(stem[^2]);

        // Feminine žena velar stems undergoing 2nd palatalization in dat/loc sg: k→c, h→z, g→z.
        // ch (moucha) is excluded — it stays on the general 1st-palatalization path (ch→š → mouše).
        // Keyed off the lemma because the test includes the nom.sg. vowel the stem no longer carries,
        // and because the žena stem may still have its derivation suffix detached at this point.
        private static bool IsVelarStem(CzechWordRequest req, string stem) =>
            req.Lemma.EndsWith("ka")
            || (req.Lemma.EndsWith("ha") && !req.Lemma.EndsWith("cha"))
            || req.Lemma.EndsWith("ga");

        private SofteningRule? GetMatchingRule(CzechWordRequest wordRequest, string stem)
        {
            return rules.FirstOrDefault(rule =>
                (rule.Pattern == null || rule.Pattern == wordRequest.Pattern) &&
                (rule.Category == null || rule.Category == wordRequest.WordCategory) &&
                (rule.Number == null || rule.Number == wordRequest.Number) &&
                (rule.Case == null || rule.Case == wordRequest.Case) &&
                (rule.CustomPredicate == null || rule.CustomPredicate(wordRequest, stem))
            );
        }

        /// <summary>
        /// Determines whether a matching rule requires consonant softening.
        /// </summary>
        /// <param name="request">The Czech word request to process.</param>
        /// <param name="stem">The resolved stem the ending will attach to.</param>
        /// <param name="context">The palatalization context used to choose the softening target.</param>
        /// <returns><see langword="true"/> when softening should be applied; otherwise, <see langword="false"/>.</returns>
        public bool ShouldApplySoftening(CzechWordRequest request, string stem, out PalatalizationContext context)
        {
            var rule = GetMatchingRule(request, stem);
            context = rule?.Context ?? PalatalizationContext.First;
            return rule?.ApplySoftening ?? false;
        }
    }
}
