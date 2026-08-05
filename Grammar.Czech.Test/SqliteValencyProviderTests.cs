using Grammar.Core.Enums;
using Grammar.Czech.Providers.SqliteProviders;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Checks that the lexicon database gives back what was authored into it.
    /// </summary>
    /// <remarks>
    /// The dictionary used to be two embedded JSON files and is now a SQLite file that has to be copied
    /// next to the assembly. That trades one class of silent failure for another: a resource path that
    /// resolved to nothing became a build that copies nothing, and either way every lemma comes back
    /// absent rather than wrong. These tests read the same entries the old ones did, so the move is
    /// checkable rather than merely assumed.
    /// </remarks>
    [TestClass]
    public sealed class SqliteValencyProviderTests
    {
        private static SqliteValencyProvider provider = null!;

        /// <summary>
        /// Opens the lexicon shipped alongside the test assembly.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _) => provider = new SqliteValencyProvider();

        /// <summary>
        /// Every noun carried over from the retired lexicon.json keeps its gender, pattern and flags.
        /// </summary>
        [DataTestMethod]
        [DataRow("student", Gender.Masculine, "pán", true, false)]
        [DataRow("studentka", Gender.Feminine, "žena", true, false)]
        [DataRow("pes", Gender.Masculine, "pán", true, true)]
        [DataRow("den", Gender.Masculine, "hrad", false, true)]
        [DataRow("otec", Gender.Masculine, "muž", true, true)]
        [DataRow("město", Gender.Neuter, "město", false, false)]
        public void GetEntry_SeededNoun_KeepsItsMorphology(
            string lemma,
            Gender gender,
            string pattern,
            bool isAnimate,
            bool hasMobileE)
        {
            var entry = provider.GetEntry(lemma);

            Assert.IsNotNull(entry, $"Lemma '{lemma}' v lexikonu není.");
            Assert.AreEqual(lemma, entry.Lemma);
            Assert.AreEqual(WordCategory.Noun, entry.Category);
            Assert.AreEqual(gender, entry.Gender);
            Assert.AreEqual(pattern, entry.Pattern);
            Assert.AreEqual(isAnimate, entry.IsAnimate);
            Assert.AreEqual(hasMobileE, entry.HasMobileE);
        }

        /// <summary>
        /// Every verb keeps its aspect and its counterpart.
        /// </summary>
        [DataTestMethod]
        [DataRow("dát", "dát", VerbAspect.Perfective, "dávat")]
        [DataRow("dávat", "trida5", VerbAspect.Imperfective, "dát")]
        [DataRow("vidět", "trida4", VerbAspect.Imperfective, "uvidět")]
        [DataRow("uvidět", "trida4", VerbAspect.Perfective, "vidět")]
        public void GetEntry_SeededVerb_KeepsItsAspect(
            string lemma,
            string pattern,
            VerbAspect aspect,
            string counterpart)
        {
            var entry = provider.GetEntry(lemma);

            Assert.IsNotNull(entry, $"Lemma '{lemma}' v lexikonu není.");
            Assert.AreEqual(WordCategory.Verb, entry.Category);
            Assert.AreEqual(pattern, entry.Pattern);
            Assert.AreEqual(aspect, entry.Aspect);
            Assert.AreEqual(counterpart, entry.AspectCounterpart);
        }

        /// <summary>
        /// An aspect counterpart, where one is recorded, resolves to an entry of the opposite aspect.
        /// </summary>
        /// <remarks>
        /// A counterpart is a lemma and not a foreign key, so nothing in the schema stops it from naming a
        /// word that is absent or that has the same aspect as the word pointing at it. Both would surface
        /// as a wrong future tense rather than as a load error.
        /// </remarks>
        [DataTestMethod]
        [DataRow("dát")]
        [DataRow("dávat")]
        [DataRow("vidět")]
        [DataRow("uvidět")]
        public void GetEntry_AspectCounterpart_ResolvesToTheOppositeAspect(string lemma)
        {
            var entry = provider.GetEntry(lemma)!;
            var counterpart = provider.GetEntry(entry.AspectCounterpart!);

            Assert.IsNotNull(counterpart, $"Protějšek '{entry.AspectCounterpart}' v lexikonu není.");
            Assert.AreNotEqual(entry.Aspect, counterpart.Aspect, "Protějšek má stejný vid.");
            Assert.AreEqual(lemma, counterpart.AspectCounterpart, "Dvojice není obousměrná.");
        }

        /// <summary>
        /// jít records no aspect counterpart, and the absence is a claim rather than a gap.
        /// </summary>
        /// <remarks>
        /// Verbs of motion perfectivize only by prefixation and every prefix adds meaning of its own —
        /// zajít is to drop by, přijít to arrive. The lexicon used to name zajít here, which would have
        /// built the future tense from a verb that means something else.
        /// </remarks>
        [TestMethod]
        public void GetEntry_Jít_HasNoAspectCounterpart()
        {
            var entry = provider.GetEntry("jít")!;

            Assert.AreEqual(VerbAspect.Imperfective, entry.Aspect);
            Assert.IsNull(entry.AspectCounterpart);
        }

        /// <summary>
        /// The adjective carried over too, so the lexicon is not verbs and nouns only.
        /// </summary>
        [TestMethod]
        public void GetEntry_Adjective_KeepsItsPattern()
        {
            var entry = provider.GetEntry("mladý");

            Assert.IsNotNull(entry);
            Assert.AreEqual(WordCategory.Adjective, entry.Category);
            Assert.AreEqual("mladý", entry.Pattern);
        }

        /// <summary>
        /// The transfer frame of dát keeps its four slots, in the order the frame states.
        /// </summary>
        [TestMethod]
        public void GetFrames_Dát_ReturnsTheTransferFrame()
        {
            var frame = provider.GetFrames("dát").Single(candidate => candidate.FrameLabel == "transfer");

            Assert.AreEqual("transfer", frame.FrameLabel);
            Assert.AreEqual(ValencyKind.Verbal, frame.Kind);
            Assert.AreEqual(Diathesis.Active, frame.Diathesis);
            Assert.IsTrue(frame.IsDefault);

            CollectionAssert.AreEqual(
                new[] { FgdFunctor.ACT, FgdFunctor.PAT, FgdFunctor.ADDR, FgdFunctor.DIR3 },
                frame.Slots.Select(slot => slot.Functor).ToArray(),
                "Sloty nepřišly v kanonickém pořadí.");

            var addressee = frame.Slots.Single(slot => slot.Functor == FgdFunctor.ADDR);

            Assert.AreEqual(Obligatoriness.Typical, addressee.Obligatoriness);
            Assert.AreEqual(Case.Dative, addressee.PreferredRealization!.Case);

            var directional = frame.Slots.Single(slot => slot.Functor == FgdFunctor.DIR3);

            Assert.AreEqual("na", directional.PreferredRealization!.Preposition);
            Assert.AreEqual(Case.Accusative, directional.PreferredRealization.Case);

            Assert.AreEqual(ReflexiveType.None, frame.ReflexiveType, "Dát knihu Pavlovi žádnou částici nebere.");
        }

        /// <summary>
        /// The other sense of the same lemma carries the particle the transfer sense does not.
        /// </summary>
        /// <remarks>
        /// The slot assertion is the point of the test as much as the particle is. reflexive_type is read
        /// by a hard-coded column ordinal, so a column added to the query in the wrong place would shift
        /// every field after it — and the particle could still come out right by luck where the slots
        /// could not.
        /// </remarks>
        [TestMethod]
        public void GetFrames_Dát_ReadsTheParticleOffTheConsumptionSense()
        {
            var frame = provider.GetFrames("dát").Single(candidate => candidate.FrameLabel == "konzumace");

            Assert.AreEqual(ReflexiveType.DerivedBenefactive_Si, frame.ReflexiveType);
            Assert.IsFalse(frame.IsDefault, "Výchozí zůstává transfer.");

            CollectionAssert.AreEqual(
                new[] { FgdFunctor.ACT, FgdFunctor.PAT },
                frame.Slots.Select(slot => slot.Functor).ToArray());

            Assert.AreEqual(
                Case.Accusative,
                frame.Slots.Single(slot => slot.Functor == FgdFunctor.PAT).PreferredRealization!.Case);
        }

        /// <summary>
        /// dát and dávat are one lexeme, so the frame is stored once and both lemmas reach it.
        /// </summary>
        /// <remarks>
        /// This is the duplication the database was meant to end. The old valency.json repeated the frame
        /// under each lemma and the two copies had already drifted apart — dát listed a directional slot
        /// that dávat did not — which is what a shared row makes impossible.
        /// </remarks>
        [DataTestMethod]
        [DataRow("dát", "dávat", "transfer")]
        [DataRow("vidět", "uvidět", "perception")]
        public void GetFrames_AspectPair_SharesOneFrame(string first, string second, string label)
        {
            var one = provider.GetFrames(first).Single(frame => frame.FrameLabel == label);
            var other = provider.GetFrames(second).Single(frame => frame.FrameLabel == label);

            Assert.AreEqual(one.LuId, other.LuId, "Vidová dvojice nesdílí lexikální jednotku.");

            CollectionAssert.AreEqual(
                one.Slots.Select(slot => slot.Functor).ToArray(),
                other.Slots.Select(slot => slot.Functor).ToArray());

            // The frame is shared but each lemma is told apart by the lemma it was asked for, so the
            // caller never has to map the perfective back onto the imperfective itself.
            Assert.AreEqual(first, one.VerbLemma);
            Assert.AreEqual(second, other.VerbLemma);
        }

        /// <summary>
        /// jít keeps both of its senses, which is what makes naming a frame label necessary.
        /// </summary>
        [TestMethod]
        public void GetFrames_Jít_ReturnsBothSenses()
        {
            var frames = provider.GetFrames("jít").ToList();

            CollectionAssert.AreEquivalent(
                new[] { "motion", "process" },
                frames.Select(frame => frame.FrameLabel).ToArray());

            var motion = frames.Single(frame => frame.FrameLabel == "motion");

            Assert.AreEqual("do", motion.Slots.Single(slot => slot.Functor == FgdFunctor.DIR3).PreferredRealization!.Preposition);
            Assert.AreEqual("z", motion.Slots.Single(slot => slot.Functor == FgdFunctor.DIR1).PreferredRealization!.Preposition);

            var process = frames.Single(frame => frame.FrameLabel == "process");

            Assert.AreEqual(1, process.Slots.Count, "Rámec process má mít jen ACT.");
        }

        /// <summary>
        /// Lookup ignores letter case, including on letters outside ASCII.
        /// </summary>
        /// <remarks>
        /// The key is folded in C# with ToLowerInvariant rather than by a database collation, because
        /// SQLite NOCASE folds ASCII only and would leave DÁT and dát as two different keys.
        /// </remarks>
        [DataTestMethod]
        [DataRow("DÁT")]
        [DataRow("Dát")]
        [DataRow("dÁt")]
        public void HasEntry_IgnoresCase(string lemma)
            => Assert.IsTrue(provider.HasEntry(lemma), $"'{lemma}' se nenašlo.");

        /// <summary>
        /// A lemma the dictionary does not hold comes back as absent rather than as an error.
        /// </summary>
        [TestMethod]
        public void GetEntry_UnknownLemma_ReturnsNull()
        {
            Assert.IsNull(provider.GetEntry("nesmyslnéslovo"));
            Assert.IsFalse(provider.HasEntry("nesmyslnéslovo"));
            Assert.IsFalse(provider.GetFrames("nesmyslnéslovo").Any());
        }

        /// <summary>
        /// A word with no valency has no frames, and that is not an error either.
        /// </summary>
        [TestMethod]
        public void GetFrames_NounWithoutValency_ReturnsEmpty()
            => Assert.IsFalse(provider.GetFrames("město").Any());

        /// <summary>
        /// A missing database says so, instead of behaving like a dictionary that happens to be empty.
        /// </summary>
        /// <remarks>
        /// SQLite creates the file when asked to open one that is not there, so without this check a build
        /// that failed to copy the lexicon would report every lemma as simply unknown — which reads as a
        /// gap in the data rather than as a broken build.
        /// </remarks>
        [TestMethod]
        public void Constructor_MissingDatabase_Throws()
        {
            var missing = Path.Combine(Path.GetTempPath(), $"chybi-{Guid.NewGuid():N}.db");

            var exception = Assert.ThrowsException<FileNotFoundException>(
                () => new SqliteValencyProvider(missing));

            StringAssert.Contains(exception.Message, missing);
        }

        /// <summary>
        /// A lexicon written for a different schema is refused when the provider is built, not later.
        /// </summary>
        /// <remarks>
        /// The dictionary is supplied by the deployment rather than carried in the package, so the two
        /// are updated separately and can disagree. A newer lexicon read by an older library does not
        /// fail cleanly — a column the queries name is simply absent, or present and meaning something
        /// else — and the tool's validator, which checks the same thing, is not something a consumer of
        /// the library has. Failing at construction puts it at application startup instead of on
        /// whichever request first happened to touch a word.
        /// </remarks>
        [TestMethod]
        public void Constructor_LexiconFromAnotherSchema_Throws()
        {
            var copy = Path.Combine(Path.GetTempPath(), $"jine-schema-{Guid.NewGuid():N}.db");
            File.Copy(Path.Combine(AppContext.BaseDirectory, SqliteValencyProvider.DefaultFileName), copy);

            try
            {
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={copy}"))
                {
                    connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText =
                        "UPDATE lexicon_meta SET meta_value = '99' WHERE meta_key = 'schema_version'";
                    command.ExecuteNonQuery();
                }

                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                var exception = Assert.ThrowsException<InvalidOperationException>(
                    () => new SqliteValencyProvider(copy));

                StringAssert.Contains(exception.Message, "99");
                StringAssert.Contains(
                    exception.Message,
                    SqliteValencyProvider.SupportedSchemaVersion.ToString());
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                File.Delete(copy);
            }
        }
    }
}
