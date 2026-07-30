using Grammar.Czech.Lexicon.Tool;

// Build, check and export the Czech lexicon database. The database is the authored source of the
// dictionary, so the tool exists to give hand editing a safety net: build makes a fresh one, validate
// says what a hand-written row broke, and dump turns the binary file into reviewable text.

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
    var path = Option(args, "--db") ?? DefaultDatabasePath();
    var report = LexiconValidator.Validate(path);

    foreach (var warning in report.Warnings)
    {
        Console.WriteLine($"varování: {warning}");
    }

    foreach (var error in report.Errors)
    {
        Console.Error.WriteLine($"chyba: {error}");
    }

    if (report.Errors.Count > 0)
    {
        Console.Error.WriteLine($"Lexikon '{path}' má {report.Errors.Count} chyb.");

        return 1;
    }

    Console.WriteLine($"Lexikon '{path}' je v pořádku ({report.Warnings.Count} varování).");

    return 0;
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

          build    [--out <cesta>] [--force]   Vytvoří lexikon ze schématu a seedu a zvaliduje ho.
          validate [--db  <cesta>]             Zkontroluje existující lexikon.
          dump     [--db  <cesta>] --out <sql> Vypíše lexikon jako přenositelné INSERTy.

        Bez --db / --out se použije Grammar.Czech/Data/Lexicon/grammar.czech.lexicon.db.
        """);
}
