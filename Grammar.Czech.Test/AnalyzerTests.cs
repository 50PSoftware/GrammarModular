using Grammar.Czech.Analyzer;
using Grammar.Czech.Analyzer.Candidates;
using Grammar.Czech.Cli.Sentence;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the generate-and-test lexicon-gap analyzer: tokenization, what counts as already
    /// known, and whether a candidate needs more than the token itself to be proposed.
    /// </summary>
    [TestClass]
    public sealed class AnalyzerTests
    {
        private static IServiceProvider services = null!;
        private static KnownWords known = null!;
        private static NounMatcher nounMatcher = null!;
        private static AdjectiveMatcher adjectiveMatcher = null!;

        /// <summary>
        /// Builds the grammar service graph once for the whole fixture, against the repository's own
        /// lexicon copy — the same one <see cref="CliDraftTests"/> resolves by default.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var collection = new ServiceCollection();

            collection.AddCzechGrammarServices();

            services = collection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
            known = new KnownWords(services);
            nounMatcher = new NounMatcher(
                services.GetRequiredService<CzechNounDeclensionService>(),
                services.GetRequiredService<INounDataProvider>());
            adjectiveMatcher = new AdjectiveMatcher(services.GetRequiredService<CzechAdjectiveDeclensionService>());
        }

        // ── Tokenizer ────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that punctuation is stripped and repeated words are counted, not listed twice.
        /// </summary>
        [TestMethod]
        public void TokenizerCountsRepeatedWordsAndDropsPunctuation()
        {
            var counts = Tokenizer.CountTokens("Pes, pes a pes. Kočka!");

            Assert.AreEqual(3, counts["pes"]);
            Assert.AreEqual(1, counts["kočka"]);
            Assert.IsFalse(counts.ContainsKey(","));
            Assert.IsFalse(counts.ContainsKey("."));
        }

        /// <summary>
        /// Verifies that single-letter tokens — prepositions like "v", "k", "s" and stray initials —
        /// are dropped, since they are never a candidate lemma on their own.
        /// </summary>
        [TestMethod]
        public void TokenizerDropsSingleLetterTokens()
        {
            var counts = Tokenizer.CountTokens("Byl v lese s Janem.");

            Assert.IsFalse(counts.ContainsKey("v"));
            Assert.IsFalse(counts.ContainsKey("s"));
        }

        /// <summary>
        /// Verifies that tokens are case-folded, so "Pes" and "pes" count as the same word.
        /// </summary>
        [TestMethod]
        public void TokenizerFoldsCase()
        {
            var counts = Tokenizer.CountTokens("Pes. pes. PES.");

            Assert.AreEqual(3, counts["pes"]);
        }

        // ── KnownWords ───────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that a closed-class word — here a conjunction — counts as known, even though it
        /// never appears in <c>lemma_entry</c>.
        /// </summary>
        [TestMethod]
        public void KnownWordsRecognizesClosedClassLemma()
        {
            Assert.IsTrue(known.IsKnown("ale"));
        }

        /// <summary>
        /// Verifies that a clitic — held only as a literal list, not a data provider — counts as known.
        /// </summary>
        [TestMethod]
        public void KnownWordsRecognizesClitic()
        {
            Assert.IsTrue(known.IsKnown("jsem"));
        }

        /// <summary>
        /// Verifies that a lemma the dictionary already carries counts as known, case-insensitively.
        /// </summary>
        [TestMethod]
        public void KnownWordsRecognizesLexiconLemma()
        {
            Assert.IsTrue(known.IsKnown("student"));
            Assert.IsTrue(known.IsKnown("Student"));
        }

        /// <summary>
        /// Verifies that a word in neither the lexicon nor a closed class is reported as unknown.
        /// </summary>
        [TestMethod]
        public void KnownWordsReportsGenuineGapAsUnknown()
        {
            Assert.IsFalse(known.IsKnown("nesmyslneslovoxyz"));
        }

        // ── NounMatcher ──────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies the core corroboration rule: a token that only matches itself (no other case or
        /// number form present in the text) is not proposed.
        /// </summary>
        [TestMethod]
        public void NounMatcherRejectsUncorroboratedToken()
        {
            var corpus = new Dictionary<string, int> { ["nesmyslneslovoxyz"] = 1 };

            var candidates = nounMatcher.Match("nesmyslneslovoxyz", corpus);

            Assert.AreEqual(0, candidates.Count);
        }

        /// <summary>
        /// Verifies that a genitive-singular form appearing alongside the nominative-singular reading
        /// is enough corroboration to propose the word — the "pořádek"/"pořádku" case that motivated
        /// the whole design.
        /// </summary>
        [TestMethod]
        public void NounMatcherAcceptsTokenWithCorroboratingCaseForm()
        {
            var corpus = new Dictionary<string, int> { ["pořádek"] = 2, ["pořádku"] = 1 };

            var candidates = nounMatcher.Match("pořádek", corpus);

            Assert.IsTrue(candidates.Count > 0);
            Assert.IsTrue(candidates.All(candidate => candidate.MatchedForms.Contains("pořádku")));
            Assert.IsTrue(candidates.All(candidate => candidate.Score >= 2));
        }

        // ── CandidateRanking ─────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that only the candidates tied for the highest score survive thinning, not merely
        /// sorted ahead of the weaker ones.
        /// </summary>
        [TestMethod]
        public void CandidateRankingDropsLowerScoringSiblingsForSameWord()
        {
            var strong = new MatchCandidate("slovo", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false, ["slovo", "slova", "slovu"]);
            var weak = new MatchCandidate("slovo", Core.Enums.WordCategory.Noun, "město", Core.Enums.Gender.Neuter, null, ["slovo", "slova"]);

            var thinned = CandidateRanking.Thin([strong, weak], maxPerWord: 3);

            Assert.AreEqual(1, thinned.Count);
            Assert.AreEqual("hrad", thinned[0].Pattern);
        }

        /// <summary>
        /// Verifies that ties at the top score are capped to the requested count rather than all kept.
        /// </summary>
        [TestMethod]
        public void CandidateRankingCapsTiedCandidatesPerWord()
        {
            var candidates = new[] { "hrad", "les", "pán", "muž" }
                .Select(pattern => new MatchCandidate("slovo", Core.Enums.WordCategory.Noun, pattern, Core.Enums.Gender.Masculine, false, ["slovo", "slova"]))
                .ToArray();

            var thinned = CandidateRanking.Thin(candidates, maxPerWord: 2);

            Assert.AreEqual(2, thinned.Count);
        }

        // ── AdjectiveMatcher ─────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that an adjective is proposed once its feminine form corroborates the masculine
        /// citation form, using <see cref="CzechAdjectiveDeclensionService.GuessAdjectivePattern"/> to
        /// pick the pattern rather than trying every one.
        /// </summary>
        [TestMethod]
        public void AdjectiveMatcherAcceptsTokenWithCorroboratingGenderForm()
        {
            var corpus = new Dictionary<string, int> { ["hezký"] = 1, ["hezká"] = 1 };

            var candidate = adjectiveMatcher.Match("hezký", corpus);

            Assert.IsNotNull(candidate);
            Assert.IsTrue(candidate.MatchedForms.Contains("hezká"));
        }

        /// <summary>
        /// Verifies that an adjective with no other corroborating form is not proposed.
        /// </summary>
        [TestMethod]
        public void AdjectiveMatcherRejectsUncorroboratedToken()
        {
            var corpus = new Dictionary<string, int> { ["hezký"] = 1 };

            var candidate = adjectiveMatcher.Match("hezký", corpus);

            Assert.IsNull(candidate);
        }

        // ── ProposalWriter ───────────────────────────────────────────────────────

        private static WordProposals TemporaryProposals() =>
            new(Path.Combine(Path.GetTempPath(), $"rozbor-navrhy-test-{Guid.NewGuid():N}.json"));

        /// <summary>
        /// Verifies that a candidate is written as an unconfirmed proposal whose note records the
        /// score and matched forms — the batch source has to stay visibly different from a
        /// hand-typed session word, since it carries much weaker evidence.
        /// </summary>
        [TestMethod]
        public void ProposalWriterWritesUnconfirmedProposalWithScoreInNote()
        {
            var store = TemporaryProposals();
            var candidate = new MatchCandidate(
                "pořádek", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false,
                ["pořádek", "pořádku"]);

            var added = ProposalWriter.WriteNew([candidate], store);

            Assert.AreEqual(1, added);
            var proposal = store.Read().Single();
            Assert.AreEqual("pořádek", proposal.Lemma);
            Assert.IsFalse(proposal.IsConfirmed);
            Assert.IsTrue(proposal.Note!.Contains("skóre 2", StringComparison.OrdinalIgnoreCase));

            store.Clear();
        }

        /// <summary>
        /// Verifies that tied alternate patterns for the same word are recorded in the note rather than
        /// each becoming its own proposal — one lemma is one row in the queue.
        /// </summary>
        [TestMethod]
        public void ProposalWriterListsAlternatePatternsInNote()
        {
            var store = TemporaryProposals();
            var strong = new MatchCandidate("slovo", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false, ["slovo", "slova"]);
            var tied = new MatchCandidate("slovo", Core.Enums.WordCategory.Noun, "les", Core.Enums.Gender.Masculine, false, ["slovo", "slova"]);

            var added = ProposalWriter.WriteNew([strong, tied], store);

            Assert.AreEqual(1, added);
            var proposal = store.Read().Single();
            Assert.IsTrue(proposal.Note!.Contains("les", StringComparison.OrdinalIgnoreCase));

            store.Clear();
        }

        /// <summary>
        /// Verifies that a lemma already in the queue is skipped rather than duplicated — "first
        /// sighting wins", the same rule the live session already follows.
        /// </summary>
        [TestMethod]
        public void ProposalWriterSkipsLemmaAlreadyInQueue()
        {
            var store = TemporaryProposals();
            var candidate = new MatchCandidate(
                "pořádek", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false,
                ["pořádek", "pořádku"]);

            store.Write([new WordProposal { Lemma = "pořádek", IsConfirmed = true }]);

            var added = ProposalWriter.WriteNew([candidate], store);

            Assert.AreEqual(0, added);
            Assert.IsTrue(store.Read().Single().IsConfirmed);

            store.Clear();
        }

        // ── Reporter ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that the CSV a person reviews has a header row and one row per candidate, with the
        /// matched forms space-joined rather than comma-joined so they cannot break the CSV shape.
        /// </summary>
        [TestMethod]
        public void ReporterWritesOneCsvRowPerCandidate()
        {
            var candidate = new MatchCandidate(
                "pořádek",
                Core.Enums.WordCategory.Noun,
                "hrad",
                Core.Enums.Gender.Masculine,
                false,
                ["pořádek", "pořádku"]);

            var corpus = new Dictionary<string, int> { ["pořádek"] = 2 };
            var path = Path.Combine(Path.GetTempPath(), $"rozbor-test-{Guid.NewGuid():N}.csv");

            try
            {
                Reporter.WriteCsv([candidate], corpus, path);
                var lines = File.ReadAllLines(path);

                Assert.AreEqual(2, lines.Length);
                Assert.IsTrue(lines[0].StartsWith("poradi,slovo,"));
                Assert.IsTrue(lines[1].Contains("pořádek"));
                Assert.IsTrue(lines[1].Contains("pořádek pořádku"));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
