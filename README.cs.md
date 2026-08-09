# Grammar.Czech

![Status](https://img.shields.io/badge/status-active%20development-orange)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-Proprietary-red)

[English](README.md) | **Čeština**

**Generativní morfologická knihovna pro češtinu na platformě .NET 8.**

Projekt generuje české slovní tvary z lemmatu, gramatických kategorií, vzoru a JSON pravidel, a nad nimi skládá věty a souvětí. Není to obecný slovník hotových tvarů. Volající musí u většiny slov dodat explicitní metadata, hlavně slovní druh, vzor, rod/číslo/pád nebo slovesné kategorie; na větné úrovni popisuje klauzi jako predikát a konstituenty s funktory, ne hotový slovosled.

## Obsah

- [Požadavky a instalace](#požadavky-a-instalace)
- [Co projekt teď umí](#co-projekt-teď-umí)
  - [Podstatná jména](#podstatná-jména)
  - [Přídavná jména](#přídavná-jména)
  - [Zájmena](#zájmena)
  - [Číslovky](#číslovky)
  - [Slovesa](#slovesa)
  - [Fonologie a pravopis](#fonologie-a-pravopis)
  - [Lexikon a valence](#lexikon-a-valence)
  - [Věty a souvětí](#věty-a-souvětí)
- [Architektura](#architektura)
- [Rychlý start](#rychlý-start)
- [Příklady](#příklady)
- [CLI](#cli)
- [Testy](#testy)
- [Datová vrstva](#datová-vrstva)
- [Známá omezení](#známá-omezení)
- [Licence](#licence)

## Požadavky a instalace

- **.NET 8 SDK** — všechny projekty cílí na `net8.0`.
- Závislosti knihovny `Grammar.Czech`: `Microsoft.Extensions.DependencyInjection.Abstractions` a `Microsoft.Extensions.Logging`.
- `Grammar.Czech.Cli` závisí na `System.CommandLine` a balí se jako .NET tool `gramatika` — viz [CLI](#cli).

Projekt se balí sám: `GeneratePackageOnBuild` je zapnutý a build vedle sestavení vytvoří `50PSoftware.GrammarModular.Czech.<verze>.nupkg`. Na nuget.org balíček není — bere se z privátního nebo lokálního feedu, případně projektovou referencí:

```bash
dotnet build Grammar.sln
```

```bash
dotnet add reference ../Grammar/Grammar.Czech/Grammar.Czech.csproj
```

Verze je pořád `-preview`, takže package reference musí povolit předběžné verze.

Pravidlová data jsou vložená jako embedded resources přímo v `Grammar.Czech`, takže se vedle sestavení nekopírují žádné soubory s pravidly. Výjimkou je lexikon, který dodává nasazení — viz [Lexikon a valence](#lexikon-a-valence).

## Co projekt teď umí

### Podstatná jména

`Grammar.Czech` umí skloňovat podstatná jména podle vzorů uložených v `Grammar.Czech/Data/Rules/Nouns/patterns.json`.

Podporované vzory:

| Rod / skupina | Vzory |
|---|---|
| mužský životný | `pán`, `muž`, `předseda`, `soudce` |
| mužský neživotný | `hrad`, `les`, `stroj` |
| ženský | `žena`, `růže`, `píseň`, `kost` |
| střední | `město`, `moře`, `kuře`, `stavení` |

Vzory mohou dědit koncovky přes `inheritsFrom`; například `les` dědí z `hrad` a přepisuje jen odlišné pády. Nepravidelnosti jsou v `Grammar.Czech/Data/Rules/Nouns/irregulars.json` (18 lemmat, například *oko*, *dům*, *ruka*, *noha*, *ucho*). Soubor `Grammar.Czech/Data/Rules/Nouns/propers.json` je prázdný — mechanismus pro vlastní jména existuje, data v něm nejsou žádná.

### Přídavná jména

Podporované jsou vzory `mladý`, `jarní`, `otcův` a `matčin` z `Grammar.Czech/Data/Rules/Adjectives/patterns.json`.

`CzechAdjectiveDeclensionService` umí:

- skloňování podle rodu, čísla, pádu a animátnosti,
- odhad vzoru pomocí `GuessAdjectivePattern`,
- komparativ a superlativ přes `Degree`,
- supletivní komparativy pro `dobrý`, `malý`, `velký`, `zlý`, `špatný` a `dlouhý`.

### Zájmena

Zájmena se čtou z `Grammar.Czech/Data/Rules/Pronouns/patterns.json` a paradigmata z `Grammar.Czech/Data/Rules/Pronouns/paradigms.json`.

Data pokrývají osobní, přivlastňovací, zvratná, ukazovací, tázací, vztažná, záporná a neurčitá zájmena. Service podporuje pevné tabulkové tvary, paradigmata, nesklonná zájmena a vybrané zájmenné tvary delegované na adjektivní skloňování.

Volitelně se rozlišuje varianta po předložce přes `CzechWordRequest.IsAfterPreposition`.

### Číslovky

Číslovky se čtou z `Grammar.Czech/Data/Rules/Numerals/patterns.json` a paradigmata z `Grammar.Czech/Data/Rules/Numerals/paradigms.json`. Data pokrývají všech devět druhů — základní, řadové, druhové, souborové, úhrnné, násobné, dílové, skupinové a neurčité napříč druhy. Podílné číslovky (*po dvou*) jsou konstrukce, ne lexikální položky, a skládá je `CzechNumeralComposer`.

`CzechNumeralService` vybírá strategii podle `NumeralMorphology`: sdílená paradigmata (*jeden*, *dva*, *oba*, *tři*, *čtyři*), pravidlo pro dvoutvarové paradigma 5–99, delegaci na adjektivní i substantivní skloňování a nesklonné položky. Dublety (*tří/třech*, *tisíc/tisíců*) i duálové tvary párových částí těla (*třema rukama*) se vybírají přes `NumeralFormOptions`.

`CzechNumeralComposer` vypíše číslo slovy se skloňováním všech částí — `365` v instrumentálu dá *třemi sty šedesáti pěti* — a zvládá nepravidelné násobky sta (*dvě stě*, *tři sta*, *pět set*). Složené číslovky 21–99 mají všechny tři varianty z IJP id=792 přes `CompoundVariant`: *dvacet jedna žáků* (výchozí), *dvacet jeden žák* a spřežku *jedenadvacet*. `ComposeOfType` pojmenuje hodnotu číslovkou kteréhokoli druhu — z `5` udělá *pět*, *pátý*, *paterý*, *patery*, *patero* i *pětkrát*.

Kongruenci počítaného předmětu nese `CardinalAgreement` a uplatňuje ji `CzechSentenceBuilder`: *pět studentů bylo* proti *tři studenti byli*. Je to jediné místo, kde shoda míří od přívlastku k řídícímu jménu, a ne naopak.

Spřežky mají i řadové číslovky: `ComposeOrdinal` s `CompoundVariant.Contracted` dá *pětadvacátý* a *stopadesátý*, a kde spřežka neexistuje, vrátí nestažený tvar místo chyby.

Necelá čísla se vypisují slovy taky — `ComposeFraction` udělá ze `3/4` *tři čtvrtiny* a z `5/8` *pět osmin*, `ComposeDecimal` z `3,14` *tři celé čtrnáct setin*. Slovo *celá* i jmenovatel jsou počítaná jména, takže se řídí číslovkou před sebou: *jedna celá*, ale *pět celých*.

Číslovku lze zadat i číslicemi. Nesklonná zůstane, ale kongruenci si odvodí z hodnoty — a desetinné číslo se řídí zlomkem, takže bere genitiv singuláru: *1,5 metru*, *14,25 sekundy*.

Číslovka nemusí být jen přívlastkem; jako řídící člen konstituentu se normálně skloňuje a nic neřídí (*Pět bylo.*).

Zápis číslovek číslicemi kontroluje `ICzechNumeralOrthographyService` — odmítne *5tý*, *10ti* i *20-krát* a umí je opravit.

### Příslovce

NESČ řadí příslovce mezi neohebné slovní druhy, takže první stupeň je samo lemma a stupňování je celá jejich morfologie. Neregistrované příslovce proto v prvním stupni projde beze změny — aby se dalo použít ve větě, nemusí být v datech.

Komparativ se ale z `Grammar.Czech/Data/Rules/adverbs.json` čte, neodvozuje. Příslovce od přídavných jmen berou tři různé přípony — `-o`, `-e/-ě` a `-y` (*nízko*, *krásně*, *česky*) — volba mezi nimi není plně předvídatelná a jedno přídavné jméno může dát dvě příslovce, která se stupňují jinak (*dlouho* a *dlouze*, *vysoko* a *vysoce*). Samo stupňování je u frekventovaných příslovcí nepravidelné (*dobře → lépe*, *špatně → hůře*, *brzy → dříve*) a pravidelná přípona `-eji/-ěji` se u některých kmenů potkává s měkčením (*hladce → hladčeji*), u jiných ne (*hustě → hustěji*). Pravidlo by se mýlilo tiše a často.

Superlativ je `nej-` na komparativu. Několik nepravidelných nese dubletu, jejíž kratší člen je ten hovorovější (*hůř* vedle *hůře*, *dřív* vedle *dříve*); vybírá se přes `CzechWordRequest.PrefersShortForm`.

Neregistrovanému příslovci se komparativ odvodí: `-ěji` po `d`, `t`, `n` a retnicích, jinde `-eji`, s měkčením, které přinášejí `-ce`, `-ky` a `-ho`. Proti 99 komparativům v datech pravidlo reprodukuje každý pravidelný a míjí jen ty nepravidelné, které jako nepravidelné uvádí příručka ÚJČ — obojí se dělí bez překryvu, a právě proto je odvozování bezpečné. Test to měří, místo aby to předpokládal, takže se pravidlo a data nemůžou nepozorovaně rozejít. Registrovaný komparativ vždycky vyhrává a příslovce registrované bez něj se bere jako nestupňované, ne jako důvod k odvození.

Přídavné jméno, ze kterého příslovce pochází, se ze stejného důvodu zaznamenává, nepočítá, a `ICzechAdverbService.GetAdverbsFor` mapování čte zpátky — kde jedno přídavné jméno dá dvě příslovce, vrátí obě. Vztažná příslovce (*kde*, *kdy*, *kam*, *jak*) uvozují vztažnou větu přes `RelativeAttachment.Relativizer`, tedy pole, které se dřív jmenovalo `Pronoun`; protože jsou neohebná, neberou pád a nic se přes ně s řídícím členem neshoduje. Záporná příslovce (*nikdy*, *nikde*, *nijak*) jsou samostatná lemmata, ne kladná s předponou.

Jinak utvořený komparativ není totéž co zkrácený tvar a data je drží zvlášť: *snadno* má *snáze* i *snadněji*, *hluboko* má *hlouběji* i *hloub*. Generuje se ten primární, zbytek ukáže `ICzechAdverbService.GetComparativeVariants`. Jedno přídavné jméno taky může dát dvě příslovce, která jsou samostatná lemmata sdílející komparativ — *dlouho* a *dlouze*, *vysoko* a *vysoce*, *těžko* a *těžce*, *široko* a *široce*, *úzko* a *úzce* — a registrovaná jsou obě.

### Slovesa

Slovesa se generují z pravidel v:

- `Grammar.Czech/Data/Rules/Verbs/patterns.json` — obecné třídy `trida1` až `trida5`, u kterých se kmeny odvozují z infinitivu, plus pojmenovaný vzor `dojme`,
- `Grammar.Czech/Data/Rules/Verbs/irregulars.json` — 37 položek s explicitně zapsanými kmeny. Nejsou to jen nepravidelná slovesa jako `být`, `mít`, `chtít`, `moci` a `vědět`; leží tu i klasické vzory `nese`, `bere`, `maže`, `peče`, `umře`, `tiskne`, `mine`, `kryje`, `kupuje`, `prosí` a `dělá`.

`CzechVerbConjugationService` umí generovat základní tvary pro indikativ, kondicionál, imperativ, minulý čas, přítomný/budoucí čas a pasivní participium. `CzechWordFormComposer` nad tím skládá některé slovesné fráze: opisné futurum u imperfektiv, pasivum s pomocným slovesem, kondicionál, negaci a reflexivní `se`/`si`.

Slovesný vzor se předává přes `Pattern` — buď třídou (`trida1`–`trida5`), nebo jménem vzoru z `irregulars.json`. Alternativně lze někdy předat `VerbClass`, která se namapuje na `trida1` až `trida5`. `GuessVerbClass` umí jednoduchou heuristiku podle infinitivní koncovky, ale není spolehlivá pro všechna česká slovesa.

### Fonologie a pravopis

Projekt obsahuje fonologickou vrstvu pro změkčení, epentezi, jotaci a kvantitu samohlásek. Rozhodování je oddělené do evaluátorů a transformace provádí `CzechPhonologyService` a `CzechOrthographyService`.

Mezi veřejně používané části patří:

- `IPhonemeRegistry` / `CzechPhonemeRegistry`,
- `ISofteningRuleEvaluator<CzechWordRequest>`,
- `IEpenthesisRuleEvaluator<CzechWordRequest>`,
- `IAlternationRuleEvaluator<CzechWordRequest>`,
- `IJotationRuleEvaluator<CzechWordRequest>`,
- `ISyncretismRuleEvaluator<CzechWordRequest>`,
- `ICzechOrthographyService`.

`CzechAlternationRuleEvaluator` krátí kmen před koncovkou genitivu plurálu. Jestli slovo krátí, je věc lexikální, ne fonologická — *kráva* dává *krav*, ale *káva* dává *káv* — takže rozhoduje `has_genitive_plural_shortening` na hesle, a `HasGenitivePluralShortening` na requestu ho přebije.

Registr fonémů pak odmítne, co krátit nejde, ať si heslo říká co chce, takže špatně vyplněný řádek nevyrobí neexistující tvar. Krátí se jen *á*, *í* a *ou*; *é*, *ó*, *ý* a *ú/ů* si délku drží (*sféra* → *sfér*, *móda* → *mód*, *rýha* → *rýh*). Krácení taky nenastane, stojí-li za dlouhou samohláskou souhláskový shluk: *brázda* → *brázd*. Veto počítá fonémy, ne písmena — proto *moucha* → *much* projde, protože *ch* je jeden foném, kdežto *zd* dva.

### Lexikon a valence

`SqliteValencyProvider` čte `Grammar.Czech/Data/Lexicon/grammar.czech.lexicon.db`, databázi SQLite. Je to jediný zdroj dat, který tu není embedded JSON — právě on má růst do tisíců hesel, zatímco pravidlové soubory v `Data/Rules/` popisují uzavřené třídy a zůstávají, jak jsou.

Slovník se edituje centrálně, v MySQL nebo MariaDB za PHP administrací, a tenhle soubor je jeho lokální kopie určená jen ke čtení. Identifikátory přiděluje server a kopie je přebírá beze změny — přečíslovaná kopie by se už nedala porovnat se serverem, ze kterého vznikla.

Slovník **není součástí NuGet balíčku**, a je to záměr: heslo přidané na serveru není důvod vydávat knihovnu a konzument na to nemá čekat. Balíček veze kód, data dodává nasazení — a může je vyměnit, aniž by cokoli přestavovalo.

Konzument řekne, kde svou kopii má, jedním ze tří způsobů, které se zkoušejí v tomhle pořadí:

```csharp
services.AddCzechGrammarServices(@"D:\data\grammar.czech.lexicon.db");   // přímo
```

```
GRAMMAR_CZECH_LEXICON=/srv/grammar/grammar.czech.lexicon.db               // prostředí
```

…nebo prostě položí `grammar.czech.lexicon.db` vedle aplikace, kam se provider dívá nakonec. Když není ani jedno, spadne to při startu s hláškou, která jmenuje všechny tři — místo aby se to tvářilo jako prázdný slovník.

Tím, že jdou zvlášť, se můžou rozejít, takže `SqliteValencyProvider` při otevření souboru přečte `schema_version` a lexikon psaný pro schéma, které neumí, odmítne. Ta kontrola patří do knihovny, ne jen do validátoru v nástroji — konzument balíčku má knihovnu, nástroj ne.

Databáze má tři vrstvy, oddělené proto, že jedno lemma má právě jednu morfologickou identitu, kdežto lexém může mít víc významů a každý význam rámec pro každou diatezi:

- `lemma_entry` — morfologická identita jednoho slovníkového tvaru: rod, vzor, životnost, pohybné `e`, krácení a vkladné `e` v genitivu plurálu, nesklonnost, pomnožnost, počitatelnost, slovesná třída, vid a jeho protějšek, reflexivní typ a sloupce o původu záznamu,
- `lexeme` a `lexical_unit` — abstraktní slovo a jeho významy. Vidová dvojice je jeden lexém, takže `dát` a `dávat` sdílejí jeden rámec, místo aby si každé neslo vlastní kopii,
- `valency_frame`, `valency_slot`, `slot_realization` — samotné rámce. Slot může mít víc realizací s preferencí pro generování, což je to, co dovolí jednomu slotu být v jednom vyjádření holým pádem a v jiném vedlejší větou nebo infinitivem.

Slot se může vyjádřit vedlejší větou a `slot_realization.clause_type` nese, která spojka ji uvozuje — `že`, `aby`, `zda` — jako lemma samotné, tak jak to zapisuje VALLEX. Slovo nese víc než druh věty: *ví, že přijde* a *ví, zda přijde* jsou obě obsahové a znamenají každá něco jiného. Žádný `CHECK` to nepohlídá, protože spojky žijí v embedded JSON pravidlech; hlídá to `lexikon validate`.

Konstrukce s kategoriálním slovesem je přísudek, jehož význam sedí ve jméně, zatímco sloveso přidává málo víc než čas — a `construction` ji vede proto, že její valence není slovesa. *Mít* řídí akuzativ a nic dalšího, jenže *mít zájem* řídí i *o* s akuzativem; přečteno přes rámec possess zůstane *o knihu* nezařazené. Sloty jsou v `template_json` v témž tvaru, jaký mají `valency_slot` a `slot_realization`, takže se konstrukce čte jako rámec, kterým se stává; jméno je jedním z nich, pod `CPHR`, tak jak jmennou část takového přísudku značí Pražský závislostní korpus.

```csharp
var constructions = provider.GetRequiredService<ICzechConstructionService>();

constructions.Find("mít", ["student", "zájem", "kniha"]);   // LVC.mít.zájem
constructions.Find("mít", ["student", "kniha"]);            // null
```

Poznává se dvojice, takže konstrukce nepřeteče do běžných užití téhož slovesa: *Student má zájem o knihu* a *Student má knihu* se staví z různých rámců. Naseedované jsou tři vzorce — `mít zájem o`, `dávat pozor na`, `mít strach z` — a doplnit inventář je práce s korpusem, ne po paměti.

Schéma v `Grammar.Czech.Lexicon.Tool/Schema/schema.sql` je záměrně přenositelné SQL — SQLite je první backend, ne poslední, a MySQL, Microsoft SQL i Firebird mají vzít stejné DDL. Vše specifické pro SQLite je v `schema.sqlite.sql`; `schema.mysql.sql` je varianta pro server.

O soubor se stará `Grammar.Czech.Lexicon.Tool`:

| příkaz | co dělá |
|---|---|
| `pull --url <api>` | stáhne slovník z API a nahradí jím lokální kopii |
| `validate` | ohlásí, co rozbil špatný řádek — rámec bez konatele, slot, který se nemůže vyjádřit, dva rámce jednoho slovesa označené jako výchozí, `lemma_key`, který žádné vyhledání netrefí |
| `build` | vytvoří lexikon ze schématu a seedu, pro práci bez serveru |
| `dump --out <sql>` | vypíše databázi jako přenositelné `INSERT`y k revizi |
| `export-json --out <adresář>` | vypíše stejný JSON, jaký posílá API, pro naplnění serveru |

```bash
dotnet run --project Grammar.Czech.Lexicon.Tool -- pull --url https://example.com/api/ --token <token>
```

`Grammar.Czech.Lexicon.Tool` se balí jako .NET tool, takže se dá nainstalovat jednou a volat odkudkoli:

```bash
dotnet tool install -g 50PSoftware.GrammarModular.LexiconTool --prerelease
lexikon pull
```

Nastavení se bere ze tří míst v tomhle pořadí: argument, `lexikon.json` v pracovním adresáři nebo kterémkoli nadřazeném, a prostředí. Každé odpovídá na jinou otázku — argument je to, co chceš teď, soubor je to, co chce projekt vždycky a patří do gitu, prostředí je to, co ví tenhle stroj. Tím se řeší i token: do souboru napiš adresu a cíl, token nech v `LEXICON_API_TOKEN`, a klíč, který v souboru není, propadne dál místo aby ho přebil zástupnou hodnotou. Vzor je v `lexikon.json.example`.

Relativní `database` v souboru se počítá od toho souboru, ne od místa, kde nástroj spustíš — hledá se totiž i v nadřazených adresářích právě proto, aby šel používat odkudkoli pod ním.

Balíček nástroje slovník taky neobsahuje. Nástroj je to, co ho stáhne.

Pull staví novou databázi do dočasného souboru a přesune ji na místo, teprve když projde `validate` — neúspěšné nebo přerušené stažení funkční lexikon nesáhne.

#### Formát na drátě

`Grammar.Czech.Lexicon.Tool/Php/api/index.php` vrací na jeden požadavek jednu stránku jedné tabulky:

```json
{"table":"lemma_entry","columns":["lemma_entry_id","lemma",…],"rows":[[1,"student",…]],"next_after":"5000"}
```

Tři rozhodnutí v tom stojí za vysvětlení, protože ke každému existuje tišší varianta, která vypadá stejně dobře:

- **Tvar tabulek, ne zanoření.** Identifikátory přiděluje server a musí cestu přežít; dokument, který by zanořoval sloty do rámců a rámce do lemmat, by je buď stejně opakoval, nebo by je importér musel vymýšlet.
- **Řádky jako pole, jména sloupců jednou.** Při stotisíci heslech je opakování dvaceti čtyř klíčů na řádek většina objemu. Ta jediná hlavička je zároveň kontrakt: importér odmítne stránku, jejíž sloupce nejsou ty očekávané ve stejném pořadí — což je to, co zabrání prohozenému sloupci naimportovat se čistě na špatné místo.
- **Stránkování podle klíče, ne podle offsetu.** Offset se posune, když někdo slovník edituje uprostřed stahování, a tiše přeskočí nebo zopakuje řádky. Klíč se na obou stranách porovnává ve vlastním typu, takže index primárního klíče zůstane použitelný.

Stránkované stahování ani tak není konzistentní snímek — nic nebrání editaci mezi dvěma stránkami — a `validate` je to, co výsledek zachytí: jako rozbitý odkaz, ne jako slovo, které se za půl roku nenaskloňuje.

#### Doplňování požadavku z lexikonu

U slova, které slovník zná, nemusíš zadat nic než lemma. `CzechLexiconEnricher` běží v `MorphologyEngine` ještě před rozřazením a doplní, co požadavek neřekl — slovní druh, rod, vzor, životnost, hláskové příznaky, slovesnou třídu, vid, reflexivitu.

Před rozřazením proto, že slovní druh je jedna z věcí, které doplňuje, a zároveň to, podle čeho se vybírá služba. `WordCategory` je tedy nullable: výchozí hodnota enumu je vždycky některý jeho člen a tady to byl `Noun`, takže požadavek, který slovní druh neuvedl, nebyl mezera, ale tvrzení — `dát` skončilo u skloňování, správně si ze slovníku doplnilo vzor `trida5` a spadlo na *Noun pattern 'trida5' not found*.

Zapisuje jen tam, kde je v požadavku `null`, takže zadaný vzor vyhraje i proti slovníku a `HasMobileE = false` zůstane false, místo aby ho přebil záznam. Proto jsou ty příznaky nullable: `false` je „volající říká, že slovo pohyblivé -e nemá", `null` je „volající to neřekl", a mezera je jen to druhé. Slovo, které slovník nezná, projde beze změny a skloní se z toho, co dodal volající — což je běžný případ, ne okrajový: většina češtiny ve slovníku není a nebude.

Záznam se použije jen tehdy, když jeho slovní druh odpovídá tomu, na co se ptáš. `GetEntry` bere lemma bez kategorie, takže u lemmatu zavedeného pod dvěma slovními druhy vrátí ten řádek, na který narazí; doplnit požadavek o sloveso *stát* z řádku pro *stát* jako zemi by ho nedoplnilo, ale odpovědělo na něco jiného.

Lexikon slouží hlavně jako provider metadat pro vybrané resolvery, není to úplný český slovník.

Valenční rámec říká, jak se realizují argumenty daného slovesa, a `CzechSentenceBuilder` z něj bere pád i předložku: u `vidět` je `PAT` akuzativ, u `dávat` je `ADDR` dativ a `PAT` akuzativ, u `jít` je `DIR3` předložka `do` s genitivem. Pád zadaný explicitně zůstává — rámec doplňuje mezery.

Sloveso s víc významy se vybírá přes `CzechClause.FrameLabel`, protože `jít` má jiné argumenty jako pohyb a jiné jako proces. Když je jeden význam ve slovníku označený jako výchozí, dostane ho volání bez labelu — `dát` je transfer, dokud volající neřekne konzumace. Když výchozí nemá žádný, jako `jít` a tři významy slovesa `být`, volání skončí výjimkou místo výběru: dvojznačnost smí rozhodnout slovník, ne kód.

Vnitřní participanty (`ACT`, `PAT`, `ADDR`, `ORIG`, `EFF` — aktanty FGP) může licencovat jen rámec, takže `vidět` s adresátem skončí výjimkou. Volná doplnění se pojí s kterýmkoli slovesem a pád si u nich zadává volající.

Funktory se řídí čtením FGP, ne intuicí, což se projeví na dvou místech, která vypadají na `COMPL` a nejsou to: infinitiv řízený modálním slovesem je jeho `PAT` (*chce jít*, *může přijít*) a totéž platí pro neslovesnou část přísudku u spony, jak to má tektogramatický manuál PDT. `COMPL` je doplněk s dvojí závislostí — nepovinný infinitiv u `pomoci`, kde nese ten, komu se pomáhá, takže kontrola míří na `PAT`, ne na `ACT`.

Které sloveso může stát v opisném pasivu, se čte z rámce. Význam, který má vlastní trpný rámec, je licencovaný tím, že ho má; u ostatních je podmínkou konatel a ještě jeden aktant, který by pasivum mohlo povýšit na podmět. Infinitiv jím není — patiens slovesa `moci` je infinitiv, který řídí, a *\*je mohnut jít* není věta — a není jím ani sponový přísudek, který se odmítá podle `kind` rámce, protože jeho patiens chodí v nominativu i instrumentálu a ani jeden pád ho neprozradí.

### Věty a souvětí

`CzechSentenceBuilder` skládá z klauzí povrchovou větu. Klauze (`CzechClause`) je predikát plus konstituenty (`ClauseElement`) s funktorem a komunikačním statusem; **nenese slovosled** — ten se odvozuje.

Builder řeší:

- **shodu** predikátu s aktorem v nominativu,
- **aktuální členění** — kontrastivní dopředu, téma před sloveso, réma za něj,
- **Wackernagelovu pozici** klitického klastru: pomocné sloveso, reflexivum, krátký dativ, krátký akuzativ, usazené za první konstituent klauze. Klastr se stěhuje celý a jde za **první** konstituent, ne za všechny předslovesné (*Klára se večer učí*),
- **stažené tvary** `ses`, `sis` a kondicionálové `by ses`, `by sis`,
- **frázové konstituenty** — přívlastek dědí od řídícího slova rod, číslo, pád a životnost všude, kde je nechal nevyplněné,
- **předložkové fráze** včetně vokalizace a kontroly rekce; celá fráze je jeden konstituent,
- **souvětí** — `Coordination` a `Subordination` nad `SentenceNode`, libovolně vnořitelné, s interpunkcí podle spojky,
- **vztažné věty** — zájmeno se shoduje s řídícím jménem v rodě, čísle a životnosti, pád si bere ze své role ve vedlejší větě. Vztažná věta je plnohodnotná věta: může souřadit i nést vedlejší větu. Jedno zájmeno je podmětem všeho, co s ním souřadí, takže shoda s řídícím jménem dojde do každého konjunktu — *žena, která přišla a odešla* — a zastaví se na podřadicí spojce, která otevírá klauzi s vlastním podmětem.

Podřadicí spojka a vztažné zájmeno obsazují první pozici své klauze, takže za nimi jde klastr: *protože se student učil*, *muž, kterého jsem viděl*. Souřadicí spojka stojí mimo klauzi a první pozici jí nechává.

Spojky jsou uzavřená třída v `Grammar.Czech/Data/Rules/conjunctions.json`; neznámá spojka skončí výjimkou, protože na jejím druhu závisí čárka i pozice klitika. Každé heslo navíc nese vztah, který vyjadřuje, ve skupinách podle NESČ — *slučovací*, *odporovací*, *stupňovací*, *vylučovací*, *příčinné*, *důsledkové* u souřadicích a *časové*, *příčinné*, *přípustkové*, *podmínkové*, *účelové*, *obsahové*, *srovnávací* u podřadicích — a druhý člen tam, kde je spojka párová (*buď – nebo*, *ani – ani*, *nejen – ale i*, *jak – tak*, *sice – ale*, *jednak – jednak*). Dvojité spojky existují jen mezi souřadicími.

Některé spojky jsou dvě slova pod jedním pravopisem a čtení se liší přímo druhem: *ať* uvozuje obsahovou větu v *Řekni mu, ať přijde* a souřadně spojuje v rozštěpeném *ať už přijde osobně, nebo vyšle zástupce*; *jak* souřadně spojuje v párovém *jak Petr, tak Pavel* a podřadně uvozuje srovnání v *udělej to, jak jsem řekl*; *než* je časové v *než přišel* a srovnávací ve *vyšší, než jsem čekal*. Heslo v souboru je primární čtení a ostatní na něm visí v `alsoReads`. Holý dotaz vrátí primární; volající, který ví, jakou konstrukci staví, si řekne o čtení podle druhu — a přesně to builder dělá: souřadné souvětí se ptá na souřadicí čtení, podřadné na podřadicí, a ani jedno nehádá.

`Coordination.Paired` staví rozštěpenou konstrukci: spojka otevírá první větu a její druhý člen spojuje každou další. Před druhým členem se čárka píše vždy, i tam, kde by ji samotné slovo nemělo — ÚJČ říká, že se píše před druhým spojovacím výrazem bez ohledu na to, co by spojka dělala sama, takže párové *nebo* a *ani* stojí proti svému bezčárkovému běžnému užití. Ptáme se na to, místo abychom to odvozovali, protože totéž slovo slouží oběma konstrukcím: *Petr ani Pavel nepřišli* spojuje prostým *ani*, *Ani nepřišel, ani nezavolal* ho štěpí.

`aby` a `kdyby` se neukládají jako paradigmata. NESČ je analyzuje jako spojky obsahující kondicionálové pomocné sloveso a shodující se přes ně s podmětem, takže dvanáct tvarů se skládá ze kmene a z částic, které už jsou v `clitics.json`: *abych*, *abys*, *aby*, *abychom*, *abyste* a řada *kdyby* vedle nich. Builder bere osobu z vedlejší věty a kondicionálovou částici v ní potlačí — pomocné sloveso se do spojky přesunulo, nezdvojilo. Skládání místo ukládání zdarma vylučuje i rozšířené \*aby jsi a \*aby jste: kondicionál žádné *jsi* nemá.

`však` se klade až za první větný člen a za klitický shluk, kdežto *avšak* zůstává na začátku klauze. Jak daleko za ním, tam zdroje končí: NESČ řadí *však* mezi *nestálá klitika*, ne mezi *klitika tantum*, takže v povinném shluku nemá pořadí — a žádný test netvrdí, že opačné pořadí je špatně.

`requiresComma` je default, ne fakt o slově. U *a*, *i*, *ani*, *nebo* a *či* plyne čárka z poměru mezi větami, ne ze spojky, takže skutečná odpověď je `Coordination.RequiresComma` od volajícího.

### Částice

Slovní druh *částice* v `Grammar.Czech/Data/Rules/particles.json` — ne klitika, která jsou v `clitics.json`. Částice je neohebná a nestupňuje se, takže není co tvořit: lemma *je* tvar a služba odpovídá na to, co částice dělá a kde smí stát.

NESČ nemá jednu ustálenou klasifikaci, ale tři. Typy tady sledují funkčně-sémantickou, kterou přisuzuje Nekulovi v PMČ — *modální*, *intenzifikační*, *vytýkací*, *modifikační*, *odpověďové*, *negační*, *přací* — doplněnou o *strukturující* a *emocionální* ze schématu MČ 2 pro to, co první nepokrývá.

Jedno pravidlo z toho plyne: modifikační částice nemůže stát v rématu, což NESČ říká o celé skupině. Který konstituent je réma, říká `ClauseElement.Status`, takže to builder kontroluje, ne jen dokumentuje. Co přací částice žádá po predikátu, zapsané záměrně **není** — *ať přijde* je prostý 3. os. prézens, čeština žádný imperativ 3. osoby nemá, a zdroj pro tu skupinu žádnou rekci způsobu neuvádí.

Větná částice (*ať*, *kéž*, *nechť*, *nuže*) obsazuje první pozici své klauze jako podřadicí spojka, takže za ní jde shluk: *Ať se student dělá*. Částice s dosahem na jeden člen jde na ten člen a otevírá ho zvenčí předložky: *jen pro studenta*.

Homonymie s příslovci a spojkami se čeká, není to vada dat. Hranice se kreslí funkcí v kontextu — *klidně* je částice v *Klidně seď* a příslovce v *Seď klidně, nevrť se* — takže *ať* stojí v tomhle souboru i v `conjunctions.json` a *bohužel* v tomhle i v `adverbs.json`. Je na to test, aby to někdo neuklidil jako duplicitu.

### Citoslovce

Devětašedesát lemmat v `Grammar.Czech/Data/Rules/interjections.json` ve čtyřech typech podle NESČ — *emocionální*, *kontaktová*, *apelová*, *zvukomalebná* — s čárou, kterou zdroj napříč nimi vede: první tři jsou subjektivní, čtvrtý objektivní. Žádná morfologie; citoslovce je neohebné a tvoří podle toho popisu nejprimitivnější typ věty.

Interpunkce je pravidlo, ne data. Citoslovce se odděluje čárkou kromě případu, kdy zastupuje větný člen, takže totéž slovo se píše obojím způsobem — *Kamarádi, hurá, vyhráli jsme* proti *Palicí buch ho po hlavě* — a `ICzechInterjectionService.RequiresComma` proto bere užití, ne jen slovo. Po slovech zapsané je to, které citoslovce vůbec může být přísudkem, protože to z typu neplyne: *hop* je *apelové* a přísudkové zároveň. Ta také nesou sloveso, které tvoří (*žbluňk → žbluňknout*), což NESČ zmiňuje jako jejich přímý vstup do slovotvorby.

Třída je otevřenější než všechny ostatní — zvukomalba se tvoří ad hoc — takže neregistrované lemma projde místo hlášení, přesný opak uzavřeného inventáře spojek. Reduplikace je zapsaná jen tam, kde ji zdroj jmenuje (*oj oj oj*, *ťuk(y) ťuk* jako volitelnou, *bubu* jako povinnou); neoznačené znamená nezaznamenané, ne „neopakuje se".

`CzechClause.Interjection` ho klade mimo klauzi za jeho čárku a první pozici nechává být. Koncové znaménko zůstává na volajícím: emocionální citoslovce k vykřičníku tíhne, ale tíhnutí není pravidlo.

## Architektura

```text
Grammar.sln
|-- Grammar.Core/               jazykově nezávislé enumy, rozhraní a modely
|-- Grammar.Czech/              česká implementace: servisy, providery, embedded JSON pravidla a databáze lexikonu
|-- Grammar.Czech.Cli/          klientská aplikace `gramatika`: z lemmat poskládá větu
|-- Grammar.Czech.Lexicon.Tool/ stahuje, staví, kontroluje a vypisuje databázi lexikonu; drží schémata a PHP API
`-- Grammar.Czech.Test/         MSTest testy pro skloňování, časování, fonologii a stavbu vět
```

Hlavní registrace pro DI je `AddCzechGrammarServices()` v `Grammar.Czech/CzechGrammarServiceFactory.cs`.

Stavba věty jde šesti stupni; každý je samostatná služba a každý se dá testovat zvlášť:

```text
SentencePlan                  co se má říct: predikát, participanti, komunikační záměr
   |  CzechRoleResolver       který participant hraje kterou roli, když to volající neřekl
   v
SentencePlan
   |  CzechSentencePlanner    význam slovesa, podmět, jeho vypuštění, co je dané
   v
CzechClause
   |  CzechClausePlanner      je slot slovo, infinitiv, nebo vedlejší věta?
   v
CzechClause / Subordination
   |  CzechMicroplanner       rámec řídí pád, číslovka ho přepíše, přísudek se shodne
   v
PlannedClause
   |  CzechWordOrderResolver  aktuální členění, pak Wackernagelova druhá pozice
   v
slova
   |  CzechSentenceBuilder    spojí klauze, napíše čárky, uzavře větu
   v
věta
```

Řez vede tam, kde se mění, co který stupeň smí měnit. Nad `PlannedClause` se ještě rozhoduje o slovech, pod ním už jen o jejich pořadí — a právě to dovoluje českému slovosledu se volně měnit, aniž by se s ním hnul jediný tvar. `CzechSentenceBuilder` zůstává vstupním bodem pro hotovou klauzi a drží si rekurzi, protože klauze může obsahovat větu: vztažná věta visí na konstituentu.

`CzechRoleResolver` je samostatný stupeň, a ne součást plánovače, ze stejného důvodu: je to jediné místo, které hádá. Plánovač role dostává a participanta bez role odmítne, takže všechno odvozené zůstává tam, kde se to dá prohlédnout a přepsat dřív, než se z toho stane věta.

### Od plánu k větě

```csharp
var roles = provider.GetRequiredService<CzechRoleResolver>();
var planner = provider.GetRequiredService<CzechSentencePlanner>();

var plan = new SentencePlan
{
    Predicate = new CzechWordRequest { Lemma = "dávat", Pattern = "trida5", WordCategory = WordCategory.Verb },
    Participants = [Student, Woman, Book],   // bez zadaných funktorů
};

Console.WriteLine(builder.Build(planner.Plan(roles.Resolve(plan))));
// Student dává ženě knihu.
```

Role plynou z rámce: konatel a adresát berou přednostně životné jméno, což je to, co udrží dva předměty slovesa dávání od sebe, aniž by je někdo pojmenoval. Participant, kterého nic nevysvětlí, si nechá prázdný funktor a vrátí se z `CzechRoleResolver.Unresolved` — špatná role dá dobře utvořenou větu o něčem jiném.

Dvě rozhodnutí, která pod plánovačem udělat nejde:

```csharp
// Vypuštění podmětu: osobu nese koncovka, takže zájmeno je důraz, ne nepříznaková věta. Shoda,
// kterou zájmeno neslo, se při tom přesune na přísudek.
planner.Plan(plan with { Participants = [Me, Book] });                       // Čtu knihu.
planner.Plan(plan with { Participants = [Me, Book], AllowSubjectDrop = false }); // Já čtu knihu.

// Perspektiva: chtít patiens jako podmět znamená chtít pasivum, což je vlastní rámec — konatel
// klesne do instrumentálu — a zároveň z patientu udělá téma, protože pasivum, které by nechalo
// konatele vepředu, by proti aktivu nezískalo nic.
planner.Plan(plan with { Perspective = FgdFunctor.PAT });
// Kniha je dávána studentem ženě.
```

Hlavní vstupy:

- `CzechSentenceBuilder` pro větu nebo souvětí z klauzí,
- `CzechWordFormComposer` pro plný tvar slova nebo slovesné fráze,
- `MorphologyEngine` pro přímé směrování podle slovního druhu — všech deseti. Substantiva, adjektiva, zájmena, číslovky, příslovce a základní slovesné tvary se tvoří; předložky, spojky, částice a citoslovce se vracejí jako lemma, což je celá jejich morfologie, ne záslepka. Co ty čtyři ve větě *dělají*, odpovídají jejich vlastní služby, na které se builder ptá přímo. Právě na něj se rozpadnou `IInflectionService<CzechWordRequest>` i `IVerbInflectionService<CzechWordRequest>`, protože je jediná implementace, která bere požadavek kteréhokoli slovního druhu,
- specializované servisy jako `CzechNounDeclensionService`, `CzechAdjectiveDeclensionService`, `CzechPronounService`, `CzechNumeralService` a `CzechVerbConjugationService`.

Vedle nich se registrují i podpůrné služby, které jde brát z kontejneru přímo:

| Služba | K čemu je |
|---|---|
| `ICzechPrepositionService` | rekce a vokalizace předložek |
| `ICzechConjunctionService` | druh spojky a pravidlo čárky |
| `ICzechValencyService` | vyhledání valenčního rámce slovesa |
| `ICzechNumeralOrthographyService` | kontrola a oprava zápisu číslovek číslicemi |
| `CzechNumeralComposer` | číslo vypsané slovy, včetně zlomků a desetinných čísel |
| `CzechAuxiliaryVerbService`, `CzechVerbPhraseBuilderService` | pomocná slovesa a složené slovesné tvary |
| `INegationService<CzechWordRequest>` | negace |
| `ICzechParticleService`, `ICzechPrefixService` | částice a předpony |
| `CzechWordStructureResolver` | rozklad lemmatu na předponu a kmeny |

## Rychlý start

```csharp
using Grammar.Core.Enums;
using Grammar.Czech;
using Grammar.Czech.Models;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCzechGrammarServices();

var provider = services.BuildServiceProvider(
    new ServiceProviderOptions { ValidateOnBuild = true });

var composer = provider.GetRequiredService<CzechWordFormComposer>();

var request = new CzechWordRequest
{
    Lemma = "student",
    WordCategory = WordCategory.Noun,
    Gender = Gender.Masculine,
    Pattern = "pán",
    IsAnimate = true,
    Number = Number.Singular,
    Case = Case.Genitive,
};

var form = composer.GetFullForm(request);
Console.WriteLine(form.Form); // studenta
```

Příklad slovesa:

```csharp
var request = new CzechWordRequest
{
    Lemma = "dělat",
    WordCategory = WordCategory.Verb,
    Aspect = VerbAspect.Imperfective,
    Pattern = "trida5",
    Tense = Tense.Present,
    Number = Number.Singular,
    Person = Person.First,
    Modus = Modus.Indicative,
    Voice = Voice.Active,
};

var form = composer.GetFullForm(request);
Console.WriteLine(form.Form); // dělám
```

## Příklady

Všechny výstupy níž jsou ověřené proti aktuálnímu kódu.

### Věta z klauze

Klauze nese predikát a konstituenty; slovosled si builder odvodí sám z komunikačního statusu. Reflexivum se usadí na druhou pozici, za první konstituent.

```csharp
using Grammar.Czech.Enums;
using Grammar.Czech.Models.Syntax;

var builder = provider.GetRequiredService<CzechSentenceBuilder>();

var predicate = new CzechWordRequest
{
    Lemma = "učit",
    Pattern = "trida4",
    WordCategory = WordCategory.Verb,
    Modus = Modus.Indicative,
    Tense = Tense.Present,
    Aspect = VerbAspect.Imperfective,
    Voice = Voice.Active,
    Person = Person.Third,
    Number = Number.Singular,
    Gender = Gender.Feminine,
    ReflexiveType = ReflexiveType.DerivedReflexive_Se,
};

var subject = ClauseElement.Of(
    new CzechWordRequest
    {
        Lemma = "studentka",
        Pattern = "žena",
        WordCategory = WordCategory.Noun,
        Gender = Gender.Feminine,
        Number = Number.Singular,
        Case = Case.Nominative,
    },
    FgdFunctor.ACT,
    InformationStatus.Given);

var time = ClauseElement.Of(
    new CzechWordRequest
    {
        Lemma = "večer",
        Pattern = "hrad",
        WordCategory = WordCategory.Noun,
        Gender = Gender.Masculine,
        Number = Number.Singular,
        Case = Case.Accusative,
        IsAnimate = false,
    },
    FgdFunctor.TWHEN);

Console.WriteLine(builder.Build(new CzechClause
{
    Predicate = predicate,
    Elements = [subject, time],
}));
// Studentka se učí večer.
```

Předložková fráze je jeden konstituent; předložka se vokalizuje podle následujícího slova a rekce se kontroluje.

```csharp
var atSchool = ClauseElement.Of(
    "v",
    new CzechWordRequest
    {
        Lemma = "škola",
        Pattern = "žena",
        WordCategory = WordCategory.Noun,
        Gender = Gender.Feminine,
        Number = Number.Singular,
        Case = Case.Locative,
    },
    FgdFunctor.LOC);

Console.WriteLine(builder.Build(new CzechClause
{
    Predicate = predicate,
    Elements = [subject, atSchool],
}));
// Studentka se učí ve škole.
```

### Souvětí

Podřadicí spojka obsazuje první pozici vedlejší věty, takže klitikum jde za ni. Souřadicí spojka stojí mimo klauzi, a tak první pozici nechává prvnímu konstituentu.

```csharp
var mainClause = new CzechClause { Predicate = works, Elements = [student] };
var subClause = new CzechClause { Predicate = studied };

Console.WriteLine(builder.Build(new Subordination(mainClause, "protože", subClause)));
// Student dělal, protože se učil.

Console.WriteLine(builder.Build(new Coordination("ale", [mainClause, subClause])));
// Student dělal, ale učil se.
```

### Slot obsazený propozicí

Některá slovesa berou celý děj tam, kde jiná berou věc. Jakým tvarem to vyjde, není na volajícím — má to zapsané rámec, takže stejné zavolání dá u jednoho slovesa infinitiv a u druhého vedlejší větu.

```csharp
var reading = new CzechClause { Predicate = Verb("číst") };

Console.WriteLine(builder.Build(new CzechClause
{
    Predicate = Verb("chtít"),
    Elements = [student, ClauseElement.Of(reading, FgdFunctor.PAT)]
}));
// Student chce číst.

Console.WriteLine(builder.Build(new CzechClause
{
    Predicate = Verb("vědět"),
    Elements = [student, ClauseElement.Of(reading, FgdFunctor.PAT)]
}));
// Student ví, že čte.
```

Infinitiv nemá vlastní podmět — na povrchu je vyloučený — takže rámec nese, se kterým participantem je koreferenční: kdo chce, ten jde. Zadej jiný podmět a věta se odmítne, protože čeština pro ni infinitiv nemá a ta vazba je vedlejší věta s *aby*.

Jeho klitika se šplhají do věty řídící, kde je ten jediný klastr klauze:

```csharp
Console.WriteLine(builder.Build(new CzechClause
{
    Predicate = Verb("chtít"),
    Elements = [student, ClauseElement.Of(
        new CzechClause { Predicate = Verb("učit") with { ReflexiveType = ReflexiveType.ReflexivumTantum_Se } },
        FgdFunctor.PAT)]
}));
// Student se chce učit.
```

Vedlejší věta si drží čas, který dostala. Čeština nezná souslednost časovou, takže *věděl, že čte* zůstává v přítomném čase — posunout ho dozadu by hlásilo něco jiného.

### Číslovky slovy

```csharp
var numerals = provider.GetRequiredService<CzechNumeralComposer>();

numerals.Compose(365, Case.Instrumental);                    // třemi sty šedesáti pěti
numerals.Compose(21, Case.Nominative, CompoundVariant.Contracted); // jedenadvacet
numerals.ComposeOrdinal(25, Case.Nominative, CompoundVariant.Contracted,
                        Gender.Masculine, isAnimate: true);  // pětadvacátý
numerals.ComposeFraction(3, 4);                              // tři čtvrtiny
numerals.ComposeDecimal(3.14m);                              // tři celé čtrnáct setin
numerals.ComposeOfType(5, NumeralType.Ordinal, Case.Nominative,
                       Gender.Masculine, isAnimate: true);   // pátý
numerals.ComposeOfType(5, NumeralType.Multiplicative, Case.Nominative); // pětkrát
numerals.ComposeDistributive(2);                             // po dvou
```

Kontrola zápisu číslicemi:

```csharp
var orthography = provider.GetRequiredService<ICzechNumeralOrthographyService>();

orthography.IsValid("5tý", out var reason); // false, reason vysvětluje proč
orthography.Normalize("5tý");               // 5.
```

Kongruence počítaného předmětu se propíše až na predikát:

```csharp
var five = new CzechWordRequest { Lemma = "pět", WordCategory = WordCategory.Numerale };

var countedSubject = ClauseElement.Of(
    new CzechWordRequest
    {
        Lemma = "student",
        Pattern = "pán",
        WordCategory = WordCategory.Noun,
        Gender = Gender.Masculine,
        IsAnimate = true,
        Case = Case.Nominative,
    },
    [five],
    FgdFunctor.ACT);

// predikát být, 3. os. sg., minulý čas
Console.WriteLine(builder.Build(new CzechClause
{
    Predicate = wasPredicate,
    Elements = [countedSubject],
}));
// Bylo pět studentů.
```

### Valence

Konstituent bez pádu si ho vezme z valenčního rámce predikátu — u `vidět` je `PAT` akuzativ:

```csharp
var patient = ClauseElement.Of(
    new CzechWordRequest
    {
        Lemma = "student",
        Pattern = "pán",
        WordCategory = WordCategory.Noun,
        Gender = Gender.Masculine,
        IsAnimate = true,
        Number = Number.Singular,
        // Case nezadán — doplní ho rámec
    },
    FgdFunctor.PAT);

Console.WriteLine(builder.Build(new CzechClause
{
    Predicate = sees,
    Elements = [subject, patient],
}));
// Studentka vidí studenta.
```

Sloveso s víc významy, mezi nimiž není žádný výchozí, si o výběr řekne výjimkou; význam se pak zadá přes `FrameLabel`:

```csharp
new CzechClause
{
    Predicate = goes,          // jít — významy motion a process, výchozí ani jeden
    Elements = [subject, toSchool],
    FrameLabel = "motion",
};
```

`být` je týž případ se třemi: `copula_nominal` pro *lev je králem zvířat*, `copula_adjectival` pro *Petr je veselý*, `existence` pro *je tam problém*. Každý má jiný `ValencyKind` a rámec je jeden na význam a diatezi, takže si jeden sdílet nemohou. `dát` je ten druhý případ — `transfer` je označený jako výchozí, takže ho věta bez labelu dostane.

### Přídavné jméno se stupňováním

```csharp
var adjectives = provider.GetRequiredService<CzechAdjectiveDeclensionService>();

adjectives.GetForm(new CzechWordRequest
{
    Lemma = "dobrý",
    WordCategory = WordCategory.Adjective,
    Pattern = "mladý",
    Gender = Gender.Masculine,
    IsAnimate = true,
    Number = Number.Singular,
    Case = Case.Nominative,
    Degree = Degree.Comparative,
}).Form; // lepší
```

## CLI

`Grammar.Czech.Cli` je klientská aplikace `gramatika`. Zadají se jí lemmata a ona z nich poskládá větu: sloveso se stane přísudkem, valenční rámec ze slovníku rozdá zbytku role a pády, a co ze slovníku nevyplyne, to se odhadne ze zakončení.

```bash
dotnet run --project Grammar.Czech.Cli -- veta student číst kniha
```

Než něco vypíše, ukáže, jak si zadání vyložila, a nechá to opravit — proto je to potvrzovací dialog a ne jednorázový příkaz. Špatně přiřazená role dá dobře utvořenou větu o něčem jiném, a to je horší než otázka.

```text
Přísudek  dávat — nedokonavý, přítomný čas, oznamovací způsob, činný rod, 3. os. jednotné č.
Rámec     transfer (ACT, PAT, ADDR, DIR3)

  #  slovo  role            členění  pád                tvar   zdroj
  1  Klára  ACT (konatel)   dané     nominativ (rámec)  Klára  odhad
  3  žena   ADDR (adresát)  nové     dativ (rámec)      ženě   slovník
  4  kniha  PAT (patiens)   nové     akuzativ (rámec)   knihu  slovník
Pozn.: Slovník nezná: Klára. Vzor a rod jsou odhadnuté ze zakončení.

Věta: Klára dává ženě knihu.

[Enter] potvrdit · 1 role=ADDR · p cas=minuly · ? nápověda · q konec
>
```

Sloupec `zdroj` je tam kvůli rozdílu mezi odpovědí a odhadem: vzor ze slovníku platí jako slovník, `pravidla` je uzavřená třída (zájmena, předložky) a `odhad` je návrh nástroje odvozený ze zakončení. Pád označený `(rámec)` v requestu nestojí — doplní ho až builder ze slovesa, a proto zmizí ve chvíli, kdy se pád zadá natvrdo.

Role ani pády si nástroj nevymýšlí sám: volá `CzechRoleResolver` a `CzechSentencePlanner`, tedy tentýž kód, jaký dostane konzument knihovny. Přehled ukazuje výsledek plánovače, ne druhý odhad vedle něj.

Podmětové zájmeno nástroj proti knihovně nechává stát — vypsat míň slov, než dostal, by vypadalo, že se jedno ztratilo. `--vypustit-podmet` zapne nepříznakovou češtinu:

```bash
gramatika veta já číst kniha                     # Já čtu knihu.
gramatika veta já číst kniha --vypustit-podmet   # Čtu knihu.
```

Na každou otázku dialogu existuje přepínač, který ji zodpoví dopředu; obojí zapisuje do téhož místa, takže sezení jde přepsat na jeden příkaz. To je zároveň to, co dělá z nástroje něco použitelného ve skriptu, kde se není koho ptát: `--bez-dotazu` udělá z otevřené otázky chybu, která pojmenuje přepínač, jímž se řeší, a `--json` k větě přidá i rozbor.

```bash
gramatika veta Klára dávat žena kniha --role kniha=PAT --cas minulý --bez-dotazu
gramatika veta student jít --ramec motion --json
```

Slovník se s balíčkem nástroje nerozdává, stejně jako s balíčkem knihovny. Cesta k němu se bere z `--slovnik`, z `GRAMMAR_CZECH_LEXICON`, nebo z adresáře aplikace; když není nikde, řekne to nástroj rovnou při startu a poradí `lexikon pull`.

Spojka ve vstupu rozdělí větu na klauze a sama říká, jak se spojují — pravidla vědí, které jsou souřadicí a které podřadicí:

```bash
gramatika veta student cist kniha a zak psat dopis        # Student čte knihu a žák píše dopis.
gramatika veta student cist kniha protoze zak psat dopis # Student čte knihu, protože žák píše dopis.
```

Klauze visí na té bezprostředně předchozí, jak to čte i člověk: v *čte, protože píše a zpívá* patří zpívání dovnitř toho *protože*. `--pripojit` řekne jinak — a protože na připojení závisí, co spojka řídí, mění to větu, ne jen obrázek o ní:

```bash
gramatika veta student cist kniha aby zak psat dopis a lekar zpivat pisen
# Student čte knihu, aby žák psal dopis a lékař zpíval píseň.

gramatika veta student cist kniha aby zak psat dopis a lekar zpivat pisen --pripojit 3=1
# Student čte knihu, aby žák psal dopis a lékař zpívá píseň.
```

Přehled ukazuje, na čem která klauze visí, a přijme `k 3=1`.

Přepínač přísudku mluví za celé souvětí, dokud nepojmenuje klauzi — a klauze, která řekne jinak, vyhraje:

```bash
gramatika veta student cist kniha a zak psat dopis --cas minuly
# Student četl knihu a žák psal dopis.

gramatika veta student cist kniha a zak psat dopis --cas minuly --cas 2=pritomny
# Student četl knihu a žák píše dopis.
```

V dialogu totéž jako `p cas=minulý` a `p2 cas=přítomný`. Číslo před rovnítkem znamená u každé rodiny přepínačů něco jiného — u `--pad` a `--role` slovo, u `--cas` a `--zpusob` klauzi — a není co rozlišovat, protože přísudek žádné vlastní pořadí, na které by šlo ukázat, nemá. Pořadová čísla slov běží přes celý zadaný seznam, takže `--pad dopis=genitiv` i `7 pad=genitiv` ukazují na totéž slovo, ať skončilo v kterékoli klauzi.

## Testy

```bash
dotnet test Grammar.Czech.Test
```

Testy jsou v MSTest a pokrývají substantiva, adjektiva, zájmena, číslovky, slovesa, vybrané fonologické evaluátory/služby, stavbu vět a souvětí, a načítání všech JSON providerů včetně referenční integrity mezi soubory.

Lexikon má vlastní sadu: průchod databáze přes JSON a zpátky, který ověří, že drátový formát unese celý slovník beze ztráty, stránkovací smyčku proti podstrčenému transportu, a porovnání schémat — přenositelné DDL proti variantě pro MySQL a mapa sloupců v C# proti té v PHP. Ty poslední existují proto, že kontrola v importéru se spustí až při stahování proti běžícímu serveru, což je nejhorší chvíle na zjištění, že se dva ručně udržované seznamy rozešly.

## Datová vrstva

Pravidlová data v projektu `Grammar.Czech` jsou embedded JSON resources. Výjimkou je lexikon: ten je databáze SQLite, protože jako jediný má růst do tisíců hesel a edituje se centrálně.

| Cesta | Obsah |
|---|---|
| `Data/Rules/Nouns/patterns.json` | substantivní vzory (15) |
| `Data/Rules/Nouns/irregulars.json` | nepravidelná substantiva (18) |
| `Data/Rules/Nouns/propers.json` | vlastní jména — zatím prázdné |
| `Data/Rules/Adjectives/patterns.json` | adjektivní vzory (4) |
| `Data/Rules/Pronouns/patterns.json` | data zájmen |
| `Data/Rules/Pronouns/paradigms.json` | zájmenná paradigmata |
| `Data/Rules/Numerals/patterns.json` | data číslovek |
| `Data/Rules/Numerals/paradigms.json` | paradigmata číslovek |
| `Data/Rules/Verbs/patterns.json` | obecné slovesné třídy `trida1`–`trida5` a vzor `dojme` |
| `Data/Rules/Verbs/irregulars.json` | nepravidelná slovesa a pojmenované vzory s explicitními kmeny (37) |
| `Data/Rules/prefixes.json` | prefixy |
| `Data/Rules/clitics.json` | kondicionálové částice, minulá pomocná slovesa, reflexiva |
| `Data/Rules/prepositions.json` | předložky, jejich rekce a vokalizace |
| `Data/Rules/conjunctions.json` | spojky, jejich druh, vztah, párovost a pravidlo čárky |
| `Data/Rules/particles.json` | částice a jejich funkce |
| `Data/Rules/interjections.json` | citoslovce, jejich druh a přísudkové užití |
| `Data/Lexicon/grammar.czech.lexicon.db` | lexikální metadata a valenční rámce (`dát`/`dávat`, `jít`, `vidět`) — SQLite, ne JSON |

## Známá omezení

### Data, ne mechanismus

- Lexikon není úplný slovník češtiny; `ResolveGenderAndPattern` a `ResolveVerbAspect` fungují jen pro lemmata, která databáze obsahuje.
- Lexikon obsahuje rámce pro třicet lexémů — čtyřicet šest slovesných lemmat, počítá-li se každý člen vidové dvojice — z dvou set šedesáti čtyř hesel. Mechanismus je hotový, data ne: u slovesa bez rámce si pády zadává volající jako dřív.
- Krácení v genitivu plurálu je jen kvantitativní a vlajku nese osm lemmat. Typ *í* → *ě* (*míra* → *měr*, *díra* → *děr*) je jiná alternace, o které `has_genitive_plural_shortening` nic neříká; taková slova potřebují `lemma_entry.stem`, který kód čte, ale který zatím žádné heslo ve slovníku nevyplňuje.
- Slot realizovaný infinitivem nebo obsahovou větou skládá `CzechClausePlanner`, ale jen jeden na klauzi: sloveso, které jich řídí víc najednou, se odmítne, místo aby se poskládalo.
- Zvratný infinitiv dává svoje se/si do klastru věty řídící, což je u jednoho správně — *chce se učit* — a u dvou se odmítne, protože klastr je v klauzi jeden a *se* v něm nemůže být dvakrát.
- `CzechNumeralComposer.ComposeOrdinal` a `ComposeOfType` skládají jen z lemmat ve slovníku; hodnota vyžadující chybějící složku (např. *dvoutisící*) selže s výjimkou, místo aby si tvar vymyslela.

### Co modelované není

- Klauze spojené přes `SentencePlan.Joined` se vnořují, jak hluboko se napíšou, a řetěz na jedné úrovni (`[a: B, protože: C]`) je jiná věta než řetěz vnoření (`a: B { protože: C }`) — vyslovit jde obojí. Vztažná věta je `SentenceNode` a souřadí i podřaďuje jako každá jiná, jenže její klauze se zadávají jako `CzechClause`, ne jako plány — uvnitř ní se tedy role neodvozují a rámec nevybírá.
- `IValencyProvider.GetEntry` bere lemma, volitelně s `WordCategory`, takže homonyma napříč kategoriemi rozlišit umí, ale homonyma uvnitř jedné ne. Schéma je nese ve sloupci `homonym_index` a provider vrátí to s nejnižším.
- Klitický klastr nezná volný dativ (*To ti byla legrace*), který podle NESČ stojí mezi pomocným slovesem a reflexivem. Ostatní pozice pořadí odpovídají.
- Ukazovací zájmeno před číslovkou (*těch pět studentů*) se shoduje s hlavou fráze, ne s celou frází.
- Že se vnitřní participant pojí se slovesem nejvýš jednou, se nevynucuje — dva `PAT` konstituenty v jedné klauzi nic nezastaví.
- Aktuální členění se promítá jen do slovosledu. NESČ ho nese i intonací a dvě čtení lišící se prozodií považuje za dvě různé věty; to modelované není.

### Kde rozhoduje úzus a vybralo se jedno čtení

- Vokalizace předložek není podle IJP ustálený jev a rozhoduje úzus. Pravidla pokrývají uváděné tendence, zbytek je výčtem v `vocalizeBefore`.
- Čárka u `nebo` a `či` závisí na poměru vět, ne na spojce. Data nesou jen běžnější čtení; vylučovací poměr se musí říct přes `Coordination.RequiresComma`.
- Číslovky generují skloňovanou úhrnnou číslovku; ustrnulou variantu (*bez patero ponožek*), kterou IJP id=792 uvádí vedle ní jako rovněž spisovnou, si vyžádat nejde.
- U vzoru `sto` se generuje skloněná varianta s genitivem (*ke stu korun*); nesklonná se shodou (*ke sto korunám*), kterou IJP uvádí vedle ní, vyjádřit nejde.

### Jak se API používá

- Volající často musí dodat `Pattern`, `Gender`, `Number`, `Case`, `Person`, `Tense`, `Aspect`, `Modus` a `Voice`; projekt zatím není analyzátor přirozeného textu.
- `MorphologyEngine.GetForm` vrací jedno slovo, takže u slovesa dá jen základní tvar. Slovesné tvary o víc slovech — opisné futurum, pasivum s pomocným slovesem, kondicionál, negace, reflexivum — potřebují `CzechWordFormComposer.GetFullForm`.
- Pojmenovaný vzor z `irregulars.json` nese kmeny doslova, takže sedí na sloveso samotného vzoru a na jeho předponové odvozeniny — `nese` pokrývá *nést* i *odnést*, `dělá` pokrývá *dělat* i *dodělat*. Nepříbuzné sloveso potřebuje třídní vzor: *prodávat* se vzorem `dělá` vrátí *dělá*, s `trida5` správné *prodává*.
- CLI je demo, ne uživatelský nástroj pro obecné dotazování.

### Slovníkový provoz

- Databáze je binární, takže git neukáže, co se v ní změnilo. `dump` vyrobí čitelnou textovou podobu; napojení na commitovací postup hotové není.
- Pull stahuje pokaždé celý slovník. Přírůstková synchronizace neexistuje a vyžadovala by na serveru sledování změn a náhrobní záznamy — smazané řádky by přírůstkový pull jinak neviděl. Přepis celého souboru je řeší zadarmo, proto se začíná tam.

## Licence

Copyright (c) 50PSoftware. Všechna práva vyhrazena.
