using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies the case a numeral imposes on the noun it counts, and the agreement that noun then imposes
    /// on the predicate.
    /// </summary>
    /// <remarks>
    /// The reference is the Internetová jazyková příručka ÚJČ, id=792 — Počítaný předmět po číslovkách.
    /// This is the one construction in Czech where agreement runs from the attribute up to the head rather
    /// than down from it, so these tests are the guard on that inversion.
    /// </remarks>
    [TestClass]
    public sealed class NumeralAgreementTests
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

        private static CzechWordRequest Numeral(string lemma) => new()
        {
            Lemma = lemma,
            WordCategory = WordCategory.Numerale
        };

        private static CzechWordRequest Noun(
            string lemma,
            string pattern,
            Gender gender,
            Case grammaticalCase = Case.Nominative,
            bool? isCountable = null) => new()
            {
                Lemma = lemma,
                Pattern = pattern,
                WordCategory = WordCategory.Noun,
                Gender = gender,
                IsAnimate = gender == Gender.Masculine,
                Number = Number.Singular,
                Case = grammaticalCase,
                IsCountable = isCountable
            };

        // "být" in the past: the auxiliary is dropped in the third person, so what surfaces is the
        // participle alone — which is exactly where the neuter singular of "pět žáků bylo" shows up.
        //
        // The clauses below name the existence frame because být has three of them — the nominal and
        // the adjectival predicate are the other two — and a verb with several is not guessed at. It is
        // the right one here in its own right: "Pět studentů bylo" says that they were, not what they
        // were, so there is no predicate complement to fill.
        private static CzechWordRequest WasPredicate() => new()
        {
            Lemma = "být",
            Pattern = "být",
            WordCategory = WordCategory.Verb,
            Modus = Modus.Indicative,
            Tense = Tense.Past,
            Voice = Voice.Active,
            Person = Person.Third,
            Number = Number.Singular,
            Gender = Gender.Masculine
        };

        private static string BuildCounted(
            string numeral,
            string nounLemma,
            string pattern,
            Gender gender,
            bool? isCountable = null)
        {
            var clause = new CzechClause
            {
                Predicate = WasPredicate(),
                FrameLabel = "existence",
                Elements =
                [
                    ClauseElement.Of(
                        Noun(nounLemma, pattern, gender, Case.Nominative, isCountable),
                        [Numeral(numeral)],
                        FgdFunctor.ACT,
                        InformationStatus.Given)
                ]
            };

            return builder.Build(clause);
        }

        #region Pád počítaného předmětu

        /// <summary>
        /// Číslovky 1–4 se pojí se shodným tvarem, od pěti výš s genitivem plurálu (IJP id=792).
        /// </summary>
        [DataTestMethod]
        [DataRow("jeden", "student", "pán", "Masculine", "Jeden student byl.", DisplayName = "jeden – shoda sg.")]
        [DataRow("dva", "student", "pán", "Masculine", "Dva studenti byli.", DisplayName = "dva – shoda pl.")]
        [DataRow("tři", "student", "pán", "Masculine", "Tři studenti byli.", DisplayName = "tři – shoda pl.")]
        [DataRow("čtyři", "student", "pán", "Masculine", "Čtyři studenti byli.", DisplayName = "čtyři – shoda pl.")]
        [DataRow("pět", "student", "pán", "Masculine", "Pět studentů bylo.", DisplayName = "pět – Gpl + neutr. sg.")]
        [DataRow("deset", "student", "pán", "Masculine", "Deset studentů bylo.", DisplayName = "deset – Gpl + neutr. sg.")]
        [DataRow("dvacet", "student", "pán", "Masculine", "Dvacet studentů bylo.", DisplayName = "dvacet – Gpl + neutr. sg.")]
        [DataRow("několik", "student", "pán", "Masculine", "Několik studentů bylo.", DisplayName = "několik – Gpl + neutr. sg.")]
        [DataRow("mnoho", "student", "pán", "Masculine", "Mnoho studentů bylo.", DisplayName = "mnoho – Gpl + neutr. sg.")]
        [DataRow("sto", "student", "pán", "Masculine", "Sto studentů bylo.", DisplayName = "sto – vždy Gpl")]
        [DataRow("tisíc", "student", "pán", "Masculine", "Tisíc studentů bylo.", DisplayName = "tisíc – vždy Gpl")]
        public void Build_CountedMasculineSubject_UsesUjcCaseAndAgreement(
            string numeral, string noun, string pattern, string gender, string expected)
            => Assert.AreEqual(expected, BuildCounted(numeral, noun, pattern, Enum.Parse<Gender>(gender)));

        /// <summary>
        /// Slovesná shoda u 2–4 jde podle rodu substantiva, u 5+ je vždy neutrum singuláru.
        /// </summary>
        [DataTestMethod]
        [DataRow("dva", "žena", "žena", "Feminine", "Dvě ženy byly.", DisplayName = "dvě ženy byly – fem. pl.")]
        [DataRow("tři", "žena", "žena", "Feminine", "Tři ženy byly.", DisplayName = "tři ženy byly – fem. pl.")]
        [DataRow("pět", "žena", "žena", "Feminine", "Pět žen bylo.", DisplayName = "pět žen bylo – neutr. sg.")]
        [DataRow("dva", "město", "město", "Neuter", "Dvě města byla.", DisplayName = "dvě města byla – neutr. pl.")]
        [DataRow("pět", "město", "město", "Neuter", "Pět měst bylo.", DisplayName = "pět měst bylo – neutr. sg.")]
        public void Build_CountedSubjectOfEveryGender_AgreesPerUjc(
            string numeral, string noun, string pattern, string gender, string expected)
            => Assert.AreEqual(expected, BuildCounted(numeral, noun, pattern, Enum.Parse<Gender>(gender)));

        /// <summary>
        /// V nepřímých pádech se počítaný předmět po 5–99 shoduje (pěti korunám), kdežto po substantivních
        /// sto a tisíc zůstává v genitivu plurálu (ke stu korun).
        /// </summary>
        /// <remarks>
        /// U sto připouští IJP id=792 dvě strategie: nesklonné sto se shodou („ke sto korunám, o sto
        /// korunách, se sto korunami“), nebo skloněné sto s genitivem i se shodou („ke stu korun/korunám“).
        /// Data volí druhou z nich a drží ji důsledně; první tímto modelem vyjádřit nejde, protože ten nese
        /// jednu vazbu na lemma. Test tu volbu fixuje, aby zůstala rozhodnutím, ne náhodou.
        /// </remarks>
        /// <param name="numeral">Číslovka.</param>
        /// <param name="grammaticalCase">Pád celé jmenné skupiny.</param>
        /// <param name="expected">Očekávaný tvar počítaného předmětu.</param>
        [DataTestMethod]
        [DataRow("pět", "Dative", "korunám", DisplayName = "pěti korunám – shoda")]
        [DataRow("pět", "Locative", "korunách", DisplayName = "pěti korunách – shoda")]
        [DataRow("sto", "Dative", "stu korun", DisplayName = "ke stu korun – skloněné sto s genitivem")]
        [DataRow("sto", "Instrumental", "stem korun", DisplayName = "se stem korun – skloněné sto s genitivem")]
        public void Build_CountedNounInObliqueCase_FollowsTheNumeralsAgreementClass(
            string numeral, string grammaticalCase, string expected)
        {
            var clause = new CzechClause
            {
                Predicate = WasPredicate(),
                FrameLabel = "existence",
                Elements =
                [
                    ClauseElement.Of(
                        Noun("koruna", "žena", Gender.Feminine, Enum.Parse<Case>(grammaticalCase)),
                        [Numeral(numeral)],
                        FgdFunctor.LOC,
                        InformationStatus.New)
                ]
            };

            StringAssert.Contains(builder.Build(clause), expected);
        }

        /// <summary>
        /// Mnoho a málo berou u nepočitatelných jmen genitiv singuláru: mnoho práce, ne *mnoho prací.
        /// </summary>
        [TestMethod]
        public void Build_UncountableNounAfterMnoho_UsesGenitiveSingular()
            => Assert.AreEqual(
                "Mnoho práce bylo.",
                BuildCounted("mnoho", "práce", "růže", Gender.Feminine, isCountable: false),
                "Nepočitatelné jméno po 'mnoho' musí stát v genitivu singuláru.");

        /// <summary>
        /// Desetinné číslo se řídí poslední pojmenovanou hodnotou, tedy zlomkem, takže počítaný předmět
        /// stojí v genitivu singuláru: 1,5 metru, 2,36 litru, 14,25 sekundy (IJP id=792).
        /// </summary>
        [DataTestMethod]
        [DataRow("1,5", "metr", "hrad", "Masculine", "1,5 metru bylo.", DisplayName = "1,5 metru – Gsg")]
        [DataRow("0,5", "metr", "hrad", "Masculine", "0,5 metru bylo.", DisplayName = "0,5 metru – Gsg")]
        [DataRow("2,36", "litr", "hrad", "Masculine", "2,36 litru bylo.", DisplayName = "2,36 litru – Gsg")]
        [DataRow("14,25", "sekunda", "žena", "Feminine", "14,25 sekundy bylo.", DisplayName = "14,25 sekundy – Gsg")]
        public void Build_DecimalValue_UsesGenitiveSingular(
            string numeral, string noun, string pattern, string gender, string expected)
            => Assert.AreEqual(expected, BuildCounted(numeral, noun, pattern, Enum.Parse<Gender>(gender)));

        /// <summary>
        /// Číslovka zapsaná číslicemi se řídí svou hodnotou úplně stejně jako zapsaná slovy.
        /// </summary>
        [DataTestMethod]
        [DataRow("1", "1 student byl.", DisplayName = "1 student – shoda sg.")]
        [DataRow("3", "3 studenti byli.", DisplayName = "3 studenti – shoda pl.")]
        [DataRow("5", "5 studentů bylo.", DisplayName = "5 studentů – Gpl")]
        [DataRow("25", "25 studentů bylo.", DisplayName = "25 studentů – Gpl")]
        [DataRow("100", "100 studentů bylo.", DisplayName = "100 studentů – Gpl")]
        public void Build_NumeralWrittenInDigits_AgreesByItsValue(string numeral, string expected)
            => Assert.AreEqual(expected, BuildCounted(numeral, "student", "pán", Gender.Masculine));

        #endregion

        #region Nepřímé pády — shoda se vrací

        /// <summary>
        /// V nepřímých pádech se genitiv nedrží a počítaný předmět se shoduje: o pěti studentech.
        /// </summary>
        /// <remarks>
        /// Rozhoduje pád celé fráze, nikoli přítomnost předložky — proto „pro pět studentů" genitiv drží
        /// (předložka řídí akuzativ, což je přímý pád), zatímco „o pěti studentech" nikoli.
        /// </remarks>
        [DataTestMethod]
        [DataRow("o", "pět", "Locative", "o pěti studentech", DisplayName = "o pěti studentech – lok. drží shodu")]
        [DataRow("s", "pět", "Instrumental", "s pěti studenty", DisplayName = "s pěti studenty – ins. drží shodu")]
        [DataRow("k", "pět", "Dative", "k pěti studentům", DisplayName = "k pěti studentům – dat. drží shodu")]
        [DataRow("pro", "pět", "Accusative", "pro pět studentů", DisplayName = "pro pět studentů – akuz. drží Gpl")]
        [DataRow("s", "čtyři", "Instrumental", "se čtyřmi studenty", DisplayName = "se čtyřmi studenty – vokalizace podle číslovky")]
        [DataRow("s", "tři", "Instrumental", "se třemi studenty", DisplayName = "se třemi studenty – vokalizace podle číslovky")]
        [DataRow("s", "dva", "Instrumental", "se dvěma studenty", DisplayName = "se dvěma studenty – vokalizace podle číslovky")]
        [DataRow("o", "dva", "Locative", "o dvou studentech", DisplayName = "o dvou studentech")]
        public void Build_CountedPrepositionalPhrase_UsesUjcCase(
            string preposition, string numeral, string grammaticalCase, string expectedPhrase)
        {
            var clause = new CzechClause
            {
                Predicate = WasPredicate(),
                FrameLabel = "existence",
                Elements =
                [
                    new ClauseElement
                    {
                        Preposition = preposition,
                        Word = Noun("student", "pán", Gender.Masculine, Enum.Parse<Case>(grammaticalCase)),
                        Modifiers = [Numeral(numeral)],
                        Functor = FgdFunctor.LOC,
                        Status = InformationStatus.New
                    }
                ]
            };

            StringAssert.Contains(builder.Build(clause), expectedPhrase);
        }

        #endregion

        #region Řadové číslovky se chovají jako běžný přívlastek

        /// <summary>
        /// Řadová číslovka nic nevnucuje — shoduje se se svým řídícím jménem jako každý přívlastek.
        /// </summary>
        [DataTestMethod]
        [DataRow("pátý", "student", "pán", "Masculine", "Pátý student byl.", DisplayName = "pátý student byl")]
        [DataRow("první", "student", "pán", "Masculine", "První student byl.", DisplayName = "první student byl")]
        [DataRow("druhý", "žena", "žena", "Feminine", "Druhá žena byla.", DisplayName = "druhá žena byla")]
        [DataRow("tisící", "student", "pán", "Masculine", "Tisící student byl.", DisplayName = "tisící student byl")]
        public void Build_OrdinalAttribute_AgreesWithHead(
            string numeral, string noun, string pattern, string gender, string expected)
            => Assert.AreEqual(expected, BuildCounted(numeral, noun, pattern, Enum.Parse<Gender>(gender)));

        #endregion

        #region Číslovka jako řídící člen

        /// <summary>
        /// Číslovka může být sama hlavou konstituentu, ne jen přívlastkem. Pak nic neřídí a chová se jako
        /// každé jiné jméno — rekce se nespouští, protože není co počítat.
        /// </summary>
        [DataTestMethod]
        [DataRow("pět", "Nominative", "Pět", DisplayName = "pět jako podmět")]
        [DataRow("pět", "Genitive", "Pěti", DisplayName = "pěti – gen.")]
        [DataRow("sto", "Nominative", "Sto", DisplayName = "sto jako podmět")]
        [DataRow("tisíc", "Instrumental", "Tisícem", DisplayName = "tisícem – ins.")]
        public void Build_NumeralAsHead_DeclinesAndGovernsNothing(string lemma, string grammaticalCase, string expectedFirstWord)
        {
            var numeral = Numeral(lemma);
            numeral.Case = Enum.Parse<Case>(grammaticalCase);
            numeral.Gender = Gender.Neuter;
            numeral.Number = Number.Singular;
            numeral.IsAnimate = false;

            var clause = new CzechClause
            {
                Predicate = WasPredicate(),
                FrameLabel = "existence",
                Elements = [ClauseElement.Of(numeral, FgdFunctor.ACT, InformationStatus.Given)]
            };

            StringAssert.StartsWith(builder.Build(clause), expectedFirstWord);
        }

        /// <summary>
        /// Číslovka v pozici hlavy neshazuje predikát do neutra singuláru — to dělá jen počítaný předmět.
        /// </summary>
        [TestMethod]
        public void Build_NumeralAsHead_LeavesPredicateAgreementAlone()
        {
            var numeral = Numeral("pět");
            numeral.Case = Case.Nominative;
            numeral.Gender = Gender.Neuter;
            numeral.Number = Number.Singular;

            var clause = new CzechClause
            {
                Predicate = WasPredicate(),
                FrameLabel = "existence",
                Elements = [ClauseElement.Of(numeral, FgdFunctor.ACT, InformationStatus.Given)]
            };

            Assert.AreEqual("Pět bylo.", builder.Build(clause));
        }

        #endregion

        #region Regrese — věty bez číslovek se nemění

        /// <summary>
        /// Zavedení číslovkové rekce nesmí změnit chování vět, které žádnou číslovku neobsahují.
        /// </summary>
        [TestMethod]
        public void Build_ClauseWithoutNumeral_IsUnaffected()
        {
            var clause = new CzechClause
            {
                Predicate = WasPredicate(),
                FrameLabel = "existence",
                Elements = [ClauseElement.Of(Noun("student", "pán", Gender.Masculine), FgdFunctor.ACT, InformationStatus.Given)]
            };

            Assert.AreEqual("Student byl.", builder.Build(clause));
        }

        #endregion
    }
}
