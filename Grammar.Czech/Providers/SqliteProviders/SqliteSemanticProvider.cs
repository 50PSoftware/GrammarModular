using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Semantics;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;

namespace Grammar.Czech.Providers.SqliteProviders
{
    /// <summary>
    /// Loads componential semantic features and synonymy/antonymy relations from the SQLite lexicon
    /// database.
    /// </summary>
    /// <remarks>
    /// A separate class from <see cref="SqliteValencyProvider"/> rather than a third and fourth interface
    /// bolted onto it: both read the same file, but neither reads it through the other, and keeping them
    /// apart stops one class from carrying every table the lexicon happens to have. Every statement goes
    /// through <see cref="DbConnection"/> and <see cref="DbCommand"/> with parameters, the same discipline
    /// <see cref="SqliteValencyProvider"/> follows and for the same reason.
    /// </remarks>
    public sealed class SqliteSemanticProvider : ISemanticFeatureProvider, ISemanticRelationProvider
    {
        private const string SchemaVersionQuery =
            "SELECT meta_value FROM lexicon_meta WHERE meta_key = 'schema_version'";

        private const string FeatureQuery = """
            SELECT feature_name, feature_value, value_kind
            FROM semantic_feature
            WHERE lu_id = @luId
            ORDER BY feature_name
            """;

        // Symmetric and stored once, so a caller asking about one sense needs both columns checked —
        // done here rather than left to every caller.
        private const string RelationQuery = """
            SELECT lu_id_a, lu_id_b, relation_type, antonym_subtype, strength
            FROM semantic_relation
            WHERE lu_id_a = @luId OR lu_id_b = @luId
            ORDER BY relation_id
            """;

        private readonly Func<DbConnection> _connectionFactory;
        private readonly string _sourceDescription;

        private readonly ConcurrentDictionary<long, IReadOnlyList<SemanticFeature>> _featureCache = new();
        private readonly ConcurrentDictionary<long, IReadOnlyList<SemanticRelation>> _relationCache = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteSemanticProvider"/> type over a lexicon file.
        /// </summary>
        /// <param name="databasePath">
        /// The path to the lexicon database, or <see langword="null"/> to look at
        /// <see cref="SqliteValencyProvider.PathVariable"/> and then beside the application.
        /// </param>
        /// <exception cref="FileNotFoundException">The lexicon database does not exist.</exception>
        public SqliteSemanticProvider(string? databasePath = null)
        {
            var path = databasePath
                ?? Environment.GetEnvironmentVariable(SqliteValencyProvider.PathVariable)
                ?? Path.Combine(AppContext.BaseDirectory, SqliteValencyProvider.DefaultFileName);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Lexikon '{path}' neexistuje. Slovník se nedodává v balíčku — stáhni si ho a ukaž na něj: "
                    + $"AddCzechGrammarServices(cesta), proměnnou {SqliteValencyProvider.PathVariable}, "
                    + $"nebo polož {SqliteValencyProvider.DefaultFileName} vedle aplikace.",
                    path);
            }

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true
            }.ToString();

            _connectionFactory = () => new SqliteConnection(connectionString);
            _sourceDescription = path;

            VerifySchemaVersion();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteSemanticProvider"/> type over any ADO.NET
        /// connection.
        /// </summary>
        /// <param name="connectionFactory">Creates a closed connection to the lexicon on each call.</param>
        /// <param name="sourceDescription">Names the source in error messages.</param>
        public SqliteSemanticProvider(Func<DbConnection> connectionFactory, string sourceDescription)
        {
            _connectionFactory = connectionFactory;
            _sourceDescription = sourceDescription;

            VerifySchemaVersion();
        }

        /// <inheritdoc/>
        public IEnumerable<SemanticFeature> GetFeatures(long luId)
            => _featureCache.GetOrAdd(luId, LoadFeatures);

        /// <inheritdoc/>
        public IEnumerable<SemanticRelation> GetRelations(long luId)
            => _relationCache.GetOrAdd(luId, LoadRelations);

        private IReadOnlyList<SemanticFeature> LoadFeatures(long luId)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = FeatureQuery;
            AddParameter(command, luId);

            using var reader = command.ExecuteReader();

            var features = new List<SemanticFeature>();

            while (reader.Read())
            {
                features.Add(new SemanticFeature
                {
                    LuId = luId,
                    FeatureName = reader.GetString(0),
                    FeatureValue = reader.GetString(1),
                    ValueKind = Enum.Parse<SemanticValueKind>(reader.GetString(2))
                });
            }

            return features;
        }

        private IReadOnlyList<SemanticRelation> LoadRelations(long luId)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = RelationQuery;
            AddParameter(command, luId);

            using var reader = command.ExecuteReader();

            var relations = new List<SemanticRelation>();

            while (reader.Read())
            {
                relations.Add(new SemanticRelation
                {
                    LuIdA = reader.GetInt64(0),
                    LuIdB = reader.GetInt64(1),
                    RelationType = Enum.Parse<SemanticRelationType>(reader.GetString(2)),
                    AntonymSubtype = reader.IsDBNull(3)
                        ? null
                        : Enum.Parse<AntonymSubtype>(reader.GetString(3)),
                    Strength = reader.IsDBNull(4) ? null : reader.GetDouble(4)
                });
            }

            return relations;
        }

        /// <summary>
        /// Refuses a lexicon built to a schema this version does not read.
        /// </summary>
        /// <exception cref="InvalidOperationException">The versions do not match.</exception>
        private void VerifySchemaVersion()
        {
            string? stored;

            try
            {
                using var connection = OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText = SchemaVersionQuery;

                stored = command.ExecuteScalar() as string;
            }
            catch (DbException exception)
            {
                throw new InvalidOperationException(
                    $"Lexikon '{_sourceDescription}' nejde přečíst. Je to opravdu databáze slovníku? "
                    + exception.Message,
                    exception);
            }

            if (stored == SqliteValencyProvider.SupportedSchemaVersion.ToString(CultureInfo.InvariantCulture))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Lexikon '{_sourceDescription}' je psaný pro schéma verze {stored ?? "(neuvedeno)"}, "
                + $"tahle verze knihovny čte {SqliteValencyProvider.SupportedSchemaVersion}. Stáhni slovník "
                + "odpovídající verzi balíčku, nebo balíček aktualizuj.");
        }

        private DbConnection OpenConnection()
        {
            var connection = _connectionFactory();
            connection.Open();

            return connection;
        }

        private static void AddParameter(DbCommand command, long luId)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@luId";
            parameter.Value = luId;
            command.Parameters.Add(parameter);
        }
    }
}
