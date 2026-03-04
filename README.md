# Grammar.Czech

A rule-based Czech morphology engine for .NET. Generates grammatically correct inflected forms of Czech words dynamically from a lemma, grammatical metadata, and linguistic rules — without requiring a pre-computed lookup table for every word form.

> **Status:** Active development. Morphology ~65–70% complete. Phonology component functional.

---

## Features

- **Noun declension** — all 7 cases, singular and plural, masculine/feminine/neuter
- **Adjective declension** — including degrees (positive, comparative, superlative)
- **Pronoun inflection** — personal, possessive, demonstrative, and others
- **Verb conjugation** — present/past/future tenses, conditional, imperative, passive voice, negation, reflexives
- **Phonological transformations** — vowel alternations, epenthesis, softening
- **Rule-based generation** — forms generated from rules + overrides, not lookup tables

---

## Installation

Add the `Grammar.Czech` project to your solution and reference it from your project.

```csharp
// In your .csproj
<ProjectReference Include="..\Grammar.Czech\Grammar.Czech.csproj" />
```

---

## Quick Start

### 1. Register services

```csharp
using Grammar.Czech;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCzechGrammarServices("path/to/data/");

var provider = services.BuildServiceProvider();
```

The `dataPath` argument points to the folder containing the JSON data files (noun patterns, verb patterns, irregulars, etc.).

### 2. Decline a noun

```csharp
var composer = provider.GetRequiredService<CzechWordFormComposer>();

var request = new CzechWordRequest
{
    Lemma      = "student",
    WordCategory = WordCategory.Noun,
    Gender     = Gender.Masculine,
    Pattern    = "pán",
    IsAnimate  = true,
    Number     = Number.Singular,
    Case       = Case.Genitive,
};

var form = composer.GetFullForm(request);
Console.WriteLine(form.Form); // → "studenta"
```

### 3. Conjugate a verb

```csharp
var request = new CzechWordRequest
{
    Lemma      = "dělat",
    WordCategory = WordCategory.Verb,
    Aspect     = VerbAspect.Imperfective,
    Pattern    = "dělá",
    Tense      = Tense.Present,
    Number     = Number.Singular,
    Person     = Person.First,
    Modus      = Modus.Conjunctive,
};

var form = composer.GetFullForm(request);
Console.WriteLine(form.Form); // → "dělám"
```

### 4. Inflect an adjective

```csharp
var request = new CzechWordRequest
{
    Lemma      = "mladý",
    WordCategory = WordCategory.Adjective,
    Gender     = Gender.Masculine,
    Number     = Number.Singular,
    Case       = Case.Dative,
    Degree     = Degree.Possitive,
};

var form = composer.GetFullForm(request);
Console.WriteLine(form.Form); // → "mladému"
```

---

## API Reference

### `CzechWordRequest`

The central request struct. All properties are optional unless required for the given `WordCategory`.

| Property | Type | Description |
|----------|------|-------------|
| `Lemma` | `string` | Dictionary form of the word (required) |
| `WordCategory` | `WordCategory` | Noun, Adjective, Pronoun, Verb, Numerale |
| `Pattern` | `string?` | Declension/conjugation pattern (e.g. `"pán"`, `"žena"`, `"dělá"`) |
| `Gender` | `Gender?` | Masculine, Feminine, Neuter |
| `Number` | `Number?` | Singular, Plural |
| `Case` | `Case?` | Nominative–Instrumental (for nouns, adjectives, pronouns) |
| `Person` | `Person?` | First, Second, Third (for verbs) |
| `Tense` | `Tense?` | Present, Past, Future |
| `Aspect` | `VerbAspect?` | Perfective, Imperfective |
| `Modus` | `Modus?` | Conjunctive, Conditional, Imperative, Indicative |
| `Voice` | `Voice?` | Active, Passive |
| `Degree` | `Degree?` | Possitive, Comparative, Superlative (adjectives) |
| `IsAnimate` | `bool?` | Animacy (masculine nouns — affects accusative) |
| `IsNegative` | `bool` | Negated form (e.g. `"nedělám"`) |
| `HasReflexive` | `bool?` | Reflexive particle (`se`/`si`) |
| `HasExplicitSubject` | `bool?` | Affects word order in conditional phrases |
| `VerbClass` | `VerbClass?` | Czech verb class 1–5 (optional hint) |
| `AdditionalData` | `string?` | Reserved for future use |

---

### Enums

#### `WordCategory`
```csharp
Noun | Adjective | Pronoun | Numerale | Verb
```

#### `Gender`
```csharp
Masculine | Feminine | Neuter
```

#### `Number`
```csharp
Singular | Plural
```

#### `Case`
```csharp
Nominative | Genitive | Dative | Accusative | Vocative | Locative | Instrumental
```

#### `Person`
```csharp
First | Second | Third
```

#### `Tense`
```csharp
Present | Past | Future
```

#### `Modus`
```csharp
Conjunctive | Conditional | Imperative | Indicative
```

#### `VerbAspect`
```csharp
Perfective | Imperfective
```

#### `Voice`
```csharp
Active | Passive
```

#### `Degree`
```csharp
Possitive | Comparative | Superlative
```

#### `VerbClass`
```csharp
Class1 | Class2 | Class3 | Class4 | Class5
```

---

### `CzechWordFormComposer`

The main high-level entry point.

| Method | Returns | Description |
|--------|---------|-------------|
| `GetFullForm(CzechWordRequest)` | `WordForm` | Returns the complete inflected/conjugated form, including verb phrases (passive, conditional, reflexive) |

### `MorphologyEngine`

Lower-level service. Use directly if you need raw morphology without phrase building.

| Method | Returns | Description |
|--------|---------|-------------|
| `GetForm(CzechWordRequest)` | `WordForm` | Noun, adjective, or pronoun inflection |
| `GetBasicForm(CzechWordRequest)` | `WordForm` | Verb infinitive / base form |

> **Prefer `CzechWordFormComposer.GetFullForm()`** for verbs — it assembles the full phrase (auxiliary verbs, particles, negation, reflexives). Use `MorphologyEngine` directly only for nouns, adjectives, and pronouns when you need the raw form.

### `WordForm`

```csharp
public class WordForm
{
    public string Form { get; } // The resulting word form string
}
```

---

## Supported Patterns

### Noun patterns
`pán`, `hrad`, `muž`, `stroj`, `předseda`, `soudce`, `žena`, `růže`, `píseň`, `kost`, `město`, `moře`, `kuře`, `stavení`

### Verb patterns
`dělá`, `prosí`, `kupuje`, `maže`, `nese`, `peče`, `tiskne`, `mine`, `kryje`, `být`

### Pronoun lemmas
`já`, `ty`, `on`, `ona`, `ono`, `my`, `vy`, `oni`, `ona_` (ony/ona), `můj`, `tvůj`, `jeho`, `její`, `náš`, `váš`, `jejich`, `sebe`, `ten`, `kdo`, `co`, `jenž`

---

## Architecture

```
Grammar.Core/          # Language-agnostic models and interfaces
  Enums/               # Case, Gender, Number, Tense, etc.
  Interfaces/          # IWordRequest, IInflectionService<T>, IPhonologyService<T>, ...
  Models/Word/         # WordRequest, WordForm, WordStructure

Grammar.Czech/         # Czech-specific implementation
  Models/              # CzechWordRequest
  Services/
    MorphologyEngine           # Routes requests to noun/adjective/pronoun/verb services
    CzechWordFormComposer      # Full form assembly including verb phrases
    CzechNounDeclensionService
    CzechAdjectiveDeclensionService
    CzechPronounService
    CzechVerbConjugationService
    CzechPhonologyService      # Vowel alternations, softening, epenthesis
    CzechAuxiliaryVerbService  # být forms
    CzechVerbPhraseBuilderService  # Passive, conditional, reflexive phrase construction
    CzechNegationService
    CzechPrefixService
    CzechParticleService
    CzechPrepositionService
  Data/                # JSON files: patterns, irregulars, pronouns, verbs, particles, ...
  CzechGrammarServiceFactory   # AddCzechGrammarServices() extension method

Grammar.Czech.Cli/     # Console demo app
```

### Key design principles

- **Rule-based generation** — inflected forms are computed, not looked up. Only metadata, rules, and an overrides layer for irregulars are required.
- **Lexical data over heuristics** — grammatical facts (animacy, mobile vowels, pattern) are stored explicitly in data files, not inferred at runtime. See `HasMobileVowel` in `NounPattern` and `substantive_irregular.json` as a concrete example.
- **Separation of concerns** — phonological transformation (how to change a form) is decoupled from the lexical decision (whether to apply it). Evaluator interfaces (`ISofteningRuleEvaluator`, `IEpenthesisRuleEvaluator`) are separate from the phonology service.
- **Dependency injection throughout** — all services are registered via `AddCzechGrammarServices()` and resolved through `IServiceCollection`.
- **Language-agnostic core** — `Grammar.Core` contains no Czech-specific logic. Adding support for another language means implementing the interfaces in a new project.

---

## Contributing

### Prerequisites
- .NET 8+
- Visual Studio 2022 or Rider

### Project structure

New language support should be added as a separate project referencing `Grammar.Core` and implementing:
- `IInflectionService<TRequest>`
- `IPhonologyService<TRequest>`
- `IWordStructureResolver<TRequest>`

### Data files

Grammar data lives in `Grammar.Czech/Data/` as JSON. Patterns, irregular forms, verb tables, pronouns, particles, and prepositions are all data-driven. When adding new words with irregular behavior, add overrides there rather than adding code branches.

A key example of this principle is **mobile vowels** — nouns like *pes*, *den*, *otec* drop a vowel in oblique cases (*pes → psa*, *den → dne*). This is modeled as a `bool HasMobileVowel` on `NounPattern` and stored in `substantive_irregular.json`:

```json
"pes": { "hasMobileVowel": true, "inheritsFrom": "pán" },
"den": { "hasMobileVowel": true, "inheritsFrom": "hrad" },
"otec": { "hasMobileVowel": true, "inheritsFrom": "muž" }
```

The phonology service (`RemoveMobileVowel`) only performs the transformation. Whether to apply it is a lexical decision — stored in data, not inferred from the word's shape.

### Tests

The project uses MSTest with data-driven test patterns. Tests live in `Grammar.Czech.Tests/` (and `Grammar.Core.Tests/`).

---

## Roadmap

- [ ] Valency dictionary — explicit lemma metadata store (eliminates remaining heuristics)
- [ ] NLG / sentence construction — generate grammatically correct Czech sentences from semantic input
- [ ] Improved GitHub documentation
- [ ] Latin language support
- [ ] NuGet package

---

## License

See [LICENSE](LICENSE) for details.
