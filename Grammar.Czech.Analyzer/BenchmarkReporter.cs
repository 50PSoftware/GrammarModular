using Grammar.Czech.Cli.Sentence;

namespace Grammar.Czech.Analyzer
{
    /// <summary>
    /// Summarizes how many of <c>rozbor</c>'s own proposals in <c>navrhy.json</c> a person went on to
    /// confirm or reject — its own track record, measured against the same review a human already does.
    /// </summary>
    /// <remarks>
    /// Only ever counts decided proposals toward the success rate: a proposal nobody has looked at yet
    /// is neither right nor wrong, just unreviewed, and folding it into either bucket would make the
    /// number lie about which one it actually is — an unreviewed backlog would drag a genuinely accurate
    /// batch's score down for no reason. See <see cref="WordProposal.IsRejected"/>'s own remarks for why
    /// that distinction exists at all: before it did, "not confirmed" covered both cases and this report
    /// could not have been built honestly.
    /// </remarks>
    public static class BenchmarkReporter
    {
        // What ProposalWriter.ToProposal's own Note always starts with — the one thing that tells a
        // rozbor-found proposal from one a live gramatika session collected, since both share the same
        // file and the same IsConfirmed/IsRejected shape.
        internal const string SourceMarker = "Z rozbor";

        /// <summary>
        /// Summarizes the subset of <paramref name="proposals"/> that came from <c>rozbor</c>.
        /// </summary>
        /// <param name="proposals">The full contents of a <c>navrhy.json</c> file.</param>
        /// <returns>The confirmed/rejected/undecided counts among rozbor's own proposals.</returns>
        public static BenchmarkResult Summarize(IReadOnlyList<WordProposal> proposals)
        {
            var ownProposals = proposals
                .Where(proposal => proposal.Note?.StartsWith(SourceMarker, StringComparison.Ordinal) == true)
                .ToList();

            var confirmed = ownProposals.Count(proposal => proposal.IsConfirmed);
            var rejected = ownProposals.Count(proposal => proposal.IsRejected);
            var undecided = ownProposals.Count - confirmed - rejected;

            return new BenchmarkResult(confirmed, rejected, undecided);
        }
    }

    /// <summary>
    /// The confirmed/rejected/undecided counts among <c>rozbor</c>'s own proposals in one
    /// <c>navrhy.json</c> file.
    /// </summary>
    /// <param name="Confirmed">How many were confirmed as real, correctly-described words.</param>
    /// <param name="Rejected">How many were reviewed and turned down.</param>
    /// <param name="Undecided">How many nobody has reviewed yet.</param>
    public sealed record BenchmarkResult(int Confirmed, int Rejected, int Undecided)
    {
        /// <summary>
        /// Gets how many proposals a person has actually reached a verdict on.
        /// </summary>
        public int Decided => Confirmed + Rejected;

        /// <summary>
        /// Gets the share of decided proposals that were confirmed, or <see langword="null"/> when
        /// nothing has been decided yet.
        /// </summary>
        public double? SuccessRate => Decided == 0 ? null : (double)Confirmed / Decided;
    }
}
