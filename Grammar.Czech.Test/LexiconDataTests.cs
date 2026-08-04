using Grammar.Czech.Helpers;
using Grammar.Czech.Lexicon.Tool;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Checks the dictionary's content for the mistakes a schema cannot catch.
    /// </summary>
    /// <remarks>
    /// A CHECK constraint can say that a gender is one of three values; it cannot say that a noun
    /// declined by <em>žena</em> is not masculine. Those are the errors that survive into the data and
    /// then surface far away, as a form that is quietly wrong rather than as anything failing — which is
    /// what happened with the mobile e of <em>větev</em>.
    /// <para>
    /// Written against the whole dictionary rather than a handful of examples, because the point is to
    /// keep holding as it grows: these checks cost nothing per entry and there are meant to be thousands.
    /// </para>
    /// </remarks>
    [TestClass]
    public sealed class LexiconDataTests
    {
        // Patterns whose grammatical animacy is decided by the pattern itself. Only masculines are
        // listed: for feminines and neuters the flag records natural animacy — žena and kuře are living
        // beings — and no declension consults it, so there is nothing to check.
        private static readonly HashSet<string> AnimatePatterns =
            ["pán", "muž", "předseda", "soudce", "učitel", "občan", "syn", "král", "turista"];

        private static readonly HashSet<string> InanimatePatterns = ["hrad", "les", "stroj"];

        private static readonly Dictionary<string, string> PatternGender = new()
        {
            ["pán"] = "Masculine", ["muž"] = "Masculine", ["předseda"] = "Masculine",
            ["soudce"] = "Masculine", ["učitel"] = "Masculine", ["občan"] = "Masculine",
            ["syn"] = "Masculine", ["král"] = "Masculine", ["turista"] = "Masculine",
            ["hrad"] = "Masculine", ["les"] = "Masculine", ["stroj"] = "Masculine",
            ["žena"] = "Feminine", ["růže"] = "Feminine", ["píseň"] = "Feminine", ["kost"] = "Feminine",
            ["město"] = "Neuter", ["moře"] = "Neuter", ["kuře"] = "Neuter", ["stavení"] = "Neuter",
        };

        // Infinitive ending to conjugation class, for the patterns named after a class. A pattern named
        // after a verb — psát, číst, být — carries explicit stems and is not derivable from the ending.
        private static readonly (string Suffix, string Class)[] VerbClasses =
        [
            ("ovat", "trida3"), ("nout", "trida2"),
            ("át", "trida5"), ("at", "trida5"),
            ("it", "trida4"), ("ět", "trida4"), ("et", "trida4"), ("ít", "trida4"),
        ];

        // Lemmas where the dictionary deliberately contradicts HasLikelyMobileE. The field exists to
        // override the rule — the rule knows only -ec, -ek and -ev, and closed-class stems like pes and
        // den have to be stated — so a disagreement is allowed, but it has to be a decision. Add the
        // lemma here and the test stops asking.
        private static readonly HashSet<string> DeliberateMobileEOverrides =
        [
            // Closed-class stems the rule cannot see: pes → psa, den → dne.
            "pes", "den",

            // -eň drops as well — píseň → písně, třešeň → třešně — but adding it to the rule would take
            // every other -eň noun with it, and the rule has no lemma list to check against.
            "píseň", "třešeň",
        ];

        private static List<Entry> entries = null!;

        /// <summary>
        /// Reads the whole dictionary once.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "grammar.czech.lexicon.db");

            Assert.IsTrue(File.Exists(path), $"Lexikon '{path}' u testů není.");

            var index = LexiconSchema.Get("lemma_entry").Columns
                .Select((name, position) => (name, position))
                .ToDictionary(pair => pair.name, pair => pair.position, StringComparer.Ordinal);

            entries = LexiconJsonExporter.Export(path, 5000)
                .Where(page => page.Table == "lemma_entry")
                .SelectMany(page => page.Rows)
                .Select(row => new Entry(
                    Text(row[index["lemma"]])!,
                    Text(row[index["category"]])!,
                    Text(row[index["gender"]]),
                    Text(row[index["pattern"]]),
                    Flag(row[index["is_animate"]]),
                    Flag(row[index["has_mobile_e"]]),
                    Text(row[index["aspect"]]),
                    Text(row[index["aspect_counterpart"]])))
                .ToList();

            Assert.IsTrue(entries.Count > 100, $"Načetl jsem jen {entries.Count} hesel — něco je špatně.");
        }

        /// <summary>
        /// A noun's gender is the one its declension pattern belongs to.
        /// </summary>
        [TestMethod]
        public void EveryNoun_HasTheGenderOfItsPattern()
            => AssertNone(entries
                .Where(entry => entry.Category == "Noun"
                    && entry.Pattern is not null
                    && PatternGender.TryGetValue(entry.Pattern, out var expected)
                    && entry.Gender != expected)
                .Select(entry => $"{entry.Lemma}: vzor {entry.Pattern} je {PatternGender[entry.Pattern!]}, "
                    + $"heslo má {entry.Gender}"));

        /// <summary>
        /// A masculine noun's animacy is the one its pattern implies.
        /// </summary>
        /// <remarks>
        /// Masculines only. Animacy is what splits pán from hrad, and stating it against the pattern
        /// produces forms from the wrong half of the paradigm.
        /// </remarks>
        [TestMethod]
        public void EveryMasculineNoun_HasTheAnimacyOfItsPattern()
            => AssertNone(entries
                .Where(entry => entry.Category == "Noun"
                    && entry.Gender == "Masculine"
                    && entry.Pattern is not null
                    && entry.IsAnimate is not null
                    && (AnimatePatterns.Contains(entry.Pattern) || InanimatePatterns.Contains(entry.Pattern))
                    && entry.IsAnimate != AnimatePatterns.Contains(entry.Pattern))
                .Select(entry => $"{entry.Lemma}: vzor {entry.Pattern}, is_animate={entry.IsAnimate}"));

        /// <summary>
        /// The mobile-e flag agrees with the rule wherever the rule has an opinion.
        /// </summary>
        /// <remarks>
        /// This is the check that would have caught <em>větev</em>. It was recorded as having no mobile e
        /// while ending in -ev, which the rule reads as having one; nothing noticed until the flag
        /// started reaching the engine, and then the noun declined to <em>*věteve</em>.
        /// <para>
        /// Disagreement is legitimate — the field exists to override the rule — so this does not forbid
        /// it, it just requires that somebody chose it, by naming the lemma in
        /// <see cref="DeliberateMobileEOverrides"/>.
        /// </para>
        /// </remarks>
        [TestMethod]
        public void EveryNoun_AgreesWithTheMobileERule()
            => AssertNone(entries
                .Where(entry => entry.Category == "Noun"
                    && entry.HasMobileE is not null
                    && !DeliberateMobileEOverrides.Contains(entry.Lemma)
                    && entry.HasMobileE != MorphologyHelper.HasLikelyMobileE(entry.Lemma))
                .Select(entry => $"{entry.Lemma}: has_mobile_e={entry.HasMobileE}, "
                    + $"pravidlo říká {MorphologyHelper.HasLikelyMobileE(entry.Lemma)}"));

        /// <summary>
        /// A verb on a class pattern is on the class its infinitive ending implies.
        /// </summary>
        [TestMethod]
        public void EveryVerb_OnAClassPattern_MatchesItsEnding()
            => AssertNone(entries
                .Where(entry => entry.Category == "Verb" && entry.Pattern?.StartsWith("trida") == true)
                .Select(entry => (entry, expected: VerbClasses
                    .FirstOrDefault(pair => entry.Lemma.EndsWith(pair.Suffix, StringComparison.Ordinal))))
                .Where(pair => pair.expected.Class is not null && pair.entry.Pattern != pair.expected.Class)
                .Select(pair => $"{pair.entry.Lemma}: vzor {pair.entry.Pattern}, "
                    + $"zakončení -{pair.expected.Suffix} znamená {pair.expected.Class}"));

        /// <summary>
        /// Every aspect counterpart resolves, carries the opposite aspect, and points back.
        /// </summary>
        /// <remarks>
        /// The counterpart is a lemma rather than a foreign key, so nothing in the schema stops it naming
        /// a word that is absent, or one of the same aspect. Either would surface as a wrong future
        /// tense, which is built from it — jít pointed at zajít for exactly that reason.
        /// </remarks>
        [TestMethod]
        public void EveryAspectCounterpart_ResolvesAndPointsBack()
        {
            var verbs = entries
                .Where(entry => entry.Category == "Verb")
                .ToDictionary(entry => entry.Lemma, StringComparer.Ordinal);

            AssertNone(verbs.Values
                .Where(verb => verb.AspectCounterpart is not null)
                .Select(verb =>
                {
                    if (!verbs.TryGetValue(verb.AspectCounterpart!, out var other))
                    {
                        return $"{verb.Lemma} → '{verb.AspectCounterpart}' ve slovníku není";
                    }

                    if (other.Aspect == verb.Aspect)
                    {
                        return $"{verb.Lemma} → {other.Lemma}: oba {verb.Aspect}";
                    }

                    return other.AspectCounterpart == verb.Lemma
                        ? null
                        : $"{verb.Lemma} → {other.Lemma}, ale zpátky ukazuje na "
                            + $"'{other.AspectCounterpart ?? "(nic)"}'";
                })
                .OfType<string>());
        }

        private static void AssertNone(IEnumerable<string> problems)
        {
            var found = problems.ToList();

            Assert.AreEqual(
                0,
                found.Count,
                $"Nalezeno {found.Count} sporných hesel:\n  " + string.Join("\n  ", found));
        }

        // The exporter reads straight from SQLite, so a row holds CLR values with null for SQL NULL.
        private static string? Text(object? value) => value?.ToString();

        private static bool? Flag(object? value) => value is null ? null : Text(value) != "0";

        private sealed record Entry(
            string Lemma,
            string Category,
            string? Gender,
            string? Pattern,
            bool? IsAnimate,
            bool? HasMobileE,
            string? Aspect,
            string? AspectCounterpart);
    }
}
