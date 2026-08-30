using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grammar.Core.Enums;
using Grammar.Czech.Analyzer.Candidates;

namespace Grammar.Czech.Analyzer.Gui.Models;

/// <summary>
/// Wraps one <see cref="MatchCandidate"/> with the extra state a row in the review grid needs — the
/// text's own frequency for the lemma, whether a person has checked it as confirmed or as an
/// exception, and the category/pattern a person can correct before either of those is written.
/// </summary>
/// <param name="candidate">The candidate this row starts out displaying.</param>
/// <param name="frequency">The text's own frequency for the lemma.</param>
/// <param name="availablePatternsFor">
/// Every pattern a given category actually has — what the pattern combo box offers, re-evaluated
/// whenever <see cref="Category"/> changes, so a correction can only ever land on a pattern the
/// lexicon recognizes for whichever category is currently selected.
/// </param>
/// <param name="repattern">
/// Rebuilds <see cref="Candidate"/> for a newly chosen pattern — re-deriving gender/animacy for a
/// noun, since those are implied by the pattern rather than independent choices; passed in rather
/// than resolved here so this row does not need its own reference to the pattern providers.
/// </param>
/// <param name="recategorize">
/// Rebuilds <see cref="Candidate"/> for a newly chosen category — clearing gender/animacy (only
/// meaningful for a noun) and resetting the pattern to the new category's own first one, via
/// <paramref name="repattern"/>, so the row is never left holding a pattern its new category does
/// not even have.
/// </param>
public partial class CandidateRow(
    MatchCandidate candidate,
    int frequency,
    Func<WordCategory, IReadOnlyList<string>> availablePatternsFor,
    Func<MatchCandidate, string, MatchCandidate> repattern,
    Func<MatchCandidate, WordCategory, MatchCandidate> recategorize) : ObservableObject
{
    /// <summary>
    /// The three categories <c>rozbor</c> actually tries — the only ones it makes sense to correct a
    /// candidate into, since nothing here ever generates a pronoun/numeral/etc. candidate to begin with.
    /// </summary>
    public static IReadOnlyList<WordCategory> AvailableCategories { get; } =
        [WordCategory.Noun, WordCategory.Adjective, WordCategory.Verb];

    /// <summary>
    /// The underlying candidate this row displays — its <c>Category</c>/<c>Pattern</c>/<c>Gender</c>/
    /// <c>IsAnimate</c> follow whatever was last chosen in <see cref="Category"/>/<see cref="Pattern"/>,
    /// not necessarily what <c>rozbor</c> originally guessed.
    /// </summary>
    public MatchCandidate Candidate { get; private set; } = candidate;

    /// <summary>
    /// Every pattern the currently selected <see cref="Category"/> has, for the pattern combo box.
    /// </summary>
    public IReadOnlyList<string> AvailablePatterns { get; private set; } = availablePatternsFor(candidate.Category);

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
    /// The category a person can pick from <see cref="AvailableCategories"/> to correct a candidate
    /// <c>rozbor</c> put in the wrong word class entirely — a verb mistaken for a noun, say.
    /// </summary>
    [ObservableProperty]
    public partial WordCategory Category { get; set; } = candidate.Category;

    partial void OnCategoryChanged(WordCategory value)
    {
        Candidate = recategorize(Candidate, value);
        AvailablePatterns = availablePatternsFor(value);
        OnPropertyChanged(nameof(AvailablePatterns));
        Pattern = Candidate.Pattern;
    }

    /// <summary>
    /// The pattern a person can pick from <see cref="AvailablePatterns"/> to correct a wrong guess
    /// before writing the row — rozbor's own guess to start with.
    /// </summary>
    [ObservableProperty]
    public partial string Pattern { get; set; } = candidate.Pattern;

    partial void OnPatternChanged(string value) => Candidate = repattern(Candidate, value);

    public string Lemma => Candidate.Lemma;

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
