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

var textArgument = new Argument<FileInfo?>("text")
{
    Description = "Cesta k souboru (UTF-8 .txt, .docx nebo .odt), který se má rozebrat. "
        + "Vynech, pokud používáš --wiki.",
    Arity = ArgumentArity.ZeroOrOne,
};

var wikiOption = new Option<string?>("--wiki")
{
    Description = "Název článku na cs.wikipedia.org ke stažení a rozboru, místo souboru. "
        + "Text se do slovníku nekopíruje, jen se z něj ověřují gramatické tvary.",
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

var benchmarkOption = new Option<bool>("--benchmark")
{
    Description = "Místo rozboru vypíše, kolik z dosavadních návrhů rozboru v navrhy.json bylo "
        + "potvrzeno a kolik zamítnuto — vlastní úspěšnost nástroje, ne rozbor textu. "
        + "Nepotřebuje ani soubor, ani --wiki.",
};

var root = new RootCommand("rozbor — najde v textu slova, která Grammar Modular nezná, a navrhne jim kandidáty na lemma")
{
    textArgument,
    wikiOption,
    lexiconOption,
    limitOption,
    minDelkaOption,
    vzoruNaSlovoOption,
    outOption,
    navrhyOption,
    bezNavrhuOption,
    benchmarkOption,
};

root.SetAction(async (parse, cancellationToken) =>
{
    var text = parse.GetValue(textArgument);
    var wiki = parse.GetValue(wikiOption);
    var navrhy = parse.GetValue(navrhyOption);

    if (parse.GetValue(benchmarkOption))
    {
        var result = BenchmarkReporter.Summarize(new WordProposals(navrhy?.FullName).Read());

        if (result.Decided == 0)
        {
            Console.WriteLine($"Zatím nic není rozhodnuto ({result.Undecided} návrhů z rozboru čeká na "
                + "':slova doplnit') — není z čeho počítat úspěšnost.");

            return 0;
        }

        Console.WriteLine($"Návrhy z rozboru: {result.Confirmed} potvrzeno, {result.Rejected} zamítnuto, "
            + $"{result.Undecided} zatím nerozhodnuto.");
        Console.WriteLine($"Úspěšnost (jen z rozhodnutých): {result.SuccessRate:P1} "
            + $"({result.Confirmed}/{result.Decided}).");

        return 0;
    }

    if (text is null == wiki is null)
    {
        Console.Error.WriteLine("Zadej buď cestu k souboru, nebo --wiki \"Název článku\" — právě jedno z obojího.");
        return 1;
    }

    var lexicon = parse.GetValue(lexiconOption);
    var limit = parse.GetValue(limitOption);
    var minDelka = parse.GetValue(minDelkaOption);
    var vzoruNaSlovo = parse.GetValue(vzoruNaSlovoOption);
    var @out = parse.GetValue(outOption)!;
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

    var rawText = wiki is not null
        ? await WikipediaReader.FetchArticleTextAsync(wiki, cancellationToken)
        : DocumentReader.ReadText(text!.FullName);
    var corpus = Tokenizer.CountTokens(rawText);
    var properNouns = Tokenizer.FindLikelyProperNouns(rawText);
    Console.Error.WriteLine($"Tokenů v textu (různých): {corpus.Count}, "
        + $"z toho vypadá na vlastní jméno: {properNouns.Count}");

    var store = new WordProposals(navrhy?.FullName);
    var existingLemmas = new HashSet<string>(
        store.Read().Select(proposal => proposal.Lemma.ToLowerInvariant()));

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

    var handled = CandidateRanking.DropShortFormAdjectiveOrParticiple(
        CandidateRanking.DropRedundantMorePattern(
            CandidateRanking.DropAlreadyHandled(candidates, known.IsKnown, existingLemmas)),
        known.IsKnown);
    var ranked = CandidateRanking.Thin(CandidateRanking.DropVowelEndingNounDuplicates(handled, known.IsKnown), vzoruNaSlovo)
        .OrderByDescending(candidate => candidate.Score)
        .ThenByDescending(candidate => corpus.GetValueOrDefault(candidate.Lemma))
        .Take(limit)
        .ToList();

    Reporter.WriteCsv(ranked, corpus, @out.FullName);
    Console.Error.WriteLine($"Zapsáno {ranked.Count} kandidátů do {@out.FullName}");

    if (!bezNavrhu)
    {
        var added = ProposalWriter.WriteNew(ranked, store);
        Console.Error.WriteLine($"Přidáno {added} nových návrhů do navrhy.json "
            + $"(zbytek už tam byl, z tohohle nebo dřívějšího běhu).");
    }

    return 0;
});

return await root.Parse(args).InvokeAsync();
