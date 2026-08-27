using System.IO.Compression;
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
        private static VerbMatcher verbMatcher = null!;
        private static readonly IReadOnlySet<string> noProperNouns = new HashSet<string>();

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
            verbMatcher = new VerbMatcher(services.GetRequiredService<CzechVerbConjugationService>());
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

        /// <summary>
        /// Verifies that a word capitalized mid-sentence and never seen lowercase is flagged as a
        /// likely proper noun — the "Praha" case that was polluting a real article's candidates.
        /// </summary>
        [TestMethod]
        public void FindLikelyProperNounsFlagsWordCapitalizedMidSentenceAndNeverLowercase()
        {
            var properNouns = Tokenizer.FindLikelyProperNouns("Byl jsem v Praze. Praha je krásná. Miluji Prahu.");

            Assert.IsTrue(properNouns.Contains("praze"));
            Assert.IsTrue(properNouns.Contains("prahu"));
        }

        /// <summary>
        /// Verifies that sentence-initial capitalization alone does not flag a word — every Czech
        /// sentence starts capitalized, so that position proves nothing about the word itself.
        /// </summary>
        [TestMethod]
        public void FindLikelyProperNounsIgnoresSentenceInitialCapitalization()
        {
            var properNouns = Tokenizer.FindLikelyProperNouns("Pes štěkal. Pes běžel. Pes spal.");

            Assert.IsFalse(properNouns.Contains("pes"));
        }

        /// <summary>
        /// Verifies that a word seen lowercase anywhere in the text is not flagged, even if it is also
        /// capitalized mid-sentence somewhere else — a common word that merely got capitalized once
        /// (a heading, an emphasis) should not be read as a name.
        /// </summary>
        [TestMethod]
        public void FindLikelyProperNounsIgnoresWordAlsoSeenLowercase()
        {
            var properNouns = Tokenizer.FindLikelyProperNouns("Měl velký Dům. Ten dům byl starý.");

            Assert.IsFalse(properNouns.Contains("dům"));
        }

        // ── DocumentReader ───────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that a plain .txt file is still read as-is — the extension switch's fallback arm.
        /// </summary>
        [TestMethod]
        public void DocumentReaderReadsPlainTextUnchanged()
        {
            var path = Path.GetTempFileName();

            try
            {
                File.WriteAllText(path, "Obyčejný text.");

                Assert.AreEqual("Obyčejný text.", DocumentReader.ReadText(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies that a .docx's own paragraphs (word/document.xml, w:p/w:t) come back as separate
        /// lines — built as a real zip-of-XML on the fly rather than checking in a binary fixture,
        /// since the whole point is exercising the same zip-plus-XML shape a real Word file has.
        /// </summary>
        [TestMethod]
        public void DocumentReaderExtractsParagraphsFromDocx()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");

            try
            {
                using (var archive = new ZipArchive(File.Create(path), ZipArchiveMode.Create))
                {
                    var entry = archive.CreateEntry("word/document.xml");
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write("""
                        <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                          <w:body>
                            <w:p><w:r><w:t>První odstavec.</w:t></w:r></w:p>
                            <w:p><w:r><w:t>Druhý </w:t></w:r><w:r><w:t>odstavec.</w:t></w:r></w:p>
                          </w:body>
                        </w:document>
                        """);
                }

                Assert.AreEqual("První odstavec.\nDruhý odstavec.", DocumentReader.ReadText(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies that an .odt's own paragraphs (content.xml, text:p) come back as separate lines,
        /// including text split across a text:span the way real formatting produces.
        /// </summary>
        [TestMethod]
        public void DocumentReaderExtractsParagraphsFromOdt()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.odt");

            try
            {
                using (var archive = new ZipArchive(File.Create(path), ZipArchiveMode.Create))
                {
                    var entry = archive.CreateEntry("content.xml");
                    using var writer = new StreamWriter(entry.Open());
                    writer.Write("""
                        <office:document-content
                            xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
                            xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">
                          <office:body>
                            <office:text>
                              <text:p>První odstavec.</text:p>
                              <text:p>Druhý <text:span>odstavec</text:span>.</text:p>
                            </office:text>
                          </office:body>
                        </office:document-content>
                        """);
                }

                Assert.AreEqual("První odstavec.\nDruhý odstavec.", DocumentReader.ReadText(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verifies that a .docx missing word/document.xml — not actually a Word file, whatever its
        /// extension claims — fails with a clear message instead of a bare zip- or XML-parsing error.
        /// </summary>
        [TestMethod]
        public void DocumentReaderRejectsDocxWithoutDocumentXml()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");

            try
            {
                using (var archive = new ZipArchive(File.Create(path), ZipArchiveMode.Create))
                {
                    archive.CreateEntry("not-a-word-document.txt");
                }

                Assert.ThrowsException<InvalidOperationException>(() => DocumentReader.ReadText(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        // ── WikipediaReader ──────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that a normal MediaWiki extract response yields the article's plain text — the
        /// only part of <see cref="WikipediaReader.FetchArticleTextAsync"/> testable without a live
        /// network call, since <see cref="WikipediaReader.ParseExtract"/> only ever needs the response
        /// body MediaWiki already returned.
        /// </summary>
        [TestMethod]
        public void WikipediaReaderParsesExtractFromNormalResponse()
        {
            var body = """
                {"batchcomplete":"","query":{"pages":{"12345":{"pageid":12345,"ns":0,
                    "title":"Vesmír","extract":"Vesmír je souhrn veškeré hmoty a energie."}}}}
                """;

            Assert.AreEqual("Vesmír je souhrn veškeré hmoty a energie.", WikipediaReader.ParseExtract(body, "Vesmír"));
        }

        /// <summary>
        /// Verifies that a missing article — MediaWiki's own <c>"missing":""</c> marker on the page
        /// object, not an HTTP error — fails with a message naming the article, not a raw JSON error.
        /// </summary>
        [TestMethod]
        public void WikipediaReaderRejectsMissingArticle()
        {
            var body = """
                {"batchcomplete":"","query":{"pages":{"-1":{"ns":0,
                    "title":"Xyzabcneexistuje","missing":""}}}}
                """;

            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => WikipediaReader.ParseExtract(body, "Xyzabcneexistuje"));
            StringAssert.Contains(exception.Message, "Xyzabcneexistuje");
        }

        /// <summary>
        /// Verifies that a real page with no extract at all — a disambiguation page, which MediaWiki
        /// still returns as a normal page object — is rejected as clearly as an outright missing one,
        /// rather than handing an empty string down the whole analysis pipeline.
        /// </summary>
        [TestMethod]
        public void WikipediaReaderRejectsArticleWithNoExtract()
        {
            var body = """
                {"batchcomplete":"","query":{"pages":{"999":{"pageid":999,"ns":0,
                    "title":"Praha (rozcestník)","extract":""}}}}
                """;

            Assert.ThrowsException<InvalidOperationException>(
                () => WikipediaReader.ParseExtract(body, "Praha (rozcestník)"));
        }

        // ── BenchmarkReporter ────────────────────────────────────────────────────

        /// <summary>
        /// Verifies the ordinary case: confirmed and rejected rozbor proposals are counted, the success
        /// rate is confirmed-over-decided, and a proposal from a live gramatika session (no "Z rozbor"
        /// note) is not counted at all — the file both write to is the same one, and only rozbor's own
        /// track record is what this measures.
        /// </summary>
        [TestMethod]
        public void BenchmarkReporterCountsOnlyRozborProposalsBySourceMarker()
        {
            var proposals = new List<WordProposal>
            {
                new() { Lemma = "dobreslovo1", IsConfirmed = true, Note = "Z rozbor (dávkový rozbor textu). Skóre 5." },
                new() { Lemma = "dobreslovo2", IsConfirmed = true, Note = "Z rozbor (dávkový rozbor textu). Skóre 4." },
                new() { Lemma = "spatneslovo", IsRejected = true, Note = "Z rozbor (dávkový rozbor textu). Skóre 2." },
                new() { Lemma = "nerozhodnuto", Note = "Z rozbor (dávkový rozbor textu). Skóre 2." },
                new() { Lemma = "ze-sezeni", IsConfirmed = true },
            };

            var result = BenchmarkReporter.Summarize(proposals);

            Assert.AreEqual(2, result.Confirmed);
            Assert.AreEqual(1, result.Rejected);
            Assert.AreEqual(1, result.Undecided);
            Assert.AreEqual(3, result.Decided);
            Assert.AreEqual(2.0 / 3.0, result.SuccessRate!.Value, 0.0001);
        }

        /// <summary>
        /// Verifies that a success rate is not reported at all when nothing has been decided — the
        /// whole reason <see cref="WordProposal.IsRejected"/> exists is so an unreviewed backlog cannot
        /// be silently counted as failure, and reporting 0% here would do exactly that.
        /// </summary>
        [TestMethod]
        public void BenchmarkReporterReportsNoSuccessRateWhenNothingDecided()
        {
            var proposals = new List<WordProposal>
            {
                new() { Lemma = "nerozhodnuto", Note = "Z rozbor (dávkový rozbor textu). Skóre 2." },
            };

            var result = BenchmarkReporter.Summarize(proposals);

            Assert.AreEqual(0, result.Decided);
            Assert.IsNull(result.SuccessRate);
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
        /// Verifies that a declined form of a registered pronoun counts as known, not just its bare
        /// lemma — "který" is registered (Pronouns/patterns.json, declining as the adjective pattern
        /// mladý), but "která"/"kterému" are not separate entries, only its declension. A real article
        /// found this: AdjectiveMatcher folded those gender endings straight back to "který" and
        /// proposed it as a gap, because only the lemma itself, never its forms, had been added.
        /// </summary>
        [TestMethod]
        public void KnownWordsRecognizesDeclinedFormOfPronoun()
        {
            Assert.IsTrue(known.IsKnown("která"));
            Assert.IsTrue(known.IsKnown("kterému"));
            Assert.IsTrue(known.IsKnown("kterých"));
        }

        /// <summary>
        /// Verifies that a declined form of a registered numeral counts as known too, the same fix
        /// applied to the other closed class with its own irregular paradigm.
        /// </summary>
        [TestMethod]
        public void KnownWordsRecognizesDeclinedFormOfNumeral()
        {
            Assert.IsTrue(known.IsKnown("jednoho"));
        }

        /// <summary>
        /// Verifies that an adverb's comparative and superlative count as known, not just the positive
        /// — the same shape of gap as the pronoun one, found by checking the rest of the closed classes
        /// on request rather than waiting for another real article to surface it.
        /// </summary>
        [TestMethod]
        public void KnownWordsRecognizesAdverbComparativeAndSuperlative()
        {
            Assert.IsTrue(known.IsKnown("rychleji"));
            Assert.IsTrue(known.IsKnown("nejrychleji"));
        }

        /// <summary>
        /// Verifies that a verb named only in an interjection's DerivedVerb field — "hop" names
        /// "hopnout", which is in no lexicon entry — counts as known, infinitive and conjugated alike.
        /// This is not a missing-forms gap like the others; the verb was not registered anywhere at
        /// all, so GuessVerbClass has to place it (reliably, since these are all -nout coinages).
        /// </summary>
        [TestMethod]
        public void KnownWordsRecognizesInterjectionDerivedVerb()
        {
            Assert.IsTrue(known.IsKnown("hopnout"));
            // trida2's past stem equals the present stem (CzechWordStructureResolver.DeriveTrida2), so
            // the generated past tense is the short literary "hopl", not the colloquial "hopnul" — the
            // same approximation the resolver already documents for every -nout verb, not something
            // specific to a derived-from-an-interjection one.
            Assert.IsTrue(known.IsKnown("hopl"));
        }

        /// <summary>
        /// Verifies that a preposition's vocalized variant (v/ve, s/se, k/ke...) counts as known.
        /// </summary>
        [TestMethod]
        public void KnownWordsRecognizesVocalizedPreposition()
        {
            Assert.IsTrue(known.IsKnown("ve"));
            Assert.IsTrue(known.IsKnown("ke"));
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
        /// Verifies that an inflected form of an already-known noun counts as known too — otherwise a
        /// text repeating "město" across cases proposes "měst"/"města"/"městu" as new lemmas in their
        /// own right, which is exactly the false-positive flood a real article surfaced.
        /// </summary>
        [TestMethod]
        public void KnownWordsRecognizesInflectedFormOfLexiconNoun()
        {
            Assert.IsTrue(known.IsKnown("měst"));
            Assert.IsTrue(known.IsKnown("městu"));
            Assert.IsTrue(known.IsKnown("městy"));
        }

        /// <summary>
        /// Verifies that a conjugated form of an already-known verb counts as known too — "dávat" is a
        /// class-5 verb in seed.000.sql, so its present tense "dává" must not turn back up as a
        /// candidate the way "měst" once did for nouns.
        /// </summary>
        [TestMethod]
        public void KnownWordsRecognizesConjugatedFormOfLexiconVerb()
        {
            Assert.IsTrue(known.IsKnown("dává"));
            Assert.IsTrue(known.IsKnown("dával"));
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

            var candidates = nounMatcher.Match("nesmyslneslovoxyz", corpus, noProperNouns);

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

            var candidates = nounMatcher.Match("pořádek", corpus, noProperNouns);

            Assert.IsTrue(candidates.Count > 0);
            Assert.IsTrue(candidates.All(candidate => candidate.MatchedForms.Contains("pořádku")));
            Assert.IsTrue(candidates.All(candidate => candidate.Score >= 2));
        }

        /// <summary>
        /// Verifies the reconstruction path: a token shaped like a plural case of some pattern, with no
        /// nominative singular anywhere in the text, still resolves to the "kandidát" hypothesis once a
        /// second plural form corroborates it — the case that motivated reconstruction for nouns at all.
        /// </summary>
        [TestMethod]
        public void NounMatcherReconstructsNominativeFromPluralToken()
        {
            var corpus = new Dictionary<string, int> { ["kandidáti"] = 1, ["kandidátů"] = 1 };

            var candidates = nounMatcher.Match("kandidáti", corpus, noProperNouns);

            Assert.IsTrue(candidates.Any(candidate => candidate.Lemma == "kandidát"));
        }

        /// <summary>
        /// Verifies that a reconstructed lemma shaped exactly like a mobile-e word — "jev" has the same
        /// single-syllable, vowel-between-consonants shape as "pes" — is not lost to the mobile-e guess.
        /// Left to the default, HasLikelyMobileE reads "jev" as mobile-e and generates "jvu" for what
        /// should be "jevu", so the only slot that ever matched was "jev" itself and the hypothesis
        /// never reached two — a real, correctly reconstructed lemma disappearing on a real article.
        /// </summary>
        [TestMethod]
        public void NounMatcherFindsReconstructedLemmaDespiteMobileEGuess()
        {
            var corpus = new Dictionary<string, int> { ["jevy"] = 1, ["jevu"] = 1, ["jevů"] = 1 };

            var candidates = nounMatcher.Match("jevy", corpus, noProperNouns);

            Assert.IsTrue(candidates.Any(candidate => candidate.Lemma == "jev" && candidate.Pattern == "hrad"));
        }

        /// <summary>
        /// Verifies that a proper noun's own inflected spelling does not count as corroboration, even
        /// though <see cref="Tokenizer.CountTokens"/> puts it in <c>corpus</c> regardless of
        /// capitalization. "polskou" (instrumental of the ordinary adjective "polský", not a name)
        /// reconstructs to "polska" under the turista pattern — which happens to be the real genitive
        /// singular of the proper noun "Polsko", corroborated by "polska"/"polsku"/"polsko" turning up
        /// in the corpus dozens of times on a real article about the Second World War. None of that is
        /// the adjective's own paradigm; excluding <c>properNouns</c> from what counts as a match is
        /// what makes the hypothesis fail to corroborate the way a genuine one still would.
        /// </summary>
        [TestMethod]
        public void NounMatcherExcludesProperNounSpellingsFromCorroboration()
        {
            var corpus = new Dictionary<string, int>
            {
                ["polskou"] = 1, ["polska"] = 15, ["polsku"] = 5, ["polsko"] = 3,
            };
            var properNouns = new HashSet<string> { "polska", "polsku", "polsko" };

            var candidates = nounMatcher.Match("polskou", corpus, properNouns);

            Assert.IsFalse(candidates.Any(candidate => candidate.Lemma == "polska"));
        }

        /// <summary>
        /// Verifies that a token ending in ě or é is rejected outright, even with corroborating
        /// "evidence" in the corpus — no regular noun pattern's own nominative singular ends either
        /// way, so a token shaped like one is never a citation form to begin with. "nekonečně" (an
        /// adverb) scored as a noun this way on a real article, borrowing part of the real adjective
        /// "nekonečný"'s own declension (nekonečnou, nekonečných).
        /// </summary>
        [TestMethod]
        public void NounMatcherRejectsTokenEndingInEWithCaron()
        {
            var corpus = new Dictionary<string, int> { ["nekonečně"] = 3, ["nekonečnou"] = 1, ["nekonečné"] = 1 };

            Assert.AreEqual(0, nounMatcher.Match("nekonečně", corpus, noProperNouns).Count);
            Assert.AreEqual(0, nounMatcher.Match("nekonečné", corpus, noProperNouns).Count);
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

        /// <summary>
        /// Verifies that repeated candidates for the same lemma and pattern collapse into one row —
        /// the case that arises once <see cref="AdjectiveMatcher"/> normalizes several source tokens
        /// (celý/celá/celé) to the same hypothesis.
        /// </summary>
        [TestMethod]
        public void CandidateRankingCollapsesRepeatedSamePatternCandidate()
        {
            var first = new MatchCandidate("celý", Core.Enums.WordCategory.Adjective, "mladý", null, null, ["celý", "celého"]);
            var second = new MatchCandidate("celý", Core.Enums.WordCategory.Adjective, "mladý", null, null, ["celý", "celého"]);

            var thinned = CandidateRanking.Thin([first, second], maxPerWord: 3);

            Assert.AreEqual(1, thinned.Count);
        }

        /// <summary>
        /// Verifies that a candidate whose lemma the lexicon already knows is dropped, even though the
        /// token it was reconstructed from never literally appears as that lemma — the gap the per-token
        /// known-word check cannot close on its own.
        /// </summary>
        [TestMethod]
        public void DropAlreadyHandledDropsCandidateAlreadyKnownToTheLexicon()
        {
            var candidate = new MatchCandidate("dělat", Core.Enums.WordCategory.Verb, "trida5", null, null, ["dělal"]);

            var dropped = CandidateRanking.DropAlreadyHandled([candidate], lemma => lemma == "dělat", new HashSet<string>());

            Assert.AreEqual(0, dropped.Count);
        }

        /// <summary>
        /// Verifies that a candidate already sitting in navrhy.json under the same lemma is dropped too
        /// — a person or an earlier run already proposed it, so it should not clutter the CSV/GUI list a
        /// second time even though it is silently skipped again at write time regardless.
        /// </summary>
        [TestMethod]
        public void DropAlreadyHandledDropsCandidateAlreadyInProposalQueue()
        {
            var candidate = new MatchCandidate("pořádek", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false, ["pořádek"]);

            var dropped = CandidateRanking.DropAlreadyHandled([candidate], _ => false, new HashSet<string> { "pořádek" });

            Assert.AreEqual(0, dropped.Count);
        }

        /// <summary>
        /// Verifies that a genuinely new candidate — neither known nor already proposed — survives.
        /// </summary>
        [TestMethod]
        public void DropAlreadyHandledKeepsGenuinelyNewCandidate()
        {
            var candidate = new MatchCandidate("pořádek", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false, ["pořádek"]);

            var dropped = CandidateRanking.DropAlreadyHandled([candidate], _ => false, new HashSet<string>());

            Assert.AreEqual(1, dropped.Count);
        }

        /// <summary>
        /// Verifies that a vowel-ending noun hypothesis is dropped when a same-pattern candidate for
        /// the consonant-stripped spelling scores at least as well — "zápasí" (really the verb form,
        /// not a noun) generates the same oblique-case forms as the real noun "zápas" once a suffix is
        /// appended, since no noun pattern declares a nominative-singular ending and every other case
        /// strips a trailing vowel unconditionally (ExtractNounRoot). This was found on a real article.
        /// </summary>
        [TestMethod]
        public void CandidateRankingDropsVowelEndingNounDuplicate()
        {
            var real = new MatchCandidate("zápas", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false, ["zápas", "zápasu", "zápase", "zápasem"]);
            var spurious = new MatchCandidate("zápasí", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false, ["zápasí", "zápasu", "zápase"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([real, spurious], _ => false);

            Assert.AreEqual(1, dropped.Count);
            Assert.AreEqual("zápas", dropped[0].Lemma);
        }

        /// <summary>
        /// Verifies that the vowel-ending candidate survives when it strictly outscores its
        /// consonant-stripped sibling — the shape a genuine finding with no real sibling looks like.
        /// </summary>
        [TestMethod]
        public void CandidateRankingKeepsVowelEndingNounWithNoWeakerSibling()
        {
            var candidate = new MatchCandidate("moře", Core.Enums.WordCategory.Noun, "moře", Core.Enums.Gender.Neuter, null, ["moře", "moři", "mořem"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([candidate], _ => false);

            Assert.AreEqual(1, dropped.Count);
        }

        /// <summary>
        /// Verifies the case exact-spelling comparison would miss: "změní" (really the verb form) and
        /// the real noun "změna" have different trailing vowels, so neither is the other minus one
        /// character — but stripping "í" and stripping "a" both land on the same root "změn", which is
        /// what should drop the weaker one.
        /// </summary>
        [TestMethod]
        public void CandidateRankingDropsNounDuplicateSharingRootWithDifferentVowel()
        {
            var real = new MatchCandidate("změna", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, ["změna", "změny", "změnu", "změně"]);
            var spurious = new MatchCandidate("změní", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, ["změní", "změny", "změnu"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([real, spurious], _ => false);

            Assert.AreEqual(1, dropped.Count);
            Assert.AreEqual("změna", dropped[0].Lemma);
        }

        /// <summary>
        /// Verifies the tie self-consistency and shorter-wins both let through: "varianta" (žena, ends
        /// "a" as žena's own pattern expects) and "varianto" (město, ends "o" as město's own pattern
        /// expects) both pass the shape check on their own pattern, both score identically off the same
        /// oblique forms, and both are eight letters — but "varianta" is the one actually written in the
        /// text (its own citation-form spelling is one of its matched forms), while "varianto" is a pure
        /// reconstruction nobody wrote. Found on a real article about internet and chess.
        /// </summary>
        [TestMethod]
        public void CandidateRankingPrefersAttestedCitationFormOnCrossPatternTie()
        {
            var zena = new MatchCandidate("varianta", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, ["varianta", "varianty", "variantě", "variant"]);
            var mesto = new MatchCandidate("varianto", Core.Enums.WordCategory.Noun, "město", Core.Enums.Gender.Neuter, null, ["varianta", "variantě", "variant", "varianty"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([zena, mesto], _ => false);

            Assert.AreEqual(1, dropped.Count);
            Assert.AreEqual("varianta", dropped[0].Lemma);
        }

        /// <summary>
        /// Verifies the tie-break needed once <see cref="NounMatcher"/> reconstructs a nominative
        /// singular from a plural token: "kandidát" (reconstructed) and "kandidáti" (the plural token
        /// tried as its own lemma) share the same root and, when "kandidát" itself never appears in the
        /// text, the identical paradigm and score — so the shorter, genuinely singular spelling should
        /// win rather than both surviving as a tie.
        /// </summary>
        [TestMethod]
        public void CandidateRankingPrefersShorterLemmaOnTiedRoot()
        {
            var reconstructed = new MatchCandidate("kandidát", Core.Enums.WordCategory.Noun, "pán", Core.Enums.Gender.Masculine, true, ["kandidáti", "kandidátů", "kandidáta"]);
            var direct = new MatchCandidate("kandidáti", Core.Enums.WordCategory.Noun, "pán", Core.Enums.Gender.Masculine, true, ["kandidáti", "kandidátů", "kandidáta"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([reconstructed, direct], _ => false);

            Assert.AreEqual(1, dropped.Count);
            Assert.AreEqual("kandidát", dropped[0].Lemma);
        }

        /// <summary>
        /// Verifies the tie the length rule alone cannot break: "vrstva", "vrstvu", "vrstvy" and
        /// "vrstvo" all reduce to the same root and, once any one of them turns up more than once in
        /// the same text, all generate the identical žena-pattern paradigm and therefore the identical
        /// score — and all four are six letters long, so shorter-wins picks nothing. žena's own
        /// nominative singular ends in "a", so only "vrstva" is shaped the way its own winning pattern
        /// says a nominative singular should be; the other three are real inflected forms of the same
        /// word standing in the citation slot. Found on a real article about global warming.
        /// </summary>
        [TestMethod]
        public void CandidateRankingPrefersSelfConsistentEndingOnSameLengthTie()
        {
            string[] forms = ["vrstvy", "vrstvu", "vrstev", "vrstvách"];
            var a = new MatchCandidate("vrstva", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, forms);
            var u = new MatchCandidate("vrstvu", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, forms);
            var y = new MatchCandidate("vrstvy", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, forms);
            var o = new MatchCandidate("vrstvo", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, forms);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([a, u, y, o], _ => false);

            Assert.AreEqual(1, dropped.Count);
            Assert.AreEqual("vrstva", dropped[0].Lemma);
        }

        /// <summary>
        /// Verifies the case NounRoot's own vowel-stripping rule cannot connect on its own: "forem"
        /// (genitive plural of "forma", with a vkladné e) ends in a consonant, so NounRoot leaves it
        /// whole, while "forma" strips down to "form" — two different root keys for the same word,
        /// unless the shared matched form "forem" (present in both candidates' tvary) is what ties the
        /// groups together instead. "forma" never appears literally in the text (cetnost 0), only
        /// "forem" does, so this is the reconstructed-but-never-seen-standalone shape of the same bug
        /// "kandidát" hit. Found on a real article about the universe.
        /// </summary>
        [TestMethod]
        public void CandidateRankingMergesMobileEDuplicateViaSharedMatchedForm()
        {
            string[] foremForms = ["forem", "formy", "formě", "formu", "formou", "formách", "formami"];
            string[] formaForms = ["formy", "formě", "formu", "formou", "forem", "formách", "formami"];
            var forem = new MatchCandidate("forem", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, foremForms);
            var forma = new MatchCandidate("forma", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, formaForms);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([forem, forma], _ => false);

            Assert.AreEqual(1, dropped.Count);
            Assert.AreEqual("forma", dropped[0].Lemma);
        }

        /// <summary>
        /// Verifies that a possessive-adjective form is dropped once its underlying noun stem is found
        /// elsewhere in the same run — "papežovo"/"papežova" (papežův, "the pope's") scored as three
        /// separate fake nouns on a real article about Jan Hus, entirely explained by "papež" (a real
        /// candidate a few rows above them on the same run) plus the possessive suffix "-ov".
        /// </summary>
        [TestMethod]
        public void CandidateRankingDropsPossessiveAdjectiveDerivativeWhenStemIsFound()
        {
            var papez = new MatchCandidate("papež", Core.Enums.WordCategory.Noun, "král", Core.Enums.Gender.Masculine, true, ["papež", "papeže", "papeži", "papežem"]);
            var papezovo = new MatchCandidate("papežovo", Core.Enums.WordCategory.Noun, "město", Core.Enums.Gender.Neuter, null, ["papežovo", "papežova", "papežovu"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([papez, papezovo], _ => false);

            Assert.AreEqual(1, dropped.Count);
            Assert.AreEqual("papež", dropped[0].Lemma);
        }

        /// <summary>
        /// Verifies that a noun candidate explained away by an adjective's own instrumental singular
        /// falls below the corroboration threshold and is dropped — "rozsáhla" (žena, not a real word)
        /// scored on "rozsáhlé"/"rozsáhlou", but "rozsáhlou" is really the real adjective "rozsáhlý"'s
        /// own accusative/instrumental feminine singular, spelled identically to žena's own
        /// instrumental singular ending. Once that shared form no longer counts twice, "rozsáhla" has
        /// only one matched form left and falls below the threshold on its own — no root-sharing or
        /// self-consistency check needed, since it was never tied against anything to begin with. Found
        /// on a real article about evolution, alongside "rychla"/"rychlý" and "cela"/"celý" doing the
        /// same thing.
        /// </summary>
        [TestMethod]
        public void CandidateRankingDropsNounExplainedByAdjectiveInstrumentalOverlap()
        {
            var adjective = new MatchCandidate("rozsáhlý", Core.Enums.WordCategory.Adjective, "mladý", null, null, ["rozsáhlá", "rozsáhlou"]);
            var noun = new MatchCandidate("rozsáhla", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, ["rozsáhle", "rozsáhlou"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([adjective, noun], _ => false);

            Assert.AreEqual(1, dropped.Count);
            Assert.AreEqual("rozsáhlý", dropped[0].Lemma);
        }

        /// <summary>
        /// Verifies that an adjective-explained sibling only drops itself out of a merged root group,
        /// not the whole group — the first version of this check dropped the entire group whenever any
        /// member was adjective-explained, the same way isKnown and the possessive check still do
        /// (correctly, for those two: a known or possessive-derived root really does mean the whole
        /// group is someone else's word). On a real article about evolution, "skupina" (žena, real,
        /// score 8) shared a merged root group with "skupinový" (mistried as a noun under turista,
        /// fully explained by the real adjective "skupinový") through
        /// <see cref="MergeByRootAndSharedForms"/>'s own shared-form linking across the dozens of
        /// pattern/token combinations one common, richly-inflected word generates — and dropping the
        /// whole group for the one poisoned member took "skupina" down with it.
        /// </summary>
        [TestMethod]
        public void CandidateRankingKeepsGroupSiblingNotExplainedByAdjectiveOverlap()
        {
            // "skupinový" tacked onto skupina's own matched forms stands in for the chain of
            // intermediate candidates (many token/pattern combinations of one common word) that bridged
            // the two roots on the real article — MergeByRootAndSharedForms only needs one candidate's
            // matched forms to contain another's lemma to union their groups, whatever the real path was.
            var skupina = new MatchCandidate("skupina", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null,
                ["skupina", "skupiny", "skupině", "skupinu", "skupinou", "skupin", "skupinách", "skupinami", "skupinový"]);
            var adjective = new MatchCandidate("skupinový", Core.Enums.WordCategory.Adjective, "mladý", null, null,
                ["skupinový", "skupinová", "skupinové", "skupinového"]);

            // Shares the merged group with "skupina" above via the bridge, and is fully explained by
            // the adjective — the poisoned sibling that must not take "skupina" down with it.
            var poisonedSibling = new MatchCandidate("skupinový", Core.Enums.WordCategory.Noun, "turista", Core.Enums.Gender.Masculine, true,
                ["skupinový", "skupinové"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([skupina, adjective, poisonedSibling], _ => false);

            Assert.IsTrue(dropped.Any(c => c.Lemma == "skupina" && c.Category == Core.Enums.WordCategory.Noun));
            Assert.IsFalse(dropped.Any(c => c.Lemma == "skupinový" && c.Category == Core.Enums.WordCategory.Noun));
        }

        /// <summary>
        /// Verifies that the bare root "papežov" — the hypothesis <see cref="NounMatcher"/>'s own
        /// reconstruction invents from "papežova" by stripping a noun case ending and reattaching
        /// nothing, since a consonant-final pattern's own nominative singular has no ending to add — is
        /// dropped alongside its possessive sibling rather than surviving as the group's sole winner.
        /// Filtering candidates before they are grouped by shared root removed only "papežovo" here,
        /// leaving "papežov" to win the root group on its own on a real run; checking the whole merged
        /// group is what catches this, the same way one already-known root drops every sibling with it.
        /// </summary>
        [TestMethod]
        public void CandidateRankingDropsBareRootSiblingOfPossessiveAdjectiveDerivative()
        {
            var papez = new MatchCandidate("papež", Core.Enums.WordCategory.Noun, "král", Core.Enums.Gender.Masculine, true, ["papež", "papeže", "papeži", "papežem"]);
            var papezovo = new MatchCandidate("papežovo", Core.Enums.WordCategory.Noun, "město", Core.Enums.Gender.Neuter, null, ["papežovo", "papežova", "papežovu"]);
            var papezov = new MatchCandidate("papežov", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false, ["papežov", "papežovu", "papežově", "papežovi"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([papez, papezovo, papezov], _ => false);

            Assert.AreEqual(1, dropped.Count);
            Assert.AreEqual("papež", dropped[0].Lemma);
        }

        /// <summary>
        /// Verifies that an "-ova"-ending noun candidate is not dropped when nothing corroborates it as
        /// a possessive adjective — the ending alone cannot say which, since "slova" (genitive plural
        /// of "slovo") ends the same way a possessive like "papežova" does, and stripping "-ova" from
        /// "slova" lands on "sl", which is neither known nor found elsewhere in the run.
        /// </summary>
        [TestMethod]
        public void CandidateRankingKeepsOvaEndingNounWithoutFoundStem()
        {
            var slova = new MatchCandidate("slova", Core.Enums.WordCategory.Noun, "žena", Core.Enums.Gender.Feminine, null, ["slova", "slov", "slovy"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([slova], _ => false);

            Assert.AreEqual(1, dropped.Count);
        }

        /// <summary>
        /// Verifies that a whole root group is dropped when the root itself is already a known word —
        /// "jeví" (the verb jevit se, not a noun) reduces to "jev", an ordinary noun already on file.
        /// "jev" never competes as a candidate — it is not a gap — so the check has to ask about the
        /// root directly rather than compare candidates against each other.
        /// </summary>
        [TestMethod]
        public void CandidateRankingDropsGroupWhenRootIsAlreadyKnown()
        {
            var candidate = new MatchCandidate("jeví", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false, ["jeví", "jevu", "jevy", "jevů"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([candidate], lemma => lemma == "jev");

            Assert.AreEqual(0, dropped.Count);
        }

        /// <summary>
        /// Verifies that a lone candidate with no sibling to lose to is still rejected when it is not
        /// shaped like its own claimed pattern's nominative singular — "jeví" ("jevit se", not a noun)
        /// under město, whose nominative singular ends in "o", not "í". Unlike
        /// <see cref="CandidateRankingDropsGroupWhenRootIsAlreadyKnown"/>, nothing here is known and no
        /// competing "jev" candidate exists to out-score it — "jev" is three letters, below the default
        /// --min-delka, so on some real articles no such competitor is ever generated. This is what
        /// caught the case on a real article the known-root check could not: the group has only one
        /// member, and it fails the shape test on its own.
        /// </summary>
        [TestMethod]
        public void CandidateRankingRejectsLoneCandidateNotShapedForItsOwnPattern()
        {
            var candidate = new MatchCandidate("jeví", Core.Enums.WordCategory.Noun, "město", Core.Enums.Gender.Neuter, null, ["jeví", "jevu", "jevem", "jev", "jevům"]);

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([candidate], _ => false);

            Assert.AreEqual(0, dropped.Count);
        }

        /// <summary>
        /// Verifies that a token ending in í is not tried as a noun once the same token already
        /// produced a verb candidate — "změní" scored higher as a noun than the "změnit" already found
        /// for the same token on raw text frequency, not because the noun reading was the better guess.
        /// </summary>
        [TestMethod]
        public void ShouldTryAsNounRejectsIEndingTokenWithVerbCandidate()
        {
            Assert.IsFalse(CandidateRanking.ShouldTryAsNoun("změní", verbCandidateCount: 1));
        }

        /// <summary>
        /// Verifies that an í-ending token is still tried as a noun when nothing corroborated it as a
        /// verb — the "stavení"/"rozhodčí" case, a small closed set no real verb reconstructs from.
        /// </summary>
        [TestMethod]
        public void ShouldTryAsNounAcceptsIEndingTokenWithNoVerbCandidate()
        {
            Assert.IsTrue(CandidateRanking.ShouldTryAsNoun("stavení", verbCandidateCount: 0));
        }

        /// <summary>
        /// Verifies that a token not ending in í is tried as a noun regardless of verb candidates —
        /// the restriction is specific to the one ending that actually collides with a verb's shape.
        /// </summary>
        [TestMethod]
        public void ShouldTryAsNounAcceptsNonIEndingTokenEvenWithVerbCandidate()
        {
            Assert.IsTrue(CandidateRanking.ShouldTryAsNoun("zápas", verbCandidateCount: 1));
        }

        /// <summary>
        /// Verifies that a token shaped like l-participle agreement is not tried as a noun once the
        /// same token already produced a verb candidate — "existovalo" (neuter, the token itself as a
        /// fake nominative singular) scored as a noun against "existoval" (fake genitive plural) and
        /// "existovala" (fake genitive singular), all of which are really just the same verb's own past
        /// tense agreeing with different genders. Found on a real article about the universe, alongside
        /// "předpokládali", "nebylo"/"nebyla", "začalo"/"začala", "vytvořili" and "souvisela" — five more
        /// instances of the same mechanism on the same text, not a one-off.
        /// </summary>
        [TestMethod]
        public void ShouldTryAsNounRejectsLParticipleEndingTokenWithVerbCandidate()
        {
            Assert.IsFalse(CandidateRanking.ShouldTryAsNoun("existovalo", verbCandidateCount: 1));
            Assert.IsFalse(CandidateRanking.ShouldTryAsNoun("existoval", verbCandidateCount: 1));
            Assert.IsFalse(CandidateRanking.ShouldTryAsNoun("existovala", verbCandidateCount: 1));
            Assert.IsFalse(CandidateRanking.ShouldTryAsNoun("existovali", verbCandidateCount: 1));
            Assert.IsFalse(CandidateRanking.ShouldTryAsNoun("existovaly", verbCandidateCount: 1));
        }

        /// <summary>
        /// Verifies that a token shaped like l-participle agreement is still tried as a noun when
        /// nothing corroborated it as a verb — a real noun like "škola"/"školy" is not l-participle
        /// shaped by coincidence often enough to need blocking on shape alone, only once a verb reading
        /// actually explains the token.
        /// </summary>
        [TestMethod]
        public void ShouldTryAsNounAcceptsLParticipleEndingTokenWithNoVerbCandidate()
        {
            Assert.IsTrue(CandidateRanking.ShouldTryAsNoun("existovalo", verbCandidateCount: 0));
        }

        /// <summary>
        /// Verifies that <see cref="VerbMatcher"/> reconstructs the infinitive from an l-participle
        /// token, the same "generate and test" move it already makes for a present-tense-shaped token —
        /// this is what makes <see cref="CandidateRanking.ShouldTryAsNoun"/>'s l-participle gate able to
        /// fire at all, since it needs a real verb candidate to compare against.
        /// </summary>
        [TestMethod]
        public void VerbMatcherReconstructsInfinitiveFromLParticiple()
        {
            var corpus = new Dictionary<string, int>
            {
                ["existovalo"] = 1, ["existoval"] = 1, ["existovala"] = 1, ["existovaly"] = 1,
            };

            var candidates = verbMatcher.Match("existovalo", corpus, noProperNouns);

            Assert.IsTrue(candidates.Any(candidate => candidate.Lemma == "existovat"));
        }

        /// <summary>
        /// Verifies that "jít"-compound l-participle forms are blocked from noun-matching outright,
        /// with no verb candidate needed first — "přišel"/"přišla"/"přišli" scored as a fake pán-pattern
        /// noun on a real article, and unlike a regular verb's l-participle, <see cref="VerbMatcher"/>
        /// can never corroborate one to gate on: class 1 is the only class that would try "přijít", and
        /// <see cref="Services.CzechWordStructureResolver.DeriveTrida1"/> refuses to derive a stem for
        /// it on principle, so the verb-candidate count for this token is always zero.
        /// </summary>
        [TestMethod]
        public void ShouldTryAsNounRejectsJitCompoundLParticipleRegardlessOfVerbCandidateCount()
        {
            Assert.IsFalse(CandidateRanking.ShouldTryAsNoun("přišel", verbCandidateCount: 0));
            Assert.IsFalse(CandidateRanking.ShouldTryAsNoun("přišla", verbCandidateCount: 0));
            Assert.IsFalse(CandidateRanking.ShouldTryAsNoun("přišli", verbCandidateCount: 0));
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

            var candidate = adjectiveMatcher.Match("hezký", corpus, noProperNouns);

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

            var candidate = adjectiveMatcher.Match("hezký", corpus, noProperNouns);

            Assert.IsNull(candidate);
        }

        /// <summary>
        /// Verifies that a token not shaped like any adjective citation form is rejected outright,
        /// even with corroborating "evidence" in the corpus — "novin" (genitive plural of "noviny")
        /// scored as a false adjective candidate on a real article via
        /// GuessAdjectivePattern's unconditional "mladý" fallback, before this check existed.
        /// </summary>
        [TestMethod]
        public void AdjectiveMatcherRejectsTokenNotShapedLikeCitationForm()
        {
            var corpus = new Dictionary<string, int> { ["novin"] = 3, ["novinu"] = 1, ["novinou"] = 1 };

            var candidate = adjectiveMatcher.Match("novin", corpus, noProperNouns);

            Assert.IsNull(candidate);
        }

        /// <summary>
        /// Verifies that a deverbal neuter noun (dýchání, from dýchat) is not tried as a jarní-pattern
        /// adjective just because it ends in í — that ending is how Czech forms a verbal noun from an
        /// infinitive, not how a soft adjective is built, and "dýchání" scored as a false adjective on
        /// a real article before this check existed.
        /// </summary>
        [TestMethod]
        public void AdjectiveMatcherRejectsDeverbalNounEnding()
        {
            var corpus = new Dictionary<string, int> { ["dýchání"] = 2, ["dýcháním"] = 1 };

            var candidate = adjectiveMatcher.Match("dýchání", corpus, noProperNouns);

            Assert.IsNull(candidate);
        }

        /// <summary>
        /// Verifies that a feminine or neuter citation-shaped token (celá/celé) is folded back to the
        /// masculine -ý lemma before anything is generated, so it produces the same hypothesis as the
        /// masculine token would — not a second, competing lemma a person has to recognize as the same
        /// word by eye.
        /// </summary>
        [TestMethod]
        public void AdjectiveMatcherNormalizesGenderVariantToCitationForm()
        {
            var corpus = new Dictionary<string, int> { ["celý"] = 1, ["celá"] = 1, ["celé"] = 1 };

            var fromFeminine = adjectiveMatcher.Match("celá", corpus, noProperNouns);
            var fromNeuter = adjectiveMatcher.Match("celé", corpus, noProperNouns);

            Assert.IsNotNull(fromFeminine);
            Assert.IsNotNull(fromNeuter);
            Assert.AreEqual("celý", fromFeminine.Lemma);
            Assert.AreEqual("celý", fromNeuter.Lemma);
        }

        // ── VerbMatcher ──────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that an infinitive with no other corroborating form is not proposed.
        /// </summary>
        [TestMethod]
        public void VerbMatcherRejectsUncorroboratedToken()
        {
            var corpus = new Dictionary<string, int> { ["dělat"] = 1 };

            var candidates = verbMatcher.Match("dělat", corpus, noProperNouns);

            Assert.AreEqual(0, candidates.Count);
        }

        /// <summary>
        /// Verifies the token-as-infinitive path: a regular class-5 infinitive corroborated by its own
        /// present tense is proposed under the trida5 pattern.
        /// </summary>
        [TestMethod]
        public void VerbMatcherAcceptsInfinitiveTokenWithCorroboratingPresentForm()
        {
            var corpus = new Dictionary<string, int> { ["dělat"] = 1, ["dělá"] = 1 };

            var candidates = verbMatcher.Match("dělat", corpus, noProperNouns);

            Assert.IsTrue(candidates.Any(candidate => candidate.Pattern == "trida5" && candidate.MatchedForms.Contains("dělá")));
        }

        /// <summary>
        /// Verifies the reconstruction path: a token shaped like a class-5 present-tense form, with no
        /// infinitive anywhere in the text, still resolves to the "dělat" hypothesis once a second
        /// present-tense form corroborates it — the case that motivated reconstruction at all.
        /// </summary>
        [TestMethod]
        public void VerbMatcherReconstructsInfinitiveFromPresentTenseToken()
        {
            var corpus = new Dictionary<string, int> { ["dělá"] = 1, ["děláme"] = 1 };

            var candidates = verbMatcher.Match("dělá", corpus, noProperNouns);

            Assert.IsTrue(candidates.Any(candidate => candidate.Lemma == "dělat" && candidate.Pattern == "trida5"));
        }

        /// <summary>
        /// Verifies that reconstructing "-at" and "-át" from the same present-tense token does not
        /// leave both as separate candidates when they score the same — they always do on present-tense
        /// evidence alone, since both reconstructions share the identical present stem by construction,
        /// so only the ordinary "-at" spelling should survive ("využívát" scored the same as real
        /// "využívat" on a live article before this check existed).
        /// </summary>
        [TestMethod]
        public void VerbMatcherPrefersAtSpellingWhenAtAndAtWithAcuteTie()
        {
            var corpus = new Dictionary<string, int> { ["využívá"] = 1, ["využíváme"] = 1 };

            var candidates = verbMatcher.Match("využívá", corpus, noProperNouns);

            Assert.IsTrue(candidates.Any(candidate => candidate.Lemma == "využívat" && candidate.Pattern == "trida5"));
            Assert.IsFalse(candidates.Any(candidate => candidate.Lemma == "využívát"));
        }

        /// <summary>
        /// Verifies the same deduplication for class 4's four-way suffix ambiguity (it/ít/et/ět) — all
        /// four reconstructions share the identical present stem by construction, so
        /// "hudebnit"/"hudebnít"/"hudebnet"/"hudebnět" tied at the same score on a real article. Only
        /// the preferred "it" spelling should survive.
        /// </summary>
        [TestMethod]
        public void VerbMatcherPrefersItSpellingWhenClass4VariantsTie()
        {
            var corpus = new Dictionary<string, int> { ["trpí"] = 1, ["trpíme"] = 1 };

            var candidates = verbMatcher.Match("trpí", corpus, noProperNouns);

            Assert.IsTrue(candidates.Any(candidate => candidate.Lemma == "trpit" && candidate.Pattern == "trida4"));
            Assert.IsFalse(candidates.Any(candidate => candidate.Lemma is "trpít" or "trpet" or "trpět"));
        }

        /// <summary>
        /// Verifies that the l-participle reconstruction's own "+nout" alternative is dropped once its
        /// "+t" sibling — the shape trida3/4/5 always land on from the same stem, since every one of
        /// their own suffixes ends in "t" — exists and scores at least as well. "rozvíjenout" tied
        /// "rozvíjet" on a real article about painting, corroborated by the exact same three l-participle
        /// forms (rozvíjela, rozvíjelo, rozvíjeli) as the real trida4 verb, not a second sighting of
        /// anything. Unlike the genuinely irregular class 2 verbs the "+nout" reading exists for
        /// (vzniknout, whose l-participle "vznikl" has no "+t" sibling at all — "vznikt" is not a valid
        /// shape for any class), an ordinary verb's l-participle stem always has one.
        /// </summary>
        [TestMethod]
        public void VerbMatcherDropsNoutAlternativeWhenTSiblingExists()
        {
            var corpus = new Dictionary<string, int> { ["rozvíjela"] = 1, ["rozvíjelo"] = 1, ["rozvíjeli"] = 1 };

            var candidates = verbMatcher.Match("rozvíjela", corpus, noProperNouns);

            Assert.IsTrue(candidates.Any(candidate => candidate.Lemma == "rozvíjet" && candidate.Pattern == "trida4"));
            Assert.IsFalse(candidates.Any(candidate => candidate.Lemma == "rozvíjenout"));
        }

        /// <summary>
        /// Verifies that a class-4 reconstruction is rejected when its only corroboration is the
        /// ambiguous í/ím pair — "prostředit" (invented) scored the same way "konkrétní" (adjective) and
        /// "prostředí" (noun, stavení-pattern) do, since a jarní adjective's own citation form and
        /// instrumental singular, or an í-final noun's own citation form and instrumental singular,
        /// produce exactly those two forms regardless of whether any verb is really there.
        /// </summary>
        [TestMethod]
        public void VerbMatcherRejectsClass4WithOnlyAmbiguousEndingCorroboration()
        {
            var corpus = new Dictionary<string, int> { ["prostředí"] = 3, ["prostředím"] = 1 };

            var candidates = verbMatcher.Match("prostředí", corpus, noProperNouns);

            Assert.IsFalse(candidates.Any(candidate => candidate.Pattern == "trida4"));
        }

        /// <summary>
        /// Verifies that a class-4 reconstruction survives when corroborated by a form outside the
        /// ambiguous í/ím pair, even if í/ím also matched — "změnit" survived on a real article because
        /// its infinitive independently appeared in the text.
        /// </summary>
        [TestMethod]
        public void VerbMatcherAcceptsClass4WithCorroborationBeyondAmbiguousEnding()
        {
            var corpus = new Dictionary<string, int> { ["změní"] = 2, ["změnit"] = 1 };

            var candidates = verbMatcher.Match("změní", corpus, noProperNouns);

            Assert.IsTrue(candidates.Any(candidate => candidate.Lemma == "změnit" && candidate.Pattern == "trida4"));
        }

        /// <summary>
        /// Verifies that a token not shaped like any class 2-5 infinitive is not tried under those
        /// classes — "vznik" (the noun, "origin") is not a class-2 infinitive, but class 2's fallback
        /// stem for an unrecognized ending equals the bare lemma, which happens to be exactly
        /// "vzniknout"'s own past stem, so without the shape check this coincidence alone would score
        /// a false verb candidate as high as a real one.
        /// </summary>
        [TestMethod]
        public void VerbMatcherRejectsTokenNotShapedLikeClass2Through5Infinitive()
        {
            var corpus = new Dictionary<string, int> { ["vznik"] = 3, ["vznikl"] = 1, ["vzniklo"] = 1, ["vznikly"] = 1 };

            var candidates = verbMatcher.Match("vznik", corpus, noProperNouns);

            Assert.IsFalse(candidates.Any(candidate => candidate.Pattern is "trida2" or "trida3" or "trida4" or "trida5"));
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

        /// <summary>
        /// Verifies that <c>confirmed: true</c> writes the proposal already confirmed — this is what
        /// the GUI passes for candidates a person checked, since the checkbox selection is itself the
        /// review and the word should not have to wait for <c>:slova doplnit</c> a second time.
        /// </summary>
        [TestMethod]
        public void ProposalWriterWritesConfirmedProposalWhenToldTheCandidateWasReviewed()
        {
            var store = TemporaryProposals();
            var candidate = new MatchCandidate(
                "pořádek", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false,
                ["pořádek", "pořádku"]);

            var added = ProposalWriter.WriteNew([candidate], store, confirmed: true);

            Assert.AreEqual(1, added);
            Assert.IsTrue(store.Read().Single().IsConfirmed);

            store.Clear();
        }

        /// <summary>
        /// Verifies that <c>rejected: true</c> writes the proposal already rejected, unconfirmed — this
        /// is what the GUI passes for a candidate checked in the exception column, so it never comes
        /// back in a later run the same way a manually rejected word from <c>:slova doplnit</c> does not.
        /// </summary>
        [TestMethod]
        public void ProposalWriterWritesRejectedProposalWhenToldTheCandidateWasAnException()
        {
            var store = TemporaryProposals();
            var candidate = new MatchCandidate(
                "pořádek", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false,
                ["pořádek", "pořádku"]);

            var added = ProposalWriter.WriteNew([candidate], store, rejected: true);

            Assert.AreEqual(1, added);
            var proposal = store.Read().Single();
            Assert.IsTrue(proposal.IsRejected);
            Assert.IsFalse(proposal.IsConfirmed);

            store.Clear();
        }

        /// <summary>
        /// Verifies that <c>rejected</c> wins if both flags are somehow set — a candidate cannot be both
        /// confirmed and an exception, and rejection is the safer of the two to default to.
        /// </summary>
        [TestMethod]
        public void ProposalWriterRejectedWinsOverConfirmedWhenBothAreSet()
        {
            var store = TemporaryProposals();
            var candidate = new MatchCandidate(
                "pořádek", Core.Enums.WordCategory.Noun, "hrad", Core.Enums.Gender.Masculine, false,
                ["pořádek", "pořádku"]);

            ProposalWriter.WriteNew([candidate], store, confirmed: true, rejected: true);

            var proposal = store.Read().Single();
            Assert.IsTrue(proposal.IsRejected);
            Assert.IsFalse(proposal.IsConfirmed);

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
