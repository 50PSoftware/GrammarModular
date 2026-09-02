using Grammar.Czech.Cli;
using Grammar.Czech.Cli.Commands;
using Grammar.Czech.Cli.Interaction;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.Globalization;
using System.Text;

// gramatika — klientská aplikace nad Grammar Modular. Zadáš lemmata, dostaneš českou větu; co z nich
// nejde odvodit, na to se doptá, a na každý dotaz existuje přepínač, kterým jde odpovědět dopředu.

Console.OutputEncoding = Encoding.UTF8;

// A taky vstupní. Bez toho čte konzole na Windows v OEM kódové stránce a 'čte' napsané do sezení
// dorazí rozsypané — což je u nástroje, jehož vstupem jsou česká slova, vada v samotném zadání.
// V try, protože nastavit kódování přesměrovaného vstupu nejde všude a spadnout na tom by bylo horší.
try
{
    Console.InputEncoding = Encoding.UTF8;
}
catch (IOException)
{
}

// Nápovědu System.CommandLine překládá satelitní assembly podle kultury rozhraní. Rozhraní je české,
// tak ať je česky celé — i na stroji nastaveném jinak.
CultureInfo.CurrentUICulture = new CultureInfo("cs");

var lexicon = new Option<FileInfo?>("--slovnik")
{
    Description = "Cesta ke slovníku. Jinak se bere z GRAMMAR_CZECH_LEXICON nebo z adresáře aplikace.",
    Recursive = true,
};

var root = new RootCommand("gramatika — poskládá českou větu ze zadaných lemmat")
{
    lexicon,
    SentenceCommand.Create(lexicon),
    RelationsCommand.Create(lexicon),
    TermsCommand.Create(),
};

// Bez argumentů se spustí sezení. Vedle 'veta', ne místo něj: 'veta' je jeden příkaz, na který jde
// odpovědět dopředu a pustit ho ze skriptu, kdežto sezení je pro to, čemu se věta dělá — šťouchá se
// do ní, dokud nesedí, a každé šťouchnutí bylo dosud nový proces.
root.SetAction(parse => Services.Build(parse.GetValue(lexicon))
    .GetRequiredService<SessionLoop>()
    .Run());

try
{
    return root.Parse(args).Invoke(new InvocationConfiguration { EnableDefaultExceptionHandler = false });
}
catch (CliException exception)
{
    Console.Error.WriteLine(exception.Message);

    return 1;
}
catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException
    or ArgumentException or KeyNotFoundException or FormatException)
{
    // Hlášky knihovny mluví ke konzumentovi NuGetu: anglicky, o vzorech a rámcích, a bez rady, co
    // udělat jinak. Sem chodí to, na co nástroj nemá vlastní hlášku, a i tak má odejít česky a s
    // původním textem stranou — bez něj by se nedalo dohledat, co se vlastně stalo.
    Console.Error.WriteLine($"""
        Na tomhle jsem si vylámal zuby a neumím poradit líp než tímhle:

          {exception.Message}

        Nejčastěji je to slovo, které slovník nezná a jehož tvar se nedá odvodit ze zakončení.
        Zkus mu doplnit vzor a rod: --vzor slovo=hrad --rod slovo=muzsky
        """);

    return 1;
}
