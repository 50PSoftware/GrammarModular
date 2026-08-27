using Grammar.Czech.Analyzer.Candidates;
using Grammar.Czech.Cli.Sentence;

namespace Grammar.Czech.Analyzer
{
    /// <summary>
    /// Feeds ranked candidates into the same <c>navrhy.json</c> queue a live <c>gramatika</c> session
    /// writes to, so <c>lexikon navrhy</c> turns confirmed words into a seed the same way regardless
    /// of whether a person typed them or <c>rozbor</c> found them in a text.
    /// </summary>
    /// <remarks>
    /// Deliberately not calling <see cref="WordProposals.Add"/>: that method takes a single
    /// <see cref="Grammar.Czech.Models.CzechWordRequest"/> and leaves <c>Note</c> empty, which would
    /// throw away exactly what makes a batch-found candidate different from a session-typed one — the
    /// score, the corroborating forms, and whichever other patterns tied for the same word. Building
    /// the <see cref="WordProposal"/> directly and going through the public <c>Read</c>/<c>Write</c>
    /// pair keeps the same file, the same atomic-write behaviour and the same "first sighting wins"
    /// rule, without needing a shared contract change in <c>Grammar.Czech.Cli</c>.
    /// <para>
    /// Every word this class writes gets a <c>Note</c> that says it came from a batch text analysis with
    /// its score — so whoever reviews <c>navrhy.json</c> can tell "I typed this in a session" from "a
    /// pattern search in some text thinks this might be a word" apart, and weigh them differently.
    /// <c>IsConfirmed</c> follows the <c>confirmed</c> argument: a plain CLI run has nobody looking at
    /// the candidates, so it stays <see langword="false"/> and waits for <c>:slova doplnit</c>; the GUI's
    /// checkbox selection is itself the review, so it writes straight through as confirmed.
    /// </para>
    /// </remarks>
    public static class ProposalWriter
    {
        /// <summary>
        /// Writes one proposal per distinct lemma among the ranked candidates, skipping lemmas the
        /// queue already holds under any category.
        /// </summary>
        /// <param name="ranked">The already-thinned, already-ranked candidates.</param>
        /// <param name="store">The proposal queue to append to.</param>
        /// <param name="confirmed">
        /// Whether a person already reviewed these candidates before they got here — the GUI's checkbox
        /// selection counts, a bare CLI batch run does not. Passed straight through to
        /// <see cref="WordProposal.IsConfirmed"/> so a reviewed write skips the review queue instead of
        /// being indistinguishable from an unreviewed guess.
        /// </param>
        /// <returns>How many proposals were newly written.</returns>
        public static int WriteNew(
            IReadOnlyList<MatchCandidate> ranked, WordProposals store, bool confirmed = false)
        {
            var existing = store.Read().ToList();
            var known = new HashSet<string>(
                existing.Select(proposal => proposal.Lemma.ToLowerInvariant()));

            var additions = new List<WordProposal>();

            foreach (var group in ranked.GroupBy(candidate => candidate.Lemma))
            {
                if (!known.Add(group.Key.ToLowerInvariant()))
                {
                    continue;
                }

                var ordered = group.OrderByDescending(candidate => candidate.Score).ToList();
                additions.Add(ToProposal(
                    ordered[0], ordered.Skip(1).Select(candidate => candidate.Pattern).ToList(), confirmed));
            }

            if (additions.Count == 0)
            {
                return 0;
            }

            // Stejný důvod jako u Add: soubor může v tu chvíli psát i běžící gramatika, a druhé psaní
            // má prohrát tiše, ne shodit dávkový běh kvůli jednomu souběhu.
            try
            {
                store.Write(existing.Concat(additions).ToList());
            }
            catch (IOException)
            {
                return 0;
            }

            return additions.Count;
        }

        private static WordProposal ToProposal(
            MatchCandidate primary, IReadOnlyList<string> alternatePatterns, bool confirmed)
        {
            var note = $"Z rozbor (dávkový rozbor textu). Skóre {primary.Score}, "
                + $"tvary v textu: {string.Join(", ", primary.MatchedForms)}.";

            if (alternatePatterns.Count > 0)
            {
                note += $" Další stejně dobré vzory: {string.Join(", ", alternatePatterns)}.";
            }

            return new WordProposal
            {
                Lemma = primary.Lemma,
                Category = primary.Category,
                Gender = primary.Gender,
                Pattern = primary.Pattern,
                IsAnimate = primary.IsAnimate,
                IsConfirmed = confirmed,
                Note = note,
                SeenAt = DateTimeOffset.Now,
            };
        }
    }
}
