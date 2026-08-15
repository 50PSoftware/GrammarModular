# Grammar.Czech

![Status](https://img.shields.io/badge/status-active%20development-orange)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-Proprietary-red)

**English** | [Čeština](README.cs.md)

**A generative morphology library for Czech, on .NET 8.**

The project generates Czech word forms from a lemma, grammatical categories, a pattern and JSON rules, and builds sentences and complex sentences on top of them. It is not a general dictionary of ready-made forms. For most words the caller has to supply explicit metadata — word class, pattern, gender/number/case or the verbal categories; at sentence level a clause is described as a predicate plus constituents with functors, not as a finished word order.

## Table of contents

- [Requirements and installation](#requirements-and-installation)
- [What the project does today](#what-the-project-does-today)
  - [Nouns](#nouns)
  - [Adjectives](#adjectives)
  - [Pronouns](#pronouns)
  - [Numerals](#numerals)
  - [Adverbs](#adverbs)
  - [Verbs](#verbs)
  - [Phonology and orthography](#phonology-and-orthography)
  - [Lexicon and valency](#lexicon-and-valency)
  - [Sentences and complex sentences](#sentences-and-complex-sentences)
  - [Particles](#particles)
  - [Interjections](#interjections)
- [Architecture](#architecture)
- [Quick start](#quick-start)
- [Examples](#examples)
- [CLI](#cli)
- [Tests](#tests)
- [Data layer](#data-layer)
- [Known limitations](#known-limitations)
- [License](#license)

## Requirements and installation

- **.NET 8 SDK** — every project targets `net8.0`.
- `Grammar.Czech` depends on `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Logging`.
- `Grammar.Czech.Cli` depends on `System.CommandLine` and packs as a .NET tool named `gramatika` — see [CLI](#cli).

The project packs itself: `GeneratePackageOnBuild` is on, and a build drops `50PSoftware.GrammarModular.Czech.<version>.nupkg` next to the assembly. The package is not on nuget.org — it is consumed from a private or local feed, or through a project reference:

```bash
dotnet build Grammar.sln
```

```bash
dotnet add reference ../Grammar/Grammar.Czech/Grammar.Czech.csproj
```

The version is still a `-preview`, so a package reference needs to allow prerelease versions.

The rule data ships as embedded resources inside `Grammar.Czech`, so no rule files need to be copied next to the assembly. The lexicon is the exception and is supplied by the deployment — see [Lexicon and valency](#lexicon-and-valency).

## What the project does today

### Nouns

`Grammar.Czech` declines nouns by the patterns stored in `Grammar.Czech/Data/Rules/Nouns/patterns.json`.

Supported patterns:

| Gender / group | Patterns |
|---|---|
| masculine animate | `pán`, `občan`, `syn`, `muž`, `učitel`, `král`, `předseda`, `turista`, `soudce` |
| masculine inanimate | `hrad`, `les`, `stroj` |
| feminine | `žena`, `růže`, `píseň`, `kost` |
| neuter | `město`, `moře`, `kuře`, `stavení` |

Patterns can inherit endings through `inheritsFrom`; `les`, for instance, inherits from `hrad` and overrides only the cases that differ. The five masculine animate sub-patterns all use it for the same thing, a nominative and vocative plural the base vzor does not have: `učitel`, `občan` and `turista` give `-é` to the *-tel*, *-an* and *-ista/-ita* suffix classes (*učitelé*, *občané*, *turisté*), and `syn` and `král` give `-ové` to the lexically defined class that takes it (*synové*, *biologové*, *králové*). Softening rules are inherited along with endings — a rule named for `pán` governs `občan` and `syn` too — so a sub-pattern does not have to restate the palatalization its base already describes. Irregular nouns live in `Grammar.Czech/Data/Rules/Nouns/irregulars.json` (*oko*, *dům*, *ruka*, *noha*, *ucho* and the like). The file `Grammar.Czech/Data/Rules/Nouns/propers.json` is empty — the mechanism for proper names exists, the data does not.

### Adjectives

The supported patterns are `mladý`, `jarní`, `otcův` and `matčin`, from `Grammar.Czech/Data/Rules/Adjectives/patterns.json`.

`CzechAdjectiveDeclensionService` handles:

- declension by gender, number, case and animacy,
- pattern guessing through `GuessAdjectivePattern`,
- comparative and superlative through `Degree`,
- suppletive comparatives for `dobrý`, `malý`, `velký`, `zlý`, `špatný` and `dlouhý`.

### Pronouns

Pronouns are read from `Grammar.Czech/Data/Rules/Pronouns/patterns.json`, their paradigms from `Grammar.Czech/Data/Rules/Pronouns/paradigms.json`.

The data covers personal, possessive, reflexive, demonstrative, interrogative, relative, negative and indefinite pronouns. The service supports fixed tabulated forms, paradigms, indeclinable pronouns, and selected pronominal forms delegated to adjectival declension.

Some pronouns are two words wearing one spelling, and the readings differ in the type itself: *co* asks in *co čteš?* and introduces a relative clause in *člověk, co přišel*, and so do *kdo* and *jaký*. The entry in the file is the primary reading and the alternatives hang off it in `alsoReads`, exactly as for conjunctions. `GetPronounType` returns the primary reading and is unchanged; a caller that knows which construction it is building asks `GetReadings` instead, which is what `CzechWordOrderResolver` does when it renders a relative clause.

A reading carries its own `inflectionClass` and states it even where it matches the primary one: omitted, it would not mean "the same" but `Substantive`, the first value of the enum. Nor is it always the same — two words under one spelling can differ in how they inflect:

- **interrogative *co*** declines (*co, čeho, čemu*), while **relative *co*** is indeclinable. It expresses its role in the clause not by its form but by a resumptive pronoun inside that clause — *člověk, co jsem ho viděl* — which is why NESČ calls it an analytic relativizer and says structures with *co* always contain a resumptive. In the nominative that pronoun is null, so *člověk, co přišel* comes out whole; other cases are refused, because the resumptive is not modelled. Stylistically it is colloquial rather than substandard — Havránek and Jedlička do not exclude it from the written language.
- **relative *kdo*** relativizes an entity rather than a property of a noun, so it leans on a demonstrative: *ten, kdo přišel*. NESČ does not list it among the relativizers of a clause with a nominal head, and `requiresPronominalHead` is that fact in the data. Without it *student, kdo přišel* would build — *kdo* has forms for the masculine animate, so no agreement check would object.
- **relative *čí*** possesses like *jehož* but takes its head like *kdo*. NESČ divides the two groups on exactly that: "Rel *čí* v RV s lehkou hlavou, *jehož*, *jejíž*, *jejichž* v RV s nominální hlavou." The three exist because they carry two gender-number features at once — the possessor's from the antecedent and the possessed noun's from inside the clause — whereas *čí* expresses only the possessor and is one word for every gender. So it declines with the noun it possesses and has nothing to select: *ten, čího studenta vidím*.
- **relative *jaký***, by contrast, behaves like *který*: it agrees with the head noun in gender and number and declines on the *mladý* pattern.

There are three possessive relative pronouns and they are not three of the same case: IJP has *jehož* and *jejichž* as indeclinable, while *jejíž* declines like *její* on the *jarní* pattern, with the suffix after the ending (*jejíhož*, *jejímuž*, *jejíchž*). They agree in two directions at once, and each direction decides something different: the antecedent's gender and number pick **which of the three words** it is — masculine or neuter singular *jehož*, feminine *jejíž*, plural *jejichž* — while the form itself follows the **noun possessed**, because the pronoun is its agreeing attribute.

That is how it is modelled: as an attribute. `PlannedRelative.Possessed` names by functor the participant of the relative clause the pronoun belongs to, and the planner puts it into that participant's `Modifiers`. From there the same agreement handles it that handles *mladý* in *mladý student*, and `CzechRoleResolver` reserves no slot for it — the slot is held by the noun possessed, which takes its case from its own role. The whole constituent opens the relative clause, because the pronoun in it does, so it becomes the theme and the clitics follow it: *žena, jejíhož studenta jsem viděl*. `CzechWordOrderResolver` checks the choice of word against the antecedent and refuses a mismatch: all three are valid words, so a wrong choice would otherwise reach the surface as a well-formed sentence about something else.

The post-preposition variant is available through `CzechWordRequest.IsAfterPreposition`.

### Numerals

Numerals are read from `Grammar.Czech/Data/Rules/Numerals/patterns.json`, their paradigms from `Grammar.Czech/Data/Rules/Numerals/paradigms.json`. The data covers every kind `NumeralType` names — cardinal, ordinal, sortal, set, aggregate, multiplicative, fractional and group — plus the indefinite numerals, which cut across those kinds rather than forming one of their own. Distributive numerals (*po dvou*) are a construction rather than a lexical entry, and `CzechNumeralComposer` builds them.

`CzechNumeralService` picks a strategy by `NumeralMorphology`: shared paradigms (*jeden*, *dva*, *oba*, *tři*, *čtyři*), the rule for the two-form 5–99 paradigm, delegation to adjectival and to nominal declension, and indeclinable entries. Doublets (*tří/třech*, *tisíc/tisíců*) and the dual forms of paired body parts (*třema rukama*) are selected through `NumeralFormOptions`.

`CzechNumeralComposer` spells a number out with every part declined — `365` in the instrumental gives *třemi sty šedesáti pěti* — and handles the irregular multiples of a hundred (*dvě stě*, *tři sta*, *pět set*). Compound numerals 21–99 offer all three variants listed by IJP id=792 through `CompoundVariant`: *dvacet jedna žáků* (the default), *dvacet jeden žák*, and the contracted *jedenadvacet*. `ComposeOfType` names a value with a numeral of any kind — `5` becomes *pět*, *pátý*, *paterý*, *patery*, *patero* or *pětkrát*.

Agreement of the counted noun is carried by `CardinalAgreement` and applied by `CzechSentenceBuilder`: *pět studentů bylo* against *tři studenti byli*. This is the one place where agreement runs from the attribute to its head noun rather than the other way round.

Ordinals have contracted forms too: `ComposeOrdinal` with `CompoundVariant.Contracted` gives *pětadvacátý* and *stopadesátý*, and where no contracted form exists it returns the uncontracted one instead of failing.

Non-integers are spelled out as well — `ComposeFraction` turns `3/4` into *tři čtvrtiny* and `5/8` into *pět osmin*, `ComposeDecimal` turns `3.14` into *tři celé čtrnáct setin*. Both the word *celá* and the denominator are counted nouns, so they follow the numeral in front of them: *jedna celá*, but *pět celých*.

A numeral can also be given in digits. It stays indeclinable, but its agreement is derived from the value — and a decimal follows the fraction, so it takes the genitive singular: *1,5 metru*, *14,25 sekundy*.

A numeral need not be an attribute; as the head of a constituent it declines normally and governs nothing (*Pět bylo.*).

Numerals written in digits are checked by `ICzechNumeralOrthographyService` — it rejects *5tý*, *10ti* and *20-krát*, and can correct them.

### Adverbs

NESČ classifies adverbs as an uninflected word class, so the positive degree is the lemma itself and comparison is the only morphology they have. An unregistered adverb therefore passes through unchanged in the positive degree — it does not have to be in the data to be usable in a sentence.

The comparative, though, is read from `Grammar.Czech/Data/Rules/adverbs.json` rather than derived. Deadjectival adverbs take three different suffixes — `-o`, `-e/-ě` and `-y` (*nízko*, *krásně*, *česky*) — the choice between them is not fully predictable, and one adjective can yield two adverbs that compare differently (*dlouho* and *dlouze*, *vysoko* and *vysoce*). Comparison itself is irregular in the frequent adverbs (*dobře → lépe*, *špatně → hůře*, *brzy → dříve*), and the regular `-eji/-ěji` suffix interacts with palatalization in some stems (*hladce → hladčeji*) but not others (*hustě → hustěji*). A rule would be wrong quietly and often.

The superlative is `nej-` on the comparative. Several irregulars carry a doublet whose shorter member is the colloquial one (*hůř* beside *hůře*, *dřív* beside *dříve*); it is selected with `CzechWordRequest.PrefersShortForm`.

An unregistered adverb has its comparative derived: `-ěji` after `d`, `t`, `n` and the labials, `-eji` elsewhere, with the palatalization `-ce`, `-ky` and `-ho` bring. Measured against the comparatives in the data, the rule reproduces every regular one and misses only the irregulars the ÚJČ reference lists as such — the two partition without overlap, which is what makes deriving safe. A test measures that rather than assuming it, so the rule and the data cannot drift apart unnoticed. A registered comparative always wins, and an adverb registered without one is taken to be uncompared rather than derived.

The adjective an adverb comes from is recorded rather than computed, for the same reason, and `ICzechAdverbService.GetAdverbsFor` reads the mapping back — returning both members where an adjective yields two. Relative adverbs (*kde*, *kdy*, *kam*, *jak*) introduce a relative clause through `RelativeAttachment.Relativizer`, the field that used to be called `Pronoun`; being uninflected, they take no case and nothing agrees with the antecedent through them. Negative adverbs (*nikdy*, *nikde*, *nijak*) are lemmas of their own, not the positive ones with a prefix.

A comparative built a different way is not the same thing as a clipping, and the data keeps them apart: *snadno* takes both *snáze* and *snadněji*, *hluboko* both *hlouběji* and *hloub*. Generation picks the primary; `ICzechAdverbService.GetComparativeVariants` shows the rest. One adjective can also yield two adverbs that are separate lemmas sharing a comparative — *dlouho* and *dlouze*, *vysoko* and *vysoce*, *těžko* and *těžce*, *široko* and *široce*, *úzko* and *úzce* — and both members are registered.

### Verbs

Verbs are generated from the rules in:

- `Grammar.Czech/Data/Rules/Verbs/patterns.json` — the general classes `trida1` through `trida5`, whose stems are derived from the infinitive, plus the named pattern `dojme`,
- `Grammar.Czech/Data/Rules/Verbs/irregulars.json` — entries with explicitly listed stems. These are not only irregular verbs such as `být`, `mít`, `chtít`, `moci` and `vědět`; the classic named patterns `nese`, `bere`, `maže`, `peče`, `umře`, `tiskne`, `mine`, `kryje`, `kupuje`, `prosí` and `dělá` live here too.

`CzechVerbConjugationService` generates the basic forms for the indicative, the conditional, the imperative, the past tense, the present/future tense and the passive participle. On top of that, `CzechWordFormComposer` assembles some verb phrases: the periphrastic future of imperfectives, the passive with an auxiliary, the conditional, negation, and the reflexive `se`/`si`.

The verb pattern is passed through `Pattern` — either a class (`trida1`–`trida5`) or a pattern name from `irregulars.json`. Alternatively `VerbClass` can sometimes be passed, and it maps onto `trida1`–`trida5`. `GuessVerbClass` implements a simple heuristic based on the infinitive ending, but it is not reliable for every Czech verb.

### Phonology and orthography

The project contains a phonological layer for softening, epenthesis, jotation and vowel quantity. The decisions are separated into evaluators; the transformations are carried out by `CzechPhonologyService` and `CzechOrthographyService`.

The parts in public use include:

- `IPhonemeRegistry` / `CzechPhonemeRegistry`,
- `ISofteningRuleEvaluator<CzechWordRequest>`,
- `IEpenthesisRuleEvaluator<CzechWordRequest>`,
- `IAlternationRuleEvaluator<CzechWordRequest>`,
- `IJotationRuleEvaluator<CzechWordRequest>`,
- `ISyncretismRuleEvaluator<CzechWordRequest>`,
- `ICzechOrthographyService`.

`CzechAlternationRuleEvaluator` shortens the stem before the genitive plural ending. Whether a noun shortens is lexical, not phonological — *kráva* gives *krav* where *káva* gives *káv* — so the decision comes from `has_genitive_plural_shortening` on the entry, with `HasGenitivePluralShortening` on the request overriding it.

The phoneme registry then vetoes what cannot shorten regardless of the flag, so a wrongly filled entry cannot invent a form. Only *á*, *í* and *ou* shorten; *é*, *ó*, *ý* and *ú/ů* keep their length (*sféra* → *sfér*, *móda* → *mód*, *rýha* → *rýh*). Shortening also fails when a consonant cluster follows the long vowel: *brázda* → *brázd*. The veto counts phonemes rather than letters, which is why *moucha* → *much* passes — *ch* is one phoneme where *zd* is two.

### Lexicon and valency

`SqliteValencyProvider` reads `Grammar.Czech/Data/Lexicon/grammar.czech.lexicon.db`, a SQLite database. It is the one data source here that is not embedded JSON, because it is the part meant to grow into thousands of entries; the rule files under `Data/Rules/` describe closed classes and stay as they are.

The dictionary is edited centrally on a server, and this file is the local read-only copy pulled from it. Identifiers are assigned by the server and carried over unchanged — a copy that renumbered could never be compared against the server it came from again.

The dictionary is **not carried inside the NuGet package**, and that is deliberate: a word added on the server is not a reason to release the library, and a consumer should not have to wait for one. The package ships the code; the deployment supplies the data and can replace it without rebuilding anything.

A consumer names its copy in one of three ways, tried in this order:

```csharp
services.AddCzechGrammarServices(@"D:\data\grammar.czech.lexicon.db");   // explicit
```

```
GRAMMAR_CZECH_LEXICON=/srv/grammar/grammar.czech.lexicon.db               // environment
```

…or simply puts `grammar.czech.lexicon.db` beside the application, which is where the provider looks last. With none of them set it throws at startup naming all three, rather than behaving like a dictionary that happens to be empty.

Separating them means the two can drift, so `SqliteValencyProvider` reads `schema_version` when it opens the file and refuses a lexicon written for a schema it does not read. That check belongs in the library rather than only in the tool's validator, since a package consumer has the library and not the tool.

The database holds three layers, kept apart because one lemma has exactly one morphological identity but a lexeme may have several senses and each sense a frame per diathesis:

- `lemma_entry` — morphological identity per dictionary form: gender, pattern, animacy, the mobile `e`, genitive-plural shortening and epenthesis, indeclinability, plural-only, countability, verb class, aspect and its counterpart, reflexive type, plus provenance columns,
- `lexeme` and `lexical_unit` — the abstract word and its senses. An aspect pair is one lexeme, so `dát` and `dávat` share a single frame instead of each carrying a copy,
- `lemma_sense` — what holds of one heslo read in one sense, which neither of the two above can say: `lemma_entry` is one row for the word under every sense, `lexical_unit` one row for the sense under every word of the lexeme. Rows are the exceptions only,
- `lemma_variant` — a second standard spelling of a headword, so *setmět* is understood and *setmít* is what comes out,
- `valency_frame`, `valency_slot`, `slot_realization` — the frames themselves. A slot may have several realizations with a generation preference, which is what lets one slot be a bare case in one wording and a `že`-clause or an infinitive in another.

A slot may surface as a dependent clause, and `slot_realization.clause_type` records which conjunction introduces it — `že`, `aby`, `zda` — as the lemma itself, the way VALLEX does. The word carries more than the kind of clause would: *ví, že přijde* and *ví, zda přijde* are both content clauses and mean different things. No `CHECK` can enforce it, because the conjunctions live in the embedded JSON rules; `lexikon validate` does.

A light verb construction is a predicate whose meaning sits in a noun while the verb contributes little beyond tense, and `construction` records it because its valency is not the verb's. *Mít* governs an accusative and nothing else, yet *mít zájem* governs *o* with the accusative as well — reading it through the possess frame leaves *o knihu* unaccounted for. The slots live in `template_json` in the same shape `valency_slot` and `slot_realization` use, so a construction reads as the frame it becomes; the noun is one of them, under `CPHR`, the way the Prague Dependency Treebank annotates the nominal half of such a predicate.

```csharp
var constructions = provider.GetRequiredService<ICzechConstructionService>();

constructions.Find("mít", ["student", "zájem", "kniha"]);   // LVC.mít.zájem
constructions.Find("mít", ["student", "kniha"]);            // null
```

The pair is what is recognized, so the construction never leaks into ordinary uses of the same verb: *Student má zájem o knihu* and *Student má knihu* are built from different frames. The seeded patterns are of the `mít zájem o`, `dávat pozor na` and `mít strach z` kind, and the inventory is a corpus job rather than a memory one.

The schema in `Grammar.Czech.Lexicon.Tool/Schema/schema.sql` is deliberately portable SQL — SQLite is the first backend, and MySQL, Microsoft SQL or Firebird are meant to take the same DDL. Everything SQLite-specific sits in `schema.sqlite.sql`, and `schema.mysql.sql` is the server's variant: the same tables with `AUTO_INCREMENT` and, importantly, binary collation on every column that is matched rather than read — the usual `utf8mb4_0900_ai_ci` is accent-insensitive and would make `dát` and `dat` the same string.

`Grammar.Czech.Lexicon.Tool` maintains the file:

| command | what it does |
|---|---|
| `pull --url <api>` | downloads the dictionary from the API and replaces the local copy |
| `validate` | reports what a bad row broke — a frame with no actor, a slot that can never surface, two frames of one verb both marked default, a `lemma_key` no lookup will match, a vzor no pattern data knows |
| `build` | creates a lexicon from the schema and the seed, for working without a server |
| `dump --out <sql>` | writes the database out as portable `INSERT`s, for review |
| `export-json --out <dir>` | writes the same JSON the API serves, for seeding the server |

```bash
dotnet run --project Grammar.Czech.Lexicon.Tool -- pull --url https://example.com/api/ --token <token>
```

`Grammar.Czech.Lexicon.Tool` packs as a .NET tool, so it can be installed once and used from anywhere:

```bash
dotnet tool install -g 50PSoftware.GrammarModular.LexiconTool --prerelease
lexikon pull
```

Settings come from three places, in this order: a command-line argument, `lexikon.json` in the working directory or any parent, and the environment. Each answers a different question — an argument is what you want this once, the file is what the project always wants and belongs in version control, the environment is what this machine knows. That is also where the token lives: commit the file with the address and the destination, keep `LEXICON_API_TOKEN` in the environment, and a key absent from the file falls through rather than overriding it with a placeholder. See `lexikon.json.example`.

A relative `database` in the file is relative to the file, not to wherever the tool was invoked — the file is searched for up the directory tree precisely so it can be used from anywhere below it.

The tool package does not contain the dictionary either. It is the thing that fetches it.

A pull writes to a temporary file and only moves it into place once `validate` passes, so a failed or interrupted download leaves the working lexicon untouched.

A pull is paged, and paging is not a consistent snapshot — nothing stops an edit between one page and the next. `validate` is what catches the result, as a broken reference rather than as a word that quietly fails to resolve later.

#### Filling a request from the lexicon

Nothing is required beyond the lemma for a word the dictionary holds. `CzechLexiconEnricher` runs in `MorphologyEngine` ahead of the dispatch and fills in whatever the request left unsaid — the word class, gender, pattern, animacy, the phonological flags, verb class, aspect, reflexive type.

Ahead of the dispatch, because the word class is one of the things it fills and is also what selects the inflection service. `WordCategory` is therefore nullable: an enum's default is one of its members, and for this one that was `Noun`, so a request that never mentioned a class was a claim rather than a gap — `dát` reached the declension service, had its vzor filled from the lexicon correctly as `trida5`, and failed with *Noun pattern 'trida5' not found*.

It only ever writes where the request holds `null`, so a stated pattern wins even when the lexicon disagrees, and `HasMobileE = false` stays false rather than being replaced by the entry. That distinction is why the flags are nullable: `false` is the caller saying the word has no mobile e, `null` is the caller not saying, and only the second is a gap. A word the lexicon has never heard of goes through untouched and inflects from what the caller passes, which is the ordinary case — most of Czech is not in the dictionary and never will be.

An entry is used only when its word class matches the one asked about, because filling a request about *stát* the verb from the row for *stát* the country would not complete it but answer a different question. Where the request states a class, the enricher looks the lemma up in that class; where it does not, it asks by lemma alone and takes whichever row it finds — which is the one case a lemma held under two classes can still come back as the wrong one.

The lexicon serves mainly as a metadata provider for selected resolvers; it is not a complete dictionary of Czech.

A valency frame states how a given verb's arguments are realized, and `CzechSentenceBuilder` takes both case and preposition from it: for `vidět` the `PAT` is accusative, for `dávat` the `ADDR` is dative and the `PAT` accusative, for `jít` the `DIR3` is the preposition `do` with the genitive. A case set explicitly is left alone — the frame only fills the gaps.

A verb with several senses is disambiguated through `CzechClause.FrameLabel`, because `jít` takes different arguments as motion than as a process. Where one sense is marked default in the dictionary, an unlabelled call gets it — `dát` is transfer unless the caller says konzumace. Where none is, as with `jít` and the three senses of `být`, the call throws instead of picking one; the dictionary is allowed to settle the ambiguity, the code is not.

Inner participants (`ACT`, `PAT`, `ADDR`, `ORIG`, `EFF` — the FGD actants) can only be licensed by a frame, so `vidět` with an addressee throws. Free modifications combine with any verb, and there the caller supplies the case.

Functors follow the FGD reading rather than the intuitive one, which matters in two places that look like they want `COMPL` and do not: the infinitive a modal verb governs is its `PAT` (*chce jít*, *může přijít*), and so is the non-verbal part of the predicate under the copula, per the tectogrammatical manual of the PDT. `COMPL` is the doplněk with its double dependency — the optional infinitive of `pomoci`, where the one who carries is the one being helped, so its control points at `PAT` rather than at `ACT`.

Which verbs can stand in the periphrastic passive is read off the frame. A sense with a passive frame of its own is licensed by having one; for the rest the test is an actor plus one more actant that the passive could lift into the subject. An infinitive is not one — the patient of `moci` is the infinitive it governs, and *\*je mohnut jít* is not a sentence — and neither is a copular predicate, which is refused on the frame's `kind` because its patient comes in both the nominative and the instrumental and neither case gives it away.

### Sentences and complex sentences

`CzechSentenceBuilder` composes a surface sentence out of clauses. A clause (`CzechClause`) is a predicate plus constituents (`ClauseElement`) with a functor and a communicative status; it **carries no word order** — that is derived.

The builder handles:

- **agreement** of the predicate with a nominative actor,
- **information structure** — contrastive to the front, theme before the verb, rheme after it,
- the **Wackernagel position** of the clitic cluster: auxiliary, reflexive, short dative, short accusative, seated after the first constituent of the clause. The cluster moves as a whole and follows the **first** constituent, not all preverbal ones (*Klára se večer učí*),
- the **contracted forms** `ses`, `sis` and the conditional `by ses`, `by sis`,
- **phrasal constituents** — an attribute inherits gender, number, case and animacy from its head wherever it left them unset,
- **prepositional phrases**, including vocalization and a government check; the whole phrase is one constituent,
- **complex sentences** — `Coordination` and `Subordination` over `SentenceNode`, nestable to any depth, punctuated by the conjunction,
- **relative clauses** — the pronoun agrees with the head noun in gender, number and animacy, and takes its case from its own role in the dependent clause. The clause is a sentence in its own right and may coordinate or carry a dependent clause; one pronoun is the subject of everything coordinated with it, so the antecedent's agreement reaches every conjunct — *žena, která přišla a odešla* — and stops at a subordinator, which opens a clause with a subject of its own.

A subordinating conjunction and a relative pronoun occupy the first position of their clause, so the cluster follows them: *protože se student učil*, *muž, kterého jsem viděl*. A coordinating conjunction stands outside the clause and leaves the first position to it.

Conjunctions are a closed class in `Grammar.Czech/Data/Rules/conjunctions.json`; an unknown conjunction throws, because both the comma and the clitic position depend on its type. Each entry also carries the relation it marks, in the NESČ groups — *slučovací*, *odporovací*, *stupňovací*, *vylučovací*, *příčinné*, *důsledkové* on the coordinating side, and *časové*, *příčinné*, *přípustkové*, *podmínkové*, *účelové*, *obsahové*, *srovnávací* on the subordinating one — and the second member where the conjunction is paired (*buď – nebo*, *ani – ani*, *nejen – ale i*, *jak – tak*, *sice – ale*, *jednak – jednak*). Doubles exist only among coordinators.

Those groups are the traditional taxonomy of dependent clauses under other names, so the lookup runs both ways. `ICzechConjunctionService.GetConjunctionsFor` takes a type and a relation and returns the conjunctions that express it — ask for a subordinating *Purpose* and get *aby*, for *Causal* and get *protože* ahead of *poněvadž*, *jelikož* and *ježto*. The caller states the kind of clause it wants instead of knowing which word introduces it. Both readings are searched, so a subordinating *Comparison* also finds *jak*, whose primary reading is coordinating, ranked after those that are comparative to begin with; the data lists the unmarked choice first, so taking the first result is the neutral one.

Some conjunctions are two words under one spelling and the readings differ in the type itself: *ať* subordinates a content clause in *Řekni mu, ať přijde* and coordinates the split *ať už přijde osobně, nebo vyšle zástupce*; *jak* coordinates the paired *jak Petr, tak Pavel* and subordinates a comparison in *udělej to, jak jsem řekl*; *než* is temporal in *než přišel* and comparative in *vyšší, než jsem čekal*. The entry in the file is the primary reading and the others hang off it in `alsoReads`. A bare lookup gives the primary one; a caller that knows which construction it is building asks for the reading by type, which is what the builder does — coordination asks for the coordinating reading, subordination for the subordinating one, and neither guesses.

`Coordination.Paired` builds the split construction: the conjunction opens the first conjunct and its correlate joins every one after it. The correlate always takes a comma, even where the bare word takes none — the ÚJČ rule is that one is written before the second connective whatever the conjunction would do alone, so paired *nebo* and *ani* are punctuated against their commaless plain use. It is asked for rather than inferred, because the same word serves both constructions: *Petr ani Pavel nepřišli* coordinates with a bare *ani*, *Ani nepřišel, ani nezavolal* splits it.

`aby` and `kdyby` are not stored as paradigms. NESČ analyses them as containing the conditional auxiliary and agreeing with the subject through it, so the twelve forms are composed from a stem and the particles already in `clitics.json`: *abych*, *abys*, *aby*, *abychom*, *abyste* and the *kdyby* row beside it. The builder takes the person from the dependent clause and suppresses the conditional particle there, since the auxiliary moved into the conjunction rather than being copied. Composing rather than storing also rules out the widespread \*aby jsi and \*aby jste for free — the conditional has no *jsi* to contribute.

`však` is placed after the first constituent and behind the clitic cluster, while *avšak* stays clause-initial. How far behind is where the sources stop being definite: NESČ counts *však* among the *nestálá klitika* rather than the *klitika tantum*, so it holds no rank in the obligatory cluster, and no test claims the other order is wrong.

`requiresComma` is a default rather than a fact about the word. For *a*, *i*, *ani*, *nebo* and *či* the comma follows from the relation between the conjuncts and not from the conjunction, so `Coordination.RequiresComma` is the real answer where the caller knows it.

### Particles

The word class *částice*, in `Grammar.Czech/Data/Rules/particles.json` — not the clitics, which live in `clitics.json`. A particle is uninflected and is not compared, so there is nothing to build: the lemma is the form and the service answers what the particle does and where it may stand.

NESČ carries three competing classifications rather than one settled inventory. The types here follow the functional-semantic one it attributes to Nekula in PMČ — *modální*, *intenzifikační*, *vytýkací*, *modifikační*, *odpověďové*, *negační*, *přací* — with *strukturující* and *emocionální* from the MČ 2 scheme for what that one leaves out.

One rule comes out of it: a modifying particle cannot stand in the rheme, which NESČ states of the whole group. `ClauseElement.Status` is what says which constituent is the rheme, so the builder checks it rather than merely documenting it. What the optative particles demand of the predicate is deliberately *not* recorded — *ať přijde* is a plain third-person present, Czech having no third-person imperative, and the source states no mood government for the group.

A clause-initial particle (*ať*, *kéž*, *nechť*, *nuže*) fills the first position of its clause like a subordinating conjunction, so the cluster follows it: *Ať se student dělá*. A particle scoping over one constituent goes on that constituent instead and opens it from outside any preposition: *jen pro studenta*.

Homonymy with the adverbs and the conjunctions is expected rather than a fault in the data. The boundary is drawn by function in context — *klidně* is a particle in *Klidně seď* and an adverb in *Seď klidně, nevrť se* — so *ať* stands in both this file and `conjunctions.json`, and *bohužel* in this one and `adverbs.json`. A test asserts it, so it is not tidied away later as duplication.

### Interjections

The lemmas in `Grammar.Czech/Data/Rules/interjections.json` fall into the four NESČ types — *emocionální*, *kontaktová*, *apelová*, *zvukomalebná* — with the line that source draws across them: the first three are subjective and the fourth objective. No morphology; an interjection is uninflected and forms, in that description, the most primitive sentence type there is.

The punctuation is a rule and not data. An interjection is set off by a comma except where it stands in for a clause member, so the same word goes both ways — *Kamarádi, hurá, vyhráli jsme* against *Palicí buch ho po hlavě* — and `ICzechInterjectionService.RequiresComma` takes the use rather than only the word. What *is* recorded per word is which interjections can be a predicate at all, because it does not follow from the type: *hop* is *apelové* and predicative all the same. Those also carry the verb they form (*žbluňk → žbluňknout*), which NESČ notes as their entering word formation directly.

The class is more open than any other here — onomatopoeia is coined on the spot — so an unregistered lemma passes through instead of being reported, the opposite of the closed conjunction inventory. Reduplication is recorded only where the source names it (*oj oj oj*, *ťuk(y) ťuk* as optional, *bubu* as obligatory); unmarked means unrecorded, not known not to repeat.

`CzechClause.Interjection` places one outside the clause behind its comma, leaving the first position alone. The terminator stays the caller's: an emotional interjection tends towards an exclamation mark, but tending is not a rule.

## Architecture

```text
Grammar.sln
|-- Grammar.Core/               language-independent enums, interfaces and models
|-- Grammar.Czech/              the Czech implementation: services, providers, embedded JSON rules and the lexicon database
|-- Grammar.Czech.Cli/          the `gramatika` client application: lemmas in, a sentence out
|-- Grammar.Czech.Lexicon.Tool/ pulls, builds, validates and dumps the lexicon database; holds the schemas
`-- Grammar.Czech.Test/         MSTest tests for declension, conjugation, phonology and sentence building
```

The main DI registration is `AddCzechGrammarServices()` in `Grammar.Czech/CzechGrammarServiceFactory.cs`.

Building a sentence runs through six stages, each a separate service and each testable on its own:

```text
SentencePlan                  what is to be said: a predicate, participants, an intent
   |  CzechRoleResolver       which participant fills which role, when the caller did not say
   v
SentencePlan
   |  CzechSentencePlanner    the sense of the verb, the subject, pro-drop, what is old information
   v
CzechClause
   |  CzechClausePlanner      is the slot a word, an infinitive, or a dependent clause?
   v
CzechClause / Subordination
   |  CzechMicroplanner       the frame governs the case, the numeral rewrites it, the predicate agrees
   v
PlannedClause
   |  CzechWordOrderResolver  functional sentence perspective, then Wackernagel's second position
   v
words
   |  CzechSentenceBuilder    joins the clauses, writes the commas, closes the sentence
   v
sentence
```

The split follows what each stage is allowed to change. Above `PlannedClause` the words are still being decided; below it only their order is, which is what lets Czech word order vary freely without any form moving with it. `CzechSentenceBuilder` remains the entry point for a clause you have already built, and owns the recursion, because a clause can contain a sentence — a relative clause hangs off a constituent.

`CzechRoleResolver` is a stage of its own rather than part of the planner for the same reason: it is the only one that guesses. The planner takes roles as given and refuses a participant without one, so everything worked out by inference stays where it can be inspected and overruled before it becomes a sentence.

### Starting from a plan

```csharp
var roles = provider.GetRequiredService<CzechRoleResolver>();
var planner = provider.GetRequiredService<CzechSentencePlanner>();

var plan = new SentencePlan
{
    Predicate = new CzechWordRequest { Lemma = "dávat", Pattern = "trida5", WordCategory = WordCategory.Verb },
    Participants = [Student, Woman, Book],   // no functors stated
};

Console.WriteLine(builder.Build(planner.Plan(roles.Resolve(plan))));
// Student dává ženě knihu.
```

The roles come off the frame: the actor and the addressee prefer an animate noun, which is what keeps the two objects of a transfer verb apart without anyone naming either. A participant nothing accounts for keeps a null functor and comes back from `CzechRoleResolver.Unresolved`, because a wrong role produces a well-formed sentence that means something else.

A relative clause is a plan too, so nothing inside one is stated by hand — the roles of its participants come off its own verb's frame, and it may be a complex sentence in its own right:

```csharp
var subject = Student with
{
    Relative = new PlannedRelative
    {
        Relativizer = "který",
        Clause = new SentencePlan
        {
            Predicate = Verb("psát"),
            Participants = [Letter],                       // no functor stated
            Joined = [new ClauseLink("a", Working)],
        },
    },
};
// Student, který píše dopis a pracuje, čte knihu.
```

The relative pronoun holds one role inside its clause and is not among the participants, so the resolver reserves the slot its case points at: with a nominative *který* the letter is the patient rather than the actor. The pronoun is also what the clause is about, so nothing inside it becomes the theme by default — *který píše dopis* and not *který dopis píše*.

### Sentences with no subject expressed

Czech leaves the subject out in three different ways, and they are different things rather than one:

```csharp
// Nothing to express: the verb has no actor at all.
planner.Plan(new SentencePlan { Predicate = Verb("pršet") });          // Prší.

// Dropped: the actor is a pronoun the ending already carries.
planner.Plan(new SentencePlan { Predicate = Verb("číst"), Participants = [Me, Book] });
// Čtu knihu.

// Unnamed: nobody is saying who, and the person is stated on the verb.
planner.Plan(new SentencePlan
{
    Predicate = Verb("psát") with { Person = Person.Third, Number = Number.Plural },
    Participants = [Letter],
});
// Dopis píšou.
```

The model tells them apart by what is in the plan, not by the surface. A first or second person on the verb is agreement with a subject that was not expressed, so the actor slot counts as taken and a noun cannot fill it — without that, *píšu dopis* would come out with the letter as the actor, in the nominative.

Whether a verb can have a subject at all is a lexical fact, so it lives in the dictionary: a frame of kind `Impersonal` has no slots, and a participant offered to one is refused rather than declined into *Prší student.* A verb the dictionary does not hold keeps its old freedom, since it is the frame that says so.

Being impersonal belongs to a sense and not to a verb, which is why it is a frame and not a flag on the entry. *Mrzne* and *Voda mrzne* are two senses, so `mrznout` carries two frames and the weather one is the default, because that is what the bare verb means:

```bash
gramatika veta mrznout                       # Mrzne.
gramatika veta voda mrznout --ramec freeze   # Voda mrzne.
gramatika veta voda mrznout
# Sloveso 'mrznout' je bezpodměťové — 'voda' k němu nepatří a věta s ním nevznikne.
# Jiný význam podmět bere: --ramec freeze.
```

Seeded: `pršet`, `sněžit`, `svítat`, `stmívat` as impersonal only; `mrznout`, `hřmít`, `blýskat` with a second sense that takes an actor; `stmívat` and `blýskat` with the reflexive their weather sense needs. Every one of them has its perfective counterpart — `napršet`, `nasněžit`, `rozednít`, `setmít`, `zmrznout`, `zahřmět`, `blýsknout` — sitting under the same lexeme and inheriting the frames rather than carrying a copy: *Napršelo*, *Zmrzlo*, *Blýsklo se*. A counterpart inherits every sense, so `zmrznout` reaches both *Zmrzlo* and *Voda zmrzla*.

What kind of event a verb names is a separate question from its aspect, and `lemma_entry.aktionsart` is where it goes — způsob slovesného děje, the whole twenty-six-group classification of NESČ. Twenty-six and not twenty-five: the groups are lettered with the Czech alphabet, where *ch* stands between *h* and *i*.

It is not a finer grade of aspect. Aspect is grammatical, has two members and every Czech verb has one; this is lexical and most verbs are in none of the groups, so `null` means unclassified rather than "none". Where a verb is classified the group implies the aspect — NESČ states it of the whole list at once, *slovesa skupin (a)–(r) jsou dok., zatímco slovesa skupin (s)–(y) jsou nedok.* — and `AktionsartFacts.RequiredAspect` is that rule. `lexikon validate` holds every classified row against it, so a semelfactive marked imperfective is a bad row rather than an unusual verb.

The entries that carry it are of this kind: `blýsknout` semelfactive against the frequentative `blýskat`, `zahřmět` ingressive, `napršet` and `nasněžit` cumulative, `pršet`, `sněžit` and `hřmít` decursive, `svítat` and `stmívat` mutative, `setmít` and `zmrznout` resultative, `rozednít` evolutive.

`mrznout` carries none on the entry, and that is a decision rather than a gap: *mrzne* is a state of the air and *voda mrzne* a gradual change of the water, which are different groups, while the entry has one row. It is the counterexample to the usual description of způsob slovesného děje as a property of the verb — the verb has no one answer and each of its readings does.

Which is what `lemma_sense` is for. It pairs one heslo with one sense and states the group there, overriding the entry the way `valency_frame.reflexive_type` overrides the reflexive type on it; where there is no row, the entry stands. `mrznout` has two rows, stative for *mrzne* and mutative for *voda mrzne*, and `ValencyFrame.Aktionsart` is where they come back.

The pairing is the point, and the obvious shortcut does not work: a sense hangs off the lexeme, a lexeme is an aspect pair, so a group written on `lexical_unit` would land on `zmrznout` too. *Zmrzlo* is a result reached under either reading — which is what its entry already says — and a perfective verb inheriting *mrzne*'s stative would be simply false. The validator is what caught that, by holding the row against the aspect of the lemma it belongs to.

The other sense of `blýskat` shows a patient in the bare instrumental, which is a whole class of Czech verbs — *mávat rukou*, *kroutit hlavou*, *házet kamenem*: `Meč blýská očima.`

Such a verb has nothing to agree with, so its participle goes into the neuter singular: *Pršelo*, not the masculine the default would otherwise give.

Whether *prší* has a subject position at all is a question the sources leave open — NESČ sets the traditional *věty bezpodměté* beside the generative reading with an unpronounced expletive, *(Ono) prší* — and an empty frame does not settle it. What it states is only that no word the caller supplies can be that subject, which both readings agree on and which is the whole of what a generator needs.

Two decisions the planner makes that nothing below it could:

```csharp
// Pro-drop: the ending already carries the person, so the pronoun is emphasis rather than the
// neutral sentence. The agreement it was carrying moves onto the predicate as it goes.
planner.Plan(plan with { Participants = [Me, Book] });                       // Čtu knihu.
planner.Plan(plan with { Participants = [Me, Book], AllowSubjectDrop = false }); // Já čtu knihu.

// Perspective: asking for the patient to be the subject asks for the passive, which is a frame of
// its own — the agent drops to the instrumental — and makes the patient the theme as well, since a
// passive that left the agent in front would have gained nothing over the active.
planner.Plan(plan with { Perspective = FgdFunctor.PAT });
// Kniha je dávána studentem ženě.
```

The main entry points:

- `CzechSentenceBuilder` for a sentence or a complex sentence built from clauses,
- `CzechWordFormComposer` for the full form of a word or a verb phrase,
- `MorphologyEngine` for direct dispatch by word class — all ten of them. Nouns, adjectives, pronouns, numerals, adverbs and the basic verb forms are built; prepositions, conjunctions, particles and interjections come back as their lemma, which is the whole of their morphology rather than a stub. What those four *do* in a sentence is answered by their own services, which the sentence builder consults directly. This is what `IInflectionService<CzechWordRequest>` and `IVerbInflectionService<CzechWordRequest>` resolve to, being the only implementation that accepts a request of any word class,
- the specialized services `CzechNounDeclensionService`, `CzechAdjectiveDeclensionService`, `CzechPronounService`, `CzechNumeralService` and `CzechVerbConjugationService`.

Alongside them, supporting services are registered and can be resolved from the container directly:

| Service | What it is for |
|---|---|
| `ICzechPrepositionService` | preposition government and vocalization |
| `ICzechConjunctionService` | conjunction type and comma rule |
| `ICzechValencyService` | valency frame lookup for a verb |
| `ICzechNumeralOrthographyService` | checking and correcting numerals written in digits |
| `CzechNumeralComposer` | numbers spelled out, including fractions and decimals |
| `CzechAuxiliaryVerbService`, `CzechVerbPhraseBuilderService` | auxiliaries and compound verb forms |
| `INegationService<CzechWordRequest>` | negation |
| `ICzechParticleService`, `ICzechPrefixService` | particles and prefixes |
| `CzechWordStructureResolver` | splitting a lemma into prefix and stems |

## Quick start

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

A verb:

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

## Examples

Every output below has been verified against the current code.

### A sentence from a clause

A clause carries a predicate and constituents; the builder derives the word order itself from the communicative status. The reflexive settles into second position, right after the first constituent.

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

A prepositional phrase is a single constituent; the preposition is vocalized against the word that follows, and its government is checked.

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

### Complex sentences

A subordinating conjunction occupies the first position of the dependent clause, so the clitic follows it. A coordinating conjunction stands outside the clause and leaves the first position to the first constituent.

```csharp
var mainClause = new CzechClause { Predicate = works, Elements = [student] };
var subClause = new CzechClause { Predicate = studied };

Console.WriteLine(builder.Build(new Subordination(mainClause, "protože", subClause)));
// Student dělal, protože se učil.

Console.WriteLine(builder.Build(new Coordination("ale", [mainClause, subClause])));
// Student dělal, ale učil se.
```

### A slot filled by a proposition

Some verbs take a whole event where another takes a thing. Which shape it comes out in is not the caller's to say — the frame records it, so the same call produces an infinitive for one verb and a dependent clause for another.

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

The infinitive has no subject of its own — it is structurally excluded — so the frame records which participant it corefers with: whoever wants is whoever goes. State a different subject and the sentence is refused, because Czech has no infinitive for it and the construction is an *aby*-clause instead.

Its clitics climb into the governing clause, where the one cluster of the clause is:

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

The dependent clause keeps the tense it was given. Czech has no sequence of tenses, so *věděl, že čte* stays present — backshifting it would report something else.

### Numerals spelled out

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

Checking numerals written in digits:

```csharp
var orthography = provider.GetRequiredService<ICzechNumeralOrthographyService>();

orthography.IsValid("5tý", out var reason); // false; reason explains why, in Czech
orthography.Normalize("5tý");               // 5.
orthography.Normalize("20-krát");           // 20krát
```

Agreement of the counted noun propagates all the way to the predicate:

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

// predicate: být, 3rd person singular, past tense
Console.WriteLine(builder.Build(new CzechClause
{
    Predicate = wasPredicate,
    Elements = [countedSubject],
}));
// Bylo pět studentů.
```

### Valency

A constituent with no case takes one from the predicate's valency frame — for `vidět` the `PAT` is accusative:

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
        // no Case — the frame fills it in
    },
    FgdFunctor.PAT);

Console.WriteLine(builder.Build(new CzechClause
{
    Predicate = sees,
    Elements = [subject, patient],
}));
// Studentka vidí studenta.
```

A verb with several senses and no default among them asks for a choice by throwing; the sense is then named through `FrameLabel`:

```csharp
new CzechClause
{
    Predicate = goes,          // jít — senses motion and process, neither default
    Elements = [subject, toSchool],
    FrameLabel = "motion",
};
```

`být` is the same case with three: `copula_nominal` for *lev je králem zvířat*, `copula_adjectival` for *Petr je veselý*, `existence` for *je tam problém*. Each is a different `ValencyKind`, and a frame is one per sense and diathesis, so they cannot share one. `dát` is the other case — `transfer` is marked default, so an unlabelled clause gets it.

### An adjective with degree

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

`Grammar.Czech.Cli` is the `gramatika` client application. Give it lemmas and it assembles a sentence from them: the verb becomes the predicate, the valency frame from the dictionary hands out the roles and the cases to the rest, and whatever the dictionary does not settle is inferred from the ending.

```bash
dotnet run --project Grammar.Czech.Cli -- veta student číst kniha
```

Before it prints anything it shows how it read the input and lets that be corrected — which is why this is a confirm step rather than a one-shot command. A misassigned role produces a well-formed sentence about something else, and that is worse than a question.

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

The `zdroj` column is there for the difference between an answer and a guess: a pattern from the dictionary is as good as the dictionary, `pravidla` is a closed class (pronouns, prepositions) and `odhad` is the tool's own proposal from the ending. A case marked `(rámec)` is not on the request at all — the builder fills it from the verb — which is why it disappears the moment a case is stated outright.

The tool works none of this out itself: it calls `CzechRoleResolver` and `CzechSentencePlanner`, the same code a library consumer gets. What the review shows is the planner's result rather than a second opinion beside it.

The subject pronoun is kept where the library would drop it — printing fewer words than it was given would look like losing one. `--vypustit-podmet` asks for the neutral Czech sentence:

```bash
gramatika veta já číst kniha                     # Já čtu knihu.
gramatika veta já číst kniha --vypustit-podmet   # Čtu knihu.
```

Every question the dialog asks has a switch that answers it in advance, and both write to the same place, so a session can be rewritten as a single command. That is also what makes the tool usable in a script, where there is nobody to ask: `--bez-dotazu` turns an open question into an error naming the switch that settles it, and `--json` adds the analysis to the sentence.

```bash
gramatika veta Klára dávat žena kniha --role kniha=PAT --cas minulý --bez-dotazu
gramatika veta student jít --ramec motion --json
```

A word the dictionary does not hold is now told apart from a word it holds in another form, which used to be the same silent guess. `učitele` is not accepted as input — the tool builds sentences from lemmas and does not read Czech — but it is recognized as a form of `učitel` and said so, instead of becoming a feminine noun of the *růže* pattern in a sentence that looked almost right. The index behind that is generated from the same inflection services that would produce the forms in a sentence, so it cannot disagree with them.

A word that is a form of nothing known is genuinely new, and gets written down. `:slova` lists what has accumulated, `:slova doplnit` walks through it and records what a person confirmed:

```text
> zahradník kopat záhon
  ! 'zahradník' slovník nezná a není to ani tvar ničeho, co zná.
    Zapsal jsem ho mezi návrhy na doplnění slovníku.
```

It cannot write to the dictionary and does not try. The SQLite file is a read-only replica of the central copy, identifiers are handed out by the server, the API only reads, and the next `lexikon pull` overwrites the local file whole — a row inserted here would live until then and be gone, which is a feature that silently discards its own result. So the collected words go in a file of their own, and the other half is a command on the tool that does own the dictionary:

```bash
lexikon navrhy --jen-potvrzene
```

That reads the list and writes a draft `seed.NNN.sql`. A draft rather than a seed: identifiers are left blank because the server assigns them, `source` is left empty because provenance is what the licensing here rests on and a word that turned up in a session has none until somebody looks it up, and the header asks for the one thing that cannot be generated — what was left out and why.

Run with no arguments it opens a session instead, which is the shape sentence-building actually has: a sentence gets poked at — another tense, another information structure, another case — and every poke used to be a fresh process and the word `veta` typed again.

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

A line with an equals sign corrects the sentence that stands; bare words start a new one. That is the one rule, and it is stated rather than guessed at.

What is said about the predicate holds until the end of the session and applies to the sentences after it — `:stav` shows what is in force and `:zapomen` clears it. What is said about a *word* does not: the next sentence has other words in those positions, so carrying `3 pad=dativ` over would silently be about something else.

The session stands beside `veta` rather than replacing it. `veta` is still one command, answerable in advance and usable from a script; the session is for the poking.

Terms can be asked about by name, in either mode: `? role` explains what functors are and why a role is stated rather than a case, `? cleneni` why dané and nové decide the word order, and `? odhad` what the tool makes up and how the display says so.

All ten word classes reach a finished sentence. The last two to get there were the particle and the interjection, and not because they were hard to recognize: there was no functor to give them. Neither is a clause member — Czech grammar says *bez větněčlenské platnosti* — so no valency frame hands them a role, and the twenty-five functors this project had were all participants or circumstances. Forcing one on them recorded that *asi* answers "how", which it does not.

The Prague Dependency Treebank has functors for exactly this, and they are now in `FgdFunctor`: `MOD` for modality, `RHEM` for a rhematizer, `ATT` for attitude, `PREC` for tying a sentence to the one before, and `PARTL` for a word standing outside the structure of the clause.

Neither class needs the dictionary for it. The rule data already sorts particles into the nine groups of Nekula's classification and the treebank sorts the same ground into functors, so lining the two up is a rule — `Modal` is `MOD`, `Focusing` is `RHEM`, `Intensifying` is `EXT`, `Response` and `Negative` are `PARTL`. An interjection is `PARTL` from being an interjection, with nothing to look up.

```bash
gramatika veta student číst kniha ano   # Student čte knihu ano.
gramatika veta student číst kniha ach   # Student čte knihu ach.
```

A word that is both an adverb and a particle still reads as an adverb, so `asi` asks for a circumstance the dictionary does not record for it rather than inventing one. `--druh asi=castice` settles it and the group supplies `MOD`.

Recognizing the class is not the same as being able to place the word. An adverb is not a valency slot, so no frame hands it a role, and until the dictionary could say what circumstance an adverb expresses, every adverb stopped as an open question. `lemma_entry.inherent_functor` is where that now lives:

```bash
gramatika veta student číst kniha dnes   # Student čte knihu dnes.
```

It cannot be derived. The ending says nothing, and neither does the adjective behind the adverb — *rychlý* and *rychle* are one word in two classes and only one of them answers "how". So it is recorded per word.

It lives in the dictionary rather than beside the adverbs in the embedded `adverbs.json` for the reason the verb stems give: correcting it should be an edit in the dictionary, not a release of the library. The two files now say different things about the same word — the JSON how it compares, which is morphology, the dictionary what it means for a sentence, which is a fact about the word. An adverb in the JSON and not in the dictionary behaves exactly as it did before: recognized, and the caller states the role.

Practically every adverb the rule data holds carries one. A good share of them rest on a rule rather than on anybody's judgement: a deadjectival adverb answers "jak", and `adverbs.json` records what each was derived from, so *pečlivý* → *pečlivě* → MANN needs no deciding. The rest were gone through one at a time, which is what the seed headers set out — mostly TWHEN, EXT, LOC and DIR3, then MOD and RHEM, with a tail of DIR1, DIR2, ATT, ACMP, CAUS and PREC.

A few are deliberately absent: `blízko`, `dokonce`, `jak`, `naproti`, `sotva`, `tak`, `uvnitř` and `vedle`. Each is also a preposition or a conjunction, and an entry would take that away — the enricher fills the word class from the dictionary before the closed-class checks run, so *vedle knihy* would become an adverb and stop governing the genitive. A test holds both halves of that: every other adverb has a functor, and these have no entry.

The judgement calls are named where they were made. `dlouho` is TWHEN rather than THL because the column holds one answer and TWHEN is the reading that does not lie in the other; `vpravo` is LOC rather than DIR3 because *je vpravo* against *jdi vpravo* is decided by the verb; `prakticky` is EXT by how it is used rather than MANN by how it looks.

Seven of the ten word classes are closed — no new preposition or conjunction gets coined — and live in the rule data beside the dictionary rather than in it. The dictionary carries nouns, adjectives and verbs; pronouns, prepositions and conjunctions were already read off the rules, and adverbs, particles, interjections and numerals now are too. Before that they fell through to the guess from the ending, which knows infinitives and adjective endings and calls everything else a noun.

```bash
gramatika veta student číst pět kniha   # Student čte pět knih.
```

They are tested in a fixed order — pronoun, preposition, conjunction, numeral, adverb, particle, interjection — because the classes overlap and something has to win: *vedle* is a preposition and an adverb, *tak* a conjunction and an adverb and an interjection, *dost* an adverb and a numeral. The four new tests run last, so nothing that already worked reads differently.

Between the new four the order is a choice rather than a fact. Forty-nine words are both an adverb and a particle — *dobře*, *jistě*, *asi*, *prý* — and the adverb wins: an adverb can be a constituent and a particle cannot, so calling *dobře* a particle would take it out of the sentence, while *asi* read as an adverb behaves exactly as a particle would, both being uninflected. Deciding it word by word would need a list of words in the code, and lexical facts belong in the dictionary.

Which is what `--druh` is for, and it is the last word over everything above:

```bash
gramatika veta student číst asi --druh asi=castice --role asi=MANN
```

`--stupen` reaches the comparison the library already had and the tool never exposed: `--stupen rychle=druhy` gives *rychleji*, and `--stupen dobre=treti` gives *nejlépe* — a registered form rather than a derived one, since the rule would have said *dobřeji*. Stated on a class that does not compare it changes nothing and says so, rather than being a switch that quietly does nothing.

A verb the dictionary holds no frame for still produces a sentence. Without one the role resolver has no slots to hand out, every constituent stays roleless and nothing comes of it — and since the dictionary carries frames for sixty verbs, that used to be the ordinary outcome rather than the rare one. The tool falls back on the unmarked Czech order: actor, patient, and an addressee after them if it is animate.

```bash
gramatika veta učitel darovat kniha student   # Učitel daruje knihu studentovi.
```

The roles it invented are marked with `*` in the review and reported underneath, and the case goes with them: a role without a case leads nowhere, and where the frame is missing both are the same guess. Anything stated wins, so `--role zahrada=LOC` is dealt around rather than over.

What word order cannot know is meaning. *zahrada* in *pes běhat zahrada* is a place, not a patient, and neither order nor animacy says so — only the verb would. It comes out as *Pes běhá zahradu*, marked, and is corrected with `--role`. This lives in the tool rather than in the library on purpose: a library that invented valency silently would be lying to whoever built on it.

A preposition governing several cases is reported as an open case rather than an open role, because that is what it is: *v zahradě* and *v zahradu* are where and whither, and the library reads the role off the preposition once the case is settled.

Lemmas may be written without diacritics. `ucitel` finds `učitel`, and the completed spelling is reported, because the sentence will contain a word nobody typed:

```bash
gramatika veta ucitel psat dopis student   # Učitel píše dopis studentovi.
```

Folding is a fallback, never a first choice: an exact spelling always wins, and one that folds onto several lemmas — `být` and `byt` are different words — is a question rather than a decision. Switches fold too, so a word can be corrected under either spelling: `--pad ucitel=dativ` reaches `učitel`.

What it does not accept is a whole sentence in one argument. Lemmas are separate arguments, and `veta "učitel psát dopis student"` says so — it used to reach the library and come back as *Verb pattern 'učitel psát dopis student' not found*, which is an English sentence about inflection patterns for someone who put the quotes in the wrong place.

The dictionary does not ship inside the tool package, the same as with the library package. Its path is resolved in the order the lexicon tool uses — `--slovnik` for this one run, then `lexikon.json`'s `database` key for the project, then `GRAMMAR_CZECH_LEXICON` for the machine, then the application directory. When it is nowhere, the tool says so at startup and lists all four.

`lexikon.json` is the same file the lexicon tool reads, so a project says where its dictionary is once and both tools find it. The path is taken relative to that file rather than to the working directory, and the file is looked for by walking up, so it works from any subdirectory. Only the `database` key is read here; the address and the token belong to the tool that talks to the API.

A verb repeated in the second conjunct is left out: `Student čte knihu a žák dopis.` The PDT manual (§12.1.1.1) treats the governing verb as elided where it is clear from the preceding clause which verb was left out — *(Jirka navštívil Marii.) Honza Jiřinu.* — and reconstructs it by copying that node, `#EmpVerb` being reserved for the grammatical ellipsis where it cannot be recovered at all.

What licenses it here is a harder test than the language needs, because a generator has to be sure rather than plausible: the lemma, tense, mood, voice, aspect, polarity and reflexivity must match, both conjuncts must have remnants to stand parallel, and the predicate must come out as a single word. Person and number may differ — they are carried by the subject that stays, which is what lets *já piju kávu a ona čaj* work at all. The single-word condition is what keeps the clitic auxiliary out of trouble: in the first and second person of the past tense it carries the tense and the person, and where to put it once the participle is gone is not something this project has established. `ClauseLink.AllowVerbEllipsis` turns it off for the contrastive reading, and is on by default for the same reason `AllowSubjectDrop` is.

A conjunction in the word list splits the sentence, and the conjunction itself says how the halves join — the rule data knows which are coordinating and which subordinate:

```bash
gramatika veta student cist kniha a zak psat dopis        # Student čte knihu a žák píše dopis.
gramatika veta student cist kniha protoze zak psat dopis # Student čte knihu, protože žák píše dopis.
```

A clause hangs off the one immediately before it, which is how a reader takes it: in *čte, protože píše a zpívá* the singing belongs inside the *protože*. `--pripojit` says otherwise, and because the attachment decides what a conjunction governs, moving it changes the sentence rather than only the picture of it:

```bash
gramatika veta student cist kniha aby zak psat dopis a lekar zpivat pisen
# Student čte knihu, aby žák psal dopis a lékař zpíval píseň.

gramatika veta student cist kniha aby zak psat dopis a lekar zpivat pisen --pripojit 3=1
# Student čte knihu, aby žák psal dopis a lékař zpívá píseň.
```

The review shows what each clause hangs off and takes `k 3=1` to move it.

A relativizer divides the word list too, only differently: the clause after a conjunction is a sibling, the clause after *který* hangs off a constituent. Both are recognized from the rule data — *který* and *jenž* are relative pronouns in `patterns.json`, *kde* and *kdy* carry the relative-adverb flag in `adverbs.json` — so neither needs a switch:

```bash
gramatika veta ucitel videt student ktery cist kniha    # Učitel vidí studenta, který čte knihu.
gramatika veta ucitel znat dum kde bydlet student       # Učitel zná dům, kde bydlí student.
```

What is written is the lemma, not the form: `ktery` is what you type even where the sentence comes out with *která* or *kterou*. The pronoun takes its gender, number and animacy from the noun it modifies. Everything after a relative clause belongs inside it, the same way a clause attaches to the one immediately before it, so a relative clause may coordinate:

```bash
gramatika veta ucitel videt student ktery cist kniha a psat dopis
# Učitel vidí studenta, který čte knihu a dopis píše.
```

The pronoun's case is the one thing it does not take from the antecedent — it holds a role in its own clause. The tool gives it the first slot its own verb's frame leaves open, and the review marks that `(rámec)` like any other derived case. It is a guess rather than a computation, so it can be overruled:

```bash
gramatika veta ucitel videt kniha ktery student cist
# Učitel vidí knihu, která čte studenta.

gramatika veta ucitel videt kniha ktery student cist --pad ktery=akuzativ
# Učitel vidí knihu, kterou čte student.
```

There is no deciding this for the user: *kniha, kterou student čte* and *kniha, která čte studenta* are both sentences, and they differ in meaning rather than in structure. A relative adverb has no case, being uninflected and not an argument of its clause.

Unstated, a relative clause hangs off the last noun of the clause before it — which is how a reader takes it, the pronoun reaching for the nearest preceding noun. `--vztazna` says otherwise and `--relativizator` changes the word it opens with; inside the dialog the two are `v 4=2` and `v 4=jenž`:

```bash
gramatika veta ucitel videt student ktery cist kniha --vztazna 1=1
# Učitel, který čte knihu, vidí studenta.

gramatika veta ucitel videt student ktery cist kniha --relativizator 3=jenz
# Učitel vidí studenta, jenž čte knihu.
```

A possessive relativizer possesses the noun written right after it and holds no case of its own — the noun possessed takes one from its own role. Which of the three words to write follows from the antecedent, and writing another is an error rather than a variant:

```bash
gramatika veta zena psat dopis jejiz student videt ucitel --vztazna 1=1 --role student=PAT
# Žena, jejíhož studenta vidí učitel, píše dopis.

gramatika veta student psat dopis jejiz kniha videt ucitel --vztazna 1=1
# K 'student' patří 'jehož', ne 'jejíž' — které ze tří to je, rozhoduje rod a číslo řídícího jména.
```

There are more relativizers than *který* and *jenž*, and they do not behave alike. *Jaký* declines like *který*. *Co* does not decline and works only where it is the subject — elsewhere its role would be carried by a resumptive pronoun, which the tool has no way to be told. *Kdo* wants a demonstrative rather than a noun:

```bash
gramatika veta ucitel videt student co cist kniha    # Učitel vidí studenta, co čte knihu.
gramatika veta ucitel videt ten kdo cist kniha       # Učitel vidí toho, kdo čte knihu.

gramatika veta ucitel videt ten ci student cist kniha --role student=PAT
# Učitel vidí toho, čího studenta čte kniha.

gramatika veta ucitel videt student kdo cist kniha
# 'kdo' se neváže na jméno 'student', ale na ukazovací zájmeno: 'ten kdo …'.
```

Interrogative *který* is told from the relative one by position: the relative stands after the noun it modifies and the interrogative before it, so `ktery student cist kniha` opens no relative clause. Words whose relative reading is only the second one — *proč* and *odkud* are just as much adverbs — additionally require a verb after them, so that `student cist kniha proc` stays a question about the reason.

A predicate switch speaks for the whole sentence unless it names a clause, and a clause that says otherwise wins:

```bash
gramatika veta student cist kniha a zak psat dopis --cas minuly
# Student četl knihu a žák psal dopis.

gramatika veta student cist kniha a zak psat dopis --cas minuly --cas 2=pritomny
# Student četl knihu a žák píše dopis.
```

The review takes the same as `p cas=minuly` and `p2 cas=pritomny`. The number before the equals sign means different things on the two families of switch — a word on `--pad` and `--role`, a clause on `--cas` and `--zpusob` — and there is nothing to tell apart, because a predicate has no position of its own that anyone would want to name. Positions run across the whole word list, so `--pad dopis=genitiv` and `7 pad=genitiv` address the same word whichever clause it landed in.

## Tests

```bash
dotnet test Grammar.Czech.Test
```

The tests are MSTest and cover nouns, adjectives, pronouns, numerals, verbs, selected phonological evaluators and services, sentence and complex-sentence building, and the loading of every JSON provider, including referential integrity between the files.

## Data layer

All grammatical data in `Grammar.Czech` ships as embedded JSON resources:

| Path | Contents |
|---|---|
| `Data/Rules/Nouns/patterns.json` | noun patterns |
| `Data/Rules/Nouns/irregulars.json` | irregular nouns |
| `Data/Rules/Nouns/propers.json` | proper names — empty so far |
| `Data/Rules/Adjectives/patterns.json` | adjective patterns |
| `Data/Rules/Pronouns/patterns.json` | pronoun data |
| `Data/Rules/Pronouns/paradigms.json` | pronoun paradigms |
| `Data/Rules/Numerals/patterns.json` | numeral data |
| `Data/Rules/Numerals/paradigms.json` | numeral paradigms |
| `Data/Rules/Verbs/patterns.json` | the general verb classes `trida1`–`trida5` and the `dojme` pattern |
| `Data/Rules/Verbs/irregulars.json` | irregular verbs and named patterns with explicit stems |
| `Data/Rules/prefixes.json` | prefixes |
| `Data/Rules/clitics.json` | conditional particles, past auxiliaries, reflexives |
| `Data/Rules/prepositions.json` | prepositions, their government and vocalization |
| `Data/Rules/conjunctions.json` | conjunctions, their type, relation, pairing and comma rule |
| `Data/Rules/adverbs.json` | adverbs and their comparison |
| `Data/Rules/particles.json` | particles and their function |
| `Data/Rules/interjections.json` | interjections, their type and predicative use |
| `Data/Lexicon/grammar.czech.lexicon.db` | lexical metadata and valency frames — SQLite, not JSON |

## Known limitations

### The data, not the mechanism

- The lexicon is not a complete dictionary of Czech; `ResolveGenderAndPattern` and `ResolveVerbAspect` only work for lemmas the database holds.
- Only a fraction of the entries in the lexicon carry a valency frame. The mechanism is finished, the data is not: for a verb without a frame the caller supplies the cases as before.
- Genitive-plural shortening is quantity only, and the flag is set on a handful of lemmas. The *í* → *ě* type (*míra* → *měr*, *díra* → *děr*) is a different alternation that `has_genitive_plural_shortening` does not describe; such words need `lemma_entry.stem`, which the code reads but which nothing in the dictionary fills in yet.
- A slot realized by an infinitive or a content clause is built by `CzechClausePlanner`, but only one such slot per clause: a verb governing two of them at once is refused rather than assembled. The PDT tectogrammatical manual (§2.4) documents the counterexample — *vyžadovat* controls two at once — so the limit is the implementation and not the language.
- A reflexive infinitive puts its particle in the governing clause's cluster, which is right for one — *chce se učit* — and refused for two, because a clause has one cluster and *se* cannot be in it twice. What Czech does instead is described as haplology of the reflexive clitic (Rosen 2014), and three strategies are attested: the long form (*sebe*), a cluster of its own, and deletion of one particle. Which of them to generate is open; refusing is the one answer that is never wrong.
- `CzechNumeralComposer.ComposeOrdinal` and `ComposeOfType` build only from lemmas present in the dictionary; a value that needs a missing component (e.g. *dvoutisící*) throws rather than inventing a form.

### Not modelled

- Clauses joined through `SentencePlan.Joined` nest as deep as they are written, and a chain at one level (`[a: B, protože: C]`) is a different sentence from a chain of nestings (`a: B { protože: C }`) — both are expressible. A relative clause is a plan in its own right, so everything that holds of a sentence holds inside one.
- `IValencyProvider.GetEntry` takes a lemma, optionally with a `WordCategory`, so homonyms across categories can be told apart but homonyms inside one cannot. The schema carries `homonym_index` and the provider returns the lowest one.
- The clitic cluster does not know the free dative (*To ti byla legrace*), which per NESČ stands between the auxiliary and the reflexive. The remaining positions match the described order. NESČ (*Dativ*) separates the semantically motivated ones — benefactive, possessive, subject and respect datives, which a valency frame could carry — from the ethical dative, which is pragmatic and belongs to none; a frame is the wrong place for the second kind and the right one for the first.
- A demonstrative in front of a numeral (*těch pět studentů*) agrees with the head of the phrase, not with the phrase as a whole.
- That an inner participant combines with a given verb at most once is not enforced. In FGD this is not a rule the grammar happens to have but the criterion that makes a participant a participant at all (Urešová et al., PBML 105, 2016): a slot that can be filled twice over is a free modification, not an actant. Nothing stops two `PAT` constituents in one clause, and coordination and apposition — the two constructions where a repeat is legitimate — are not modelled either, so the check has nothing to make an exception for yet.
- Information structure is reflected in word order only. NESČ carries it in intonation too, and treats two readings differing in prosody as two different sentences; that is not modelled. Neither is contrastive focus, which the PDT manual (9.1.2, 9.3.1.1) needs an independent mark of the intonation centre for — word order alone cannot stand in for it.
- Control is modelled, but singly. `valency_slot.control_target` records which participant of the matrix clause the unexpressed subject of an infinitive corefers with, and `CzechClausePlanner` refuses the construction when the two are different people. What is not modelled is double control (PDT §2.4) and the same coreference stated on a content clause rather than an infinitive.
- Two of the five Czech diatheses are not reachable. `SentencePlan.Diathesis` names the one to build in and the deagentive (*pracovalo se*) and the dispositional (*studentovi se pracovalo*) come out of it; the resultative (*mám napsáno*) and the recipient deagentive (*dostat* with a participle) do not, because both need a second verb carrying a participle of the first and the composer does not build that form. Frames for the two that work are in the dictionary for two verbs each. The periphrastic passive, which was down to a single verb, now covers fourteen senses — derived from their active frames by the regular rule, actor to the instrumental and patient to the nominative.
- Shared modification in coordination is not modelled: a dependent of the first conjunct cannot be stated once and read as belonging to both predicates. Verb ellipsis is (see above); this is the other half of PDT §12.3.
- Coordination of members with different functors, and gapping with it, is not modelled.
- Apposition is not modelled, which is also why the uniqueness of a participant has no exception to make.
- Clefts and pseudo-clefts (*to, co potřebujeme, je…*) are not modelled.
- Projectivity is not enforced. Czech word order permits non-projective constructions and the word-order resolver has no notion of a crossing dependency.
- Negation is a property of the predicate, not something with a scope: *nepřišel kvůli dešti* has two readings and the plan cannot tell them apart.
- Condensation — a proposition expressed by a noun or an infinitive instead of a clause (*po jeho příchodu* for *když přišel*) — is not modelled as a choice; a slot is filled the way it is written.

- The second class forms its passive participle on *-nut* — *poslechnut*, *tisknut* — which is what IJP gives. The *tištěn* variant, which IJP lists alongside *tisknut* for the printing sense, is not generated: the *sk* → *ště* alternation belongs to the participle in *-en*, and substituting the stem cannot produce it because the ending is appended to whatever the stem becomes.

### Where usage decides and one reading was chosen

- Preposition vocalization is, per IJP, not a settled phenomenon and usage decides. The rules cover the documented tendencies; the rest is an enumeration in `vocalizeBefore`.
- The comma before `nebo` and `či` depends on the relation between the clauses, not on the conjunction. The data carries only the commoner reading; the exclusive one has to be stated through `Coordination.RequiresComma`.
- Numerals generate the declined aggregate numeral; the frozen variant (*bez patero ponožek*), which IJP id=792 lists as standard alongside it, cannot be requested.
- For the pattern `sto`, the declined variant with the genitive is generated (*ke stu korun*); the indeclinable one with agreement (*ke sto korunám*), which IJP lists alongside it, cannot be expressed.

### How the API is used

- The caller often has to supply `Pattern`, `Gender`, `Number`, `Case`, `Person`, `Tense`, `Aspect`, `Modus` and `Voice`; the project is not yet an analyzer of natural text.
- `MorphologyEngine.GetForm` returns a single word, so for a verb it gives the basic form only. The verb forms that are several words — the periphrastic future, the passive with an auxiliary, the conditional, negation, the reflexive — need `CzechWordFormComposer.GetFullForm`.
- A named pattern from `irregulars.json` carries the stems literally, so it fits the pattern's own verb and its prefixed derivatives — `nese` covers *nést* and *odnést*, `dělá` covers *dělat* and *dodělat*. An unrelated verb needs a class pattern: *prodávat* with `dělá` returns *dělá*, with `trida5` the correct *prodává*.
- The CLI builds sentences; it does not read them. An inflected word is recognized as a form of a lemma and named as one, but it is not accepted as input — the tool generates from lemmas and is not an analyzer.

### The dictionary workflow

- The database is binary, so git cannot show what changed inside it. `dump` produces the reviewable text form; wiring that into the commit workflow is not done.
- A pull downloads the whole dictionary every time. There is no incremental sync, and adding one would need change tracking and tombstones on the server — deletions are invisible to a delta pull otherwise. Rewriting the file handles them for free, which is why it is the starting point.

## License

Copyright (c) 50PSoftware. All rights reserved.
