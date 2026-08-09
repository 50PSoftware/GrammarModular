using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Core.Models.Valency;
using Grammar.Czech.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;

namespace Grammar.Czech.Providers.SqliteProviders
{
    /// <summary>
    /// Loads Czech lexical entries and valency frames from the SQLite lexicon database.
    /// </summary>
    /// <remarks>
    /// The database is the authored source of the dictionary, not a build artefact of the old JSON. It
    /// replaced JsonValencyProvider because a hand-written JSON object is an authoring format for tens of
    /// entries and the lexicon is meant to reach thousands.
    /// <para>
    /// Every statement here goes through <see cref="DbConnection"/> and <see cref="DbCommand"/> and every
    /// value is passed as a parameter, so that moving the lexicon onto MySQL, Microsoft SQL or Firebird
    /// means handing a different factory to <see cref="SqliteValencyProvider(Func{DbConnection}, string)"/>
    /// rather than rewriting the queries. The SQL avoids LIMIT and TOP for the same reason: the reader
    /// stops after the row it wants.
    /// </para>
    /// </remarks>
    public sealed class SqliteValencyProvider : IValencyProvider<CzechLexicalEntry>, IConstructionProvider
    {
        /// <summary>
        /// The file name looked for beside the application when no path is given.
        /// </summary>
        public const string DefaultFileName = "grammar.czech.lexicon.db";

        /// <summary>
        /// The environment variable naming the lexicon, used when no path is passed in code.
        /// </summary>
        /// <remarks>
        /// The dictionary is not shipped inside the package: it grows on its own schedule, and binding it
        /// to a package version would mean a release of this library every time a word is added. A
        /// deployment therefore says where its copy is, and this is the way to say it without a rebuild.
        /// </remarks>
        public const string PathVariable = "GRAMMAR_CZECH_LEXICON";

        /// <summary>
        /// The schema version this provider reads, matching schema_version in lexicon_meta.
        /// </summary>
        public const int SupportedSchemaVersion = 5;

        private const string SchemaVersionQuery =
            "SELECT meta_value FROM lexicon_meta WHERE meta_key = 'schema_version'";

        private const string EntryColumns = """
            SELECT lemma, category, gender, pattern, is_animate, has_mobile_e,
                   has_genitive_plural_shortening, has_epenthesis_in_genitive_plural,
                   is_indeclinable, is_plural_only, is_countable, prefers_short_form,
                   verb_class, aspect, aspect_counterpart, reflexive_type, base_verb_lemma,
                   stem, present_stem, past_stem, future_stem, imperative_stem, passive_stem,
                   infinitive, forms_passive,

                   -- Na konci, ne u aspect_counterpart, kam patří významem. Připsáním se nemůže posunout
                   -- nic nad ním, a ordinály v ReadEntry jsou jediné, co stojí mezi tímhle dotazem a tichem.
                   aktionsart
            FROM lemma_entry

            """;

        private const string EntryQuery = EntryColumns + """
            WHERE lemma_key = @key
            ORDER BY homonym_index
            """;

        private const string EntryByCategoryQuery = EntryColumns + """
            WHERE lemma_key = @key AND category = @category
            ORDER BY homonym_index
            """;

        private const string FrameQuery = """
            SELECT f.frame_id, e.lemma, f.lu_id, u.sense_label, f.kind, f.diathesis, f.is_default,
                   s.slot_id, s.functor, s.canonical_order, s.obligatoriness,
                   s.can_drop_contextual, s.can_drop_generic, s.control_target,
                   r.morph_case, r.preposition, r.clause_type, r.takes_infinitive, r.preference,
                   f.reflexive_type
            FROM lemma_entry e
            JOIN lexical_unit u ON u.lexeme_id = e.lexeme_id
            JOIN valency_frame f ON f.lu_id = u.lu_id
            LEFT JOIN valency_slot s ON s.frame_id = f.frame_id
            LEFT JOIN slot_realization r ON r.slot_id = s.slot_id
            WHERE e.lemma_key = @key
            ORDER BY f.frame_id, s.canonical_order, s.slot_id, r.preference, r.realization_id
            """;

        // Column ordinals of FrameQuery. Named because the query is wide enough that a bare index says
        // nothing, and because one column inserted in the middle would otherwise silently shift the rest.
        private const int FrameId = 0;
        private const int FrameLemma = 1;
        private const int FrameLuId = 2;
        private const int FrameSenseLabel = 3;
        private const int FrameKind = 4;
        private const int FrameDiathesis = 5;
        private const int FrameIsDefault = 6;
        private const int SlotId = 7;
        private const int SlotFunctor = 8;
        private const int SlotCanonicalOrder = 9;
        private const int SlotObligatoriness = 10;
        private const int SlotCanDropContextual = 11;
        private const int SlotCanDropGeneric = 12;
        private const int SlotControlTarget = 13;
        private const int RealizationCase = 14;
        private const int RealizationPreposition = 15;
        private const int RealizationClauseType = 16;
        private const int RealizationTakesInfinitive = 17;
        private const int RealizationPreference = 18;

        // Last rather than beside is_default, where it belongs by meaning: appending cannot shift
        // anything above it, and this list is the only thing standing between the query and silence.
        private const int FrameReflexiveType = 19;

        // Constructions are few and every one of them is read the moment any of them is, so the whole
        // table is loaded at once rather than queried per lemma.
        private const string ConstructionQuery = """
            SELECT pattern_name, light_verb_lemma, pred_noun_lemma, template_json
            FROM construction
            ORDER BY pattern_name
            """;

        private readonly Func<DbConnection> _connectionFactory;
        private readonly string _sourceDescription;

        private readonly ConcurrentDictionary<string, CzechLexicalEntry?> _entryCache = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, IReadOnlyList<ValencyFrame>> _frameCache = new(StringComparer.Ordinal);
        private readonly Lazy<IReadOnlyList<ConstructionTemplate>> _constructions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteValencyProvider"/> type over a lexicon file.
        /// </summary>
        /// <param name="databasePath">
        /// The path to the lexicon database, or <see langword="null"/> to look at
        /// <see cref="PathVariable"/> and then beside the application.
        /// </param>
        /// <exception cref="FileNotFoundException">The lexicon database does not exist.</exception>
        /// <remarks>
        /// The dictionary is not carried inside the package. It grows on its own schedule — a word added
        /// on the server is not a reason to release the library — so a deployment points at its own copy
        /// and can replace that copy without rebuilding anything.
        /// </remarks>
        public SqliteValencyProvider(string? databasePath = null)
        {
            var path = databasePath
                ?? Environment.GetEnvironmentVariable(PathVariable)
                ?? Path.Combine(AppContext.BaseDirectory, DefaultFileName);

            // SQLite would otherwise create an empty database and every lemma would read as a gap in the
            // dictionary rather than as a lexicon nobody supplied.
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Lexikon '{path}' neexistuje. Slovník se nedodává v balíčku — stáhni si ho a ukaž na něj: "
                    + $"AddCzechGrammarServices(cesta), proměnnou {PathVariable}, nebo polož {DefaultFileName} "
                    + "vedle aplikace.",
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

            _constructions = new(LoadConstructions);

            VerifySchemaVersion();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteValencyProvider"/> type over any ADO.NET
        /// connection.
        /// </summary>
        /// <param name="connectionFactory">Creates a closed connection to the lexicon on each call.</param>
        /// <param name="sourceDescription">Names the source in error messages.</param>
        /// <remarks>
        /// This is the seam for a server-side backend. The factory is called per lookup because a single
        /// <see cref="SqliteConnection"/> is not safe to use from several threads at once and the provider
        /// is registered as a singleton; connection pooling makes the repeated open cheap.
        /// </remarks>
        public SqliteValencyProvider(Func<DbConnection> connectionFactory, string sourceDescription)
        {
            _connectionFactory = connectionFactory;
            _sourceDescription = sourceDescription;

            _constructions = new(LoadConstructions);

            VerifySchemaVersion();
        }

        /// <summary>
        /// Refuses a lexicon built to a schema this version does not read.
        /// </summary>
        /// <exception cref="InvalidOperationException">The versions do not match.</exception>
        /// <remarks>
        /// This matters more now that the dictionary is supplied by the deployment rather than carried in
        /// the package: the two can be updated separately, so they can disagree. Reading a newer lexicon
        /// with an older library does not fail cleanly — a column the queries expect is simply missing,
        /// or worse, present and meaning something else. The tool's validator checks the same thing, but
        /// the validator lives in the tool and a consumer of the library does not have it.
        /// <para>
        /// Checked when the provider is constructed rather than at the first lookup, so a mismatch stops
        /// the application at startup instead of on whichever request first touched a word.
        /// </para>
        /// </remarks>
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

            if (stored == SupportedSchemaVersion.ToString(CultureInfo.InvariantCulture))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Lexikon '{_sourceDescription}' je psaný pro schéma verze {stored ?? "(neuvedeno)"}, "
                + $"tahle verze knihovny čte {SupportedSchemaVersion}. Stáhni slovník odpovídající "
                + "verzi balíčku, nebo balíček aktualizuj.");
        }

        /// <summary>
        /// Gets the lexical entry registered for the supplied lemma.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns>The lexical entry for the lemma, or null when the lemma is not present.</returns>
        /// <remarks>
        /// A lemma held under two word classes yields whichever comes first. Use the overload taking a
        /// <see cref="WordCategory"/> when the class is known.
        /// </remarks>
        public CzechLexicalEntry? GetEntry(string lemma)
            => _entryCache.GetOrAdd(ToKey(lemma), LoadEntry);

        /// <summary>
        /// Gets the lexical entry registered for the supplied lemma in the supplied word class.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="category">The word class to look the lemma up in.</param>
        /// <returns>The lexical entry, or null when the lemma is not present in that class.</returns>
        public CzechLexicalEntry? GetEntry(string lemma, WordCategory category)
            => _entryCache.GetOrAdd(
                $"{category}|{ToKey(lemma)}",
                _ => LoadEntry(ToKey(lemma), category));

        /// <summary>
        /// Gets valency frames registered for the supplied verb lemma.
        /// </summary>
        /// <param name="verbLemma">The verb lemma whose valency frames are requested.</param>
        /// <returns>The valency frames for the lemma, or an empty sequence when no frames are registered.</returns>
        public IEnumerable<ValencyFrame> GetFrames(string verbLemma)
            => _frameCache.GetOrAdd(ToKey(verbLemma), LoadFrames);

        /// <summary>
        /// Gets the light verb constructions headed by the supplied verb.
        /// </summary>
        /// <param name="verbLemma">The verb lemma whose constructions are requested.</param>
        /// <returns>The constructions, or an empty sequence when the verb heads none.</returns>
        public IEnumerable<ConstructionTemplate> GetConstructions(string verbLemma) =>
            _constructions.Value.Where(construction =>
                string.Equals(construction.LightVerbLemma, verbLemma, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Gets the construction registered under the supplied pattern name.
        /// </summary>
        /// <param name="patternName">The name of the pattern.</param>
        /// <returns>The construction, or null when nothing is registered under it.</returns>
        public ConstructionTemplate? GetConstruction(string patternName) =>
            _constructions.Value.FirstOrDefault(construction =>
                string.Equals(construction.Name, patternName, StringComparison.Ordinal));

        /// <summary>
        /// Determines whether the lexicon contains an entry for the supplied lemma.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <returns><see langword="true"/> when the lemma is present in the lexicon; otherwise, <see langword="false"/>.</returns>
        public bool HasEntry(string lemma) => GetEntry(lemma) is not null;

        // Folded here and not by a collation: NOCASE folds ASCII only, so DÁT and dát would be two keys,
        // and a Czech culture collation treats ch as a unit.
        private static string ToKey(string lemma) => lemma.ToLowerInvariant();

        private CzechLexicalEntry? LoadEntry(string key)
        {
            using var connection = OpenConnection();
            using var command = CreateCommand(connection, EntryQuery, key);
            using var reader = command.ExecuteReader();

            return reader.Read() ? ReadEntry(reader) : null;
        }

        private CzechLexicalEntry? LoadEntry(string key, WordCategory category)
        {
            using var connection = OpenConnection();
            using var command = CreateCommand(connection, EntryByCategoryQuery, key);
            AddParameter(command, "@category", category.ToString());

            using var reader = command.ExecuteReader();

            return reader.Read() ? ReadEntry(reader) : null;
        }

        // The slots live in template_json rather than in columns of their own, because a construction is
        // the one thing here that is not a row per fact: the pattern is the unit, it is edited whole, and
        // splitting it across three tables would buy joins and lose the ability to read one at a glance.
        private IReadOnlyList<ConstructionTemplate> LoadConstructions()
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = ConstructionQuery;

            using var reader = command.ExecuteReader();

            var constructions = new List<ConstructionTemplate>();

            while (reader.Read())
            {
                var name = reader.GetString(0);

                constructions.Add(new ConstructionTemplate
                {
                    Name = name,
                    LightVerbLemma = reader.GetString(1),
                    PredicativeNounLemma = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Slots = ConstructionSlots.Parse(reader.GetString(3), name),
                });
            }

            return constructions;
        }

        private IReadOnlyList<ValencyFrame> LoadFrames(string key)
        {
            using var connection = OpenConnection();
            using var command = CreateCommand(connection, FrameQuery, key);
            using var reader = command.ExecuteReader();

            // The join flattens three levels into one result set, folded back in a single pass. Keyed on
            // frame_id, not lu_id — one lexical unit carries a frame per diathesis.
            var frames = new List<(long Id, ValencyFrame Frame)>();
            var slotIdsByFrame = new Dictionary<long, List<long>>();
            var slotsById = new Dictionary<long, ValencySlot>();
            var realizationsBySlot = new Dictionary<long, List<SlotRealization>>();

            while (reader.Read())
            {
                var frameId = reader.GetInt64(FrameId);

                if (!slotIdsByFrame.ContainsKey(frameId))
                {
                    frames.Add((frameId, new ValencyFrame
                    {
                        VerbLemma = reader.GetString(FrameLemma),
                        LuId = reader.GetInt64(FrameLuId),
                        FrameLabel = GetNullableString(reader, FrameSenseLabel),
                        Kind = ParseEnum<ValencyKind>(reader.GetString(FrameKind), "kind"),
                        Diathesis = ParseEnum<Diathesis>(reader.GetString(FrameDiathesis), "diathesis"),
                        IsDefault = reader.GetInt64(FrameIsDefault) != 0,
                        ReflexiveType = ParseEnum<ReflexiveType>(
                            reader.GetString(FrameReflexiveType), "reflexive_type")
                    }));

                    slotIdsByFrame[frameId] = [];
                }

                // Null for a frame with no slots or a slot with no realization — data errors that
                // validate reports, not ones to fail a lookup on.
                if (reader.IsDBNull(SlotId))
                {
                    continue;
                }

                var slotId = reader.GetInt64(SlotId);

                if (!slotsById.ContainsKey(slotId))
                {
                    slotsById[slotId] = new ValencySlot
                    {
                        Functor = ParseEnum<FgdFunctor>(reader.GetString(SlotFunctor), "functor"),
                        CanonicalOrder = (int)reader.GetInt64(SlotCanonicalOrder),
                        Obligatoriness = ParseEnum<Obligatoriness>(reader.GetString(SlotObligatoriness), "obligatoriness"),
                        CanDropContextual = reader.GetInt64(SlotCanDropContextual) != 0,
                        CanDropGeneric = reader.GetInt64(SlotCanDropGeneric) != 0,
                        ControlTarget = reader.IsDBNull(SlotControlTarget)
                            ? null
                            : ParseEnum<FgdFunctor>(reader.GetString(SlotControlTarget), "control_target")
                    };

                    realizationsBySlot[slotId] = [];
                    slotIdsByFrame[frameId].Add(slotId);
                }

                if (reader.IsDBNull(RealizationPreference))
                {
                    continue;
                }

                realizationsBySlot[slotId].Add(new SlotRealization
                {
                    Case = reader.IsDBNull(RealizationCase)
                        ? null
                        : ParseEnum<Case>(reader.GetString(RealizationCase), "morph_case"),
                    Preposition = GetNullableString(reader, RealizationPreposition),
                    ClauseType = GetNullableString(reader, RealizationClauseType),
                    TakesInfinitive = reader.GetInt64(RealizationTakesInfinitive) != 0,
                    Preference = (int)reader.GetInt64(RealizationPreference)
                });
            }

            return frames
                .Select(entry => entry.Frame with
                {
                    Slots = slotIdsByFrame[entry.Id]
                        .Select(slotId => slotsById[slotId] with { Realizations = realizationsBySlot[slotId] })
                        .ToList()
                })
                .ToList();
        }

        private CzechLexicalEntry ReadEntry(DbDataReader reader) => new()
        {
            Lemma = reader.GetString(0),
            Category = ParseEnum<WordCategory>(reader.GetString(1), "category"),
            Gender = reader.IsDBNull(2) ? null : ParseEnum<Gender>(reader.GetString(2), "gender"),
            Pattern = GetNullableString(reader, 3),
            IsAnimate = GetNullableBoolean(reader, 4),
            HasMobileE = GetNullableBoolean(reader, 5),
            HasGenitivePluralShortening = GetNullableBoolean(reader, 6),
            HasEpenthesisInGenitivePlural = GetNullableBoolean(reader, 7),
            IsIndeclinable = GetNullableBoolean(reader, 8),
            IsPluralOnly = GetNullableBoolean(reader, 9),
            IsCountable = GetNullableBoolean(reader, 10),
            PrefersShortForm = GetNullableBoolean(reader, 11),
            VerbClass = reader.IsDBNull(12) ? null : ParseEnum<VerbClass>(reader.GetString(12), "verb_class"),
            Aspect = reader.IsDBNull(13) ? null : ParseEnum<VerbAspect>(reader.GetString(13), "aspect"),
            AspectCounterpart = GetNullableString(reader, 14),
            ReflexiveType = ParseEnum<ReflexiveType>(reader.GetString(15), "reflexive_type"),
            BaseVerbLemma = GetNullableString(reader, 16),
            Stem = GetNullableString(reader, 17),
            PresentStem = GetNullableString(reader, 18),
            PastStem = GetNullableString(reader, 19),
            FutureStem = GetNullableString(reader, 20),
            ImperativeStem = GetNullableString(reader, 21),
            PassiveStem = GetNullableString(reader, 22),
            Infinitive = GetNullableString(reader, 23),
            FormsPassive = GetNullableBoolean(reader, 24),
            Aktionsart = reader.IsDBNull(25) ? null : ParseEnum<Aktionsart>(reader.GetString(25), "aktionsart")
        };

        private DbConnection OpenConnection()
        {
            var connection = _connectionFactory();
            connection.Open();

            return connection;
        }

        private static DbCommand CreateCommand(DbConnection connection, string sql, string key)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@key", key);

            return command;
        }

        private static void AddParameter(DbCommand command, string name, string value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        private static string? GetNullableString(DbDataReader reader, int ordinal)
            => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

        private static bool? GetNullableBoolean(DbDataReader reader, int ordinal)
            => reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal) != 0;

        private TEnum ParseEnum<TEnum>(string value, string column) where TEnum : struct, Enum
            => Enum.TryParse<TEnum>(value, out var parsed)
                ? parsed
                : throw new InvalidOperationException(
                    $"Lexikon '{_sourceDescription}' má ve sloupci {column} hodnotu '{value}', "
                    + $"kterou {typeof(TEnum).Name} nezná. Povolené hodnoty: "
                    + string.Join(", ", Enum.GetNames<TEnum>()) + ".");
    }
}
