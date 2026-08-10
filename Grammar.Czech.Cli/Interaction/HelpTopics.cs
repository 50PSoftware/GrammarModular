namespace Grammar.Czech.Cli.Interaction
{
    /// <summary>
    /// Holds the explanatory half of the help — what the terms in the review actually mean.
    /// </summary>
    /// <remarks>
    /// The short help lists the syntax of a correction, which is useful to whoever already knows what
    /// they want to correct. This is the other reader: the tool talks in functors, in dané and nové and
    /// in diatheses, and someone who has not read the Functional Generative Description has nowhere else
    /// to find out what those are. A line saying <c>role podle FGD: ACT, PAT, ADDR, …</c> is help only
    /// for the person who does not need it.
    /// <para>
    /// Kept apart from the short help so that neither has to be read to get at the other. A topic is
    /// asked for by name — <c>? role</c> — and the short help lists the names.
    /// </para>
    /// </remarks>
    public static class HelpTopics
    {
        /// <summary>
        /// The topics, keyed by the name they are asked for under.
        /// </summary>
        public static IReadOnlyDictionary<string, string> All { get; } = new Dictionary<string, string>
        {
            ["role"] = Roles,
            ["cleneni"] = Status,
            ["pad"] = Cases,
            ["ramec"] = Frames,
            ["odhad"] = Guesses,
        };

        /// <summary>
        /// Finds the topic asked for.
        /// </summary>
        /// <param name="name">The topic name, as typed.</param>
        /// <returns>The topic text, or <see langword="null"/> when there is no such topic.</returns>
        public static string? Find(string name) =>
            All.TryGetValue(Terms.Plain(name), out var text) ? text : null;

        /// <summary>
        /// Lists the topic names, for the short help to point at.
        /// </summary>
        public static string Names => string.Join(", ", All.Keys);

        private const string Roles = """

            ROLE — čím je slovo ve větě

            Role neříká pád ani pozici, ale co to slovo v ději dělá. Pád z ní teprve plyne: adresát je
            v češtině obvykle v dativu, ale u 'ptát se' je v genitivu, a to rozhoduje sloveso, ne role.
            Proto se zadává role a pád se dopočítá — kdo zadá rovnou pád, obejde tím rámec slovesa.

            Značky jsou z Funkčního generativního popisu, na kterém stojí Pražský závislostní korpus.

            Účastníci děje — ty váže sloveso a je jich málo:

              ACT   konatel      Kdo děj koná. 'Učitel píše dopis.'
                                 U trpného rodu zůstává konatelem, i když stojí v instrumentálu:
                                 'Dopis je psán učitelem.' — proto se role nemění s převodem na trpný.
              PAT   patiens      Koho nebo co děj zasahuje. 'Učitel píše DOPIS.'
                                 Není to totéž co předmět v akuzativu: 'mávat rukou' má patiens
                                 v instrumentálu, protože tak to sloveso váže.
              ADDR  adresát      Komu je děj určen. 'Učitel píše dopis STUDENTOVI.'
              ORIG  původ        Z čeho děj vychází. 'Postavil dům Z KAMENE.'
              EFF   výsledek     Co dějem vzniká. 'Udělal z něj ODBORNÍKA.'

            Okolnosti — ty může vzít skoro každé sloveso a bývají s předložkou:

              LOC   kde          DIR1  odkud       DIR2  kudy        DIR3  kam
              TWHEN kdy          THL   jak dlouho  MANN  jak         MEANS čím
              CAUS  proč         AIM   za účelem   BEN   pro koho    ACMP  s kým

            Zadává se: --role kniha=PAT, nebo v sezení '3 role=PAT'.

            Hvězdička u role v tabulce znamená, že ji nikdo neurčil a nástroj ji rozdal podle pořadí —
            viz '? odhad'.

            """;

        private const string Status = """

            ČLENĚNÍ — co je ve větě známé a co nové

            Čeština nemá pevný slovosled a pořadí slov v ní nese informaci: co posluchač už zná, stojí
            vpředu, a to nové na konci. Tomu se říká aktuální členění větné a je to důvod, proč se
            'Student čte knihu' a 'Knihu čte student' liší, i když mají stejná slova a stejné pády.

              dané   téma  Už je ve hře, mluvčí to nepředstavuje. Jde dopředu.
              nové   réma  To, kvůli čemu se věta říká. Jde dozadu, do jádra výpovědi.

            'Student čte knihu.'   — nové je kniha: odpovídá na otázku co čte.
            'Knihu čte student.'   — nové je student: odpovídá na otázku kdo ji čte.

            Nástroj tedy slovosled nevybírá stylem, ale počítá ho z toho, co označíš. Výchozí je
            konatel jako dané a zbytek jako nové, což dá běžnou oznamovací větu.

            Zadává se: --cleneni kniha=dane, nebo v sezení '3 cleneni=dane'.

            """;

        private const string Cases = """

            PÁD — a proč ho většinou zadávat nemusíš

            Pád členu určuje sloveso svým valenčním rámcem: 'psát' váže patiens akuzativem, 'ptát se'
            genitivem, 'mávat' instrumentálem. Nástroj ho proto bere z rámce a v tabulce takový pád
            značí '(rámec)' — je to jediný sloupec, který ve tvém zadání nestojí.

            Zadaný pád rámec přebije. Hodí se, když slovo stojí mimo rámec (okolnost s předložkou),
            nebo když slovník sloveso nevede a rámec není odkud vzít.

            Předložka pád zužuje, ale ne vždycky na jeden: 'do' je vždycky s genitivem, kdežto 'v'
            připouští lokál i akuzativ a rozdíl je 'v zahradě' proti 'v zahradu' — kde proti kam. Tam
            se nástroj ptá, protože to za tebe rozhodnout nemůže.

            Zadává se: --pad kniha=dativ. Bere i číslo: --pad kniha=3.

            """;

        private const string Frames = """

            RÁMEC — který význam slovesa se bere

            Valenční rámec je seznam argumentů jednoho významu slovesa i s pády, ve kterých stojí.
            Slovník ho vede pro každý význam zvlášť, protože různé významy váží různě: 'jít' ve významu
            pohybu bere směr, 'jít' ve významu 'jde o něco' bere úplně jiné doplnění.

            Když má sloveso víc významů a žádný není výchozí, nástroj se zeptá — vybrat význam za tebe
            si tenhle projekt zakazuje, protože z toho vzniká správně utvořená věta o něčem jiném.

            Bezpodměťová slovesa mají rámec prázdný: 'pršet' nemá konatele a věta 'Prší.' je úplná.
            Některá mají vedle toho druhý význam, který podmět bere — 'mrzne' proti 'voda mrzne' —
            a mezi nimi se přepíná právě rámcem.

            Zadává se: --ramec freeze, nebo v sezení 'p ramec=freeze'.

            """;

        private const string Guesses = """

            ODHAD — co si nástroj domýšlí a jak to poznáš

            Slovník má hesel jen část češtiny a nikdy nebude mít celou. Co v něm není, nástroj odhadne,
            ale nikdy to nezamlčí. Ve výpisu se to pozná takhle:

              sloupec 'zdroj'    slovník  — vzor a rod jsou z hesláře, tedy tak dobré jako slovník
                                 pravidla — uzavřená třída (zájmena, předložky), nic se neodhadovalo
                                 odhad    — rod a vzor jsou odvozené ze zakončení lemmatu
                                 zadáno   — řekl jsi to ty

              hvězdička u role   Roli nikdo neurčil a sloveso nemá ve slovníku rámec, tak ji nástroj
                                 rozdal podle pořadí: první jméno konatel, druhé patiens, třetí
                                 životné adresát. Pád k ní patří a je odhadnutý taky.

            Odhad pořadím nezná význam slovesa, takže 'pes běhat zahrada' z něj vyjde jako 'Pes běhá
            zahradu' — 'zahrada' je ve skutečnosti místo. Opraví se to rolí: --role zahrada=LOC
            a předložkou --predlozka zahrada=v.

            Doplněná diakritika se hlásí taky: kdo napíše 'ucitel', dostane ve větě 'učitel', a nástroj
            to řekne, protože ve větě je slovo, které nikdo nenapsal.

            """;
    }
}
