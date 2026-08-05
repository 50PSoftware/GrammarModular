using Grammar.Czech.Lexicon.Tool;
using System.Text.RegularExpressions;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Holds the portable schema and its MySQL variant to the same shape.
    /// </summary>
    /// <remarks>
    /// The two are separate files because a table cannot be altered into existence and MySQL differs from
    /// the first line of every declaration — AUTO_INCREMENT, ENGINE, and the collations that keep Czech
    /// from folding together. Separate files drift, and this is the drift that would hurt: the central
    /// editable copy and the local replica disagreeing about what a column is called, or one of them
    /// accepting a functor the other rejects. Neither would show up until data written on the server
    /// failed to load on the client.
    /// </remarks>
    [TestClass]
    public sealed class SchemaParityTests
    {
        // A column line is "name TYPE …", the type being what tells it from a CONSTRAINT or a CHECK
        // continuation. The word boundary applies to the bare types only: a trailing \b after
        // VARCHAR(64)'s parenthesis matches nothing and silently dropped every VARCHAR column.
        private static readonly Regex ColumnPattern = new(
            @"^\s*(?<name>[a-z_][a-z0-9_]*)\s+(?:(?:INTEGER|INT|SMALLINT)\b|VARCHAR\s*\(\s*\d+\s*\))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TablePattern = new(
            @"CREATE\s+TABLE\s+(?<name>[a-z_][a-z0-9_]*)\s*\(",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex LiteralPattern = new("'(?<value>[^']*)'", RegexOptions.Compiled);

        private static Dictionary<string, TableDefinition> portable = null!;
        private static Dictionary<string, TableDefinition> mysql = null!;

        /// <summary>
        /// Parses both schema files once.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            portable = Parse(ToolResources.Read(ToolResources.Schema));
            mysql = Parse(ToolResources.Read(ToolResources.MysqlSchema));
        }

        /// <summary>
        /// The parser actually found the schema, so the comparisons below are not passing on nothing.
        /// </summary>
        /// <remarks>
        /// Every other test here compares one parse against another, and two empty parses compare equal.
        /// A regex that stopped matching — after a reformat, or a rename of the embedded resource — would
        /// therefore turn the whole class green while checking nothing at all.
        /// </remarks>
        [TestMethod]
        public void Parser_FindsTheKnownTablesAndColumns()
        {
            string[] expected =
            [
                "lexicon_meta", "lexeme", "lemma_entry", "lexical_unit",
                "valency_frame", "valency_slot", "slot_realization", "construction"
            ];

            CollectionAssert.AreEquivalent(expected, portable.Keys.ToArray(), "Parser nenašel očekávané tabulky.");
            CollectionAssert.AreEquivalent(expected, mysql.Keys.ToArray(), "Parser nenašel očekávané tabulky.");

            Assert.AreEqual(32, portable["lemma_entry"].Columns.Count, "Parser nenačetl sloupce lemma_entry.");
            Assert.AreEqual(7, portable["slot_realization"].Columns.Count);

            Assert.IsTrue(
                portable["valency_slot"].Literals.Contains("ACT"),
                "Parser nenačetl hodnoty z CHECK omezení.");
        }

        /// <summary>
        /// Both schemas declare the same tables.
        /// </summary>
        [TestMethod]
        public void BothSchemas_DeclareTheSameTables()
            => CollectionAssert.AreEquivalent(
                portable.Keys.ToArray(),
                mysql.Keys.ToArray(),
                "Schémata se rozešla v seznamu tabulek.");

        /// <summary>
        /// Every table declares the same columns in the same order in both schemas.
        /// </summary>
        [TestMethod]
        public void EveryTable_DeclaresTheSameColumns()
        {
            foreach (var (table, definition) in portable)
            {
                Assert.IsTrue(mysql.ContainsKey(table), $"Tabulka '{table}' v MySQL schématu chybí.");

                CollectionAssert.AreEqual(
                    definition.Columns,
                    mysql[table].Columns,
                    $"Tabulka '{table}': sloupce se rozešly.\n"
                    + $"  přenositelné: {string.Join(", ", definition.Columns)}\n"
                    + $"  MySQL:        {string.Join(", ", mysql[table].Columns)}");
            }
        }

        /// <summary>
        /// Every table constrains the same set of literal values in both schemas.
        /// </summary>
        /// <remarks>
        /// This is what catches an enum member added to one file and not the other — a functor accepted by
        /// the server and rejected by the replica, which would surface as a frame that fails to import
        /// rather than as anything pointing at the schema.
        /// </remarks>
        [TestMethod]
        public void EveryTable_ConstrainsTheSameValues()
        {
            foreach (var (table, definition) in portable)
            {
                CollectionAssert.AreEquivalent(
                    definition.Literals,
                    mysql[table].Literals,
                    $"Tabulka '{table}': povolené hodnoty se rozešly. Rozdíl: "
                    + string.Join(", ", definition.Literals.Except(mysql[table].Literals)
                        .Concat(mysql[table].Literals.Except(definition.Literals)).DefaultIfEmpty("(pořadí)")));
            }
        }

        /// <summary>
        /// The MySQL schema keeps every matched column on a binary collation.
        /// </summary>
        /// <remarks>
        /// The default utf8mb4_0900_ai_ci is accent-insensitive, so under it dát and dat are one string:
        /// the unique index on the lookup key would reject one as a duplicate of the other, and an enum
        /// column would accept a spelling that Enum.TryParse then refuses case-sensitively. Columns read
        /// by a human rather than matched keep the forgiving default on purpose.
        /// </remarks>
        [TestMethod]
        public void MysqlSchema_CollatesMatchedColumnsBinary()
        {
            var text = ToolResources.Read(ToolResources.MysqlSchema);

            string[] mustBeBinary =
            [
                "lemma_key", "category", "gender", "verb_class", "aspect", "reflexive_type",
                "kind", "diathesis", "functor", "control_target", "obligatoriness", "morph_case"
            ];

            foreach (var column in mustBeBinary)
            {
                var declaration = new Regex(
                    $@"^\s*{column}\s+VARCHAR\s*\(\s*\d+\s*\)\s+COLLATE\s+utf8mb4_bin\b",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                Assert.IsTrue(
                    declaration.IsMatch(text),
                    $"Sloupec '{column}' nemá v MySQL schématu COLLATE utf8mb4_bin — "
                    + "výchozí kolace je akcentově necitlivá a slévala by česká slova.");
            }
        }

        /// <summary>
        /// The server schema sticks to collations MariaDB also has.
        /// </summary>
        /// <remarks>
        /// The utf8mb4_0900 family is MySQL 8 only. MariaDB does not know those names and rejects the
        /// whole script with "Unknown collation" — not a subtle failure, but one that only appears at
        /// deployment, on whichever host turns out to run MariaDB rather than MySQL. Shared hosting
        /// usually does.
        /// </remarks>
        [TestMethod]
        public void MysqlSchema_AvoidsCollationsMariaDbLacks()
        {
            var declarations = ToolResources.Read(ToolResources.MysqlSchema)
                .Split('\n')
                .Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal))
                .Where(line => line.Contains("utf8mb4_0900", StringComparison.Ordinal))
                .ToArray();

            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                declarations,
                "Schéma používá kolaci utf8mb4_0900_*, kterou MariaDB nezná:\n"
                + string.Join("\n", declarations));
        }

        private static Dictionary<string, TableDefinition> Parse(string sql)
        {
            var tables = new Dictionary<string, TableDefinition>(StringComparer.Ordinal);

            foreach (Match match in TablePattern.Matches(sql))
            {
                var body = ReadBody(sql, match.Index + match.Length - 1);

                tables[match.Groups["name"].Value] = new TableDefinition(
                    body.Split('\n')
                        .Select(line => ColumnPattern.Match(line))
                        .Where(column => column.Success)
                        .Select(column => column.Groups["name"].Value)
                        .ToList(),
                    LiteralPattern.Matches(body)
                        .Select(literal => literal.Groups["value"].Value)
                        .ToList());
            }

            return tables;
        }

        // Scans from the opening parenthesis to its match, so that a nested one inside a CHECK does not
        // end the table early.
        private static string ReadBody(string sql, int openingParenthesis)
        {
            var depth = 0;

            for (var index = openingParenthesis; index < sql.Length; index++)
            {
                if (sql[index] == '(')
                {
                    depth++;
                }
                else if (sql[index] == ')' && --depth == 0)
                {
                    return sql[(openingParenthesis + 1)..index];
                }
            }

            throw new InvalidOperationException("Neuzavřená závorka v CREATE TABLE.");
        }

        private sealed record TableDefinition(List<string> Columns, List<string> Literals);
    }
}
