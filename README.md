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
  - [Verbs](#verbs)
  - [Phonology and orthography](#phonology-and-orthography)
  - [Lexicon and valency](#lexicon-and-valency)
  - [Sentences and complex sentences](#sentences-and-complex-sentences)
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

Patterns can inherit endings through `inheritsFrom`; `les`, for instance, inherits from `hrad` and overrides only the cases that differ. The five masculine animate sub-patterns all use it for the same thing, a nominative and vocative plural the base vzor does not have: `učitel`, `občan` and `turista` give `-é` to the *-tel*, *-an* and *-ista/-ita* suffix classes (*učitelé*, *občané*, *turisté*), and `syn` and `král` give `-ové` to the lexically defined class that takes it (*synové*, *biologové*, *králové*). Softening rules are inherited along with endings — a rule named for `pán` governs `občan` and `syn` too — so a sub-pattern does not have to restate the palatalization its base already describes. Irregular nouns live in `Grammar.Czech/Data/Rules/Nouns/irregulars.json` (19 lemmas, e.g. *oko*, *dům*, *ruka*, *noha*, *ucho*). The file `Grammar.Czech/Data/Rules/Nouns/propers.json` is empty — the mechanism for proper names exists, the data does not.

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

The post-preposition variant is available through `CzechWordRequest.IsAfterPreposition`.

### Numerals

Numerals are read from `Grammar.Czech/Data/Rules/Numerals/patterns.json`, their paradigms from `Grammar.Czech/Data/Rules/Numerals/paradigms.json`. The data covers all nine kinds — cardinal, ordinal, sortal, set, aggregate, multiplicative, fractional, group, and the indefinite numerals across kinds. Distributive numerals (*po dvou*) are a construction rather than a lexical entry, and `CzechNumeralComposer` builds them.

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

An unregistered adverb has its comparative derived: `-ěji` after `d`, `t`, `n` and the labials, `-eji` elsewhere, with the palatalization `-ce`, `-ky` and `-ho` bring. Measured against the 99 comparatives in the data, the rule reproduces every regular one and misses only the irregulars the ÚJČ reference lists as such — the two partition without overlap, which is what makes deriving safe. A test measures that rather than assuming it, so the rule and the data cannot drift apart unnoticed. A registered comparative always wins, and an adverb registered without one is taken to be uncompared rather than derived.

The adjective an adverb comes from is recorded rather than computed, for the same reason, and `ICzechAdverbService.GetAdverbsFor` reads the mapping back — returning both members where an adjective yields two. Relative adverbs (*kde*, *kdy*, *kam*, *jak*) introduce a relative clause through `RelativeAttachment.Relativizer`, the field that used to be called `Pronoun`; being uninflected, they take no case and nothing agrees with the antecedent through them. Negative adverbs (*nikdy*, *nikde*, *nijak*) are lemmas of their own, not the positive ones with a prefix.

A comparative built a different way is not the same thing as a clipping, and the data keeps them apart: *snadno* takes both *snáze* and *snadněji*, *hluboko* both *hlouběji* and *hloub*. Generation picks the primary; `ICzechAdverbService.GetComparativeVariants` shows the rest. One adjective can also yield two adverbs that are separate lemmas sharing a comparative — *dlouho* and *dlouze*, *vysoko* and *vysoce*, *těžko* and *těžce*, *široko* and *široce*, *úzko* and *úzce* — and both members are registered.

### Verbs

Verbs are generated from the rules in:

- `Grammar.Czech/Data/Rules/Verbs/patterns.json` — the general classes `trida1` through `trida5`, whose stems are derived from the infinitive, plus the named pattern `dojme`,
- `Grammar.Czech/Data/Rules/Verbs/irregulars.json` — 37 entries with explicitly listed stems. These are not only irregular verbs such as `být`, `mít`, `chtít`, `moci` and `vědět`; the classic named patterns `nese`, `bere`, `maže`, `peče`, `umře`, `tiskne`, `mine`, `kryje`, `kupuje`, `prosí` and `dělá` live here too.

`CzechVerbConjugationService` generates the basic forms for the indicative, the conditional, the imperative, the past tense, the present/future tense and the passive participle. On top of that, `CzechWordFormComposer` assembles some verb phrases: the periphrastic future of imperfectives, the passive with an auxiliary, the conditional, negation, and the reflexive `se`/`si`.

The verb pattern is passed through `Pattern` — either a class (`trida1`–`trida5`) or a pattern name from `irregulars.json`. Alternatively `VerbClass` can sometimes be passed, and it maps onto `trida1`–`trida5`. `GuessVerbClass` implements a simple heuristic based on the infinitive ending, but it is not reliable for every Czech verb.

### Phonology and orthography

The project contains a phonological layer for softening, epenthesis, jotation and vowel quantity. The decisions are separated into evaluators; the transformations are carried out by `CzechPhonologyService` and `CzechOrthographyService`.

The parts in public use include:

- `IPhonemeRegistry` / `CzechPhonemeRegistry`,
- `ISofteningRuleEvaluator<CzechWordRequest>`,
- `IEpenthesisRuleEvaluator<CzechWordRequest>`,
- `IJotationRuleEvaluator<CzechWordRequest>`,
- `ISyncretismRuleEvaluator<CzechWordRequest>`,
- `ICzechOrthographyService`.

`CzechAlternationRuleEvaluator`, for genitive-plural shortening, exists but is currently not registered in `AddCzechGrammarServices()` and is not wired into `CzechNounDeclensionService`.

### Lexicon and valency

`SqliteValencyProvider` reads `Grammar.Czech/Data/Lexicon/grammar.czech.lexicon.db`, a SQLite database. It is the one data source here that is not embedded JSON, because it is the part meant to grow into thousands of entries; the rule files under `Data/Rules/` describe closed classes and stay as they are.

The dictionary is edited centrally, in MySQL behind a PHP admin, and this file is the local read-only copy pulled from it. Identifiers are assigned by the server and carried over unchanged — a copy that renumbered could never be compared against the server it came from again.

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
- `valency_frame`, `valency_slot`, `slot_realization` — the frames themselves. A slot may have several realizations with a generation preference, which is what lets one slot be a bare case in one wording and a `že`-clause or an infinitive in another.

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

#### The wire format

`Grammar.Czech.Lexicon.Tool/Php/api/index.php` serves one page of one table per request:

```json
{"table":"lemma_entry","columns":["lemma_entry_id","lemma",…],"rows":[[1,"student",…]],"next_after":"5000"}
```

Three choices in there are worth knowing about, because each has a quieter alternative that looks fine:

- **Table-shaped, not nested.** Identifiers come from the server and have to survive the trip; a document nesting slots inside frames inside lemmas would either repeat them anyway or make the importer invent its own.
- **Rows as arrays, with the column names stated once.** At a hundred thousand lemmas, repeating twenty-four keys per row is most of the payload. The single header is also the contract: the importer refuses a page whose columns are not the ones it expects, in that order, which is what stops a reordered column from importing cleanly into the wrong place.
- **Keyset paging, not offsets.** An offset shifts when the dictionary is edited mid-pull, silently dropping or repeating rows. The key is compared in its own type on both sides so the primary key index stays usable.

A paged pull is still not a consistent snapshot — nothing stops an edit between one page and the next — and `validate` is what catches the result, as a broken reference rather than as a word that quietly fails to resolve later.

#### Deploying

[`docs/nasazeni-slovniku-wedos.html`](docs/nasazeni-slovniku-wedos.html) walks the whole thing through in Czech, step by step, with a check after each one and a table of the failures that do not point at themselves. Open it from disk — it is self-contained.

Copy the contents of `Php/` into the document root. There are two entry points and everything else is either denied or never requested:

```
www/                ← document root
  index.php         ← the admin, at /
  style.css
  api/index.php     ← the API, at /api/
  .env.php          ← secrets, as PHP; git-ignored
  .env.php.example  ← the template, committed
  .htaccess         denies the dotfiles and the includes
  env.php           shared include, denied
  schema-tables.php shared include, denied
  admin/            the admin's internals, denied wholesale
```

**The secrets go in `.env.php`, not `.env`.** Since the admin serves from the root, the document root is the only place they can live, and a plain `.env` there is handed out as text by any server that has not been told otherwise — `https://example.com/.env` giving up the database password with nothing logged but an access line. `.env.php` returns an array instead, so a request for it is executed rather than read out, and that holds with no `.htaccess`, with `AllowOverride` off, and on nginx.

Configuration is read from the real environment first and from the file second, so a PHP-FPM pool can override any single value with `env[NAME]` without the file being edited. `getenv()` under FPM sees only what the pool passes, which is why a file is needed at all.

There is deliberately no catch-all rewrite. An earlier layout served the endpoint from the root and needed one; now that the admin is at the root and the API has a directory of its own, nothing has to be routed — which also removes the sharpest edge in the whole arrangement, where adding `RewriteCond %{REQUEST_FILENAME} !-f` to such a rule makes it skip `.env` precisely *because* `.env` is a real file.

API authentication is one shared bearer token, compared with `hash_equals` so it cannot be guessed a character at a time, and the API refuses to serve at all when the token is unset rather than serving openly. It rides in a header on every request, so **HTTPS is load-bearing here, not advisory**. Prefer the `LEXICON_API_TOKEN` environment variable over `--token` on the pull side: a command line is visible in `ps` and lands in shell history.

That matters because the alternative protection is thinner than it looks. The shipped `.htaccess` denies dotfiles and the two includes with `Require` directives, which hold whether or not mod_rewrite is loaded — but only while `.htaccess` is read at all, and not on nginx. Doing the same job with a rewrite would be worse still: adding `RewriteCond %{REQUEST_FILENAME} !-f`, the near-universal "leave real files alone" condition, makes a catch-all skip `.env` precisely *because* it is a real file, and it is served in the clear from then on with nothing to show anything changed.

Four more things need checking, and each fails in a way that does not point at itself:

- **The database host is not `localhost`.** Shared hosting puts MySQL on a separate machine; take the hostname from the admin panel and put it in the DSN, with `charset=utf8mb4`.
- **It is probably MariaDB, not MySQL.** `schema.mysql.sql` sticks to collations both have — `utf8mb4_0900_*` is MySQL 8 only and MariaDB rejects the whole script with *Unknown collation*. A test guards against those creeping back in.
- **Set PHP to 8.1 or newer** in the admin panel.
- **The `Authorization` header is likely stripped** before PHP sees it. `env.php` looks in three places for it, including `getallheaders()`, but if requests still come back 401 with a correct token, add to `www/.htaccess`:

  ```apache
  RewriteEngine On
  RewriteCond %{HTTP:Authorization} .
  RewriteRule .* - [E=HTTP_AUTHORIZATION:%{HTTP:Authorization}]
  ```

Verify the deployment with five requests. `/.env.php`, `/env.php` and `/admin/lib.php` must return 403 or 404, or at worst an empty body — never source. An API request with no token must return 401, and one with the correct token 200.

##### The admin

`/` is where entries are written — a server-rendered PHP interface, signed into with a password whose `password_hash` output goes in the configuration as `LEXICON_ADMIN_PASSWORD_HASH`. The configuration holds the hash and never the password, so a leaked `.env.php` does not hand over a working login.

The forms are per *word* rather than per table, because adding a verb touches four of them: the entry, the lexeme it hangs on, the sense, and the frame with its slots and realizations. Enum dropdowns are built from `LEXICON_ENUMS` in `schema-tables.php`, so the admin cannot offer a value the importer would reject, and a test compares that list against the real C# enums.

It writes to the database directly rather than through `/api/`, and that is on purpose. The API exists for replication — pages of whole tables, in dependency order, so the C# client can rebuild a copy — which is a different job from "save this one entry". Routing writes through it would add an HTTP hop to the same server, a second set of endpoints and a second authentication path, and share nothing worth sharing: the rules that would benefit from one implementation live in the C# validator, not in PHP. What the two do share is `schema-tables.php`, which is where sharing actually pays.

Nothing under `admin/` is served — `admin/.htaccess` denies the directory, and every file in it also refuses to run unless `index.php` included it first, which is the lock that holds where `.htaccess` is ignored.

It deliberately does **not** re-implement `LexiconValidator`. Two hand-maintained copies of the same rules drift, and the validator already gates every pull, so anything the admin lets through is caught before it reaches a local lexicon. What it does enforce is the part that cannot be repaired afterwards — `lemma_key` folded with `mb_strtolower` (the byte-wise `strtolower` leaves `Á` alone and yields a key no lookup matches), the permitted enum values, and the shape of a realization. Missing actors and slots with no preferred realization are shown as warnings where they occur rather than blocked.

Nothing is required beyond the lemma for a word the dictionary holds. `CzechLexiconEnricher` runs in `MorphologyEngine` ahead of the dispatch and fills in whatever the request left unsaid — the word class, gender, pattern, animacy, the phonological flags, verb class, aspect, reflexive type.

Ahead of the dispatch, because the word class is one of the things it fills and is also what selects the inflection service. `WordCategory` is therefore nullable: an enum's default is one of its members, and for this one that was `Noun`, so a request that never mentioned a class was a claim rather than a gap — `dát` reached the declension service, had its vzor filled from the lexicon correctly as `trida5`, and failed with *Noun pattern 'trida5' not found*.

It only ever writes where the request holds `null`, so a stated pattern wins even when the lexicon disagrees, and `HasMobileE = false` stays false rather than being replaced by the entry. That distinction is why the flags are nullable: `false` is the caller saying the word has no mobile e, `null` is the caller not saying, and only the second is a gap. A word the lexicon has never heard of goes through untouched and inflects from what the caller passes, which is the ordinary case — most of Czech is not in the dictionary and never will be.

An entry is used only when its word class matches the one asked about. `GetEntry` takes a lemma and no category, so on a lemma entered under two classes it returns whichever row it finds; filling a request about *stát* the verb from the row for *stát* the country would not complete it but answer a different question.

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
- **relative clauses** — the pronoun agrees with the head noun in gender, number and animacy, and takes its case from its own role in the dependent clause.

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

Sixty-nine lemmas in `Grammar.Czech/Data/Rules/interjections.json`, in the four NESČ types — *emocionální*, *kontaktová*, *apelová*, *zvukomalebná* — with the line that source draws across them: the first three are subjective and the fourth objective. No morphology; an interjection is uninflected and forms, in that description, the most primitive sentence type there is.

The punctuation is a rule and not data. An interjection is set off by a comma except where it stands in for a clause member, so the same word goes both ways — *Kamarádi, hurá, vyhráli jsme* against *Palicí buch ho po hlavě* — and `ICzechInterjectionService.RequiresComma` takes the use rather than only the word. What *is* recorded per word is which interjections can be a predicate at all, because it does not follow from the type: *hop* is *apelové* and predicative all the same. Those also carry the verb they form (*žbluňk → žbluňknout*), which NESČ notes as their entering word formation directly.

The class is more open than any other here — onomatopoeia is coined on the spot — so an unregistered lemma passes through instead of being reported, the opposite of the closed conjunction inventory. Reduplication is recorded only where the source names it (*oj oj oj*, *ťuk(y) ťuk* as optional, *bubu* as obligatory); unmarked means unrecorded, not known not to repeat.

`CzechClause.Interjection` places one outside the clause behind its comma, leaving the first position alone. The terminator stays the caller's: an emotional interjection tends towards an exclamation mark, but tending is not a rule.

## Architecture

```text
Grammar.sln
|-- Grammar.Core/               language-independent enums, interfaces and models
|-- Grammar.Czech/              the Czech implementation: services, providers, embedded JSON rules and the lexicon database
|-- Grammar.Czech.Cli/          console demo with hard-coded examples
|-- Grammar.Czech.Lexicon.Tool/ pulls, builds, validates and dumps the lexicon database; holds the schemas and the PHP API
`-- Grammar.Czech.Test/         MSTest tests for declension, conjugation, phonology and sentence building
```

The main DI registration is `AddCzechGrammarServices()` in `Grammar.Czech/CzechGrammarServiceFactory.cs`.

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

`Grammar.Czech.Cli` is still a demo application. It has no general argument handling; on start it prints the forms of a few hard-coded examples from `Program.cs`.

```bash
dotnet run --project Grammar.Czech.Cli
```

## Tests

```bash
dotnet test Grammar.Czech.Test
```

The tests are MSTest and cover nouns, adjectives, pronouns, numerals, verbs, selected phonological evaluators and services, sentence and complex-sentence building, and the loading of every JSON provider, including referential integrity between the files.

## Data layer

All grammatical data in `Grammar.Czech` ships as embedded JSON resources:

| Path | Contents |
|---|---|
| `Data/Rules/Nouns/patterns.json` | noun patterns (15) |
| `Data/Rules/Nouns/irregulars.json` | irregular nouns (18) |
| `Data/Rules/Nouns/propers.json` | proper names — empty so far |
| `Data/Rules/Adjectives/patterns.json` | adjective patterns (4) |
| `Data/Rules/Pronouns/patterns.json` | pronoun data |
| `Data/Rules/Pronouns/paradigms.json` | pronoun paradigms |
| `Data/Rules/Numerals/patterns.json` | numeral data |
| `Data/Rules/Numerals/paradigms.json` | numeral paradigms |
| `Data/Rules/Verbs/patterns.json` | the general verb classes `trida1`–`trida5` and the `dojme` pattern |
| `Data/Rules/Verbs/irregulars.json` | irregular verbs and named patterns with explicit stems (37) |
| `Data/Rules/prefixes.json` | prefixes |
| `Data/Rules/clitics.json` | conditional particles, past auxiliaries, reflexives |
| `Data/Rules/prepositions.json` | prepositions, their government and vocalization |
| `Data/Rules/conjunctions.json` | conjunctions, their type, relation, pairing and comma rule |
| `Data/Rules/adverbs.json` | adverbs and their comparison |
| `Data/Rules/particles.json` | particles and their function |
| `Data/Rules/interjections.json` | interjections, their type and predicative use |
| `Data/Lexicon/grammar.czech.lexicon.db` | lexical metadata and valency frames (`dát`/`dávat`, `jít`, `vidět`) — SQLite, not JSON |

## Known limitations

- The caller often has to supply `Pattern`, `Gender`, `Number`, `Case`, `Person`, `Tense`, `Aspect`, `Modus` and `Voice`; the project is not yet an analyzer of natural text.
- `MorphologyEngine.GetForm` returns a single word, so for a verb it gives the basic form only. The verb forms that are several words — the periphrastic future, the passive with an auxiliary, the conditional, negation, the reflexive — need `CzechWordFormComposer.GetFullForm`.
- A named pattern from `irregulars.json` carries the stems literally, so it fits the pattern's own verb and its prefixed derivatives — `nese` covers *nést* and *odnést*, `dělá` covers *dělat* and *dodělat*. An unrelated verb needs a class pattern: *prodávat* with `dělá` returns *dělá*, with `trida5` the correct *prodává*.
- `CzechAlternationRuleEvaluator` is not registered in DI, and genitive-plural shortening is not actively wired into noun declension.
- The lexicon is not a complete dictionary of Czech; `ResolveGenderAndPattern` and `ResolveVerbAspect` only work for lemmas the database holds.
- `IValencyProvider.GetEntry` takes a lemma and nothing else, so it cannot tell homonyms apart. The schema carries `homonym_index` and the provider returns the lowest one.
- The CLI is a demo, not a general-purpose query tool.
- Numerals do not support the frozen variant of aggregate numerals (*bez patero ponožek*), which IJP id=792 lists as standard alongside the declined one; the declined form is always generated.
- A demonstrative in front of a numeral (*těch pět studentů*) agrees with the head of the phrase, not with the phrase as a whole.
- `CzechNumeralComposer.ComposeOrdinal` and `ComposeOfType` build only from lemmas present in the dictionary; a value that needs a missing component (e.g. *dvoutisící*) throws rather than inventing a form.
- The lexicon contains frames for thirty lexemes — forty-six verb lemmas, counting both members of each aspect pair — out of two hundred and fifty-five entries. The mechanism is finished, the data is not: for a verb without a frame the caller supplies the cases as before.
- A slot can be stored as realized by a `že`-clause or an infinitive, but nothing generates one yet: that needs a clause planner, and until it exists `CzechSentenceBuilder` leaves such a constituent to the caller.
- The database is binary, so git cannot show what changed inside it. `dump` produces the reviewable text form; wiring that into the commit workflow is not done.
- A pull downloads the whole dictionary every time. There is no incremental sync, and adding one would need change tracking and tombstones on the server — deletions are invisible to a delta pull otherwise. Rewriting the file handles them for free, which is why it is the starting point.
- The PHP half has not been run: there is no PHP in this development environment, so the API is reviewed code rather than tested code. The C# half of the same contract is covered end to end by exporting the real lexicon, serializing it as the API would, and importing it back.
- The clitic cluster does not know the free dative (*To ti byla legrace*), which per NESČ stands between the auxiliary and the reflexive. The remaining positions match the described order.
- The conjunctions `aby` and `kdyby` are not supported — they fuse with the conditional auxiliary and inflect by person (*abych*, *abys*, *abychom*). Nor is `však`, which is itself second-position rather than clause-initial.
- The comma before `nebo` and `či` depends on the relation between the clauses, not on the conjunction. The data carries only the commoner reading; the exclusive one has to be stated through `Coordination.RequiresComma`.
- Preposition vocalization is, per IJP, not a settled phenomenon and usage decides. The rules cover the documented tendencies; the rest is an enumeration in `vocalizeBefore`.
- Information structure is reflected in word order only. NESČ carries it in intonation too, and treats two readings differing in prosody as two different sentences; that is not modelled.
- The rule that an inner participant combines with a verb at most once is not enforced — nothing stops two `PAT` constituents in one clause.
- A relative clause must be a single clause; a complex sentence inside a relative clause is not supported.
- For the pattern `sto`, the declined variant with the genitive is generated (*ke stu korun*); the indeclinable one with agreement (*ke sto korunám*), which IJP lists alongside it, cannot be expressed.

## License

Copyright (c) 50PSoftware. All rights reserved.
