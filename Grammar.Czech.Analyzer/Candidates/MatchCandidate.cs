using Grammar.Core.Enums;

namespace Grammar.Czech.Analyzer.Candidates
{
    /// <summary>
    /// One hypothesis — "this token could be the lemma of this pattern" — together with the evidence
    /// for it: which of the pattern's other generated forms actually turned up in the same text.
    /// </summary>
    /// <param name="Lemma">The hypothesized lemma — the token itself, read as a nominative singular.</param>
    /// <param name="Category">The word class the pattern belongs to.</param>
    /// <param name="Pattern">The declension pattern the hypothesis was tried against.</param>
    /// <param name="Gender">The gender implied by the pattern, or <see langword="null"/> for adjectives, which span all four.</param>
    /// <param name="IsAnimate">The animacy implied by the pattern, when it is meaningful.</param>
    /// <param name="MatchedForms">The distinct generated forms — including the lemma itself — that were found in the text.</param>
    public sealed record MatchCandidate(
        string Lemma,
        WordCategory Category,
        string Pattern,
        Gender? Gender,
        bool? IsAnimate,
        IReadOnlyList<string> MatchedForms)
    {
        /// <summary>
        /// Gets how many distinct forms of this hypothesis were corroborated by the text — the score
        /// a person reads to judge how confident a guess this is.
        /// </summary>
        public int Score => MatchedForms.Count;
    }
}
