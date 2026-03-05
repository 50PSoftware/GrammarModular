# Grammar.Czech

![Status](https://img.shields.io/badge/status-active%20development-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)

**A rule-based Czech morphology engine for .NET.**

Most tools that deal with Czech word forms work by looking up pre-computed tables. Grammar.Czech takes a different approach — it *generates* inflected forms dynamically from linguistic rules, a lemma, and grammatical metadata. No pre-computed database required.

This makes the engine well-suited for scenarios where you need to inflect arbitrary words at runtime — such as procedurally generated NPC dialogue in games, or document automation where word forms cannot be pre-enumerated.

---

## Features

- **Noun declension** — all 7 cases, singular and plural, all genders and patterns
- **Adjective declension** — hard/soft patterns, degrees (positive, comparative, superlative), possessive adjectives
- **Pronoun inflection** — personal, possessive, demonstrative, reflexive, relative
- **Verb conjugation** — present/past/future tenses, conditional, imperative, passive voice, negation, reflexives
- **Phonological transformations** — vowel alternations (quantity), mobile vowel (pohybné e), epenthesis, consonant softening
- **Rule-based generation** — forms generated from rules + JSON overrides, not lookup tables
- **Language-agnostic core** — `Grammar.Core` has no Czech-specific logic; adding another language means adding a new project

---

## Use Cases

- **Game development** — grammatically correct NPC dialogue, item descriptions, and quest text in Czech
- **Language learning tools** — on-demand paradigm generation for any lemma
- **Document automation** — legal, administrative, or HR documents requiring correct inflection
- **NLP research** — lightweight morphology component for Czech language pipelines

---

## Installation

Add `Grammar.Czech` to your solution and reference it from your project.

```xml
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
var composer = provider.GetRequiredService<CzechWordFormComposer>();
```

### 2. Decline a noun

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

### 3. Conjugate a verb

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

### 4. Inflect an adjective

```csharp
var request = new CzechWordRequest
{
    Lemma        = "mladý",
    WordCategory = WordCategory.Adjective,
    Gender       = Gender.Masculine,
    Number       = Number.Singular,
    Case         = Case.Dative,
    Degree       = Degree.Possitive,
};

var form = composer.GetFullForm(request);
Console.WriteLine(form.Form); // → "mladému"
```

---

## API Reference

### `CzechWordRequest`

The central request object. Properties are optional unless required for the given `WordCategory`.

| Property | Type | Description |
|---|---|---|
| `Lemma` | `string` | Dictionary form of the word (required) |
| `WordCategory` | `WordCategory` | Noun, Adjective, Pronoun, Verb, Numerale |
| `Pattern` | `string?` | Declension/conjugation pattern (e.g. `"pán"`, `"žena"`, `"dělá"`) |
| `Gender` | `Gender?` | Masculine, Feminine, Neuter |
| `Number` | `Number?` | Singular, Plural |
| `Case` | `Case?` | Nominative–Instrumental |
| `Person` | `Person?` | First, Second, Third (verbs) |
| `Tense` | `Tense?` | Present, Past, Future |
| `Aspect` | `VerbAspect?` | Perfective, Imperfective |
| `Modus` | `Modus?` | Indicative, Conditional, Imperative |
| `Voice` | `Voice?` | Active, Passive |
| `Degree` | `Degree?` | Possitive, Comparative, Superlative |
| `IsAnimate` | `bool?` | Animacy — affects accusative of masculine nouns |
| `IsNegative` | `bool` | Produce negated form (e.g. `"nedělám"`) |
| `HasReflexive` | `bool?` | Append reflexive particle `se`/`si` |
| `HasExplicitSubject` | `bool?` | Affects word order in conditional phrases |
| `VerbClass` | `VerbClass?` | Czech verb class 1–5 (optional hint) |

### `CzechWordFormComposer` — main entry point

| Method | Returns | Description |
|---|---|---|
| `GetFullForm(CzechWordRequest)` | `WordForm` | Complete inflected/conjugated form, including verb phrases |

### `MorphologyEngine` — lower-level access

Use directly when you need raw morphology without phrase assembly (nouns, adjectives, pronouns).

| Method | Returns | Description |
|---|---|---|
| `GetForm(CzechWordRequest)` | `WordForm` | Noun, adjective, or pronoun inflection |
| `GetBasicForm(CzechWordRequest)` | `WordForm` | Verb base form |

---

## Supported Patterns

**Noun patterns**
`pán`, `hrad`, `muž`, `stroj`, `předseda`, `soudce`, `žena`, `růže`, `píseň`, `kost`, `město`, `moře`, `kuře`, `stavení`

**Verb patterns**
`dělá`, `prosí`, `kupuje`, `maže`, `nese`, `peče`, `tiskne`, `mine`, `kryje`, `být`

**Pronoun lemmas**
`já`, `ty`, `on`, `ona`, `ono`, `my`, `vy`, `oni`, `ona_`, `můj`, `tvůj`, `jeho`, `její`, `náš`, `váš`, `jejich`, `sebe`, `ten`, `kdo`, `co`, `jenž`

---

## Architecture

```
Grammar.Core/               # Language-agnostic models and interfaces
  Enums/                    # Case, Gender, Number, Tense, VerbAspect, ...
  Interfaces/               # IInflectionService<T>, IPhonologyService<T>, ...
  Models/                   # WordRequest, WordForm, WordStructure, Phoneme

Grammar.Czech/              # Czech-specific implementation
  Models/                   # CzechWordRequest
  Services/
    MorphologyEngine                  # Routes requests by WordCategory
    CzechWordFormComposer             # Top-level entry point; assembles full forms
    CzechNounDeclensionService        # Noun paradigms
    CzechAdjectiveDeclensionService   # Adjective paradigms
    CzechPronounService               # Pronoun paradigms
    CzechVerbConjugationService       # Verb paradigms
    CzechPhonologyService             # Vowel alternations, softening, epenthesis
    CzechAuxiliaryVerbService         # být auxiliary forms
    CzechVerbPhraseBuilderService     # Passive, conditional, reflexive construction
    CzechNegationService              # Negation prefix and být negation
    CzechPrefixService                # Perfective prefixes, negative prefix
    CzechParticleService              # Conditional particles, reflexives se/si
    CzechPrepositionService           # Preposition–case validation
  Data/                     # JSON: patterns, irregulars, verbs, pronouns, particles, ...
  CzechGrammarServiceFactory          # AddCzechGrammarServices() extension method

Grammar.Czech.Cli/          # Console demo app
Grammar.Czech.Test/         # MSTest unit and data-driven tests
```

### Key design principles

- **Rule-based generation** — inflected forms are computed from rules + a minimal overrides layer, not looked up from a full database
- **Lexical data over heuristics** — grammatical facts (animacy, mobile vowels, pattern) are stored explicitly in JSON, not inferred at runtime
- **Separation of concerns** — phonological *transformation* is decoupled from the *decision* to apply it (`ISofteningRuleEvaluator`, `IEpenthesisRuleEvaluator`)
- **Dependency injection throughout** — all services registered via `AddCzechGrammarServices()` and resolved through `IServiceCollection`
- **Language-agnostic core** — `Grammar.Core` contains no Czech-specific logic; new language support means a new project implementing the core interfaces

---

## Known Limitations

- **Iotation not implemented** — consonant+`ě` combinations after labials (`p`, `b`, `m`, `v`) are not yet handled. Affects a small subset of words (e.g. certain forms of `zem`). Intentionally deferred.
- **Guess heuristics not implemented** — `GuessGenderAndPattern()` and `GuessVerbAspect()` are stubbed. Callers must supply pattern and aspect explicitly. This will be superseded by the planned valency dictionary.
- **No numerals yet** — numeral inflection is planned but not started.
- **No sentence generation** — the engine produces individual word forms. Full NLG (sentence construction from semantic input) is on the roadmap.

---

## Roadmap

### Next
- [ ] Complete pronoun data coverage
- [ ] Numeral inflection
- [ ] Valency dictionary — explicit per-lemma metadata (gender, pattern, aspect, animacy) — eliminates the need for guess heuristics

### Future
- [ ] NLG / sentence construction — generate grammatically correct Czech sentences from semantic input
- [ ] Iotation
- [ ] Latin language support
- [ ] NuGet package

---

## Contributing

### Prerequisites
- .NET 8+
- Visual Studio 2022 or Rider

### Adding a new language

Add a new project referencing `Grammar.Core` and implement:
- `IInflectionService<TRequest>`
- `IPhonologyService<TRequest>`
- `IWordStructureResolver<TRequest>`

### Adding irregular words

Grammar data lives in `Grammar.Czech/Data/` as JSON. When adding a word with irregular behaviour, add an override entry there — do not add code branches.

Example — mobile vowels (`pes → psa`, `den → dne`, `otec → otce`):

```json
"pes": { "hasMobileVowel": true, "inheritsFrom": "pán" },
"den": { "hasMobileVowel": true, "inheritsFrom": "hrad" },
"otec": { "hasMobileVowel": true, "inheritsFrom": "muž" }
```

### Tests

MSTest with data-driven test patterns. Tests live in `Grammar.Czech.Test/`.

---

## License

MIT — see [LICENSE](LICENSE.txt) for details.
