using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the top of the pipeline: the sense of the verb, which participant is the subject,
    /// whether it is expressed at all, and what counts as old information.
    /// </summary>
    /// <remarks>
    /// The planner takes roles as given and the resolver is what works them out, so the two are tested
    /// apart. Everything here that guesses is in <see cref="CzechRoleResolver"/>.
    /// </remarks>
    [TestClass]
    public sealed class SentencePlannerTests
    {
        private static CzechSentencePlanner planner = null!;
        private static CzechRoleResolver roles = null!;
        private static CzechSentenceBuilder builder = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            planner = provider.GetRequiredService<CzechSentencePlanner>();
            roles = provider.GetRequiredService<CzechRoleResolver>();
            builder = provider.GetRequiredService<CzechSentenceBuilder>();
        }

        private static CzechWordRequest Verb(string lemma, string pattern) => new()
        {
            Lemma = lemma,
            Pattern = pattern,
            WordCategory = WordCategory.Verb,
            Aspect = VerbAspect.Imperfective
        };

        private static PlannedParticipant Noun(
            string lemma, string pattern, Gender gender, bool animate = false, FgdFunctor? functor = null)
            => PlannedParticipant.Of(
                new CzechWordRequest
                {
                    Lemma = lemma,
                    Pattern = pattern,
                    WordCategory = WordCategory.Noun,
                    Gender = gender,
                    IsAnimate = animate,
                    Number = Number.Singular
                },
                functor);

        private static PlannedParticipant Pronoun(string lemma, FgdFunctor functor) =>
            PlannedParticipant.Of(
                new CzechWordRequest
                {
                    Lemma = lemma,
                    WordCategory = WordCategory.Pronoun,
                    Number = Number.Singular,
                    Gender = Gender.Masculine
                },
                functor);

        private static PlannedParticipant Student(FgdFunctor? functor = null) =>
            Noun("student", "pán", Gender.Masculine, animate: true, functor);

        private static PlannedParticipant Book(FgdFunctor? functor = null) =>
            Noun("kniha", "žena", Gender.Feminine, functor: functor);

        private static string Build(SentencePlan plan) => builder.Build(planner.Plan(plan));

        /// <summary>
        /// The passive answers for more than one verb now that the frames are in the dictionary.
        /// </summary>
        /// <remarks>
        /// The mechanism was finished long before the data: <see cref="SentencePlan.Perspective"/> has
        /// selected a passive frame since it was written, and the dictionary held exactly one. Each of
        /// these is derived from the active frame by the regular Czech rule — actor to the instrumental,
        /// patient to the nominative — so what is asserted is that the derivation reaches the sentence.
        /// </remarks>
        [DataTestMethod]
        [DataRow("číst", "číst", "Kniha je čtena studentem.")]
        [DataRow("psát", "psát", "Kniha je psána studentem.")]
        [DataRow("volat", "trida5", "Kniha je volána studentem.")]
        public void PassiveReachesTheVerbsTheDictionaryNowCarries(
            string lemma, string pattern, string expected)
        {
            var plan = new SentencePlan
            {
                Predicate = Verb(lemma, pattern),
                Participants = [Student(FgdFunctor.ACT), Book(FgdFunctor.PAT)],
                Perspective = FgdFunctor.PAT
            };

            Assert.AreEqual(expected, Build(plan));
        }


        /// <summary>
        /// A verb repeated in the second conjunct is left out, and the remnants carry the clause.
        /// </summary>
        /// <remarks>
        /// The PDT manual (§12.1.1.1) treats the governing verb as elided where it is clear from the
        /// preceding clause which verb was left out — <em>(Jirka navštívil Marii.) Honza Jiřinu.</em> —
        /// and reconstructs it by copying that node. The cases of what remains come from the elided
        /// verb, which is why nothing here is recomputed: the clause is planned whole and only the verb
        /// goes unsaid.
        /// </remarks>
        [TestMethod]
        public void RepeatedVerbIsLeftOutOfTheSecondConjunct()
        {
            var plan = Reads() with
            {
                Joined =
                [
                    new ClauseLink("a", new SentencePlan
                    {
                        Predicate = Verb("číst", "číst"),
                        Participants =
                        [
                            Noun("žák", "pán", Gender.Masculine, animate: true, FgdFunctor.ACT),
                            Noun("dopis", "hrad", Gender.Masculine, functor: FgdFunctor.PAT)
                        ]
                    })
                ]
            };

            Assert.AreEqual("Student čte knihu a žák dopis.", Build(plan));
        }

        /// <summary>
        /// Turning it off keeps the verb, which is the contrastive reading.
        /// </summary>
        /// <remarks>
        /// Both sentences are Czech and they are not the same sentence, so the caller has to be able to
        /// have either. On by default for the same reason <see cref="SentencePlan.AllowSubjectDrop"/>
        /// is: the language does it by default and turning it off is what marks the reading.
        /// </remarks>
        [TestMethod]
        public void EllipsisCanBeTurnedOff()
        {
            var plan = Reads() with
            {
                Joined =
                [
                    new ClauseLink("a", new SentencePlan
                    {
                        Predicate = Verb("číst", "číst"),
                        Participants =
                        [
                            Noun("žák", "pán", Gender.Masculine, animate: true, FgdFunctor.ACT),
                            Noun("dopis", "hrad", Gender.Masculine, functor: FgdFunctor.PAT)
                        ]
                    }, AllowVerbEllipsis: false)
                ]
            };

            Assert.AreEqual("Student čte knihu a žák čte dopis.", Build(plan));
        }

        /// <summary>
        /// A different verb is not a repetition, so nothing is left out.
        /// </summary>
        [TestMethod]
        public void DifferentVerbIsNotElided()
        {
            var plan = Reads() with { Joined = [new ClauseLink("a", Writes())] };

            StringAssert.Contains(Build(plan), "píše");
        }

        /// <summary>
        /// The two clauses may differ in person and number and the verb still goes.
        /// </summary>
        /// <remarks>
        /// Person and number are carried by the subject, which stays in the second conjunct, so they are
        /// recoverable without the verb. Requiring the predicates to match outright would refuse
        /// <em>já piju kávu a ona čaj</em>, which is the commonest shape this construction has.
        /// </remarks>
        [TestMethod]
        public void PersonAndNumberMayDifferAndTheVerbStillGoes()
        {
            var plan = new SentencePlan
            {
                Predicate = Verb("číst", "číst") with { Person = Person.First, Number = Number.Singular },
                Participants = [Book(FgdFunctor.PAT)],
                AllowSubjectDrop = false,
                Joined =
                [
                    new ClauseLink("a", new SentencePlan
                    {
                        Predicate = Verb("číst", "číst"),
                        Participants =
                        [
                            Student(FgdFunctor.ACT),
                            Noun("dopis", "hrad", Gender.Masculine, functor: FgdFunctor.PAT)
                        ]
                    })
                ]
            };

            Assert.AreEqual("Knihu čtu a student dopis.", Build(plan));
        }

        /// <summary>
        /// A predicate that needs more than one word keeps all of them.
        /// </summary>
        /// <remarks>
        /// In the first and second person of the past tense the clitic auxiliary carries the tense and
        /// the person, so leaving the participle out would strand it, and where it then belongs is not
        /// something this project has established. Not eliding is always grammatical, so that is what a
        /// multi-word predicate gets.
        /// </remarks>
        [TestMethod]
        public void MultiWordPredicateIsNotElided()
        {
            var past = Verb("číst", "číst") with { Tense = Tense.Past, Person = Person.First, Number = Number.Singular };

            var plan = new SentencePlan
            {
                Predicate = past,
                Participants = [Book(FgdFunctor.PAT)],
                AllowSubjectDrop = false,
                Joined =
                [
                    new ClauseLink("a", new SentencePlan
                    {
                        Predicate = past,
                        Participants =
                        [
                            Student(FgdFunctor.ACT),
                            Noun("dopis", "hrad", Gender.Masculine, functor: FgdFunctor.PAT)
                        ]
                    })
                ]
            };

            StringAssert.Contains(Build(plan), "jsem");
        }


        private static SentencePlan Reads() => new()
        {
            Predicate = Verb("číst", "číst"),
            Participants = [Student(FgdFunctor.ACT), Book(FgdFunctor.PAT)]
        };

        private static SentencePlan Writes() => new()
        {
            Predicate = Verb("psát", "psát"),
            Participants =
            [
                Noun("žák", "pán", Gender.Masculine, animate: true, FgdFunctor.ACT),
                Noun("dopis", "hrad", Gender.Masculine, functor: FgdFunctor.PAT)
            ]
        };

        /// <summary>
        /// What the plan leaves unsaid takes the unmarked value: the present indicative active, and the
        /// first participant as what the sentence is about.
        /// </summary>
        [TestMethod]
        public void UnstatedCategoriesTakeTheUnmarkedValue()
        {
            var sentence = Build(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(FgdFunctor.ACT), Book(FgdFunctor.PAT)]
            });

            Assert.AreEqual("Student čte knihu.", sentence);
        }

        /// <summary>
        /// A subject pronoun that adds nothing is left out, and the agreement it was carrying moves onto
        /// the predicate — which is the whole of what makes the sentence still say who.
        /// </summary>
        [TestMethod]
        public void SubjectPronounIsDropped()
        {
            var plan = new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Pronoun("já", FgdFunctor.ACT), Book(FgdFunctor.PAT)]
            };

            Assert.AreEqual("Čtu knihu.", Build(plan));
            Assert.AreEqual("Já čtu knihu.", Build(plan with { AllowSubjectDrop = false }));
        }

        /// <summary>
        /// A clause with no actor at all is a sentence like any other — Czech has plenty — and the
        /// categories the missing subject would have carried are stated on the predicate instead.
        /// </summary>
        /// <remarks>
        /// Three different things come out looking alike and are not: <em>Prší</em> has no subject to
        /// express, <em>Čtu knihu</em> has one that was dropped, and <em>Píšou o tom</em> has one nobody
        /// is naming. The model distinguishes them by what is in the plan, not by the surface.
        /// </remarks>
        [DataTestMethod]
        [DataRow("pršet", "trida4", "Third", "Singular", "Prší.", DisplayName = "bezpodmětné")]
        [DataRow("psát", "psát", "Third", "Plural", "Píšou.", DisplayName = "neurčitý konatel")]
        [DataRow("psát", "psát", "First", "Singular", "Píšu.", DisplayName = "osoba jen na slovese")]
        public void ClauseWithNoActorStandsOnItsPredicate(
            string lemma, string pattern, string person, string number, string expected)
        {
            var sentence = Build(new SentencePlan
            {
                Predicate = Verb(lemma, pattern) with
                {
                    Person = Enum.Parse<Person>(person),
                    Number = Enum.Parse<Number>(number)
                }
            });

            Assert.AreEqual(expected, sentence);
        }

        /// <summary>
        /// A verb the dictionary records as impersonal has no participants to be had, so one offered is
        /// refused — and the refusal says why rather than reporting a role somebody forgot to fill in.
        /// </summary>
        [TestMethod]
        public void ImpersonalVerbRefusesAParticipant()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => Build(new SentencePlan
            {
                Predicate = Verb("pršet", "trida4"),
                Participants = [Student()]
            }));

            StringAssert.Contains(failure.Message, "bezpodměťové");
        }

        /// <summary>
        /// An impersonal verb has nothing to agree with, and Czech puts its participle in the neuter
        /// singular — pršelo, not pršel, which is what the masculine default would have given.
        /// </summary>
        [DataTestMethod]
        [DataRow("pršet", "trida4", "Pršelo.")]
        [DataRow("sněžit", "trida4", "Sněžilo.")]
        [DataRow("svítat", "trida5", "Svítalo.")]
        public void ImpersonalVerbTakesTheNeuterInThePast(string lemma, string pattern, string expected)
        {
            Assert.AreEqual(expected, Build(new SentencePlan
            {
                Predicate = Verb(lemma, pattern) with { Tense = Tense.Past }
            }));
        }

        /// <summary>
        /// A verb is impersonal in one sense and not in another, and the two are two frames. The bare
        /// verb takes the weather sense because that is what it means on its own; the other is reached
        /// by naming it.
        /// </summary>
        [DataTestMethod]
        [DataRow("mrznout", "trida2", "freeze", "Voda mrzne.")]
        [DataRow("hřmít", "trida4", "sound", "Voda hřmí.")]
        [DataRow("blýskat", "trida5", "flash", "Voda blýská.")]
        public void ImpersonalIsASenseRatherThanAVerb(
            string lemma, string pattern, string label, string expected)
        {
            var water = Noun("voda", "žena", Gender.Feminine);

            Assert.AreEqual(expected, Build(roles.Resolve(new SentencePlan
            {
                Predicate = Verb(lemma, pattern),
                Participants = [water],
                FrameLabel = label
            })));

            // Bez popisku vyhrává výchozí význam, a ten je bezpodměťový.
            var failure = Assert.ThrowsException<InvalidOperationException>(() => Build(roles.Resolve(
                new SentencePlan { Predicate = Verb(lemma, pattern), Participants = [water] })));

            StringAssert.Contains(failure.Message, label);
        }

        /// <summary>
        /// A perfective counterpart sits under the same lexeme, so it inherits the frames rather than
        /// carrying a copy — which is the whole reason the lexeme layer exists.
        /// </summary>
        [DataTestMethod]
        [DataRow("setmít", "trida4", "Setmělo se.")]
        [DataRow("nasněžit", "trida4", "Nasněžilo.")]
        [DataRow("rozednít", "trida4", "Rozednilo se.")]
        [DataRow("napršet", "trida4", "Napršelo.")]
        [DataRow("blýsknout", "trida2", "Blýsklo se.")]
        [DataRow("zahřmět", "trida4", "Zahřmělo.")]
        [DataRow("zmrznout", "trida2", "Zmrzlo.")]
        public void PerfectiveCounterpartInheritsTheImpersonalFrame(
            string lemma, string pattern, string expected)
        {
            Assert.AreEqual(expected, Build(new SentencePlan
            {
                Predicate = Verb(lemma, pattern) with { Aspect = VerbAspect.Perfective, Tense = Tense.Past }
            }));

            // A dědí i to, co ten rámec zakazuje.
            Assert.ThrowsException<InvalidOperationException>(() => Build(new SentencePlan
            {
                Predicate = Verb(lemma, pattern) with { Aspect = VerbAspect.Perfective },
                Participants = [Student(FgdFunctor.ACT)]
            }));
        }

        /// <summary>
        /// A perfective inherits every sense of its lexeme, not only the one it was added for, so the
        /// counterpart of a two-sense verb reaches both.
        /// </summary>
        [TestMethod]
        public void PerfectiveInheritsEverySenseOfItsLexeme()
        {
            var water = Noun("voda", "žena", Gender.Feminine);

            Assert.AreEqual("Voda zmrzla.", Build(roles.Resolve(new SentencePlan
            {
                Predicate = Verb("zmrznout", "trida2") with { Aspect = VerbAspect.Perfective, Tense = Tense.Past },
                Participants = [water],
                FrameLabel = "freeze"
            })));

            Assert.AreEqual("Zmrzlo.", Build(new SentencePlan
            {
                Predicate = Verb("zmrznout", "trida2") with { Aspect = VerbAspect.Perfective, Tense = Tense.Past }
            }));
        }

        /// <summary>
        /// A free modification attaches to any verb and is never licensed by a frame, so an empty frame
        /// does not stand in its way: an impersonal verb still takes a time or a place.
        /// </summary>
        [TestMethod]
        public void ImpersonalVerbStillTakesAFreeModification()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("pršet", "trida4"),
                Participants =
                [
                    Noun("ráno", "město", Gender.Neuter) with { Preposition = "od" }
                ]
            });

            Assert.AreEqual("Od rána prší.", Build(plan));
        }

        /// <summary>
        /// A patient can stand in the bare instrumental — mávat rukou, blýskat očima — and the frame is
        /// what says which verbs take it that way.
        /// </summary>
        [TestMethod]
        public void PatientCanStandInTheInstrumental()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("blýskat", "trida5"),
                Participants =
                [
                    Noun("meč", "stroj", Gender.Masculine),
                    Noun("oko", "město", Gender.Neuter) with
                    {
                        Word = new CzechWordRequest
                        {
                            Lemma = "oko",
                            Pattern = "město",
                            WordCategory = WordCategory.Noun,
                            Gender = Gender.Neuter,
                            Number = Number.Plural
                        }
                    }
                ],
                FrameLabel = "flash"
            });

            Assert.AreEqual(FgdFunctor.PAT, plan.Participants[1].Functor);
            Assert.AreEqual("Meč blýská očima.", Build(plan));
        }

        /// <summary>
        /// The reflexive of an impersonal verb comes out where a reflexive comes out, with nothing in
        /// front of the verb for the cluster to follow.
        /// </summary>
        [DataTestMethod]
        [DataRow("stmívat", "trida5", "Stmívá se.", "Stmívalo se.")]
        [DataRow("blýskat", "trida5", "Blýská se.", "Blýskalo se.")]
        public void ImpersonalVerbCarriesItsReflexive(
            string lemma, string pattern, string present, string past)
        {
            Assert.AreEqual(present, Build(new SentencePlan { Predicate = Verb(lemma, pattern) }));

            Assert.AreEqual(past, Build(new SentencePlan
            {
                Predicate = Verb(lemma, pattern) with { Tense = Tense.Past }
            }));
        }

        /// <summary>
        /// The frame is what says so, so a verb the dictionary does not hold keeps its old freedom: an
        /// unlisted weather verb is not refused, it is simply not known to be impersonal.
        /// </summary>
        [TestMethod]
        public void VerbOutsideTheDictionaryIsNotHeldToTheRule()
        {
            // Bez rámce si volající zadává pád sám — tak to platilo vždycky a nic na tom nemění ani to,
            // že jiné sloveso v témž slovníku bezpodměťové je.
            var subject = Student(FgdFunctor.ACT);
            var word = subject.Word;
            word.Case = Case.Nominative;

            Assert.AreEqual("Student mrholí.", Build(new SentencePlan
            {
                Predicate = Verb("mrholit", "trida4"),
                Participants = [subject with { Word = word }]
            }));
        }

        /// <summary>
        /// A subjectless clause takes its arguments as any other does; only the actor is missing.
        /// </summary>
        [TestMethod]
        public void SubjectlessClauseStillTakesItsArguments()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("psát", "psát") with { Person = Person.First, Number = Number.Singular },
                Participants = [Noun("dopis", "hrad", Gender.Masculine)]
            });

            Assert.AreEqual(FgdFunctor.PAT, plan.Participants[0].Functor);
            Assert.AreEqual("Dopis píšu.", Build(plan));
        }

        /// <summary>
        /// A contrasted pronoun is doing work and stays, because dropping it would take the contrast
        /// with it.
        /// </summary>
        [TestMethod]
        public void ContrastedSubjectPronounStays()
        {
            var sentence = Build(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants =
                [
                    Pronoun("já", FgdFunctor.ACT) with { Status = InformationStatus.Contrastive },
                    Book(FgdFunctor.PAT)
                ]
            });

            StringAssert.Contains(sentence, "Já");
        }

        /// <summary>
        /// A noun subject never drops: nothing else in the sentence would name it.
        /// </summary>
        [TestMethod]
        public void NounSubjectIsNotDropped()
        {
            Assert.AreEqual("Student čte knihu.", Build(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(FgdFunctor.ACT), Book(FgdFunctor.PAT)]
            }));
        }

        /// <summary>
        /// Asking for the patient to be the subject selects the passive frame, and with it the whole
        /// remapping: the agent drops to the instrumental and the patient rises to the nominative, which
        /// the predicate then agrees with.
        /// </summary>
        [TestMethod]
        public void PerspectiveOnThePatientSelectsThePassive()
        {
            var sentence = Build(new SentencePlan
            {
                Predicate = Verb("dávat", "trida5"),
                Participants =
                [
                    Student(FgdFunctor.ACT),
                    Noun("žena", "žena", Gender.Feminine, animate: true, FgdFunctor.ADDR),
                    Book(FgdFunctor.PAT)
                ],
                Perspective = FgdFunctor.PAT
            });

            // Ženský rod přísudku je důkaz, že podmětem je kniha a ne student.
            Assert.AreEqual("Kniha je dávána studentem ženě.", sentence);
        }

        /// <summary>
        /// The perspective is also what the sentence is about, so the participant it names becomes the
        /// theme — a passive that left the agent in front would have gained nothing over the active.
        /// </summary>
        [TestMethod]
        public void PerspectiveDecidesTheTheme()
        {
            var sentence = Build(new SentencePlan
            {
                Predicate = Verb("dávat", "trida5"),
                Participants = [Student(FgdFunctor.ACT), Book(FgdFunctor.PAT)],
                Perspective = FgdFunctor.PAT
            });

            StringAssert.StartsWith(sentence, "Kniha");
        }

        /// <summary>
        /// A verb whose senses the dictionary does not rank is an open question, not a coin toss.
        /// </summary>
        [TestMethod]
        public void AmbiguousVerbIsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => Build(new SentencePlan
            {
                Predicate = Verb("jít", "jít"),
                Participants = [Student(FgdFunctor.ACT)]
            }));

            StringAssert.Contains(failure.Message, "motion");
        }

        /// <summary>
        /// An inner participant the verb has no slot for is refused at the top, where the message can
        /// still name what the caller wrote.
        /// </summary>
        [TestMethod]
        public void FunctorOutsideTheFrameIsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => Build(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(FgdFunctor.ACT), Book(FgdFunctor.ADDR)]
            }));

            StringAssert.Contains(failure.Message, "ADDR");
        }

        /// <summary>
        /// The planner takes roles as given; a participant without one is refused rather than guessed
        /// at, and the message names the stage that does the guessing.
        /// </summary>
        [TestMethod]
        public void ParticipantWithoutARoleIsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => Build(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(), Book()]
            }));

            StringAssert.Contains(failure.Message, nameof(CzechRoleResolver));
        }

        /// <summary>
        /// The resolver reads the roles off the frame, so a caller that knows what it wants to say need
        /// not know what the Functional Generative Description calls the parts of it.
        /// </summary>
        [TestMethod]
        public void RoleResolverReadsTheRolesOffTheFrame()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(), Book()]
            });

            Assert.AreEqual(FgdFunctor.ACT, plan.Participants[0].Functor);
            Assert.AreEqual(FgdFunctor.PAT, plan.Participants[1].Functor);
            Assert.AreEqual("Student čte knihu.", Build(plan));
        }

        /// <summary>
        /// The addressee prefers an animate noun, which is what keeps the two objects of a transfer verb
        /// apart without the caller naming either.
        /// </summary>
        [TestMethod]
        public void AnimateNounBecomesTheAddressee()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("dávat", "trida5"),
                Participants =
                [
                    Student(),
                    Noun("žena", "žena", Gender.Feminine, animate: true),
                    Book()
                ]
            });

            Assert.AreEqual("Student dává ženě knihu.", Build(plan));
        }

        /// <summary>
        /// The addressee takes an animate noun where there is one and stands aside where there is not:
        /// a three-place verb used with two arguments is far likelier to be naming what than to whom,
        /// so "žák píše dopis" is a letter written and not a letter written to.
        /// </summary>
        [TestMethod]
        public void AddresseeStandsAsideForThePatientWhenNothingAnimateIsLeft()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("psát", "psát"),
                Participants = [Noun("žák", "pán", Gender.Masculine, animate: true), Noun("dopis", "hrad", Gender.Masculine)]
            });

            Assert.AreEqual(FgdFunctor.ACT, plan.Participants[0].Functor);
            Assert.AreEqual(FgdFunctor.PAT, plan.Participants[1].Functor);
            Assert.AreEqual("Žák píše dopis.", Build(plan));
        }

        /// <summary>
        /// A stated role is never overwritten by the guess.
        /// </summary>
        [TestMethod]
        public void StatedRoleSurvivesTheResolver()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(FgdFunctor.PAT), Book()]
            });

            Assert.AreEqual(FgdFunctor.PAT, plan.Participants[0].Functor);
            Assert.AreEqual(FgdFunctor.ACT, plan.Participants[1].Functor);
        }

        /// <summary>
        /// A participant the frame does not account for and no preposition explains comes back
        /// unresolved rather than plausible.
        /// </summary>
        [TestMethod]
        public void UnexplainedParticipantStaysUnresolved()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [Student(), Book(), Noun("den", "hrad", Gender.Masculine)]
            });

            Assert.AreEqual(1, CzechRoleResolver.Unresolved(plan).Count);
            Assert.AreEqual("den", CzechRoleResolver.Unresolved(plan)[0].Word.Lemma);
        }

        /// <summary>
        /// The conjunction is what says how two clauses are joined, so the caller names it and nothing
        /// else: a coordinating one puts them side by side, a subordinating one hangs the second off the
        /// first and takes the comma with it.
        /// </summary>
        [DataTestMethod]
        [DataRow("a", "Student čte knihu a žák píše dopis.", DisplayName = "souřadné bez čárky")]
        [DataRow("ale", "Student čte knihu, ale žák píše dopis.", DisplayName = "odporovací s čárkou")]
        [DataRow("protože", "Student čte knihu, protože žák píše dopis.", DisplayName = "podřadné")]
        public void ConjunctionDecidesHowTheClausesJoin(string conjunction, string expected)
        {
            Assert.AreEqual(expected, Build(Reads() with { Joined = [new ClauseLink(conjunction, Writes())] }));
        }

        /// <summary>
        /// Three clauses on one conjunction are one coordination rather than two nested ones, which is
        /// what keeps the punctuation of the inner relation from being applied twice.
        /// </summary>
        [TestMethod]
        public void ClausesOnOneConjunctionFormASingleCoordination()
        {
            var sentence = Build(Reads() with
            {
                Joined = [new ClauseLink("a", Writes()), new ClauseLink("a", Reads())]
            });

            Assert.AreEqual("Student čte knihu a žák píše dopis a student čte knihu.", sentence);
        }

        /// <summary>
        /// The paired construction opens with the conjunction and joins with its correlate, which the
        /// data supplies — the caller says that it is paired, not what the second half is.
        /// </summary>
        [TestMethod]
        public void PairedCoordinationOpensWithItsConjunction()
        {
            var sentence = Build(Reads() with
            {
                Joined = [new ClauseLink("buď", Writes(), Paired: true)]
            });

            Assert.AreEqual("Buď student čte knihu, nebo žák píše dopis.", sentence);
        }

        /// <summary>
        /// A clause joined to a joined clause nests: the link belongs to the plan it hangs off, so a
        /// chain at one level and a chain of nestings are different sentences and both are expressible.
        /// </summary>
        [TestMethod]
        public void ClausesNestAsDeepAsTheyAreWritten()
        {
            var flat = Build(Reads() with
            {
                Joined = [new ClauseLink("a", Writes()), new ClauseLink("protože", Reads())]
            });

            var nested = Build(Reads() with
            {
                Joined = [new ClauseLink("protože", Writes() with { Joined = [new ClauseLink("a", Reads())] })]
            });

            Assert.AreEqual("Student čte knihu a žák píše dopis, protože student čte knihu.", flat);
            Assert.AreEqual("Student čte knihu, protože žák píše dopis a student čte knihu.", nested);
        }

        /// <summary>
        /// aby has the conditional auxiliary welded into it, so the clause under it is in the conditional
        /// whether the caller said so or not — otherwise the sentence comes out as "aby zpívá".
        /// </summary>
        [TestMethod]
        public void AbyGovernsTheConditional()
        {
            Assert.AreEqual(
                "Student čte knihu, aby žák psal dopis.",
                Build(Reads() with { Joined = [new ClauseLink("aby", Writes())] }));
        }

        /// <summary>
        /// The auxiliary the conjunction carries is not rendered again below it, however deeply the
        /// clause it governs is nested — "aby žák psal" and never "aby žák by psal".
        /// </summary>
        [TestMethod]
        public void ConditionalCarriedByAbyIsNotRepeatedWhenNested()
        {
            var sentence = Build(Reads() with
            {
                Joined =
                [
                    new ClauseLink("aby", Writes() with
                    {
                        Joined = [new ClauseLink("když", Reads())]
                    })
                ]
            });

            Assert.AreEqual("Student čte knihu, aby žák psal dopis, když student čte knihu.", sentence);
        }

        /// <summary>
        /// Coordination joins equals, so a clause coordinated with a conditional one is conditional too
        /// — one aby carries the auxiliary for both halves. A subordinator inside opens a domain of its
        /// own and stops it, because a wish about the writing is not a wish about the singing.
        /// </summary>
        [TestMethod]
        public void ConditionalReachesCoordinatedClausesAndStopsAtASubordinator()
        {
            var coordinated = Build(Reads() with
            {
                Joined = [new ClauseLink("aby", Writes() with { Joined = [new ClauseLink("a", Reads())] })]
            });

            var subordinated = Build(Reads() with
            {
                Joined = [new ClauseLink("aby", Writes() with { Joined = [new ClauseLink("když", Reads())] })]
            });

            Assert.AreEqual("Student čte knihu, aby žák psal dopis a student četl knihu.", coordinated);
            Assert.AreEqual("Student čte knihu, aby žák psal dopis, když student čte knihu.", subordinated);
        }

        /// <summary>
        /// Stating a mood aby cannot govern is a contradiction rather than a preference, so it is
        /// reported instead of being rendered as one or the other.
        /// </summary>
        [TestMethod]
        public void MoodAgainstTheConjunctionIsRefused()
        {
            var failure = Assert.ThrowsException<InvalidOperationException>(() => Build(Reads() with
            {
                Joined =
                [
                    new ClauseLink("aby", Writes() with
                    {
                        Predicate = Verb("psát", "psát") with { Modus = Modus.Imperative }
                    })
                ]
            }));

            StringAssert.Contains(failure.Message, "podmiňovací");
        }

        /// <summary>
        /// A joined clause is a plan in its own right, so it is planned in its own right: the second
        /// clause here has a subject of its own and agrees with it, in its own tense.
        /// </summary>
        [TestMethod]
        public void JoinedClauseIsPlannedOnItsOwnTerms()
        {
            var sentence = Build(Reads() with
            {
                Joined =
                [
                    new ClauseLink("a", Writes() with
                    {
                        Predicate = Verb("psát", "psát") with { Tense = Tense.Past }
                    })
                ]
            });

            Assert.AreEqual("Student čte knihu a žák psal dopis.", sentence);
        }

        /// <summary>
        /// A relative clause hangs off a participant rather than off the sentence, so it says something
        /// about a thing while a joined clause says something about the event — and the two combine.
        /// </summary>
        [TestMethod]
        public void RelativeClauseHangsOffAParticipant()
        {
            // Vztažná věta je plán: nic se v ní nedodává ručně, ani způsob, ani čas, ani osoba.
            var subject = Student(FgdFunctor.ACT) with
            {
                Relative = new PlannedRelative
                {
                    Relativizer = "který",
                    Case = Case.Nominative,
                    Clause = new SentencePlan { Predicate = Verb("pracovat", "trida3") }
                }
            };

            var plan = new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [subject, Book(FgdFunctor.PAT)]
            };

            Assert.AreEqual("Student, který pracuje, čte knihu.", Build(plan));

            Assert.AreEqual(
                "Student, který pracuje, čte knihu a žák píše dopis.",
                Build(plan with { Joined = [new ClauseLink("a", Writes())] }));
        }

        /// <summary>
        /// Everything that holds of a sentence holds inside a relative clause, because it is one: the
        /// roles of its participants are read off its own verb's frame, and it can be a complex sentence
        /// in its own right.
        /// </summary>
        [TestMethod]
        public void RelativeClauseIsPlannedLikeAnyOtherSentence()
        {
            var subject = Student(FgdFunctor.ACT) with
            {
                Relative = new PlannedRelative
                {
                    Relativizer = "který",
                    Case = Case.Nominative,

                    // Role knihy nikdo neuvádí — plyne z rámce slovesa uvnitř vztažné věty.
                    Clause = new SentencePlan
                    {
                        Predicate = Verb("psát", "psát"),
                        Participants = [Noun("dopis", "hrad", Gender.Masculine)],
                        Joined = [new ClauseLink("a", new SentencePlan { Predicate = Verb("pracovat", "trida3") })]
                    }
                }
            };

            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants = [subject, Book(FgdFunctor.PAT)]
            });

            Assert.AreEqual(
                FgdFunctor.PAT,
                plan.Participants[0].Relative!.Clause.Participants[0].Functor,
                "Role uvnitř vztažné věty se odvozuje stejně jako kdekoli jinde.");

            Assert.AreEqual("Student, který píše dopis a pracuje, čte knihu.", Build(plan));
        }

        /// <summary>
        /// A clause coordinated inside a relative clause shares the relative pronoun, so it shares the
        /// theme the pronoun already spoke for and nothing inside it becomes the theme by default.
        /// </summary>
        /// <remarks>
        /// The same inheritance <see cref="CzechRoleResolver"/> gives the reserved slot, and for the same
        /// reason: one pronoun is the subject of everything coordinated with it. Without it the second
        /// conjunct took its own first participant as the theme and came out as <em>a dopis píše</em> —
        /// a marked reading nobody asked for, in a clause whose subject was spoken for two words earlier.
        /// A subordinator opens a clause with a subject of its own, so there the inheritance stops.
        /// </remarks>
        [TestMethod]
        public void CoordinationInsideARelativeClauseInheritsTheTakenTheme()
        {
            PlannedParticipant Reader(string conjunction) => Student(FgdFunctor.ACT) with
            {
                Relative = new PlannedRelative
                {
                    Relativizer = "který",
                    Case = Case.Nominative,
                    Clause = new SentencePlan
                    {
                        Predicate = Verb("číst", "číst"),
                        Participants = [Book(FgdFunctor.PAT)],
                        Joined =
                        [
                            new ClauseLink(conjunction, new SentencePlan
                            {
                                Predicate = Verb("psát", "psát"),
                                Participants = [Noun("dopis", "hrad", Gender.Masculine, functor: FgdFunctor.PAT)],
                            }),
                        ],
                    },
                },
            };

            Assert.AreEqual(
                "Student, který čte knihu a píše dopis, pracuje.",
                Build(new SentencePlan { Predicate = Verb("pracovat", "trida3"), Participants = [Reader("a")] }));

            // Za podřadicí spojkou je téma zase volné, takže si ho druhá klauze určí sama.
            Assert.AreEqual(
                "Student, který čte knihu, protože dopis píše, pracuje.",
                Build(new SentencePlan
                {
                    Predicate = Verb("pracovat", "trida3"),
                    Participants = [Reader("protože")],
                }));
        }

        /// <summary>
        /// A possessive relative pronoun modifies a participant of its clause instead of standing for
        /// one, and agrees in two directions at once.
        /// </summary>
        /// <remarks>
        /// Which of the three words it is comes from the antecedent — feminine singular žena takes jejíž —
        /// and the form it takes comes from the noun possessed. Both show here: dům is the patient of
        /// vidět and therefore accusative, so the pronoun is accusative masculine inanimate singular, and
        /// the whole constituent opens the clause because the pronoun in it does.
        /// </remarks>
        [TestMethod]
        public void PossessiveRelativeModifiesAParticipantAndAgreesBothWays()
        {
            var woman = Noun("žena", "žena", Gender.Feminine, animate: true, FgdFunctor.ACT) with
            {
                Relative = new PlannedRelative
                {
                    Relativizer = "jejíž",
                    Possessed = FgdFunctor.PAT,
                    Clause = new SentencePlan
                    {
                        Predicate = Verb("vidět", "vidět") with { Person = Person.First },
                        Participants = [Noun("dům", "hrad", Gender.Masculine, functor: FgdFunctor.PAT)],
                        AllowSubjectDrop = true,
                    },
                },
            };

            Assert.AreEqual(
                "Žena, jejíž dům vidím, pracuje.",
                Build(new SentencePlan { Predicate = Verb("pracovat", "trida3"), Participants = [woman] }));

            // Týž antecedent, jiná role vlastněného jména: dativ vytáhne z 'jejíž' jiný tvar, takže tohle
            // je to, co odlišuje skloňování od shody, která se náhodou trefila do nominativu.
            var giver = Noun("žena", "žena", Gender.Feminine, animate: true, FgdFunctor.ACT) with
            {
                Relative = new PlannedRelative
                {
                    Relativizer = "jejíž",
                    Possessed = FgdFunctor.ADDR,
                    Clause = new SentencePlan
                    {
                        Predicate = Verb("dávat", "trida5") with { Person = Person.First },
                        Participants =
                        [
                            Noun("student", "pán", Gender.Masculine, animate: true, FgdFunctor.ADDR),
                            Noun("kniha", "žena", Gender.Feminine, functor: FgdFunctor.PAT),
                        ],
                        AllowSubjectDrop = true,
                    },
                },
            };

            Assert.AreEqual(
                "Žena, jejímuž studentovi dávám knihu, pracuje.",
                Build(new SentencePlan { Predicate = Verb("pracovat", "trida3"), Participants = [giver] }));
        }

        /// <summary>
        /// The indeclinable two keep one form whatever the noun they possess stands in, and which of the
        /// three is right is decided by the antecedent rather than left to the caller.
        /// </summary>
        [TestMethod]
        public void PossessiveRelativeIsCheckedAgainstItsAntecedent()
        {
            SentencePlan Sentence(string relativizer, PlannedParticipant antecedent) => new()
            {
                Predicate = Verb("pracovat", "trida3"),
                Participants =
                [
                    antecedent with
                    {
                        Relative = new PlannedRelative
                        {
                            Relativizer = relativizer,
                            Possessed = FgdFunctor.PAT,
                            Clause = new SentencePlan
                            {
                                Predicate = Verb("vidět", "vidět") with { Person = Person.First },
                                Participants = [Noun("dům", "hrad", Gender.Masculine, functor: FgdFunctor.PAT)],
                                AllowSubjectDrop = true,
                            },
                        },
                    },
                ],
            };

            var student = Noun("student", "pán", Gender.Masculine, animate: true, FgdFunctor.ACT);

            Assert.AreEqual(
                "Student, jehož dům vidím, pracuje.",
                Build(Sentence("jehož", student)));

            // Rod řídícího jména rozhoduje, které ze tří slov to je. Všechna tři jsou platná slova,
            // takže by se věta postavila i špatně — proto se to kontroluje a nehádá.
            var wrong = Assert.ThrowsException<InvalidOperationException>(
                () => Build(Sentence("jejíž", student)));

            StringAssert.Contains(wrong.Message, "jehož");
        }

        /// <summary>
        /// A preposition names the free modification the frame cannot: the semantic group of the
        /// preposition and its case is what the functor comes from.
        /// </summary>
        [TestMethod]
        public void PrepositionNamesTheFreeModification()
        {
            var plan = roles.Resolve(new SentencePlan
            {
                Predicate = Verb("číst", "číst"),
                Participants =
                [
                    Student(),
                    Book(),
                    Noun("les", "les", Gender.Masculine) with { Preposition = "u" }
                ]
            });

            Assert.AreEqual(FgdFunctor.LOC, plan.Participants[2].Functor);
            Assert.AreEqual(Case.Genitive, plan.Participants[2].Word.Case);
        }
    }
}
