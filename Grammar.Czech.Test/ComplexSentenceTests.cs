using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies clause combination: comma placement, and where the clitic cluster lands in a dependent clause.
    /// </summary>
    [TestClass]
    public sealed class ComplexSentenceTests
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

        private static CzechWordRequest Verb(string lemma, string pattern, ReflexiveType reflexive = ReflexiveType.None) => new()
        {
            Lemma = lemma,
            Pattern = pattern,
            WordCategory = WordCategory.Verb,
            Modus = Modus.Indicative,
            Tense = Tense.Past,
            Aspect = VerbAspect.Imperfective,
            Voice = Voice.Active,
            Person = Person.Third,
            Number = Number.Singular,
            Gender = Gender.Masculine,
            ReflexiveType = reflexive
        };

        private static ClauseElement Petr() =>
            ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = "student",
                    Pattern = "pán",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Masculine,
                    Number = Number.Singular,
                    IsAnimate = true,
                    Case = Case.Nominative
                },
                FgdFunctor.ACT,
                InformationStatus.Given);

        private static CzechClause Clause(CzechWordRequest predicate, params ClauseElement[] elements) =>
            new() { Predicate = predicate, Elements = elements };

        #region Coordination

        /// <summary>
        /// No comma before a, i, ani, nebo, či.
        /// </summary>
        /// <param name="conjunction">The coordinating conjunction.</param>
        [DataTestMethod]
        [DataRow("a")]
        [DataRow("i")]
        [DataRow("nebo")]
        [DataRow("či")]
        public void Build_CoordinationWithoutComma_JoinsWithASpace(string conjunction)
        {
            var sentence = new Coordination(conjunction,
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ]);

            Assert.AreEqual($"Student dělal {conjunction} dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// Comma before ale, avšak, nýbrž, neboť.
        /// </summary>
        /// <param name="conjunction">The coordinating conjunction.</param>
        [DataTestMethod]
        [DataRow("ale")]
        [DataRow("avšak")]
        [DataRow("nýbrž")]
        [DataRow("neboť")]
        public void Build_CoordinationWithComma_PutsACommaBeforeTheConjunction(string conjunction)
        {
            var sentence = new Coordination(conjunction,
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ]);

            Assert.AreEqual($"Student dělal, {conjunction} dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// A coordinating conjunction stands outside the clause it precedes, so that clause keeps its own
        /// first position and the cluster follows the verb rather than the conjunction.
        /// </summary>
        [TestMethod]
        public void Build_CoordinatedReflexiveClause_LeavesFirstPositionToTheClause()
        {
            var sentence = new Coordination("a",
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se))
            ]);

            Assert.AreEqual("Student dělal a dělal se.", builder.Build(sentence));
        }

        /// <summary>
        /// More than two conjuncts repeat the conjunction between each pair.
        /// </summary>
        [TestMethod]
        public void Build_ThreeConjuncts_RepeatsTheConjunction()
        {
            var sentence = new Coordination("a",
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá")),
                Clause(Verb("dělat", "dělá"))
            ]);

            Assert.AreEqual("Student dělal a dělal a dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// nebo and či are punctuated by the relation between the clauses, not by the conjunction, so the
        /// exclusive reading has to be stated: freely interchangeable alternatives take no comma, mutually
        /// exclusive ones do.
        /// </summary>
        /// <param name="conjunction">The conjunction.</param>
        /// <param name="requiresComma">The relation, or null to take the conjunction's default.</param>
        /// <param name="expected">The expected sentence.</param>
        [DataTestMethod]
        [DataRow("nebo", null, "Student dělal nebo dělal.")]
        [DataRow("nebo", true, "Student dělal, nebo dělal.")]
        [DataRow("či", true, "Student dělal, či dělal.")]
        public void Build_ExclusiveCoordination_TakesACommaWhenStated(string conjunction, bool? requiresComma, string expected)
        {
            var sentence = new Coordination(conjunction,
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ],
            requiresComma);

            Assert.AreEqual(expected, builder.Build(sentence));
        }

        #endregion Coordination

        #region Subordination

        /// <summary>
        /// A dependent clause is preceded by a comma and gets neither a capital nor a full stop of its own.
        /// </summary>
        [TestMethod]
        public void Build_Subordination_PunctuatesTheBoundaryOnly()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "protože",
                Clause(Verb("dělat", "dělá")));

            Assert.AreEqual("Student dělal, protože dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// The subordinating conjunction fills the first position of its clause, so the cluster follows the
        /// conjunction rather than the verb.
        /// </summary>
        [TestMethod]
        public void Build_SubordinateReflexiveClause_PlacesClusterAfterTheConjunction()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "protože",
                Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se)));

            Assert.AreEqual("Student dělal, protože se dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// With first position taken by the conjunction, the cluster precedes the subject of the
        /// dependent clause.
        /// </summary>
        [TestMethod]
        public void Build_SubordinateClauseWithSubject_PutsTheClusterAheadOfTheSubject()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá")),
                "protože",
                Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se), Petr()));

            Assert.AreEqual("Dělal, protože se student dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// The past auxiliary travels with the cluster into the position after the conjunction.
        /// </summary>
        [TestMethod]
        public void Build_SubordinateClauseWithAuxiliary_MovesTheWholeClusterAfterTheConjunction()
        {
            var predicate = Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se);
            predicate.Person = Person.First;

            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "že",
                Clause(predicate));

            Assert.AreEqual("Student dělal, že jsem se dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// Nesting composes: a coordination can sit inside a subordination.
        /// </summary>
        [TestMethod]
        public void Build_CoordinationInsideSubordination_Composes()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "protože",
                new Coordination("a",
                [
                    Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se)),
                    Clause(Verb("dělat", "dělá"))
                ]));

            Assert.AreEqual("Student dělal, protože se dělal a dělal.", builder.Build(sentence));
        }

        #endregion Subordination

        #region Relative clauses

        private static CzechWordRequest Noun(string lemma, string pattern, Gender gender, Case @case, bool isAnimate, Number number = Number.Singular) => new()
        {
            Lemma = lemma,
            Pattern = pattern,
            WordCategory = WordCategory.Noun,
            Gender = gender,
            Number = number,
            IsAnimate = isAnimate,
            Case = @case
        };

        /// <summary>
        /// A subject relative pronoun agrees with its antecedent, and so does the predicate through it.
        /// </summary>
        /// <param name="lemma">The antecedent lemma.</param>
        /// <param name="pattern">The antecedent pattern.</param>
        /// <param name="gender">The antecedent gender.</param>
        /// <param name="isAnimate">The antecedent animacy.</param>
        /// <param name="expected">The expected sentence.</param>
        [DataTestMethod]
        [DataRow("student", "pán", "Masculine", true, "Student, který se dělal, dělal.")]
        [DataRow("žena", "žena", "Feminine", false, "Žena, která se dělala, dělala.")]
        [DataRow("město", "město", "Neuter", false, "Město, které se dělalo, dělalo.")]
        public void Build_SubjectRelative_AgreesPronounAndPredicateWithTheAntecedent(
            string lemma, string pattern, string gender, bool isAnimate, string expected)
        {
            var antecedent = Noun(lemma, pattern, Enum.Parse<Gender>(gender), Case.Nominative, isAnimate);

            var subject = new ClauseElement
            {
                Word = antecedent,
                Functor = FgdFunctor.ACT,
                Status = InformationStatus.Given,
                Relative = new RelativeAttachment
                {
                    Pronoun = "který",
                    Case = Case.Nominative,
                    Clause = Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se))
                }
            };

            var predicate = Verb("dělat", "dělá");

            Assert.AreEqual(expected, builder.Build(Clause(predicate, subject)));
        }

        /// <summary>
        /// An object relative takes its case from its role in the relative clause while still agreeing with
        /// the antecedent — masculine animate accusative is kterého, inanimate is který.
        /// </summary>
        /// <param name="lemma">The antecedent lemma.</param>
        /// <param name="pattern">The antecedent pattern.</param>
        /// <param name="isAnimate">The antecedent animacy.</param>
        /// <param name="expected">The expected sentence.</param>
        [DataTestMethod]
        [DataRow("student", "pán", true, "Dělal studenta, kterého jsem dělal.")]
        [DataRow("hrad", "hrad", false, "Dělal hrad, který jsem dělal.")]
        public void Build_ObjectRelative_TakesItsCaseFromTheRelativeClause(
            string lemma, string pattern, bool isAnimate, string expected)
        {
            var relativePredicate = Verb("dělat", "dělá");
            relativePredicate.Person = Person.First;

            var @object = new ClauseElement
            {
                Word = Noun(lemma, pattern, Gender.Masculine, Case.Accusative, isAnimate),
                Functor = FgdFunctor.PAT,
                Status = InformationStatus.New,
                Relative = new RelativeAttachment
                {
                    Pronoun = "který",
                    Case = Case.Accusative,
                    Clause = Clause(relativePredicate)
                }
            };

            Assert.AreEqual(expected, builder.Build(Clause(Verb("dělat", "dělá"), @object)));
        }

        /// <summary>
        /// The relative pronoun fills the first position of its clause, so the cluster follows the pronoun
        /// rather than the verb.
        /// </summary>
        [TestMethod]
        public void Build_RelativeClauseWithAuxiliary_PlacesClusterAfterThePronoun()
        {
            var relativePredicate = Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se);
            relativePredicate.Person = Person.First;

            var @object = new ClauseElement
            {
                Word = Noun("student", "pán", Gender.Masculine, Case.Accusative, true),
                Functor = FgdFunctor.PAT,
                Status = InformationStatus.New,
                Relative = new RelativeAttachment
                {
                    Pronoun = "který",
                    Case = Case.Genitive,
                    Clause = Clause(relativePredicate)
                }
            };

            Assert.AreEqual("Dělal studenta, kterého jsem se dělal.", builder.Build(Clause(Verb("dělat", "dělá"), @object)));
        }

        /// <summary>
        /// The antecedent and its relative clause are one constituent, so the main clause's cluster follows
        /// the whole thing rather than splitting it.
        /// </summary>
        [TestMethod]
        public void Build_RelativeOnSubject_KeepsTheMainClusterAfterTheWholeConstituent()
        {
            var subject = new ClauseElement
            {
                Word = Noun("student", "pán", Gender.Masculine, Case.Nominative, true),
                Functor = FgdFunctor.ACT,
                Status = InformationStatus.Given,
                Relative = new RelativeAttachment
                {
                    Pronoun = "který",
                    Case = Case.Nominative,
                    Clause = Clause(Verb("dělat", "dělá"))
                }
            };

            var predicate = Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se);

            Assert.AreEqual("Student, který dělal, se dělal.", builder.Build(Clause(predicate, subject)));
        }

        /// <summary>
        /// jenž declines from its own paradigm rather than as an adjective.
        /// </summary>
        [TestMethod]
        public void Build_JenzPronoun_UsesItsOwnParadigm()
        {
            var subject = new ClauseElement
            {
                Word = Noun("student", "pán", Gender.Masculine, Case.Nominative, true),
                Functor = FgdFunctor.ACT,
                Status = InformationStatus.Given,
                Relative = new RelativeAttachment
                {
                    Pronoun = "jenž",
                    Case = Case.Nominative,
                    Clause = Clause(Verb("dělat", "dělá"))
                }
            };

            Assert.AreEqual("Student, jenž dělal, dělal.", builder.Build(Clause(Verb("dělat", "dělá"), subject)));
        }

        /// <summary>
        /// A pronoun that is not a relative one is reported.
        /// </summary>
        [TestMethod]
        public void Build_NonRelativePronoun_Throws()
        {
            var subject = new ClauseElement
            {
                Word = Noun("student", "pán", Gender.Masculine, Case.Nominative, true),
                Functor = FgdFunctor.ACT,
                Status = InformationStatus.Given,
                Relative = new RelativeAttachment
                {
                    Pronoun = "ten",
                    Case = Case.Nominative,
                    Clause = Clause(Verb("dělat", "dělá"))
                }
            };

            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => builder.Build(Clause(Verb("dělat", "dělá"), subject)));

            StringAssert.Contains(exception.Message, "ten");
        }

        /// <summary>
        /// A relative clause inside a subordinate clause keeps both boundaries punctuated once each.
        /// </summary>
        [TestMethod]
        public void Build_RelativeInsideSubordination_DoesNotDoubleTheComma()
        {
            var subject = new ClauseElement
            {
                Word = Noun("student", "pán", Gender.Masculine, Case.Nominative, true),
                Functor = FgdFunctor.ACT,
                Status = InformationStatus.Given,
                Relative = new RelativeAttachment
                {
                    Pronoun = "který",
                    Case = Case.Nominative,
                    Clause = Clause(Verb("dělat", "dělá"))
                }
            };

            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "protože",
                Clause(Verb("dělat", "dělá"), subject));

            Assert.AreEqual("Student dělal, protože student, který dělal, dělal.", builder.Build(sentence));
        }

        #endregion Relative clauses

        #region End to end

        /// <summary>
        /// One sentence exercising the whole layer at once: an attribute agreeing inside a phrase, a relative
        /// clause agreeing with its antecedent, argument cases taken from the verb's valency frame, a
        /// prepositional phrase with vocalization, a dependent clause, and the clitic cluster landing after
        /// the conjunction and after the relative pronoun rather than after either verb.
        /// </summary>
        [TestMethod]
        public void Build_WholeComplexSentence_ComposesEveryLayer()
        {
            CzechWordRequest Fem(string lemma, Case? @case = null) => new()
            {
                Lemma = lemma,
                Pattern = "žena",
                WordCategory = WordCategory.Noun,
                Gender = Gender.Feminine,
                Number = Number.Singular,
                Case = @case
            };

            CzechWordRequest Ucit() => new()
            {
                Lemma = "učit",
                Pattern = "trida4",
                WordCategory = WordCategory.Verb,
                Modus = Modus.Indicative,
                Tense = Tense.Past,
                Aspect = VerbAspect.Imperfective,
                Voice = Voice.Active,
                Person = Person.Third,
                Number = Number.Singular,
                Gender = Gender.Feminine,
                ReflexiveType = ReflexiveType.ReflexivumTantum_Se
            };

            var subject = new ClauseElement
            {
                Word = Fem("studentka", Case.Nominative),
                Modifiers = [new CzechWordRequest { Lemma = "mladý", Pattern = "mladý", WordCategory = WordCategory.Adjective, Degree = Degree.Positive }],
                Functor = FgdFunctor.ACT,
                Status = InformationStatus.Given,
                Relative = new RelativeAttachment
                {
                    Pronoun = "který",
                    Case = Case.Nominative,
                    Clause = new CzechClause
                    {
                        Predicate = Ucit(),
                        Elements = [ClauseElement.Of("v", Fem("škola", Case.Locative), FgdFunctor.LOC, InformationStatus.New)]
                    }
                }
            };

            var main = new CzechClause
            {
                Predicate = new CzechWordRequest
                {
                    Lemma = "dát",
                    Pattern = "dát",
                    WordCategory = WordCategory.Verb,
                    Modus = Modus.Indicative,
                    Tense = Tense.Past,
                    Aspect = VerbAspect.Perfective,
                    Voice = Voice.Active
                },
                Elements =
                [
                    subject,
                    // No case on either: both come from the valency frame of dát.
                    ClauseElement.Of(Fem("žena"), FgdFunctor.ADDR, InformationStatus.New),
                    ClauseElement.Of(Fem("kniha"), FgdFunctor.PAT, InformationStatus.New)
                ]
            };

            var sentence = new Subordination(main, "protože", new CzechClause { Predicate = Ucit() });

            Assert.AreEqual(
                "Mladá studentka, která se učila ve škole, dala ženě knihu, protože se učila.",
                builder.Build(sentence));
        }

        #endregion End to end

        #region Inventory

        /// <summary>
        /// Conjunctions are a closed class, so an unknown one is reported instead of guessed at.
        /// </summary>
        [TestMethod]
        public void Build_UnknownConjunction_Throws()
        {
            var sentence = new Coordination("jakožto",
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ]);

            var exception = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(sentence));
            StringAssert.Contains(exception.Message, "jakožto");
        }

        /// <summary>
        /// aby and kdyby are deliberately absent: they fuse with the conditional auxiliary and inflect for
        /// person, which placing a fixed string in front of a clause cannot express.
        /// </summary>
        [TestMethod]
        public void Build_AbyConjunction_IsReportedAsUnsupported()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "aby",
                Clause(Verb("dělat", "dělá")));

            var exception = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(sentence));
            StringAssert.Contains(exception.Message, "abych");
        }

        #endregion Inventory
    }
}
