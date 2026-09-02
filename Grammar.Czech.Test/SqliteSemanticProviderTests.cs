using Grammar.Core.Enums;
using Grammar.Czech.Providers.SqliteProviders;
using Microsoft.Data.Sqlite;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Checks that <see cref="SqliteSemanticProvider"/> reads semantic_feature and semantic_relation rows
    /// back the way they were written.
    /// </summary>
    /// <remarks>
    /// The shipped lexicon carries no semantic data yet — the tables exist from the schema, empty. These
    /// tests work on a private copy with rows written directly by SQL, the same approach
    /// <see cref="LexiconStemOverrideTests"/> uses for the stem columns, and against two real lu_id values
    /// already in the shipped dictionary so the foreign keys hold.
    /// </remarks>
    [TestClass]
    public sealed class SqliteSemanticProviderTests
    {
        private static string databasePath = null!;
        private static SqliteSemanticProvider provider = null!;
        private static long luIdA;
        private static long luIdB;

        /// <summary>
        /// Copies the shipped lexicon, picks two real senses, and writes a feature and a relation onto them.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            databasePath = Path.Combine(Path.GetTempPath(), $"semantika-{Guid.NewGuid():N}.db");
            File.Copy(
                Path.Combine(AppContext.BaseDirectory, SqliteValencyProvider.DefaultFileName),
                databasePath);

            using (var connection = new SqliteConnection($"Data Source={databasePath}"))
            {
                connection.Open();

                using (var select = connection.CreateCommand())
                {
                    select.CommandText = "SELECT lu_id FROM lexical_unit ORDER BY lu_id LIMIT 2";

                    using var reader = select.ExecuteReader();
                    reader.Read();
                    luIdA = reader.GetInt64(0);
                    reader.Read();
                    luIdB = reader.GetInt64(0);
                }

                using var insert = connection.CreateCommand();
                insert.CommandText = """
                    INSERT INTO semantic_feature (lu_id, feature_name, feature_value, value_kind, source)
                    VALUES (@a, 'animate', 'true', 'Binary', 'manual');

                    INSERT INTO semantic_relation
                        (lu_id_a, lu_id_b, relation_type, antonym_subtype, strength, source)
                    VALUES (@a, @b, 'Antonym', 'Complementary', 0.9, 'manual');
                    """;
                insert.Parameters.AddWithValue("@a", luIdA);
                insert.Parameters.AddWithValue("@b", luIdB);
                insert.ExecuteNonQuery();
            }

            SqliteConnection.ClearAllPools();

            provider = new SqliteSemanticProvider(databasePath);
        }

        /// <summary>
        /// Releases the copy.
        /// </summary>
        [ClassCleanup]
        public static void CleanupClass()
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }

        /// <summary>
        /// A feature written for a sense comes back with its name, value and kind.
        /// </summary>
        [TestMethod]
        public void GetFeatures_SenseWithFeature_ReadsItBack()
        {
            var features = provider.GetFeatures(luIdA).ToList();

            Assert.AreEqual(1, features.Count);
            Assert.AreEqual("animate", features[0].FeatureName);
            Assert.AreEqual("true", features[0].FeatureValue);
            Assert.AreEqual(SemanticValueKind.Binary, features[0].ValueKind);
        }

        /// <summary>
        /// A sense with no features recorded gets an empty sequence, not an error.
        /// </summary>
        [TestMethod]
        public void GetFeatures_SenseWithoutFeatures_IsEmpty()
        {
            var features = provider.GetFeatures(luIdB);

            Assert.AreEqual(0, features.Count());
        }

        /// <summary>
        /// A relation is found from the side stored as lu_id_a, with its antonym subtype and strength.
        /// </summary>
        [TestMethod]
        public void GetRelations_FromSideA_ReadsTheRelation()
        {
            var relation = provider.GetRelations(luIdA).Single();

            Assert.AreEqual(luIdA, relation.LuIdA);
            Assert.AreEqual(luIdB, relation.LuIdB);
            Assert.AreEqual(SemanticRelationType.Antonym, relation.RelationType);
            Assert.AreEqual(AntonymSubtype.Complementary, relation.AntonymSubtype);
            Assert.AreEqual(0.9, relation.Strength);
        }

        /// <summary>
        /// The same relation is found from the side stored as lu_id_b — a caller asking about either
        /// sense of a symmetric relation gets it, not just the one that happens to sort first.
        /// </summary>
        [TestMethod]
        public void GetRelations_FromSideB_ReadsTheSameRelation()
        {
            var relation = provider.GetRelations(luIdB).Single();

            Assert.AreEqual(luIdA, relation.LuIdA);
            Assert.AreEqual(luIdB, relation.LuIdB);
        }

        /// <summary>
        /// A relation with no antonym subtype — a synonym, which has none — comes back null rather than
        /// a default enum member standing in for "not applicable".
        /// </summary>
        [TestMethod]
        public void GetRelations_Synonym_HasNoAntonymSubtype()
        {
            var otherPath = Path.Combine(Path.GetTempPath(), $"semantika-syn-{Guid.NewGuid():N}.db");
            File.Copy(
                Path.Combine(AppContext.BaseDirectory, SqliteValencyProvider.DefaultFileName),
                otherPath);

            try
            {
                using (var connection = new SqliteConnection($"Data Source={otherPath}"))
                {
                    connection.Open();

                    using var insert = connection.CreateCommand();
                    insert.CommandText = """
                        INSERT INTO semantic_relation (lu_id_a, lu_id_b, relation_type, source)
                        VALUES (@a, @b, 'Synonym', 'manual');
                        """;
                    insert.Parameters.AddWithValue("@a", luIdA);
                    insert.Parameters.AddWithValue("@b", luIdB);
                    insert.ExecuteNonQuery();
                }

                SqliteConnection.ClearAllPools();

                var syn = new SqliteSemanticProvider(otherPath);
                var relation = syn.GetRelations(luIdA).Single();

                Assert.AreEqual(SemanticRelationType.Synonym, relation.RelationType);
                Assert.IsNull(relation.AntonymSubtype);
            }
            finally
            {
                SqliteConnection.ClearAllPools();

                if (File.Exists(otherPath))
                {
                    File.Delete(otherPath);
                }
            }
        }
    }
}
