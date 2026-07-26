using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Forces every JSON provider to load and checks that content actually comes back.
    /// </summary>
    /// <remarks>
    /// Providers load lazily from embedded resources, so a wrong resource path or a key-casing mismatch
    /// stays invisible until something reads the data — and nothing did, for two of them. prepositions.json
    /// was written in PascalCase against a camelCase naming policy and silently deserialized to nothing;
    /// JsonValencyProvider pointed at Data.Valency, a folder that never existed. These tests exist so the
    /// next such slip fails here rather than years later.
    /// </remarks>
    [TestClass]
    public sealed class ProviderDataLoadingTests
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
        /// Releases the service graph.
        /// </summary>
        [ClassCleanup]
        public static void CleanupClass() => provider.Dispose();

        /// <summary>
        /// Noun patterns and irregulars load, and a pattern carries its endings.
        /// </summary>
        [TestMethod]
        public void NounDataProvider_LoadsPatternsAndIrregulars()
        {
            var data = provider.GetRequiredService<INounDataProvider>();

            Assert.IsTrue(data.GetPatterns().Count > 0, "Vzory podstatných jmen se nenačetly.");
            Assert.IsTrue(data.GetIrregulars().Count > 0, "Nepravidelná podstatná jména se nenačetla.");
            Assert.IsTrue(data.GetPatterns()["pán"].Endings["singular"].Count > 0, "Vzor pán nemá koncovky.");

            // propers.json is intentionally an empty object, so only the load itself is asserted.
            Assert.IsNotNull(data.GetPropers());
        }

        /// <summary>
        /// Verb patterns and irregulars load, and an irregular carries its stem.
        /// </summary>
        [TestMethod]
        public void VerbDataProvider_LoadsPatternsAndIrregulars()
        {
            var data = provider.GetRequiredService<IVerbDataProvider>();

            Assert.IsTrue(data.GetPatterns().Count > 0, "Vzory sloves se nenačetly.");
            Assert.IsTrue(data.GetIrregulars().Count > 0, "Nepravidelná slovesa se nenačetla.");
            Assert.IsFalse(string.IsNullOrEmpty(data.GetIrregulars()["být"].PresentStem ?? data.GetIrregulars()["být"].Stem));
        }

        /// <summary>
        /// Adjective patterns load with their endings.
        /// </summary>
        [TestMethod]
        public void AdjectiveDataProvider_LoadsPatterns()
        {
            var patterns = provider.GetRequiredService<IAdjectiveDataProvider>().GetPatterns();

            Assert.IsTrue(patterns.Count > 0, "Vzory přídavných jmen se nenačetly.");
            Assert.IsTrue(patterns["mladý"].Endings.Count > 0, "Vzor mladý nemá koncovky.");
        }

        /// <summary>
        /// Pronoun patterns and paradigms load, and a fixed form comes back.
        /// </summary>
        [TestMethod]
        public void PronounDataProvider_LoadsPatternsAndParadigms()
        {
            var data = provider.GetRequiredService<IPronounDataProvider>();

            Assert.IsTrue(data.GetPronouns().Count > 0, "Zájmena se nenačetla.");
            Assert.IsTrue(data.GetParadigms().Count > 0, "Paradigmata zájmen se nenačetla.");
            Assert.IsNotNull(data.GetPronouns()["já"].FixedForms, "Zájmeno já nemá tvary.");
        }

        /// <summary>
        /// Particles load, including the past auxiliary section.
        /// </summary>
        [TestMethod]
        public void ParticlesDataProvider_LoadsConditionalReflexiveAndPastAuxiliary()
        {
            var particles = provider.GetRequiredService<IParticleDataProvider>().GetParticles();

            Assert.AreEqual("bych", particles.Conditional.Singular["First"]);
            Assert.AreEqual("jsem", particles.PastAuxiliary.Singular["First"]);
            Assert.AreEqual("se", particles.Reflexive.Accusative);
            Assert.AreEqual("si", particles.Reflexive.Dative);
        }

        /// <summary>
        /// Prefixes load.
        /// </summary>
        [TestMethod]
        public void PrefixDataProvider_LoadsPrefixes()
        {
            Assert.IsTrue(provider.GetRequiredService<IPrefixDataProvider>().GetPrefixes().Count > 0, "Předpony se nenačetly.");
        }

        /// <summary>
        /// Prepositions load with their case variants — the mismatch that started this audit.
        /// </summary>
        [TestMethod]
        public void PrepositionDataProvider_LoadsPrepositionsWithVariants()
        {
            var prepositions = provider.GetRequiredService<IPrepositionDataProvider>().GetPrepositions();

            Assert.IsTrue(prepositions.Count > 0, "Předložky se nenačetly.");
            Assert.IsTrue(prepositions["v"].Variants.Count > 0, "Předložka v nemá varianty.");
            Assert.AreEqual("ve", prepositions["v"].Vocalized);
        }

        /// <summary>
        /// Every pattern a pronoun points at exists in the data it points into.
        /// </summary>
        /// <remarks>
        /// Loading a file is not enough — a reference across files can dangle just as quietly.
        /// Eight pronouns named the adjective pattern "mlady" while the key is "mladý", so every
        /// adjective-declining pronoun (který, každý, takový, …) threw on inflection.
        /// </remarks>
        [TestMethod]
        public void PronounDataProvider_EveryPatternReferenceResolves()
        {
            var pronouns = provider.GetRequiredService<IPronounDataProvider>().GetPronouns();
            var paradigms = provider.GetRequiredService<IPronounDataProvider>().GetParadigms();
            var adjectives = provider.GetRequiredService<IAdjectiveDataProvider>().GetPatterns();

            foreach (var (lemma, data) in pronouns)
            {
                if (data.DeclensionPattern is not null)
                {
                    Assert.IsTrue(
                        adjectives.ContainsKey(data.DeclensionPattern),
                        $"Zájmeno '{lemma}' odkazuje na neexistující vzor přídavných jmen '{data.DeclensionPattern}'.");
                }

                if (data.ParadigmId is not null)
                {
                    Assert.IsTrue(
                        paradigms.ContainsKey(data.ParadigmId),
                        $"Zájmeno '{lemma}' odkazuje na neexistující paradigma '{data.ParadigmId}'.");
                }
            }
        }

        /// <summary>
        /// Numeral patterns and paradigms load, and an entry carries its paradigm.
        /// </summary>
        [TestMethod]
        public void NumeralDataProvider_LoadsPatternsAndParadigms()
        {
            var data = provider.GetRequiredService<INumeralDataProvider>();

            Assert.IsTrue(data.GetNumerals().Count > 0, "Číslovky se nenačetly.");
            Assert.IsTrue(data.GetParadigms().Count > 0, "Paradigmata číslovek se nenačetla.");
            Assert.AreEqual("dva", data.GetNumerals()["dva"].ParadigmId, "Číslovka dva ztratila odkaz na paradigma.");
            Assert.AreEqual(5m, data.GetNumerals()["pět"].Value, "Číslovka pět ztratila číselnou hodnotu.");
        }

        /// <summary>
        /// Every paradigm and declension pattern a numeral points at exists, in the provider its morphology
        /// says it should.
        /// </summary>
        [TestMethod]
        public void NumeralDataProvider_EveryPatternReferenceResolves()
        {
            var numerals = provider.GetRequiredService<INumeralDataProvider>().GetNumerals();
            var paradigms = provider.GetRequiredService<INumeralDataProvider>().GetParadigms();
            var adjectives = provider.GetRequiredService<IAdjectiveDataProvider>().GetPatterns();
            var nouns = provider.GetRequiredService<INounDataProvider>().GetPatterns();

            foreach (var (lemma, data) in numerals)
            {
                if (data.ParadigmId is not null)
                {
                    Assert.IsTrue(
                        paradigms.ContainsKey(data.ParadigmId),
                        $"Číslovka '{lemma}' odkazuje na neexistující paradigma '{data.ParadigmId}'.");
                }

                if (data.DeclensionPattern is null)
                {
                    continue;
                }

                IEnumerable<string>? expected = data.Morphology switch
                {
                    NumeralMorphology.HardAdjective or NumeralMorphology.SoftAdjective => adjectives.Keys,
                    NumeralMorphology.NounMasculine or NumeralMorphology.NounNeuter
                        or NumeralMorphology.NounFeminine => nouns.Keys,
                    _ => null
                };

                Assert.IsNotNull(
                    expected,
                    $"Číslovka '{lemma}' má vzor '{data.DeclensionPattern}', ale morfologie {data.Morphology} žádný vzor nepoužívá.");

                Assert.IsTrue(
                    expected.Contains(data.DeclensionPattern),
                    $"Číslovka '{lemma}' odkazuje na neexistující vzor '{data.DeclensionPattern}'.");
            }
        }

        /// <summary>
        /// Every numeral paradigm resolves all seven cases in at least one gender slot.
        /// </summary>
        /// <remarks>
        /// The converter accepts three nesting depths and tells them apart by the first property name, so a
        /// mistyped key would quietly normalize to the wrong shape rather than throw. This is the net under
        /// that: a paradigm that lost a level ends up missing cases here.
        /// </remarks>
        [TestMethod]
        public void NumeralDataProvider_EveryParadigmCoversAllCases()
        {
            var paradigms = provider.GetRequiredService<INumeralDataProvider>().GetParadigms();
            var allCases = Enum.GetValues<Case>();

            foreach (var (id, paradigm) in paradigms)
            {
                Assert.IsTrue(paradigm.Slots.Count > 0, $"Paradigma '{id}' je prázdné.");

                foreach (var (number, genderSlots) in paradigm.Slots)
                {
                    foreach (var (genderSlot, forms) in genderSlots)
                    {
                        var missing = allCases.Where(c => !forms.ContainsKey(c)).ToList();

                        Assert.AreEqual(
                            0,
                            missing.Count,
                            $"Paradigma '{id}' ({number}/{genderSlot}) postrádá pády: {string.Join(", ", missing)}.");
                    }
                }
            }
        }

        /// <summary>
        /// Every pattern a noun irregular inherits from exists.
        /// </summary>
        [TestMethod]
        public void NounDataProvider_EveryInheritsFromResolves()
        {
            var data = provider.GetRequiredService<INounDataProvider>();
            var patterns = data.GetPatterns();

            foreach (var (lemma, irregular) in data.GetIrregulars())
            {
                if (irregular.InheritsFrom is not null)
                {
                    Assert.IsTrue(
                        patterns.ContainsKey(irregular.InheritsFrom),
                        $"Nepravidelné '{lemma}' dědí z neexistujícího vzoru '{irregular.InheritsFrom}'.");
                }
            }
        }

        /// <summary>
        /// Conjunctions load with both their type and their comma rule.
        /// </summary>
        [TestMethod]
        public void ConjunctionDataProvider_LoadsConjunctions()
        {
            var conjunctions = provider.GetRequiredService<IConjunctionDataProvider>().GetConjunctions();

            Assert.IsTrue(conjunctions.Count > 0, "Spojky se nenačetly.");
            Assert.AreEqual(ConjunctionType.Coordinating, conjunctions["a"].Type);
            Assert.IsFalse(conjunctions["a"].RequiresComma);
            Assert.AreEqual(ConjunctionType.Subordinating, conjunctions["protože"].Type);
            Assert.IsTrue(conjunctions["protože"].RequiresComma);
        }

        /// <summary>
        /// The lexicon and the valency frames load — both pointed at a folder that never existed.
        /// </summary>
        [TestMethod]
        public void ValencyProvider_LoadsLexiconAndFrames()
        {
            var valency = provider.GetRequiredService<IValencyProvider<CzechLexicalEntry>>();

            Assert.IsTrue(valency.HasEntry("student"), "Lexikon se nenačetl.");
            Assert.IsTrue(valency.GetFrames("dát").Any(), "Valenční rámce se nenačetly.");

            var slots = valency.GetFrames("dát").First().Slots;
            Assert.IsTrue(slots.Count > 0, "Rámec slovesa dát nemá sloty.");

            var patient = slots.Single(slot => slot.Functor == FgdFunctor.PAT);
            Assert.AreEqual(Case.Accusative, patient.Realization.Case, "Realizace slotu se nenačetla.");
            Assert.IsTrue(patient.IsObligatory);
        }

        /// <summary>
        /// Every field of a lexical entry deserializes, not just the ones the tests happened to read.
        /// </summary>
        /// <remarks>
        /// The mobile-vowel flag is the case in point: the property was HasMobileVowel while the data says
        /// hasMobileE, so it came back null for every entry and the declension fell through to the heuristic.
        /// </remarks>
        [TestMethod]
        public void ValencyProvider_LexicalEntry_DeserializesEveryField()
        {
            var valency = provider.GetRequiredService<IValencyProvider<CzechLexicalEntry>>();

            var pes = valency.GetEntry("pes")!;

            Assert.AreEqual("pes", pes.Lemma);
            Assert.AreEqual(WordCategory.Noun, pes.Category);
            Assert.AreEqual(Gender.Masculine, pes.Gender);
            Assert.AreEqual("pán", pes.Pattern);
            Assert.AreEqual(true, pes.IsAnimate);
            Assert.AreEqual(true, pes.HasMobileE, "Příznak pohyblivého -e se z lexikonu nenačetl.");

            var dat = valency.GetEntry("dát")!;

            Assert.AreEqual(VerbAspect.Perfective, dat.Aspect);
            Assert.AreEqual("dávat", dat.AspectCounterpart);
        }

        /// <summary>
        /// Gender and pattern resolve from the lexicon, which is the one consumer of the valency provider
        /// in the declension path and could never have worked while the resource path was wrong.
        /// </summary>
        [TestMethod]
        public void ResolveGenderAndPattern_KnownLemma_ReadsFromTheLexicon()
        {
            var declension = provider.GetRequiredService<CzechNounDeclensionService>();

            var (gender, pattern, _, isAnimate, _) = declension.ResolveGenderAndPattern("student");

            Assert.AreEqual(Gender.Masculine, gender);
            Assert.AreEqual("pán", pattern);
            Assert.IsTrue(isAnimate);
        }
    }
}
