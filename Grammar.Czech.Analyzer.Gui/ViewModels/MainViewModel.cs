using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grammar.Czech.Analyzer.Candidates;
using Grammar.Czech.Analyzer.Gui.Models;
using Grammar.Czech.Cli.Sentence;
using Grammar.Czech.Interfaces;
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

    public MainViewModel()
    {
        var collection = new ServiceCollection();
        collection.AddCzechGrammarServices(LexiconSettings.DatabasePath());
        _services = collection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        _known = new KnownWords(_services);
        _nounMatcher = new NounMatcher(
            _services.GetRequiredService<CzechNounDeclensionService>(),
            _services.GetRequiredService<INounDataProvider>());
        _adjectiveMatcher = new AdjectiveMatcher(_services.GetRequiredService<CzechAdjectiveDeclensionService>());
        _verbMatcher = new VerbMatcher(_services.GetRequiredService<CzechVerbConjugationService>());
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
        var confirmed = Candidates.Where(row => row.IsSelected).Select(row => row.Candidate).ToList();
        var excluded = Candidates.Where(row => row.IsExcluded).Select(row => row.Candidate).ToList();

        if (confirmed.Count == 0 && excluded.Count == 0)
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

        foreach (var row in Candidates.Where(row => row.IsSelected || row.IsExcluded))
        {
            row.IsSelected = false;
            row.IsExcluded = false;
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

        var handled = CandidateRanking.DropAlreadyHandled(candidates, _known.IsKnown, existingLemmas);
        var ranked = CandidateRanking.Thin(CandidateRanking.DropVowelEndingNounDuplicates(handled, _known.IsKnown), VzoruNaSlovo)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => corpus.GetValueOrDefault(candidate.Lemma))
            .Take(Limit)
            .ToList();

        return ranked.Select(candidate => new CandidateRow(candidate, corpus.GetValueOrDefault(candidate.Lemma))).ToList();
    }
}
