using Grammar.Core.Enums;
using Grammar.Czech.Enums.Phonology;

namespace Grammar.Czech.Models
{
    /// <summary>
    /// Represents a Czech softening rule and its trigger context.
    /// </summary>
    /// <remarks>
    /// <see cref="CustomPredicate"/> receives both the request and the resolved stem. Rules that key off
    /// the surface shape the ending attaches to must test the stem, because it can differ from the lemma
    /// (nůž → noz-, dům → dom-, otec → otc-). Rules that key off a derivational suffix consumed by the
    /// stem alternation itself must test the lemma instead.
    /// </remarks>
    public sealed record SofteningRule(
        string? Pattern = null,
        WordCategory? Category = null,
        Number? Number = null,
        Case? Case = null,
        Func<CzechWordRequest, string, bool>? CustomPredicate = null,
        bool ApplySoftening = true,
        string? EndingTransformation = null,
        PalatalizationContext Context = PalatalizationContext.First
    );
}
