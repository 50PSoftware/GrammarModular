using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies that the engine covers every word class it dispatches for, and that the inflection
    /// interfaces resolve to it rather than to one of the per-class services.
    /// </summary>
    [TestClass]
    public sealed class MorphologyEngineTests
    {
        private static ServiceProvider provider = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        }

        /// <summary>
        /// Releases the fixture's service graph.
        /// </summary>
        [ClassCleanup]
        public static void CleanupClass() => provider.Dispose();

        private static CzechWordRequest Noun() => new()
        {
            Lemma = "student",
            Pattern = "pán",
            WordCategory = WordCategory.Noun,
            Gender = Gender.Masculine,
            IsAnimate = true,
            Number = Number.Singular,
            Case = Case.Genitive,
        };

        private static CzechWordRequest Verb() => new()
        {
            Lemma = "dělat",
            Pattern = "trida5",
            WordCategory = WordCategory.Verb,
            Aspect = VerbAspect.Imperfective,
            Modus = Modus.Indicative,
            Tense = Tense.Present,
            Voice = Voice.Active,
            Person = Person.First,
            Number = Number.Singular,
        };

        /// <summary>
        /// A verb reaches the conjugation service through GetForm as well, so a caller does not have to
        /// switch on the word class to pick between GetForm and GetBasicForm.
        /// </summary>
        [TestMethod]
        public void GetForm_Verb_ReturnsTheBasicForm()
        {
            var engine = provider.GetRequiredService<MorphologyEngine>();

            Assert.AreEqual("dělám", engine.GetForm(Verb()).Form);
        }

        /// <summary>
        /// Both entry points agree on a verb.
        /// </summary>
        [TestMethod]
        public void GetForm_Verb_MatchesGetBasicForm()
        {
            var engine = provider.GetRequiredService<MorphologyEngine>();

            Assert.AreEqual(engine.GetBasicForm(Verb()).Form, engine.GetForm(Verb()).Form);
        }

        /// <summary>
        /// Four types implement IInflectionService and the container keeps one registration per key, so
        /// this pins which one wins: the engine, which accepts every word class.
        /// </summary>
        [TestMethod]
        public void IInflectionService_ResolvesToTheEngine()
        {
            var resolved = provider.GetRequiredService<IInflectionService<CzechWordRequest>>();

            Assert.IsInstanceOfType<MorphologyEngine>(resolved);
        }

        /// <summary>
        /// The consequence of the registration above: a noun asked for through the interface inflects
        /// instead of throwing, which is what a per-class service would have done.
        /// </summary>
        [TestMethod]
        public void IInflectionService_InflectsEveryWordClass()
        {
            var inflection = provider.GetRequiredService<IInflectionService<CzechWordRequest>>();

            Assert.AreEqual("studenta", inflection.GetForm(Noun()).Form);
            Assert.AreEqual("dělám", inflection.GetForm(Verb()).Form);
        }

        /// <summary>
        /// The verb interface is reachable from the container too.
        /// </summary>
        [TestMethod]
        public void IVerbInflectionService_ResolvesToTheEngine()
        {
            var resolved = provider.GetRequiredService<IVerbInflectionService<CzechWordRequest>>();

            Assert.IsInstanceOfType<MorphologyEngine>(resolved);
            Assert.AreEqual("dělám", resolved.GetBasicForm(Verb()).Form);
        }
    }
}
