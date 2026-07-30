using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Models;
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
    internal sealed record ValidationReport(IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);

    /// <summary>
    /// Checks a lexicon database for the errors hand-editing introduces.
    /// </summary>
    /// <remarks>
    /// The database is edited by hand, so nothing upstream guarantees its shape. The CHECK constraints
    /// catch a misspelled enum at insert time but say nothing about a frame with no actor or a slot that
    /// can never surface, and they cannot notice that the CHECK list itself has fallen behind the C# enum
    /// it mirrors — which is why the enum columns are re-checked here against the real types.
    /// </remarks>
    internal static class LexiconValidator
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

        /// <summary>
        /// Validates the lexicon at the supplied path.
        /// </summary>
        /// <param name="path">The database to check.</param>
        /// <returns>Everything found, split into what blocks loading and what merely deserves attention.</returns>
        public static ValidationReport Validate(string path)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                ForeignKeys = true
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            CheckSchemaVersion(connection, errors);
            CheckReferentialIntegrity(connection, errors);
            CheckEnums(connection, errors);
            CheckLemmaKeys(connection, errors);
            CheckFrames(connection, errors);
            CheckSlots(connection, errors);
            CheckDanglingLemmaReferences(connection, warnings);
            CheckEmptyLexemes(connection, warnings);

            return new ValidationReport(errors, warnings);
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

        // SQLite accepts foreign key clauses whether or not it enforces them, and enforcement is off by
        // default, so a row written by a connection that forgot the pragma can sit in the file pointing at
        // nothing. This is the sweep that finds those.
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

        // lemma_key is what every lookup matches on and it is folded in C# with ToLowerInvariant, so a
        // hand-typed row with the wrong key is a lemma that quietly cannot be found. SQLite's own lower()
        // folds ASCII only and would pass Dát as correct, which is why the comparison happens here.
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

        // These two columns hold a lemma rather than a foreign key, because lemma_entry is unique on
        // (lemma_key, category, homonym_index) and neither reference carries a category. A dangling one is
        // a warning and not an error: dát points at dávat and both are present, but jít points at zajít,
        // which is a real Czech verb the dictionary has simply not reached yet.
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
