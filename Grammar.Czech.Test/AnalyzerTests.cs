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

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([real, spurious]);

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

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([candidate]);

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

            var dropped = CandidateRanking.DropVowelEndingNounDuplicates([real, spurious]);

            Assert.AreEqual(1, dropped.Count);
            Assert.AreEqual("změna", dropped[0].Lemma);
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

            var candidate = adjectiveMatcher.Match("novin", corpus);

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

            var candidate = adjectiveMatcher.Match("dýchání", corpus);

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

            var fromFeminine = adjectiveMatcher.Match("celá", corpus);
            var fromNeuter = adjectiveMatcher.Match("celé", corpus);

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

            var candidates = verbMatcher.Match("dělat", corpus);

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

            var candidates = verbMatcher.Match("dělat", corpus);

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

            var candidates = verbMatcher.Match("dělá", corpus);

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

            var candidates = verbMatcher.Match("využívá", corpus);

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

            var candidates = verbMatcher.Match("trpí", corpus);

            Assert.IsTrue(candidates.Any(candidate => candidate.Lemma == "trpit" && candidate.Pattern == "trida4"));
            Assert.IsFalse(candidates.Any(candidate => candidate.Lemma is "trpít" or "trpet" or "trpět"));
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

            var candidates = verbMatcher.Match("prostředí", corpus);

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

            var candidates = verbMatcher.Match("změní", corpus);

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

            var candidates = verbMatcher.Match("vznik", corpus);

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
