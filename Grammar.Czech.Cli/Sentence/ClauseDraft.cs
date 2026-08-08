using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Holds the whole clause as the tool understands it: what it read, what it guessed and what is
    /// still missing.
    /// </summary>
    /// <remarks>
    /// The draft is what the user reviews. It exists because <see cref="CzechClause"/> is the finished
    /// article — it cannot hold an undecided functor or record that a pattern was inferred rather than
    /// looked up, and both are things the review has to show.
    /// </remarks>
    public sealed class ClauseDraft
    {
        /// <summary>
        /// Gets or sets the conjunction that attaches this clause to the one before it, or null when
        /// it is the first.
        /// </summary>
        /// <remarks>
        /// Whether it coordinates or subordinates is not recorded: the conjunction says it, and the
        /// library reads that off the rule data.
        /// </remarks>
        public string? Conjunction { get; set; }

        /// <summary>
        /// Gets or sets the lemma of the predicate.
        /// </summary>
        public string PredicateLemma { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the one-based position the predicate was entered in.
        /// </summary>
        /// <remarks>
        /// Kept so the review can tell a correction aimed at the predicate from one aimed at a
        /// constituent — the predicate takes none of what a constituent takes.
        /// </remarks>
        public int PredicatePosition { get; set; }

        /// <summary>
        /// Gets or sets the word request for the predicate.
        /// </summary>
        public CzechWordRequest Predicate { get; set; }

        /// <summary>
        /// Gets or sets where the predicate's metadata came from.
        /// </summary>
        public MetadataOrigin PredicateOrigin { get; set; }

        /// <summary>
        /// Gets the constituents, in the order the lemmas were entered.
        /// </summary>
        public List<ConstituentDraft> Constituents { get; } = [];

        /// <summary>
        /// Gets or sets the valency frame the arguments are read from, or <see langword="null"/> when the
        /// verb has none in the lexicon.
        /// </summary>
        public ValencyFrame? Frame { get; set; }

        /// <summary>
        /// Gets or sets the frames the verb has under the current diathesis, for a message that has to
        /// list them.
        /// </summary>
        public IReadOnlyList<ValencyFrame> FrameChoices { get; set; } = [];

        /// <summary>
        /// Gets or sets the plan the library completed, which is what the sentence is built from.
        /// </summary>
        /// <remarks>
        /// Kept so that building goes through the same plan the review displayed, rather than through a
        /// second one assembled from the draft afterwards.
        /// </remarks>
        public SentencePlan? Plan { get; set; }

        /// <summary>
        /// Gets or sets the communicative force of the clause.
        /// </summary>
        public SentenceType SentenceType { get; set; } = SentenceType.Declarative;

        /// <summary>
        /// Gets or sets the punctuation mark that closes the sentence.
        /// </summary>
        public string Terminator { get; set; } = ".";

        /// <summary>
        /// Gets the remarks worth showing with the sentence — what was guessed, what the lexicon has
        /// never heard of.
        /// </summary>
        public List<string> Notes { get; } = [];

        /// <summary>
        /// Gets the reasons the draft cannot be built yet, one per unresolved decision.
        /// </summary>
        /// <returns>The open questions, empty when the draft is complete.</returns>
        public IReadOnlyList<string> Gaps()
        {
            var gaps = Constituents
                .Where(constituent => constituent.Functor is null)
                .Select(constituent =>
                    $"U slova '{constituent.Lemma}' (č. {constituent.Position}) není jasná role. "
                    + "Doplň ji přepínačem --role.")
                .ToList();

            // Význam slovesa vybírá slovník, ne kód. Když ho nevybral ani on, je to otázka na uživatele.
            if (Frame is null && FrameChoices.Count > 1)
            {
                gaps.Add(
                    $"Sloveso '{PredicateLemma}' má víc významů a žádný není výchozí: "
                    + $"{string.Join(", ", FrameChoices.Select(frame => frame.FrameLabel ?? "bez popisku"))}. "
                    + "Vyber jeden přepínačem --ramec.");
            }

            return gaps;
        }

        /// <summary>
        /// Converts the draft into the clause the sentence builder consumes.
        /// </summary>
        /// <returns>The clause.</returns>
        /// <exception cref="CliException">Thrown when a decision is still open.</exception>
        public SentencePlan ToPlan() => Plan
            ?? throw new CliException("Návrh ještě neprošel plánovačem, takže z něj větu postavit nejde.");
    }
}
