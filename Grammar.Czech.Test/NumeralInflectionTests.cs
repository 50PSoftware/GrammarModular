using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies numeral declension against the Internetová jazyková příručka ÚJČ.
    /// </summary>
    /// <remarks>
    /// The reference chapters are id=670 (dva, oba), id=671 (tři, čtyři), id=791 and id=792 (compounds and
    /// the counted object), and id=160/785 (ordinals).
    /// </remarks>
    [TestClass]
    public sealed class NumeralInflectionTests
    {
        private static ServiceProvider provider = null!;
        private static CzechNumeralService service = null!;

        /// <summary>
        /// Builds the service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
            service = provider.GetRequiredService<CzechNumeralService>();
        }

        /// <summary>
        /// Releases the service graph.
        /// </summary>
        [ClassCleanup]
        public static void CleanupClass() => provider.Dispose();

        private static string? Form(
            string lemma,
            string grammaticalCase,
            string? gender = null,
            string? number = null,
            bool? animate = null,
            NumeralFormOptions? options = null) =>
            service.TryGetForm(
                lemma,
                Enum.Parse<Case>(grammaticalCase),
                gender is null ? null : Enum.Parse<Gender>(gender),
                number is null ? null : Enum.Parse<Number>(number),
                animate,
                options);

        #region jeden — vzor ten

        /// <summary>
        /// Pokrývá singulár číslovky jeden ve všech rodech a pádech.
        /// </summary>
        [DataTestMethod]
        [DataRow("Nominative", "Masculine", true, "jeden", DisplayName = "jeden – nom. m. živ.")]
        [DataRow("Genitive", "Masculine", true, "jednoho", DisplayName = "jeden – gen. m. živ.")]
        [DataRow("Dative", "Masculine", true, "jednomu", DisplayName = "jeden – dat. m. živ.")]
        [DataRow("Accusative", "Masculine", true, "jednoho", DisplayName = "jeden – akuz. m. živ.")]
        [DataRow("Locative", "Masculine", true, "jednom", DisplayName = "jeden – lok. m. živ.")]
        [DataRow("Instrumental", "Masculine", true, "jedním", DisplayName = "jeden – ins. m. živ.")]
        [DataRow("Accusative", "Masculine", false, "jeden", DisplayName = "jeden – akuz. m. než. (= nom.)")]
        [DataRow("Nominative", "Feminine", null, "jedna", DisplayName = "jedna – nom. fem.")]
        [DataRow("Genitive", "Feminine", null, "jedné", DisplayName = "jedna – gen. fem.")]
        [DataRow("Accusative", "Feminine", null, "jednu", DisplayName = "jedna – akuz. fem.")]
        [DataRow("Instrumental", "Feminine", null, "jednou", DisplayName = "jedna – ins. fem.")]
        [DataRow("Nominative", "Neuter", null, "jedno", DisplayName = "jedno – nom. neutr.")]
        [DataRow("Genitive", "Neuter", null, "jednoho", DisplayName = "jedno – gen. neutr.")]
        public void GetForm_JedenSingular_ReturnsUjcForm(string grammaticalCase, string gender, bool? animate, string expected)
            => Assert.AreEqual(expected, Form("jeden", grammaticalCase, gender, "Singular", animate));

        /// <summary>
        /// Pokrývá plurál číslovky jeden — pro pluralia tantum a spojení „jedni — druzí".
        /// </summary>
        [DataTestMethod]
        [DataRow("Nominative", "Masculine", true, "jedni", DisplayName = "jedni – nom. pl. m. živ.")]
        [DataRow("Accusative", "Masculine", true, "jedny", DisplayName = "jedny – akuz. pl. m. živ.")]
        [DataRow("Nominative", "Feminine", null, "jedny", DisplayName = "jedny – nom. pl. fem.")]
        [DataRow("Nominative", "Neuter", null, "jedna", DisplayName = "jedna – nom. pl. neutr.")]
        [DataRow("Genitive", "Feminine", null, "jedněch", DisplayName = "jedněch – gen. pl.")]
        [DataRow("Dative", "Feminine", null, "jedněm", DisplayName = "jedněm – dat. pl.")]
        [DataRow("Locative", "Feminine", null, "jedněch", DisplayName = "jedněch – lok. pl.")]
        [DataRow("Instrumental", "Feminine", null, "jedněmi", DisplayName = "jedněmi – ins. pl.")]
        public void GetForm_JedenPlural_ReturnsUjcForm(string grammaticalCase, string gender, bool? animate, string expected)
            => Assert.AreEqual(expected, Form("jeden", grammaticalCase, gender, "Plural", animate));

        #endregion

        #region dva, oba — duálové relikty (IJP id=670)

        /// <summary>
        /// Pokrývá dva/dvě a oba/obě. Ve 2. a 6. p. je pro všechny rody dvou/obou, ve 3. a 7. p. dvěma/oběma.
        /// </summary>
        [DataTestMethod]
        [DataRow("dva", "Nominative", "Masculine", "dva", DisplayName = "dva – nom. mask.")]
        [DataRow("dva", "Nominative", "Feminine", "dvě", DisplayName = "dvě – nom. fem.")]
        [DataRow("dva", "Nominative", "Neuter", "dvě", DisplayName = "dvě – nom. neutr.")]
        [DataRow("dva", "Genitive", "Masculine", "dvou", DisplayName = "dvou – gen. mask.")]
        [DataRow("dva", "Genitive", "Feminine", "dvou", DisplayName = "dvou – gen. fem.")]
        [DataRow("dva", "Dative", "Masculine", "dvěma", DisplayName = "dvěma – dat. mask.")]
        [DataRow("dva", "Dative", "Feminine", "dvěma", DisplayName = "dvěma – dat. fem.")]
        [DataRow("dva", "Accusative", "Masculine", "dva", DisplayName = "dva – akuz. mask.")]
        [DataRow("dva", "Accusative", "Feminine", "dvě", DisplayName = "dvě – akuz. fem.")]
        [DataRow("dva", "Locative", "Neuter", "dvou", DisplayName = "dvou – lok. neutr.")]
        [DataRow("dva", "Instrumental", "Neuter", "dvěma", DisplayName = "dvěma – ins. neutr.")]
        [DataRow("oba", "Nominative", "Masculine", "oba", DisplayName = "oba – nom. mask.")]
        [DataRow("oba", "Nominative", "Feminine", "obě", DisplayName = "obě – nom. fem.")]
        [DataRow("oba", "Genitive", "Feminine", "obou", DisplayName = "obou – gen. fem.")]
        [DataRow("oba", "Dative", "Masculine", "oběma", DisplayName = "oběma – dat. mask.")]
        [DataRow("oba", "Locative", "Masculine", "obou", DisplayName = "obou – lok. mask.")]
        [DataRow("oba", "Instrumental", "Feminine", "oběma", DisplayName = "oběma – ins. fem.")]
        public void GetForm_DualRelic_ReturnsUjcForm(string lemma, string grammaticalCase, string gender, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase, gender, "Plural", true));

        /// <summary>
        /// Nespisovné tvary se nesmějí objevit v žádném pádu ani rodu.
        /// </summary>
        [DataTestMethod]
        [DataRow("dva", "dvouch", DisplayName = "dva – nikdy *dvouch")]
        [DataRow("dva", "dvoum", DisplayName = "dva – nikdy *dvoum")]
        [DataRow("dva", "dvouma", DisplayName = "dva – nikdy *dvouma")]
        [DataRow("dva", "dvěmi", DisplayName = "dva – nikdy *dvěmi")]
        [DataRow("oba", "obouch", DisplayName = "oba – nikdy *obouch")]
        [DataRow("oba", "oboum", DisplayName = "oba – nikdy *oboum")]
        [DataRow("oba", "obouma", DisplayName = "oba – nikdy *obouma")]
        [DataRow("oba", "oběmi", DisplayName = "oba – nikdy *oběmi")]
        public void GetForm_DualRelic_NeverProducesNonStandardForm(string lemma, string forbidden)
        {
            foreach (var grammaticalCase in Enum.GetValues<Case>())
            {
                foreach (var gender in Enum.GetValues<Gender>())
                {
                    Assert.AreNotEqual(
                        forbidden,
                        service.TryGetForm(lemma, grammaticalCase, gender, Number.Plural, true, null),
                        $"'{lemma}' vytvořilo nespisovný tvar '{forbidden}' v pádu {grammaticalCase} ({gender}).");
                }
            }
        }

        #endregion

        #region tři, čtyři — vzor kost s odchylkami (IJP id=671)

        /// <summary>
        /// Pokrývá tři a čtyři včetně odchylek v instrumentálu.
        /// </summary>
        [DataTestMethod]
        [DataRow("tři", "Nominative", "tři", DisplayName = "tři – nom.")]
        [DataRow("tři", "Genitive", "tří", DisplayName = "tří – gen. (preferováno)")]
        [DataRow("tři", "Dative", "třem", DisplayName = "třem – dat.")]
        [DataRow("tři", "Accusative", "tři", DisplayName = "tři – akuz.")]
        [DataRow("tři", "Locative", "třech", DisplayName = "třech – lok.")]
        [DataRow("tři", "Instrumental", "třemi", DisplayName = "třemi – ins.")]
        [DataRow("čtyři", "Nominative", "čtyři", DisplayName = "čtyři – nom.")]
        [DataRow("čtyři", "Genitive", "čtyř", DisplayName = "čtyř – gen. (preferováno)")]
        [DataRow("čtyři", "Dative", "čtyřem", DisplayName = "čtyřem – dat.")]
        [DataRow("čtyři", "Locative", "čtyřech", DisplayName = "čtyřech – lok.")]
        [DataRow("čtyři", "Instrumental", "čtyřmi", DisplayName = "čtyřmi – ins.")]
        public void GetForm_ThreeFour_ReturnsUjcForm(string lemma, string grammaticalCase, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase));

        /// <summary>
        /// Dubletní genitiv je spisovný a dostupný přes PreferColloquial.
        /// </summary>
        [DataTestMethod]
        [DataRow("tři", "třech", DisplayName = "tři – gen. dubleta třech")]
        [DataRow("čtyři", "čtyřech", DisplayName = "čtyři – gen. dubleta čtyřech")]
        public void GetForm_ThreeFourGenitive_PreferColloquialReturnsDoublet(string lemma, string expected)
            => Assert.AreEqual(expected, Form(lemma, "Genitive", options: new NumeralFormOptions(PreferColloquial: true)));

        /// <summary>
        /// Duálový instrumentál pro párové části těla: s třema rukama, mezi čtyřma očima.
        /// </summary>
        [DataTestMethod]
        [DataRow("tři", "třema", DisplayName = "tři – ins. duál třema")]
        [DataRow("čtyři", "čtyřma", DisplayName = "čtyři – ins. duál čtyřma")]
        public void GetForm_ThreeFourInstrumental_PairedReturnsDualForm(string lemma, string expected)
            => Assert.AreEqual(expected, Form(lemma, "Instrumental", options: new NumeralFormOptions(Paired: true)));

        #endregion

        #region pět–devadesát — dvoutvarové paradigma

        /// <summary>
        /// Pokrývá dvoutvarové paradigma: 1./4. p. holý tvar, ostatní pády s -i.
        /// </summary>
        [DataTestMethod]
        [DataRow("pět", "Nominative", "pět", DisplayName = "pět – nom.")]
        [DataRow("pět", "Accusative", "pět", DisplayName = "pět – akuz.")]
        [DataRow("pět", "Genitive", "pěti", DisplayName = "pěti – gen.")]
        [DataRow("pět", "Dative", "pěti", DisplayName = "pěti – dat.")]
        [DataRow("pět", "Locative", "pěti", DisplayName = "pěti – lok.")]
        [DataRow("pět", "Instrumental", "pěti", DisplayName = "pěti – ins.")]
        [DataRow("šest", "Genitive", "šesti", DisplayName = "šesti – gen.")]
        [DataRow("sedm", "Genitive", "sedmi", DisplayName = "sedmi – gen.")]
        [DataRow("osm", "Instrumental", "osmi", DisplayName = "osmi – ins.")]
        [DataRow("devět", "Genitive", "devíti", DisplayName = "devíti – gen. (ě→í)")]
        [DataRow("devět", "Instrumental", "devíti", DisplayName = "devíti – ins. (ě→í)")]
        [DataRow("deset", "Genitive", "deseti", DisplayName = "deseti – gen.")]
        [DataRow("jedenáct", "Genitive", "jedenácti", DisplayName = "jedenácti – gen.")]
        [DataRow("devatenáct", "Locative", "devatenácti", DisplayName = "devatenácti – lok.")]
        [DataRow("dvacet", "Genitive", "dvaceti", DisplayName = "dvaceti – gen.")]
        [DataRow("padesát", "Instrumental", "padesáti", DisplayName = "padesáti – ins.")]
        [DataRow("devadesát", "Genitive", "devadesáti", DisplayName = "devadesáti – gen.")]
        public void GetForm_FiveNinetyNine_ReturnsUjcForm(string lemma, string grammaticalCase, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase));

        /// <summary>
        /// Hovorové tvary na -ma jsou nespisovné a nesmějí vzniknout.
        /// </summary>
        [DataTestMethod]
        [DataRow("pět", "pětma", DisplayName = "pět – nikdy *pětma")]
        [DataRow("sedm", "sedma", DisplayName = "sedm – nikdy *sedma")]
        [DataRow("osm", "osma", DisplayName = "osm – nikdy *osma")]
        public void GetForm_FiveNinetyNine_NeverProducesNonStandardForm(string lemma, string forbidden)
        {
            foreach (var grammaticalCase in Enum.GetValues<Case>())
            {
                Assert.AreNotEqual(
                    forbidden,
                    Form(lemma, grammaticalCase.ToString()),
                    $"'{lemma}' vytvořilo nespisovný tvar '{forbidden}' v pádu {grammaticalCase}.");
            }
        }

        #endregion

        #region sto, tisíc, milion, miliarda — substantivní flexe

        /// <summary>
        /// Pokrývá sto (vzor město) včetně nepravidelného lokálu stu a genitivu plurálu set.
        /// </summary>
        [DataTestMethod]
        [DataRow("Nominative", "Singular", "sto", DisplayName = "sto – nom. sg.")]
        [DataRow("Genitive", "Singular", "sta", DisplayName = "sta – gen. sg.")]
        [DataRow("Dative", "Singular", "stu", DisplayName = "stu – dat. sg.")]
        [DataRow("Accusative", "Singular", "sto", DisplayName = "sto – akuz. sg.")]
        [DataRow("Locative", "Singular", "stu", DisplayName = "stu – lok. sg. (NE *stě)")]
        [DataRow("Instrumental", "Singular", "stem", DisplayName = "stem – ins. sg.")]
        [DataRow("Nominative", "Plural", "sta", DisplayName = "sta – nom. pl.")]
        [DataRow("Genitive", "Plural", "set", DisplayName = "set – gen. pl.")]
        [DataRow("Dative", "Plural", "stům", DisplayName = "stům – dat. pl.")]
        [DataRow("Locative", "Plural", "stech", DisplayName = "stech – lok. pl.")]
        [DataRow("Instrumental", "Plural", "sty", DisplayName = "sty – ins. pl.")]
        public void GetForm_Sto_ReturnsUjcForm(string grammaticalCase, string number, string expected)
            => Assert.AreEqual(expected, Form("sto", grammaticalCase, "Neuter", number, false));

        /// <summary>
        /// Pokrývá tisíc (vzor stroj), milion a bilion (hrad) a miliardu (žena).
        /// </summary>
        [DataTestMethod]
        [DataRow("tisíc", "Nominative", "Singular", "tisíc", DisplayName = "tisíc – nom. sg.")]
        [DataRow("tisíc", "Genitive", "Singular", "tisíce", DisplayName = "tisíce – gen. sg.")]
        [DataRow("tisíc", "Dative", "Singular", "tisíci", DisplayName = "tisíci – dat. sg.")]
        [DataRow("tisíc", "Instrumental", "Singular", "tisícem", DisplayName = "tisícem – ins. sg.")]
        [DataRow("tisíc", "Nominative", "Plural", "tisíce", DisplayName = "tisíce – nom. pl.")]
        [DataRow("tisíc", "Genitive", "Plural", "tisíc", DisplayName = "tisíc – gen. pl. (preferováno)")]
        [DataRow("tisíc", "Dative", "Plural", "tisícům", DisplayName = "tisícům – dat. pl.")]
        [DataRow("tisíc", "Locative", "Plural", "tisících", DisplayName = "tisících – lok. pl.")]
        [DataRow("milion", "Genitive", "Singular", "milionu", DisplayName = "milionu – gen. sg.")]
        [DataRow("milion", "Genitive", "Plural", "milionů", DisplayName = "milionů – gen. pl.")]
        [DataRow("milion", "Instrumental", "Singular", "milionem", DisplayName = "milionem – ins. sg.")]
        [DataRow("miliarda", "Genitive", "Singular", "miliardy", DisplayName = "miliardy – gen. sg.")]
        [DataRow("miliarda", "Dative", "Singular", "miliardě", DisplayName = "miliardě – dat. sg.")]
        [DataRow("miliarda", "Accusative", "Singular", "miliardu", DisplayName = "miliardu – akuz. sg.")]
        [DataRow("miliarda", "Genitive", "Plural", "miliard", DisplayName = "miliard – gen. pl.")]
        public void GetForm_ScaleNoun_ReturnsUjcForm(string lemma, string grammaticalCase, string number, string expected)
        {
            var data = service.GetData(lemma)!;
            Assert.AreEqual(expected, Form(lemma, grammaticalCase, data.Gender.ToString(), number, false));
        }

        /// <summary>
        /// Genitiv plurálu tisíců je spisovná dubleta k preferovanému tisíc.
        /// </summary>
        [TestMethod]
        public void GetForm_TisicGenitivePlural_PreferRareReturnsDoublet()
            => Assert.AreEqual(
                "tisíců",
                Form("tisíc", "Genitive", "Masculine", "Plural", false, new NumeralFormOptions(PreferRare: true)),
                "Dubletní genitiv plurálu 'tisíců' není dostupný.");

        #endregion

        #region Řadové číslovky (IJP id=160, 785)

        /// <summary>
        /// Pokrývá řadové číslovky. První, třetí a tisící jdou podle vzoru jarní, ostatní podle mladý.
        /// </summary>
        [DataTestMethod]
        [DataRow("první", "Nominative", "Masculine", "první", DisplayName = "první – nom. m.")]
        [DataRow("první", "Genitive", "Masculine", "prvního", DisplayName = "prvního – gen. m.")]
        [DataRow("první", "Instrumental", "Feminine", "první", DisplayName = "první – ins. fem.")]
        [DataRow("druhý", "Nominative", "Masculine", "druhý", DisplayName = "druhý – nom. m.")]
        [DataRow("druhý", "Genitive", "Masculine", "druhého", DisplayName = "druhého – gen. m.")]
        [DataRow("druhý", "Nominative", "Feminine", "druhá", DisplayName = "druhá – nom. fem.")]
        [DataRow("třetí", "Genitive", "Masculine", "třetího", DisplayName = "třetího – gen. m.")]
        [DataRow("pátý", "Nominative", "Neuter", "páté", DisplayName = "páté – nom. neutr.")]
        [DataRow("dvacátý", "Genitive", "Masculine", "dvacátého", DisplayName = "dvacátého – gen. m.")]
        [DataRow("stý", "Nominative", "Masculine", "stý", DisplayName = "stý – nom. m.")]
        [DataRow("tisící", "Nominative", "Masculine", "tisící", DisplayName = "tisící – nom. m. (NE *tisícátý)")]
        [DataRow("tisící", "Genitive", "Masculine", "tisícího", DisplayName = "tisícího – gen. m.")]
        public void GetForm_Ordinal_ReturnsUjcForm(string lemma, string grammaticalCase, string gender, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase, gender, "Singular", true));

        /// <summary>
        /// Řadová číslovka od tisíc je tisící, nikoli tvar podle vzoru mladý.
        /// </summary>
        [TestMethod]
        public void GetMorphology_Tisici_IsSoftAdjective()
        {
            Assert.AreEqual(NumeralMorphology.SoftAdjective, service.GetMorphology("tisící"), "tisící musí jít podle vzoru jarní.");
            Assert.IsFalse(service.IsNumeral("tisícátý"), "*tisícátý není spisovný tvar a nesmí být ve slovníku.");
        }

        #endregion

        #region Neurčité číslovky

        /// <summary>
        /// Pokrývá neurčité číslovky s dvoutvarovým paradigmatem.
        /// </summary>
        [DataTestMethod]
        [DataRow("několik", "Nominative", "několik", DisplayName = "několik – nom.")]
        [DataRow("několik", "Genitive", "několika", DisplayName = "několika – gen.")]
        [DataRow("několik", "Instrumental", "několika", DisplayName = "několika – ins.")]
        [DataRow("kolik", "Genitive", "kolika", DisplayName = "kolika – gen.")]
        [DataRow("tolik", "Dative", "tolika", DisplayName = "tolika – dat.")]
        [DataRow("mnoho", "Nominative", "mnoho", DisplayName = "mnoho – nom.")]
        [DataRow("mnoho", "Genitive", "mnoha", DisplayName = "mnoha – gen.")]
        [DataRow("mnoho", "Locative", "mnoha", DisplayName = "mnoha – lok.")]
        [DataRow("málo", "Genitive", "mála", DisplayName = "mála – gen.")]
        public void GetForm_Indefinite_ReturnsUjcForm(string lemma, string grammaticalCase, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase));

        /// <summary>
        /// Nesklonné neurčité číslovky vracejí lemma ve všech pádech.
        /// </summary>
        [DataTestMethod]
        [DataRow("moc", DisplayName = "moc – nesklonné")]
        [DataRow("hodně", DisplayName = "hodně – nesklonné")]
        [DataRow("dost", DisplayName = "dost – nesklonné")]
        [DataRow("pár", DisplayName = "pár – nesklonné")]
        public void GetForm_Indeclinable_ReturnsLemmaInEveryCase(string lemma)
        {
            foreach (var grammaticalCase in Enum.GetValues<Case>())
            {
                Assert.AreEqual(lemma, Form(lemma, grammaticalCase.ToString()), $"'{lemma}' se nesmí skloňovat.");
            }
        }

        #endregion

        #region Druhové číslovky (kolikerý?)

        /// <summary>
        /// Druhové číslovky jdou podle vzorů jarní (dvojí, trojí, obojí) a mladý (čtverý a výš).
        /// </summary>
        [DataTestMethod]
        [DataRow("dvojí", "Nominative", "Masculine", "dvojí", DisplayName = "dvojí – nom. m.")]
        [DataRow("dvojí", "Genitive", "Masculine", "dvojího", DisplayName = "dvojího – gen. m.")]
        [DataRow("trojí", "Genitive", "Masculine", "trojího", DisplayName = "trojího – gen. m.")]
        [DataRow("obojí", "Dative", "Masculine", "obojímu", DisplayName = "obojímu – dat. m.")]
        [DataRow("paterý", "Nominative", "Masculine", "paterý", DisplayName = "paterý – nom. m.")]
        [DataRow("paterý", "Nominative", "Feminine", "paterá", DisplayName = "paterá – nom. fem.")]
        [DataRow("paterý", "Genitive", "Masculine", "paterého", DisplayName = "paterého – gen. m.")]
        [DataRow("čtverý", "Genitive", "Masculine", "čtverého", DisplayName = "čtverého – gen. m.")]
        [DataRow("desaterý", "Nominative", "Neuter", "desateré", DisplayName = "desateré – nom. neutr.")]
        public void GetForm_Sortal_ReturnsUjcForm(string lemma, string grammaticalCase, string gender, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase, gender, "Singular", true));

        #endregion

        #region Souborové číslovky (kolikery?)

        /// <summary>
        /// Souborové číslovky se pojí s pluralia tantum a stojí vždy v plurálu: dvoje dveře, patery housle.
        /// </summary>
        [DataTestMethod]
        [DataRow("dvoje", "Nominative", "dvoje", DisplayName = "dvoje – nom. pl.")]
        [DataRow("dvoje", "Accusative", "dvoje", DisplayName = "dvoje – akuz. pl.")]
        [DataRow("dvoje", "Genitive", "dvojích", DisplayName = "dvojích – gen. pl.")]
        [DataRow("dvoje", "Dative", "dvojím", DisplayName = "dvojím – dat. pl.")]
        [DataRow("dvoje", "Instrumental", "dvojími", DisplayName = "dvojími – ins. pl.")]
        [DataRow("troje", "Nominative", "troje", DisplayName = "troje – nom. pl.")]
        [DataRow("oboje", "Nominative", "oboje", DisplayName = "oboje – nom. pl.")]
        [DataRow("patery", "Nominative", "patery", DisplayName = "patery – nom. pl.")]
        [DataRow("patery", "Genitive", "paterých", DisplayName = "paterých – gen. pl.")]
        [DataRow("patery", "Dative", "paterým", DisplayName = "paterým – dat. pl.")]
        [DataRow("patery", "Instrumental", "paterými", DisplayName = "paterými – ins. pl.")]
        [DataRow("čtvery", "Nominative", "čtvery", DisplayName = "čtvery – nom. pl.")]
        [DataRow("jedny", "Nominative", "jedny", DisplayName = "jedny – nom. pl.")]
        [DataRow("jedny", "Genitive", "jedněch", DisplayName = "jedněch – gen. pl.")]
        public void GetForm_Set_ReturnsUjcForm(string lemma, string grammaticalCase, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase, "Feminine", "Plural", false));

        #endregion

        #region Úhrnné číslovky (kolikero?)

        /// <summary>
        /// Úhrnné číslovky se skloňují jako neutrum podle vzoru město (IJP id=792).
        /// </summary>
        [DataTestMethod]
        [DataRow("patero", "Nominative", "patero", DisplayName = "patero – nom.")]
        [DataRow("patero", "Genitive", "patera", DisplayName = "patera – gen. (bez patera ponožek)")]
        [DataRow("patero", "Dative", "pateru", DisplayName = "pateru – dat.")]
        [DataRow("patero", "Locative", "pateru", DisplayName = "pateru – lok. (o pateru ponožek)")]
        [DataRow("patero", "Instrumental", "paterem", DisplayName = "paterem – ins. (s paterem ponožek)")]
        [DataRow("desatero", "Genitive", "desatera", DisplayName = "desatera – gen.")]
        [DataRow("desatero", "Instrumental", "desaterem", DisplayName = "desaterem – ins.")]
        [DataRow("čtvero", "Genitive", "čtvera", DisplayName = "čtvera – gen.")]
        public void GetForm_Aggregate_ReturnsUjcForm(string lemma, string grammaticalCase, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase, "Neuter", "Singular", false));

        /// <summary>
        /// Dvé a tré jsou ustrnulé a neskloňují se.
        /// </summary>
        [DataTestMethod]
        [DataRow("dvé", DisplayName = "dvé – ustrnulé")]
        [DataRow("tré", DisplayName = "tré – ustrnulé")]
        public void GetForm_AggregateFrozen_ReturnsLemmaInEveryCase(string lemma)
        {
            foreach (var grammaticalCase in Enum.GetValues<Case>())
            {
                Assert.AreEqual(lemma, Form(lemma, grammaticalCase.ToString()), $"'{lemma}' se nesmí skloňovat.");
            }
        }

        #endregion

        #region Násobné číslovky (kolikrát?)

        /// <summary>
        /// Tvary na -krát, -násob a -mo jsou adverbia a neskloňují se.
        /// </summary>
        [DataTestMethod]
        [DataRow("dvakrát", DisplayName = "dvakrát – adverbium")]
        [DataRow("pětkrát", DisplayName = "pětkrát – adverbium")]
        [DataRow("tisíckrát", DisplayName = "tisíckrát – adverbium")]
        [DataRow("několikrát", DisplayName = "několikrát – adverbium")]
        [DataRow("dvojnásob", DisplayName = "dvojnásob – adverbium")]
        [DataRow("trojmo", DisplayName = "trojmo – adverbium")]
        public void GetForm_MultiplicativeAdverb_ReturnsLemmaInEveryCase(string lemma)
        {
            foreach (var grammaticalCase in Enum.GetValues<Case>())
            {
                Assert.AreEqual(lemma, Form(lemma, grammaticalCase.ToString()), $"'{lemma}' se nesmí skloňovat.");
            }
        }

        /// <summary>
        /// Tvary na -násobný a -itý jsou adjektiva podle vzoru mladý.
        /// </summary>
        [DataTestMethod]
        [DataRow("dvojnásobný", "Nominative", "Masculine", "dvojnásobný", DisplayName = "dvojnásobný – nom. m.")]
        [DataRow("dvojnásobný", "Genitive", "Masculine", "dvojnásobného", DisplayName = "dvojnásobného – gen. m.")]
        [DataRow("trojnásobný", "Nominative", "Feminine", "trojnásobná", DisplayName = "trojnásobná – nom. fem.")]
        [DataRow("dvojitý", "Nominative", "Neuter", "dvojité", DisplayName = "dvojité – nom. neutr.")]
        public void GetForm_MultiplicativeAdjective_ReturnsUjcForm(string lemma, string grammaticalCase, string gender, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase, gender, "Singular", true));

        #endregion

        #region Dílové a skupinové číslovky

        /// <summary>
        /// Dílové číslovky jsou feminina podle vzoru žena; půl a čtvrt se neskloňují.
        /// </summary>
        [DataTestMethod]
        [DataRow("třetina", "Genitive", "Singular", "třetiny", DisplayName = "třetiny – gen. sg.")]
        [DataRow("třetina", "Dative", "Singular", "třetině", DisplayName = "třetině – dat. sg.")]
        [DataRow("třetina", "Accusative", "Singular", "třetinu", DisplayName = "třetinu – akuz. sg.")]
        [DataRow("třetina", "Instrumental", "Singular", "třetinou", DisplayName = "třetinou – ins. sg.")]
        [DataRow("třetina", "Genitive", "Plural", "třetin", DisplayName = "třetin – gen. pl.")]
        [DataRow("polovina", "Genitive", "Singular", "poloviny", DisplayName = "poloviny – gen. sg.")]
        [DataRow("desetina", "Instrumental", "Singular", "desetinou", DisplayName = "desetinou – ins. sg.")]
        public void GetForm_Fractional_ReturnsUjcForm(string lemma, string grammaticalCase, string number, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase, "Feminine", number, false));

        /// <summary>
        /// Skupinové číslovky jsou substantiva: -ice podle růže, hovorové -ka podle žena.
        /// </summary>
        [DataTestMethod]
        [DataRow("trojice", "Genitive", "Singular", "trojice", DisplayName = "trojice – gen. sg.")]
        [DataRow("trojice", "Dative", "Singular", "trojici", DisplayName = "trojici – dat. sg.")]
        [DataRow("trojice", "Instrumental", "Singular", "trojicí", DisplayName = "trojicí – ins. sg. (s trojicí studentů)")]
        [DataRow("dvojice", "Genitive", "Plural", "dvojic", DisplayName = "dvojic – gen. pl.")]
        [DataRow("pětka", "Genitive", "Singular", "pětky", DisplayName = "pětky – gen. sg.")]
        [DataRow("stovka", "Instrumental", "Singular", "stovkou", DisplayName = "stovkou – ins. sg.")]
        public void GetForm_Group_ReturnsUjcForm(string lemma, string grammaticalCase, string number, string expected)
            => Assert.AreEqual(expected, Form(lemma, grammaticalCase, "Feminine", number, false));

        /// <summary>
        /// Genitiv plurálu skupinových číslovek: -ice ztrácí koncovou samohlásku, -ka vkládá pohyblivé -e-.
        /// </summary>
        [DataTestMethod]
        [DataRow("dvojice", "dvojic", DisplayName = "dvojic – gen. pl.")]
        [DataRow("jedenáctice", "jedenáctic", DisplayName = "jedenáctic – gen. pl.")]
        [DataRow("čtyřka", "čtyřek", DisplayName = "čtyřek – gen. pl. (NE *čtyřk)")]
        [DataRow("šestka", "šestek", DisplayName = "šestek – gen. pl.")]
        [DataRow("sedmička", "sedmiček", DisplayName = "sedmiček – gen. pl.")]
        [DataRow("stovka", "stovek", DisplayName = "stovek – gen. pl.")]
        [DataRow("desítka", "desítek", DisplayName = "desítek – gen. pl.")]
        public void GetForm_GroupGenitivePlural_InsertsMobileVowel(string lemma, string expected)
            => Assert.AreEqual(expected, Form(lemma, "Genitive", "Feminine", "Plural", false));

        /// <summary>
        /// Násobné číslovky na -krát pokrývají celou řadu, ne jen vybrané hodnoty.
        /// </summary>
        [DataTestMethod]
        [DataRow(6, "šestkrát", DisplayName = "6 – šestkrát")]
        [DataRow(7, "sedmkrát", DisplayName = "7 – sedmkrát (NE *sedmikrát)")]
        [DataRow(8, "osmkrát", DisplayName = "8 – osmkrát")]
        [DataRow(9, "devětkrát", DisplayName = "9 – devětkrát")]
        [DataRow(14, "čtrnáctkrát", DisplayName = "14 – čtrnáctkrát")]
        [DataRow(60, "šedesátkrát", DisplayName = "60 – šedesátkrát")]
        public void IsNumeral_MultiplicativeSeries_IsComplete(int value, string lemma)
        {
            Assert.IsTrue(service.IsNumeral(lemma), $"'{lemma}' chybí ve slovníku.");
            Assert.AreEqual(value, service.GetValue(lemma), $"'{lemma}' má špatnou hodnotu.");
            Assert.AreEqual(lemma, Form(lemma, "Genitive"), $"'{lemma}' je adverbium a nesmí se skloňovat.");
        }

        /// <summary>
        /// Číslovka zapsaná číslicemi se neskloňuje a nese hodnotu podle zápisu — včetně desetinné.
        /// </summary>
        [DataTestMethod]
        [DataRow("5", DisplayName = "5 – nesklonné")]
        [DataRow("25", DisplayName = "25 – nesklonné")]
        [DataRow("1,5", DisplayName = "1,5 – nesklonné")]
        public void GetForm_NumeralInDigits_IsIndeclinable(string token)
        {
            foreach (var grammaticalCase in Enum.GetValues<Case>())
            {
                Assert.AreEqual(token, Form(token, grammaticalCase.ToString()), $"'{token}' se nesmí skloňovat.");
            }
        }

        /// <summary>
        /// Kongruence odvozená z hodnoty: desetinné číslo se řídí zlomkem, tedy genitivem singuláru.
        /// </summary>
        [DataTestMethod]
        [DataRow("1", CardinalAgreement.AgreesSingular, DisplayName = "1 – shoda sg.")]
        [DataRow("3", CardinalAgreement.AgreesPlural, DisplayName = "3 – shoda pl.")]
        [DataRow("5", CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "5 – Gpl v přímých pádech")]
        [DataRow("100", CardinalAgreement.AlwaysGenitivePlural, DisplayName = "100 – vždy Gpl")]
        [DataRow("1,5", CardinalAgreement.GenitiveSingular, DisplayName = "1,5 – Gsg")]
        [DataRow("0,2", CardinalAgreement.GenitiveSingular, DisplayName = "0,2 – Gsg")]
        [DataRow("14,25", CardinalAgreement.GenitiveSingular, DisplayName = "14,25 – Gsg")]
        public void GetAgreement_NumeralInDigits_DerivesFromValue(string token, CardinalAgreement expected)
            => Assert.AreEqual(expected, service.GetAgreement(token), $"'{token}' má špatnou kongruenci.");

        /// <summary>
        /// Set a stovka jsou různé lexémy: set je genitiv plurálu číslovky sto, stovka je substantivum.
        /// </summary>
        [TestMethod]
        public void GetForm_SetAndStovka_AreDistinctLexemes()
        {
            Assert.AreEqual("set", Form("sto", "Genitive", "Neuter", "Plural", false), "Gpl číslovky sto je 'set'.");
            Assert.AreEqual(NumeralType.Group, service.GetNumeralType("stovka"), "Stovka je skupinová číslovka, ne číslovka základní.");
        }

        #endregion

        #region Duálové instrumentály párových částí těla

        /// <summary>
        /// Spojení číslovky s párovou částí těla má v instrumentálu duálovou koncovku -ma na obou členech:
        /// s třema rukama, mezi čtyřma očima, s oběma nohama.
        /// </summary>
        [DataTestMethod]
        [DataRow("tři", "ruka", "žena", "třema rukama", DisplayName = "s třema rukama")]
        [DataRow("čtyři", "oko", "město", "čtyřma očima", DisplayName = "mezi čtyřma očima")]
        [DataRow("tři", "noha", "žena", "třema nohama", DisplayName = "s třema nohama")]
        [DataRow("čtyři", "ucho", "město", "čtyřma ušima", DisplayName = "se čtyřma ušima")]
        public void GetForm_PairedBodyPart_UsesDualInstrumentalOnBothWords(
            string numeral, string nounLemma, string pattern, string expected)
        {
            var nounService = provider.GetRequiredService<CzechNounDeclensionService>();
            var numeralForm = Form(numeral, "Instrumental", options: new NumeralFormOptions(Paired: true));

            var nounForm = nounService.GetForm(new CzechWordRequest
            {
                Lemma = nounLemma,
                Pattern = pattern,
                WordCategory = WordCategory.Noun,
                Number = Number.Plural,
                Case = Case.Instrumental,
                IsAnimate = false
            }).Form;

            Assert.AreEqual(expected, $"{numeralForm} {nounForm}");
        }

        #endregion

        #region Metadata a kongruence

        /// <summary>
        /// Každá číslovka nese kongruenci, kterou vnucuje počítanému předmětu (IJP id=792).
        /// </summary>
        [DataTestMethod]
        [DataRow("jeden", CardinalAgreement.AgreesSingular, DisplayName = "jeden – shoda sg.")]
        [DataRow("dva", CardinalAgreement.AgreesPlural, DisplayName = "dva – shoda pl.")]
        [DataRow("tři", CardinalAgreement.AgreesPlural, DisplayName = "tři – shoda pl.")]
        [DataRow("čtyři", CardinalAgreement.AgreesPlural, DisplayName = "čtyři – shoda pl.")]
        [DataRow("oba", CardinalAgreement.AgreesPlural, DisplayName = "oba – shoda pl.")]
        [DataRow("pět", CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "pět – Gpl v přímých pádech")]
        [DataRow("devadesát", CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "devadesát – Gpl v přímých pádech")]
        [DataRow("několik", CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "několik – Gpl v přímých pádech")]
        [DataRow("sto", CardinalAgreement.AlwaysGenitivePlural, DisplayName = "sto – vždy Gpl")]
        [DataRow("tisíc", CardinalAgreement.AlwaysGenitivePlural, DisplayName = "tisíc – vždy Gpl")]
        [DataRow("miliarda", CardinalAgreement.AlwaysGenitivePlural, DisplayName = "miliarda – vždy Gpl")]
        [DataRow("pátý", CardinalAgreement.None, DisplayName = "pátý – řadová nic nevnucuje")]
        public void GetAgreement_KnownNumeral_ReturnsUjcAgreement(string lemma, CardinalAgreement expected)
            => Assert.AreEqual(expected, service.GetAgreement(lemma), $"Číslovka '{lemma}' má špatnou kongruenci.");

        /// <summary>
        /// Neznámé lemma nevrací tvar ani kongruenci.
        /// </summary>
        [TestMethod]
        public void TryGetForm_UnknownLemma_ReturnsNull()
        {
            Assert.IsNull(service.TryGetForm("nesmysl", Case.Genitive, null, null, null, null));
            Assert.AreEqual(CardinalAgreement.None, service.GetAgreement("nesmysl"));
            Assert.IsNull(service.GetValue("nesmysl"));
        }

        /// <summary>
        /// Neurčité číslovky nemají číselnou hodnotu, určité ano.
        /// </summary>
        [TestMethod]
        public void GetValue_DefiniteAndIndefinite_DiffersOnValue()
        {
            Assert.AreEqual(5m, service.GetValue("pět"));
            Assert.AreEqual(100m, service.GetValue("sto"));
            Assert.IsNull(service.GetValue("několik"), "Neurčitá číslovka nemá mít číselnou hodnotu.");
        }

        #endregion
    }
}
