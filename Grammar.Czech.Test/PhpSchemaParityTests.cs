using Grammar.Czech.Lexicon.Tool;
using Grammar.Czech.Services;
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
        /// Parses the PHP column map compiled into the tool.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _) => php = Parse(ToolResources.Read(ToolResources.PhpSchemaTables));

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
            var source = ToolResources.Read(ToolResources.PhpSchemaTables);

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
            var offered = ParseEnum(ToolResources.Read(ToolResources.PhpSchemaTables), column);

            Assert.IsTrue(offered.Count > 0, $"V schema-tables.php nenajdu hodnoty pro '{column}'.");

            CollectionAssert.AreEquivalent(
                Enum.GetNames(enumType),
                offered.ToArray(),
                $"Sloupec '{column}': PHP nabízí jiné hodnoty než {enumType.Name}.\n"
                + $"  chybí v PHP: {string.Join(", ", Enum.GetNames(enumType).Except(offered))}\n"
                + $"  navíc v PHP: {string.Join(", ", offered.Except(Enum.GetNames(enumType)))}");
        }

        /// <summary>
        /// The vzory the admin accepts are exactly the ones the pattern data defines.
        /// </summary>
        /// <remarks>
        /// This one is not an enum and cannot be a CHECK: the real list is the pattern JSON embedded in
        /// Grammar.Czech, which nothing on the server can read, so the admin has to carry a copy to
        /// refuse a typo at save time. This is what stops the copy from being a second source of truth.
        /// A vzor added to the JSON and not to PHP is one nobody can enter; one added to PHP and not to
        /// the JSON saves, survives a pull, passes validate, and throws on the first form generated.
        /// </remarks>
        [TestMethod]
        public void Php_AcceptsExactlyThePatternsTheDataDefines()
        {
            var offered = ParsePatterns(ToolResources.Read(ToolResources.PhpSchemaTables));

            CollectionAssert.AreEquivalent(
                LexiconValidator.PatternsByCategory.Keys.ToArray(),
                offered.Keys.ToArray(),
                "Kategorie se vzory se rozešly.\n"
                + $"  C#:  {string.Join(", ", LexiconValidator.PatternsByCategory.Keys)}\n"
                + $"  PHP: {string.Join(", ", offered.Keys)}");

            foreach (var (category, expected) in LexiconValidator.PatternsByCategory)
            {
                // Folded on both sides for the same reason the validator folds: the inflection services
                // look the pattern up through ToLower(), so casing is not what these two must agree on.
                var actual = offered[category].Select(name => name.ToLowerInvariant()).ToArray();

                CollectionAssert.AreEquivalent(
                    expected.ToArray(),
                    actual,
                    $"Kategorie '{category}': PHP nabízí jiné vzory než data.\n"
                    + $"  chybí v PHP: {string.Join(", ", expected.Except(actual))}\n"
                    + $"  navíc v PHP: {string.Join(", ", actual.Except(expected))}");
            }
        }

        /// <summary>
        /// Each slovesná třída in the admin fills the vzor the conjugation service conjugates it by.
        /// </summary>
        /// <remarks>
        /// A second copy of a mapping the engine owns, and the failure if they drift is silent: the entry
        /// saves, pulls and validates, and the verb is conjugated by the wrong class. The examples are a
        /// label, but each vzor they name has to exist or the form recommends an unsaveable one.
        /// </remarks>
        [TestMethod]
        public void Php_MapsEachVerbClassToThePatternTheEngineUses()
        {
            var source = ToolResources.Read(ToolResources.PhpSchemaTables);
            var offered = ParseVerbClasses(source);
            var expected = CzechVerbConjugationService.PatternByVerbClass;

            CollectionAssert.AreEquivalent(
                expected.Keys.Select(verbClass => verbClass.ToString()).ToArray(),
                offered.Keys.ToArray(),
                "Třídy se rozešly.\n"
                + $"  C#:  {string.Join(", ", expected.Keys)}\n"
                + $"  PHP: {string.Join(", ", offered.Keys)}");

            var verbPatterns = ParsePatterns(source)["Verb"];

            foreach (var (verbClass, pattern) in expected)
            {
                Assert.AreEqual(
                    pattern,
                    offered[verbClass.ToString()].Pattern,
                    $"Třída {verbClass} plní v administraci jiný vzor, než kterým ji engine časuje.");

                foreach (var example in offered[verbClass.ToString()].Examples)
                {
                    CollectionAssert.Contains(
                        verbPatterns,
                        example,
                        $"Třída {verbClass} nabízí jako příklad vzor '{example}', který neexistuje.");
                }
            }
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
            ["reflexive_type", typeof(Core.Enums.ReflexiveType)],
            ["kind", typeof(Core.Enums.ValencyKind)],
            ["diathesis", typeof(Core.Enums.Diathesis)],
            ["functor", typeof(Core.Enums.FgdFunctor)],
            ["obligatoriness", typeof(Core.Enums.Obligatoriness)],
            ["morph_case", typeof(Core.Enums.Case)]
        ];

        // Each entry is 'ClassN' => ['pattern' => '…', 'ending' => '…', 'examples' => ['…', …]].
        private static Dictionary<string, (string Pattern, List<string> Examples)> ParseVerbClasses(
            string source)
        {
            var block = Regex.Match(source, @"const\s+LEXICON_VERB_CLASSES\s*=\s*\[(?<body>.*?)^\];",
                RegexOptions.Singleline | RegexOptions.Multiline);

            Assert.IsTrue(block.Success, "V schema-tables.php nenajdu konstantu LEXICON_VERB_CLASSES.");

            var classes = new Dictionary<string, (string, List<string>)>(StringComparer.Ordinal);

            foreach (Match entry in Regex.Matches(
                block.Groups["body"].Value,
                @"'(?<class>Class\d)'\s*=>\s*\[\s*'pattern'\s*=>\s*'(?<pattern>[^']+)'.*?"
                    + @"'examples'\s*=>\s*\[(?<examples>[^\]]*)\]",
                RegexOptions.Singleline))
            {
                classes[entry.Groups["class"].Value] = (
                    entry.Groups["pattern"].Value,
                    Regex.Matches(entry.Groups["examples"].Value, @"'(?<example>[^']+)'")
                        .Select(example => example.Groups["example"].Value)
                        .ToList());
            }

            Assert.IsTrue(classes.Count > 0, "Parser nenačetl z LEXICON_VERB_CLASSES žádnou třídu.");

            return classes;
        }

        // Each entry is 'Category' => ['vzor', 'vzor', …] — a flat list, unlike the enum map, because a
        // vzor is its own label. The values carry diacritics, so the value pattern cannot be [a-z_]+.
        private static Dictionary<string, List<string>> ParsePatterns(string source)
        {
            var block = Regex.Match(source, @"const\s+LEXICON_PATTERNS\s*=\s*\[(?<body>.*?)^\];",
                RegexOptions.Singleline | RegexOptions.Multiline);

            Assert.IsTrue(block.Success, "V schema-tables.php nenajdu konstantu LEXICON_PATTERNS.");

            var categories = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (Match entry in Regex.Matches(
                block.Groups["body"].Value,
                @"'(?<category>[A-Za-z]+)'\s*=>\s*\[(?<patterns>[^\]]*)\]",
                RegexOptions.Singleline))
            {
                categories[entry.Groups["category"].Value] = Regex
                    .Matches(entry.Groups["patterns"].Value, @"'(?<pattern>[^']+)'")
                    .Select(pattern => pattern.Groups["pattern"].Value)
                    .ToList();
            }

            Assert.IsTrue(categories.Count > 0, "Parser nenačetl z LEXICON_PATTERNS žádnou kategorii.");

            return categories;
        }

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
