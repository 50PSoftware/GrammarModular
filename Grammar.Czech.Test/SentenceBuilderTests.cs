using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies clause linearization: subject agreement, functional sentence perspective,
    /// and Wackernagel placement of the clitic cluster.
    /// </summary>
    [TestClass]
    public sealed class SentenceBuilderTests
    {
        private static CzechSentenceBuilder builder = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            builder = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })
                              .GetRequiredService<CzechSentenceBuilder>();
        }

        private static CzechWordRequest Verb(
            ReflexiveType reflexive = ReflexiveType.None,
            Modus modus = Modus.Indicative,
            Tense tense = Tense.Present,
            VerbAspect? aspect = null,
            bool isNegative = false) => new()
            {
                Lemma = "dělat",
                Pattern = "dělá",
                WordCategory = WordCategory.Verb,
                Modus = modus,
                Tense = tense,
                Aspect = aspect,
                Voice = Voice.Active,
                Person = Person.Third,
                Number = Number.Singular,
                Gender = Gender.Masculine,
                IsNegative = isNegative,
                ReflexiveType = reflexive
            };

        private static ClauseElement Noun(string lemma, string pattern, Case @case, Gender gender, InformationStatus status, FgdFunctor functor) =>
            ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = lemma,
                    Pattern = pattern,
                    WordCategory = WordCategory.Noun,
                    Gender = gender,
                    Number = Number.Singular,
                    IsAnimate = gender == Gender.Masculine,
                    Case = @case
                },
                functor,
                status);

        private static ClauseElement Pronoun(string lemma, Number number) =>
            ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = lemma,
                    WordCategory = WordCategory.Pronoun,
                    Case = Case.Nominative,
                    Gender = Gender.Masculine,
                    Number = number
                },
                FgdFunctor.ACT,
                InformationStatus.Given);

        private static ClauseElement Subject(string lemma = "student", InformationStatus status = InformationStatus.Given) =>
            Noun(lemma, "pán", Case.Nominative, Gender.Masculine, status, FgdFunctor.ACT);

        #region Functional sentence perspective

        /// <summary>
        /// Given material forms the theme before the verb, new material the rheme after it.
        /// </summary>
        [TestMethod]
        public void Build_GivenSubjectAndNewObject_PutsThemeFirstAndRhemeLast()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements =
                [
                    Subject(),
                    Noun("úkol", "hrad", Case.Accusative, Gender.Masculine, InformationStatus.New, FgdFunctor.PAT)
                ]
            };

            Assert.AreEqual("Student dělá úkol.", builder.Build(clause));
        }

        /// <summary>
        /// A new subject belongs to the rheme, so it follows the verb.
        /// </summary>
        [TestMethod]
        public void Build_NewSubject_FollowsTheVerb()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements = [Subject(status: InformationStatus.New)]
            };

            Assert.AreEqual("Dělá student.", builder.Build(clause));
        }

        /// <summary>
        /// Contrastive material is fronted ahead of the theme.
        /// </summary>
        [TestMethod]
        public void Build_ContrastiveElement_IsFrontedAheadOfTheTheme()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements =
                [
                    Subject(),
                    Noun("úkol", "hrad", Case.Accusative, Gender.Masculine, InformationStatus.Contrastive, FgdFunctor.PAT)
                ]
            };

            Assert.AreEqual("Úkol student dělá.", builder.Build(clause));
        }

        #endregion Functional sentence perspective

        #region Wackernagel position

        /// <summary>
        /// The bug this builder exists for: the clitic follows the first constituent even when that
        /// constituent is not the subject.
        /// </summary>
        [TestMethod]
        public void Build_FrontedNonSubject_PlacesCliticAfterIt()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, tense: Tense.Past),
                Elements = [Noun("večer", "hrad", Case.Nominative, Gender.Masculine, InformationStatus.Given, FgdFunctor.TWHEN)]
            };

            Assert.AreEqual("Večer se dělal.", builder.Build(clause));
        }

        /// <summary>
        /// The cluster follows the first constituent only — not all pre-verbal constituents.
        /// </summary>
        [TestMethod]
        public void Build_TwoPreVerbalConstituents_PlacesCliticAfterTheFirstOnly()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, tense: Tense.Past),
                Elements =
                [
                    Subject(),
                    Noun("večer", "hrad", Case.Nominative, Gender.Masculine, InformationStatus.Given, FgdFunctor.TWHEN)
                ]
            };

            Assert.AreEqual("Student se večer dělal.", builder.Build(clause));
        }

        /// <summary>
        /// With nothing before the verb, the verb opens the clause and the clitic follows its first word.
        /// </summary>
        [TestMethod]
        public void Build_VerbInitialClause_PlacesCliticAfterTheVerb()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, tense: Tense.Past)
            };

            Assert.AreEqual("Dělal se.", builder.Build(clause));
        }

        /// <summary>
        /// The conditional particle outranks the reflexive inside the cluster, and the whole cluster moves together.
        /// </summary>
        [TestMethod]
        public void Build_ConditionalWithReflexive_KeepsClusterOrderAndMovesItTogether()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, modus: Modus.Conditional),
                Elements = [Subject()]
            };

            Assert.AreEqual("Student by se dělal.", builder.Build(clause));
        }

        /// <summary>
        /// Verb-initial conditional keeps the cluster after the participle.
        /// </summary>
        [TestMethod]
        public void Build_VerbInitialConditionalWithReflexive_PlacesClusterAfterTheParticiple()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, modus: Modus.Conditional)
            };

            Assert.AreEqual("Dělal by se.", builder.Build(clause));
        }

        /// <summary>
        /// Budu carries stress, so it opens a verb-initial clause and the clitic follows it;
        /// with a constituent in front, the clitic goes there instead.
        /// </summary>
        [TestMethod]
        public void Build_PeriphrasticFutureWithReflexive_PlacesCliticAfterTheFirstStressedWord()
        {
            var verbInitial = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, tense: Tense.Future, aspect: VerbAspect.Imperfective)
            };

            Assert.AreEqual("Bude se dělat.", builder.Build(verbInitial));

            var withSubject = verbInitial with { Elements = [Subject()] };

            Assert.AreEqual("Student se bude dělat.", builder.Build(withSubject));
        }

        /// <summary>
        /// Negation attaches to the verb; the cluster still sits in second position.
        /// </summary>
        [TestMethod]
        public void Build_NegatedConditionalWithReflexive_KeepsNegationOnTheVerb()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, modus: Modus.Conditional, isNegative: true),
                Elements = [Subject()]
            };

            Assert.AreEqual("Student by se nedělal.", builder.Build(clause));
        }

        /// <summary>
        /// The compound past auxiliary joins the cluster at the same rank as the conditional particle,
        /// and the whole cluster moves into second position together.
        /// </summary>
        [TestMethod]
        public void Build_PastWithAuxiliaryAndReflexive_MovesTheWholeClusterIntoSecondPosition()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, tense: Tense.Past),
                Elements = [Pronoun("já", Number.Singular)]
            };

            Assert.AreEqual("Já jsem se dělal.", builder.Build(clause));
        }

        /// <summary>
        /// Verb-initial past keeps the cluster after the participle.
        /// </summary>
        [TestMethod]
        public void Build_VerbInitialPastWithReflexive_PlacesClusterAfterTheParticiple()
        {
            var predicate = Verb(ReflexiveType.ReflexivumTantum_Se, tense: Tense.Past);
            predicate.Person = Person.First;

            Assert.AreEqual("Dělal jsem se.", builder.Build(new CzechClause { Predicate = predicate }));
        }

        /// <summary>
        /// The second-person auxiliary fuses with the reflexive after the cluster has been seated.
        /// </summary>
        [TestMethod]
        public void Build_SecondPersonPastWithReflexive_ContractsTheAuxiliary()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, tense: Tense.Past),
                Elements = [Pronoun("ty", Number.Singular)]
            };

            Assert.AreEqual("Ty ses dělal.", builder.Build(clause));
        }

        /// <summary>
        /// A fronted non-subject takes the auxiliary cluster too, not just the reflexive.
        /// </summary>
        [TestMethod]
        public void Build_FrontedNonSubjectWithPastAuxiliary_PlacesClusterAfterIt()
        {
            var predicate = Verb(ReflexiveType.ReflexivumTantum_Se, tense: Tense.Past);
            predicate.Person = Person.First;

            var clause = new CzechClause
            {
                Predicate = predicate,
                Elements = [Noun("večer", "hrad", Case.Nominative, Gender.Masculine, InformationStatus.Given, FgdFunctor.TWHEN)]
            };

            Assert.AreEqual("Večer jsem se dělal.", builder.Build(clause));
        }

        /// <summary>
        /// The third person takes no auxiliary, so the cluster holds the reflexive alone.
        /// </summary>
        [TestMethod]
        public void Build_ThirdPersonPast_HasNoAuxiliaryInTheCluster()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, tense: Tense.Past),
                Elements = [Subject()]
            };

            Assert.AreEqual("Student se dělal.", builder.Build(clause));
        }

        #endregion Wackernagel position

        #region Short pronouns

        private static ClauseElement ObjectPronoun(string lemma, Case @case, InformationStatus status = InformationStatus.Given) =>
            ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = lemma,
                    WordCategory = WordCategory.Pronoun,
                    Case = @case,
                    Gender = Gender.Masculine,
                    Number = Number.Singular,
                    IsAnimate = true
                },
                @case == Case.Dative ? FgdFunctor.ADDR : FgdFunctor.PAT,
                status);

        /// <summary>
        /// A short pronoun object leaves the constituent order and joins the cluster in its clitic form.
        /// </summary>
        [TestMethod]
        public void Build_AccusativePronounObject_JoinsTheClusterAsAClitic()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(tense: Tense.Past),
                Elements = [Pronoun("já", Number.Singular), ObjectPronoun("on", Case.Accusative)]
            };

            Assert.AreEqual("Já jsem ho dělal.", builder.Build(clause));
        }

        /// <summary>
        /// Dative precedes accusative inside the cluster.
        /// </summary>
        [TestMethod]
        public void Build_DativeAndAccusativePronouns_OrdersDativeFirst()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(tense: Tense.Past),
                Elements =
                [
                    Pronoun("já", Number.Singular),
                    ObjectPronoun("on", Case.Accusative),
                    ObjectPronoun("ona", Case.Dative)
                ]
            };

            Assert.AreEqual("Já jsem jí ho dělal.", builder.Build(clause));
        }

        /// <summary>
        /// The reflexive outranks both pronoun slots.
        /// </summary>
        [TestMethod]
        public void Build_ReflexiveAndAccusativePronoun_KeepsTheReflexiveFirst()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.DerivedBenefactive_Si, tense: Tense.Past),
                Elements = [Pronoun("já", Number.Singular), ObjectPronoun("on", Case.Accusative)]
            };

            Assert.AreEqual("Já jsem si ho dělal.", builder.Build(clause));
        }

        /// <summary>
        /// The cluster moves as a whole, so a pronoun clitic follows a fronted non-subject too.
        /// </summary>
        [TestMethod]
        public void Build_FrontedNonSubjectWithPronounClitic_PlacesTheWholeClusterAfterIt()
        {
            var predicate = Verb(tense: Tense.Past);
            predicate.Person = Person.First;

            var clause = new CzechClause
            {
                Predicate = predicate,
                Elements =
                [
                    Noun("večer", "hrad", Case.Nominative, Gender.Masculine, InformationStatus.Given, FgdFunctor.TWHEN),
                    ObjectPronoun("on", Case.Accusative)
                ]
            };

            Assert.AreEqual("Večer jsem ho dělal.", builder.Build(clause));
        }

        /// <summary>
        /// A contrastive pronoun is stressed, so it keeps its long form and its place in the constituent order.
        /// Being fronted, it also becomes the first constituent, and the cluster lands right after it —
        /// ahead of the subject, which second position requires.
        /// </summary>
        [TestMethod]
        public void Build_ContrastivePronoun_StaysOutOfTheCluster()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(tense: Tense.Past),
                Elements =
                [
                    Pronoun("já", Number.Singular),
                    ObjectPronoun("on", Case.Dative, InformationStatus.Contrastive)
                ]
            };

            Assert.AreEqual("Jemu jsem já dělal.", builder.Build(clause));
        }

        /// <summary>
        /// A pronoun inside a prepositional phrase belongs to that phrase and cannot be extracted;
        /// it also takes the prepositional form něj rather than the clitic ho.
        /// The clause model has no preposition of its own yet, so the expected string is the pronoun
        /// alone — this pins the extraction rule, not a well-formed sentence.
        /// </summary>
        [TestMethod]
        public void Build_PronounAfterPreposition_StaysOutOfTheCluster()
        {
            var pronoun = new CzechWordRequest
            {
                Lemma = "on",
                WordCategory = WordCategory.Pronoun,
                Case = Case.Accusative,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                IsAnimate = true,
                IsAfterPreposition = true
            };

            var clause = new CzechClause
            {
                Predicate = Verb(tense: Tense.Past),
                Elements =
                [
                    Pronoun("já", Number.Singular),
                    ClauseElement.Of(pronoun, FgdFunctor.PAT, InformationStatus.New)
                ]
            };

            Assert.AreEqual("Já jsem dělal něj.", builder.Build(clause));
        }

        #endregion Short pronouns

        #region Phrasal constituents

        private static CzechWordRequest Attribute(string lemma, string pattern) => new()
        {
            Lemma = lemma,
            Pattern = pattern,
            WordCategory = WordCategory.Adjective,
            Degree = Degree.Positive
        };

        /// <summary>
        /// The bug this feature exists for: second position falls after the whole phrase,
        /// not after its first word.
        /// </summary>
        [TestMethod]
        public void Build_ModifiedSubject_PlacesClusterAfterTheWholePhrase()
        {
            var subject = ClauseElement.Of(
                Subject().Word,
                [Attribute("mladý", "mladý")],
                FgdFunctor.ACT,
                InformationStatus.Given);

            var clause = new CzechClause
            {
                Predicate = Verb(ReflexiveType.ReflexivumTantum_Se, tense: Tense.Past),
                Elements = [subject]
            };

            Assert.AreEqual("Mladý student se dělal.", builder.Build(clause));
        }

        /// <summary>
        /// The attribute inherits gender, number, case and animacy from the head.
        /// </summary>
        /// <param name="lemma">The head noun lemma.</param>
        /// <param name="pattern">The head noun pattern.</param>
        /// <param name="gender">The head noun gender.</param>
        /// <param name="case">The case of the whole phrase.</param>
        /// <param name="expected">The expected phrase.</param>
        [DataTestMethod]
        [DataRow("student", "pán", "Masculine", "Nominative", "Mladý student")]
        [DataRow("žena", "žena", "Feminine", "Nominative", "Mladá žena")]
        [DataRow("město", "město", "Neuter", "Nominative", "Mladé město")]
        [DataRow("student", "pán", "Masculine", "Dative", "Mladému studentovi")]
        public void Build_ModifiedConstituent_AgreesTheAttributeWithTheHead(
            string lemma, string pattern, string gender, string @case, string expected)
        {
            var head = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Noun,
                Gender = Enum.Parse<Gender>(gender),
                Number = Number.Singular,
                IsAnimate = gender == "Masculine",
                Case = Enum.Parse<Case>(@case)
            };

            var predicate = Verb();
            predicate.Person = Person.Third;

            var clause = new CzechClause
            {
                Predicate = predicate,
                Elements = [ClauseElement.Of(head, [Attribute("mladý", "mladý")], FgdFunctor.PAT, InformationStatus.Contrastive)]
            };

            // The phrase is fronted, so it opens the sentence and can be read off the front.
            StringAssert.StartsWith(builder.Build(clause), expected);
        }

        /// <summary>
        /// An attribute that carries its own case keeps it rather than inheriting the head's.
        /// </summary>
        [TestMethod]
        public void Build_AttributeWithExplicitCase_KeepsIt()
        {
            var attribute = Attribute("mladý", "mladý");
            attribute.Case = Case.Genitive;
            attribute.Gender = Gender.Masculine;
            attribute.Number = Number.Singular;
            attribute.IsAnimate = true;

            var head = new CzechWordRequest
            {
                Lemma = "student",
                Pattern = "pán",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                IsAnimate = true,
                Case = Case.Nominative
            };

            var predicate = Verb();
            predicate.Person = Person.Third;

            var clause = new CzechClause
            {
                Predicate = predicate,
                Elements = [ClauseElement.Of(head, [attribute], FgdFunctor.ACT, InformationStatus.Given)]
            };

            Assert.AreEqual("Mladého student dělá.", builder.Build(clause));
        }

        /// <summary>
        /// A pronoun that heads a phrase is no longer a weak word, so it stays out of the cluster.
        /// </summary>
        [TestMethod]
        public void Build_ModifiedPronoun_StaysOutOfTheCluster()
        {
            var pronoun = new CzechWordRequest
            {
                Lemma = "on",
                WordCategory = WordCategory.Pronoun,
                Case = Case.Accusative,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                IsAnimate = true
            };

            var clause = new CzechClause
            {
                Predicate = Verb(tense: Tense.Past),
                Elements =
                [
                    Pronoun("já", Number.Singular),
                    ClauseElement.Of(pronoun, [Attribute("mladý", "mladý")], FgdFunctor.PAT, InformationStatus.New)
                ]
            };

            Assert.AreEqual("Já jsem dělal mladého jeho.", builder.Build(clause));
        }

        #endregion Phrasal constituents

        #region Agreement

        /// <summary>
        /// Person, number and gender of the predicate follow the nominative actor.
        /// </summary>
        /// <param name="pronoun">The subject pronoun lemma.</param>
        /// <param name="expected">The expected sentence.</param>
        [DataTestMethod]
        [DataRow("já", "Já dělám.")]
        [DataRow("ty", "Ty děláš.")]
        [DataRow("my", "My děláme.")]
        [DataRow("vy", "Vy děláte.")]
        public void Build_PronounSubject_AgreesInPerson(string pronoun, string expected)
        {
            var subject = new CzechWordRequest
            {
                Lemma = pronoun,
                WordCategory = WordCategory.Pronoun,
                Case = Case.Nominative,
                Number = pronoun is "my" or "vy" ? Number.Plural : Number.Singular
            };

            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements = [ClauseElement.Of(subject, FgdFunctor.ACT, InformationStatus.Given)]
            };

            Assert.AreEqual(expected, builder.Build(clause));
        }

        #endregion Agreement
    }
}
