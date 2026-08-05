using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Providers.SqliteProviders;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Reports the result of validating a lexicon database.
    /// </summary>
    /// <param name="Errors">Findings that make the lexicon unusable.</param>
    /// <param name="Warnings">Findings worth looking at that still load.</param>
    public sealed record ValidationReport(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);

    /// <summary>
    /// Checks a lexicon database for the errors hand-editing introduces.
    /// </summary>
    /// <remarks>
    /// The database is edited by hand, so nothing upstream guarantees its shape. The CHECK constraints
    /// catch a misspelled enum at insert time but say nothing about a frame with no actor or a slot that
    /// can never surface, and they cannot notice that the CHECK list itself has fallen behind the C# enum
    /// it mirrors — which is why the enum columns are re-checked here against the real types.
    /// </remarks>
    public static class LexiconValidator
    {
        private static readonly (string Table, string Column, Type EnumType)[] EnumColumns =
        [
            ("lemma_entry", "category", typeof(WordCategory)),
            ("lemma_entry", "gender", typeof(Gender)),
            ("lemma_entry", "aspect", typeof(VerbAspect)),
            ("lemma_entry", "verb_class", typeof(VerbClass)),
            ("lemma_entry", "reflexive_type", typeof(ReflexiveType)),
            ("valency_frame", "kind", typeof(ValencyKind)),
            ("valency_frame", "diathesis", typeof(Diathesis)),
            ("valency_slot", "functor", typeof(FgdFunctor)),
            ("valency_slot", "control_target", typeof(FgdFunctor)),
            ("valency_slot", "obligatoriness", typeof(Obligatoriness)),
            ("slot_realization", "morph_case", typeof(Case))
        ];

        // The vzory live in Grammar.Czech's embedded JSON, which no CHECK can reach, so the check belongs
        // here — and is public because PhpSchemaParityTests holds LEXICON_PATTERNS against it.
        private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlySet<string>>> patternsByCategory = new(() =>
        {
            var verbs = new JsonVerbDataProvider();

            return new Dictionary<string, IReadOnlySet<string>>
            {
                [nameof(WordCategory.Noun)] = Fold(new JsonNounDataProvider().GetPatterns().Keys),
                [nameof(WordCategory.Adjective)] = Fold(new JsonAdjectiveDataProvider().GetPatterns().Keys),

                // Verbs accept both halves: the classes (trida4) and the named irregulars (moci, být),
                // because CzechVerbConjugationService looks the pattern up in both.
                [nameof(WordCategory.Verb)] = Fold(verbs.GetPatterns().Keys.Concat(verbs.GetIrregulars().Keys))
            };
        });

        /// <summary>
        /// Gets the inflection patterns each word category accepts, folded to lower case.
        /// </summary>
        /// <remarks>
        /// Keys are <see cref="WordCategory"/> member names. Categories that inflect by pattern are the
        /// only ones present; anything else carrying a pattern is an error rather than an empty list.
        /// </remarks>
        public static IReadOnlyDictionary<string, IReadOnlySet<string>> PatternsByCategory => patternsByCategory.Value;

        /// <summary>
        /// Validates the lexicon at the supplied path.
        /// </summary>
        /// <param name="path">The database to check.</param>
        /// <returns>Everything found, split into what blocks loading and what merely deserves attention.</returns>
        public static ValidationReport Validate(string path)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // SQLite would fail with "unable to open database file", naming neither the path nor the fix.
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Lexikon '{Path.GetFullPath(path)}' neexistuje. Vytvoř ho příkazem build, "
                    + "stáhni příkazem pull, nebo předej správnou cestu přes --db.",
                    path);
            }

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                ForeignKeys = true
            }.ToString();

            try
            {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                CheckSchemaVersion(connection, errors);
                CheckReferentialIntegrity(connection, errors);
                CheckEnums(connection, errors);
                CheckPatterns(connection, errors);
                CheckLemmaKeys(connection, errors);
                CheckFrames(connection, errors);
                CheckSlots(connection, errors);
                CheckDanglingLemmaReferences(connection, warnings);
                CheckEmptyLexemes(connection, warnings);
                CheckUnreachableLexemes(connection, warnings);

                return new ValidationReport(errors, warnings);
            }
            finally
            {
                // Disposing only pools the connection, which keeps the file open — and a pull renames
                // the database it has just validated, which on Windows fails while a handle is held.
                SqliteConnection.ClearAllPools();
            }
        }

        private static void CheckSchemaVersion(SqliteConnection connection, List<string> errors)
        {
            var stored = ScalarText(connection, "SELECT meta_value FROM lexicon_meta WHERE meta_key = 'schema_version'");

            if (stored is null)
            {
                errors.Add("V lexicon_meta chybí schema_version. Bez něj nelze poznat starou databázi od aktuální.");

                return;
            }

            if (stored != SqliteValencyProvider.SupportedSchemaVersion.ToString())
            {
                errors.Add(
                    $"schema_version je '{stored}', provider čte verzi "
                    + $"{SqliteValencyProvider.SupportedSchemaVersion}.");
            }
        }

        // Foreign key enforcement is off by default, so a row written without the pragma can sit in the
        // file pointing at nothing.
        private static void CheckReferentialIntegrity(SqliteConnection connection, List<string> errors)
        {
            foreach (var row in Query(connection, "PRAGMA foreign_key_check"))
            {
                errors.Add($"Porušená cizí klíč: tabulka {row[0]}, rowid {row[1]}, cíl {row[2]}.");
            }
        }

        private static void CheckEnums(SqliteConnection connection, List<string> errors)
        {
            foreach (var (table, column, enumType) in EnumColumns)
            {
                var sql = $"SELECT DISTINCT {column} FROM {table} WHERE {column} IS NOT NULL";

                foreach (var row in Query(connection, sql))
                {
                    var value = row[0]?.ToString() ?? string.Empty;

                    if (!Enum.TryParse(enumType, value, ignoreCase: false, out _))
                    {
                        errors.Add(
                            $"{table}.{column} obsahuje '{value}', což {enumType.Name} nezná. Povolené: "
                            + string.Join(", ", Enum.GetNames(enumType)) + ".");
                    }
                }
            }
        }

        // Nothing else refuses a misspelled vzor; it surfaces the first time something declines the word.
        // Folded, because the inflection services look it up through ToLower().
        private static void CheckPatterns(SqliteConnection connection, List<string> errors)
        {
            const string sql = "SELECT DISTINCT category, pattern FROM lemma_entry WHERE pattern IS NOT NULL";

            foreach (var row in Query(connection, sql))
            {
                var category = row[0]?.ToString() ?? string.Empty;
                var pattern = row[1]?.ToString() ?? string.Empty;

                if (!PatternsByCategory.TryGetValue(category, out var known))
                {
                    errors.Add(
                        $"lemma_entry.pattern je '{pattern}' u kategorie {category}, která vzory nemá. "
                        + "Vzor patří jen ke kategoriím "
                        + string.Join(", ", PatternsByCategory.Keys) + ".");

                    continue;
                }

                if (!known.Contains(pattern.ToLowerInvariant()))
                {
                    errors.Add(
                        $"lemma_entry.pattern obsahuje '{pattern}', což mezi vzory kategorie {category} není. "
                        + "Povolené: " + string.Join(", ", known.OrderBy(name => name, StringComparer.Ordinal)) + ".");
                }
            }
        }

        private static IReadOnlySet<string> Fold(IEnumerable<string> keys) =>
            keys.Select(key => key.ToLowerInvariant()).ToHashSet();

        // A wrong lemma_key is a lemma no lookup finds. Compared here and not in SQL, whose lower()
        // folds ASCII only and would pass Dát as correct.
        private static void CheckLemmaKeys(SqliteConnection connection, List<string> errors)
        {
            foreach (var row in Query(connection, "SELECT lemma, lemma_key FROM lemma_entry"))
            {
                var lemma = (string)row[0]!;
                var key = (string)row[1]!;

                if (key != lemma.ToLowerInvariant())
                {
                    errors.Add($"Lemma '{lemma}' má lemma_key '{key}', očekáváno '{lemma.ToLowerInvariant()}'.");
                }
            }
        }

        private static void CheckFrames(SqliteConnection connection, List<string> errors)
        {
            const string noSlots = """
                SELECT f.frame_id, u.sense_label
                FROM valency_frame f
                JOIN lexical_unit u ON u.lu_id = f.lu_id
                WHERE NOT EXISTS (SELECT 1 FROM valency_slot s WHERE s.frame_id = f.frame_id)
                """;

            foreach (var row in Query(connection, noSlots))
            {
                errors.Add($"Rámec {row[0]} ({row[1] ?? "bez názvu"}) nemá žádný slot.");
            }

            // Every Czech predicate has an actor, even where it goes unexpressed, so a frame without one is
            // an unfinished row rather than a verb of an unusual kind.
            const string noActor = """
                SELECT f.frame_id, u.sense_label
                FROM valency_frame f
                JOIN lexical_unit u ON u.lu_id = f.lu_id
                WHERE NOT EXISTS (
                    SELECT 1 FROM valency_slot s WHERE s.frame_id = f.frame_id AND s.functor = 'ACT')
                """;

            foreach (var row in Query(connection, noActor))
            {
                errors.Add($"Rámec {row[0]} ({row[1] ?? "bez názvu"}) nemá slot ACT.");
            }
        }

        private static void CheckSlots(SqliteConnection connection, List<string> errors)
        {
            const string noRealization = """
                SELECT s.slot_id, s.functor, s.frame_id
                FROM valency_slot s
                WHERE NOT EXISTS (
                    SELECT 1 FROM slot_realization r WHERE r.slot_id = s.slot_id)
                """;

            foreach (var row in Query(connection, noRealization))
            {
                errors.Add($"Slot {row[0]} ({row[1]}) v rámci {row[2]} nemá žádnou realizaci — nemůže se vyjádřit.");
            }

            // Generation takes the lowest-ranked realization, so a slot whose ranks all start above one has
            // no form the generator will produce even though it has forms.
            const string noPreferred = """
                SELECT s.slot_id, s.functor, s.frame_id
                FROM valency_slot s
                WHERE EXISTS (SELECT 1 FROM slot_realization r WHERE r.slot_id = s.slot_id)
                  AND NOT EXISTS (
                    SELECT 1 FROM slot_realization r WHERE r.slot_id = s.slot_id AND r.preference = 1)
                """;

            foreach (var row in Query(connection, noPreferred))
            {
                errors.Add($"Slot {row[0]} ({row[1]}) v rámci {row[2]} nemá realizaci s preference = 1.");
            }
        }

        // Both hold a lemma rather than a foreign key, since neither reference carries a category. A
        // dangling one warns rather than fails: it may name a real word the dictionary has yet to reach.
        private static void CheckDanglingLemmaReferences(SqliteConnection connection, List<string> warnings)
        {
            const string dangling = """
                SELECT lemma, column_name, target FROM (
                    SELECT lemma, 'aspect_counterpart' AS column_name, aspect_counterpart AS target
                    FROM lemma_entry WHERE aspect_counterpart IS NOT NULL
                    UNION ALL
                    SELECT lemma, 'base_verb_lemma', base_verb_lemma
                    FROM lemma_entry WHERE base_verb_lemma IS NOT NULL)
                WHERE target NOT IN (SELECT lemma FROM lemma_entry)
                """;

            foreach (var row in Query(connection, dangling))
            {
                warnings.Add($"'{row[0]}' odkazuje přes {row[1]} na '{row[2]}', které v lexikonu není.");
            }
        }

        private static void CheckEmptyLexemes(SqliteConnection connection, List<string> warnings)
        {
            const string empty = """
                SELECT x.lexeme_id, x.primary_lemma
                FROM lexeme x
                WHERE NOT EXISTS (SELECT 1 FROM lexical_unit u WHERE u.lexeme_id = x.lexeme_id)
                """;

            foreach (var row in Query(connection, empty))
            {
                warnings.Add($"Lexém {row[0]} ({row[1]}) nemá žádnou lexikální jednotku, takže nemá ani rámec.");
            }
        }

        // Valency is reached from a lemma, so a lexeme no lemma points at holds frames nothing can look
        // up — what deleting a heslo leaves, since the foreign key runs the other way and nothing objects.
        private static void CheckUnreachableLexemes(SqliteConnection connection, List<string> warnings)
        {
            const string unreachable = """
                SELECT x.lexeme_id, x.primary_lemma,
                       (SELECT COUNT(*) FROM lexical_unit u WHERE u.lexeme_id = x.lexeme_id)
                FROM lexeme x
                WHERE EXISTS (SELECT 1 FROM lexical_unit u WHERE u.lexeme_id = x.lexeme_id)
                  AND NOT EXISTS (SELECT 1 FROM lemma_entry e WHERE e.lexeme_id = x.lexeme_id)
                """;

            foreach (var row in Query(connection, unreachable))
            {
                var senses = Convert.ToInt64(row[2]);

                warnings.Add(
                    $"Na lexém {row[0]} ({row[1]}) neukazuje žádné heslo, takže jeho {senses} "
                    + $"{SenseCountNoun(senses)} a jejich rámce jsou nedosažitelné. Nejspíš zbytek po "
                    + "smazaném hesle.");
            }
        }

        // 1 / 2–4 / 5+ is the whole rule for this noun.
        private static string SenseCountNoun(long count) => count switch
        {
            1 => "význam",
            >= 2 and <= 4 => "významy",
            _ => "významů"
        };

        private static string? ScalarText(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            return command.ExecuteScalar() as string;
        }

        private static List<object?[]> Query(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = command.ExecuteReader();

            return ReadAll(reader);
        }

        private static List<object?[]> ReadAll(DbDataReader reader)
        {
            var rows = new List<object?[]>();

            while (reader.Read())
            {
                var row = new object?[reader.FieldCount];

                for (var index = 0; index < reader.FieldCount; index++)
                {
                    row[index] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                }

                rows.Add(row);
            }

            return rows;
        }
    }
}
