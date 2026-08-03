using Grammar.Czech.Lexicon.Tool;
using System.Text;
using System.Text.Json;

// Build, check, pull and export the Czech lexicon database.
//
// The dictionary is edited centrally, in MySQL behind a PHP admin, and read locally out of a SQLite
// file. pull is what carries it across; build and dump are what keep the local file workable on its
// own, and export-json is the same wire format in the other direction, for seeding the server.

Console.OutputEncoding = Encoding.UTF8;

return Run(args);

static int Run(string[] args)
{
    if (args.Length == 0)
    {
        PrintUsage();

        return 1;
    }

    try
    {
        switch (args[0])
        {
            case "build":
                return Build(args);

            case "validate":
                return Validate(args);

            case "dump":
                return Dump(args);

            case "pull":
                return Pull(args);

            case "export-json":
                return ExportJson(args);

            default:
                Console.Error.WriteLine($"Neznámý příkaz '{args[0]}'.");
                PrintUsage();

                return 1;
        }
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception.Message);

        return 1;
    }
}

static int Build(string[] args)
{
    var path = Option(args, "--out") ?? DefaultDatabasePath();
    var force = args.Contains("--force");

    LexiconBuilder.Build(path, force);
    Console.WriteLine($"Lexikon vytvořen: {Path.GetFullPath(path)}");

    return Validate(["validate", "--db", path]);
}

static int Validate(string[] args)
{
    // --url je vědomě jen z argumentu, ne z LEXICON_API_URL. Kdyby se bral z prostředí, `validate` bez
    // argumentů by u někoho kontroloval lokální soubor a u někoho server, podle toho, co má nastavené.
    var url = Option(args, "--url");

    if (url is null)
    {
        var path = Option(args, "--db") ?? DefaultDatabasePath();

        return Report(LexiconValidator.Validate(path), path);
    }

    var token = Option(args, "--token") ?? Environment.GetEnvironmentVariable("LEXICON_API_TOKEN");
    var pageSize = int.Parse(Option(args, "--page-size") ?? "5000");

    Console.WriteLine($"Kontroluji {url}");

    using var client = new LexiconApiClient(new Uri(url), token, pageSize);

    // Stáhne se celý slovník, zvaliduje a zahodí. Lokální lexikon se nemění ani při úspěchu — na to
    // je pull; tohle odpovídá jen na otázku, jestli to, co je na serveru, vůbec jde načíst.
    return Report(LexiconPuller.Check(client.Fetch(), Console.WriteLine), url);
}

static int Dump(string[] args)
{
    var path = Option(args, "--db") ?? DefaultDatabasePath();
    var output = Option(args, "--out")
        ?? throw new InvalidOperationException("Chybí --out s cestou k .sql souboru.");

    LexiconDumper.Dump(path, output);
    Console.WriteLine($"Zapsáno: {Path.GetFullPath(output)}");

    return 0;
}

static int Pull(string[] args)
{
    var url = Option(args, "--url")
        ?? Environment.GetEnvironmentVariable("LEXICON_API_URL")
        ?? throw new InvalidOperationException(
            "Chybí --url s adresou API, např. --url https://example.com/api/");

    var token = Option(args, "--token") ?? Environment.GetEnvironmentVariable("LEXICON_API_TOKEN");
    var destination = Option(args, "--out") ?? DefaultDatabasePath();
    var pageSize = int.Parse(Option(args, "--page-size") ?? "5000");

    Console.WriteLine($"Stahuji z {url}");

    using var client = new LexiconApiClient(new Uri(url), token, pageSize);

    var validation = LexiconPuller.Pull(client.Fetch(), destination, Console.WriteLine);

    if (validation.Errors.Count > 0)
    {
        Console.Error.WriteLine(
            "Stažený lexikon neprošel kontrolou, takže se nepoužil. Lokální soubor zůstal beze změny.");
    }

    return Report(validation, destination);
}

static int ExportJson(string[] args)
{
    var path = Option(args, "--db") ?? DefaultDatabasePath();
    var output = Option(args, "--out")
        ?? throw new InvalidOperationException("Chybí --out s cestou k adresáři pro .json soubory.");
    var pageSize = int.Parse(Option(args, "--page-size") ?? "5000");

    Directory.CreateDirectory(output);

    var written = 0;
    var pageNumbers = new Dictionary<string, int>(StringComparer.Ordinal);

    foreach (var page in LexiconJsonExporter.Export(path, pageSize))
    {
        var number = pageNumbers.GetValueOrDefault(page.Table) + 1;
        pageNumbers[page.Table] = number;

        var file = Path.Combine(output, $"{page.Table}.{number:D4}.json");
        File.WriteAllText(file, JsonSerializer.Serialize(page, LexiconPage.SerializerOptions), Encoding.UTF8);

        Console.WriteLine($"  {Path.GetFileName(file),-32} {page.Rows.Count,7} řádků");
        written++;
    }

    Console.WriteLine($"Zapsáno {written} stránek do {Path.GetFullPath(output)}");

    return 0;
}

static int Report(ValidationReport validation, string path)
{
    foreach (var warning in validation.Warnings)
    {
        Console.WriteLine($"varování: {warning}");
    }

    foreach (var error in validation.Errors)
    {
        Console.Error.WriteLine($"chyba: {error}");
    }

    if (validation.Errors.Count > 0)
    {
        Console.Error.WriteLine($"Lexikon '{path}' má {validation.Errors.Count} chyb.");

        return 1;
    }

    Console.WriteLine($"Lexikon '{path}' je v pořádku ({validation.Warnings.Count} varování).");

    return 0;
}

// The lexicon lives with the rest of the Czech data, so running the tool without arguments from
// anywhere in the repository works on the file that actually ships.
static string DefaultDatabasePath()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory is not null)
    {
        var candidate = Path.Combine(
            directory.FullName, "Grammar.Czech", "Data", "Lexicon", "grammar.czech.lexicon.db");

        if (File.Exists(candidate) || Directory.Exists(Path.GetDirectoryName(candidate)!))
        {
            return candidate;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException(
        "Nenašel jsem Grammar.Czech/Data/Lexicon. Spusť nástroj z repozitáře, nebo předej --db / --out.");
}

static string? Option(string[] args, string name)
{
    var index = Array.IndexOf(args, name);

    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static void PrintUsage()
{
    Console.WriteLine("""
        Grammar.Czech.Lexicon.Tool

          build       [--out <cesta>] [--force]      Vytvoří lexikon ze schématu a seedů a zvaliduje ho.
          validate    [--db  <cesta>]                Zkontroluje lokální lexikon.
          validate    --url <api> [--token <t>]      Zkontroluje, co má server — nic nemění.
          dump        [--db  <cesta>] --out <sql>    Vypíše lexikon jako přenositelné INSERTy.
          pull        --url <api> [--token <t>]      Stáhne slovník z API a nahradí jím lokální lexikon.
                      [--out <cesta>] [--page-size <n>]
          export-json [--db  <cesta>] --out <adresář> Vypíše lexikon ve formátu, který posílá API.
                      [--page-size <n>]

        Bez --db / --out se použije Grammar.Czech/Data/Lexicon/grammar.czech.lexicon.db.
        pull bere --url a --token i z proměnných LEXICON_API_URL a LEXICON_API_TOKEN.
        """);
}
