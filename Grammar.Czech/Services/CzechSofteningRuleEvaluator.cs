using Grammar.Core.Enums;
using Grammar.Czech.Enums.Phonology;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    public class CzechSofteningRuleEvaluator : ISofteningRuleEvaluator<CzechWordRequest>
    {
        private readonly List<SofteningRule> rules = new()
        {
            new("žena", WordCategory.Noun, Number.Singular, Case.Dative, req => req.Lemma.EndsWith("ka"), EndingTransformation: "-e", Context: PalatalizationContext.Second),
            new("žena", WordCategory.Noun, Number.Singular, Case.Locative, req => req.Lemma.EndsWith("ka"), EndingTransformation: "-e",Context: PalatalizationContext.Second),

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

        public string? GetEndingTransformation(CzechWordRequest wordRequest, out bool applied)
        {
            var rule = GetMatchingRule(wordRequest);
            applied = rule?.EndingTransformation is not null;
            return rule?.EndingTransformation;
        }

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

        public bool ShouldApplySoftening(CzechWordRequest request, out PalatalizationContext context)
        {
            var rule = GetMatchingRule(request);
            context = rule?.Context ?? PalatalizationContext.First;
            return rule?.ApplySoftening ?? false;
        }
    }
}
