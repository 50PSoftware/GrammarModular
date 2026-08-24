// rozbor — protějšek ke gramatika. Ta skládá větu z lemmat, tenhle jde opačným směrem: vezme český
// text, a pro slova, která tokenizace najde, zkusí generate-and-test nad Grammar.Czech.Services —
// je-li token čtený jako lemma pod nějakým vzorem, souhlasí s ním i jiné pádové/číselné tvary, které
// se v textu taky objevily? Shoda víc než jednoho tvaru je důkaz, ne jen náhoda.
//
// Výstupem jsou kandidáti k ručnímu ověření přes IJP, ne hotová hesla — stejná pozice, jakou dnes má
// WordProposals v gramatika: odhad, dokud se na to člověk nepodívá.

using System.CommandLine;
using Grammar.Czech;
using Grammar.Czech.Analyzer;
using Grammar.Czech.Analyzer.Candidates;
using Grammar.Czech.Models;
using Microsoft.Extensions.DependencyInjection;

var textArgument = new Argument<FileInfo>("text")
{
    Description = "Cesta k textovému souboru (UTF-8), který se má rozebrat.",
};

var lexiconOption = new Option<FileInfo?>("--slovnik")
{
    Description = "Cesta ke slovníku. Jinak se bere z GRAMMAR_CZECH_LEXICON, z lexikon.json, nebo z adresáře aplikace.",
};

var limitOption = new Option<int>("--limit")
{
    Description = "Kolik kandidátů vypsat, seřazeno podle skóre.",
    DefaultValueFactory = _ => 200,
};

var outOption = new Option<FileInfo>("--out")
{
    Description = "Kam zapsat CSV s kandidáty.",
    DefaultValueFactory = _ => new FileInfo("rozbor_candidates.csv"),
};

var root = new RootCommand("rozbor — najde v textu slova, která Grammar Modular nezná, a navrhne jim kandidáty na lemma")
{
    textArgument,
    lexiconOption,
    limitOption,
    outOption,
};

root.SetAction(parse =>
{
    var text = parse.GetValue(textArgument)!;
    var lexicon = parse.GetValue(lexiconOption);
    var limit = parse.GetValue(limitOption);
    var @out = parse.GetValue(outOption)!;

    var lexiconPath = lexicon?.FullName ?? LexiconSettings.DatabasePath();

    var services = new ServiceCollection();
    services.AddCzechGrammarServices(lexiconPath);

    var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

    var known = new KnownWords(provider);
    var nounMatcher = new NounMatcher(
        provider.GetRequiredService<Grammar.Czech.Services.CzechNounDeclensionService>(),
        provider.GetRequiredService<Grammar.Czech.Interfaces.INounDataProvider>());
    var adjectiveMatcher = new AdjectiveMatcher(
        provider.GetRequiredService<Grammar.Czech.Services.CzechAdjectiveDeclensionService>());

    var corpus = Tokenizer.CountTokens(File.ReadAllText(text.FullName));
    Console.Error.WriteLine($"Tokenů v textu (různých): {corpus.Count}");

    var candidates = new List<MatchCandidate>();

    foreach (var token in corpus.Keys)
    {
        if (known.IsKnown(token))
        {
            continue;
        }

        candidates.AddRange(nounMatcher.Match(token, corpus));

        if (adjectiveMatcher.Match(token, corpus) is { } adjective)
        {
            candidates.Add(adjective);
        }
    }

    var ranked = candidates
        .OrderByDescending(candidate => candidate.Score)
        .ThenByDescending(candidate => corpus[candidate.Lemma])
        .Take(limit)
        .ToList();

    Reporter.WriteCsv(ranked, corpus, @out.FullName);
    Console.Error.WriteLine($"Zapsáno {ranked.Count} kandidátů do {@out.FullName}");
});

return root.Parse(args).Invoke();
