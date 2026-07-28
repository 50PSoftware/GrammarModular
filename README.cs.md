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

Projekt se balí sám: `GeneratePackageOnBuild` je zapnutý a build vedle sestavení vytvoří `50PSoftware.GrammarModular.Czech.<verze>.nupkg`. Na nuget.org balíček není — bere se z privátního nebo lokálního feedu, případně projektovou referencí:

```bash
dotnet build Grammar.sln
```

```bash
dotnet add reference ../Grammar/Grammar.Czech/Grammar.Czech.csproj
```

Verze je pořád `-preview`, takže package reference musí povolit předběžné verze.

Gramatická data jsou vložená jako embedded resources přímo v `Grammar.Czech`, takže se vedle sestavení nekopírují žádné datové soubory.

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
- `IJotationRuleEvaluator<CzechWordRequest>`,
- `ISyncretismRuleEvaluator<CzechWordRequest>`,
- `ICzechOrthographyService`.

`CzechAlternationRuleEvaluator` pro krácení genitivu plurálu existuje, ale aktuálně není registrovaný v `AddCzechGrammarServices()` a není zapojený v `CzechNounDeclensionService`.

### Lexikon a valence

`JsonValencyProvider` načítá embedded JSON z `Grammar.Czech/Data/Lexicon/`:

- `lexicon.json` obsahuje morfologická metadata lemmat, např. rod, vzor, vid, animátnost, pohybné `e` nebo příznak krácení genitivu plurálu,
- `valency.json` obsahuje valenční rámce.

Lexikon slouží hlavně jako provider metadat pro vybrané resolvery, není to úplný český slovník.

Valenční rámec říká, jak se realizují argumenty daného slovesa, a `CzechSentenceBuilder` z něj bere pád i předložku: u `vidět` je `PAT` akuzativ, u `dávat` je `ADDR` dativ a `PAT` akuzativ, u `jít` je `DIR3` předložka `do` s genitivem. Pád zadaný explicitně zůstává — rámec doplňuje mezery. Sloveso s víc rámci se vybírá přes `CzechClause.FrameLabel`, protože `jít` má jiné argumenty jako pohyb a jiné jako proces.

Vnitřní participanty (`ACT`, `PAT`, `ADDR`, `ORIG`, `EFF` — aktanty FGP) může licencovat jen rámec, takže `vidět` s adresátem skončí výjimkou. Volná doplnění se pojí s kterýmkoli slovesem a pád si u nich zadává volající.

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
- **vztažné věty** — zájmeno se shoduje s řídícím jménem v rodě, čísle a životnosti, pád si bere ze své role ve vedlejší větě.

Podřadicí spojka a vztažné zájmeno obsazují první pozici své klauze, takže za nimi jde klastr: *protože se student učil*, *muž, kterého jsem viděl*. Souřadicí spojka stojí mimo klauzi a první pozici jí nechává.

Spojky jsou uzavřená třída v `Grammar.Czech/Data/Rules/conjunctions.json`; neznámá spojka skončí výjimkou, protože na jejím druhu závisí čárka i pozice klitika. Každé heslo navíc nese vztah, který vyjadřuje, ve skupinách podle NESČ — *slučovací*, *odporovací*, *stupňovací*, *vylučovací*, *příčinné*, *důsledkové* u souřadicích a *časové*, *příčinné*, *přípustkové*, *podmínkové*, *účelové*, *obsahové*, *srovnávací* u podřadicích — a druhý člen tam, kde je spojka párová (*buď – nebo*, *ani – ani*, *nejen – ale i*, *jak – tak*, *sice – ale*, *jednak – jednak*). Dvojité spojky existují jen mezi souřadicími.

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
|-- Grammar.Core/        jazykově nezávislé enumy, rozhraní a modely
|-- Grammar.Czech/       česká implementace, servisy, providery a embedded JSON data
|-- Grammar.Czech.Cli/   konzolové demo s hardcodovanými příklady
`-- Grammar.Czech.Test/  MSTest testy pro skloňování, časování, fonologii a stavbu vět
```

Hlavní registrace pro DI je `AddCzechGrammarServices()` v `Grammar.Czech/CzechGrammarServiceFactory.cs`.

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

Sloveso s víc rámci si o výběr řekne výjimkou; rámec se pak zadá přes `FrameLabel`:

```csharp
new CzechClause
{
    Predicate = goes,          // jít — rámce motion a process
    Elements = [subject, toSchool],
    FrameLabel = "motion",
};
```

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

`Grammar.Czech.Cli` je zatím demo aplikace. Nemá obecné zpracování argumentů; po spuštění vypíše tvary několika pevně zapsaných příkladů z `Program.cs`.

```bash
dotnet run --project Grammar.Czech.Cli
```

## Testy

```bash
dotnet test Grammar.Czech.Test
```

Testy jsou v MSTest a pokrývají substantiva, adjektiva, zájmena, číslovky, slovesa, vybrané fonologické evaluátory/služby, stavbu vět a souvětí, a načítání všech JSON providerů včetně referenční integrity mezi soubory.

## Datová vrstva

Všechna gramatická data v projektu `Grammar.Czech` jsou embedded JSON resources:

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
| `Data/Lexicon/lexicon.json` | lexikální metadata |
| `Data/Lexicon/valency.json` | valenční rámce (`dát`, `dávat`, `jít`, `vidět`) |

## Známá omezení

- Volající často musí dodat `Pattern`, `Gender`, `Number`, `Case`, `Person`, `Tense`, `Aspect`, `Modus` a `Voice`; projekt zatím není analyzátor přirozeného textu.
- `MorphologyEngine.GetForm` vrací jedno slovo, takže u slovesa dá jen základní tvar. Slovesné tvary o víc slovech — opisné futurum, pasivum s pomocným slovesem, kondicionál, negace, reflexivum — potřebují `CzechWordFormComposer.GetFullForm`.
- Pojmenovaný vzor z `irregulars.json` nese kmeny doslova, takže sedí na sloveso samotného vzoru a na jeho předponové odvozeniny — `nese` pokrývá *nést* i *odnést*, `dělá` pokrývá *dělat* i *dodělat*. Nepříbuzné sloveso potřebuje třídní vzor: *prodávat* se vzorem `dělá` vrátí *dělá*, s `trida5` správné *prodává*.
- `CzechAlternationRuleEvaluator` není registrovaný v DI a krácení genitivu plurálu není aktivně napojené ve skloňování substantiv.
- Lexikon není úplný slovník češtiny; `ResolveGenderAndPattern` a `ResolveVerbAspect` fungují jen pro lemmata obsažená v `lexicon.json`.
- CLI je demo, ne uživatelský nástroj pro obecné dotazování.
- Číslovky nepodporují ustrnulou variantu úhrnných číslovek (*bez patero ponožek*), kterou IJP id=792 uvádí vedle skloňované jako rovněž spisovnou; generuje se vždy skloňovaná.
- Ukazovací zájmeno před číslovkou (*těch pět studentů*) se shoduje s hlavou fráze, ne s celou frází.
- `CzechNumeralComposer.ComposeOrdinal` a `ComposeOfType` skládají jen z lemmat ve slovníku; hodnota vyžadující chybějící složku (např. *dvoutisící*) selže s výjimkou, místo aby si tvar vymyslela.
- `valency.json` obsahuje rámce jen pro čtyři slovesa. Mechanismus je hotový, data ne — u slovesa bez rámce si pády zadává volající jako dřív.
- Klitický klastr nezná volný dativ (*To ti byla legrace*), který podle NESČ stojí mezi pomocným slovesem a reflexivem. Ostatní pozice pořadí odpovídají.
- Spojky `aby` a `kdyby` podporované nejsou — splývají s kondicionálovým pomocným slovesem a časují se podle osoby (*abych*, *abys*, *abychom*). Stejně tak `však`, které je samo druhopozicové, ne uvozovací.
- Čárka u `nebo` a `či` závisí na poměru vět, ne na spojce. Data nesou jen běžnější čtení; vylučovací poměr se musí říct přes `Coordination.RequiresComma`.
- Vokalizace předložek není podle IJP ustálený jev a rozhoduje úzus. Pravidla pokrývají uváděné tendence, zbytek je výčtem v `vocalizeBefore`.
- Aktuální členění se promítá jen do slovosledu. NESČ ho nese i intonací a dvě čtení lišící se prozodií považuje za dvě různé věty; to modelované není.
- Že se vnitřní participant pojí se slovesem nejvýš jednou, se nevynucuje — dva `PAT` konstituenty v jedné klauzi nic nezastaví.
- Vztažná věta musí být jedna klauze; souvětí uvnitř vztažné věty podporované není.
- U vzoru `sto` se generuje skloněná varianta s genitivem (*ke stu korun*); nesklonná se shodou (*ke sto korunám*), kterou IJP uvádí vedle ní, vyjádřit nejde.

## Licence

Copyright (c) 50PSoftware. Všechna práva vyhrazena.
