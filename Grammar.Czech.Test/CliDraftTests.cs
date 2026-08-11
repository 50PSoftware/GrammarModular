using Grammar.Core.Enums;
using Grammar.Czech.Cli;
using Grammar.Czech.Models;
using Grammar.Czech.Cli.Interaction;
using Grammar.Czech.Cli.Rendering;
using Grammar.Czech.Cli.Sentence;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies what the client application makes of a bare list of lemmas: which word becomes the
    /// predicate, what role each of the others gets, and what it refuses to decide on its own.
    /// </summary>
    [TestClass]
    public sealed class CliDraftTests
    {
        private static IServiceProvider services = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var collection = new ServiceCollection();

            collection.AddCzechGrammarServices();
            collection.AddSingleton<LemmaGuess>();
            collection.AddSingleton<LemmaLookup>();
            collection.AddSingleton<RoleGuess>();
            collection.AddSingleton<FormLookup>();
            // Vlastní soubor, ne ten uživatelův: testy sbírají neznámá slova jako každý běh a psát
            // je do %APPDATA% by znamenalo, že test mění stav stroje, na kterém běží.
            collection.AddSingleton(new WordProposals(Path.Combine(
                Path.GetTempPath(), $"gramatika-test-{Guid.NewGuid():N}.json")));
            collection.AddSingleton<DraftBuilder>();
            collection.AddSingleton<DraftView>();
            collection.AddSingleton<SentenceComposer>();

            services = collection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        }

        private static SentenceDraft Whole(DraftOverrides? overrides, params string[] lemmas) =>
            services.GetRequiredService<DraftBuilder>().Build(lemmas, overrides ?? new DraftOverrides());

        // Většina těchhle testů mluví o jedné klauzi, tak si sáhne rovnou na ni.
        private static ClauseDraft Draft(DraftOverrides? overrides, params string[] lemmas) =>
            Whole(overrides, lemmas).Main;

        private static DraftOverrides Dropping()
        {
            var overrides = new DraftOverrides();
            overrides.Predicate.DropSubject = true;

            return overrides;
        }

        private static string Sentence(DraftOverrides? overrides, params string[] lemmas) =>
            services.GetRequiredService<SentenceComposer>().Compose(Whole(overrides, lemmas));

        /// <summary>
        /// Verifies that the verb becomes the predicate wherever it stands among the lemmas.
        /// </summary>
        [TestMethod]
        public void PredicateIsTheVerbWhereverItStands()
        {
            Assert.AreEqual("číst", Draft(null, "student", "číst", "kniha").PredicateLemma);
            Assert.AreEqual("číst", Draft(null, "číst", "student", "kniha").PredicateLemma);
        }

        /// <summary>
        /// Verifies that the valency frame assigns the roles and governs the cases.
        /// </summary>
        [TestMethod]
        public void FrameAssignsRolesAndCases()
        {
            var draft = Draft(null, "student", "číst", "kniha");

            Assert.AreEqual(FgdFunctor.ACT, draft.Constituents[0].Functor);
            Assert.AreEqual(Case.Nominative, draft.Constituents[0].EffectiveCase);
            Assert.AreEqual(FgdFunctor.PAT, draft.Constituents[1].Functor);
            Assert.AreEqual(Case.Accusative, draft.Constituents[1].EffectiveCase);

            // Pád si request nenese — plyne z rámce a doplňuje ho až builder.
            Assert.IsNull(draft.Constituents[1].Word.Case);
        }

        /// <summary>
        /// Verifies that an animate noun is preferred for the addressee, which is what keeps the two
        /// objects of a transfer verb apart.
        /// </summary>
        [TestMethod]
        public void AnimateNounBecomesTheAddressee()
        {
            Assert.AreEqual("Klára dává ženě knihu.", Sentence(null, "Klára", "dávat", "žena", "kniha"));
        }

        /// <summary>
        /// Verifies that a stated role overrules the one the frame would have assigned.
        /// </summary>
        [TestMethod]
        public void StatedRoleWins()
        {
            var overrides = new DraftOverrides();
            overrides.For("kniha").Functor = FgdFunctor.ACT;

            Assert.AreEqual(FgdFunctor.ACT, Draft(overrides, "student", "číst", "kniha").Constituents[1].Functor);
        }

        /// <summary>
        /// Verifies that a lemma the lexicon does not hold is inferred from its ending and reported as
        /// inferred rather than passed off as known.
        /// </summary>
        [TestMethod]
        public void UnknownLemmaIsInferredAndReported()
        {
            var draft = Draft(null, "Klára", "číst", "kniha");

            Assert.AreEqual(MetadataOrigin.Guess, draft.Constituents[0].Origin);
            Assert.AreEqual("žena", draft.Constituents[0].Word.Pattern);
            Assert.AreEqual(Gender.Feminine, draft.Constituents[0].Word.Gender);
            Assert.IsTrue(draft.Notes.Any(note => note.Contains("Klára")));
        }

        /// <summary>
        /// Verifies that a stated pattern beats both the lexicon and the inference.
        /// </summary>
        [TestMethod]
        public void StatedPatternWins()
        {
            var overrides = new DraftOverrides();
            overrides.For("Ivana").Pattern = "žena";
            overrides.For("Ivana").Gender = Gender.Feminine;

            var draft = Draft(overrides, "Ivana", "číst", "kniha");

            Assert.AreEqual(MetadataOrigin.User, draft.Constituents[0].Origin);
            Assert.AreEqual("žena", draft.Constituents[0].Word.Pattern);
        }

        /// <summary>
        /// Verifies that a preposition standing before a noun becomes that constituent's preposition and
        /// takes the case it governs, and that its semantic group names the free modification.
        /// </summary>
        [TestMethod]
        public void PrepositionOpensTheConstituentAndGovernsIt()
        {
            var draft = Draft(null, "student", "číst", "kniha", "u", "les");
            var adjunct = draft.Constituents[^1];

            Assert.AreEqual("u", adjunct.EffectivePreposition);
            Assert.AreEqual(Case.Genitive, adjunct.EffectiveCase);
            Assert.AreEqual(FgdFunctor.LOC, adjunct.Functor);
        }

        /// <summary>
        /// Verifies that an adjective in front of a noun becomes its agreeing attribute.
        /// </summary>
        [TestMethod]
        public void AdjectiveBecomesAnAttribute()
        {
            Assert.AreEqual("Mladý student čte knihu.", Sentence(null, "mladý", "student", "číst", "kniha"));
        }

        /// <summary>
        /// Verifies that a verb whose senses the dictionary does not rank is left undecided rather than
        /// picked for the user.
        /// </summary>
        [TestMethod]
        public void AmbiguousFrameIsAQuestionRatherThanAChoice()
        {
            var draft = Draft(null, "student", "jít");

            Assert.IsNull(draft.Frame);
            Assert.IsTrue(draft.Gaps().Any(gap => gap.Contains("motion")));

            var overrides = new DraftOverrides();
            overrides.Predicate.FrameLabel = "motion";

            Assert.AreEqual(0, Draft(overrides, "student", "jít").Gaps().Count);
        }

        /// <summary>
        /// Verifies that a constituent no frame accounts for and no preposition explains is reported as
        /// an open question instead of being given a role at random.
        /// </summary>
        [TestMethod]
        public void UnexplainedConstituentIsReportedAsAGap()
        {
            var draft = Draft(null, "student", "číst", "kniha", "den");

            Assert.IsNull(draft.Constituents[^1].Functor);
            Assert.IsTrue(draft.Gaps().Any(gap => gap.Contains("den")));
        }

        /// <summary>
        /// Verifies that a conjunction in the word list splits the sentence into clauses, and that the
        /// conjunction itself decides whether they are coordinated or subordinated.
        /// </summary>
        [DataTestMethod]
        [DataRow("a", "Student čte knihu a žák píše dopis.", DisplayName = "souřadné")]
        [DataRow("protože", "Student čte knihu, protože žák píše dopis.", DisplayName = "podřadné")]
        public void ConjunctionSplitsTheWordListIntoClauses(string conjunction, string expected)
        {
            var whole = Whole(null, "student", "číst", "kniha", conjunction, "žák", "psát", "dopis");

            Assert.AreEqual(2, whole.Clauses.Count);
            Assert.IsNull(whole.Clauses[0].Conjunction);
            Assert.AreEqual(conjunction, whole.Clauses[1].Conjunction);
            Assert.AreEqual(expected, Sentence(null, "student", "číst", "kniha", conjunction, "žák", "psát", "dopis"));
        }

        /// <summary>
        /// Verifies that a chain of conjunctions builds a chain of clauses, and that a conjunction which
        /// governs a mood applies it even though the tool builds each clause on its own.
        /// </summary>
        [TestMethod]
        public void SeveralConjunctionsChainAndGovernTheirClauses()
        {
            Assert.AreEqual(
                3,
                Whole(null, "student", "číst", "kniha", "a", "žák", "psát", "dopis", "a", "student", "pracovat")
                    .Clauses.Count);

            Assert.AreEqual(
                "Student čte knihu a žák píše dopis a student pracuje.",
                Sentence(null, "student", "číst", "kniha", "a", "žák", "psát", "dopis", "a", "student", "pracovat"));

            // Kondicionál z 'aby', přestože nástroj staví každou klauzi zvlášť a spojku nad sebou
            // v tu chvíli ještě nezná.
            Assert.AreEqual(
                "Student čte knihu, aby žák psal dopis.",
                Sentence(null, "student", "číst", "kniha", "aby", "žák", "psát", "dopis"));
        }

        /// <summary>
        /// Verifies that a clause hangs off the one immediately before it unless told otherwise, which
        /// is how a reader takes it — the singing belongs inside the <em>aby</em>, not beside the whole
        /// sentence.
        /// </summary>
        [TestMethod]
        public void ClauseHangsOffTheOneBeforeItByDefault()
        {
            string[] words = ["student", "číst", "kniha", "aby", "žák", "psát", "dopis", "a", "lékař", "zpívat", "píseň"];
            var whole = Whole(null, words);

            Assert.IsNull(whole.Clauses[0].ParentOrdinal);
            Assert.AreEqual(1, whole.Clauses[1].ParentOrdinal);
            Assert.AreEqual(2, whole.Clauses[2].ParentOrdinal);

            // Kondicionál dosáhne i na třetí klauzi, protože je souřadná s tou pod 'aby'.
            Assert.AreEqual("Student čte knihu, aby žák psal dopis a lékař zpíval píseň.", Sentence(null, words));
        }

        /// <summary>
        /// Verifies that the attachment can be moved, and that moving it changes the sentence rather
        /// than only the picture of it.
        /// </summary>
        [TestMethod]
        public void AttachmentCanBeMovedAndChangesTheSentence()
        {
            string[] words = ["student", "číst", "kniha", "aby", "žák", "psát", "dopis", "a", "lékař", "zpívat", "píseň"];

            var overrides = new DraftOverrides();
            overrides.Attach(3, 1);

            Assert.AreEqual(1, Whole(overrides, words).Clauses[2].ParentOrdinal);

            // Mimo dosah 'aby' se třetí klauze vrací do oznamovacího způsobu.
            Assert.AreEqual("Student čte knihu, aby žák psal dopis a lékař zpívá píseň.", Sentence(overrides, words));
        }

        /// <summary>
        /// Verifies that an attachment which would leave the sentence without a root is refused.
        /// </summary>
        [TestMethod]
        public void AttachmentThatCannotHoldIsRefused()
        {
            Assert.ThrowsException<CliException>(() => new DraftOverrides().Attach(1, 1));
            Assert.ThrowsException<CliException>(() => new DraftOverrides().Attach(2, 2));
            Assert.ThrowsException<CliException>(() => new DraftOverrides().Attach(2, 3));

            var beyond = new DraftOverrides();
            beyond.Attach(3, 1);

            Assert.ThrowsException<CliException>(
                () => Whole(beyond, "student", "číst", "kniha", "a", "žák", "psát", "dopis"));
        }

        /// <summary>
        /// Verifies that a predicate switch speaks for the whole sentence, and that a clause which says
        /// otherwise wins over it — which is what makes the pair read as it looks.
        /// </summary>
        [TestMethod]
        public void PredicateSwitchSpeaksForTheSentenceUnlessAClauseSaysOtherwise()
        {
            string[] words = ["student", "číst", "kniha", "a", "žák", "psát", "dopis"];

            var everywhere = new DraftOverrides();
            everywhere.Predicate.Tense = Tense.Past;

            var onlySecond = new DraftOverrides();
            onlySecond.PredicateOf(2).Tense = Tense.Past;

            var bothWays = new DraftOverrides();
            bothWays.Predicate.Tense = Tense.Past;
            bothWays.PredicateOf(2).Tense = Tense.Present;

            Assert.AreEqual("Student četl knihu a žák psal dopis.", Sentence(everywhere, words));
            Assert.AreEqual("Student čte knihu a žák psal dopis.", Sentence(onlySecond, words));
            Assert.AreEqual("Student četl knihu a žák píše dopis.", Sentence(bothWays, words));
        }

        /// <summary>
        /// Verifies that a clause number naming no clause is reported rather than quietly ignored.
        /// </summary>
        [TestMethod]
        public void PredicateOfAClauseThatIsNotThereIsRefused()
        {
            var overrides = new DraftOverrides();
            overrides.PredicateOf(5).Tense = Tense.Past;

            var failure = Assert.ThrowsException<CliException>(
                () => Whole(overrides, "student", "číst", "kniha", "a", "žák", "psát", "dopis"));

            StringAssert.Contains(failure.Message, "klauze 5");
        }

        /// <summary>
        /// Verifies that positions stay global across the whole word list, so a correction addresses the
        /// same word whichever clause it ended up in.
        /// </summary>
        [TestMethod]
        public void PositionsRunAcrossTheWholeSentence()
        {
            var overrides = new DraftOverrides();
            overrides.For("7").Case = Case.Genitive;

            var whole = Whole(overrides, "student", "číst", "kniha", "a", "žák", "psát", "dopis");

            Assert.AreEqual(7, whole.Clauses[1].Constituents[^1].Position);
            Assert.AreEqual(Case.Genitive, whole.Clauses[1].Constituents[^1].EffectiveCase);
        }

        /// <summary>
        /// Verifies that a conjunction with nothing on one side of it is refused, since there is then
        /// nothing to join.
        /// </summary>
        [TestMethod]
        public void ConjunctionWithNothingToJoinIsRefused()
        {
            Assert.ThrowsException<CliException>(() => Whole(null, "student", "číst", "kniha", "a"));
            Assert.ThrowsException<CliException>(() => Whole(null, "a", "student", "číst", "kniha"));
        }

        /// <summary>
        /// Verifies that a clause with no verb in it is refused with an explanation.
        /// </summary>
        [TestMethod]
        public void ClauseWithoutAVerbIsRefused()
        {
            var failure = Assert.ThrowsException<CliException>(() => Draft(null, "student", "kniha"));

            Assert.IsTrue(failure.Message.Contains("sloveso"));
        }

        /// <summary>
        /// Verifies that the predicate's categories are taken from the switches.
        /// </summary>
        [TestMethod]
        public void PredicateCategoriesComeFromTheSwitches()
        {
            var overrides = new DraftOverrides();
            overrides.Predicate.Tense = Tense.Past;
            overrides.Predicate.IsNegative = true;

            Assert.AreEqual("Student nečetl knihu.", Sentence(overrides, "student", "číst", "kniha"));
        }

        /// <summary>
        /// Verifies that a pronoun is recognized from the rule data rather than declined as a noun, and
        /// that the tool does not report a closed class as a word the dictionary is missing.
        /// </summary>
        [TestMethod]
        public void PronounComesFromTheRulesRatherThanTheGuess()
        {
            var draft = Draft(null, "já", "číst", "kniha");

            Assert.AreEqual(MetadataOrigin.Rules, draft.Constituents[0].Origin);
            Assert.IsFalse(draft.Notes.Any(note => note.Contains("já")));
            Assert.AreEqual("Já čtu knihu.", Sentence(null, "já", "číst", "kniha"));
        }

        /// <summary>
        /// Verifies that the subject pronoun is kept unless dropping it is asked for — the tool prints
        /// what it was given — and that asking for it produces the neutral Czech sentence.
        /// </summary>
        [TestMethod]
        public void SubjectIsDroppedOnlyWhenAskedFor()
        {
            Assert.AreEqual("Já čtu knihu.", Sentence(null, "já", "číst", "kniha"));

            Assert.AreEqual(
                "Čtu knihu.",
                Sentence(Dropping(), "já", "číst", "kniha"));
        }

        /// <summary>
        /// Verifies that the communicative status decides the word order, which is the whole reason the
        /// draft carries it.
        /// </summary>
        [TestMethod]
        public void StatusDecidesTheWordOrder()
        {
            var overrides = new DraftOverrides();
            overrides.For("kniha").Status = InformationStatus.Given;
            overrides.For("student").Status = InformationStatus.New;

            Assert.AreEqual("Knihu čte student.", Sentence(overrides, "student", "číst", "kniha"));
        }

        private static WordCategory? CategoryOf(string lemma) =>
            Draft(null, "student", "číst", lemma)
                .Constituents.Single(constituent => constituent.Lemma == lemma)
                .Word.WordCategory;

        /// <summary>
        /// The four closed classes the dictionary does not carry are recognized from the rule data, the
        /// same way pronouns, prepositions and conjunctions already were.
        /// </summary>
        /// <remarks>
        /// The SQLite dictionary holds nouns, adjectives and verbs and nothing else, so every adverb,
        /// particle, interjection and numeral used to fall through to the guess from the ending — which
        /// knows infinitives and adjective endings and calls everything else a noun.
        /// </remarks>
        [DataTestMethod]
        [DataRow("rychle", WordCategory.Adverb)]
        [DataRow("pomalu", WordCategory.Adverb)]
        [DataRow("ano", WordCategory.Particle)]
        [DataRow("copak", WordCategory.Particle)]
        [DataRow("ach", WordCategory.Interjection)]
        [DataRow("bum", WordCategory.Interjection)]
        public void ClosedClassWordIsRecognized(string lemma, WordCategory expected)
        {
            Assert.AreEqual(expected, CategoryOf(lemma));
        }

        /// <summary>
        /// A numeral in words counts the noun after it, with the agreement that number takes.
        /// </summary>
        /// <remarks>
        /// Asserted through the sentence rather than through the category, because a numeral is not a
        /// constituent of its own — it modifies the noun it stands before, the same as an adjective, so
        /// there is no row in the table to read the class off. And <em>pět knih</em> rather than
        /// <em>pět knihy</em> is what says the class was recognized: five governs the genitive plural,
        /// and a noun mistaken for a numeral would have been declined instead.
        /// </remarks>
        [DataTestMethod]
        [DataRow("pět", "Student čte pět knih.")]
        [DataRow("dvacet", "Student čte dvacet knih.")]
        public void NumeralInWordsCountsTheNounAfterIt(string lemma, string expected)
        {
            Assert.AreEqual(expected, Sentence(null, "student", "číst", lemma, "kniha"));
        }

        /// <summary>
        /// The classes that already worked keep working, because the new tests run after them.
        /// </summary>
        /// <remarks>
        /// They overlap: <em>vedle</em> is a preposition and an adverb, <em>tak</em> a conjunction, an
        /// adverb and an interjection, <em>na</em> a preposition and an interjection. Putting the new
        /// tests last is what keeps every one of those reading as it did.
        /// </remarks>
        [TestMethod]
        public void OrderKeepsThePronounUnchanged()
        {
            Assert.AreEqual(WordCategory.Pronoun, CategoryOf("já"));
        }

        /// <summary>
        /// A word that is both a preposition and something else stays a preposition, which is what the
        /// order of the tests is for.
        /// </summary>
        /// <remarks>
        /// Through the sentence again: a preposition is not a constituent either, it opens the one that
        /// follows it. <em>vedle</em> is also an adverb and <em>na</em> also an interjection, and both
        /// govern the noun after them here, which no other reading would.
        /// </remarks>
        [DataTestMethod]
        [DataRow("vedle", Case.Genitive, "Student čte vedle knihy.")]
        [DataRow("na", Case.Locative, "Student čte na knize.")]
        public void OrderKeepsThePrepositionsUnchanged(string lemma, Case governed, string expected)
        {
            var overrides = new DraftOverrides();
            overrides.For("kniha").Case = governed;

            Assert.AreEqual(expected, Sentence(overrides, "student", "číst", lemma, "kniha"));
        }

        /// <summary>
        /// Where a word is both an adverb and a particle, the adverb wins — and that is a choice rather
        /// than a fact.
        /// </summary>
        /// <remarks>
        /// Forty-nine words are in both sets: <em>dobře</em>, <em>jistě</em>, <em>asi</em>,
        /// <em>prý</em>. The adverb reading wins because an adverb can be a constituent and a
        /// particle cannot, so calling <em>dobře</em> a particle would take it out of the sentence,
        /// while <em>asi</em> read as an adverb behaves exactly as a particle would — both are
        /// uninflected and neither declines. Deciding it word by word would need a list of words in the
        /// code, which is what the dictionary is for; <c>--druh</c> is the way out.
        /// </remarks>
        [DataTestMethod]
        [DataRow("dobře")]
        [DataRow("asi")]
        public void AdverbWinsOverParticleWhereBothApply(string lemma)
        {
            Assert.AreEqual(WordCategory.Adverb, CategoryOf(lemma));
        }

        /// <summary>
        /// A stated word class beats what the tool worked out, even where the tool had an answer.
        /// </summary>
        [TestMethod]
        public void StatedWordClassWins()
        {
            var overrides = new DraftOverrides();
            overrides.For("asi").WordCategory = WordCategory.Particle;

            Assert.AreEqual(
                WordCategory.Particle,
                Draft(overrides, "student", "číst", "asi")
                    .Constituents.Single(constituent => constituent.Lemma == "asi")
                    .Word.WordCategory);
        }

        /// <summary>
        /// A word the tool would otherwise call a noun becomes what the user says it is.
        /// </summary>
        [TestMethod]
        public void StatedWordClassOverridesTheGuess()
        {
            var overrides = new DraftOverrides();
            overrides.For("mimoň").WordCategory = WordCategory.Adverb;

            Assert.AreEqual(WordCategory.Noun, CategoryOf("mimoň"));
            Assert.AreEqual(
                WordCategory.Adverb,
                Draft(overrides, "student", "číst", "mimoň")
                    .Constituents.Single(constituent => constituent.Lemma == "mimoň")
                    .Word.WordCategory);
        }

        /// <summary>
        /// The degree reaches the sentence, regularly and irregularly.
        /// </summary>
        /// <remarks>
        /// <em>lépe</em> and <em>nejlépe</em> are not derived from <em>dobře</em> by any
        /// rule; they are registered forms, which is what makes them worth asserting — a rule-derived
        /// answer would be <em>dobřeji</em>.
        /// </remarks>
        [DataTestMethod]
        [DataRow("rychle", Degree.Comparative, "Student čte rychleji.")]
        [DataRow("dobře", Degree.Positive, "Student čte dobře.")]
        [DataRow("dobře", Degree.Comparative, "Student čte lépe.")]
        [DataRow("dobře", Degree.Superlative, "Student čte nejlépe.")]
        public void DegreeReachesTheSentence(string lemma, Degree degree, string expected)
        {
            var overrides = new DraftOverrides();
            overrides.For(lemma).Degree = degree;
            overrides.For(lemma).Functor = FgdFunctor.MANN;

            Assert.AreEqual(expected, Sentence(overrides, "student", "číst", lemma));
        }

        /// <summary>
        /// A degree stated on a class that does not compare is reported rather than quietly dropped.
        /// </summary>
        /// <remarks>
        /// A switch that does nothing and does not say so is worse than one that fails. It is a note and
        /// not an error because the sentence is otherwise sound, and refusing to build it over a switch
        /// that changed nothing would be out of proportion.
        /// </remarks>
        [TestMethod]
        public void DegreeOnAClassThatDoesNotCompareIsReported()
        {
            var overrides = new DraftOverrides();
            overrides.For("kniha").Degree = Degree.Comparative;

            var draft = Draft(overrides, "student", "číst", "kniha");

            Assert.IsTrue(
                draft.Notes.Any(note => note.Contains("Stupeň") && note.Contains("kniha")),
                "Mělo se to říct: " + string.Join(" | ", draft.Notes));
        }

        /// <summary>
        /// An uninflected word is not asked for a case it cannot have.
        /// </summary>
        [TestMethod]
        public void UninflectedWordIsNotAskedForACase()
        {
            var overrides = new DraftOverrides();
            overrides.For("rychle").Functor = FgdFunctor.MANN;

            Assert.IsFalse(
                Draft(overrides, "student", "číst", "rychle").Notes.Any(note => note.Contains("--pad")),
                "Příslovce pád nemá, tak se na něj nemá ptát.");
        }

        /// <summary>
        /// An adverb the dictionary carries reaches the sentence without anyone stating a role.
        /// </summary>
        /// <remarks>
        /// Recognizing the class was never enough on its own: an adverb is not a valency slot, so the
        /// role resolver had nothing to give it and every adverb stopped as an open question. Which
        /// circumstance an adverb expresses is not derivable — the ending says nothing, and neither does
        /// the adjective behind it, since <em>rychlý</em> and <em>rychle</em> are one word in two classes
        /// and only one answers "how" — so it is recorded per word in the dictionary.
        /// </remarks>
        [DataTestMethod]
        [DataRow("dnes", FgdFunctor.TWHEN, "Student čte knihu dnes.")]
        [DataRow("doma", FgdFunctor.LOC, "Student čte knihu doma.")]
        [DataRow("rychle", FgdFunctor.MANN, "Student čte knihu rychle.")]
        public void AdverbFromTheDictionaryCarriesItsOwnRole(
            string lemma, FgdFunctor expected, string sentence)
        {
            var draft = Draft(null, "student", "číst", "kniha", lemma);

            Assert.AreEqual(
                expected,
                draft.Constituents.Single(constituent => constituent.Lemma == lemma).Functor);

            Assert.AreEqual(sentence, Sentence(null, "student", "číst", "kniha", lemma));
        }

        /// <summary>
        /// An adverb the dictionary does not carry still asks, which is what it always did.
        /// </summary>
        /// <remarks>
        /// The column is sparse on purpose: twenty-one adverbs have it and the other two hundred and
        /// seventy in the rule data do not. Nothing about them got worse — they are recognized as
        /// adverbs and the caller states the role, exactly as before the column existed.
        /// </remarks>
        [TestMethod]
        public void AdverbOutsideTheDictionaryStillAsks()
        {
            var draft = Draft(null, "student", "číst", "kniha", "chytře");

            Assert.AreEqual(WordCategory.Adverb, CategoryOf("chytře"));
            Assert.IsNull(draft.Constituents.Single(constituent => constituent.Lemma == "chytře").Functor);
            Assert.IsTrue(draft.Gaps().Any(gap => gap.Contains("chytře")));
        }

        /// <summary>
        /// A stated role beats the one the dictionary records.
        /// </summary>
        [TestMethod]
        public void StatedRoleBeatsTheRecordedCircumstance()
        {
            var overrides = new DraftOverrides();
            overrides.For("dnes").Functor = FgdFunctor.MANN;

            Assert.AreEqual(
                FgdFunctor.MANN,
                Draft(overrides, "student", "číst", "kniha", "dnes")
                    .Constituents.Single(constituent => constituent.Lemma == "dnes").Functor);
        }

        /// <summary>
        /// Every one of the ten word classes reaches a finished sentence.
        /// </summary>
        /// <remarks>
        /// The two that took longest were the particle and the interjection, and not because they were
        /// hard to recognize: there was no functor to give them. Neither is a clause member — Czech
        /// grammar says <em>bez větněčlenské platnosti</em> — so no valency frame hands them a role, and
        /// the twenty-five functors this project had were all participants or circumstances. Forcing one
        /// on them would have recorded that <em>asi</em> answers "how", which it does not.
        /// </remarks>
        [DataTestMethod]
        [DataRow("Student čte knihu.", new[] { "student", "číst", "kniha" })]
        [DataRow("Mladý student čte knihu.", new[] { "mladý", "student", "číst", "kniha" })]
        [DataRow("Já čtu knihu.", new[] { "já", "číst", "kniha" })]
        [DataRow("Student čte pět knih.", new[] { "student", "číst", "pět", "kniha" })]
        [DataRow("Student čte knihu dnes.", new[] { "student", "číst", "kniha", "dnes" })]
        [DataRow("Student čte ve školu.", new[] { "student", "číst", "v", "škola" })]
        [DataRow("Student čte knihu ano.", new[] { "student", "číst", "kniha", "ano" })]
        [DataRow("Student čte knihu ach.", new[] { "student", "číst", "kniha", "ach" })]
        public void EveryWordClassReachesASentence(string expected, string[] lemmas)
        {
            Assert.AreEqual(expected, Sentence(null, lemmas));
        }

        /// <summary>
        /// A particle takes its functor from the group the rule data already sorts it into.
        /// </summary>
        /// <remarks>
        /// Nine groups of Nekula's classification against the functors of the Prague Dependency
        /// Treebank. Lining two classifications up is a rule and lives in code; a list of words would
        /// have belonged in the dictionary.
        /// </remarks>
        [DataTestMethod]
        [DataRow(ParticleType.Modal, FgdFunctor.MOD)]
        [DataRow(ParticleType.Optative, FgdFunctor.MOD)]
        [DataRow(ParticleType.Focusing, FgdFunctor.RHEM)]
        [DataRow(ParticleType.Intensifying, FgdFunctor.EXT)]
        [DataRow(ParticleType.Emotional, FgdFunctor.ATT)]
        [DataRow(ParticleType.Modifying, FgdFunctor.ATT)]
        [DataRow(ParticleType.Structuring, FgdFunctor.PREC)]
        [DataRow(ParticleType.Response, FgdFunctor.PARTL)]
        [DataRow(ParticleType.Negative, FgdFunctor.PARTL)]
        public void ParticleTypeDecidesTheFunctor(ParticleType type, FgdFunctor expected)
        {
            Assert.AreEqual(expected, ClassFunctors.Of(type));
        }

        /// <summary>
        /// An interjection is PARTL from being an interjection, with nothing to look up.
        /// </summary>
        /// <remarks>
        /// Unlike an adverb, whose circumstance is a fact about the word, and unlike a particle, whose
        /// group the rule data records: every interjection stands outside the clause the same way, so
        /// there is nothing to record per word.
        /// </remarks>
        [DataTestMethod]
        [DataRow("ach")]
        [DataRow("bum")]
        public void InterjectionIsPartlFromItsClass(string lemma)
        {
            var draft = Draft(null, "student", "číst", "kniha", lemma);

            Assert.AreEqual(
                FgdFunctor.PARTL,
                draft.Constituents.Single(constituent => constituent.Lemma == lemma).Functor);
        }

        /// <summary>
        /// A word that is both an adverb and a particle reads as an adverb and asks, and
        /// <c>--druh</c> is what settles it.
        /// </summary>
        /// <remarks>
        /// Forty-nine words are in both sets. Read as an adverb, <em>asi</em> would need a circumstance
        /// the dictionary does not record for it, so it stops and asks rather than inventing one; stated
        /// as a particle, its group answers.
        /// </remarks>
        [TestMethod]
        public void AmbiguousWordAsksUntilTheClassIsStated()
        {
            Assert.IsTrue(Draft(null, "student", "číst", "kniha", "asi").Gaps().Any(gap => gap.Contains("asi")));

            var overrides = new DraftOverrides();
            overrides.For("asi").WordCategory = WordCategory.Particle;

            Assert.AreEqual(
                FgdFunctor.MOD,
                Draft(overrides, "student", "číst", "kniha", "asi")
                    .Constituents.Single(constituent => constituent.Lemma == "asi").Functor);
        }

        /// <summary>
        /// The lexicon path is taken from the settings file the lexicon tool already uses, found by
        /// walking up from the working directory and read relative to the file it stands in.
        /// </summary>
        /// <remarks>
        /// Both tools work on the same dictionary and a project should not have to say where it is
        /// twice. Relative to the file rather than to the working directory, or the same setting would
        /// mean a different file in every subdirectory it is run from.
        /// </remarks>
        [TestMethod]
        [DoNotParallelize]
        public void LexiconPathComesFromTheSettingsFile()
        {
            var root = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), $"gramatika-nastaveni-{Guid.NewGuid():N}"));
            var deep = root.CreateSubdirectory("a").CreateSubdirectory("b");
            var previous = Directory.GetCurrentDirectory();

            try
            {
                File.WriteAllText(
                    Path.Combine(root.FullName, LexiconSettings.FileName),
                    """{ "database": "slovnik/lexikon.db" }""");

                Directory.SetCurrentDirectory(deep.FullName);

                Assert.AreEqual(
                    Path.Combine(root.FullName, "slovnik", "lexikon.db"),
                    LexiconSettings.DatabasePath());
            }
            finally
            {
                Directory.SetCurrentDirectory(previous);
                root.Delete(recursive: true);
            }
        }

        /// <summary>
        /// A settings file that says nothing about the dictionary falls through instead of overriding
        /// the environment with nothing.
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void SettingsFileWithoutADatabaseKeyFallsThrough()
        {
            var root = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), $"gramatika-nastaveni-{Guid.NewGuid():N}"));
            var previous = Directory.GetCurrentDirectory();

            try
            {
                File.WriteAllText(
                    Path.Combine(root.FullName, LexiconSettings.FileName),
                    """{ "url": "https://example.invalid/api/", "database": "" }""");

                Directory.SetCurrentDirectory(root.FullName);

                Assert.IsNull(LexiconSettings.DatabasePath());
            }
            finally
            {
                Directory.SetCurrentDirectory(previous);
                root.Delete(recursive: true);
            }
        }

        /// <summary>
        /// An inflected word is named as such rather than taken for a lemma of its own.
        /// </summary>
        /// <remarks>
        /// The tool builds sentences out of lemmas and does not read Czech, so <c>učitele</c> is not
        /// accepted — but it used to be guessed at, coming out as a feminine noun of the <em>růže</em>
        /// pattern in a sentence that looked almost right. Telling a form of a known word from a genuinely
        /// unknown one is what separates a question from a discovery.
        /// </remarks>
        [TestMethod]
        public void FormOfAKnownLemmaIsNamedAsAForm()
        {
            var draft = Draft(null, "učitele", "psát", "dopis");

            Assert.IsTrue(
                draft.Notes.Any(note => note.Contains("učitele") && note.Contains("'učitel'")),
                "Mělo se říct, čeho je to tvar: " + string.Join(" | ", draft.Notes));
        }

        /// <summary>
        /// A word that is neither a lemma nor a form of one is collected for the dictionary.
        /// </summary>
        // Sdílí soubor návrhů s ostatními testy třídy, které do něj taky sbírají, a ty běží
        // souběžně (Parallelize na úrovni metod). Bez tohohle si navzájem přepisují stav.
        [TestMethod]
        [DoNotParallelize]
        public void GenuinelyUnknownWordIsCollected()
        {
            var proposals = services.GetRequiredService<WordProposals>();
            proposals.Clear();

            Draft(null, "zahradník", "kopat", "záhon");

            CollectionAssert.AreEquivalent(
                new[] { "zahradník", "kopat", "záhon" },
                proposals.Read().Select(proposal => proposal.Lemma).ToArray());
        }

        /// <summary>
        /// Seeing the same word twice does not record it twice, nor overwrite what was said about it.
        /// </summary>
        // Sdílí soubor návrhů s ostatními testy třídy, které do něj taky sbírají, a ty běží
        // souběžně (Parallelize na úrovni metod). Bez tohohle si navzájem přepisují stav.
        [TestMethod]
        [DoNotParallelize]
        public void CollectingTheSameWordTwiceKeepsTheFirstRecord()
        {
            var proposals = services.GetRequiredService<WordProposals>();
            proposals.Clear();

            Draft(null, "zahradník", "kopat", "záhon");
            proposals.Write([.. proposals.Read().Select(proposal =>
            {
                proposal.IsConfirmed = true;

                return proposal;
            })]);
            Draft(null, "zahradník", "kopat", "záhon");

            Assert.AreEqual(3, proposals.Read().Count);
            Assert.IsTrue(proposals.Read().All(proposal => proposal.IsConfirmed), "Potvrzení se nemá přepsat.");
        }

        /// <summary>
        /// A verb the dictionary holds no frame for still produces a sentence, from word order.
        /// </summary>
        /// <remarks>
        /// The dictionary has frames for sixty verbs, so this is the ordinary case and not the edge
        /// one. Without it every other verb is a wall: no frame means no slots, no slots means no roles,
        /// and no roles means no sentence.
        /// </remarks>
        [TestMethod]
        public void VerbWithoutAFrameStillMakesASentence()
        {
            Assert.AreEqual(
                "Učitel daruje knihu studentovi.",
                Sentence(null, "učitel", "darovat", "kniha", "student"));
        }

        /// <summary>
        /// The invented roles are the unmarked Czech order, and they are reported as invented.
        /// </summary>
        [TestMethod]
        public void InventedRolesAreOrderedAndReported()
        {
            var draft = Draft(null, "učitel", "darovat", "kniha", "student");

            CollectionAssert.AreEqual(
                new[] { FgdFunctor.ACT, FgdFunctor.PAT, FgdFunctor.ADDR },
                draft.Constituents.Select(constituent => constituent.Functor).ToArray());

            Assert.IsTrue(
                draft.Constituents.All(constituent => constituent.FunctorIsGuessed),
                "Role odhadnuté z pořadí se mají značit.");

            Assert.IsTrue(
                draft.Notes.Any(note => note.Contains("podle pořadí")),
                "A oznámit: " + string.Join(" | ", draft.Notes));
        }

        /// <summary>
        /// A stated role wins over the order, and the rest is dealt out around it.
        /// </summary>
        [TestMethod]
        public void StatedRoleWinsOverTheOrder()
        {
            var overrides = new DraftOverrides();
            overrides.For("zahrada").Functor = FgdFunctor.LOC;
            overrides.For("zahrada").Preposition = "v";
            overrides.For("zahrada").Case = Case.Locative;

            Assert.AreEqual("Pes běhá v zahradě.", Sentence(overrides, "pes", "běhat", "zahrada"));
        }

        /// <summary>
        /// A verb the dictionary does hold keeps its own frame, guesses nothing, and marks nothing.
        /// </summary>
        [TestMethod]
        public void VerbWithAFrameIsNotGuessedAt()
        {
            var draft = Draft(null, "učitel", "psát", "dopis", "student");

            Assert.IsFalse(draft.Constituents.Any(constituent => constituent.FunctorIsGuessed));
            Assert.IsFalse(draft.Notes.Any(note => note.Contains("podle pořadí")));
        }

        /// <summary>
        /// A constituent opened by a preposition of several rections is reported as an open case, which
        /// is what it is — the role follows from the case and the library reads it off the preposition.
        /// </summary>
        /// <remarks>
        /// <em>v zahradě</em> and <em>v zahradu</em> are where and whither. Telling the user to supply a
        /// role there sends them after something they cannot work out and did not get wrong.
        /// </remarks>
        [TestMethod]
        public void PrepositionWithSeveralRectionsAsksForTheCase()
        {
            var draft = Draft(null, "pes", "běhat", "v", "zahrada");

            Assert.IsTrue(
                draft.Gaps().Any(gap => gap.Contains("--pad") && gap.Contains("lokál")),
                "Otevřený je pád, ne role: " + string.Join(" | ", draft.Gaps()));
        }

        /// <summary>
        /// A lemma written without diacritics reaches the entry the dictionary holds it under.
        /// </summary>
        /// <remarks>
        /// Typing Czech on a keyboard that is not set up for it is the ordinary case for this tool, and
        /// refusing <c>ucitel</c> would mean refusing most of what gets typed at it.
        /// </remarks>
        [TestMethod]
        public void LemmaWithoutDiacriticsFindsItsEntry()
        {
            Assert.AreEqual(
                "Učitel píše dopis studentovi.",
                Sentence(null, "ucitel", "psat", "dopis", "student"));
        }

        /// <summary>
        /// The completed spelling is reported, because the sentence contains a word nobody wrote.
        /// </summary>
        [TestMethod]
        public void CompletedSpellingIsReported()
        {
            var draft = Draft(null, "ucitel", "psat", "dopis", "student");

            Assert.IsTrue(
                draft.Notes.Any(note => note.Contains("ucitel") && note.Contains("učitel")),
                "Doplnění diakritiky se má oznámit: " + string.Join(" | ", draft.Notes));
        }

        /// <summary>
        /// A word already spelled the way the dictionary spells it says nothing about spelling.
        /// </summary>
        [TestMethod]
        public void ExactSpellingIsNotReported()
        {
            var draft = Draft(null, "učitel", "psát", "dopis", "student");

            Assert.IsFalse(
                draft.Notes.Any(note => note.Contains("diakritiku")),
                "Nic se nedoplňovalo, tak se nemá nic hlásit.");
        }

        /// <summary>
        /// A switch reaches its word under either spelling, so a correction never has to be retyped
        /// with diacritics the user could not produce in the first place.
        /// </summary>
        [DataTestMethod]
        [DataRow("ucitel")]
        [DataRow("učitel")]
        [DataRow("UČITEL")]
        public void SwitchReachesItsWordUnderEitherSpelling(string target)
        {
            var overrides = new DraftOverrides();
            overrides.For(target).Case = Case.Dative;

            Assert.AreEqual(
                "Učiteli píše dopis studentovi.",
                Sentence(overrides, "ucitel", "psat", "dopis", "student"));
        }

        /// <summary>
        /// A whole sentence in one argument is refused rather than taken as one enormous lemma.
        /// </summary>
        /// <remarks>
        /// It used to reach the library and come back as <em>Verb pattern &apos;učitel psát dopis
        /// student&apos; not found</em> — an English sentence about inflection patterns, for someone who
        /// only put quotes in the wrong place.
        /// </remarks>
        [TestMethod]
        public void WholeSentenceInOneArgumentIsRefused()
        {
            var exception = Assert.ThrowsException<CliException>(
                () => Whole(null, "učitel psát dopis student"));

            StringAssert.Contains(exception.Message, "zvlášť");
        }
    }
}
