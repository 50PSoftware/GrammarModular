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
    public partial string StatusText { get; set; } = "Vyber textový soubor a klikni na Rozebrat.";

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

    private bool CanAnalyze => !IsBusy && !string.IsNullOrWhiteSpace(FilePath);

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        var path = FilePath!;

        IsBusy = true;
        StatusText = "Rozebírám…";
        Candidates.Clear();

        try
        {
            var rows = await Task.Run(() => Analyze(path));

            foreach (var row in rows)
            {
                Candidates.Add(row);
            }

            StatusText = $"Nalezeno {Candidates.Count} kandidátů.";
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
        var selected = Candidates.Where(row => row.IsSelected).Select(row => row.Candidate).ToList();

        if (selected.Count == 0)
        {
            StatusText = "Nic není zaškrtnuté.";
            return;
        }

        var added = ProposalWriter.WriteNew(selected, new WordProposals());
        StatusText = $"Přidáno {added} nových návrhů do navrhy.json (zbytek už tam byl).";

        foreach (var row in Candidates.Where(row => row.IsSelected))
        {
            row.IsSelected = false;
        }
    }

    // Mirrors Program.cs's loop exactly — same gate order (délka, známost, vlastní jméno), same
    // ShouldTryAsNoun/CandidateRanking pipeline — just returning rows instead of writing a CSV.
    private List<CandidateRow> Analyze(string path)
    {
        var rawText = File.ReadAllText(path);
        var corpus = Tokenizer.CountTokens(rawText);
        var properNouns = Tokenizer.FindLikelyProperNouns(rawText);

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

        var ranked = CandidateRanking.Thin(CandidateRanking.DropVowelEndingNounDuplicates(candidates, _known.IsKnown), VzoruNaSlovo)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => corpus.GetValueOrDefault(candidate.Lemma))
            .Take(Limit)
            .ToList();

        return ranked.Select(candidate => new CandidateRow(candidate, corpus.GetValueOrDefault(candidate.Lemma))).ToList();
    }
}
