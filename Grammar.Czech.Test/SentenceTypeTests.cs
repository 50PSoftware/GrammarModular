using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the communicative force of a clause: interrogative focus fronting, its interaction with the
    /// clitic cluster, and the checks that keep an inconsistent clause from being linearized silently.
    /// </summary>
    /// <remarks>
    /// The wh-words used here are pronouns, because WordCategory has no adverb — "jak" and "kde" cannot be
    /// realized at all yet, so the examples are built from kdo and co.
    /// </remarks>
    [TestClass]
    public sealed class SentenceTypeTests
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
            string lemma = "dělat",
            string pattern = "dělá",
            Person person = Person.Third,
            ReflexiveType reflexive = ReflexiveType.None,
            Tense tense = Tense.Past) => new()
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Modus = Modus.Indicative,
                Tense = tense,
                Aspect = VerbAspect.Imperfective,
                Voice = Voice.Active,
                Person = person,
                Number = Number.Singular,
                Gender = Gender.Masculine,
                ReflexiveType = reflexive
            };

        // Deliberately without a case where a valency frame can supply one.
        private static CzechWordRequest WhPronoun(string lemma, Case? @case = null) => new()
        {
            Lemma = lemma,
            WordCategory = WordCategory.Pronoun,
            Gender = lemma == "kdo" ? Gender.Masculine : Gender.Neuter,
            IsAnimate = lemma == "kdo",
            Number = Number.Singular,
            Case = @case
        };

        private static ClauseElement Student(InformationStatus status = InformationStatus.Given) =>
            ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = "student",
                    Pattern = "pán",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Masculine,
                    IsAnimate = true,
                    Number = Number.Singular,
                    Case = Case.Nominative
                },
                FgdFunctor.ACT,
                status);

        #region Declarative default

        /// <summary>
        /// The field defaults to declarative, so a clause built exactly as before comes out exactly as before.
        /// </summary>
        [TestMethod]
        public void SentenceType_DefaultsToDeclarative_ExistingBehaviorUnchanged()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(reflexive: ReflexiveType.ReflexivumTantum_Se),
                Elements = [Student()]
            };

            Assert.AreEqual(SentenceType.Declarative, clause.SentenceType);
            Assert.AreEqual("Student se dělal.", builder.Build(clause));
        }

        /// <summary>
        /// Force and punctuation are separate: a rhetorical question stays declarative and still ends in a
        /// question mark, and nothing about its word order changes.
        /// </summary>
        [TestMethod]
        public void Build_DeclarativeSentenceType_TerminatorUnaffected()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(reflexive: ReflexiveType.ReflexivumTantum_Se),
                Elements = [Student()],
                Terminator = "?"
            };

            Assert.AreEqual("Student se dělal?", builder.Build(clause));
        }

        #endregion Declarative default

        #region Interrogative focus

        /// <summary>
        /// The interrogative focus opens the clause, ahead of both contrastive and given material.
        /// </summary>
        [TestMethod]
        public void RenderClause_InterrogativeFocus_FrontsAheadOfContrastiveAndGiven()
        {
            var contrastive = ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = "kniha",
                    Pattern = "žena",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Feminine,
                    Number = Number.Singular,
                    Case = Case.Accusative
                },
                FgdFunctor.PAT,
                InformationStatus.Contrastive);

            var clause = new CzechClause
            {
                SentenceType = SentenceType.Interrogative,
                Predicate = Verb(),
                Elements =
                [
                    Student(),
                    contrastive,
                    ClauseElement.Of(WhPronoun("kdo", Case.Dative), FgdFunctor.BEN, InformationStatus.Interrogative)
                ],
                Terminator = "?"
            };

            Assert.AreEqual("Komu knihu student dělal?", builder.Build(clause));
        }

        /// <summary>
        /// The clitic cluster follows the wh-word, which is what second position means once the wh-word
        /// opens the clause.
        /// </summary>
        [TestMethod]
        public void RenderClause_InterrogativeFocusWithClitic_CliticFollowsWhWord()
        {
            var clause = new CzechClause
            {
                SentenceType = SentenceType.Interrogative,
                Predicate = Verb(lemma: "vidět", pattern: "trida4", person: Person.Second),
                // No case: the valency frame of vidět makes the patient accusative.
                Elements = [ClauseElement.Of(WhPronoun("kdo"), FgdFunctor.PAT, InformationStatus.Interrogative)],
                Terminator = "?"
            };

            Assert.AreEqual("Koho jsi viděl?", builder.Build(clause));
        }

        /// <summary>
        /// The reflexive lands in the same place as the auxiliary does.
        /// </summary>
        [TestMethod]
        public void RenderClause_InterrogativeFocusWithReflexive_CliticFollowsWhWord()
        {
            var clause = new CzechClause
            {
                SentenceType = SentenceType.Interrogative,
                Predicate = Verb(reflexive: ReflexiveType.ReflexivumTantum_Se),
                Elements = [ClauseElement.Of(WhPronoun("co", Case.Accusative), FgdFunctor.PAT, InformationStatus.Interrogative)],
                Terminator = "?"
            };

            Assert.AreEqual("Co se dělal?", builder.Build(clause));
        }

        #endregion Interrogative focus

        #region Consistency

        /// <summary>
        /// Two wh-words in one clause are out of scope and say so.
        /// </summary>
        [TestMethod]
        public void RenderClause_MultipleInterrogativeElements_Throws()
        {
            var clause = new CzechClause
            {
                SentenceType = SentenceType.Interrogative,
                Predicate = Verb(),
                Elements =
                [
                    ClauseElement.Of(WhPronoun("kdo", Case.Nominative), FgdFunctor.ACT, InformationStatus.Interrogative),
                    ClauseElement.Of(WhPronoun("co", Case.Accusative), FgdFunctor.PAT, InformationStatus.Interrogative)
                ],
                Terminator = "?"
            };

            var exception = Assert.ThrowsException<NotSupportedException>(() => builder.Build(clause));
            StringAssert.Contains(exception.Message, "tázacích");
        }

        /// <summary>
        /// Fronting applies unconditionally, so an interrogative element on a declarative clause would
        /// produce a question's word order with a statement's force. Reported instead.
        /// </summary>
        [TestMethod]
        public void RenderClause_InterrogativeStatusOnDeclarativeSentence_Throws()
        {
            var clause = new CzechClause
            {
                Predicate = Verb(),
                Elements = [ClauseElement.Of(WhPronoun("co", Case.Accusative), FgdFunctor.PAT, InformationStatus.Interrogative)]
            };

            var exception = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(clause));
            StringAssert.Contains(exception.Message, "SentenceType.Interrogative");
        }

        /// <summary>
        /// An interrogative clause with nothing fronted is a yes/no question, which is valid and needs no
        /// word-order change at all.
        /// </summary>
        [TestMethod]
        public void RenderClause_InterrogativeSentenceTypeWithoutFocusElement_YesNoQuestion_DoesNotThrow()
        {
            var clause = new CzechClause
            {
                SentenceType = SentenceType.Interrogative,
                Predicate = Verb(reflexive: ReflexiveType.ReflexivumTantum_Se),
                Elements = [Student()],
                Terminator = "?"
            };

            Assert.AreEqual("Student se dělal?", builder.Build(clause));
        }

        /// <summary>
        /// A wh-word inside a clause already opened by a conjunction is refused: an indirect question is
        /// introduced by the wh-word itself, not by a conjunction with a wh-word behind it, so the two
        /// claims on first position do not describe a real sentence.
        /// </summary>
        [TestMethod]
        public void RenderClause_InterrogativeFocusInsideSubordinateClause_Throws()
        {
            var subordinate = new CzechClause
            {
                SentenceType = SentenceType.Interrogative,
                Predicate = Verb(person: Person.Second),
                Elements = [ClauseElement.Of(WhPronoun("co", Case.Accusative), FgdFunctor.PAT, InformationStatus.Interrogative)]
            };

            var sentence = new Subordination(
                new CzechClause { Predicate = Verb(), Elements = [Student()] },
                "protože",
                subordinate);

            var exception = Assert.ThrowsException<NotSupportedException>(() => builder.Build(sentence));
            StringAssert.Contains(exception.Message, "spojkou");
        }

        #endregion Consistency

        #region Yes/no questions

        /// <summary>
        /// Czech has neither do-support nor obligatory inversion, so a yes/no question is the declarative
        /// word order with a question mark. This needed no new logic and exists to keep it that way.
        /// </summary>
        [TestMethod]
        public void Build_YesNoQuestion_DeclarativeWordOrderWithQuestionMark()
        {
            var predicate = new CzechWordRequest
            {
                Lemma = "dělat",
                Pattern = "dělá",
                WordCategory = WordCategory.Verb,
                Modus = Modus.Indicative,
                Tense = Tense.Present,
                Aspect = VerbAspect.Imperfective,
                Voice = Voice.Active,
                Person = Person.Second,
                Number = Number.Singular
            };

            var clause = new CzechClause
            {
                SentenceType = SentenceType.Interrogative,
                Predicate = predicate,
                Terminator = "?"
            };

            Assert.AreEqual("Děláš?", builder.Build(clause));
        }

        #endregion Yes/no questions
    }
}
