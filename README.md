# Grammar.Czech

![Status](https://img.shields.io/badge/status-active%20development-orange)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Version](https://img.shields.io/badge/version-1.0.0--preview.4-blue)
![License](https://img.shields.io/badge/license-Proprietary-red)

**A rule-based Czech morphology engine for .NET 8.**

Most tools that deal with Czech word forms work by looking up pre-computed tables.
Grammar.Czech takes a different approach — it *generates* inflected forms dynamically
from linguistic rules, a lemma, and explicit grammatical metadata. No pre-computed
full-form database required.

This makes the engine well-suited for scenarios where you need to inflect arbitrary
words at runtime — such as procedurally generated NPC dialogue in games, document
automation, or language-learning tools where word forms cannot be pre-enumerated.

---

## Features

| Category | What's covered |
|---|---|
| **Noun declension** | All 7 cases × singular/plural, all genders and patterns |
| **Adjective declension** | Hard/soft patterns, positive/comparative/superlative, possessives |
| **Pronoun inflection** | Personal, possessive, demonstrative, reflexive, interrogative, negative, indefinite |
| **Verb conjugation** | Present/past/future, conditional, imperative, passive, negation, reflexives (`se`/`si`) |
| **Phonological transformations** | Vowel quantity, mobile vowel (*pohybné e*), epenthesis, consonant softening (1st & 2nd palatalization), DTN orthography |
| **Rule-based generation** | Forms computed from rules + a minimal JSON override layer |
| **Language-agnostic core** | `Grammar.Core` carries no Czech-specific logic |

---

## Repository Layout

```
Grammar.sln

Grammar.Core/               # Language-agnostic models and interfaces (.NET 8 library)
  Enums/                    # Case, Gender, Number, Tense, VerbAspect, WordCategory, …
  Enums/PhonologicalFeatures/  # ArticulationPlace, ArticulationManner, Voicing, …
  Interfaces/               # IInflectionService<T>, IPhonologyService<T>,
  │                         # IPhonemeRegistry, IWordRequest, …
  Models/
    Phonology/              # Phoneme record (Symbol, Place, Manner, Voicing,
                            #   PalatalizeTo, Short/LongCounterpart, …)
    Word/                   # WordForm, WordStructure

Grammar.Czech/              # Czech-specific implementation (.NET 8 library, NuGet-ready)
  Models/                   # CzechWordRequest, CzechPhoneme, NounPattern, …
  Enums/                    # PalatalizationContext, PronounType, InflectionClass, …
  Interfaces/               # ICzechPhonologyService, ICzechPronounService,
  │                         # ICzechOrtographyService, …
  Services/
  │  MorphologyEngine                  # Routes by WordCategory to the correct service
  │  CzechWordFormComposer             # Top-level entry point; assembles full word forms
  │  CzechNounDeclensionService        # Noun paradigm resolution
  │  CzechAdjectiveDeclensionService   # Adjective paradigm + degree construction
  │  CzechPronounService               # Pronoun lookup + adjective delegation
  │  CzechVerbConjugationService       # Verb paradigm resolution
  │  CzechPhonologyService             # Vowel alternations, softening, epenthesis
  │  CzechOrtographyService            # DTN orthography, jotation orthography
  │  CzechAuxiliaryVerbService         # `být` auxiliary forms
  │  CzechVerbPhraseBuilderService     # Passive, conditional, reflexive construction
  │  CzechNegationService              # Negation prefix + `být` negation
  │  CzechPrefixService                # Perfective/negative prefixes
  │  CzechParticleService              # Conditional particles, reflexives `se`/`si`
  │  CzechPrepositionService           # Preposition–case validation
  Providers/JsonProviders/  # Thread-safe Lazy<T> JSON providers for all data categories
  Data/                     # Embedded JSON: noun patterns, irregulars, proper names,
  │                         # adjective paradigms, verb paradigms, pronouns,
  │                         # phonemes registry, particles, prepositions, …
  CzechGrammarServiceFactory.cs  # AddCzechGrammarServices() DI extension method

Grammar.Czech.Cli/          # Console demo / scratch runner (net8.0 executable)
Grammar.Czech.Test/         # MSTest data-driven unit tests (MSTest.Sdk 3.6.4)
```

---

## Design Principles

### 1 — Rule-based, not table-based
Inflected forms are computed. The JSON data layer holds paradigm *endings* and a
minimal *overrides* layer for irregular words. The code layer holds *rules* (softening,
epenthesis, mobile vowel removal, DTN orthography). No single file maps every lemma to
every form.

### 2 — Phoneme registry as the single source of truth
Every phonological decision — whether to soften a consonant, how to shorten a vowel,
whether a stem ends in a DTN consonant — is resolved by querying the `IPhonemeRegistry`.
The `Phoneme` record encodes articulation properties (`Place`, `Manner`, `Voicing`) and
derived mappings (`PalatalizeTo`, `ShortCounterpart`, `LongCounterpart`,
`VoicedCounterpart`). New alternation rules should read from the registry, never from
hard-coded character comparisons.

### 3 — Data vs. code separation
Directional phonological changes (e.g. `e→ě` after a labial) belong in JSON paradigm
data. Code handles only *reversals* and *guards* (e.g. `ě→e` normalization for non-DTN,
non-labial stems). Violations of this principle have historically caused bugs.

### 4 — Evaluator pattern for phonology
Decisions about *whether* to apply a phonological rule are separated from the act of
*applying* it. Evaluator interfaces (`ISofteningRuleEvaluator`, `IEpenthesisRuleEvaluator`,
`IJotationRuleEvaluator`, …) own the decision logic; `CzechPhonologyService` owns the
transformation. Inflection and conjugation services act as orchestrators that call both.

### 5 — Dependency injection throughout
All services are registered via `AddCzechGrammarServices()` and resolved through
`IServiceCollection`. No service instantiates its collaborators directly.

### 6 — Thread-safe data providers
All JSON providers use `Lazy<T>` with `LazyThreadSafetyMode.ExecutionAndPublication`.
Pattern inheritance (e.g. `pán` → `student`) is resolved once at load time, not at
request time.

### 7 — Language-agnostic core
`Grammar.Core` contains no Czech-specific code. Adding a new language means adding a new
project that references `Grammar.Core` and implements its interfaces.

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 / JetBrains Rider (any recent version)

### Add the reference

```xml
<!-- in your .csproj -->
<ProjectReference Include="..\Grammar.Czech\Grammar.Czech.csproj" />
```

### Register services

```csharp
using Grammar.Czech;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCzechGrammarServices();   // data is embedded — no path argument needed

var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
var composer = provider.GetRequiredService<CzechWordFormComposer>();
```

---

## Quick Start Examples

### Decline a noun

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

### Conjugate a verb

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

### Inflect an adjective (comparative)

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

### Inflect a pronoun

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

## API Reference

### `CzechWordRequest`

The central request object. All properties except `Lemma` and `WordCategory` are
optional; supply only those relevant to the word category and the form you need.

| Property | Type | Applies to | Description |
|---|---|---|---|
| `Lemma` | `string` | All | Dictionary form (required) |
| `WordCategory` | `WordCategory` | All | Noun / Adjective / Pronoun / Verb / Numerale |
| `Pattern` | `string?` | All | Paradigm key, e.g. `"pán"`, `"žena"`, `"dělá"` |
| `Gender` | `Gender?` | Noun, Adj, Pron | Masculine / Feminine / Neuter |
| `Number` | `Number?` | Noun, Adj, Pron, Verb | Singular / Plural |
| `Case` | `Case?` | Noun, Adj, Pron | Nominative … Instrumental (1–7) |
| `IsAnimate` | `bool?` | Noun | Affects masculine accusative |
| `Person` | `Person?` | Verb | First / Second / Third |
| `Tense` | `Tense?` | Verb | Present / Past / Future |
| `Aspect` | `VerbAspect?` | Verb | Perfective / Imperfective |
| `Modus` | `Modus?` | Verb | Indicative / Conditional / Imperative |
| `Voice` | `Voice?` | Verb | Active / Passive |
| `VerbClass` | `VerbClass?` | Verb | Czech verb class 1–5 (optional hint) |
| `Degree` | `Degree?` | Adjective | Possitive / Comparative / Superlative |
| `IsNegative` | `bool` | Verb | Prepend negation prefix (`ne-`) |
| `HasReflexive` | `bool?` | Verb | Append `se` / `si` |
| `HasExplicitSubject` | `bool?` | Verb | Affects conditional word order |

### `CzechWordFormComposer` — main entry point

```csharp
WordForm GetFullForm(CzechWordRequest request)
```

Returns the complete word form including auxiliary verbs, reflexive particles, and
conditional particles where applicable.

### `MorphologyEngine` — lower-level access

Use when you need raw morphology without phrase assembly.

```csharp
WordForm GetForm(CzechWordRequest request)       // nouns, adjectives, pronouns
WordForm GetBasicForm(CzechWordRequest request)  // verb base form only
```

### `WordForm`

```csharp
public record WordForm(string Form, string Lemma, ...);
```

The `Form` property contains the final orthographic string. Additional metadata
(lemma, case, number, …) is preserved for downstream use.

---

## Supported Paradigms

### Nouns

| Masculine animate | Masculine inanimate | Feminine | Neuter |
|---|---|---|---|
| `pán` | `hrad` | `žena` | `město` |
| `muž` | `stroj` | `růže` | `moře` |
| `předseda` | | `píseň` | `kuře` |
| `soudce` | | `kost` | `stavení` |

### Adjectives

`mladý` (hard), `jarní` (soft). Comparative and superlative are derived
algorithmically. Suppletive comparatives (`dobrý→lepší`, `malý→menší`,
`velký→větší`, `zlý→horší`, `špatný→horší`, `dlouhý→delší`) are handled
as a closed set.

### Verbs

`dělá`, `prosí`, `kupuje`, `maže`, `nese`, `peče`, `tiskne`, `mine`, `kryje`, `být`

### Pronouns

`já`, `ty`, `on`, `ona`, `ono`, `my`, `vy`, `oni`, `ona_` (neuter plural),
`můj`, `tvůj`, `jeho`, `její`, `náš`, `váš`, `jejich`,
`sebe`, `ten`, `kdo`, `co`, `jenž`

---

## Phonological Transformations

All phonological decisions are driven by the `IPhonemeRegistry`. The `Phoneme` record
encodes:

- **Articulation** — `ArticulationPlace` (Bilabial, Alveolar, Velar, …),
  `ArticulationManner` (Plosive, Nasal, Fricative, …), `Voicing`
- **Alternation targets** — `PalatalizeTo` (universal softening target),
  `CzechPhoneme.PalatalizationTargets` (context-keyed: First / Second palatalization)
- **Quantity pairs** — `ShortCounterpart`, `LongCounterpart`
- **Voice pairs** — `VoicedCounterpart`, `VoicelessCounterpart`

The transformations available through `ICzechPhonologyService`:

| Method | What it does |
|---|---|
| `ApplySoftening(stem, context)` | Consonant palatalization (1st or 2nd) |
| `RevertSoftening(stem)` | Reverse a palatalization |
| `RemoveMobileVowel(stem, flag)` | `pes` → `ps-` (genitive stem) |
| `InsertMobileVowel(stem, pos)` | `ps-` → `pes` |
| `ApplyEpenthesis(flag, stem, suffix)` | Inserts `e` before a derivational suffix |
| `ShortenVowel(stem)` | Quantitative shortening via registry |
| `LengthenVowel(stem)` | Quantitative lengthening via registry |

DTN orthography (`d/t/n + e → dě/tě/ně`) is handled separately by
`ICzechOrtographyService.ApplyDTNEndingOrthography`.

---

## Data Layer

All grammatical data is stored as **embedded JSON** in `Grammar.Czech/Data/` and loaded
once via thread-safe `Lazy<T>` providers. The key files are:

| File / directory | Contents |
|---|---|
| `Data/Nouns/patterns.json` | Paradigm endings for all noun patterns |
| `Data/Nouns/irregulars.json` | Per-lemma overrides (mobile vowel, custom endings) |
| `Data/Nouns/propers.json` | Proper-noun overrides |
| `Data/Adjectives/` | Adjective paradigm endings |
| `Data/Verbs/` | Verb paradigm endings + irregular conjugations |
| `Data/Pronouns/pronouns.json` | Pronoun data and paradigm references |
| `Data/Pronouns/paradigms.json` | Pronoun declension tables |
| `Data/Phonology/phonemes.json` | Phoneme registry |
| `Data/Particles/` | Conditional particles, reflexives |
| `Data/Prepositions/` | Preposition–case mappings |

Pattern inheritance is resolved at provider load time. For example, `"student"` inherits
from `"pán"` and only stores the endings that differ:

```json
"student": {
  "inheritsFrom": "pán",
  "endings": {
    "singular": { "vocative": "-e" }
  }
}
```

---

## Adding Irregular Words

When a word has irregular behaviour, add an entry to the relevant JSON file — do **not**
add a code branch.

```json
// Data/Nouns/irregulars.json
"pes": { "hasMobileVowel": true, "inheritsFrom": "pán" },
"den": { "hasMobileVowel": true, "inheritsFrom": "hrad" },
"otec": { "hasMobileVowel": true, "inheritsFrom": "muž" }
```

---

## Adding a New Language

Create a new project referencing `Grammar.Core` and implement:

- `IInflectionService<TRequest>` — noun/adjective/pronoun inflection
- `IPhonologyService<TRequest>` — phonological transformations
- `IPhonemeRegistry` — phoneme data for the target language

Register everything in a DI extension method analogous to `AddCzechGrammarServices()`.

---

## Running Tests

```bash
dotnet test Grammar.Czech.Test/
```

Tests use MSTest's data-driven `[DataRow]` pattern to cover full paradigm tables.
Currently covered: noun vzory *pán*, *žena*, *píseň* (gen sg pattern), *pes*;
adjective comparative/superlative.

---

## Known Limitations

| Area | Status |
|---|---|
| Iotation (labials + `ě`) | Identified, not yet implemented |
| `n/d/t + e → ně/dě/tě` in comparatives | Identified (affects *jemnější*), next on roadmap |
| Vowel shortening (`ShouldShortenVowel`) | Stubbed |
| `GuessGenderAndPattern` / `GuessVerbAspect` | Stubbed — blocked on valency dictionary |
| Numerals | Not started |
| Sentence generation (NLG) | Out of scope for current milestone |
| *oni* dative `afterPreposition` | Data bug: `"nim"` should be `"ním"` |

---

## Roadmap

### Near term
- [ ] Implement `n/d/t + e → ně/dě/tě` orthographic rule in comparatives
- [ ] Implement `ShouldShortenVowel` / `ShouldLengthenVowel` via `IPhonemeRegistry`
- [ ] Fix *oni* dative `afterPreposition` data bug
- [ ] Complete missing unit tests — vzory *hrad*, *muž*, *stroj*, *město*, *moře*,
      *kuře*, *stavení*; all pronoun lemmas; verb conjugations
- [ ] SQLite-backed `IValencyProvider` (lemma, category, pattern, gender, aspect, animacy)
- [ ] Implement `GuessGenderAndPattern` and `GuessVerbAspect` (unblocked by valency dictionary)

### Future
- [ ] Full jotation support (`IJotationRuleEvaluator` + `ApplyJotation`)
- [ ] `CzechAlternationRuleEvaluator` (registered in DI)
- [ ] Numeral inflection
- [ ] NLG sentence construction from semantic input (`SemanticInput` → `SentencePlanner`)
- [ ] NuGet package (dual MIT / commercial licensing)
- [ ] Latin language support

---

## License

MIT — see [LICENSE.txt](LICENSE.txt) for details.

---

*Author: Vojtěch Pchálek · [50PSoftware](https://github.com/50PSoftware) · `v1.0.0-preview.2`*
