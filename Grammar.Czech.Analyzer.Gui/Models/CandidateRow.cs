using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grammar.Czech.Analyzer.Candidates;

namespace Grammar.Czech.Analyzer.Gui.Models;

/// <summary>
/// Wraps one <see cref="MatchCandidate"/> with the extra state a row in the review grid needs — the
/// text's own frequency for the lemma, whether a person has checked it as confirmed or as an
/// exception, and the pattern a person can correct before either of those is written.
/// </summary>
/// <param name="candidate">The candidate this row starts out displaying.</param>
/// <param name="frequency">The text's own frequency for the lemma.</param>
/// <param name="availablePatterns">
/// Every pattern this row's own category actually has — what the pattern combo box offers, so a
/// correction can only ever land on a pattern the lexicon recognizes.
/// </param>
/// <param name="repattern">
/// Rebuilds <see cref="Candidate"/> for a newly chosen pattern — re-deriving gender/animacy for a
/// noun, since those are implied by the pattern rather than independent choices; passed in rather
/// than resolved here so this row does not need its own reference to the pattern providers.
/// </param>
public partial class CandidateRow(
    MatchCandidate candidate,
    int frequency,
    IReadOnlyList<string> availablePatterns,
    Func<MatchCandidate, string, MatchCandidate> repattern) : ObservableObject
{
    /// <summary>
    /// The underlying candidate this row displays — its <c>Pattern</c>/<c>Gender</c>/<c>IsAnimate</c>
    /// follow whatever was last chosen in <see cref="Pattern"/>, not necessarily what <c>rozbor</c>
    /// originally guessed.
    /// </summary>
    public MatchCandidate Candidate { get; private set; } = candidate;

    /// <summary>
    /// Every pattern this row's own category has, for the pattern combo box.
    /// </summary>
    public IReadOnlyList<string> AvailablePatterns { get; } = availablePatterns;

    /// <summary>
    /// Whether a person has checked this row to be added to the proposal queue as confirmed.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Whether a person has checked this row as an exception — wrong, and never to be proposed again.
    /// Mutually exclusive with <see cref="IsSelected"/>: a row cannot be both confirmed and rejected.
    /// </summary>
    [ObservableProperty]
    public partial bool IsExcluded { get; set; }

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
        {
            IsExcluded = false;
        }
    }

    partial void OnIsExcludedChanged(bool value)
    {
        if (value)
        {
            IsSelected = false;
        }
    }

    /// <summary>
    /// The pattern a person can pick from <see cref="AvailablePatterns"/> to correct a wrong guess
    /// before writing the row — rozbor's own guess to start with.
    /// </summary>
    [ObservableProperty]
    public partial string Pattern { get; set; } = candidate.Pattern;

    partial void OnPatternChanged(string value) => Candidate = repattern(Candidate, value);

    public string Lemma => Candidate.Lemma;

    public string Category => Candidate.Category.ToString();

    public int Score => Candidate.Score;

    /// <summary>
    /// How often the lemma's own spelling turns up in the text — 0 for a hypothesis reconstructed from
    /// some other case or tense that was never itself written down.
    /// </summary>
    public int Frequency { get; } = frequency;

    public string MatchedForms => string.Join(" ", Candidate.MatchedForms);

    public string IjpUrl => $"https://prirucka.ujc.cas.cz/?slovo={Uri.EscapeDataString(Candidate.Lemma)}";

    // UseShellExecute delegates to the OS's own "open URL" handler — xdg-open on Linux, the shell
    // association on Windows — which is what makes this work unchanged on both.
    [RelayCommand]
    private void OpenIjp() => Process.Start(new ProcessStartInfo(IjpUrl) { UseShellExecute = true });
}
