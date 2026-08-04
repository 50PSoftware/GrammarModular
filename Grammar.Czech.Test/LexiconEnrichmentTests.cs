using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Checks that the lexicon fills gaps without overruling anything the caller said.
    /// </summary>
    /// <remarks>
    /// The library was built on the caller supplying every category, and the lexicon arrived later. Making
    /// it consulted automatically is only safe while it stays additive, so what is worth testing is not
    /// that it works — that is one assertion — but that it changes nothing it should not: a stated
    /// pattern wins, a stated flag wins even when it says false, and a word nobody has entered still
    /// inflects from what the caller passes.
    /// </remarks>
    [TestClass]
    public sealed class LexiconEnrichmentTests
    {
        private static ServiceProvider provider = null!;
        private static MorphologyEngine engine = null!;

        /// <summary>
        /// Builds the full service graph once.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            provider = new ServiceCollection().AddCzechGrammarServices().BuildServiceProvider();
            engine = provider.GetRequiredService<MorphologyEngine>();
        }

        /// <summary>
        /// Releases the service graph.
        /// </summary>
        [ClassCleanup]
        public static void CleanupClass() => provider.Dispose();

        /// <summary>
        /// A lemma the lexicon knows inflects from nothing but the lemma and the categories.
        /// </summary>
        [DataTestMethod]
        [DataRow("student", Case.Dative, Number.Singular, "studentovi")]
        [DataRow("student", Case.Locative, Number.Plural, "studentech")]
        [DataRow("žena", Case.Dative, Number.Singular, "ženě")]
        [DataRow("pes", Case.Genitive, Number.Singular, "psa")]
        [DataRow("město", Case.Genitive, Number.Plural, "měst")]
        public void GetForm_LemmaInLexicon_NeedsNoMetadata(
            string lemma,
            Case @case,
            Number number,
            string expected)
        {
            var form = engine.GetForm(new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Noun,
                Case = @case,
                Number = number,
            });

            Assert.AreEqual(expected, form.Form);
        }

        /// <summary>
        /// A lemma the lexicon knows inflects without being told what word class it is.
        /// </summary>
        /// <remarks>
        /// The word class picks the service, so it has to be filled before the routing. Until it was, an
        /// unstated one was not a gap: the default was Noun, so <c>dát</c> went to the declension service
        /// and failed with "Noun pattern 'trida5' not found".
        /// </remarks>
        [TestMethod]
        public void GetForm_CategoryNotStated_ComesFromTheLexicon()
        {
            var verb = engine.GetForm(new CzechWordRequest
            {
                Lemma = "dát",
                Person = Person.First,
                Number = Number.Singular,
                Tense = Tense.Present,
                Modus = Modus.Indicative,
            });

            var noun = engine.GetForm(new CzechWordRequest
            {
                Lemma = "student",
                Case = Case.Dative,
                Number = Number.Singular,
            });

            Assert.AreEqual("dám", verb.Form, "Sloveso se mělo časovat, ne skloňovat.");
            Assert.AreEqual("studentovi", noun.Form);
        }

        /// <summary>
        /// A lemma held under two word classes inflects correctly as either.
        /// </summary>
        /// <remarks>
        /// stát is the one lemma in the dictionary entered twice — a masculine inanimate noun and an
        /// imperfective verb — and nothing about the two rows is shared. Whichever one a lookup returns,
        /// the other request has to still come out right.
        /// </remarks>
        [DataTestMethod]
        [DataRow(WordCategory.Noun, "státu")]
        [DataRow(WordCategory.Verb, "stojím")]
        public void GetForm_LemmaInTwoWordClasses_InflectsAsTheOneAskedFor(
            WordCategory category,
            string expected)
        {
            var form = engine.GetForm(new CzechWordRequest
            {
                Lemma = "stát",
                WordCategory = category,
                Case = Case.Genitive,
                Number = Number.Singular,
                Person = Person.First,
                Tense = Tense.Present,
                Modus = Modus.Indicative,
            });

            Assert.AreEqual(expected, form.Form);
        }

        /// <summary>
        /// The lexicon is not read across word classes, so a homonym of another class fills nothing.
        /// </summary>
        /// <remarks>
        /// Filling from the row for the other word does not complete the request, it answers a different
        /// one — a caller conjugating <em>stát</em> handed the vzor <em>hrad</em>. Against a stub rather
        /// than the dictionary, so the property holds for any pair and not just the one that exists.
        /// </remarks>
        [TestMethod]
        public void Enrich_EntryOfAnotherCategory_FillsNothing()
        {
            var lexicon = new SingleEntryProvider(new CzechLexicalEntry
            {
                Lemma = "stát",
                Category = WordCategory.Noun,
                Pattern = "hrad",
                Gender = Gender.Masculine,
                IsAnimate = false,
            });

            var request = new CzechWordRequest { Lemma = "stát", WordCategory = WordCategory.Verb };

            var enriched = new CzechLexiconEnricher(lexicon).Enrich(request);

            Assert.IsNull(enriched.Pattern, "Vzor podstatného jména se neměl dostat na sloveso.");
            Assert.IsNull(enriched.Gender, "Rod podstatného jména se neměl dostat na sloveso.");
            Assert.AreEqual(WordCategory.Verb, enriched.WordCategory, "Slovní druh měl zůstat.");
        }

        /// <summary>
        /// A verb with no vzor and no třída says so instead of throwing NullReferenceException.
        /// </summary>
        /// <remarks>
        /// The lexicon holds nowhere near every verb, so this is an ordinary call and not an edge case.
        /// It used to dereference the missing vzor.
        /// </remarks>
        [TestMethod]
        public void GetForm_VerbWithNoPatternAnywhere_ExplainsWhatIsMissing()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => engine.GetForm(new CzechWordRequest
                {
                    Lemma = "kobzolovat",
                    WordCategory = WordCategory.Verb,
                    Person = Person.Third,
                    Number = Number.Singular,
                    Tense = Tense.Present,
                    Modus = Modus.Indicative,
                }));

            StringAssert.Contains(exception.Message, "kobzolovat");
            StringAssert.Contains(exception.Message, "vzor");
        }

        /// <summary>
        /// A word nobody described and the lexicon does not hold says the word class is what is missing.
        /// </summary>
        [TestMethod]
        public void GetForm_CategoryNeitherStatedNorKnown_SaysSo()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => engine.GetForm(new CzechWordRequest
                {
                    Lemma = "kobzol",
                    Case = Case.Genitive,
                    Number = Number.Singular,
                }));

            StringAssert.Contains(exception.Message, "slovní druh");
        }

        /// <summary>
        /// A pattern the caller states is used even when the lexicon holds a different one.
        /// </summary>
        /// <remarks>
        /// This is the property the whole design rests on. Without it the lexicon would silently overrule
        /// deliberate choices, and every existing caller would be at the mercy of whatever someone typed
        /// into the admin.
        /// </remarks>
        [TestMethod]
        public void GetForm_PatternStatedByCaller_OverridesTheLexicon()
        {
            var request = new CzechWordRequest
            {
                Lemma = "student",
                WordCategory = WordCategory.Noun,
                Case = Case.Genitive,
                Number = Number.Singular,
            };

            var fromLexicon = engine.GetForm(request).Form;

            // student is a pán in the lexicon. Declining it as muž is wrong Czech, and that is the point:
            // a deliberate choice has to reach the engine even when the dictionary disagrees.
            request.Pattern = "muž";
            request.Gender = Gender.Masculine;
            request.IsAnimate = true;

            var stated = engine.GetForm(request).Form;

            Assert.AreEqual("studenta", fromLexicon, "Slovník má u studenta vzor pán.");
            Assert.AreNotEqual(
                fromLexicon,
                stated,
                "Vzor od volajícího neměl být přebit slovníkem — obojí dalo stejný tvar.");
        }

        /// <summary>
        /// A flag the caller sets to false stays false.
        /// </summary>
        /// <remarks>
        /// The three-state flags are where an additive merge is easiest to get wrong. otec carries a
        /// mobile e in the lexicon, so a caller saying it does not is contradicting the entry — and has
        /// to win, because false and unset are different claims.
        /// </remarks>
        [TestMethod]
        public void GetForm_FlagStatedAsFalse_IsNotFilledFromTheLexicon()
        {
            var request = new CzechWordRequest
            {
                Lemma = "otec",
                WordCategory = WordCategory.Noun,
                Pattern = "muž",
                Gender = Gender.Masculine,
                IsAnimate = true,
                Case = Case.Genitive,
                Number = Number.Singular,
            };

            var fromLexicon = engine.GetForm(request).Form;

            request.HasMobileE = false;
            var suppressed = engine.GetForm(request).Form;

            Assert.AreEqual("otce", fromLexicon, "Slovník má u otce pohyblivé -e.");
            Assert.AreNotEqual(fromLexicon, suppressed, "HasMobileE = false se mělo projevit.");
        }

        // A lexicon holding exactly one entry, so a test can state the shape it needs instead of hunting
        // the dictionary for a lemma that happens to have it.
        private sealed class SingleEntryProvider(CzechLexicalEntry entry)
            : Core.Interfaces.IValencyProvider<CzechLexicalEntry>
        {
            public CzechLexicalEntry? GetEntry(string lemma)
                => string.Equals(lemma, entry.Lemma, StringComparison.OrdinalIgnoreCase) ? entry : null;

            public CzechLexicalEntry? GetEntry(string lemma, WordCategory category)
                => GetEntry(lemma) is { } found && found.Category == category ? found : null;

            public bool HasEntry(string lemma) => GetEntry(lemma) is not null;

            public IEnumerable<Core.Models.Valency.ValencyFrame> GetFrames(string verbLemma) => [];
        }

        /// <summary>
        /// A word the lexicon has never heard of still inflects from what the caller passes.
        /// </summary>
        /// <remarks>
        /// Most of Czech is not in the dictionary and never will be, so this is the ordinary case rather
        /// than the edge one.
        /// </remarks>
        [TestMethod]
        public void GetForm_LemmaNotInLexicon_StillInflects()
        {
            var form = engine.GetForm(new CzechWordRequest
            {
                Lemma = "kobzol",
                WordCategory = WordCategory.Noun,
                Pattern = "hrad",
                Gender = Gender.Masculine,
                IsAnimate = false,
                Case = Case.Genitive,
                Number = Number.Singular,
            });

            Assert.AreEqual("kobzolu", form.Form);
        }

        /// <summary>
        /// A word the lexicon does not have and the caller did not describe says which is missing.
        /// </summary>
        [TestMethod]
        public void GetForm_NeitherLexiconNorCaller_ExplainsWhatIsMissing()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => engine.GetForm(new CzechWordRequest
                {
                    Lemma = "kobzol",
                    WordCategory = WordCategory.Noun,
                    Case = Case.Genitive,
                    Number = Number.Singular,
                }));

            StringAssert.Contains(exception.Message, "vzor");
        }
    }
}
