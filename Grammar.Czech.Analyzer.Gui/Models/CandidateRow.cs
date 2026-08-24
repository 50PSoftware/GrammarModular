using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grammar.Czech.Analyzer.Candidates;

namespace Grammar.Czech.Analyzer.Gui.Models;

/// <summary>
/// Wraps one <see cref="MatchCandidate"/> with the extra state a row in the review grid needs — the
/// text's own frequency for the lemma, and whether a person has checked it to go into the queue.
/// </summary>
public partial class CandidateRow(MatchCandidate candidate, int frequency) : ObservableObject
{
    /// <summary>
    /// The underlying candidate this row displays.
    /// </summary>
    public MatchCandidate Candidate { get; } = candidate;

    /// <summary>
    /// Whether a person has checked this row to be added to the proposal queue.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public string Lemma => Candidate.Lemma;

    public string Category => Candidate.Category.ToString();

    public string Pattern => Candidate.Pattern;

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
