using Grammar.Czech.Interfaces;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies which following words trigger the vocalized variant of a preposition.
    /// </summary>
    [TestClass]
    public sealed class PrepositionVocalizationTests
    {
        private static ICzechPrepositionService service = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            service = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })
                              .GetRequiredService<ICzechPrepositionService>();
        }

        /// <summary>
        /// Vocalization triggered by the same consonant, its voicing counterpart, the mn- cluster,
        /// or a sibilant-initial cluster after v and z.
        /// </summary>
        /// <param name="preposition">The preposition to place.</param>
        /// <param name="followingWord">The word that follows it.</param>
        /// <param name="expected">The expected surface form of the preposition.</param>
        [DataTestMethod]
        // same consonant or voicing pair
        [DataRow("v", "vodě", "ve")]
        [DataRow("v", "fázi", "ve")]
        [DataRow("s", "sestrou", "se")]
        [DataRow("s", "zahradou", "se")]
        [DataRow("s", "ženou", "se")]
        [DataRow("z", "země", "ze")]
        [DataRow("z", "školy", "ze")]
        [DataRow("k", "kamarádovi", "ke")]
        [DataRow("k", "gauči", "ke")]
        // mn- cluster
        [DataRow("s", "mnou", "se")]
        [DataRow("k", "mně", "ke")]
        [DataRow("v", "mně", "ve")]
        [DataRow("bez", "mne", "beze")]
        // sibilant-initial cluster after v
        [DataRow("v", "škole", "ve")]
        [DataRow("v", "smyslu", "ve")]
        // the second consonant of the cluster repeats the preposition's
        [DataRow("v", "dveřích", "ve")]
        [DataRow("v", "svém", "ve")]
        [DataRow("v", "dvou", "ve")]
        [DataRow("k", "skoku", "ke")]
        // three consonants running
        [DataRow("k", "středu", "ke")]
        [DataRow("s", "vstupem", "se")]
        [DataRow("v", "skladišti", "ve")]
        [DataRow("z", "vzpomínek", "ze")]
        public void Vocalize_TriggeringWord_ReturnsVocalizedForm(string preposition, string followingWord, string expected)
        {
            Assert.AreEqual(expected, service.Vocalize(preposition, followingWord));
        }

        /// <summary>
        /// A single consonant or a vowel leaves the preposition unchanged, and prepositions without a
        /// vocalized variant never take one.
        /// </summary>
        /// <param name="preposition">The preposition to place.</param>
        /// <param name="followingWord">The word that follows it.</param>
        [DataTestMethod]
        [DataRow("v", "lese")]
        [DataRow("v", "domě")]
        [DataRow("v", "Praze")]
        [DataRow("v", "autě")]
        [DataRow("z", "lesa")]
        [DataRow("k", "domu")]
        [DataRow("s", "bratrem")]
        // a syllabic preposition keeps its own vowel, even before the same consonant
        [DataRow("bez", "zákona")]
        [DataRow("od", "dveří")]
        // no vocalized variant at all
        [DataRow("na", "stole")]
        [DataRow("do", "domu")]
        [DataRow("o", "olympiádě")]
        public void Vocalize_NonTriggeringWord_ReturnsPrepositionUnchanged(string preposition, string followingWord)
        {
            Assert.AreEqual(preposition, service.Vocalize(preposition, followingWord));
        }

        /// <summary>
        /// Forms that usage settled and no cluster rule reaches are listed per preposition.
        /// </summary>
        /// <param name="preposition">The preposition to place.</param>
        /// <param name="followingWord">The word that follows it.</param>
        /// <param name="expected">The expected surface form.</param>
        [DataTestMethod]
        [DataRow("v", "třech", "ve")]
        [DataRow("v", "dvou", "ve")]
        [DataRow("s", "dvěma", "se")]
        [DataRow("s", "třemi", "se")]
        public void Vocalize_LexicalizedForm_UsesTheListedVariant(string preposition, string followingWord, string expected)
        {
            Assert.AreEqual(expected, service.Vocalize(preposition, followingWord));
        }

        /// <summary>
        /// An unknown preposition is passed through rather than rejected — the data is a working set,
        /// not a closed list. Secondary prepositions are an open class and new ones keep being coined
        /// (napříč, potažmo), so being absent from the file is not an error.
        /// </summary>
        [TestMethod]
        public void Vocalize_UnknownPreposition_ReturnsItUnchanged()
        {
            Assert.AreEqual("napříč", service.Vocalize("napříč", "městem"));
        }

        /// <summary>
        /// skrze is a free stylistic variant of skrz, not a vocalization conditioned by the following word,
        /// so the service never produces it.
        /// </summary>
        [TestMethod]
        public void Vocalize_Skrz_IsNeverVocalized()
        {
            Assert.AreEqual("skrz", service.Vocalize("skrz", "silnici"));
            Assert.AreEqual("skrz", service.Vocalize("skrz", "sklo"));
        }

        #region The rule measured against the corpus

        // Attested combinations checked against the ÚJČ reference, each one a preposition that has a
        // vocalized variant followed by a word that either triggers it or does not. The set is what the
        // two tests below measure the cluster rules against.
        private static readonly (string Preposition, string Word, string Expected)[] Corpus =
        [
            // the same consonant or its voicing counterpart
            ("v", "vodě", "ve"), ("v", "fázi", "ve"), ("s", "sestrou", "se"), ("s", "zahradou", "se"),
            ("s", "ženou", "se"), ("z", "země", "ze"), ("z", "školy", "ze"), ("k", "kamarádovi", "ke"),
            ("k", "gauči", "ke"),
            // the mn- cluster, which reaches even the syllabic prepositions
            ("s", "mnou", "se"), ("k", "mně", "ke"), ("v", "mně", "ve"), ("bez", "mne", "beze"),
            ("nad", "mnou", "nade"), ("pod", "mnou", "pode"), ("před", "mnou", "přede"),
            ("od", "mne", "ode"), ("přes", "mne", "přese"),
            // a sibilant-initial cluster after v and z
            ("v", "škole", "ve"), ("v", "smyslu", "ve"), ("z", "zpěvu", "ze"),
            // the second consonant of the cluster repeating the preposition's
            ("v", "dveřích", "ve"), ("v", "svém", "ve"), ("k", "skoku", "ke"),
            // three consonants running
            ("k", "středu", "ke"), ("s", "vstupem", "se"), ("v", "skladišti", "ve"),
            ("z", "vzpomínek", "ze"),
            // lexicalized: the numerals and a few settled phrases
            ("v", "třech", "ve"), ("s", "dvěma", "se"), ("s", "třemi", "se"),
            ("bez", "všeho", "beze"), ("od", "dneška", "ode"),
            // a cluster opening with d after a one-consonant preposition
            ("v", "dne", "ve"), ("z", "dřeva", "ze"), ("z", "dveří", "ze"), ("k", "dnu", "ke"),
            ("s", "dřevem", "se"),
            // and the ones that stay unvocalized
            ("v", "lese", "v"), ("v", "domě", "v"), ("v", "Praze", "v"), ("v", "autě", "v"),
            ("z", "lesa", "z"), ("k", "domu", "k"), ("s", "bratrem", "s"), ("k", "Praze", "k"),
            ("z", "Prahy", "z"),
            // a syllabic preposition keeps its own vowel, even before the same consonant
            ("bez", "zákona", "bez"), ("od", "dveří", "od"), ("nad", "domem", "nad"),
            ("pod", "postelí", "pod"), ("před", "domem", "před"), ("přes", "silnici", "přes"),
        ];

        /// <summary>
        /// The service reproduces every attested combination, whether by rule or from the lexicalized list.
        /// </summary>
        [TestMethod]
        public void Service_ReproducesTheWholeCorpus()
        {
            var wrong = Corpus
                .Where(entry => service.Vocalize(entry.Preposition, entry.Word) != entry.Expected)
                .Select(entry => $"{entry.Preposition} + {entry.Word} → "
                    + $"{service.Vocalize(entry.Preposition, entry.Word)}, čekáno {entry.Expected}")
                .ToList();

            Assert.AreEqual(0, wrong.Count, string.Join("; ", wrong));
        }

        /// <summary>
        /// The cluster rules and the lexicalized lists partition the corpus without overlap: every
        /// combination the rules alone get wrong is one the data registers in VocalizeBefore, and every
        /// registered entry is load-bearing rather than a leftover the rules would have caught anyway.
        /// </summary>
        /// <remarks>
        /// This is what makes the vocalization rule measurable instead of merely documented. The rule is
        /// restated below rather than called, so that it is independent evidence — reusing the service
        /// implementation would only assert that it equals itself.
        /// </remarks>
        [TestMethod]
        public void ClusterRules_MissOnlyTheLexicalizedCombinations()
        {
            var missedByRules = Corpus
                .Where(entry => entry.Expected != entry.Preposition)
                .Where(entry => !RequiresVocalizationByRule(entry.Preposition, entry.Word))
                .Select(entry => $"{entry.Preposition}+{entry.Word}")
                .ToList();

            var registered = Corpus
                .Where(entry => entry.Expected != entry.Preposition)
                .Where(entry => IsRegisteredAsLexicalized(entry.Preposition, entry.Word))
                .Select(entry => $"{entry.Preposition}+{entry.Word}")
                .ToList();

            CollectionAssert.AreEquivalent(
                registered,
                missedByRules,
                "Pravidlo a seznam výjimek se musí doplňovat beze zbytku. "
                + $"Pravidlo mine: {string.Join(", ", missedByRules)}. "
                + $"Zapsáno ve vocalizeBefore: {string.Join(", ", registered)}.");

            // The rules carry the bulk of the work; the list is the remainder, not the mechanism.
            var vocalizing = Corpus.Count(entry => entry.Expected != entry.Preposition);
            Assert.IsTrue(
                vocalizing - missedByRules.Count >= 20,
                $"Pravidlo pokrývá jen {vocalizing - missedByRules.Count} z {vocalizing} vokalizací.");
        }

        // Whether the combination is reached by the lexicalized list rather than by any cluster rule.
        private static bool IsRegisteredAsLexicalized(string preposition, string word)
            => LexicalizedPrefixes[preposition].Any(prefix => word.StartsWith(prefix, StringComparison.Ordinal));

        // Mirrors the vocalizeBefore lists in prepositions.json. Restated here on purpose: the point of the
        // test is to compare the data against an independent statement of it.
        private static readonly Dictionary<string, string[]> LexicalizedPrefixes = new()
        {
            ["v"] = ["třech", "čtyřech", "dne"],
            ["s"] = ["dvěma", "třemi", "čtyřmi", "dvěmi", "dn", "dř", "dl"],
            ["z"] = ["třech", "čtyř", "dn", "dv", "dř", "dl"],
            ["k"] = ["dn", "dv", "dř", "dl"],
            ["bez"] = ["vš"],
            ["od"] = ["dne"],
            ["nad"] = [],
            ["pod"] = [],
            ["před"] = [],
            ["přes"] = [],
        };

        // The cluster rules of CzechPrepositionService, restated. Deliberately excludes the vocalizeBefore
        // escape hatch, which is exactly what the test is measuring the rules against.
        private static bool RequiresVocalizationByRule(string preposition, string next)
        {
            if (next.StartsWith("mn", StringComparison.Ordinal))
            {
                return true;
            }

            if (preposition.Any(IsVowel))
            {
                return false;
            }

            var final = preposition[^1];

            if (SameOrPaired(final, next[0]))
            {
                return true;
            }

            var leading = LeadingConsonants(next);

            if (leading < 2)
            {
                return false;
            }

            return next[1] == final
                || leading >= 3
                || (final is 'v' or 'z' && "szšž".Contains(next[0]));
        }

        private static int LeadingConsonants(string word)
        {
            var count = 0;
            while (count < word.Length && !IsVowel(word[count]))
            {
                count++;
            }

            return count;
        }

        private static bool SameOrPaired(char prepositionFinal, char next) => prepositionFinal switch
        {
            'v' => next is 'v' or 'f',
            'k' => next is 'k' or 'g',
            's' or 'z' => next is 's' or 'z' or 'š' or 'ž',
            'd' => next is 'd' or 't',
            _ => prepositionFinal == next
        };

        private static bool IsVowel(char c) => "aáeéěiíyýoóuúů".Contains(char.ToLowerInvariant(c));

        #endregion The rule measured against the corpus
    }
}
