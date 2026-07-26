using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies that a number spells out as a correctly declined multi-word Czech numeral.
    /// </summary>
    /// <remarks>
    /// The reference is the Internetová jazyková příručka ÚJČ, id=791 and id=792. The primary norm is that
    /// every part declines — před třemi sty šedesáti pěti lety — and the hundreds are irregular in the
    /// direct cases: dvě stě, tři sta, pět set.
    /// </remarks>
    [TestClass]
    public sealed class NumeralCompositionTests
    {
        private static ServiceProvider provider = null!;
        private static CzechNumeralComposer composer = null!;

        /// <summary>
        /// Builds the service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
            composer = provider.GetRequiredService<CzechNumeralComposer>();
        }

        /// <summary>
        /// Releases the service graph.
        /// </summary>
        [ClassCleanup]
        public static void CleanupClass() => provider.Dispose();

        #region Základní tvary v nominativu

        /// <summary>
        /// Pokrývá rozklad na řády a jejich spojení v 1. pádu.
        /// </summary>
        [DataTestMethod]
        [DataRow(0, "nula", DisplayName = "0 – nula")]
        [DataRow(1, "jeden", DisplayName = "1 – jeden")]
        [DataRow(7, "sedm", DisplayName = "7 – sedm")]
        [DataRow(15, "patnáct", DisplayName = "15 – patnáct (jeden lexém)")]
        [DataRow(21, "dvacet jedna", DisplayName = "21 – dvacet jedna")]
        [DataRow(24, "dvacet čtyři", DisplayName = "24 – dvacet čtyři")]
        [DataRow(47, "čtyřicet sedm", DisplayName = "47 – čtyřicet sedm")]
        [DataRow(99, "devadesát devět", DisplayName = "99 – devadesát devět")]
        [DataRow(100, "sto", DisplayName = "100 – sto (ne *jedno sto)")]
        [DataRow(247, "dvě stě čtyřicet sedm", DisplayName = "247 – dvě stě čtyřicet sedm")]
        [DataRow(365, "tři sta šedesát pět", DisplayName = "365 – tři sta šedesát pět")]
        [DataRow(1000, "tisíc", DisplayName = "1000 – tisíc")]
        [DataRow(1009, "tisíc devět", DisplayName = "1009 – tisíc devět")]
        [DataRow(1900, "tisíc devět set", DisplayName = "1900 – tisíc devět set")]
        [DataRow(2547, "dva tisíce pět set čtyřicet sedm", DisplayName = "2547 – dva tisíce pět set čtyřicet sedm")]
        [DataRow(3000, "tři tisíce", DisplayName = "3000 – tři tisíce")]
        [DataRow(5000, "pět tisíc", DisplayName = "5000 – pět tisíc")]
        [DataRow(1000000, "milion", DisplayName = "1000000 – milion")]
        [DataRow(2000000, "dva miliony", DisplayName = "2000000 – dva miliony")]
        public void Compose_Nominative_SpellsOutPerUjc(int value, string expected)
            => Assert.AreEqual(expected, composer.Compose(value, Case.Nominative));

        /// <summary>
        /// Násobky sta mají v přímých pádech tři různé tvary — stě, sta, set (IJP id=792).
        /// </summary>
        [DataTestMethod]
        [DataRow(100, "sto", DisplayName = "100 – sto")]
        [DataRow(200, "dvě stě", DisplayName = "200 – dvě stě (NE *dvě sta)")]
        [DataRow(300, "tři sta", DisplayName = "300 – tři sta")]
        [DataRow(400, "čtyři sta", DisplayName = "400 – čtyři sta")]
        [DataRow(500, "pět set", DisplayName = "500 – pět set")]
        [DataRow(900, "devět set", DisplayName = "900 – devět set")]
        public void Compose_Hundreds_UsesCorrectScaleForm(int value, string expected)
            => Assert.AreEqual(expected, composer.Compose(value, Case.Nominative));

        /// <summary>
        /// Tvar „dvě sta" ve významu 200 je chybný a nesmí vzniknout v žádném přímém pádu.
        /// </summary>
        [TestMethod]
        public void Compose_TwoHundred_NeverProducesDveSta()
        {
            foreach (var grammaticalCase in new[] { Case.Nominative, Case.Accusative, Case.Vocative })
            {
                Assert.AreEqual(
                    "dvě stě",
                    composer.Compose(200, grammaticalCase),
                    $"200 v pádu {grammaticalCase} musí být 'dvě stě', nikdy 'dvě sta'.");
            }
        }

        #endregion

        #region Skloňování víceslovných výrazů

        /// <summary>
        /// Primární normou je skloňovat všechny části výrazu (IJP id=791).
        /// </summary>
        [DataTestMethod]
        [DataRow(365, "Instrumental", "třemi sty šedesáti pěti", DisplayName = "365 – ins. třemi sty šedesáti pěti")]
        [DataRow(365, "Genitive", "tří set šedesáti pěti", DisplayName = "365 – gen. tří set šedesáti pěti")]
        [DataRow(200, "Genitive", "dvou set", DisplayName = "200 – gen. dvou set")]
        [DataRow(200, "Dative", "dvěma stům", DisplayName = "200 – dat. dvěma stům")]
        [DataRow(200, "Locative", "dvou stech", DisplayName = "200 – lok. dvou stech")]
        [DataRow(200, "Instrumental", "dvěma sty", DisplayName = "200 – ins. dvěma sty")]
        [DataRow(500, "Genitive", "pěti set", DisplayName = "500 – gen. pěti set")]
        [DataRow(500, "Instrumental", "pěti sty", DisplayName = "500 – ins. pěti sty")]
        [DataRow(47, "Instrumental", "čtyřiceti sedmi", DisplayName = "47 – ins. čtyřiceti sedmi")]
        [DataRow(2000, "Instrumental", "dvěma tisíci", DisplayName = "2000 – ins. dvěma tisíci")]
        [DataRow(5000, "Instrumental", "pěti tisíci", DisplayName = "5000 – ins. pěti tisíci")]
        [DataRow(5000, "Genitive", "pěti tisíc", DisplayName = "5000 – gen. pěti tisíc")]
        [DataRow(1000, "Instrumental", "tisícem", DisplayName = "1000 – ins. tisícem")]
        public void Compose_ObliqueCase_DeclinesEveryPart(int value, string grammaticalCase, string expected)
            => Assert.AreEqual(expected, composer.Compose(value, Enum.Parse<Case>(grammaticalCase)));

        /// <summary>
        /// Násobitel se shoduje s rodem řádového slova, ne s rodem počítaného jména:
        /// sto je neutrum, tisíc a milion maskulinum, miliarda femininum.
        /// </summary>
        [DataTestMethod]
        [DataRow(200, "dvě stě", DisplayName = "200 – dvě stě (sto = neutrum)")]
        [DataRow(2000, "dva tisíce", DisplayName = "2000 – dva tisíce (tisíc = mask.)")]
        [DataRow(2000000, "dva miliony", DisplayName = "2000000 – dva miliony (milion = mask.)")]
        [DataRow(2000000000, "dvě miliardy", DisplayName = "2000000000 – dvě miliardy (miliarda = fem.)")]
        public void Compose_MultiplierOfScaleWord_AgreesWithScaleGender(long value, string expected)
            => Assert.AreEqual(expected, composer.Compose(value, Case.Nominative, Gender.Feminine, false));

        #endregion

        #region Rod u jedné a dvou

        /// <summary>
        /// Jen jeden a dva odrážejí rod počítaného jména; ve složeném výrazu tuto shodu ztrácejí.
        /// </summary>
        [DataTestMethod]
        [DataRow(1, "Masculine", "jeden", DisplayName = "1 – mask. jeden")]
        [DataRow(1, "Feminine", "jedna", DisplayName = "1 – fem. jedna")]
        [DataRow(1, "Neuter", "jedno", DisplayName = "1 – neutr. jedno")]
        [DataRow(2, "Masculine", "dva", DisplayName = "2 – mask. dva")]
        [DataRow(2, "Feminine", "dvě", DisplayName = "2 – fem. dvě")]
        [DataRow(2, "Neuter", "dvě", DisplayName = "2 – neutr. dvě")]
        public void Compose_OneAndTwo_ReflectGender(int value, string gender, string expected)
            => Assert.AreEqual(expected, composer.Compose(value, Case.Nominative, Enum.Parse<Gender>(gender), true));

        /// <summary>
        /// Ve složeném výrazu se jednička ustrnuje na tvaru jedna bez ohledu na rod (IJP id=792, varianta B).
        /// </summary>
        [DataTestMethod]
        [DataRow("Masculine", DisplayName = "21 – mask. dvacet jedna")]
        [DataRow("Feminine", DisplayName = "21 – fem. dvacet jedna")]
        [DataRow("Neuter", DisplayName = "21 – neutr. dvacet jedna")]
        public void Compose_OneInCompound_FreezesAsJedna(string gender)
            => Assert.AreEqual("dvacet jedna", composer.Compose(21, Case.Nominative, Enum.Parse<Gender>(gender), true));

        #endregion

        #region Rozklad a kongruence

        /// <summary>
        /// Rozklad drží řády od nejvyššího a nedělí lexémy 11–19.
        /// </summary>
        [TestMethod]
        public void Decompose_MixedValue_SplitsByScaleHighestFirst()
        {
            CollectionAssert.AreEqual(
                new[] { (2L, "tisíc"), (5L, "sto"), (40L, null), (7L, null) },
                composer.Decompose(2547).Select(group => (group.Multiplier, group.ScaleLemma)).ToArray());

            CollectionAssert.AreEqual(
                new[] { (1L, "sto"), (17L, null) },
                composer.Decompose(117).Select(group => (group.Multiplier, group.ScaleLemma)).ToArray(),
                "Sedmnáct je jeden lexém a nesmí se rozpadnout na deset a sedm.");
        }

        /// <summary>
        /// Kongruence složeného výrazu se řídí IJP id=792; u 21–99 je preferovaná varianta genitiv plurálu.
        /// </summary>
        [DataTestMethod]
        [DataRow(1, CardinalAgreement.AgreesSingular, DisplayName = "1 – shoda sg.")]
        [DataRow(2, CardinalAgreement.AgreesPlural, DisplayName = "2 – shoda pl.")]
        [DataRow(4, CardinalAgreement.AgreesPlural, DisplayName = "4 – shoda pl.")]
        [DataRow(5, CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "5 – Gpl v přímých pádech")]
        [DataRow(21, CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "21 – Gpl (varianta B)")]
        [DataRow(24, CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "24 – Gpl (varianta B)")]
        [DataRow(99, CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "99 – Gpl")]
        [DataRow(100, CardinalAgreement.AlwaysGenitivePlural, DisplayName = "100 – vždy Gpl")]
        [DataRow(2547, CardinalAgreement.AlwaysGenitivePlural, DisplayName = "2547 – vždy Gpl")]
        public void GetAgreement_Value_MatchesUjc(int value, CardinalAgreement expected)
            => Assert.AreEqual(expected, composer.GetAgreement(value));

        #endregion

        #region Řadové a podílné číslovky

        /// <summary>
        /// U víceslovných řadových číslovek se skloňuje každá část zvlášť (IJP id=791).
        /// </summary>
        [DataTestMethod]
        [DataRow(1, "Nominative", "první", DisplayName = "1. – první")]
        [DataRow(5, "Nominative", "pátý", DisplayName = "5. – pátý")]
        [DataRow(25, "Nominative", "dvacátý pátý", DisplayName = "25. – dvacátý pátý")]
        [DataRow(25, "Genitive", "dvacátého pátého", DisplayName = "25. – gen. dvacátého pátého")]
        [DataRow(1000, "Nominative", "tisící", DisplayName = "1000. – tisící (NE *tisícátý)")]
        [DataRow(1956, "Nominative", "tisící devítistý padesátý šestý", DisplayName = "1956. – tisící devítistý padesátý šestý")]
        [DataRow(100, "Nominative", "stý", DisplayName = "100. – stý")]
        public void ComposeOrdinal_Value_SpellsOutPerUjc(int value, string grammaticalCase, string expected)
            => Assert.AreEqual(expected, composer.ComposeOrdinal(value, Enum.Parse<Case>(grammaticalCase)));

        /// <summary>
        /// Podílné číslovky jsou konstrukce po + lokál, nikoli lexikální položky.
        /// </summary>
        [DataTestMethod]
        [DataRow(1, "po jednom", DisplayName = "po jednom")]
        [DataRow(2, "po dvou", DisplayName = "po dvou")]
        [DataRow(5, "po pěti", DisplayName = "po pěti")]
        public void ComposeDistributive_Value_UsesPoPlusLocative(int value, string expected)
            => Assert.AreEqual(expected, composer.ComposeDistributive(value));

        #endregion

        #region Varianty složených číslovek 21–99 (IJP id=792)

        /// <summary>
        /// Příručka připouští tři zpracování složené číslovky; liší se tvarem i pádem počítaného předmětu.
        /// </summary>
        [DataTestMethod]
        [DataRow(21, "Preferred", "dvacet jedna", DisplayName = "21 – B: dvacet jedna")]
        [DataRow(21, "AgreeingLastMember", "dvacet jeden", DisplayName = "21 – A: dvacet jeden")]
        [DataRow(21, "Contracted", "jedenadvacet", DisplayName = "21 – C: jedenadvacet")]
        [DataRow(24, "Preferred", "dvacet čtyři", DisplayName = "24 – B: dvacet čtyři")]
        [DataRow(24, "AgreeingLastMember", "dvacet čtyři", DisplayName = "24 – A: dvacet čtyři")]
        [DataRow(24, "Contracted", "čtyřiadvacet", DisplayName = "24 – C: čtyřiadvacet")]
        [DataRow(25, "Contracted", "pětadvacet", DisplayName = "25 – C: pětadvacet")]
        [DataRow(32, "Contracted", "dvaatřicet", DisplayName = "32 – C: dvaatřicet")]
        [DataRow(99, "Contracted", "devětadevadesát", DisplayName = "99 – C: devětadevadesát")]
        public void Compose_CompoundVariant_SpellsOutPerUjc(int value, string variant, string expected)
            => Assert.AreEqual(
                expected,
                composer.Compose(value, Case.Nominative, Enum.Parse<CompoundVariant>(variant), Gender.Masculine, true));

        /// <summary>
        /// Spřežka se skloňuje dvoutvarově jako každá číslovka od pěti výš.
        /// </summary>
        [DataTestMethod]
        [DataRow(25, "Genitive", "pětadvaceti", DisplayName = "25 – C gen. pětadvaceti")]
        [DataRow(25, "Instrumental", "pětadvaceti", DisplayName = "25 – C ins. pětadvaceti")]
        [DataRow(21, "Genitive", "jedenadvaceti", DisplayName = "21 – C gen. jedenadvaceti")]
        public void Compose_ContractedOblique_UsesTwoFormRule(int value, string grammaticalCase, string expected)
            => Assert.AreEqual(
                expected,
                composer.Compose(value, Enum.Parse<Case>(grammaticalCase), CompoundVariant.Contracted));

        /// <summary>
        /// Kongruence se u varianty A řídí posledním členem, u B a C je genitiv plurálu.
        /// </summary>
        [DataTestMethod]
        [DataRow(21, "Preferred", CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "21 – B: Gpl")]
        [DataRow(21, "AgreeingLastMember", CardinalAgreement.AgreesSingular, DisplayName = "21 – A: shoda sg.")]
        [DataRow(24, "AgreeingLastMember", CardinalAgreement.AgreesPlural, DisplayName = "24 – A: shoda pl.")]
        [DataRow(25, "AgreeingLastMember", CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "25 – A: Gpl (poslední člen je 5)")]
        [DataRow(24, "Contracted", CardinalAgreement.GenitivePluralInDirectCases, DisplayName = "24 – C: Gpl")]
        public void GetAgreement_CompoundVariant_FollowsUjc(int value, string variant, CardinalAgreement expected)
            => Assert.AreEqual(expected, composer.GetAgreement(value, Enum.Parse<CompoundVariant>(variant)));

        #endregion

        #region Ostatní druhy číslovek z hodnoty

        /// <summary>
        /// Z jedné hodnoty lze pojmenovat číslovku kteréhokoli druhu.
        /// </summary>
        [DataTestMethod]
        [DataRow(5, "Cardinal", "pět", DisplayName = "5 – základní pět")]
        [DataRow(5, "Ordinal", "pátý", DisplayName = "5 – řadová pátý")]
        [DataRow(5, "Sortal", "paterý", DisplayName = "5 – druhová paterý")]
        [DataRow(5, "Aggregate", "patero", DisplayName = "5 – úhrnná patero")]
        [DataRow(5, "Multiplicative", "pětkrát", DisplayName = "5 – násobná pětkrát")]
        [DataRow(2, "Sortal", "dvojí", DisplayName = "2 – druhová dvojí")]
        [DataRow(2, "Multiplicative", "dvakrát", DisplayName = "2 – násobná dvakrát")]
        [DataRow(3, "Group", "trojice", DisplayName = "3 – skupinová trojice")]
        [DataRow(10, "Aggregate", "desatero", DisplayName = "10 – úhrnná desatero")]
        public void ComposeOfType_Value_NamesNumeralOfThatKind(int value, string type, string expected)
            => Assert.AreEqual(
                expected,
                composer.ComposeOfType(value, Enum.Parse<NumeralType>(type), Case.Nominative, Gender.Masculine, true, Number.Singular));

        /// <summary>
        /// Souborové číslovky stojí v plurálu, takže se vyžádají v něm.
        /// </summary>
        [DataTestMethod]
        [DataRow(2, "dvoje", DisplayName = "2 – souborová dvoje")]
        [DataRow(5, "patery", DisplayName = "5 – souborová patery")]
        public void ComposeOfType_Set_NamesPluralForm(int value, string expected)
            => Assert.AreEqual(
                expected,
                composer.ComposeOfType(value, NumeralType.Set, Case.Nominative, Gender.Feminine, false, Number.Plural));

        /// <summary>
        /// Chybějící lemma pro daný druh a hodnotu selže hlasitě.
        /// </summary>
        [TestMethod]
        public void ComposeOfType_MissingLemma_ThrowsNamingValueAndType()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => composer.ComposeOfType(37, NumeralType.Sortal, Case.Nominative));

            StringAssert.Contains(exception.Message, "37");
            StringAssert.Contains(exception.Message, "Sortal");
        }

        #endregion

        #region Řadové spřežky

        /// <summary>
        /// Spřežky řadových číslovek podle IJP id=791: pětadvacátý, stopadesátý.
        /// </summary>
        [DataTestMethod]
        [DataRow(21, "Nominative", "jedenadvacátý", DisplayName = "21. – jedenadvacátý")]
        [DataRow(22, "Nominative", "dvaadvacátý", DisplayName = "22. – dvaadvacátý")]
        [DataRow(24, "Nominative", "čtyřiadvacátý", DisplayName = "24. – čtyřiadvacátý")]
        [DataRow(25, "Nominative", "pětadvacátý", DisplayName = "25. – pětadvacátý")]
        [DataRow(25, "Genitive", "pětadvacátého", DisplayName = "25. – gen. pětadvacátého")]
        [DataRow(99, "Nominative", "devětadevadesátý", DisplayName = "99. – devětadevadesátý")]
        [DataRow(150, "Nominative", "stopadesátý", DisplayName = "150. – stopadesátý")]
        [DataRow(125, "Nominative", "stopětadvacátý", DisplayName = "125. – stopětadvacátý")]
        public void ComposeOrdinal_Contracted_SpellsOutSprezka(int value, string grammaticalCase, string expected)
            => Assert.AreEqual(
                expected,
                composer.ComposeOrdinal(value, Enum.Parse<Case>(grammaticalCase), CompoundVariant.Contracted));

        /// <summary>
        /// Kde spřežka neexistuje, vrátí se nestažený tvar, ne chyba.
        /// </summary>
        [DataTestMethod]
        [DataRow(5, "pátý", DisplayName = "5. – pátý (není co stahovat)")]
        [DataRow(30, "třicátý", DisplayName = "30. – třicátý (celá desítka)")]
        [DataRow(100, "stý", DisplayName = "100. – stý")]
        [DataRow(1956, "tisící devítistý padesátý šestý", DisplayName = "1956. – nestažené")]
        public void ComposeOrdinal_ContractedWhereNoSprezkaExists_FallsBackToSpacedForm(int value, string expected)
            => Assert.AreEqual(expected, composer.ComposeOrdinal(value, Case.Nominative, CompoundVariant.Contracted));

        #endregion

        #region Zlomky a desetinná čísla slovy

        /// <summary>
        /// Jmenovatel je počítané jméno jako každé jiné, takže se řídí čitatelem.
        /// </summary>
        [DataTestMethod]
        [DataRow(1, 2, "Nominative", "jedna polovina", DisplayName = "1/2 – jedna polovina")]
        [DataRow(2, 3, "Nominative", "dvě třetiny", DisplayName = "2/3 – dvě třetiny")]
        [DataRow(3, 4, "Nominative", "tři čtvrtiny", DisplayName = "3/4 – tři čtvrtiny")]
        [DataRow(5, 8, "Nominative", "pět osmin", DisplayName = "5/8 – pět osmin (Gpl)")]
        [DataRow(7, 10, "Nominative", "sedm desetin", DisplayName = "7/10 – sedm desetin")]
        [DataRow(3, 4, "Genitive", "tří čtvrtin", DisplayName = "3/4 – gen. tří čtvrtin")]
        [DataRow(3, 100, "Nominative", "tři setiny", DisplayName = "3/100 – tři setiny")]
        public void ComposeFraction_Values_SpellsOutPerUjc(int numerator, int denominator, string grammaticalCase, string expected)
            => Assert.AreEqual(expected, composer.ComposeFraction(numerator, denominator, Enum.Parse<Case>(grammaticalCase)));

        /// <summary>
        /// Desetinné číslo se čte jako celá část, slovo celá a zlomek podle počtu desetinných míst.
        /// </summary>
        [DataTestMethod]
        [DataRow("0,5", "nula celá pět desetin", DisplayName = "0,5 – nula celá pět desetin")]
        [DataRow("1,5", "jedna celá pět desetin", DisplayName = "1,5 – jedna celá pět desetin")]
        [DataRow("2,3", "dvě celé tři desetiny", DisplayName = "2,3 – dvě celé tři desetiny")]
        [DataRow("3,14", "tři celé čtrnáct setin", DisplayName = "3,14 – tři celé čtrnáct setin")]
        [DataRow("1,25", "jedna celá dvacet pět setin", DisplayName = "1,25 – jedna celá dvacet pět setin")]
        [DataRow("14,25", "čtrnáct celých dvacet pět setin", DisplayName = "14,25 – čtrnáct celých dvacet pět setin")]
        public void ComposeDecimal_Values_SpellsOutPerUjc(string literal, string expected)
            => Assert.AreEqual(
                expected,
                composer.ComposeDecimal(decimal.Parse(literal.Replace(',', '.'), CultureInfo.InvariantCulture)));

        /// <summary>
        /// Celé číslo zapsané jako desetinné se vypíše jako základní číslovka.
        /// </summary>
        [TestMethod]
        public void ComposeDecimal_WholeNumber_FallsBackToCardinal()
            => Assert.AreEqual("pět", composer.ComposeDecimal(5m));

        #endregion

        #region Chybové stavy

        /// <summary>
        /// Záporné číslo nelze vypsat a chyba to musí říct hned.
        /// </summary>
        [TestMethod]
        public void Compose_NegativeValue_Throws()
            => Assert.ThrowsException<ArgumentOutOfRangeException>(() => composer.Compose(-1, Case.Nominative));

        /// <summary>
        /// Chybějící řadová složka selže hlasitě, místo aby si tvar vymyslela.
        /// </summary>
        [TestMethod]
        public void ComposeOrdinal_MissingComponent_ThrowsNamingTheValue()
        {
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => composer.ComposeOrdinal(2000, Case.Nominative));

            StringAssert.Contains(exception.Message, "2000");
        }

        #endregion
    }
}
