# Grammar.Czech

![Status](https://img.shields.io/badge/status-active%20development-orange)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Version](https://img.shields.io/badge/version-1.0.0--preview.9-blue)
![License](https://img.shields.io/badge/license-Proprietary-red)

**Generativní morfologická knihovna pro češtinu na platformě .NET 8.**

Většina nástrojů pro práci s českými slovními tvary funguje jako slovník — předem uložené tabulky pro každé slovo. Grammar.Czech jde opačnou cestou: **tvary generuje za běhu z pravidel**, lemmatu a explicitních gramatických metadat. Žádná předpočítaná databáze celých paradigmat.

Primárně navrženo pro procedurálně generované dialogy NPC ve hrách, dokumentovou automatizaci a jazykově-vzdělávací nástroje, kde nelze tvary slov předem vypsat.

---

## Co projekt v současnosti reálně umí

### Skloňování podstatných jmen

Všech 7 pádů × jednotné/množné číslo pro všechny standardní české vzory:

| Mužský životný | Mužský neživotný | Ženský | Střední |
|---|---|---|---|
| `pán` | `hrad` | `žena` | `město` |
| `muž` | `les` *(dědí z hrad)* | `růže` | `moře` |
| `předseda` | `stroj` | `píseň` | `kuře` |
| `soudce` | | `kost` | `stavení` |

Vzory dědí koncovky přes `inheritsFrom` v JSON — `les` přepisuje jen ty tvary, kde se od `hrad` liší.

Nepravidelná slova (pohybné e, vlastní jména) se řeší příznakem v `irregulars.json`, nikoli větvením kódu.

### Stupňování a skloňování přídavných jmen

- Tvrdý vzor `mladý`, měkký vzor `jarní` — všechny rody, čísla, pády
- Komparativ a superlativ generovány algoritmicky z kmene
- Supletivní komparativy jako uzavřená množina:

  | Základní | Komparativ |
  |---|---|
  | `dobrý` | `lepší` |
  | `malý` | `menší` |
  | `velký` | `větší` |
  | `zlý` / `špatný` | `horší` |
  | `dlouhý` | `delší` |

### Skloňování zájmen

Pokryta tato lemmata:

| Typ | Lemmata |
|---|---|
| Osobní | `já`, `ty`, `on`, `ona`, `ono`, `my`, `vy`, `oni`, `ony`, `ona_` |
| Přivlastňovací | `můj`, `tvůj`, `jeho`, `její`, `náš`, `váš`, `jejich` |
| Zvratné | `sebe` |
| Ukazovací | `ten` |
| Tázací | `kdo`, `co`, `jenž` |

Skloňovací třídy: `Substantive` (fixní tabulka), `PronounHard/Soft` (vzorová tabulka), `AdjectiveHard/Soft` (deleguje na `CzechAdjectiveDeclensionService`), `Indeclinable` (vrací lemma beze změny).

Kde jazykově existují, pokrývá i varianty `afterPreposition` (po předložce).

### Časování sloves

Vzory: `dělá`, `prosí`, `kupuje`, `maže`, `nese`, `peče`, `tiskne`, `mine`, `kryje`, `být`

Pokryté gramatické kategorie:

| Kategorie | Hodnoty |
|---|---|
| Čas | Přítomný, minulý, budoucí |
| Způsob | Indikativ, kondicionál, imperativ |
| Rod | Aktivum, pasivum |
| Číslo | Singulár, plurál |
| Osoba | 1., 2., 3. |
| Negace | `ne-` prefixem |
| Reflexivita | `se` / `si` |

Složené slovesné fráze (kondicionál, pasivum s auxiliárem `být`, reflexiva) sestavuje `CzechVerbPhraseBuilderService`.

### Fonologické transformace

Všechna fonologická rozhodnutí jdou přes `IPhonemeRegistry` — žádné hardcoded porovnávání znaků v servisní vrstvě.

| Metoda / proces | Popis |
|---|---|
| `ApplySoftening(stem, context)` | 1. a 2. palatalizace velár (`k→č`, `h→ž`, `k→c`, ...) |
| `RevertSoftening(stem)` | Zpětná palatalizace |
| `RemoveMobileVowel(stem)` | `pes → ps-` (genitivní kmen) |
| `InsertMobileVowel(stem, pos)` | `ps- → pes` |
| `ApplyEpenthesis(stem, suffix)` | Vkládání `e` před derivační sufix |
| `ShortenVowel(stem)` | Kvantitativní krácení přes registry |
| `LengthenVowel(stem)` | Kvantitativní dloužení přes registry |

Ortografická vrstva je v `CzechOrtographyService` (registrováno jako `ICzechOrtographyService`):

| Metoda | Popis |
|---|---|
| `NormalizeEndingOrthography(stem, ending)` | `ě→e` reverze pro non-DTN, non-labiální kmeny |
| `ApplyJotationOrthography(ending)` | `e→ě` po labiálách (ortografický zápis jotace) |

### Valenční slovník

`JsonValencyProvider` načítá z embedded JSON (v `Data/Valency/`):

- `lexicon.json` — morfologická metadata lemmatu (rod, vzor, vid, animátnost, `hasMobileVowel`, `hasGenitivePluralShortening`, ...)
- `valency.json` — valenční rámce pro slovesa

Provider implementuje `IValencyProvider<CzechLexicalEntry>`. Architektura umožňuje záměnu za SQLite backend pouhou změnou DI registrace.

### Alternace v genitiv plurálu

`CzechAlternationRuleEvaluator` implementuje `ShouldShortenGenitivePlural` — čte příznak `hasGenitivePluralShortening` z valenčního slovníku nebo přímo z requestu. Data pro lemmata `kráva`, `síla`, `lípa`, `houba`, `žíla` jsou v `lexicon.json`.

> ⚠️ Třída existuje a je implementovaná, ale **není zaregistrovaná v DI** (`CzechGrammarServiceFactory` ji neobsahuje). Krácení gen. pl. tedy zatím aktivně neprobíhá.

---

## Architektura solution

```
Grammar.sln
│
├── Grammar.Core/               # Jazyk-agnostický základ (.NET 8 library)
│     Enums/                    # Case, Gender, Number, Tense, VerbAspect, WordCategory, …
│     Enums/PhonologicalFeatures/  # ArticulationPlace, ArticulationManner, Voicing, …
│     Interfaces/               # IInflectionService<T>, IPhonologyService<T>,
│                               # IPhonemeRegistry, IValencyProvider<T>, …
│     Models/Phonology/         # Phoneme record (Symbol, Place, Manner, Voicing,
│                               #   PalatalizeTo, Short/LongCounterpart, …)
│     Models/Word/              # WordForm, WordStructure
│     Models/Valency/           # ValencyFrame, ValencySlot
│
├── Grammar.Czech/              # Česká implementace (.NET 8 library, NuGet-ready)
│     Models/                   # CzechWordRequest, CzechLexicalEntry, NounPattern, …
│     Enums/                    # PalatalizationContext, PronounType, InflectionClass, …
│     Interfaces/               # ICzechPhonologyService, ICzechPronounService, …
│     Services/
│       MorphologyEngine                 # Routing dle WordCategory
│       CzechWordFormComposer            # Hlavní vstupní bod; sestavuje kompletní tvar
│       CzechNounDeclensionService       # Skloňování substantiv
│       CzechAdjectiveDeclensionService  # Skloňování adjektiv + stupňování
│       CzechPronounService             # Zájmena (lookup + delegace na adjektivní vzory)
│       CzechVerbConjugationService     # Konjugace sloves
│       CzechPhonologyService           # Hláskové alternace
│       CzechOrtographyService          # DTN ortografie, iotace
│       CzechAlternationRuleEvaluator   # ShouldShortenGenitivePlural
│       CzechAuxiliaryVerbService       # Tvary auxiliáru být
│       CzechVerbPhraseBuilderService   # Pasivum, kondicionál, reflexiva
│       CzechNegationService            # Negační prefix + negace být
│       CzechPrefixService              # Perfektivní / negační prefixy
│       CzechParticleService            # Kondicionální partikule, reflexiva se/si
│       CzechPrepositionService         # Validace prepozice–pád
│     Providers/JsonProviders/  # Thread-safe Lazy<T> JSON providery
│     Data/                     # Embedded JSON: vzory, nepravidelná slova, vlastní jména,
│                               # adjektivní paradigmata, slovesná paradigmata, zájmena,
│                               # foném registry, partikule, předložky, valenční slovník
│     CzechGrammarServiceFactory.cs    # AddCzechGrammarServices() DI extension
│
├── Grammar.Czech.Cli/          # Konzolová demo aplikace (net8.0)
└── Grammar.Czech.Test/         # MSTest data-driven testy (MSTest.Sdk 3.6.4)
```

---

## Design principy

### 1 — Generativní morfologie, ne tabulkový slovník
Tvary vznikají výpočtem. JSON vrstva drží paradigmatické **koncovky** a minimální **přepisovou** vrstvu pro nepravidelná slova. V kódu jsou **pravidla** (změkčení, epenteze, pohybné e, DTN ortografie). Žádný soubor nemapuje každé lemma na každý tvar.

### 2 — Foném registry jako jediný zdroj pravdy
Každé fonologické rozhodnutí — zda změkčit konsonant, jak zkrátit vokál, zda je kmen DTN — se vyřeší dotazem na `IPhonemeRegistry`. `Phoneme` record nese artikulaci (`ArticulationPlace`, `ArticulationManner`, `Voicing`), cíle palatalizace, kvantitativní páry i znělostní páry.

### 3 — Oddělení rozhodnutí od transformace
Evaluatory (`ISofteningRuleEvaluator`, `IEpenthesisRuleEvaluator`, `IJotationRuleEvaluator`, `IAlternationRuleEvaluator`) vlastní *rozhodovací* logiku; `CzechPhonologyService` vlastní *transformaci*. Servisy jsou orchestrátoři, kteří volají oboje.

### 4 — Dependency injection
Vše registrováno přes `AddCzechGrammarServices()` a resolvováno přes `IServiceCollection`. Žádný service neinstancuje závislosti přímo.

### 5 — Thread-safe data providery
Všechny JSON providery používají `Lazy<T>` s `LazyThreadSafetyMode.ExecutionAndPublication`. Dědičnost vzorů (např. `pán → student`) se resolvuje jednou při načtení, ne při každém requestu.

### 6 — Jazyk-agnostické jádro
`Grammar.Core` neobsahuje žádný kód specifický pro češtinu. Přidání nového jazyka = nový projekt referencující `Grammar.Core` implementující jeho interface.

---

## Rychlý start

### Registrace services

```csharp
using Grammar.Czech;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCzechGrammarServices();

var provider = services.BuildServiceProvider(
    new ServiceProviderOptions { ValidateOnBuild = true });
var composer = provider.GetRequiredService<CzechWordFormComposer>();
```

### Skloňování podstatného jména

```csharp
var request = new CzechWordRequest
{
    Lemma        = "student",
    WordCategory = WordCategory.Noun,
    Gender       = Gender.Masculine,
    Pattern      = "pán",
    IsAnimate    = true,
    Number       = Number.Singular,
    Case         = Case.Genitive,
};

var form = composer.GetFullForm(request);
Console.WriteLine(form.Form); // → "studenta"
```

### Časování slovesa

```csharp
var request = new CzechWordRequest
{
    Lemma        = "dělat",
    WordCategory = WordCategory.Verb,
    Aspect       = VerbAspect.Imperfective,
    Pattern      = "dělá",
    Tense        = Tense.Present,
    Number       = Number.Singular,
    Person       = Person.First,
    Modus        = Modus.Indicative,
};

var form = composer.GetFullForm(request);
Console.WriteLine(form.Form); // → "dělám"
```

### Komparativ přídavného jména

```csharp
var request = new CzechWordRequest
{
    Lemma        = "mladý",
    WordCategory = WordCategory.Adjective,
    Gender       = Gender.Masculine,
    Number       = Number.Singular,
    Case         = Case.Dative,
    Degree       = Degree.Comparative,
};

var form = composer.GetFullForm(request);
Console.WriteLine(form.Form); // → "mladšímu"
```

### Skloňování zájmena

```csharp
var request = new CzechWordRequest
{
    Lemma        = "já",
    WordCategory = WordCategory.Pronoun,
    Case         = Case.Dative,
};

var form = composer.GetFullForm(request);
Console.WriteLine(form.Form); // → "mně" / "mi"
```

---

## Datová vrstva

Všechna gramatická data jsou v **embedded JSON** souborech v `Grammar.Czech/Data/` a načítána jednou přes thread-safe `Lazy<T>` providery.

| Soubor / adresář | Obsah |
|---|---|
| `Data/Nouns/patterns.json` | Paradigmatické koncovky všech substantivních vzorů |
| `Data/Nouns/irregulars.json` | Per-lemma přepisy (pohybné e, vlastní koncovky) |
| `Data/Nouns/propers.json` | Vlastní jména |
| `Data/Adjectives/` | Adjektivní paradigmata |
| `Data/Verbs/` | Slovesná paradigmata + nepravidelné konjugace |
| `Data/Pronouns/pronouns.json` | Data zájmen a reference na paradigmata |
| `Data/Pronouns/paradigms.json` | Deklinační tabulky zájmen |
| `Data/Phonology/phonemes.json` | Foném registry (artikulace, alternace, kvantitativní páry) |
| `Data/Particles/` | Kondicionální partikule, reflexiva |
| `Data/Prepositions/` | Mapování prepozice–pád |
| `Data/Valency/lexicon.json` | Morfologická metadata lemmatu (rod, vzor, vid, animátnost, ...) |
| `Data/Valency/valency.json` | Valenční rámce sloves |

---

## Přidání nepravidelného slova

Při nestandardním chování slova se přidá záznam do příslušného JSON, **ne větev v kódu**.

```json
// Data/Nouns/irregulars.json
"pes":  { "hasMobileVowel": true, "inheritsFrom": "pán"  },
"den":  { "hasMobileVowel": true, "inheritsFrom": "hrad" },
"otec": { "hasMobileVowel": true, "inheritsFrom": "muž"  }
```

---

## Přidání nového jazyka

Vytvoř nový projekt referencující `Grammar.Core` a implementuj:

- `IInflectionService<TRequest>` — skloňování/časování
- `IPhonologyService<TRequest>` — fonologické transformace
- `IPhonemeRegistry` — registr fonémů pro cílový jazyk

Vše zaregistruj v DI extension metodě analogické k `AddCzechGrammarServices()`.

---

## Spuštění testů

```bash
dotnet test Grammar.Czech.Test/
```

Testy používají MSTest `[DataTestMethod]` / `[DataRow]` pro pokrytí celých paradigmatických tabulek.

---

## Známá omezení

| Oblast | Stav |
|---|---|
| `CzechAlternationRuleEvaluator` | Implementovaná, ale **nezaregistrovaná v DI** — krácení gen. pl. aktivní není |
| `GuessGenderAndPattern` | Neimplementováno — volající musí dodat vzor explicitně |
| `ResolveVerbAspect` | Funguje pro lemmata v `lexicon.json`; pro nezaregistrovaná lemmata hází `LemmaNotFoundException` |
| Numeralia | Nenačato |
| Generování vět (NLG) | Mimo scope aktuálního milníku |

---

## Roadmap

### Blízký horizont
- [ ] Zaregistrovat `CzechAlternationRuleEvaluator` v DI (`IAlternationRuleEvaluator`) a aktivovat krácení gen. pl.
- [ ] Rozšířit `lexicon.json` — doplnit lemmata pro `ResolveVerbAspect` a `GuessGenderAndPattern`
- [ ] Implementovat `GuessGenderAndPattern` (suffix heuristika + valenční slovník)
- [ ] Doplnit chybějící testy — všechny vzory, všechna zájmenná lemmata, slovesné konjugace

### Výhled
- [ ] SQLite-backed `IValencyProvider` jako záměna za `JsonValencyProvider`
- [ ] Skloňování číslovek (`CzechNumeralService`)
- [ ] NLG — generování vět ze sémantického vstupu (`SemanticInput → SentencePlanner`)
- [ ] NuGet balíček (`50PSoftware.GrammarModular.Czech`)
- [ ] Podpora latiny

---

## Licence

Copyright © 50PSoftware. Všechna práva vyhrazena.
