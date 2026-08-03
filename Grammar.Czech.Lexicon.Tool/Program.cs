using Grammar.Czech.Lexicon.Tool;
using System.Text;
using System.Text.Json;

// Build, check, pull and export the Czech lexicon database.
//
// The dictionary is edited centrally, in MySQL behind a PHP admin, and read locally out of a SQLite
// file. pull is what carries it across; build and dump are what keep the local file workable on its
// own, and export-json is the same wire format in the other direction, for seeding the server.
//
// Settings come from the command line, from lexikon.json in the working directory or one of its
// parents, or from the environment — see ToolSettings.

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
        var settings = ToolSettings.Load(args);

        return args[0] switch
        {
            "build" => Build(args, settings),
            "validate" => Validate(args, settings),
            "dump" => Dump(args, settings),
            "pull" => Pull(args, settings),
            "export-json" => ExportJson(args, settings),
            _ => Unknown(args[0]),
        };
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception.Message);

        return 1;
    }
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Neznámý příkaz '{command}'.");
    PrintUsage();

    return 1;
}

static int Build(string[] args, ToolSettings settings)
{
    // Tady --out říká, kam lexikon vytvořit.
    var path = Argument(args, "--out") ?? LexiconPath(settings);

    LexiconBuilder.Build(path, args.Contains("--force"));
    Console.WriteLine($"Lexikon vytvořen: {Path.GetFullPath(path)}");

    return Report(LexiconValidator.Validate(path), path);
}

static int Validate(string[] args, ToolSettings settings)
{
    // Kontrola serveru se musí vyžádat. Kdyby stačilo mít adresu v konfiguraci, `validate` bez
    // argumentů by u jednoho člověka kontroloval lokální soubor a u druhého server.
    var wantsServer = args.Contains("--server") || Array.IndexOf(args, "--url") >= 0;

    if (!wantsServer)
    {
        var path = LexiconPath(settings);

        return Report(LexiconValidator.Validate(path), path);
    }

    var url = settings.RequireUrl();
    Console.WriteLine($"Kontroluji {url}");

    using var client = new LexiconApiClient(new Uri(url), settings.Token, settings.PageSize);

    // Stáhne celý slovník, zvaliduje a zahodí. Lokální lexikon se nemění ani při úspěchu — na to je
    // pull; tohle odpovídá jen na otázku, jestli to, co je na serveru, vůbec jde načíst.
    return Report(LexiconPuller.Check(client.Fetch(), Console.WriteLine), url);
}

static int Dump(string[] args, ToolSettings settings)
{
    var output = Argument(args, "--out")
        ?? throw new InvalidOperationException("Chybí --out s cestou k .sql souboru.");

    // Tady --out říká, kam zapsat výpis; zdrojem je lexikon z --db nebo z nastavení.
    LexiconDumper.Dump(LexiconPath(settings), output);
    Console.WriteLine($"Zapsáno: {Path.GetFullPath(output)}");

    return 0;
}

static int Pull(string[] args, ToolSettings settings)
{
    var url = settings.RequireUrl();

    // Tady --out říká, kam lexikon uložit.
    var destination = Argument(args, "--out") ?? LexiconPath(settings);

    Console.WriteLine($"Stahuji z {url}");

    using var client = new LexiconApiClient(new Uri(url), settings.Token, settings.PageSize);

    var validation = LexiconPuller.Pull(client.Fetch(), destination, Console.WriteLine);

    if (validation.Errors.Count > 0)
    {
        Console.Error.WriteLine(
            "Stažený lexikon neprošel kontrolou, takže se nepoužil. Lokální soubor zůstal beze změny.");
    }

    return Report(validation, destination);
}

static int ExportJson(string[] args, ToolSettings settings)
{
    var output = Argument(args, "--out")
        ?? throw new InvalidOperationException("Chybí --out s cestou k adresáři pro .json soubory.");

    Directory.CreateDirectory(output);

    var written = 0;
    var pageNumbers = new Dictionary<string, int>(StringComparer.Ordinal);

    foreach (var page in LexiconJsonExporter.Export(LexiconPath(settings), settings.PageSize))
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

static string LexiconPath(ToolSettings settings) => settings.DatabasePath ?? DefaultDatabasePath();

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

// Poslední záchrana, když cestu neřekl nikdo: uvnitř repozitáře se lexikon najde sám. Nainstalovaný
// nástroj spuštěný jinde ji nenajde a řekne, jak ji zadat.
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
        $"Nevím, kde je lexikon. Zapiš cestu do {ToolSettings.FileName} jako \"database\", "
        + "nebo ji předej přes --db.");
}

static string? Argument(string[] args, string name)
{
    var index = Array.IndexOf(args, name);

    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static void PrintUsage()
{
    Console.WriteLine($"""
        lexikon — správa českého slovníku

          build       [--db <cesta>] [--force]       Vytvoří lexikon ze schématu a seedů a zvaliduje ho.
          validate    [--db <cesta>]                 Zkontroluje lokální lexikon.
          validate    --server | --url <api>         Zkontroluje, co má server — nic nemění.
          pull        [--url <api>] [--out <cesta>]  Stáhne slovník z API a nahradí jím lokální lexikon.
          dump        [--db <cesta>] --out <sql>     Vypíše lexikon jako přenositelné INSERTy.
          export-json [--db <cesta>] --out <adresář> Vypíše lexikon ve formátu, který posílá API.

        Nastavení se bere v tomhle pořadí:

          1. argument         --url, --token, --db, --out, --page-size
          2. {ToolSettings.FileName,-16}    url, token, database, pageSize — hledá se i v nadřazených adresářích
          3. prostředí        LEXICON_API_URL, LEXICON_API_TOKEN

        Token nech v prostředí; {ToolSettings.FileName} patří do gitu, token ne.
        """);
}
