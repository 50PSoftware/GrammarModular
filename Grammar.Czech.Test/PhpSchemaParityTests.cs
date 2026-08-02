using Grammar.Czech.Lexicon.Tool;
using System.Text.RegularExpressions;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Holds the PHP column map and <see cref="LexiconSchema"/> to the same shape.
    /// </summary>
    /// <remarks>
    /// The API sends rows as bare arrays with the column names in a single header, so the two lists are
    /// the whole contract between the server and the importer. The importer does check the header it
    /// receives — but only at pull time, against a running server, which is the worst moment and the
    /// worst place to discover that the two files disagree. This checks it at build time instead.
    /// </remarks>
    [TestClass]
    public sealed class PhpSchemaParityTests
    {
        private static Dictionary<string, List<string>> php = null!;

        /// <summary>
        /// Parses the PHP column map copied next to the test assembly.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Php", "schema-tables.php");

            Assert.IsTrue(File.Exists(path), $"'{path}' se nezkopíroval do výstupu.");

            php = Parse(File.ReadAllText(path));
        }

        /// <summary>
        /// The parser found the map, so the comparisons below are not passing on nothing.
        /// </summary>
        [TestMethod]
        public void Parser_FindsTheMap()
        {
            Assert.AreEqual(LexiconSchema.Tables.Count, php.Count, "Parser nenačetl všechny tabulky.");
            Assert.AreEqual(24, php["lemma_entry"].Count, "Parser nenačetl sloupce lemma_entry.");
        }

        /// <summary>
        /// PHP declares the same tables as the C# schema, in the same order.
        /// </summary>
        /// <remarks>
        /// The order matters as much as the membership: both sides insert parents before children, and a
        /// table moved ahead of the one it references would fail on a foreign key halfway through a pull.
        /// </remarks>
        [TestMethod]
        public void Php_DeclaresTheSameTablesInTheSameOrder()
            => CollectionAssert.AreEqual(
                LexiconSchema.Tables.Select(table => table.Name).ToArray(),
                php.Keys.ToArray(),
                "Tabulky se rozešly.\n"
                + $"  C#:  {string.Join(", ", LexiconSchema.Tables.Select(table => table.Name))}\n"
                + $"  PHP: {string.Join(", ", php.Keys)}");

        /// <summary>
        /// Both sides agree on which tables are keyed by text.
        /// </summary>
        /// <remarks>
        /// The flag decides how a paging key is bound. If the two disagreed, one side would compare the
        /// key as text and the other as a number, and the pages would not line up — the failure being
        /// rows that are never fetched rather than an error.
        /// </remarks>
        [TestMethod]
        public void Php_AgreesOnWhichKeysAreText()
        {
            var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Php", "schema-tables.php"));

            var declared = Regex.Match(source, @"const\s+LEXICON_TEXT_KEY_TABLES\s*=\s*\[(?<body>[^\]]*)\]");

            Assert.IsTrue(declared.Success, "V schema-tables.php nenajdu LEXICON_TEXT_KEY_TABLES.");

            CollectionAssert.AreEquivalent(
                LexiconSchema.Tables.Where(table => table.KeyIsText).Select(table => table.Name).ToArray(),
                Regex.Matches(declared.Groups["body"].Value, @"'(?<table>[a-z_]+)'")
                    .Select(match => match.Groups["table"].Value)
                    .ToArray(),
                "Tabulky s textovým klíčem se rozešly.");
        }

        /// <summary>
        /// Every table declares the same columns in the same order on both sides.
        /// </summary>
        [TestMethod]
        public void Php_DeclaresTheSameColumns()
        {
            foreach (var table in LexiconSchema.Tables)
            {
                Assert.IsTrue(php.ContainsKey(table.Name), $"Tabulka '{table.Name}' v PHP chybí.");

                CollectionAssert.AreEqual(
                    table.Columns.ToArray(),
                    php[table.Name],
                    $"Tabulka '{table.Name}': sloupce se rozešly.\n"
                    + $"  C#:  {string.Join(", ", table.Columns)}\n"
                    + $"  PHP: {string.Join(", ", php[table.Name])}");
            }
        }

        /// <summary>
        /// The values the admin offers are exactly the members of the enums the provider parses.
        /// </summary>
        /// <remarks>
        /// The admin builds its dropdowns from that list, and the provider parses what comes back
        /// case-sensitively. A functor added to the C# enum and not to the PHP map is a value nobody can
        /// enter; one added to PHP and not to C# is a value that saves, survives a pull, and then throws
        /// on the first lookup that touches the frame holding it.
        /// </remarks>
        [DataTestMethod]
        [DynamicData(nameof(EnumColumns), DynamicDataSourceType.Property)]
        public void Php_OffersExactlyTheEnumMembers(string column, Type enumType)
        {
            var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Php", "schema-tables.php"));
            var offered = ParseEnum(source, column);

            Assert.IsTrue(offered.Count > 0, $"V schema-tables.php nenajdu hodnoty pro '{column}'.");

            CollectionAssert.AreEquivalent(
                Enum.GetNames(enumType),
                offered.ToArray(),
                $"Sloupec '{column}': PHP nabízí jiné hodnoty než {enumType.Name}.\n"
                + $"  chybí v PHP: {string.Join(", ", Enum.GetNames(enumType).Except(offered))}\n"
                + $"  navíc v PHP: {string.Join(", ", offered.Except(Enum.GetNames(enumType)))}");
        }

        /// <summary>
        /// Gets the constrained columns and the enum each one mirrors.
        /// </summary>
        public static IEnumerable<object[]> EnumColumns =>
        [
            ["category", typeof(Core.Enums.WordCategory)],
            ["gender", typeof(Core.Enums.Gender)],
            ["aspect", typeof(Core.Enums.VerbAspect)],
            ["verb_class", typeof(Czech.Models.VerbClass)],
            ["reflexive_type", typeof(Czech.Enums.ReflexiveType)],
            ["kind", typeof(Core.Enums.ValencyKind)],
            ["diathesis", typeof(Core.Enums.Diathesis)],
            ["functor", typeof(Core.Enums.FgdFunctor)],
            ["obligatoriness", typeof(Core.Enums.Obligatoriness)],
            ["morph_case", typeof(Core.Enums.Case)]
        ];

        // Each entry is 'column' => ['Value' => 'label', …]. Only the keys are data; the labels are
        // Czech and belong to the screen.
        private static List<string> ParseEnum(string source, string column)
        {
            var block = Regex.Match(
                source,
                $@"'{Regex.Escape(column)}'\s*=>\s*\[(?<body>[^\]]*)\]",
                RegexOptions.Singleline);

            return block.Success
                ? Regex.Matches(block.Groups["body"].Value, @"'(?<value>[^']+)'\s*=>")
                    .Select(match => match.Groups["value"].Value)
                    .ToList()
                : [];
        }

        // Each entry is 'table' => ['col', 'col', …]. The inner arrays are flat, so a non-greedy match up
        // to the first closing bracket takes exactly one table's columns.
        private static Dictionary<string, List<string>> Parse(string source)
        {
            var block = Regex.Match(source, @"const\s+LEXICON_TABLES\s*=\s*\[(?<body>.*?)^\];",
                RegexOptions.Singleline | RegexOptions.Multiline);

            Assert.IsTrue(block.Success, "V schema-tables.php nenajdu konstantu LEXICON_TABLES.");

            var tables = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (Match entry in Regex.Matches(
                block.Groups["body"].Value,
                @"'(?<table>[a-z_]+)'\s*=>\s*\[(?<columns>[^\]]*)\]",
                RegexOptions.Singleline))
            {
                tables[entry.Groups["table"].Value] = Regex
                    .Matches(entry.Groups["columns"].Value, @"'(?<column>[a-z_]+)'")
                    .Select(column => column.Groups["column"].Value)
                    .ToList();
            }

            return tables;
        }
    }
}
