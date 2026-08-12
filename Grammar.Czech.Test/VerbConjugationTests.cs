using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Providers.SqliteProviders;
using Grammar.Czech.Services;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies verb Conjugation behavior.
    /// </summary>
    [TestClass]
    public sealed class VerbConjugationTests
    {
        private CzechVerbConjugationService service;

        /// <summary>
        /// Creates the test subject and its dependencies.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            var verbDataProvider = new JsonVerbDataProvider();
            var nounDataProvider = new JsonNounDataProvider();
            var prefixDataProvider = new JsonPrefixDataProvider();
            var particleDataProvider = new JsonCliticsDataProvider();

            var registry = new CzechPhonemeRegistry();
            var phonologyService = new CzechPhonologyService(registry);
            var prefixService = new CzechPrefixService(prefixDataProvider);
            var cliticService = new CzechCliticService(particleDataProvider);
            var epenthesisRule = new CzechEpenthesisRuleEvaluator(registry);

            var verbStructureResolver = new CzechWordStructureResolver(verbDataProvider, nounDataProvider, prefixService, phonologyService, registry, epenthesisRule);

            var valencyProvider = new SqliteValencyProvider();

            service = new CzechVerbConjugationService(
                verbDataProvider,
                verbStructureResolver,
                prefixService,
                registry,
                valencyProvider);
        }

        #region Present Tense

        /// <summary>
        /// Gets basic form present tense returns correct form.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="pattern">The inflection pattern used to choose the rule.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="expected">The expected surface form asserted by the test.</param>
        [DataTestMethod]
        [DataRow("nést", "nese", "First", "Singular", "nesu", DisplayName = "nést – 1sg")]
        [DataRow("nést", "nese", "Second", "Singular", "neseš", DisplayName = "nést – 2sg")]
        [DataRow("nést", "nese", "Third", "Singular", "nese", DisplayName = "nést – 3sg")]
        [DataRow("nést", "nese", "First", "Plural", "neseme", DisplayName = "nést – 1pl")]
        [DataRow("nést", "nese", "Second", "Plural", "nesete", DisplayName = "nést – 2pl")]
        [DataRow("nést", "nese", "Third", "Plural", "nesou", DisplayName = "nést – 3pl")]
        [DataRow("dělat", "dělá", "First", "Singular", "dělám", DisplayName = "dělat – 1sg")]
        [DataRow("dělat", "dělá", "Third", "Singular", "dělá", DisplayName = "dělat – 3sg")]
        [DataRow("dělat", "dělá", "Third", "Plural", "dělají", DisplayName = "dělat – 3pl")]
        [DataRow("prosit", "trida4", "First", "Singular", "prosím", DisplayName = "prosit – 1sg")]
        [DataRow("prosit", "trida4", "Third", "Singular", "prosí", DisplayName = "prosit – 3sg")]
        [DataRow("kupovat", "trida3", "First", "Singular", "kupuji", DisplayName = "kupovat – 1sg")]
        [DataRow("kupovat", "trida3", "Third", "Singular", "kupuje", DisplayName = "kupovat – 3sg")]
        [DataRow("být", "být", "First", "Singular", "jsem", DisplayName = "být – 1sg")]
        [DataRow("být", "být", "Second", "Singular", "jsi", DisplayName = "být – 2sg")]
        [DataRow("být", "být", "Third", "Singular", "je", DisplayName = "být – 3sg")]
        [DataRow("být", "být", "First", "Plural", "jsme", DisplayName = "být – 1pl")]
        [DataRow("být", "být", "Third", "Plural", "jsou", DisplayName = "být – 3pl")]
        public void GetBasicForm_PresentTense_ReturnsCorrectForm(
            string lemma, string pattern, string person, string number, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Tense = Tense.Present,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Person = Enum.Parse<Person>(person),
                Number = Enum.Parse<Number>(number),
            };

            var result = service.GetBasicForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        #endregion Present Tense

        #region Prefixes

        /// <summary>
        /// Verifies that the prefix of a named pattern's stems is only prepended for a lemma that really
        /// is a prefixed derivative. A lemma that merely opens with the letters of some prefix — vidět,
        /// prosit, spát, vědět — must keep the pattern's stems as they are, and not double the prefix.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="pattern">The inflection pattern used to choose the rule.</param>
        /// <param name="expected">The expected surface form asserted by the test.</param>
        [DataTestMethod]
        // Lemma opens with a prefix but is the pattern's own verb — nothing to prepend.
        [DataRow("vidět", "vidět", "vidí", DisplayName = "vidět – vzor vidět, prefix v se nepřidává")]
        [DataRow("prosit", "prosí", "prosí", DisplayName = "prosit – vzor prosí, prefix pro se nepřidává")]
        [DataRow("spát", "spát", "spí", DisplayName = "spát – vzor spát, prefix s se nepřidává")]
        [DataRow("vědět", "vědět", "ví", DisplayName = "vědět – vzor vědět, prefix v se nepřidává")]
        // Genuinely prefixed derivatives — the prefix has to survive.
        [DataRow("odnést", "nese", "odnese", DisplayName = "odnést – vzor nese, prefix od zůstává")]
        [DataRow("vyprosit", "prosí", "vyprosí", DisplayName = "vyprosit – vzor prosí, prefix vy zůstává")]
        [DataRow("napsat", "psát", "napíše", DisplayName = "napsat – vzor psát, prefix na zůstává")]
        // The class patterns derive from the stripped lemma, so they were never affected.
        [DataRow("vidět", "trida4", "vidí", DisplayName = "vidět – trida4 beze změny")]
        public void GetBasicForm_NamedPatternWithPrefixLikeLemma_DoesNotDoubleThePrefix(
            string lemma, string pattern, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Tense = Tense.Present,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Person = Person.Third,
                Number = Number.Singular,
            };

            var result = service.GetBasicForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        #endregion Prefixes

        #region Past Tense

        /// <summary>
        /// Gets basic form past tense returns correct form.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="pattern">The inflection pattern used to choose the rule.</param>
        /// <param name="gender">The grammatical gender supplied by the test data.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="expected">The expected surface form asserted by the test.</param>
        [DataTestMethod]
        [DataRow("nést", "nese", "Masculine", "Singular", "nesl", DisplayName = "nést – min. sg m")]
        [DataRow("nést", "nese", "Feminine", "Singular", "nesla", DisplayName = "nést – min. sg f")]
        [DataRow("nést", "nese", "Neuter", "Singular", "neslo", DisplayName = "nést – min. sg n")]
        [DataRow("nést", "nese", "Masculine", "Plural", "nesli", DisplayName = "nést – min. pl m")]
        [DataRow("nést", "nese", "Feminine", "Plural", "nesly", DisplayName = "nést – min. pl f")]
        [DataRow("dělat", "dělá", "Masculine", "Singular", "dělal", DisplayName = "dělat – min. sg m")]
        [DataRow("dělat", "dělá", "Feminine", "Singular", "dělala", DisplayName = "dělat – min. sg f")]
        [DataRow("dělat", "dělá", "Neuter", "Singular", "dělalo", DisplayName = "dělat – min. sg n")]
        [DataRow("dělat", "dělá", "Masculine", "Plural", "dělali", DisplayName = "dělat – min. pl m")]
        [DataRow("prosit", "trida4", "Masculine", "Singular", "prosil", DisplayName = "prosit – min. sg m")]
        [DataRow("prosit", "trida4", "Feminine", "Singular", "prosila", DisplayName = "prosit – min. sg f")]
        [DataRow("být", "být", "Masculine", "Singular", "byl", DisplayName = "být – min. sg m")]
        [DataRow("být", "být", "Feminine", "Singular", "byla", DisplayName = "být – min. sg f")]
        [DataRow("být", "být", "Neuter", "Singular", "bylo", DisplayName = "být – min. sg n")]
        [DataRow("být", "být", "Masculine", "Plural", "byli", DisplayName = "být – min. pl m")]
        // Krácení á→a je lexikální, ne pravidlo: dát/dal proti hrát/hrál. Obecná trida5 odvozuje
        // minulý kmen z infinitivu a dala by *dál, proto má dát vlastní vzor.
        [DataRow("dát", "dát", "Masculine", "Singular", "dal", DisplayName = "dát – min. sg m")]
        [DataRow("dát", "dát", "Feminine", "Singular", "dala", DisplayName = "dát – min. sg f")]
        [DataRow("dát", "dát", "Masculine", "Plural", "dali", DisplayName = "dát – min. pl m")]
        [DataRow("dávat", "trida5", "Masculine", "Singular", "dával", DisplayName = "dávat – min. sg m")]
        [DataRow("hrát", "hrát", "Masculine", "Singular", "hrál", DisplayName = "hrát – min. sg m")]
        // Činný rod týchž vzorů byl rozbitý stejně jako trpný, jen si toho nikdo nevšiml.
        [DataRow("jíst", "jíst", "Masculine", "Singular", "jedl", DisplayName = "jíst – min. sg m")]
        [DataRow("jíst", "jíst", "Feminine", "Singular", "jedla", DisplayName = "jíst – min. sg f")]
        [DataRow("jmout", "jmout", "Masculine", "Singular", "jal", DisplayName = "jmout – min. sg m")]
        [DataRow("dojmout", "dojme", "Masculine", "Singular", "dojal", DisplayName = "dojmout – min. sg m")]
        [DataRow("dojmout", "dojme", "Feminine", "Singular", "dojala", DisplayName = "dojmout – min. sg f")]
        public void GetBasicForm_PastTense_ReturnsCorrectForm(
            string lemma, string pattern, string gender, string number, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Tense = Tense.Past,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Person = Person.Third,
                Gender = Enum.Parse<Gender>(gender),
                Number = Enum.Parse<Number>(number),
            };

            var result = service.GetBasicForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        /// <summary>
        /// The passive participle keeps the long vowel the l-participle drops.
        /// </summary>
        /// <remarks>
        /// dán against dal, off one lemma. The passive stem falls back to the past one when the pattern
        /// states none, which for dát would have given *dan — the shortening belongs to the l-participle
        /// alone, so the pattern has to say both.
        /// </remarks>
        [DataTestMethod]
        [DataRow("dát", "dát", "Masculine", "Singular", "dán", DisplayName = "dát – trpný sg m")]
        [DataRow("dát", "dát", "Feminine", "Singular", "dána", DisplayName = "dát – trpný sg f")]
        // Kmen na -a se v trpném rodě dlouží, ať přijde odkudkoli — z pojmenovaného vzoru i z odvození
        // třídy.
        [DataRow("dělat", "dělá", "Masculine", "Singular", "dělán", DisplayName = "dělat – trpný sg m")]
        [DataRow("dělat", "dělá", "Feminine", "Singular", "dělána", DisplayName = "dělat – trpný sg f")]
        [DataRow("volat", "trida5", "Masculine", "Singular", "volán", DisplayName = "volat – trpný sg m")]
        [DataRow("zpívat", "trida5", "Masculine", "Singular", "zpíván", DisplayName = "zpívat – trpný sg m")]
        [DataRow("dávat", "trida5", "Masculine", "Singular", "dáván", DisplayName = "dávat – trpný sg m")]
        [DataRow("kupovat", "kupuje", "Masculine", "Singular", "kupován", DisplayName = "kupovat – trpný sg m")]
        [DataRow("pracovat", "trida3", "Masculine", "Singular", "pracován", DisplayName = "pracovat – trpný sg m")]
        [DataRow("brát", "bere", "Masculine", "Singular", "brán", DisplayName = "brát – trpný sg m")]
        [DataRow("mazat", "maže", "Masculine", "Singular", "mazán", DisplayName = "mazat – trpný sg m")]
        // Kmeny, které na -a nekončí, se dloužit nesmí — jsou tu proto, že pravidlo je vidět jen proti nim.
        [DataRow("prosit", "prosí", "Masculine", "Singular", "prošen", DisplayName = "prosit – trpný sg m")]
        [DataRow("nést", "nese", "Masculine", "Singular", "nesen", DisplayName = "nést – trpný sg m")]
        [DataRow("tisknout", "tiskne", "Masculine", "Singular", "tištěn", DisplayName = "tisknout – trpný sg m")]
        [DataRow("péct", "peče", "Masculine", "Singular", "pečen", DisplayName = "péct – trpný sg m")]
        [DataRow("číst", "číst", "Masculine", "Singular", "čten", DisplayName = "číst – trpný sg m")]
        [DataRow("klást", "klást", "Masculine", "Singular", "kladen", DisplayName = "klást – trpný sg m")]
        // Příčestí na -t se nedlouží, i když kmen na -a končí: vzat proti vzán.
        [DataRow("vzít", "vzít", "Masculine", "Singular", "vzat", DisplayName = "vzít – trpný sg m")]
        [DataRow("vzít", "vzít", "Feminine", "Singular", "vzata", DisplayName = "vzít – trpný sg f")]
        [DataRow("minout", "mine", "Masculine", "Singular", "minut", DisplayName = "minout – trpný sg m")]
        [DataRow("krýt", "kryje", "Masculine", "Singular", "kryt", DisplayName = "krýt – trpný sg m")]
        // Vzor dojme nese -a- už v koncovce (-al, -at), takže kmen ho nesmí mít taky.
        [DataRow("jmout", "jmout", "Masculine", "Singular", "jat", DisplayName = "jmout – trpný sg m")]
        [DataRow("dojmout", "dojme", "Masculine", "Singular", "dojat", DisplayName = "dojmout – trpný sg m")]
        [DataRow("jíst", "jíst", "Masculine", "Singular", "jeden", DisplayName = "jíst – trpný sg m")]
        // Nepřechodnost trpné příčestí neruší — neosobně se používá dál (bylo jito, bylo pomoženo).
        // Tvary jsou z IJP, ne odvozené: jít a jet berou -t, téci a pomoci -n s jotací.
        [DataRow("jít", "jít", "Masculine", "Singular", "jit", DisplayName = "jít – trpný sg m")]
        [DataRow("jet", "jet", "Masculine", "Singular", "jet", DisplayName = "jet – trpný sg m")]
        [DataRow("téci", "téci", "Masculine", "Singular", "tečen", DisplayName = "téci – trpný sg m")]
        [DataRow("pomoci", "pomoci", "Masculine", "Singular", "pomožen", DisplayName = "pomoci – trpný sg m")]
        [DataRow("umřít", "umře", "Masculine", "Singular", "umřen", DisplayName = "umřít – trpný sg m")]
        [DataRow("chtít", "chtít", "Masculine", "Singular", "chtěn", DisplayName = "chtít – trpný sg m")]
        // Doplnění rozhoduje, ne přechodnost: umět něco ho má, a tvar se proto tvoří — na rozdíl od
        // sousedních vzorů 4. třídy, které jsou bez předmětu a odmítají.
        [DataRow("umět", "umět", "Masculine", "Singular", "uměn", DisplayName = "umět – trpný sg m")]
        // Jotace k→č na kmeni, který ji v žádném jiném tvaru neukáže: řekl, ale řečen.
        [DataRow("říct", "říct", "Masculine", "Singular", "řečen", DisplayName = "říct – trpný sg m")]
        [DataRow("říct", "říct", "Feminine", "Singular", "řečena", DisplayName = "říct – trpný sg f")]
        // Obecná 4. třída: -ět si téma nese do příčestí, -it ho zahazuje a místo něj jotuje.
        [DataRow("vidět", "trida4", "Masculine", "Singular", "viděn", DisplayName = "vidět – trpný sg m")]
        [DataRow("trpět", "trida4", "Masculine", "Singular", "trpěn", DisplayName = "trpět – trpný sg m")]
        [DataRow("rozumět", "trida4", "Masculine", "Singular", "rozuměn", DisplayName = "rozumět – trpný sg m")]
        // Souhláska se mění: s→š, z→ž, t→c, d→z.
        [DataRow("prosit", "trida4", "Masculine", "Singular", "prošen", DisplayName = "prosit – trpný sg m (třída)")]
        [DataRow("nosit", "trida4", "Masculine", "Singular", "nošen", DisplayName = "nosit – trpný sg m")]
        [DataRow("kazit", "trida4", "Masculine", "Singular", "kažen", DisplayName = "kazit – trpný sg m")]
        [DataRow("platit", "trida4", "Masculine", "Singular", "placen", DisplayName = "platit – trpný sg m")]
        [DataRow("zaplatit", "trida4", "Masculine", "Singular", "zaplacen", DisplayName = "zaplatit – trpný sg m")]
        [DataRow("vrátit", "trida4", "Masculine", "Singular", "vrácen", DisplayName = "vrátit – trpný sg m")]
        [DataRow("hodit", "trida4", "Masculine", "Singular", "hozen", DisplayName = "hodit – trpný sg m")]
        [DataRow("narodit", "trida4", "Masculine", "Singular", "narozen", DisplayName = "narodit – trpný sg m")]
        // Shluky: st→šť, zd→žď, psané před ě jako ště/žďě.
        [DataRow("pustit", "trida4", "Masculine", "Singular", "puštěn", DisplayName = "pustit – trpný sg m")]
        [DataRow("čistit", "trida4", "Masculine", "Singular", "čištěn", DisplayName = "čistit – trpný sg m")]
        [DataRow("jezdit", "trida4", "Masculine", "Singular", "ježděn", DisplayName = "jezdit – trpný sg m")]
        // Souhláska, která se nemění: po n nese měkkost ě, jinde stojí e.
        [DataRow("změnit", "trida4", "Masculine", "Singular", "změněn", DisplayName = "změnit – trpný sg m")]
        [DataRow("chránit", "trida4", "Masculine", "Singular", "chráněn", DisplayName = "chránit – trpný sg m")]
        [DataRow("koupit", "trida4", "Masculine", "Singular", "koupen", DisplayName = "koupit – trpný sg m")]
        [DataRow("mluvit", "trida4", "Masculine", "Singular", "mluven", DisplayName = "mluvit – trpný sg m")]
        [DataRow("stavit", "trida4", "Masculine", "Singular", "staven", DisplayName = "stavit – trpný sg m")]
        [DataRow("kreslit", "trida4", "Masculine", "Singular", "kreslen", DisplayName = "kreslit – trpný sg m")]
        [DataRow("vařit", "trida4", "Masculine", "Singular", "vařen", DisplayName = "vařit – trpný sg m")]
        [DataRow("zlomit", "trida4", "Feminine", "Singular", "zlomena", DisplayName = "zlomit – trpný sg f")]
        // Výjimku z jotace nese vlastní vzor v irregulars.json, stejně jako u dát.
        [DataRow("cítit", "cítit", "Masculine", "Singular", "cítěn", DisplayName = "cítit – trpný sg m")]
        public void GetBasicForm_PassiveParticiple_KeepsItsOwnStem(
            string lemma, string pattern, string gender, string number, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Tense = Tense.Past,
                Modus = Modus.Indicative,
                Voice = Voice.Passive,
                Person = Person.Third,
                Gender = Enum.Parse<Gender>(gender),
                Number = Enum.Parse<Number>(number),
            };

            var result = service.GetBasicForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        /// <summary>
        /// A verb that forms no passive participle says so instead of inventing one.
        /// </summary>
        /// <remarks>
        /// Being intransitive is not the reason and would be the wrong test: IJP gives jít the participle
        /// jit, and pomoci pomožen. The condition NESČ states for the -n-/-t- participle is a valency one
        /// — an agent and at least one true complement — so what bars a verb is having no complement at
        /// all, which is why NESČ stars *Je běženo. moci and mít are here on separate evidence: IJP leaves
        /// their row empty where the near-identical pomoci has a form.
        /// <para>
        /// None of it is derivable from what a pattern holds, so it is stated. Once the frame answers the
        /// valency question, this list should shrink to the two that are genuinely lexical.
        /// </para>
        /// </remarks>
        [DataTestMethod]
        [DataRow("moci", "moci", DisplayName = "moci netvoří trpné příčestí")]
        [DataRow("mít", "mít", DisplayName = "mít netvoří trpné příčestí")]
        [DataRow("spát", "spát", DisplayName = "spát netvoří trpné příčestí")]
        [DataRow("stát", "stát", DisplayName = "stát netvoří trpné příčestí")]
        [DataRow("běžet", "běžet", DisplayName = "běžet netvoří trpné příčestí")]
        [DataRow("ležet", "ležet", DisplayName = "ležet netvoří trpné příčestí")]
        [DataRow("sedět", "sedět", DisplayName = "sedět netvoří trpné příčestí")]
        public void GetBasicForm_VerbWithNoPassiveParticiple_SaysSoRatherThanInventingOne(
            string lemma, string pattern)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Tense = Tense.Past,
                Modus = Modus.Indicative,
                Voice = Voice.Passive,
                Person = Person.Third,
                Gender = Gender.Masculine,
                Number = Number.Singular,
            };

            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => service.GetBasicForm(request));

            StringAssert.Contains(exception.Message, lemma);
        }

        /// <summary>
        /// The generic first class refuses to derive stems instead of returning ones nobody should use.
        /// </summary>
        /// <remarks>
        /// nést, brát, péct and mazat share the class and not a single stem, so the infinitive cannot
        /// yield them. The class said as much in a comment while still answering — nél in the past, nén in
        /// the passive — which is worse than not answering, because a wrong form looks like a form.
        /// </remarks>
        [TestMethod]
        public void GetBasicForm_GenericFirstClass_RefusesAndNamesTheAlternative()
        {
            var request = new CzechWordRequest
            {
                Lemma = "nést",
                Pattern = "trida1",
                WordCategory = WordCategory.Verb,
                Tense = Tense.Past,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Person = Person.Third,
                Gender = Gender.Masculine,
                Number = Number.Singular,
            };

            var exception = Assert.ThrowsException<NotSupportedException>(
                () => service.GetBasicForm(request));

            StringAssert.Contains(exception.Message, "nést");
            StringAssert.Contains(exception.Message, "nese");
        }

        /// <summary>
        /// A pattern that states no endings for the participle says so, rather than throwing from inside
        /// the lookup.
        /// </summary>
        /// <remarks>
        /// vědět forms no passive participle in Czech and carries no endings for one, which is the honest
        /// state of the data. What was wrong was the answer: a NullReferenceException naming neither the
        /// pattern nor the missing block. jíst answered the same way for the past tense until it was given
        /// a base to inherit from.
        /// </remarks>
        [TestMethod]
        public void GetBasicForm_PatternWithoutParticipleEndings_SaysWhichPatternAndWhich()
        {
            var request = new CzechWordRequest
            {
                Lemma = "vědět",
                Pattern = "vědět",
                WordCategory = WordCategory.Verb,
                Tense = Tense.Past,
                Modus = Modus.Indicative,
                Voice = Voice.Passive,
                Person = Person.Third,
                Gender = Gender.Masculine,
                Number = Number.Singular,
            };

            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => service.GetBasicForm(request));

            StringAssert.Contains(exception.Message, "trpné");
            StringAssert.Contains(exception.Message, "inheritsFrom");
        }

        #endregion Past Tense

        #region Future Tense

        /// <summary>
        /// Gets basic form future tense returns correct form.
        /// </summary>
        /// <param name="lemma">The dictionary form to resolve or analyze.</param>
        /// <param name="pattern">The inflection pattern used to choose the rule.</param>
        /// <param name="person">The requested grammatical person.</param>
        /// <param name="number">The grammatical number supplied by the test data.</param>
        /// <param name="aspect">The verb aspect expected by the test case.</param>
        /// <param name="expected">The expected surface form asserted by the test.</param>
        [DataTestMethod]
        [DataRow("být", "být", "First", "Singular", "Imperfective", "budu", DisplayName = "být – bud. 1sg")]
        [DataRow("být", "být", "Second", "Singular", "Imperfective", "budeš", DisplayName = "být – bud. 2sg")]
        [DataRow("být", "být", "Third", "Singular", "Imperfective", "bude", DisplayName = "být – bud. 3sg")]
        [DataRow("být", "být", "First", "Plural", "Imperfective", "budeme", DisplayName = "být – bud. 1pl")]
        [DataRow("být", "být", "Third", "Plural", "Imperfective", "budou", DisplayName = "být – bud. 3pl")]
        [DataRow("donést", "nese", "First", "Singular", "Perfective", "donesu", DisplayName = "donést – bud. 1sg (pf přít.)")]
        [DataRow("donést", "nese", "Third", "Singular", "Perfective", "donese", DisplayName = "donést – bud. 3sg (pf přít.)")]
        [DataRow("dělat", "dělá", "Third", "Singular", "Imperfective", "dělat", DisplayName = "dělat – bud. opisné → infinitiv")]
        public void GetBasicForm_FutureTense_ReturnsCorrectForm(
            string lemma, string pattern, string person, string number,
            string aspect, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Tense = Tense.Future,
                Modus = Modus.Indicative,
                Voice = Voice.Active,
                Person = Enum.Parse<Person>(person),
                Number = Enum.Parse<Number>(number),
                Aspect = Enum.Parse<VerbAspect>(aspect),
                Gender = Gender.Masculine,
            };

            var result = service.GetBasicForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        #endregion Future Tense

        #region Imperative

        /// <summary>
        /// Pokrývá všechny třídy a obě čísla:
        /// - být    → explicitní ImperativeStem z dat ("buď"); ď není DTN → buďme/buďte
        /// - nést   → trida1/nese, fallback na PresentStem="nes"; jedna souhláska → Ø/me/te
        /// - prosit → trida4, PresentStem="pros"; jedna souhláska → Ø/me/te
        /// - tisknout → trida2, ImperativeStem="tiskn"; dvě souhlásky: +i / DTN n → +ěme/+ěte
        /// - kupovat → trida3, ImperativeStem="kupuj"; vokál na konci → Ø/me/te
        /// - dělat  → trida5, ImperativeStem="dělej"; vokál na konci → Ø/me/te
        /// </summary>
        [DataTestMethod]
        // být
        [DataRow("být", "být", "Second", "Singular", "buď", DisplayName = "být – imp. 2sg")]
        [DataRow("být", "být", "First", "Plural", "buďme", DisplayName = "být – imp. 1pl")]
        [DataRow("být", "být", "Second", "Plural", "buďte", DisplayName = "být – imp. 2pl")]
        // nést (trida1 / named pattern nese)
        [DataRow("nést", "nese", "Second", "Singular", "nes", DisplayName = "nést – imp. 2sg")]
        [DataRow("nést", "nese", "First", "Plural", "nesme", DisplayName = "nést – imp. 1pl")]
        [DataRow("nést", "nese", "Second", "Plural", "neste", DisplayName = "nést – imp. 2pl")]
        // prosit (trida4)
        [DataRow("prosit", "trida4", "Second", "Singular", "pros", DisplayName = "prosit – imp. 2sg")]
        [DataRow("prosit", "trida4", "First", "Plural", "prosme", DisplayName = "prosit – imp. 1pl")]
        [DataRow("prosit", "trida4", "Second", "Plural", "proste", DisplayName = "prosit – imp. 2pl")]
        // tisknout (trida2) — dvě souhlásky, finální n je DTN
        [DataRow("tisknout", "trida2", "Second", "Singular", "tiskni", DisplayName = "tisknout – imp. 2sg")]
        [DataRow("tisknout", "trida2", "First", "Plural", "tiskněme", DisplayName = "tisknout – imp. 1pl")]
        [DataRow("tisknout", "trida2", "Second", "Plural", "tiskněte", DisplayName = "tisknout – imp. 2pl")]
        // kupovat (trida3) — ImperativeStem="kupuj", končí vokálem
        [DataRow("kupovat", "trida3", "Second", "Singular", "kupuj", DisplayName = "kupovat – imp. 2sg")]
        [DataRow("kupovat", "trida3", "First", "Plural", "kupujme", DisplayName = "kupovat – imp. 1pl")]
        [DataRow("kupovat", "trida3", "Second", "Plural", "kupujte", DisplayName = "kupovat – imp. 2pl")]
        // dělat (trida5) — ImperativeStem="dělej", končí vokálem
        [DataRow("dělat", "dělá", "Second", "Singular", "dělej", DisplayName = "dělat – imp. 2sg")]
        [DataRow("dělat", "dělá", "First", "Plural", "dělejme", DisplayName = "dělat – imp. 1pl")]
        [DataRow("dělat", "dělá", "Second", "Plural", "dělejte", DisplayName = "dělat – imp. 2pl")]
        public void GetBasicForm_Imperative_ReturnsCorrectForm(
            string lemma, string pattern, string person, string number, string expected)
        {
            var request = new CzechWordRequest
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Verb,
                Modus = Modus.Imperative,
                Voice = Voice.Active,
                Person = Enum.Parse<Person>(person),
                Number = Enum.Parse<Number>(number),
            };

            var result = service.GetBasicForm(request);

            Assert.AreEqual(expected, result.Form);
        }

        #endregion Imperative

        /// <summary>
        /// A second-class verb forms its passive participle on -nut, not on the bare stem.
        /// </summary>
        /// <remarks>
        /// The pattern used to append -n and produced <em>poslechna</em>, which is not a word. IJP gives
        /// <em>poslechnut</em> for poslechnout and lists <em>tisknut</em> for tisknout, so -nut is the
        /// regular form of the class and the old ending was written from an exception.
        /// <para>
        /// What the change costs is the <em>tištěn</em> variant, which IJP also lists: the sk → ště
        /// alternation belongs to the participle in -en and is now skipped for -nut, and substituting the
        /// stem cannot express it because the ending is appended to whatever the stem becomes.
        /// </para>
        /// </remarks>
        [DataTestMethod]
        [DataRow("poslechnout", "poslechnut")]
        [DataRow("tisknout", "tisknut")]
        [DataRow("mrznout", "mrznut")]
        [DataRow("blýsknout", "blýsknut")]
        public void SecondClassFormsThePassiveParticipleOnNut(string lemma, string expected)
        {
            var result = service.GetBasicForm(new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Verb,
                Pattern = "trida2",
                Voice = Voice.Passive,
                Modus = Modus.Indicative,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Person = Person.Third,
                Tense = Tense.Present,
            });

            Assert.AreEqual(expected, result.Form);
        }

        /// <summary>
        /// říct conjugates by the second class and keeps the participle of the first.
        /// </summary>
        [TestMethod]
        public void RictKeepsItsOwnParticiple()
        {
            var result = service.GetBasicForm(new CzechWordRequest
            {
                Lemma = "říct",
                WordCategory = WordCategory.Verb,
                Pattern = "říct",
                Voice = Voice.Passive,
                Modus = Modus.Indicative,
                Gender = Gender.Masculine,
                Number = Number.Singular,
                Person = Person.Third,
                Tense = Tense.Present,
            });

            Assert.AreEqual("řečen", result.Form);
        }
    }
}
