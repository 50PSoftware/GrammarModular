using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies that a verb's valency frame supplies the case and preposition of its arguments,
    /// and that it licenses which arguments the verb can take at all.
    /// </summary>
    [TestClass]
    public sealed class ValencyFrameTests
    {
        private static CzechSentenceBuilder builder = null!;
        private static ICzechValencyService valency = null!;
        private static IValencyProvider<CzechLexicalEntry> provider = null!;
        private static ICzechPrepositionService prepositions = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            var built = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            builder = built.GetRequiredService<CzechSentenceBuilder>();
            valency = built.GetRequiredService<ICzechValencyService>();
            provider = built.GetRequiredService<IValencyProvider<CzechLexicalEntry>>();
            prepositions = built.GetRequiredService<ICzechPrepositionService>();
        }

        // Deliberately carries no Case — that is the point: the frame supplies it.
        private static ClauseElement Argument(string lemma, string pattern, Gender gender, FgdFunctor functor, bool isAnimate = false) =>
            ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = lemma,
                    Pattern = pattern,
                    WordCategory = WordCategory.Noun,
                    Gender = gender,
                    Number = Number.Singular,
                    IsAnimate = isAnimate
                },
                functor,
                InformationStatus.New);

        private static CzechWordRequest Verb(string lemma, string pattern) => new()
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
            Gender = Gender.Masculine
        };

        private static CzechWordRequest Perfective(string lemma, string pattern)
        {
            var verb = Verb(lemma, pattern);
            verb.Aspect = VerbAspect.Perfective;

            return verb;
        }

        #region Case from the frame

        /// <summary>
        /// The patient of vidět is accusative because the frame says so, not because the caller said so.
        /// </summary>
        [TestMethod]
        public void Build_ArgumentWithoutCase_TakesItFromTheFrame()
        {
            var clause = new CzechClause
            {
                Predicate = Verb("vidět", "trida4"),
                Elements = [Argument("žena", "žena", Gender.Feminine, FgdFunctor.PAT)]
            };

            Assert.AreEqual("Viděl ženu.", builder.Build(clause));
        }

        /// <summary>
        /// Two arguments of one verb get two different cases from the same frame.
        /// </summary>
        [TestMethod]
        public void Build_PatientAndAddressee_EachTakeTheirOwnCase()
        {
            var clause = new CzechClause
            {
                Predicate = Verb("dávat", "trida5"),
                FrameLabel = "transfer",
                Elements =
                [
                    Argument("žena", "žena", Gender.Feminine, FgdFunctor.ADDR),
                    Argument("kniha", "žena", Gender.Feminine, FgdFunctor.PAT)
                ]
            };

            Assert.AreEqual("Dával ženě knihu.", builder.Build(clause));
        }

        /// <summary>
        /// A slot that carries a preposition supplies that too, and it vocalizes as usual.
        /// </summary>
        [TestMethod]
        public void Build_SlotWithPreposition_SuppliesThePrepositionAndItsCase()
        {
            var clause = new CzechClause
            {
                Predicate = Verb("jít", "jít"),
                FrameLabel = "motion",
                Elements = [Argument("škola", "žena", Gender.Feminine, FgdFunctor.DIR3)]
            };

            Assert.AreEqual("Šel do školy.", builder.Build(clause));
        }

        /// <summary>
        /// A case set explicitly is left alone — the frame fills gaps rather than overruling a choice.
        /// </summary>
        [TestMethod]
        public void Build_ExplicitCase_IsNotOverruledByTheFrame()
        {
            var argument = Argument("žena", "žena", Gender.Feminine, FgdFunctor.PAT);
            var word = argument.Word;
            word.Case = Case.Dative;

            var clause = new CzechClause
            {
                Predicate = Verb("vidět", "trida4"),
                Elements = [argument with { Word = word }]
            };

            Assert.AreEqual("Viděl ženě.", builder.Build(clause));
        }

        /// <summary>
        /// A verb with no frame leaves everything to the caller, as before.
        /// </summary>
        [TestMethod]
        public void Build_VerbWithoutFrame_LeavesTheCaseToTheCaller()
        {
            var argument = Argument("žena", "žena", Gender.Feminine, FgdFunctor.PAT);
            var word = argument.Word;
            word.Case = Case.Accusative;

            var clause = new CzechClause
            {
                Predicate = Verb("dělat", "dělá"),
                Elements = [argument with { Word = word }]
            };

            Assert.AreEqual("Dělal ženu.", builder.Build(clause));
        }

        #endregion Case from the frame

        #region Reflexive particle

        /// <summary>
        /// The particle comes from the frame when the reflexivity belongs to the sense.
        /// </summary>
        /// <remarks>
        /// Same lemma as <see cref="Build_PatientAndAddressee_EachTakeTheirOwnCase"/>, which takes none:
        /// dal si kávu against dával ženě knihu. That is the whole reason the frame needs a say — an
        /// answer on the entry would have to be the same for both.
        /// </remarks>
        [TestMethod]
        public void Build_FrameStatingAParticle_PutsItInTheCluster()
        {
            var clause = new CzechClause
            {
                Predicate = Perfective("dát", "dát"),
                FrameLabel = "konzumace",
                Elements = [Argument("káva", "žena", Gender.Feminine, FgdFunctor.PAT)]
            };

            Assert.AreEqual("Dal si kávu.", builder.Build(clause));
        }

        /// <summary>
        /// A reflexivum tantum keeps its particle under a frame that states none.
        /// </summary>
        /// <remarks>
        /// starat se has no non-reflexive form, so the particle is on the entry and the frame is silent.
        /// The entry has to be read by the builder: the enricher fills the same field, but it runs inside
        /// MorphologyEngine on a copy, by which point the cluster has already been assembled.
        /// </remarks>
        [TestMethod]
        public void Build_EntryStatingAParticle_SurvivesAFrameThatStatesNone()
        {
            var clause = new CzechClause
            {
                Predicate = Verb("starat", "trida5"),
                Elements = [Argument("žena", "žena", Gender.Feminine, FgdFunctor.PAT, isAnimate: true)]
            };

            Assert.AreEqual("Staral se o ženu.", builder.Build(clause));
        }

        /// <summary>
        /// What the caller states is not overruled by the frame.
        /// </summary>
        /// <remarks>
        /// The sentence is here for the precedence and not for the Czech: se on this frame is wrong, and
        /// that it comes out anyway is exactly the assertion. None is what "not stated" looks like on a
        /// non-nullable field, so anything else has to be treated as the caller's decision.
        /// </remarks>
        [TestMethod]
        public void Build_CallerStatingAParticle_IsNotOverruledByTheFrame()
        {
            var predicate = Perfective("dát", "dát");
            predicate.ReflexiveType = ReflexiveType.DerivedReflexive_Se;

            var clause = new CzechClause
            {
                Predicate = predicate,
                FrameLabel = "konzumace",
                Elements = [Argument("káva", "žena", Gender.Feminine, FgdFunctor.PAT)]
            };

            Assert.AreEqual("Dal se kávu.", builder.Build(clause));
        }

        #endregion Reflexive particle

        #region Licensing

        /// <summary>
        /// An inner participant the verb has no slot for is rejected — vidět takes no addressee.
        /// </summary>
        [TestMethod]
        public void Build_UnlicensedInnerParticipant_Throws()
        {
            var clause = new CzechClause
            {
                Predicate = Verb("vidět", "trida4"),
                Elements = [Argument("žena", "žena", Gender.Feminine, FgdFunctor.ADDR)]
            };

            var exception = Assert.ThrowsException<InvalidOperationException>(() => builder.Build(clause));
            StringAssert.Contains(exception.Message, "ADDR");
        }

        /// <summary>
        /// A free modification attaches to any verb and needs no slot, so the caller supplies its case.
        /// </summary>
        [TestMethod]
        public void Build_FreeModification_NeedsNoSlot()
        {
            var adjunct = Argument("večer", "hrad", Gender.Masculine, FgdFunctor.TWHEN);
            var word = adjunct.Word;
            word.Case = Case.Accusative;

            var clause = new CzechClause
            {
                Predicate = Verb("vidět", "trida4"),
                Elements =
                [
                    Argument("žena", "žena", Gender.Feminine, FgdFunctor.PAT),
                    adjunct with { Word = word, Status = InformationStatus.Given }
                ]
            };

            Assert.AreEqual("Večer viděl ženu.", builder.Build(clause));
        }

        /// <summary>
        /// A verb with several frames has to be told which one, because they take different arguments.
        /// </summary>
        [TestMethod]
        public void GetFrame_AmbiguousVerbWithoutLabel_Throws()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(() => valency.GetFrame("jít", null));
            StringAssert.Contains(exception.Message, "motion");
        }

        /// <summary>
        /// A label naming no frame of that verb is reported.
        /// </summary>
        [TestMethod]
        public void GetFrame_UnknownLabel_Throws()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(() => valency.GetFrame("jít", "transfer"));
            StringAssert.Contains(exception.Message, "transfer");
        }

        /// <summary>
        /// The process frame of jít takes only an actor, so a direction is not licensed under it.
        /// </summary>
        [TestMethod]
        public void GetFrame_LabelSelectsTheFrame()
        {
            var motion = valency.GetFrame("jít", "motion")!;
            var process = valency.GetFrame("jít", "process")!;

            Assert.IsNotNull(valency.GetSlot(motion, FgdFunctor.DIR3));
            Assert.IsNull(valency.GetSlot(process, FgdFunctor.DIR3));
        }

        #endregion Licensing

        #region Data integrity

        /// <summary>
        /// Every slot that names a preposition must use a case that preposition actually governs.
        /// </summary>
        /// <remarks>
        /// The motion frame of jít had do with the accusative, while do governs the genitive — a frame can
        /// contradict the preposition data and nothing would notice until a sentence came out wrong.
        /// </remarks>
        [TestMethod]
        public void EveryFrameSlot_WithPreposition_UsesAGovernedCase()
        {
            foreach (var lemma in new[] { "dát", "dávat", "jít", "vidět" })
            {
                foreach (var frame in provider.GetFrames(lemma))
                {
                    // Every realization is checked, not just the preferred one: a variant nobody generates
                    // today is still a variant the lexicon claims is grammatical.
                    foreach (var realization in frame.Slots
                        .SelectMany(slot => slot.Realizations)
                        .Where(realization => realization.Preposition is not null))
                    {
                        var preposition = realization.Preposition!;

                        Assert.IsNotNull(
                            realization.Case,
                            $"Rámec '{lemma}/{frame.FrameLabel}': předložka '{preposition}' bez pádu neřídí nic.");

                        Assert.IsTrue(
                            prepositions.IsAllowed(preposition, realization.Case.Value),
                            $"Rámec '{lemma}/{frame.FrameLabel}': předložka '{preposition}' neřídí pád {realization.Case}.");
                    }
                }
            }
        }

        /// <summary>
        /// The l-participle of jít inserts its vowel in the masculine singular only.
        /// </summary>
        /// <remarks>
        /// The stem is š, and šl would have no vowel at all, so the masculine singular is šel while every
        /// other form attaches straight to the stem. The data used to carry the e in the stem itself
        /// (pastStem "še"), which gave šel correctly and šela, šeli, šelo for everything else.
        /// </remarks>
        /// <param name="gender">The requested gender.</param>
        /// <param name="number">The requested number.</param>
        /// <param name="expected">The expected participle.</param>
        [DataTestMethod]
        [DataRow("Masculine", "Singular", "šel")]
        [DataRow("Feminine", "Singular", "šla")]
        [DataRow("Neuter", "Singular", "šlo")]
        [DataRow("Masculine", "Plural", "šli")]
        [DataRow("Feminine", "Plural", "šly")]
        public void Build_JitPastParticiple_InsertsTheVowelOnlyInTheMasculineSingular(string gender, string number, string expected)
        {
            var predicate = Verb("jít", "jít");
            predicate.Gender = Enum.Parse<Gender>(gender);
            predicate.Number = Enum.Parse<Number>(number);

            var clause = new CzechClause { Predicate = predicate, FrameLabel = "process" };

            Assert.AreEqual($"{char.ToUpperInvariant(expected[0])}{expected[1..]}.", builder.Build(clause));
        }

        #endregion Data integrity
    }
}
