using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies that the ten Czech word classes are all reachable through the morphology engine.
    /// </summary>
    /// <remarks>
    /// WordCategory held six members while four word classes were served only by their own lookups, so a
    /// preposition or a particle could not travel through a word request at all and a lexicon entry had no
    /// way to say what it was. These tests hold the inventory complete.
    /// </remarks>
    [TestClass]
    public sealed class WordCategoryCoverageTests
    {
        private static IInflectionService<CzechWordRequest> engine = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            engine = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })
                             .GetRequiredService<IInflectionService<CzechWordRequest>>();
        }

        /// <summary>
        /// All ten word classes are present, in the traditional order.
        /// </summary>
        [TestMethod]
        public void WordCategory_CoversTheTenWordClasses()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    WordCategory.Noun, WordCategory.Adjective, WordCategory.Pronoun, WordCategory.Numerale,
                    WordCategory.Verb, WordCategory.Adverb, WordCategory.Preposition, WordCategory.Conjunction,
                    WordCategory.Particle, WordCategory.Interjection
                },
                Enum.GetValues<WordCategory>());
        }

        /// <summary>
        /// An uninflected word comes back as its lemma. That is not a stub standing in for missing
        /// morphology — it is the whole of the morphology these classes have.
        /// </summary>
        /// <param name="category">The word category.</param>
        /// <param name="lemma">The lemma to put through the engine.</param>
        [DataTestMethod]
        [DataRow("Preposition", "bez")]
        [DataRow("Conjunction", "protože")]
        [DataRow("Particle", "asi")]
        [DataRow("Interjection", "ach")]
        public void GetForm_UninflectedCategory_ReturnsTheLemma(string category, string lemma)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = Enum.Parse<WordCategory>(category)
            };

            Assert.AreEqual(lemma, engine.GetForm(request).Form);
        }

        /// <summary>
        /// The grammatical categories are ignored rather than rejected, because an uninflected word has none
        /// to express and a caller filling them in has not said anything wrong.
        /// </summary>
        [TestMethod]
        public void GetForm_UninflectedCategoryWithCategoriesSet_IgnoresThem()
        {
            var request = new CzechWordRequest
            {
                Lemma = "bez",
                WordCategory = WordCategory.Preposition,
                Case = Case.Genitive,
                Number = Number.Plural,
                Gender = Gender.Feminine
            };

            Assert.AreEqual("bez", engine.GetForm(request).Form);
        }

        /// <summary>
        /// Every category the enum declares is handled, so adding a member without teaching the engine about
        /// it fails here rather than at the first caller that uses it.
        /// </summary>
        [TestMethod]
        public void GetForm_EveryDeclaredCategory_IsRoutedSomewhere()
        {
            var unroutable = new List<WordCategory>();

            foreach (var category in Enum.GetValues<WordCategory>())
            {
                var request = new CzechWordRequest
                {
                    Lemma = Sample(category),
                    Pattern = Pattern(category),
                    WordCategory = category,
                    Case = Case.Nominative,
                    Number = Number.Singular,
                    Gender = Gender.Masculine,
                    IsAnimate = true,
                    Person = Person.Third,
                    Modus = Modus.Indicative,
                    Tense = Tense.Present,
                    Aspect = VerbAspect.Imperfective,
                    Voice = Voice.Active
                };

                try
                {
                    engine.GetForm(request);
                }
                catch (NotSupportedException exception) when (exception.Message.Contains("Unsupported category"))
                {
                    unroutable.Add(category);
                }
            }

            Assert.AreEqual(0, unroutable.Count, $"Bez obsluhy v MorphologyEngine: {string.Join(", ", unroutable)}.");
        }

        private static string Sample(WordCategory category) => category switch
        {
            WordCategory.Noun => "student",
            WordCategory.Adjective => "mladý",
            WordCategory.Pronoun => "on",
            WordCategory.Numerale => "pět",
            WordCategory.Verb => "dělat",
            WordCategory.Adverb => "rychle",
            WordCategory.Preposition => "bez",
            WordCategory.Conjunction => "protože",
            WordCategory.Particle => "asi",
            _ => "ach"
        };

        private static string? Pattern(WordCategory category) => category switch
        {
            WordCategory.Noun => "pán",
            WordCategory.Adjective => "mladý",
            WordCategory.Verb => "dělá",
            _ => null
        };
    }
}
