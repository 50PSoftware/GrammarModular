using Grammar.Czech.Cli;
using Grammar.Czech.Cli.Commands;
using System.CommandLine;
using System.Globalization;
using System.Text;

// gramatika — klientská aplikace nad Grammar Modular. Zadáš lemmata, dostaneš českou větu; co z nich
// nejde odvodit, na to se doptá, a na každý dotaz existuje přepínač, kterým jde odpovědět dopředu.

Console.OutputEncoding = Encoding.UTF8;

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
};

try
{
    return root.Parse(args).Invoke(new InvocationConfiguration { EnableDefaultExceptionHandler = false });
}
catch (CliException exception)
{
    Console.Error.WriteLine(exception.Message);

    return 1;
}
