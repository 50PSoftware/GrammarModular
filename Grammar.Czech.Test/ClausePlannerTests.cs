using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the stage that decides how a slot filled by a proposition comes out: as an infinitive
    /// inside the clause, or as a dependent clause behind a conjunction.
    /// </summary>
    /// <remarks>
    /// Which of the two it is comes from the valency frame and never from the caller, so the same call
    /// produces "chce číst" for one verb and "ví, že čte" for another. That is the whole point of the
    /// stage, and every test here states the proposition the same way.
    /// </remarks>
    [TestClass]
    public sealed class ClausePlannerTests
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

        private static CzechWordRequest Verb(string lemma, Tense tense = Tense.Present) => new()
        {
            Lemma = lemma,
            Pattern = lemma,
            WordCategory = WordCategory.Verb,
            Modus = Modus.Indicative,
            Tense = tense,
            Voice = Voice.Active,
            Aspect = VerbAspect.Imperfective,
            Person = Person.Third,
            Number = Number.Singular,
            Gender = Gender.Masculine
        };

        private static ClauseElement Subject(string lemma = "student") => ClauseElement.Of(
            new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = "pán",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Masculine,
                IsAnimate = true,
                Number = Number.Singular,
                Case = Case.Nominative
            },
            FgdFunctor.ACT,
            InformationStatus.Given);

        private static ClauseElement Book() => ClauseElement.Of(
            new CzechWordRequest
            {
                Lemma = "kniha",
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular
            },
            FgdFunctor.PAT);

        /// <summary>
        /// A slot the frame records as taking an infinitive comes out as one, inside the same clause.
        /// </summary>
        [TestMethod]
        public void InfinitiveSlot_IsRealizedInsideTheClause()
        {
            var sentence = builder.Build(new CzechClause
            {
                Predicate = Verb("chtít"),
                Elements = [Subject(), ClauseElement.Of(new CzechClause { Predicate = Verb("číst") }, FgdFunctor.PAT)]
            });

            Assert.AreEqual("Student chce číst.", sentence);
        }

        /// <summary>
        /// The infinitive keeps its own arguments, and the whole of it is one constituent.
        /// </summary>
        [TestMethod]
        public void Infinitive_KeepsItsOwnArguments()
        {
            var sentence = builder.Build(new CzechClause
            {
                Predicate = Verb("chtít"),
                Elements =
                [
                    Subject(),
                    ClauseElement.Of(
                        new CzechClause { Predicate = Verb("číst"), Elements = [Book()] },
                        FgdFunctor.PAT)
                ]
            });

            Assert.AreEqual("Student chce číst knihu.", sentence);
        }

        /// <summary>
        /// Only the governing verb is finite; the tense of the sentence is carried there and the
        /// infinitive states none.
        /// </summary>
        [TestMethod]
        public void OnlyTheGoverningVerbIsFinite()
        {
            var sentence = builder.Build(new CzechClause
            {
                Predicate = Verb("chtít", Tense.Past),
                Elements = [Subject(), ClauseElement.Of(new CzechClause { Predicate = Verb("číst") }, FgdFunctor.PAT)]
            });

            Assert.AreEqual("Student chtěl číst.", sentence);
        }

        /// <summary>
        /// A reflexive infinitive puts its particle in the governing clause's cluster, where the one
        /// cluster of the clause is — "chce se učit" and never "chce učit se".
        /// </summary>
        [TestMethod]
        public void ReflexiveOfTheInfinitiveClimbsIntoTheCluster()
        {
            var sentence = builder.Build(new CzechClause
            {
                Predicate = Verb("chtít"),
                Elements =
                [
                    Subject(),
                    ClauseElement.Of(
                        new CzechClause
                        {
                            Predicate = Verb("učit") with { ReflexiveType = ReflexiveType.ReflexivumTantum_Se }
                        },
                        FgdFunctor.PAT)
                ]
            });

            Assert.AreEqual("Student se chce učit.", sentence);
        }

        /// <summary>
        /// Control is what licenses the infinitive: the understood subject has to be the participant the
        /// frame names. Where the two are different people Czech has no infinitive construction, and the
        /// refusal says which one to use instead.
        /// </summary>
        [TestMethod]
        public void InfinitiveWithoutCoreference_IsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(new CzechClause
            {
                Predicate = Verb("chtít"),
                Elements =
                [
                    Subject(),
                    ClauseElement.Of(
                        new CzechClause { Predicate = Verb("číst"), Elements = [Subject("žák")] },
                        FgdFunctor.PAT)
                ]
            }));

            StringAssert.Contains(failure.Message, "aby");
        }

        /// <summary>
        /// A slot the frame records as a content clause comes out as a dependent clause behind its
        /// conjunction, with the comma the subordination rule writes.
        /// </summary>
        [TestMethod]
        public void ContentClauseSlot_IsRealizedAsADependentClause()
        {
            var sentence = builder.Build(new CzechClause
            {
                Predicate = Verb("vědět"),
                Elements = [Subject(), ClauseElement.Of(new CzechClause { Predicate = Verb("číst") }, FgdFunctor.PAT)]
            });

            Assert.AreEqual("Student ví, že čte.", sentence);
        }

        /// <summary>
        /// Czech has no sequence of tenses: the dependent clause keeps the tense it was given rather than
        /// being shifted back to match the governing verb.
        /// </summary>
        /// <remarks>
        /// Worth a test of its own because getting it wrong produces a well-formed sentence that reports
        /// something else — "věděl, že četl" says the reading was over by the time he knew.
        /// </remarks>
        [TestMethod]
        public void ContentClause_KeepsItsOwnTense()
        {
            var sentence = builder.Build(new CzechClause
            {
                Predicate = Verb("vědět", Tense.Past),
                Elements = [Subject(), ClauseElement.Of(new CzechClause { Predicate = Verb("číst") }, FgdFunctor.PAT)]
            });

            Assert.AreEqual("Student věděl, že čte.", sentence);
        }

        /// <summary>
        /// The dependent clause has a subject of its own, unlike the infinitive, and agrees with it.
        /// </summary>
        [TestMethod]
        public void ContentClause_AgreesWithItsOwnSubject()
        {
            var sentence = builder.Build(new CzechClause
            {
                Predicate = Verb("vědět"),
                Elements =
                [
                    Subject(),
                    ClauseElement.Of(
                        new CzechClause { Predicate = Verb("číst"), Elements = [Subject("žák")] },
                        FgdFunctor.PAT)
                ]
            });

            Assert.AreEqual("Student ví, že žák čte.", sentence);
        }

        /// <summary>
        /// A verb whose slot the dictionary records only as a case cannot take a proposition, and saying
        /// so is more use than building one.
        /// </summary>
        [TestMethod]
        public void PropositionInACaseOnlySlot_IsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(new CzechClause
            {
                Predicate = Verb("kupovat"),
                Elements = [Subject(), ClauseElement.Of(new CzechClause { Predicate = Verb("číst") }, FgdFunctor.PAT)]
            }));

            StringAssert.Contains(failure.Message, "jen pádem");
        }

        /// <summary>
        /// A verb the dictionary does not hold has no frame to read the realization off, so the request
        /// is refused rather than guessed at.
        /// </summary>
        [TestMethod]
        public void PropositionUnderAnUnknownVerb_IsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(new CzechClause
            {
                Predicate = Verb("dělat") with { Lemma = "hloupnout", Pattern = "trida2" },
                Elements = [Subject(), ClauseElement.Of(new CzechClause { Predicate = Verb("číst") }, FgdFunctor.PAT)]
            }));

            StringAssert.Contains(failure.Message, "nemá ve slovníku rámec");
        }
    }
}
