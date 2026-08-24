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
using Grammar.Czech.Cli.Sentence;
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

var minDelkaOption = new Option<int>("--min-delka")
{
    Description = "Nejkratší token, který se zkouší jako lemma. Krátká slova (2-3 písmena) sedí "
        + "náhodou na spoustu vzorů najednou a zaplavují výstup šumem.",
    DefaultValueFactory = _ => 4,
};

var vzoruNaSlovoOption = new Option<int>("--vzoru-na-slovo")
{
    Description = "Kolik vzorů se stejným (nejlepším) skóre vypsat pro jedno slovo. Slabší vzory "
        + "pro totéž slovo se zahodí, ne jen ty s nižším skóre.",
    DefaultValueFactory = _ => 3,
};

var outOption = new Option<FileInfo>("--out")
{
    Description = "Kam zapsat CSV s kandidáty.",
    DefaultValueFactory = _ => new FileInfo("rozbor_candidates.csv"),
};

var navrhyOption = new Option<FileInfo?>("--navrhy")
{
    Description = "Kam zapsat návrhy vedle CSV. Jinak se bere z GRAMMAR_CZECH_NAVRHY nebo z výchozí "
        + "cesty gramatika (%APPDATA%/gramatika/navrhy.json) — stejný soubor, do kterého píše i živé "
        + "sezení, takže `lexikon navrhy` zpracuje obojí stejně.",
};

var bezNavrhuOption = new Option<bool>("--bez-navrhu")
{
    Description = "Nezapisovat do navrhy.json, jen CSV — pro rozkoukání, než se něco přidá do fronty.",
};

var root = new RootCommand("rozbor — najde v textu slova, která Grammar Modular nezná, a navrhne jim kandidáty na lemma")
{
    textArgument,
    lexiconOption,
    limitOption,
    minDelkaOption,
    vzoruNaSlovoOption,
    outOption,
    navrhyOption,
    bezNavrhuOption,
};

root.SetAction(parse =>
{
    var text = parse.GetValue(textArgument)!;
    var lexicon = parse.GetValue(lexiconOption);
    var limit = parse.GetValue(limitOption);
    var minDelka = parse.GetValue(minDelkaOption);
    var vzoruNaSlovo = parse.GetValue(vzoruNaSlovoOption);
    var @out = parse.GetValue(outOption)!;
    var navrhy = parse.GetValue(navrhyOption);
    var bezNavrhu = parse.GetValue(bezNavrhuOption);

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
    var verbMatcher = new VerbMatcher(
        provider.GetRequiredService<Grammar.Czech.Services.CzechVerbConjugationService>());

    var rawText = File.ReadAllText(text.FullName);
    var corpus = Tokenizer.CountTokens(rawText);
    var properNouns = Tokenizer.FindLikelyProperNouns(rawText);
    Console.Error.WriteLine($"Tokenů v textu (různých): {corpus.Count}, "
        + $"z toho vypadá na vlastní jméno: {properNouns.Count}");

    var candidates = new List<MatchCandidate>();

    foreach (var token in corpus.Keys)
    {
        if (token.Length < minDelka || known.IsKnown(token) || properNouns.Contains(token))
        {
            continue;
        }

        var verbCandidates = verbMatcher.Match(token, corpus, properNouns);
        candidates.AddRange(verbCandidates);

        if (CandidateRanking.ShouldTryAsNoun(token, verbCandidates.Count))
        {
            candidates.AddRange(nounMatcher.Match(token, corpus, properNouns));
        }

        if (adjectiveMatcher.Match(token, corpus, properNouns) is { } adjective)
        {
            candidates.Add(adjective);
        }
    }

    var ranked = CandidateRanking.Thin(CandidateRanking.DropVowelEndingNounDuplicates(candidates, known.IsKnown), vzoruNaSlovo)
        .OrderByDescending(candidate => candidate.Score)
        .ThenByDescending(candidate => corpus.GetValueOrDefault(candidate.Lemma))
        .Take(limit)
        .ToList();

    Reporter.WriteCsv(ranked, corpus, @out.FullName);
    Console.Error.WriteLine($"Zapsáno {ranked.Count} kandidátů do {@out.FullName}");

    if (!bezNavrhu)
    {
        var added = ProposalWriter.WriteNew(ranked, new WordProposals(navrhy?.FullName));
        Console.Error.WriteLine($"Přidáno {added} nových návrhů do navrhy.json "
            + $"(zbytek už tam byl, z tohohle nebo dřívějšího běhu).");
    }
});

return root.Parse(args).Invoke();
