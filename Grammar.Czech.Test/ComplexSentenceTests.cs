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
                    Relativizer = "který",
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
                    Relativizer = "který",
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
                    Relativizer = "který",
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
                    Relativizer = "který",
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
                    Relativizer = "jenž",
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
                    Relativizer = "ten",
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
                    Relativizer = "který",
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

        #region Recursion

        /// <summary>
        /// Coordination and subordination both take sentence nodes rather than clauses, so they nest into
        /// each other without a ceiling. Four levels mixing the two, checked as one string.
        /// </summary>
        [TestMethod]
        public void Build_DeeplyNestedSentence_ComposesAtEveryLevel()
        {
            var sentence = new Subordination(
                new Coordination("a",
                [
                    Clause(Verb("dělat", "dělá"), Petr()),
                    Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se))
                ]),
                "protože",
                new Subordination(
                    Clause(Verb("dělat", "dělá")),
                    "že",
                    new Coordination("ale",
                    [
                        Clause(Verb("dělat", "dělá", ReflexiveType.ReflexivumTantum_Se)),
                        Clause(Verb("dělat", "dělá"))
                    ])));

            Assert.AreEqual(
                "Student dělal a dělal se, protože dělal, že se dělal, ale dělal.",
                builder.Build(sentence));
        }

        /// <summary>
        /// Nesting has no built-in limit, so the depth it survives is worth knowing rather than assuming.
        /// A hundred levels is far past anything a real sentence reaches and still costs nothing.
        /// </summary>
        [TestMethod]
        public void Build_HundredLevelsOfSubordination_DoesNotFail()
        {
            const int depth = 100;

            SentenceNode sentence = Clause(Verb("dělat", "dělá"), Petr());

            for (var level = 0; level < depth; level++)
            {
                sentence = new Subordination(sentence, "protože", Clause(Verb("dělat", "dělá")));
            }

            var built = builder.Build(sentence);

            Assert.AreEqual(depth, built.Split("protože").Length - 1, "Každá úroveň má přispět jednou spojkou.");
            Assert.AreEqual(depth, built.Count(character => character == ','), "A jednou čárkou.");
            StringAssert.StartsWith(built, "Student dělal, protože");
            StringAssert.EndsWith(built, "dělal.");
        }

        /// <summary>
        /// The one place recursion stops: a relative clause is a single clause, not a sentence node, so it
        /// cannot itself coordinate or subordinate. "muž, který přišel a odešel" is not expressible.
        /// </summary>
        [TestMethod]
        public void RelativeAttachment_TakesAClauseRatherThanASentence_WhichCapsNestingThere()
        {
            var attachment = typeof(RelativeAttachment).GetProperty(nameof(RelativeAttachment.Clause))!;

            Assert.AreEqual(
                typeof(CzechClause),
                attachment.PropertyType,
                "Kdyby to byl SentenceNode, tenhle test i dokumentované omezení jsou k přepsání.");
        }

        #endregion Recursion

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
                    Relativizer = "který",
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

        #endregion Inventory

        #region aby a kdyby pohlcují kondicionál

        private static CzechWordRequest Conditional(Person person, Number number) => new()
        {
            Lemma = "dělat",
            Pattern = "dělá",
            WordCategory = WordCategory.Verb,
            Modus = Modus.Conditional,
            Tense = Tense.Present,
            Aspect = VerbAspect.Imperfective,
            Voice = Voice.Active,
            Person = person,
            Number = number,
            Gender = Gender.Masculine
        };

        /// <summary>
        /// aby carries the conditional auxiliary and agrees with the subject through it, so the paradigm is
        /// composed from the stem and the particle rather than stored.
        /// </summary>
        /// <param name="person">The grammatical person of the dependent clause.</param>
        /// <param name="number">The grammatical number of the dependent clause.</param>
        /// <param name="expected">The expected form of the conjunction.</param>
        [DataTestMethod]
        [DataRow("First", "Singular", "abych")]
        [DataRow("Second", "Singular", "abys")]
        [DataRow("Third", "Singular", "aby")]
        [DataRow("First", "Plural", "abychom")]
        [DataRow("Second", "Plural", "abyste")]
        [DataRow("Third", "Plural", "aby")]
        public void Build_AbyClause_InflectsWithTheConditionalAuxiliary(string person, string number, string expected)
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "aby",
                Clause(Conditional(Enum.Parse<Person>(person), Enum.Parse<Number>(number))));

            StringAssert.Contains(builder.Build(sentence), $", {expected} ");
        }

        /// <summary>
        /// kdyby takes the same auxiliary on a different stem.
        /// </summary>
        /// <param name="person">The grammatical person of the dependent clause.</param>
        /// <param name="number">The grammatical number of the dependent clause.</param>
        /// <param name="expected">The expected form of the conjunction.</param>
        [DataTestMethod]
        [DataRow("First", "Singular", "kdybych")]
        [DataRow("Second", "Singular", "kdybys")]
        [DataRow("Third", "Singular", "kdyby")]
        [DataRow("First", "Plural", "kdybychom")]
        [DataRow("Second", "Plural", "kdybyste")]
        public void Build_KdybyClause_InflectsWithTheConditionalAuxiliary(string person, string number, string expected)
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "kdyby",
                Clause(Conditional(Enum.Parse<Person>(person), Enum.Parse<Number>(number))));

            StringAssert.Contains(builder.Build(sentence), $", {expected} ");
        }

        /// <summary>
        /// The auxiliary moved into the conjunction rather than being copied, so the clause itself no longer
        /// carries one — "abych dělal", never "abych bych dělal".
        /// </summary>
        [TestMethod]
        public void Build_AbyClause_DoesNotRepeatTheConditionalParticle()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "aby",
                Clause(Conditional(Person.First, Number.Singular)));

            var built = builder.Build(sentence);

            Assert.AreEqual("Student dělal, abych dělal.", built);
            Assert.IsFalse(built.Contains(" bych "), $"Kondicionál se zdvojil: {built}");
        }

        /// <summary>
        /// The conjunction agrees through the subject when the predicate does not state a person itself,
        /// because it is read after subject agreement has run rather than worked out ahead of it.
        /// </summary>
        [TestMethod]
        public void Build_AbyClauseWithNominalSubject_AgreesInTheThirdPerson()
        {
            var predicate = Conditional(Person.First, Number.Singular);
            predicate.Person = null;
            predicate.Number = null;

            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()), "aby", Clause(predicate, Petr()));

            Assert.AreEqual("Student dělal, aby student dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// And through a pronoun subject it reaches the second person, which the noun above cannot show.
        /// </summary>
        [TestMethod]
        public void Build_AbyClauseWithPronounSubject_AgreesInItsPerson()
        {
            var predicate = Conditional(Person.First, Number.Singular);
            predicate.Person = null;
            predicate.Number = null;

            var subject = ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = "ty",
                    WordCategory = WordCategory.Pronoun,
                    Number = Number.Singular,
                    Gender = Gender.Masculine,
                    Case = Case.Nominative
                },
                FgdFunctor.ACT,
                InformationStatus.Given);

            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()), "aby", Clause(predicate, subject));

            StringAssert.Contains(builder.Build(sentence), ", abys ");
        }

        /// <summary>
        /// The reflexive stays in the cluster behind the conjunction, which fills first position like any
        /// other subordinator: "abych se učil".
        /// </summary>
        [TestMethod]
        public void Build_AbyClauseWithReflexive_KeepsTheReflexiveAfterTheConjunction()
        {
            var predicate = Conditional(Person.First, Number.Singular);
            predicate.Lemma = "učit";
            predicate.Pattern = "trida4";
            predicate.ReflexiveType = ReflexiveType.ReflexivumTantum_Se;

            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()), "aby", Clause(predicate));

            Assert.AreEqual("Student dělal, abych se učil.", builder.Build(sentence));
        }

        #endregion aby a kdyby pohlcují kondicionál

        #region však stojí na druhém místě

        /// <summary>
        /// však does not open its clause — modern Czech has it after the first constituent, unlike avšak,
        /// which is always clause-initial.
        /// </summary>
        [TestMethod]
        public void Build_VsakCoordination_PlacesTheConjunctionAfterTheFirstConstituent()
        {
            var sentence = new Coordination("však",
            [
                Clause(Verb("dělat", "dělá")),
                Clause(Verb("dělat", "dělá"), Petr())
            ]);

            Assert.AreEqual("Dělal, student však dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// The obligatory cluster keeps its slot and však follows it: NESČ counts však among the unstable
        /// clitics, which take no rank inside the cluster.
        /// </summary>
        [TestMethod]
        public void Build_VsakCoordinationWithClitic_PutsTheClusterFirst()
        {
            var sentence = new Coordination("však",
            [
                Clause(Verb("dělat", "dělá")),
                Clause(Verb("učit", "trida4", ReflexiveType.ReflexivumTantum_Se), Petr())
            ]);

            Assert.AreEqual("Dělal, student se však učil.", builder.Build(sentence));
        }

        /// <summary>
        /// avšak is the non-enclitic twin and stays in front of the clause it joins.
        /// </summary>
        [TestMethod]
        public void Build_AvsakCoordination_StaysClauseInitial()
        {
            var sentence = new Coordination("avšak",
            [
                Clause(Verb("dělat", "dělá")),
                Clause(Verb("dělat", "dělá"), Petr())
            ]);

            Assert.AreEqual("Dělal, avšak student dělal.", builder.Build(sentence));
        }

        #endregion však stojí na druhém místě

        #region Párové spojky

        /// <summary>
        /// The split construction: the conjunction opens the first conjunct and its correlate joins the
        /// second, rather than one word standing between the two.
        /// </summary>
        /// <param name="conjunction">The opening member.</param>
        /// <param name="expected">The expected sentence.</param>
        [DataTestMethod]
        [DataRow("buď", "Buď student dělal, nebo dělal.")]
        [DataRow("ani", "Ani student dělal, ani dělal.")]
        [DataRow("nejen", "Nejen student dělal, ale i dělal.")]
        [DataRow("jednak", "Jednak student dělal, jednak dělal.")]
        [DataRow("jak", "Jak student dělal, tak dělal.")]
        [DataRow("sice", "Sice student dělal, ale dělal.")]
        public void Build_PairedCoordination_SplitsTheConjunction(string conjunction, string expected)
        {
            var sentence = new Coordination(conjunction,
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ], Paired: true);

            Assert.AreEqual(expected, builder.Build(sentence));
        }

        /// <summary>
        /// The correlate always takes a comma, even where the bare conjunction takes none. The ÚJČ rule is
        /// that one is written before the second connective whatever the word would do on its own, so paired
        /// nebo and ani are punctuated against their commaless plain use.
        /// </summary>
        [TestMethod]
        public void Build_PairedCoordination_PutsACommaBeforeTheCorrelateEvenWhereThePlainWordTakesNone()
        {
            var plain = new Coordination("ani",
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ]);

            Assert.AreEqual("Student dělal ani dělal.", builder.Build(plain));

            var paired = plain with { Paired = true };

            Assert.AreEqual("Ani student dělal, ani dělal.", builder.Build(paired));
        }

        /// <summary>
        /// Every conjunct after the first is joined by the correlate, which is what the ÚJČ example
        /// "ani jídlo, ani stan, ani mapu" shows for more than two.
        /// </summary>
        [TestMethod]
        public void Build_PairedCoordinationOfThree_RepeatsTheCorrelate()
        {
            var sentence = new Coordination("ani",
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá")),
                Clause(Verb("dělat", "dělá"))
            ], Paired: true);

            Assert.AreEqual("Ani student dělal, ani dělal, ani dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// The opening member stands outside the first conjunct, exactly as an ordinary coordinating
        /// conjunction does, so it leaves that clause's first position to the cluster.
        /// </summary>
        [TestMethod]
        public void Build_PairedCoordination_LeavesFirstPositionToTheConjunct()
        {
            var sentence = new Coordination("buď",
            [
                Clause(Verb("učit", "trida4", ReflexiveType.ReflexivumTantum_Se), Petr()),
                Clause(Verb("dělat", "dělá"))
            ], Paired: true);

            Assert.AreEqual("Buď student se učil, nebo dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// Asking for the split construction from a conjunction that has no second member is reported rather
        /// than rendered as an ordinary coordination.
        /// </summary>
        [TestMethod]
        public void Build_PairedCoordinationOnAnUnpairedConjunction_Throws()
        {
            var sentence = new Coordination("a",
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ], Paired: true);

            var exception = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(sentence));

            StringAssert.Contains(exception.Message, "není párová");
        }

        /// <summary>
        /// ať is on the ÚJČ list of split connectives, and it is a different word from the ať that
        /// subordinates a content clause. The builder knows it is coordinating here, so it gets that reading.
        /// </summary>
        [TestMethod]
        public void Build_PairedAt_UsesTheCoordinatingReading()
        {
            var sentence = new Coordination("ať",
            [
                Clause(Verb("dělat", "dělá"), Petr()),
                Clause(Verb("dělat", "dělá"))
            ], Paired: true);

            Assert.AreEqual("Ať student dělal, nebo dělal.", builder.Build(sentence));
        }

        /// <summary>
        /// The same lemma still subordinates a content clause, where the other reading applies — and there
        /// it fills first position, so the cluster follows it.
        /// </summary>
        [TestMethod]
        public void Build_SubordinatingAt_KeepsTheContentReading()
        {
            var sentence = new Subordination(
                Clause(Verb("dělat", "dělá"), Petr()),
                "ať",
                Clause(Verb("učit", "trida4", ReflexiveType.ReflexivumTantum_Se)));

            Assert.AreEqual("Student dělal, ať se učil.", builder.Build(sentence));
        }

        #endregion Párové spojky
    }
}
