using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grammar.Core.Enums;
using Grammar.Czech.Analyzer.Candidates;
using Grammar.Czech.Analyzer.Gui.Models;
using Grammar.Czech.Cli.Sentence;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Analyzer.Gui.ViewModels;

/// <summary>
/// Drives the same generate-and-test pipeline the <c>rozbor</c> CLI runs — tokenize, try every
/// unknown word as a noun/verb/adjective, rank the survivors — but keeps the result in a grid a
/// person can check off, instead of a CSV they read afterwards.
/// </summary>
/// <remarks>
/// No analysis logic lives here: <see cref="NounMatcher"/>, <see cref="VerbMatcher"/>,
/// <see cref="AdjectiveMatcher"/> and <see cref="CandidateRanking"/> are the same classes the CLI
/// calls, referenced straight from <c>Grammar.Czech.Analyzer</c>. This only re-runs the orchestration
/// <c>Program.cs</c> does — top-level statements cannot be called into directly — and adds the
/// UI-facing state (selection, busy flag, status text) a CLI run never needed.
/// </remarks>
public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    private readonly KnownWords _known;
    private readonly NounMatcher _nounMatcher;
    private readonly AdjectiveMatcher _adjectiveMatcher;
    private readonly VerbMatcher _verbMatcher;
    private readonly IReadOnlyDictionary<string, NounPattern> _nounPatterns;
    private readonly IReadOnlyList<string> _nounPatternNames;
    private readonly IReadOnlyList<string> _adjectivePatternNames;

    public MainViewModel()
    {
        var collection = new ServiceCollection();
        collection.AddCzechGrammarServices(LexiconSettings.DatabasePath());
        _services = collection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        _known = new KnownWords(_services);
        var nounDataProvider = _services.GetRequiredService<INounDataProvider>();
        _nounMatcher = new NounMatcher(
            _services.GetRequiredService<CzechNounDeclensionService>(), nounDataProvider);
        _adjectiveMatcher = new AdjectiveMatcher(_services.GetRequiredService<CzechAdjectiveDeclensionService>());
        _verbMatcher = new VerbMatcher(_services.GetRequiredService<CzechVerbConjugationService>());

        _nounPatterns = nounDataProvider.GetPatterns();
        _nounPatternNames = _nounPatterns.Keys.OrderBy(name => name, StringComparer.Ordinal).ToList();
        _adjectivePatternNames = _services.GetRequiredService<IAdjectiveDataProvider>()
            .GetPatterns().Keys.OrderBy(name => name, StringComparer.Ordinal).ToList();
    }

    // Only the patterns a candidate's own category could ever be — offering a verb's patterns on a
    // noun row (or vice versa) would let a correction land on a shape GenerateAndTest never tried for
    // it in the first place.
    private IReadOnlyList<string> AvailablePatterns(WordCategory category) => category switch
    {
        WordCategory.Noun => _nounPatternNames,
        WordCategory.Adjective => _adjectivePatternNames,
        WordCategory.Verb => VerbMatcher.Patterns,
        _ => [],
    };

    // Gender/animacy are implied by a noun pattern, not an independent choice (žena is feminine
    // because it is žena, not because someone also ticked "feminine") — re-deriving them here the same
    // way NounMatcher itself does keeps a corrected candidate as internally consistent as one rozbor
    // found on its own. Adjectives and verbs carry no such per-pattern gender/animacy to re-derive.
    private MatchCandidate Repattern(MatchCandidate candidate, string pattern)
    {
        if (candidate.Category != WordCategory.Noun || !_nounPatterns.TryGetValue(pattern, out var nounPattern))
        {
            return candidate with { Pattern = pattern };
        }

        var (gender, isAnimate) = NounMatcher.ParseGender(nounPattern.Gender);

        return candidate with { Pattern = pattern, Gender = gender, IsAnimate = isAnimate };
    }

    // A category correction is a bigger jump than a pattern one: the old pattern almost never exists
    // under the new category (no "trida3" among noun patterns), so there is nothing sensible to keep —
    // clearing gender/animacy and handing the new category's own first pattern to Repattern leaves the
    // candidate exactly as internally consistent as Recategorize's own noun branch already guarantees
    // for a plain pattern correction.
    private MatchCandidate Recategorize(MatchCandidate candidate, WordCategory category)
    {
        var reset = candidate with { Category = category, Gender = null, IsAnimate = null };
        var patterns = AvailablePatterns(category);

        return patterns.Count > 0 ? Repattern(reset, patterns[0]) : reset;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    public partial string? WikiTitle { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Vyber textový soubor nebo napiš název článku na Wikipedii a klikni na Rozebrat.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial int MinDelka { get; set; } = 4;

    [ObservableProperty]
    public partial int VzoruNaSlovo { get; set; } = 3;

    [ObservableProperty]
    public partial int Limit { get; set; } = 500;

    public ObservableCollection<CandidateRow> Candidates { get; } = [];

    // Exactly one, the same rule --wiki and the positional file argument follow on the CLI — a token
    // ambiguous about its own source is worse than one the button simply refuses to run yet.
    private bool CanAnalyze => !IsBusy && (!string.IsNullOrWhiteSpace(FilePath) != !string.IsNullOrWhiteSpace(WikiTitle));

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        IsBusy = true;
        StatusText = "Rozebírám…";
        Candidates.Clear();

        var fromWiki = !string.IsNullOrWhiteSpace(WikiTitle);

        try
        {
            var rawText = fromWiki
                ? await WikipediaReader.FetchArticleTextAsync(WikiTitle!)
                : await Task.Run(() => DocumentReader.ReadText(FilePath!));

            var rows = await Task.Run(() => Analyze(rawText));

            foreach (var row in rows)
            {
                Candidates.Add(row);
            }

            // WikipediaReader's own licence reminder goes to Console.Error, which nobody sees from a
            // windowed app with no console — the status line is this build's only way to show it.
            StatusText = fromWiki
                ? $"Nalezeno {Candidates.Count} kandidátů. Zdroj: cs.wikipedia.org, článek „{WikiTitle}“ "
                    + "(CC BY-SA) — text se do slovníku nekopíruje, jen se z něj ověřují gramatické tvary."
                : $"Nalezeno {Candidates.Count} kandidátů.";
        }
        catch (Exception exception)
        {
            StatusText = $"Rozbor selhal: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void WriteSelected()
    {
        var written = Candidates.Where(row => row.IsSelected || row.IsExcluded).ToList();
        var confirmed = written.Where(row => row.IsSelected).Select(row => row.Candidate).ToList();
        var excluded = written.Where(row => row.IsExcluded).Select(row => row.Candidate).ToList();

        if (written.Count == 0)
        {
            StatusText = "Nic není zaškrtnuté.";
            return;
        }

        // Jedno úložiště, dva zápisy: druhý přečte soubor znovu, takže vidí i to, co zapsal ten první,
        // a lemma zaškrtnuté v obou sloupcích (což UI samo nedovolí) by nikdy neskončilo dvakrát.
        var store = new WordProposals();
        var addedConfirmed = confirmed.Count > 0 ? ProposalWriter.WriteNew(confirmed, store, confirmed: true) : 0;
        var addedExcluded = excluded.Count > 0 ? ProposalWriter.WriteNew(excluded, store, rejected: true) : 0;

        StatusText = $"Přidáno {addedConfirmed} potvrzených a {addedExcluded} vyloučených do navrhy.json "
            + "(zbytek už tam byl).";

        // Zapsaný řádek zmizí z gridu, ne že by se jen odškrtl — jednou rozhodnuté slovo se v seznamu
        // ke kontrole plete se skutečně novými kandidáty, a stejné lemma navíc příští rozbor stejně
        // sám vynechá (DropAlreadyHandled), takže tu už nemá co dělat.
        foreach (var row in written)
        {
            Candidates.Remove(row);
        }
    }

    // Mirrors Program.cs's loop exactly — same gate order (délka, známost, vlastní jméno), same
    // ShouldTryAsNoun/CandidateRanking pipeline — just returning rows instead of writing a CSV. Takes
    // the already-obtained text directly: acquiring it (file vs. wiki fetch) is AnalyzeAsync's job,
    // not this one's, the same split Program.cs has between reading the input and running the pipeline.
    private List<CandidateRow> Analyze(string rawText)
    {
        var corpus = Tokenizer.CountTokens(rawText);
        var properNouns = Tokenizer.FindLikelyProperNouns(rawText);

        var existingLemmas = new HashSet<string>(
            new WordProposals().Read().Select(proposal => proposal.Lemma.ToLowerInvariant()));

        var candidates = new List<MatchCandidate>();

        foreach (var token in corpus.Keys)
        {
            if (token.Length < MinDelka || _known.IsKnown(token) || properNouns.Contains(token))
            {
                continue;
            }

            var verbCandidates = _verbMatcher.Match(token, corpus, properNouns);
            candidates.AddRange(verbCandidates);

            if (CandidateRanking.ShouldTryAsNoun(token, verbCandidates.Count))
            {
                candidates.AddRange(_nounMatcher.Match(token, corpus, properNouns));
            }

            if (_adjectiveMatcher.Match(token, corpus, properNouns) is { } adjective)
            {
                candidates.Add(adjective);
            }
        }

        var deduplicated = CandidateRanking.DropVowelEndingNounDuplicates(
            CandidateRanking.DropShortFormAdjectiveOrParticiple(
                CandidateRanking.DropRedundantMorePattern(candidates),
                _known.IsKnown),
            _known.IsKnown);
        var handled = CandidateRanking.DropAlreadyHandled(deduplicated, _known.IsKnown, existingLemmas);
        var ranked = CandidateRanking.Thin(handled, VzoruNaSlovo)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => corpus.GetValueOrDefault(candidate.Lemma))
            .Take(Limit)
            .ToList();

        return ranked.Select(candidate => new CandidateRow(
            candidate,
            corpus.GetValueOrDefault(candidate.Lemma),
            AvailablePatterns,
            Repattern,
            Recategorize)).ToList();
    }
}
