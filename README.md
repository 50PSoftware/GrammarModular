# Grammar.Czech

![Status](https://img.shields.io/badge/status-active%20development-orange)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Version](https://img.shields.io/badge/version-1.0.0--preview.13-blue)
![License](https://img.shields.io/badge/license-Proprietary-red)

**Generativní morfologická knihovna pro češtinu na platformě .NET 8.**

Projekt generuje české slovní tvary z lemmatu, gramatických kategorií, vzoru a JSON pravidel. Není to obecný slovník hotových tvarů ani hotové NLG pro skládání vět. Volající musí u většiny slov dodat explicitní metadata, hlavně slovní druh, vzor, rod/číslo/pád nebo slovesné kategorie.

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

Vzory mohou dědit koncovky přes `inheritsFrom`; například `les` dědí z `hrad` a přepisuje jen odlišné pády. Nepravidelnosti a vybraná vlastní jména jsou v `Grammar.Czech/Data/Rules/Nouns/irregulars.json` a `Grammar.Czech/Data/Rules/Nouns/propers.json`.

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

### Slovesa

Slovesa se generují z pravidel v:

- `Grammar.Czech/Data/Rules/Verbs/patterns.json` pro obecné třídy `trida1` až `trida5` a další vzory,
- `Grammar.Czech/Data/Rules/Verbs/irregulars.json` pro nepravidelná slovesa jako `být`, `mít`, `chtít`, `moci` a `vědět`.

`CzechVerbConjugationService` umí generovat základní tvary pro indikativ, kondicionál, imperativ, minulý čas, přítomný/budoucí čas a pasivní participium. `CzechWordFormComposer` nad tím skládá některé slovesné fráze: opisné futurum u imperfektiv, pasivum s pomocným slovesem, kondicionál, negaci a reflexivní `se`/`si`.

Slovesný vzor se předává přes `Pattern`; alternativně lze někdy předat `VerbClass`, která se namapuje na `trida1` až `trida5`. `GuessVerbClass` umí jednoduchou heuristiku podle infinitivní koncovky, ale není spolehlivá pro všechna česká slovesa.

### Fonologie a pravopis

Projekt obsahuje fonologickou vrstvu pro změkčení, epentezi, jotaci a kvantitu samohlásek. Rozhodování je oddělené do evaluátorů a transformace provádí `CzechPhonologyService` a `CzechOrtographyService`.

Mezi veřejně používané části patří:

- `IPhonemeRegistry` / `CzechPhonemeRegistry`,
- `ISofteningRuleEvaluator<CzechWordRequest>`,
- `IEpenthesisRuleEvaluator<CzechWordRequest>`,
- `IJotationRuleEvaluator<CzechWordRequest>`,
- `ICzechOrtographyService`.

`CzechAlternationRuleEvaluator` pro krácení genitivu plurálu existuje, ale aktuálně není registrovaný v `AddCzechGrammarServices()` a není zapojený v `CzechNounDeclensionService`.

### Lexikon a valence

`JsonValencyProvider` načítá embedded JSON z `Grammar.Czech/Data/Lexicon/`:

- `lexicon.json` obsahuje morfologická metadata lemmat, např. rod, vzor, vid, animátnost, pohybné `e` nebo příznak krácení genitivu plurálu,
- `valency.json` obsahuje valenční rámce.

Lexikon slouží hlavně jako provider metadat pro vybrané resolvery, není to úplný český slovník.

## Architektura

```text
Grammar.sln
|-- Grammar.Core/        jazykově nezávislé enumy, rozhraní a modely
|-- Grammar.Czech/       česká implementace, servisy, providery a embedded JSON data
|-- Grammar.Czech.Cli/   konzolové demo s hardcodovanými příklady
`-- Grammar.Czech.Test/  MSTest testy pro skloňování, časování a fonologická pravidla
```

Hlavní registrace pro DI je `AddCzechGrammarServices()` v `Grammar.Czech/CzechGrammarServiceFactory.cs`.

Hlavní vstupy:

- `CzechWordFormComposer` pro plný tvar slova nebo slovesné fráze,
- `MorphologyEngine` pro přímé směrování na substantiva, adjektiva, zájmena a základní slovesné tvary,
- specializované servisy jako `CzechNounDeclensionService`, `CzechAdjectiveDeclensionService`, `CzechPronounService` a `CzechVerbConjugationService`.

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

## CLI

`Grammar.Czech.Cli` je zatím demo aplikace. Nemá obecné zpracování argumentů; po spuštění vypíše tvary několika pevně zapsaných příkladů z `Program.cs`.

```bash
dotnet run --project Grammar.Czech.Cli
```

## Testy

```bash
dotnet test Grammar.Czech.Test
```

Testy jsou v MSTest a pokrývají hlavně substantiva, adjektiva, zájmena, slovesa a vybrané fonologické evaluátory/služby.

## Datová vrstva

Všechna gramatická data v projektu `Grammar.Czech` jsou embedded JSON resources:

| Cesta | Obsah |
|---|---|
| `Data/Rules/Nouns/patterns.json` | substantivní vzory |
| `Data/Rules/Nouns/irregulars.json` | nepravidelná substantiva |
| `Data/Rules/Nouns/propers.json` | vybraná vlastní jména |
| `Data/Rules/Adjectives/patterns.json` | adjektivní vzory |
| `Data/Rules/Pronouns/patterns.json` | data zájmen |
| `Data/Rules/Pronouns/paradigms.json` | zájmenná paradigmata |
| `Data/Rules/Verbs/patterns.json` | obecné slovesné třídy a vzory |
| `Data/Rules/Verbs/irregulars.json` | nepravidelná slovesa |
| `Data/Rules/prefixes.json` | prefixy |
| `Data/Rules/particles.json` | partikule |
| `Data/Rules/prepositions.json` | předložky a pády |
| `Data/Lexicon/lexicon.json` | lexikální metadata |
| `Data/Lexicon/valency.json` | valenční rámce |

## Známá omezení

- Volající často musí dodat `Pattern`, `Gender`, `Number`, `Case`, `Person`, `Tense`, `Aspect`, `Modus` a `Voice`; projekt zatím není analyzátor přirozeného textu.
- `MorphologyEngine.GetForm` podporuje jen `Noun`, `Adjective` a `Pronoun`; slovesa jdou přes `GetBasicForm` nebo přes `CzechWordFormComposer.GetFullForm`.
- `CzechAlternationRuleEvaluator` není registrovaný v DI a krácení genitivu plurálu není aktivně napojené ve skloňování substantiv.
- Lexikon není úplný slovník češtiny; `ResolveGenderAndPattern` a `ResolveVerbAspect` fungují jen pro lemmata obsažená v `lexicon.json`.
- CLI je demo, ne uživatelský nástroj pro obecné dotazování.
- Numeralia a generování celých vět jsou mimo aktuální implementaci.

## Licence

Copyright (c) 50PSoftware. Všechna práva vyhrazena.
