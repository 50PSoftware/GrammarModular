using Grammar.Core.Enums;
using Grammar.Czech.Cli;
using Grammar.Czech.Cli.Interaction;
using Grammar.Czech.Cli.Rendering;
using Grammar.Czech.Cli.Sentence;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the confirm step of the client application: the reading it offers, the corrections it
    /// takes, and its refusal to finish while something is undecided.
    /// </summary>
    [TestClass]
    public sealed class CliReviewTests
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
            collection.AddSingleton<DraftBuilder>();
            collection.AddSingleton<DraftView>();
            collection.AddSingleton<SentenceComposer>();

            services = collection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        }

        private static (SentenceDraft? Draft, string Dialog) Review(string answers, params string[] lemmas)
        {
            var output = new StringWriter();

            var loop = new ReviewLoop(
                services.GetRequiredService<DraftBuilder>(),
                services.GetRequiredService<DraftView>(),
                services.GetRequiredService<SentenceComposer>(),
                new StringReader(answers),
                output);

            return (loop.Run(lemmas, new DraftOverrides()), output.ToString());
        }

        /// <summary>
        /// Verifies that the reading offered for confirmation names the role, the case and where the
        /// metadata came from — which is what there is to confirm.
        /// </summary>
        [TestMethod]
        public void ReviewShowsTheReadingBeforeTheSentence()
        {
            var (draft, dialog) = Review("\n", "student", "číst", "kniha");

            Assert.IsNotNull(draft);
            Assert.IsTrue(dialog.Contains("Přísudek  číst"));
            Assert.IsTrue(dialog.Contains("ACT"));
            Assert.IsTrue(dialog.Contains("akuzativ (rámec)"));
            Assert.IsTrue(dialog.Contains("slovník"));
            Assert.IsTrue(dialog.Contains("Věta: Student čte knihu."));
        }

        /// <summary>
        /// Verifies that a correction is taken and the whole reading rebuilt from it.
        /// </summary>
        [TestMethod]
        public void CorrectionIsAppliedAndTheDraftRebuilt()
        {
            var (draft, dialog) = Review("p cas=minuly\n\n", "student", "číst", "kniha");

            Assert.IsNotNull(draft);
            Assert.AreEqual(Tense.Past, draft.Main.Predicate.Tense);
            Assert.IsTrue(dialog.Contains("Věta: Student četl knihu."));
        }

        /// <summary>
        /// Verifies that a word can be addressed by its position as well as by its lemma.
        /// </summary>
        [TestMethod]
        public void WordIsAddressableByPositionAndByLemma()
        {
            Assert.AreEqual(
                InformationStatus.Given,
                Review("3 cleneni=dane\n\n", "student", "číst", "kniha").Draft!.Main.Constituents[1].Status);

            Assert.AreEqual(
                InformationStatus.Given,
                Review("kniha cleneni=dane\n\n", "student", "číst", "kniha").Draft!.Main.Constituents[1].Status);
        }

        /// <summary>
        /// Verifies that confirming does not finish the sentence while a decision is still open.
        /// </summary>
        [TestMethod]
        public void ConfirmingDoesNotCloseAnOpenQuestion()
        {
            var (draft, dialog) = Review("\n1 role=LOC\n\n", "student", "číst", "kniha", "den");

            Assert.IsNotNull(draft);
            Assert.IsTrue(dialog.Contains("Ještě to není hotové"));
        }

        /// <summary>
        /// Verifies that a mistyped correction is answered rather than fatal.
        /// </summary>
        [TestMethod]
        public void MistypedCorrectionIsAnsweredAndTheLoopGoesOn()
        {
            var (draft, dialog) = Review("3 pad=sedmy cleneni=dane\n\n", "student", "číst", "kniha");

            Assert.IsNotNull(draft);
            Assert.IsTrue(dialog.Contains("není pád"));

            // Řádek se buď přijme celý, nebo vůbec: druhá dvojice byla v pořádku a přesto se nezapsala.
            Assert.AreEqual(InformationStatus.New, draft.Main.Constituents[1].Status);
        }

        /// <summary>
        /// Verifies that the predicate of one clause can be corrected on its own: the number after p is
        /// a clause, since a predicate has no position of its own anyone would name.
        /// </summary>
        [TestMethod]
        public void PredicateOfOneClauseIsCorrectedOnItsOwn()
        {
            string[] words = ["student", "číst", "kniha", "a", "žák", "psát", "dopis"];

            var (_, both) = Review("p cas=minuly\n\n", words);
            var (_, second) = Review("p2 cas=minuly\n\n", words);

            Assert.IsTrue(both.Contains("Věta: Student četl knihu a žák psal dopis."));
            Assert.IsTrue(second.Contains("Věta: Student čte knihu a žák psal dopis."));
        }

        /// <summary>
        /// Verifies that a correction aimed at the predicate says so, instead of being written into a
        /// record no constituent ever reads.
        /// </summary>
        [TestMethod]
        public void CorrectionAimedAtThePredicateIsRefused()
        {
            var (_, dialog) = Review("2 role=PAT\n\n", "student", "číst", "kniha");

            Assert.IsTrue(dialog.Contains("je přísudek"));
        }

        /// <summary>
        /// Verifies that abandoning the dialog produces no sentence, and that a closed input counts as
        /// abandoning it rather than looping for ever.
        /// </summary>
        [TestMethod]
        public void AbandoningTheDialogProducesNothing()
        {
            Assert.IsNull(Review("q\n", "student", "číst", "kniha").Draft);
            Assert.IsNull(Review(string.Empty, "student", "číst", "kniha").Draft);
        }
    }
}
