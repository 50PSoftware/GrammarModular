using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies that the imperative survives the clause path, not just the conjugation service.
    /// </summary>
    /// <remarks>
    /// VerbConjugationTests covers the forms themselves. What was untested is the route through
    /// ApplySubjectAgreement and RenderClause, which matters because a command normally has no expressed
    /// subject — the second person is elided.
    /// </remarks>
    [TestClass]
    public sealed class ImperativeClauseTests
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

        private static CzechWordRequest Imperative(
            Person person = Person.Second,
            Number number = Number.Singular,
            ReflexiveType reflexive = ReflexiveType.None) => new()
            {
                Lemma = "dělat",
                Pattern = "dělá",
                WordCategory = WordCategory.Verb,
                Modus = Modus.Imperative,
                Voice = Voice.Active,
                Person = person,
                Number = number,
                ReflexiveType = reflexive
            };

        /// <summary>
        /// A command with no expressed subject goes through the clause path.
        /// </summary>
        [TestMethod]
        public void RenderClause_ImperativeWithoutExplicitSubject_DoesNotThrow()
        {
            var clause = new CzechClause { Predicate = Imperative(), Terminator = "!" };

            Assert.AreEqual("Dělej!", builder.Build(clause));
        }

        /// <summary>
        /// The reflexive still takes second position after the verb that opens the clause.
        /// </summary>
        [TestMethod]
        public void RenderClause_ImperativeWithReflexive_PlacesCliticAfterTheVerb()
        {
            var clause = new CzechClause
            {
                Predicate = Imperative(reflexive: ReflexiveType.ReflexivumTantum_Se),
                Terminator = "!"
            };

            Assert.AreEqual("Dělej se!", builder.Build(clause));
        }

        /// <summary>
        /// A reflexivum tantum out of the lexicon lands in the cluster once, not twice.
        /// </summary>
        /// <remarks>
        /// starat se states its particle on the entry rather than on the request, and the imperative used
        /// to add one of its own off the enriched copy — on top of the one the cluster already carried,
        /// giving "Starej se se". Every other tense read the request the builder had cleared and so was
        /// never affected, which is why the whole thing stayed invisible until an entry set the field.
        /// </remarks>
        [TestMethod]
        public void RenderClause_ImperativeOfALexiconReflexive_AddsTheParticleOnce()
        {
            var predicate = Imperative();
            predicate.Lemma = "starat";
            predicate.Pattern = "trida5";

            var clause = new CzechClause { Predicate = predicate, Terminator = "!" };

            Assert.AreEqual("Starej se!", builder.Build(clause));
        }

        /// <summary>
        /// An addressee written out is a vocative, not a subject, so it must not drive agreement — the verb
        /// stays in the second person it was asked for.
        /// </summary>
        [TestMethod]
        public void RenderClause_ImperativeWithExplicitAddressee_AgreesSecondPerson()
        {
            var addressee = ClauseElement.Of(
                new CzechWordRequest
                {
                    Lemma = "student",
                    Pattern = "pán",
                    WordCategory = WordCategory.Noun,
                    Gender = Gender.Masculine,
                    IsAnimate = true,
                    Number = Number.Singular,
                    Case = Case.Vocative
                },
                FgdFunctor.ADDR,
                InformationStatus.Given);

            var clause = new CzechClause
            {
                Predicate = Imperative(),
                Elements = [addressee],
                Terminator = "!"
            };

            Assert.AreEqual("Studente dělej!", builder.Build(clause));
        }

        /// <summary>
        /// The plural imperative is reached the same way.
        /// </summary>
        [TestMethod]
        public void RenderClause_ImperativePlural_UsesThePluralForm()
        {
            var clause = new CzechClause
            {
                Predicate = Imperative(number: Number.Plural),
                Terminator = "!"
            };

            Assert.AreEqual("Dělejte!", builder.Build(clause));
        }
    }
}
