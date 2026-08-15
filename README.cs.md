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
  - [Příslovce](#příslovce)
  - [Slovesa](#slovesa)
  - [Fonologie a pravopis](#fonologie-a-pravopis)
  - [Lexikon a valence](#lexikon-a-valence)
  - [Věty a souvětí](#věty-a-souvětí)
  - [Částice](#částice)
  - [Citoslovce](#citoslovce)
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

Vzory mohou dědit koncovky přes `inheritsFrom`; například `les` dědí z `hrad` a přepisuje jen odlišné pády. Nepravidelnosti jsou v `Grammar.Czech/Data/Rules/Nouns/irregulars.json` (*oko*, *dům*, *ruka*, *noha*, *ucho* a další). Soubor `Grammar.Czech/Data/Rules/Nouns/propers.json` je prázdný — mechanismus pro vlastní jména existuje, data v něm nejsou žádná.

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

Některá zájmena jsou dvě slova pod jedním pravopisem a čtení se liší přímo druhem: *co* se ptá v *co čteš?* a uvozuje vztažnou větu v *člověk, co přišel*, stejně *kdo* a *jaký*. Heslo v souboru je primární čtení a ostatní na něm visí v `alsoReads`, přesně jako u spojek. `GetPronounType` vrací primární čtení a nemění se; volající, který ví, jakou konstrukci staví, se ptá `GetReadings` — a to dělá `CzechWordOrderResolver`, když vykresluje vztažnou větu.

Čtení si přitom nese vlastní `inflectionClass` a uvádí ji i tam, kde je stejná jako primární: vynechaná by neznamenala „táž“, ale `Substantive`, což je první hodnota enumu. A stejná není vždycky — dvě slova pod jedním pravopisem se můžou lišit i tím, jak se skloňují:

- **tázací *co*** se skloňuje (*co, čeho, čemu*), kdežto **vztažné *co*** je nesklonné. Svou roli ve vztažné větě nevyjadřuje tvarem, ale odkazovacím zájmenem uvnitř té věty — *člověk, co jsem ho viděl* — takže NESČ o něm mluví jako o analytickém relativizátoru a konstrukce s ním podle něj resumptivum obsahují vždycky. V nominativu je to zájmeno nulové, takže *člověk, co přišel* vyjde celé; ostatní pády jádro odmítne, protože resumptivum nemodeluje. Stylově je vztažné *co* hovorové, ne nespisovné — Havránek–Jedlička je z psaného jazyka nevylučují.
- **vztažné *kdo*** relativizuje entitu, ne vlastnost jména, takže se opírá o ukazovací zájmeno: *ten, kdo přišel*. Mezi relativizátory věty se jmennou hlavou ho NESČ nevede, a `requiresPronominalHead` je to v datech. Bez toho by *student, kdo přišel* prošlo — *kdo* má tvar pro mužský životný rod, takže by shoda nic nenamítla.
- **vztažné *jaký*** se naopak chová jako *který*: shoduje se s řídícím jménem v rodě a čísle a skloňuje se podle vzoru *mladý*.

Přivlastňovací vztažná zájmena jsou tři a nejsou to tři stejné případy: *jehož* a *jejichž* jsou podle IJP nesklonná, *jejíž* se skloňuje jako *její* podle vzoru *jarní*, tedy s příponou až za koncovkou (*jejíhož*, *jejímuž*, *jejíchž*). Shodují se dvěma směry naráz, a každý směr rozhoduje o něčem jiném: rod a číslo řídícího jména vybírají, **které ze tří slov** to je — mužský a střední rod v jednotném čísle *jehož*, ženský *jejíž*, množné číslo *jejichž* — kdežto tvar samotný se řídí **vlastněným jménem**, protože zájmeno je jeho shodný přívlastek.

Modeluje se to tak, že přívlastek je: `PlannedRelative.Possessed` pojmenuje funktorem ten participant vztažné věty, kterému zájmeno patří, a plánovač ho tomu participantu vloží mezi `Modifiers`. Odtud dál se o něj stará stejná shoda jako o *mladý* u *mladý student*, a `CzechRoleResolver` mu žádný slot nerezervuje — slot drží vlastněné jméno, které si pád bere ze své vlastní role. Celý ten člen otevírá vztažnou větu, protože ji otevírá zájmeno v něm, takže se stává tématem a klitika jdou až za ním: *žena, jejíhož studenta jsem viděl*. Volbu ze tří slov `CzechWordOrderResolver` kontroluje proti řídícímu jménu a neshodu odmítne — všechna tři jsou platná slova, takže by špatná volba prošla až na povrch jako bezvadná věta o něčem jiném.

Volitelně se rozlišuje varianta po předložce přes `CzechWordRequest.IsAfterPreposition`.

### Číslovky

Číslovky se čtou z `Grammar.Czech/Data/Rules/Numerals/patterns.json` a paradigmata z `Grammar.Czech/Data/Rules/Numerals/paradigms.json`. Data pokrývají každý druh, který `NumeralType` jmenuje — základní, řadové, druhové, souborové, úhrnné, násobné, dílové a skupinové — a k tomu neurčité číslovky, které jdou napříč druhy, místo aby tvořily vlastní. Podílné číslovky (*po dvou*) jsou konstrukce, ne lexikální položky, a skládá je `CzechNumeralComposer`.

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

Neregistrovanému příslovci se komparativ odvodí: `-ěji` po `d`, `t`, `n` a retnicích, jinde `-eji`, s měkčením, které přinášejí `-ce`, `-ky` a `-ho`. Proti komparativům v datech pravidlo reprodukuje každý pravidelný a míjí jen ty nepravidelné, které jako nepravidelné uvádí příručka ÚJČ — obojí se dělí bez překryvu, a právě proto je odvozování bezpečné. Test to měří, místo aby to předpokládal, takže se pravidlo a data nemůžou nepozorovaně rozejít. Registrovaný komparativ vždycky vyhrává a příslovce registrované bez něj se bere jako nestupňované, ne jako důvod k odvození.

Přídavné jméno, ze kterého příslovce pochází, se ze stejného důvodu zaznamenává, nepočítá, a `ICzechAdverbService.GetAdverbsFor` mapování čte zpátky — kde jedno přídavné jméno dá dvě příslovce, vrátí obě. Vztažná příslovce (*kde*, *kdy*, *kam*, *jak*) uvozují vztažnou větu přes `RelativeAttachment.Relativizer`, tedy pole, které se dřív jmenovalo `Pronoun`; protože jsou neohebná, neberou pád a nic se přes ně s řídícím členem neshoduje. Záporná příslovce (*nikdy*, *nikde*, *nijak*) jsou samostatná lemmata, ne kladná s předponou.

Jinak utvořený komparativ není totéž co zkrácený tvar a data je drží zvlášť: *snadno* má *snáze* i *snadněji*, *hluboko* má *hlouběji* i *hloub*. Generuje se ten primární, zbytek ukáže `ICzechAdverbService.GetComparativeVariants`. Jedno přídavné jméno taky může dát dvě příslovce, která jsou samostatná lemmata sdílející komparativ — *dlouho* a *dlouze*, *vysoko* a *vysoce*, *těžko* a *těžce*, *široko* a *široce*, *úzko* a *úzce* — a registrovaná jsou obě.

### Slovesa

Slovesa se generují z pravidel v:

- `Grammar.Czech/Data/Rules/Verbs/patterns.json` — obecné třídy `trida1` až `trida5`, u kterých se kmeny odvozují z infinitivu, plus pojmenovaný vzor `dojme`,
- `Grammar.Czech/Data/Rules/Verbs/irregulars.json` — položky s explicitně zapsanými kmeny. Nejsou to jen nepravidelná slovesa jako `být`, `mít`, `chtít`, `moci` a `vědět`; leží tu i klasické vzory `nese`, `bere`, `maže`, `peče`, `umře`, `tiskne`, `mine`, `kryje`, `kupuje`, `prosí` a `dělá`.

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

Slovník se edituje centrálně na serveru a tenhle soubor je jeho lokální kopie určená jen ke čtení. Identifikátory přiděluje server a kopie je přebírá beze změny — přečíslovaná kopie by se už nedala porovnat se serverem, ze kterého vznikla.

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
- `lemma_sense` — co platí o jednom hesle v jednom významu, což ani jedna z předchozích tabulek říct neumí: `lemma_entry` je jeden řádek pro slovo přes všechny významy, `lexical_unit` jeden řádek pro význam přes všechna slova lexému. Řádky jsou jen výjimky,
- `lemma_variant` — druhá spisovná podoba hesla, takže *setmět* se pozná a ven jde *setmít*,
- `valency_frame`, `valency_slot`, `slot_realization` — samotné rámce. Slot může mít víc realizací s preferencí pro generování, což je to, co dovolí jednomu slotu být v jednom vyjádření holým pádem a v jiném vedlejší větou nebo infinitivem.

Slot se může vyjádřit vedlejší větou a `slot_realization.clause_type` nese, která spojka ji uvozuje — `že`, `aby`, `zda` — jako lemma samotné, tak jak to zapisuje VALLEX. Slovo nese víc než druh věty: *ví, že přijde* a *ví, zda přijde* jsou obě obsahové a znamenají každá něco jiného. Žádný `CHECK` to nepohlídá, protože spojky žijí v embedded JSON pravidlech; hlídá to `lexikon validate`.

Konstrukce s kategoriálním slovesem je přísudek, jehož význam sedí ve jméně, zatímco sloveso přidává málo víc než čas — a `construction` ji vede proto, že její valence není slovesa. *Mít* řídí akuzativ a nic dalšího, jenže *mít zájem* řídí i *o* s akuzativem; přečteno přes rámec possess zůstane *o knihu* nezařazené. Sloty jsou v `template_json` v témž tvaru, jaký mají `valency_slot` a `slot_realization`, takže se konstrukce čte jako rámec, kterým se stává; jméno je jedním z nich, pod `CPHR`, tak jak jmennou část takového přísudku značí Pražský závislostní korpus.

```csharp
var constructions = provider.GetRequiredService<ICzechConstructionService>();

constructions.Find("mít", ["student", "zájem", "kniha"]);   // LVC.mít.zájem
constructions.Find("mít", ["student", "kniha"]);            // null
```

Poznává se dvojice, takže konstrukce nepřeteče do běžných užití téhož slovesa: *Student má zájem o knihu* a *Student má knihu* se staví z různých rámců. Naseedované vzorce jsou druhu `mít zájem o`, `dávat pozor na` a `mít strach z` a doplnit inventář je práce s korpusem, ne po paměti.

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

Stahování je stránkované a stránkování není konzistentní snímek — nic nebrání editaci mezi dvěma stránkami. `validate` je to, co výsledek zachytí: jako rozbitý odkaz, ne jako slovo, které se za půl roku nenaskloňuje.

#### Doplňování požadavku z lexikonu

U slova, které slovník zná, nemusíš zadat nic než lemma. `CzechLexiconEnricher` běží v `MorphologyEngine` ještě před rozřazením a doplní, co požadavek neřekl — slovní druh, rod, vzor, životnost, hláskové příznaky, slovesnou třídu, vid, reflexivitu.

Před rozřazením proto, že slovní druh je jedna z věcí, které doplňuje, a zároveň to, podle čeho se vybírá služba. `WordCategory` je tedy nullable: výchozí hodnota enumu je vždycky některý jeho člen a tady to byl `Noun`, takže požadavek, který slovní druh neuvedl, nebyl mezera, ale tvrzení — `dát` skončilo u skloňování, správně si ze slovníku doplnilo vzor `trida5` a spadlo na *Noun pattern 'trida5' not found*.

Zapisuje jen tam, kde je v požadavku `null`, takže zadaný vzor vyhraje i proti slovníku a `HasMobileE = false` zůstane false, místo aby ho přebil záznam. Proto jsou ty příznaky nullable: `false` je „volající říká, že slovo pohyblivé -e nemá", `null` je „volající to neřekl", a mezera je jen to druhé. Slovo, které slovník nezná, projde beze změny a skloní se z toho, co dodal volající — což je běžný případ, ne okrajový: většina češtiny ve slovníku není a nebude.

Záznam se použije jen tehdy, když jeho slovní druh odpovídá tomu, na co se ptáš — doplnit požadavek o sloveso *stát* z řádku pro *stát* jako zemi by ho nedoplnilo, ale odpovědělo na něco jiného. Když požadavek slovní druh uvádí, enricher hledá lemma rovnou v něm; když ne, ptá se jen na lemma a vezme ten řádek, na který narazí — a to je jediný případ, kdy se u lemmatu zavedeného pod dvěma slovními druhy pořád může vrátit ten nesprávný.

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

Lemmata v `Grammar.Czech/Data/Rules/interjections.json` se dělí do čtyř typů podle NESČ — *emocionální*, *kontaktová*, *apelová*, *zvukomalebná* — s čárou, kterou zdroj napříč nimi vede: první tři jsou subjektivní, čtvrtý objektivní. Žádná morfologie; citoslovce je neohebné a tvoří podle toho popisu nejprimitivnější typ věty.

Interpunkce je pravidlo, ne data. Citoslovce se odděluje čárkou kromě případu, kdy zastupuje větný člen, takže totéž slovo se píše obojím způsobem — *Kamarádi, hurá, vyhráli jsme* proti *Palicí buch ho po hlavě* — a `ICzechInterjectionService.RequiresComma` proto bere užití, ne jen slovo. Po slovech zapsané je to, které citoslovce vůbec může být přísudkem, protože to z typu neplyne: *hop* je *apelové* a přísudkové zároveň. Ta také nesou sloveso, které tvoří (*žbluňk → žbluňknout*), což NESČ zmiňuje jako jejich přímý vstup do slovotvorby.

Třída je otevřenější než všechny ostatní — zvukomalba se tvoří ad hoc — takže neregistrované lemma projde místo hlášení, přesný opak uzavřeného inventáře spojek. Reduplikace je zapsaná jen tam, kde ji zdroj jmenuje (*oj oj oj*, *ťuk(y) ťuk* jako volitelnou, *bubu* jako povinnou); neoznačené znamená nezaznamenané, ne „neopakuje se".

`CzechClause.Interjection` ho klade mimo klauzi za jeho čárku a první pozici nechává být. Koncové znaménko zůstává na volajícím: emocionální citoslovce k vykřičníku tíhne, ale tíhnutí není pravidlo.

## Architektura

```text
Grammar.sln
|-- Grammar.Core/               jazykově nezávislé enumy, rozhraní a modely
|-- Grammar.Czech/              česká implementace: servisy, providery, embedded JSON pravidla a databáze lexikonu
|-- Grammar.Czech.Cli/          klientská aplikace `gramatika`: z lemmat poskládá větu
|-- Grammar.Czech.Lexicon.Tool/ stahuje, staví, kontroluje a vypisuje databázi lexikonu; drží schémata
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

Vztažná věta je plán taky, takže se v ní nic nezadává ručně — role jejích participantů plynou z rámce jejího vlastního slovesa a může být sama souvětím:

```csharp
var subject = Student with
{
    Relative = new PlannedRelative
    {
        Relativizer = "který",
        Clause = new SentencePlan
        {
            Predicate = Verb("psát"),
            Participants = [Letter],                       // bez zadaného funktoru
            Joined = [new ClauseLink("a", Working)],
        },
    },
};
// Student, který píše dopis a pracuje, čte knihu.
```

Vztažné zájmeno drží uvnitř své věty jednu roli a mezi participanty není, takže resolver rezervuje slot, na který ukazuje jeho pád: u nominativního *který* je dopis patiens, ne konatel. Zájmeno je zároveň to, o čem ta věta je, takže se uvnitř nic nestává tématem samo od sebe — *který píše dopis*, ne *který dopis píše*.

### Věty s nevyjádřeným podmětem

Čeština vynechává podmět trojím způsobem, a jsou to tři různé věci, ne jedna:

```csharp
// Není co vyjádřit: sloveso konatele nemá vůbec.
planner.Plan(new SentencePlan { Predicate = Verb("pršet") });          // Prší.

// Vypuštěný: konatel je zájmeno, které už nese koncovka.
planner.Plan(new SentencePlan { Predicate = Verb("číst"), Participants = [Me, Book] });
// Čtu knihu.

// Nepojmenovaný: nikdo neříká kdo, a osoba stojí na slovese.
planner.Plan(new SentencePlan
{
    Predicate = Verb("psát") with { Person = Person.Third, Number = Number.Plural },
    Participants = [Letter],
});
// Dopis píšou.
```

Model je rozlišuje podle toho, co je v plánu, ne podle povrchu. První a druhá osoba na slovese je shoda s podmětem, který se nevyslovil, takže slot konatele je obsazený a jméno na něj nemůže — bez toho by z *píšu dopis* vyšel dopis jako konatel, v nominativu.

Jestli sloveso vůbec může mít podmět, je lexikální fakt, a bydlí tedy ve slovníku: rámec druhu `Impersonal` nemá žádný slot a participant k němu nabídnutý se odmítne, místo aby se vyskloňoval do *Prší student.* Sloveso, které slovník nevede, si svou dosavadní volnost drží, protože to říká rámec.

Bezpodměťovost patří významu, ne slovesu — proto je to rámec, a ne příznak na hesle. *Mrzne* a *Voda mrzne* jsou dva významy, takže `mrznout` nese dva rámce a ten o počasí je výchozí, protože to znamená holé sloveso:

```bash
gramatika veta mrznout                       # Mrzne.
gramatika veta voda mrznout --ramec freeze   # Voda mrzne.
gramatika veta voda mrznout
# Sloveso 'mrznout' je bezpodměťové — 'voda' k němu nepatří a věta s ním nevznikne.
# Jiný význam podmět bere: --ramec freeze.
```

Naseedováno: `pršet`, `sněžit`, `svítat`, `stmívat` jen jako bezpodměťová; `mrznout`, `hřmít`, `blýskat` i s druhým významem, který konatele bere; `stmívat` a `blýskat` se zvratným, které jejich význam o počasí vyžaduje. Každé z nich má dokonavý protějšek — `napršet`, `nasněžit`, `rozednít`, `setmít`, `zmrznout`, `zahřmět`, `blýsknout` — a ten stojí pod týmž lexémem a rámce dědí, místo aby nesl jejich kopii: *Napršelo*, *Zmrzlo*, *Blýsklo se*. Protějšek dědí všechny významy, takže `zmrznout` dosáhne na *Zmrzlo* i na *Voda zmrzla*.

Jaký druh děje sloveso pojmenovává, je jiná otázka než jeho vid, a bydlí v `lemma_entry.aktionsart` — způsob slovesného děje, celá dvacetišestiskupinová klasifikace z NESČ. Dvacet šest a ne dvacet pět: skupiny jsou značené českou abecedou, kde *ch* stojí mezi *h* a *i*.

Není to jemnější vid. Vid je gramatický, má dva členy a má ho každé české sloveso; tohle je lexikální a většina sloves do žádné skupiny nepatří, takže `null` znamená nezařazeno, ne „žádný". Kde sloveso zařazené je, skupina vid určuje — NESČ to říká o celém výčtu najednou, *slovesa skupin (a)–(r) jsou dok., zatímco slovesa skupin (s)–(y) jsou nedok.* — a `AktionsartFacts.RequiredAspect` je právě to pravidlo. `lexikon validate` proti němu drží každý zařazený řádek, takže nedokonavé semelfaktivum je vadný řádek, ne neobvyklé sloveso.

Nesou ho hesla tohoto druhu: `blýsknout` semelfaktivum proti frekventativu `blýskat`, `zahřmět` ingresivum, `napršet` a `nasněžit` kumulativa, `pršet`, `sněžit` a `hřmít` dekurziva, `svítat` a `stmívat` mutativa, `setmít` a `zmrznout` rezultativa, `rozednít` evolutivum.

`mrznout` na hesle nenese žádné, a to je rozhodnutí, ne mezera: *mrzne* je stav vzduchu a *voda mrzne* postupná změna vody, což jsou různé skupiny, a heslo má jeden řádek. Je to protipříklad k obvyklému popisu způsobu slovesného děje jako vlastnosti slovesa — sloveso samo jednu odpověď nemá a každé jeho čtení ano.

Na to je `lemma_sense`. Páruje jedno heslo s jedním významem a skupinu zapisuje tam; přebíjí heslo stejně, jako `valency_frame.reflexive_type` přebíjí reflexivní typ na něm, a kde řádek není, platí heslo. `mrznout` má řádky dva, stativní pro *mrzne* a mutativní pro *voda mrzne*, a vracejí se jako `ValencyFrame.Aktionsart`.

To párování je celý vtip a zkratka, která se nabízí, nefunguje: význam visí na lexému, lexém je vidová dvojice, takže skupina napsaná na `lexical_unit` by dopadla i na `zmrznout`. *Zmrzlo* je dosažený výsledek v obou čteních — což jeho heslo už říká — a dokonavé sloveso, které zdědí stativnost od *mrzne*, je prostě nepravda. Chytil to validátor: řádek drží proti vidu toho lemmatu, kterému patří.

Druhý význam `blýskat` ukazuje patiens v holém instrumentálu, což je celá třída českých sloves — *mávat rukou*, *kroutit hlavou*, *házet kamenem*: `Meč blýská očima.`

Takové sloveso nemá s čím se shodovat, takže jeho příčestí jde do středního rodu jednotného čísla: *Pršelo*, ne mužský rod, který by dalo výchozí nastavení.

Jestli *prší* vůbec má pozici podmětu, nechávají zdroje otevřené — NESČ staví tradiční *věty bezpodměté* vedle generativního čtení s nevysloveným expletivem *(Ono) prší* — a prázdný rámec ten spor nerozhoduje. Říká jen tolik, že tím podmětem nemůže být žádné slovo, které volající předá; na tom se obě čtení shodnou a generátoru to stačí.

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

Slovo, které slovník nemá, se teď rozliší od slova, které má v jiném tvaru — dřív to byl týž tichý odhad. `učitele` se jako vstup nebere, nástroj skládá věty z lemmat a češtinu nečte, ale pozná se jako tvar slova `učitel` a řekne se to, místo aby z toho bylo ženské jméno vzoru růže ve větě, která vypadá skoro dobře. Index za tím se generuje týmiž službami, které by ty tvary vyrobily do věty, takže se s nimi nemůže rozejít.

Slovo, které není tvarem ničeho známého, je opravdu nové a zapíše se. `:slova` ukáže, co se nasbíralo, `:slova doplnit` to projde a zaznamená, co člověk potvrdil:

```text
> zahradník kopat záhon
  ! 'zahradník' slovník nezná a není to ani tvar ničeho, co zná.
    Zapsal jsem ho mezi návrhy na doplnění slovníku.
```

Do slovníku zapisovat neumí a nepokouší se o to. Soubor SQLite je kopie centrální databáze, určená jen ke čtení; id přiděluje server, API umí jen číst a další `lexikon pull` lokální soubor přepíše celý — řádek vložený tady by žil do té doby a pak byl pryč, což je funkce, která tiše zahazuje vlastní výsledek. Sebraná slova tedy jdou do vlastního souboru a druhá půlka je příkaz nástroje, který slovník opravdu vlastní:

```bash
lexikon navrhy --jen-potvrzene
```

Ten seznam přečte a zapíše návrh `seed.NNN.sql`. Návrh, ne seed: id zůstávají prázdná, protože je přiděluje server, `source` je prázdný, protože na provenienci stojí licenční kázeň projektu a slovo, které se objevilo v sezení, žádnou nemá, dokud ji někdo nedohledá — a hlavička si říká o to jediné, co vygenerovat nejde: co jsi vynechal a proč.

Spuštěný bez argumentů otevře sezení, což je tvar, jaký skládání věty doopravdy má: do věty se šťouchá — jiný čas, jiné členění, jiný pád — a každé šťouchnutí byl dosud nový proces a znovu napsané slovo `veta`.

```text
gramatika — sezení. Piš lemmata a stiskni Enter.
':?' nápověda, '? role' vysvětlení pojmů, ':konec' konec.

> student číst kniha
  …
  Student čte knihu.

> p cas=minuly
  …
  Student četl knihu.
```

Řádek s rovnítkem opraví větu, která stojí; holá slova začnou novou. To je jediné pravidlo a je vyslovené, ne uhádnuté.

Co se řekne o přísudku, platí do konce sezení a použije se i na věty po něm — `:stav` ukáže, co platí, `:zapomen` to zruší. Co se řekne o *slovu*, neplatí: další věta má na těch místech jiná slova, takže přenést `3 pad=dativ` by znamenalo mluvit potichu o něčem jiném.

Sezení stojí vedle `veta`, ne místo něj. `veta` je pořád jeden příkaz, na který jde odpovědět dopředu a pustit ho ze skriptu; sezení je na to šťouchání.

Na pojmy se dá zeptat jménem, v obou režimech: `? role` vysvětlí, co jsou funktory a proč se zadává role a ne pád, `? cleneni` proč dané a nové rozhodují slovosled, a `? odhad` co si nástroj domýšlí a jak to výpis říká.

Všech deset slovních druhů dojde k hotové větě. Poslední dva byly částice a citoslovce, a ne proto, že by je bylo těžké poznat: nebyl pro ně funktor. Ani jedno není větný člen — čeština u nich mluví o slovech *bez větněčlenské platnosti* — takže jim žádný valenční rámec roli nedá, a dosavadních 25 funktorů byli samí účastníci děje nebo okolnosti. Vnutit jim některý znamenalo zapsat, že *asi* odpovídá na otázku „jak", což neodpovídá.

Pražský závislostní korpus na to funktory má a teď jsou v `FgdFunctor`: `MOD` pro modalitu, `RHEM` pro rematizátor, `ATT` pro postoj, `PREC` pro navázání na předchozí větu a `PARTL` pro slovo stojící mimo stavbu věty.

Ani jedna třída na to nepotřebuje slovník. Pravidla už částice třídí do devíti skupin Nekulovy klasifikace a korpus třídí totéž na funktory, takže srovnat je vedle sebe je pravidlo — `Modal` je `MOD`, `Focusing` je `RHEM`, `Intensifying` je `EXT`, `Response` a `Negative` jsou `PARTL`. Citoslovce je `PARTL` z toho, že je citoslovce, a není co dohledávat.

```bash
gramatika veta student číst kniha ano   # Student čte knihu ano.
gramatika veta student číst kniha ach   # Student čte knihu ach.
```

Slovo, které je zároveň příslovce i částice, se pořád čte jako příslovce, takže se `asi` zeptá na okolnost, kterou u něj slovník nevede, místo aby si ji vymyslelo. `--druh asi=castice` to rozhodne a skupina dodá `MOD`.

Poznat slovní druh není totéž co umět to slovo zasadit. Příslovce není valenční slot, takže mu žádný rámec roli nedá, a dokud slovník neuměl říct, jakou okolnost příslovce vyjadřuje, končilo každé příslovce jako otevřená otázka. Od toho je `lemma_entry.inherent_functor`:

```bash
gramatika veta student číst kniha dnes   # Student čte knihu dnes.
```

Odvodit to nejde. Zakončení neříká nic a přídavné jméno za tím příslovcem taky ne — *rychlý* a *rychle* je jedno slovo ve dvou slovních druzích a na otázku „jak“ odpovídá jen jedno z nich. Zapisuje se to tedy po slovech.

Bydlí to ve slovníku, a ne u příslovcí ve vestavěném `adverbs.json`, ze stejného důvodu jako slovesné kmeny: oprava má být edit ve slovníku, ne vydání knihovny. Ty dva soubory teď o témž slově říkají různé věci — JSON, jak se stupňuje, což je morfologie, slovník, co znamená pro větu, což je fakt o slově. Příslovce, které je v JSONu a ne ve slovníku, se chová přesně jako dřív: pozná se a roli zadá volající.

Nese ho prakticky každé příslovce, které pravidla vedou. Slušná část z nich stojí na pravidle, ne na něčím úsudku: deadjektivní příslovce odpovídá na „jak" a `adverbs.json` si pamatuje, z čeho které vzniklo, takže *pečlivý* → *pečlivě* → MANN není co rozhodovat. Zbytek je projitý po jednom, což hlavičky seedů rozepisují — nejvíc TWHEN, EXT, LOC a DIR3, pak MOD a RHEM a ocásek DIR1, DIR2, ATT, ACMP, CAUS a PREC.

Několik jich tam vědomě není: `blízko`, `dokonce`, `jak`, `naproti`, `sotva`, `tak`, `uvnitř` a `vedle`. Každé z nich je zároveň předložka nebo spojka a heslo by jim tu roli vzalo — enricher doplní slovní druh ze slovníku dřív, než se na slovo dostane rozpoznání uzavřených tříd, takže by z *vedle knihy* bylo příslovce a předložka by přestala řídit genitiv. Test hlídá obě půlky: každé ostatní příslovce funktor má a tahle heslo nemají.

Sporná rozhodnutí jsou pojmenovaná tam, kde padla. `dlouho` je TWHEN a ne THL, protože sloupec drží jednu odpověď a TWHEN je to čtení, které nelže v tom druhém; `vpravo` je LOC a ne DIR3, protože *je vpravo* proti *jdi vpravo* rozhoduje sloveso; `prakticky` je EXT podle toho, jak se používá, ne MANN podle toho, jak vypadá.

Sedm z deseti slovních druhů je uzavřených — nová předložka ani spojka nevzniká — a bydlí v pravidlech vedle slovníku, ne v něm. Slovník vede podstatná a přídavná jména a slovesa; zájmena, předložky a spojky se z pravidel četly už dřív, příslovce, částice, citoslovce a číslovky teď taky. Předtím propadly odhadu ze zakončení, který zná infinitiv a adjektivní koncovky a všechno ostatní pojmenuje podstatným jménem.

```bash
gramatika veta student číst pět kniha   # Student čte pět knih.
```

Testují se v pevném pořadí — zájmeno, předložka, spojka, číslovka, příslovce, částice, citoslovce — protože se ty třídy překrývají a něco vyhrát musí: *vedle* je předložka i příslovce, *tak* spojka i příslovce i citoslovce, *dost* příslovce i číslovka. Ty čtyři nové jdou až nakonec, takže nic, co fungovalo, nečte jinak.

Mezi těmi čtyřmi je pořadí volba, ne fakt. 49 slov je zároveň příslovce i částice — *dobře*, *jistě*, *asi*, *prý* — a vyhrává příslovce: příslovce může být větný člen a částice ne, takže kdyby se z *dobře* stala částice, vypadlo by z věty, kdežto *asi* čtené jako příslovce se chová stejně jako částice, obojí je neohebné. Rozhodnout to slovo od slova by chtělo výčet slov v kódu, a lexikální fakta patří do slovníku.

Od toho je `--druh`, a má poslední slovo nad vším výše:

```bash
gramatika veta student číst asi --druh asi=castice --role asi=MANN
```

`--stupen` pouští ven stupňování, které knihovna uměla a nástroj ho nikam nepustil: `--stupen rychle=druhy` dá *rychleji*, `--stupen dobre=treti` dá *nejlépe* — zapsaný tvar, ne odvozený, protože pravidlo by řeklo *dobřeji*. Na druhu, který se nestupňuje, nic neudělá a řekne to, místo aby to byl přepínač, co tiše nedělá nic.

Sloveso, na které slovník nemá rámec, dá větu i tak. Bez rámce nemá rozhodovač rolí co rozdávat, každý člen zůstane bez role a nevznikne nic — a protože slovník vede rámce k šedesáti slovesům, býval to běžný konec, ne výjimka. Nástroj se opře o bezpříznakové české pořadí: konatel, patiens, a po nich adresát, když je životný.

```bash
gramatika veta učitel darovat kniha student   # Učitel daruje knihu studentovi.
```

Role, které si vymyslel, jsou v přehledu značené `*` a hlásí se pod ním, a pád jde s nimi: role bez pádu nikam nevede a tam, kde chybí rámec, je obojí týž odhad. Zadané vyhrává, takže `--role zahrada=LOC` se nepřepisuje, jen se kolem něj rozdá zbytek.

Co pořadí slov vědět nemůže, je význam. *zahrada* v *pes běhat zahrada* je místo, ne patiens, a neřekne to ani pořadí, ani životnost — řeklo by to jen sloveso. Vyjde z toho *Pes běhá zahradu*, označené, a opraví se to rolí. Bydlí to v nástroji a ne v knihovně záměrně: knihovna, která by si valenci tiše vymýšlela, by lhala tomu, kdo na ní staví.

Předložka s víc rekcemi se hlásí jako otevřený pád, ne jako otevřená role, protože to tak je: *v zahradě* proti *v zahradu* je kde proti kam a roli si knihovna z předložky odvodí sama, jakmile je pád jasný.

Lemmata jde psát bez diakritiky. `ucitel` najde `učitel` a doplněný pravopis se oznámí, protože ve větě bude slovo, které nikdo nenapsal:

```bash
gramatika veta ucitel psat dopis student   # Učitel píše dopis studentovi.
```

Skládání je záloha, ne první volba: přesný zápis vyhrává vždycky a ten, který sedí na víc hesel — `být` a `byt` jsou různá slova — je dotaz, ne rozhodnutí. Skládají se i cíle přepínačů, takže slovo jde opravit kteroukoli podobou: `--pad ucitel=dativ` trefí `učitel`.

Co nástroj nebere, je celá věta v jednom argumentu. Lemmata jsou samostatné argumenty a `veta "učitel psát dopis student"` to řekne — dřív to došlo až do knihovny a vrátilo se jako *Verb pattern 'učitel psát dopis student' not found*, tedy anglická věta o skloňovacích vzorech pro někoho, kdo jen dal uvozovky na špatné místo.

Slovník se s balíčkem nástroje nerozdává, stejně jako s balíčkem knihovny. Cesta k němu se hledá v tom pořadí, jaké má nástroj `lexikon` — `--slovnik` pro tohle jedno spuštění, pak klíč `database` v `lexikon.json` pro celý projekt, pak `GRAMMAR_CZECH_LEXICON` pro tenhle stroj, nakonec adresář aplikace. Když není nikde, řekne to nástroj rovnou při startu a všechny čtyři vypíše.

`lexikon.json` je týž soubor, který čte nástroj lexikonu, takže projekt řekne jednou, kde má slovník, a najdou ho oba. Cesta se bere relativně k tomu souboru, ne k pracovnímu adresáři, a soubor se hledá směrem nahoru, takže to funguje z libovolného podadresáře. Čte se odsud jen klíč `database`; adresa a token patří nástroji, který mluví s API.

Sloveso zopakované v druhém konjunktu se vypustí: `Student čte knihu a žák dopis.` Manuál PDT (§12.1.1.1) považuje řídící sloveso za vypuštěné tam, kde je z předchozí klauze jasné, které to bylo — *(Jirka navštívil Marii.) Honza Jiřinu.* — a obnovuje ho kopií toho uzlu; `#EmpVerb` je vyhrazený gramatické elipse, kde se sloveso z kontextu obnovit nedá vůbec.

Podmínka je tu tvrdší, než jazyk vyžaduje, protože generátor si musí být jistý, ne pravděpodobný: shodnout se musí lemma, čas, způsob, slovesný rod, vid, zápor i zvratnost, oba konjunkty musí mít zbytky, které si stojí naproti, a přísudek musí vyjít jako jedno slovo. Osoba a číslo se lišit smí — nese je podmět, který zůstal, a bez toho by neprošlo ani *já piju kávu a ona čaj*. Podmínka jednoho slova drží stranou klitické pomocné sloveso: v 1. a 2. osobě minulého času nese čas i osobu a kam ho po vypuštění příčestí položit, tenhle projekt doložené nemá. `ClauseLink.AllowVerbEllipsis` to vypne pro příznakové čtení a zapnutý je ze stejného důvodu jako `AllowSubjectDrop`.

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

Vztažné slovo je předěl taky, jen jiného druhu: klauze za spojkou je sourozenec, klauze za *který* visí na členu. Poznají se stejně jako spojky, z pravidel — *který* a *jenž* jsou v `patterns.json` vedené jako vztažná zájmena, *kde* a *kdy* nesou v `adverbs.json` příznak vztažného příslovce — takže k tomu není potřeba žádný přepínač:

```bash
gramatika veta ucitel videt student ktery cist kniha    # Učitel vidí studenta, který čte knihu.
gramatika veta ucitel znat dum kde bydlet student       # Učitel zná dům, kde bydlí student.
```

Zadává se lemma, ne tvar: píše se `ktery` i tam, kde ve větě vyjde *která* nebo *kterou*. Rod, číslo a životnost si zájmeno vezme z řídícího jména. Všechno za vztažnou větou patří dovnitř ní, stejně jako se klauze připojuje k bezprostředně předchozí, takže vztažná věta smí souřadit:

```bash
gramatika veta ucitel videt student ktery cist kniha a psat dopis
# Učitel vidí studenta, který čte knihu a dopis píše.
```

Pád zájmena je jediné, co si nebere z řídícího jména — drží si roli ve své vlastní větě. Nástroj mu dá první slot, který rámec jejího slovesa nechá volný, a v přehledu ho značí `(rámec)` jako každý jiný odvozený pád. Je to odhad, ne výpočet, a proto jde přepsat:

```bash
gramatika veta ucitel videt kniha ktery student cist
# Učitel vidí knihu, která čte studenta.

gramatika veta ucitel videt kniha ktery student cist --pad ktery=akuzativ
# Učitel vidí knihu, kterou čte student.
```

Rozhodnout to za uživatele nejde: *kniha, kterou student čte* a *kniha, která čte studenta* jsou obě věty a liší se významem, ne stavbou. Vztažné příslovce pád nemá, protože je neohebné a argumentem své věty není.

Bez přepínače visí vztažná věta na posledním jméně klauze před ní — tak ji čte i člověk, protože zájmeno sahá k nejbližšímu předcházejícímu jménu. `--vztazna` řekne jinak a `--relativizator` přepne slovo, kterým se uvozuje; v dialogu je obojí `v 4=2` a `v 4=jenž`:

```bash
gramatika veta ucitel videt student ktery cist kniha --vztazna 1=1
# Učitel, který čte knihu, vidí studenta.

gramatika veta ucitel videt student ktery cist kniha --relativizator 3=jenz
# Učitel vidí studenta, jenž čte knihu.
```

Přivlastňovací vztažné zájmeno přivlastňuje jménu hned za sebou a nedrží žádný pád — ten má vlastněné jméno ze své vlastní role. Které ze tří slov se píše, rozhoduje řídící jméno, a napsat jiné je chyba, ne varianta:

```bash
gramatika veta zena psat dopis jejiz student videt ucitel --vztazna 1=1 --role student=PAT
# Žena, jejíhož studenta vidí učitel, píše dopis.

gramatika veta student psat dopis jejiz kniha videt ucitel --vztazna 1=1
# K 'student' patří 'jehož', ne 'jejíž' — které ze tří to je, rozhoduje rod a číslo řídícího jména.
```

Vztažných slov je víc než *který* a *jenž* a nechovají se stejně. *Jaký* se skloňuje jako *který*. *Co* se neskloňuje a jde jen tam, kde je podmětem — jinde by roli neslo odkazovací zájmeno, které nástroj zadat neumí. *Kdo* chce ukazovací zájmeno, ne jméno:

```bash
gramatika veta ucitel videt student co cist kniha    # Učitel vidí studenta, co čte knihu.
gramatika veta ucitel videt ten kdo cist kniha       # Učitel vidí toho, kdo čte knihu.

gramatika veta ucitel videt student kdo cist kniha
# 'kdo' se neváže na jméno 'student', ale na ukazovací zájmeno: 'ten kdo …'.
```

Tázací *který* se od vztažného rozliší pozicí: vztažné stojí za jménem, které rozvíjí, tázací před ním, takže `ktery student cist kniha` žádnou vztažnou větu neotevře. U slov, kterým je vztažné čtení až to druhé — *proč*, *odkud* jsou stejně dobře příslovce — se navíc vyžaduje sloveso za nimi, aby `student cist kniha proc` zůstalo otázkou po důvodu.

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

## Datová vrstva

Pravidlová data v projektu `Grammar.Czech` jsou embedded JSON resources. Výjimkou je lexikon: ten je databáze SQLite, protože jako jediný má růst do tisíců hesel a edituje se centrálně.

| Cesta | Obsah |
|---|---|
| `Data/Rules/Nouns/patterns.json` | substantivní vzory |
| `Data/Rules/Nouns/irregulars.json` | nepravidelná substantiva |
| `Data/Rules/Nouns/propers.json` | vlastní jména — zatím prázdné |
| `Data/Rules/Adjectives/patterns.json` | adjektivní vzory |
| `Data/Rules/Pronouns/patterns.json` | data zájmen |
| `Data/Rules/Pronouns/paradigms.json` | zájmenná paradigmata |
| `Data/Rules/Numerals/patterns.json` | data číslovek |
| `Data/Rules/Numerals/paradigms.json` | paradigmata číslovek |
| `Data/Rules/Verbs/patterns.json` | obecné slovesné třídy `trida1`–`trida5` a vzor `dojme` |
| `Data/Rules/Verbs/irregulars.json` | nepravidelná slovesa a pojmenované vzory s explicitními kmeny |
| `Data/Rules/prefixes.json` | prefixy |
| `Data/Rules/clitics.json` | kondicionálové částice, minulá pomocná slovesa, reflexiva |
| `Data/Rules/prepositions.json` | předložky, jejich rekce a vokalizace |
| `Data/Rules/conjunctions.json` | spojky, jejich druh, vztah, párovost a pravidlo čárky |
| `Data/Rules/adverbs.json` | příslovce a jejich stupňování |
| `Data/Rules/particles.json` | částice a jejich funkce |
| `Data/Rules/interjections.json` | citoslovce, jejich druh a přísudkové užití |
| `Data/Lexicon/grammar.czech.lexicon.db` | lexikální metadata a valenční rámce — SQLite, ne JSON |

## Známá omezení

### Data, ne mechanismus

- Lexikon není úplný slovník češtiny; `ResolveGenderAndPattern` a `ResolveVerbAspect` fungují jen pro lemmata, která databáze obsahuje.
- Valenční rámec nese jen zlomek hesel lexikonu. Mechanismus je hotový, data ne: u slovesa bez rámce si pády zadává volající jako dřív.
- Krácení v genitivu plurálu je jen kvantitativní a vlajku nese hrstka lemmat. Typ *í* → *ě* (*míra* → *měr*, *díra* → *děr*) je jiná alternace, o které `has_genitive_plural_shortening` nic neříká; taková slova potřebují `lemma_entry.stem`, který kód čte, ale který zatím ve slovníku nic nevyplňuje.
- Slot realizovaný infinitivem nebo obsahovou větou skládá `CzechClausePlanner`, ale jen jeden na klauzi: sloveso, které jich řídí víc najednou, se odmítne, místo aby se poskládalo. Tektogramatický manuál PDT (§2.4) doložený protipříklad má — *vyžadovat* řídí dva naráz — takže je to mez implementace, ne jazyka.
- Zvratný infinitiv dává svoje se/si do klastru věty řídící, což je u jednoho správně — *chce se učit* — a u dvou se odmítne, protože klastr je v klauzi jeden a *se* v něm nemůže být dvakrát. Tomu, co čeština dělá místo toho, se říká haplologie zvratného klitika (Rosen 2014), a doložené jsou tři cesty: dlouhý tvar (*sebe*), vlastní klastr a smazání jedné částice. Kterou generovat, otevřené je; odmítnout je jediná odpověď, která nikdy není špatně.
- `CzechNumeralComposer.ComposeOrdinal` a `ComposeOfType` skládají jen z lemmat ve slovníku; hodnota vyžadující chybějící složku (např. *dvoutisící*) selže s výjimkou, místo aby si tvar vymyslela.

### Co modelované není

- Klauze spojené přes `SentencePlan.Joined` se vnořují, jak hluboko se napíšou, a řetěz na jedné úrovni (`[a: B, protože: C]`) je jiná věta než řetěz vnoření (`a: B { protože: C }`) — vyslovit jde obojí. Vztažná věta je plnohodnotný plán, takže uvnitř ní platí všechno, co platí o větě.
- `IValencyProvider.GetEntry` bere lemma, volitelně s `WordCategory`, takže homonyma napříč kategoriemi rozlišit umí, ale homonyma uvnitř jedné ne. Schéma je nese ve sloupci `homonym_index` a provider vrátí to s nejnižším.
- Klitický klastr nezná volný dativ (*To ti byla legrace*), který podle NESČ stojí mezi pomocným slovesem a reflexivem. Ostatní pozice pořadí odpovídají. NESČ (*Dativ*) odděluje sémanticky motivované — prospěchový, posesivní, subjektový a zřetelový, které by valenční rámec nést mohl — od etického dativu, který je pragmatický a do žádného rámce nepatří; u druhého druhu je rámec špatné místo a u prvního správné.
- Ukazovací zájmeno před číslovkou (*těch pět studentů*) se shoduje s hlavou fráze, ne s celou frází.
- Že se vnitřní participant pojí s daným slovesem nejvýš jednou, se nevynucuje. V FGD to není pravidlo, které gramatika náhodou má, ale kritérium, které z participantu dělá participant (Urešová et al., PBML 105, 2016): slot, který jde obsadit dvakrát, je volné doplnění, ne aktant. Dvěma `PAT` v jedné klauzi nic nebrání a koordinace ani apozice — jediné dvě konstrukce, kde je opakování legitimní — modelované nejsou, takže ta kontrola zatím nemá pro co dělat výjimku.
- Aktuální členění se promítá jen do slovosledu. NESČ ho nese i v intonaci a dvě čtení lišící se prozodií považuje za dvě různé věty; to modelované není. Stejně tak kontrastivní ohnisko, na které manuál PDT (9.1.2, 9.3.1.1) potřebuje samostatný příznak intonačního centra — slovosled sám ho nezastoupí.
- Kontrola modelovaná je, ale jen jednoduchá. `valency_slot.control_target` zapisuje, se kterým participantem řídící klauze se nevyjádřený podmět infinitivu ztotožňuje, a `CzechClausePlanner` konstrukci odmítne, když jde o dva různé lidi. Modelovaná není dvojí kontrola (PDT §2.4) a táž koreference u obsahové věty místo infinitivu.
- Dvě z pěti českých diatezí dostupné nejsou. `SentencePlan.Diathesis` říká, ve které se má stavět, a deagentiv (*pracovalo se*) i dispoziční (*studentovi se pracovalo*) z toho vyjdou; rezultativ (*mám napsáno*) a recipientní deagentiv (*dostat* s příčestím) ne, protože obojí potřebuje druhé sloveso nesoucí příčestí prvního a takový tvar skladatel netvoří. Rámce pro ty dvě, které fungují, jsou ve slovníku u dvou sloves. Opisné pasivum, které mělo jediné sloveso, pokrývá teď čtrnáct významů — odvozených z jejich aktivních rámců pravidlem, které konatele posílá do instrumentálu a patiens do nominativu.
- Sdílené rozvití v koordinaci modelované není: doplnění prvního konjunktu nejde říct jednou a číst jako patřící oběma přísudkům. Elipsa slovesa modelovaná je (viz výš); tohle je druhá půlka §12.3 manuálu PDT.
- Koordinace členů s různými funktory, a s ní gapping, modelovaná není.
- Apozice modelovaná není, což je zároveň důvod, proč jedinečnost participantu nemá pro co dělat výjimku.
- Vytýkací konstrukce (*to, co potřebujeme, je…*) modelované nejsou.
- Projektivita se nevynucuje. Český slovosled neprojektivní konstrukce připouští a rozhodovač slovosledu o křížící se závislosti neví.
- Zápor je vlastnost přísudku, ne něco s dosahem: *nepřišel kvůli dešti* má dvě čtení a plán je nerozliší.
- Kondenzace — propozice vyjádřená jménem nebo infinitivem místo věty (*po jeho příchodu* za *když přišel*) — jako volba modelovaná není; slot se obsadí tak, jak je napsaný.

- Druhá třída tvoří trpné příčestí na *-nut* — *poslechnut*, *tisknut* — což je tvar, který uvádí IJP. Varianta *tištěn*, kterou IJP vede vedle *tisknut* pro význam tisknutí knih, se negeneruje: alternace *sk* → *ště* patří k příčestí na *-en* a nahrazením kmene ji vyrobit nejde, protože koncovka se připojuje k tomu, čím se kmen stane.

### Kde rozhoduje úzus a vybralo se jedno čtení

- Vokalizace předložek není podle IJP ustálený jev a rozhoduje úzus. Pravidla pokrývají uváděné tendence, zbytek je výčtem v `vocalizeBefore`.
- Čárka u `nebo` a `či` závisí na poměru vět, ne na spojce. Data nesou jen běžnější čtení; vylučovací poměr se musí říct přes `Coordination.RequiresComma`.
- Číslovky generují skloňovanou úhrnnou číslovku; ustrnulou variantu (*bez patero ponožek*), kterou IJP id=792 uvádí vedle ní jako rovněž spisovnou, si vyžádat nejde.
- U vzoru `sto` se generuje skloněná varianta s genitivem (*ke stu korun*); nesklonná se shodou (*ke sto korunám*), kterou IJP uvádí vedle ní, vyjádřit nejde.

### Jak se API používá

- Volající často musí dodat `Pattern`, `Gender`, `Number`, `Case`, `Person`, `Tense`, `Aspect`, `Modus` a `Voice`; projekt zatím není analyzátor přirozeného textu.
- `MorphologyEngine.GetForm` vrací jedno slovo, takže u slovesa dá jen základní tvar. Slovesné tvary o víc slovech — opisné futurum, pasivum s pomocným slovesem, kondicionál, negace, reflexivum — potřebují `CzechWordFormComposer.GetFullForm`.
- Pojmenovaný vzor z `irregulars.json` nese kmeny doslova, takže sedí na sloveso samotného vzoru a na jeho předponové odvozeniny — `nese` pokrývá *nést* i *odnést*, `dělá` pokrývá *dělat* i *dodělat*. Nepříbuzné sloveso potřebuje třídní vzor: *prodávat* se vzorem `dělá` vrátí *dělá*, s `trida5` správné *prodává*.
- CLI věty skládá, nečte je. Skloňovaný tvar se pozná jako tvar lemmatu a řekne se to, ale jako vstup se nebere — nástroj generuje z lemmat a analyzátor to není.

### Slovníkový provoz

- Databáze je binární, takže git neukáže, co se v ní změnilo. `dump` vyrobí čitelnou textovou podobu; napojení na commitovací postup hotové není.
- Pull stahuje pokaždé celý slovník. Přírůstková synchronizace neexistuje a vyžadovala by na serveru sledování změn a náhrobní záznamy — smazané řádky by přírůstkový pull jinak neviděl. Přepis celého souboru je řeší zadarmo, proto se začíná tam.

## Licence

Copyright (c) 50PSoftware. Všechna práva vyhrazena.
