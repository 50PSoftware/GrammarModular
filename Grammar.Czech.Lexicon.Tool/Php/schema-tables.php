<?php

declare(strict_types=1);

/**
 * The tables of the lexicon and the columns each one carries.
 *
 * The counterpart of LexiconSchema.cs, and the one place the API is allowed to learn a table or column
 * name from. Everything reaching SQL goes through here rather than through a request parameter, so a
 * caller cannot name a table of its own — the identifiers are not values and cannot be bound as
 * parameters, which makes a whitelist the only safe way to build these statements.
 *
 * Parent tables first, matching the order the importer inserts them in.
 *
 * Keep in step with Grammar.Czech.Lexicon.Tool/LexiconSchema.cs. The columns are written out rather
 * than read from information_schema so that a column added to MySQL and not to the SQLite schema is
 * refused by the importer instead of silently travelling into a database that has nowhere to put it.
 */

const LEXICON_TABLES = [
    'lexicon_meta' => ['meta_key', 'meta_value'],
    'lexeme' => ['lexeme_id', 'primary_lemma', 'note'],
    'lemma_entry' => [
        'lemma_entry_id', 'lemma', 'lemma_key', 'homonym_index', 'category', 'gender', 'pattern',
        'is_animate', 'has_mobile_e', 'has_genitive_plural_shortening',
        'has_epenthesis_in_genitive_plural', 'is_indeclinable', 'is_plural_only', 'is_countable',
        'prefers_short_form', 'verb_class', 'aspect', 'aspect_counterpart', 'reflexive_type',
        'base_verb_lemma', 'stem', 'present_stem', 'past_stem', 'future_stem',
        'imperative_stem', 'passive_stem', 'infinitive', 'forms_passive',
        'lexeme_id', 'source', 'is_verified', 'note',
    ],
    'lexical_unit' => ['lu_id', 'lexeme_id', 'sense_label', 'gloss', 'ssc_class_id'],
    'valency_frame' => ['frame_id', 'lu_id', 'kind', 'diathesis', 'is_default', 'reflexive_type'],
    'valency_slot' => [
        'slot_id', 'frame_id', 'functor', 'canonical_order', 'obligatoriness',
        'can_drop_contextual', 'can_drop_generic', 'control_target',
    ],
    'slot_realization' => [
        'realization_id', 'slot_id', 'morph_case', 'preposition', 'clause_type',
        'takes_infinitive', 'preference',
    ],
    'construction' => [
        'construction_id', 'pattern_name', 'light_verb_lemma', 'pred_noun_lemma', 'template_json',
    ],
];

/**
 * Tables whose primary key is text rather than an integer.
 *
 * Paging compares the key in its own type so that the primary key index stays usable, which means the
 * two kinds are bound differently. The counterpart of LexiconTable.KeyIsText.
 */
const LEXICON_TEXT_KEY_TABLES = ['lexicon_meta'];

/**
 * The values each constrained column accepts, and what to call them in the admin.
 *
 * Keys are what goes in the database — the C# enum member names, spelled exactly, because the
 * provider parses them case-sensitively. Values are Czech labels and exist only on screen.
 *
 * Keeping the list here rather than in the admin means the form cannot offer a value the importer
 * would reject; a test compares the keys against the real C# enums, so adding a functor in one place
 * and not the other fails at build time instead of at data-entry time.
 */
const LEXICON_ENUMS = [
    'category' => [
        'Noun' => 'podstatné jméno',
        'Adjective' => 'přídavné jméno',
        'Pronoun' => 'zájmeno',
        'Numerale' => 'číslovka',
        'Verb' => 'sloveso',
        'Adverb' => 'příslovce',
        'Preposition' => 'předložka',
        'Conjunction' => 'spojka',
        'Particle' => 'částice',
        'Interjection' => 'citoslovce',
    ],
    'gender' => [
        'Masculine' => 'mužský',
        'Feminine' => 'ženský',
        'Neuter' => 'střední',
    ],
    'aspect' => [
        'Perfective' => 'dokonavý',
        'Imperfective' => 'nedokonavý',
    ],
    'verb_class' => [
        'Class1' => '1. třída',
        'Class2' => '2. třída',
        'Class3' => '3. třída',
        'Class4' => '4. třída',
        'Class5' => '5. třída',
    ],
    'reflexive_type' => [
        'None' => 'bez reflexiva',
        'ReflexivumTantum_Se' => 'reflexivum tantum – se (bát se)',
        'ReflexivumTantum_Si' => 'reflexivum tantum – si (přát si)',
        'DerivedReflexive_Se' => 'odvozené reflexivum – se (mýt se)',
        'DerivedBenefactive_Si' => 'odvozený benefaktiv – si (koupit si)',
        'Reciprocal_Se' => 'vzájemnostní – se (potkat se)',
        'DeagentivePassive_Se' => 'deagentní pasivum – se (mluví se)',
    ],
    'kind' => [
        'Verbal' => 'plnovýznamové sloveso',
        'Copular_NominalPred' => 'spona se jmenným přísudkem',
        'Copular_AdjectivalPred' => 'spona s adjektivním přísudkem',
        'Existential' => 'existenciální',
        'Modal' => 'modální',
        'PhasalLightVerb' => 'fázové',
        'LightVerb' => 'kategoriální (mít zájem)',
    ],
    'diathesis' => [
        'Active' => 'aktivum',
        'PassivePeriphrastic' => 'opisné pasivum',
        'ReflexivePassive' => 'reflexivní pasivum',
        'RecipientDeobjective' => 'recipientní deobjektivum (dostat)',
        'Dispositional' => 'dispoziční',
        'Resultative' => 'rezultativ (mám napsáno)',
    ],
    'functor' => [
        'ACT' => 'ACT – konatel',
        'PAT' => 'PAT – patiens',
        'ADDR' => 'ADDR – adresát',
        'ORIG' => 'ORIG – původ',
        'EFF' => 'EFF – efekt',
        'DIR1' => 'DIR1 – odkud',
        'DIR2' => 'DIR2 – kudy',
        'DIR3' => 'DIR3 – kam',
        'LOC' => 'LOC – kde',
        'MANN' => 'MANN – jak',
        'MEANS' => 'MEANS – čím',
        'BEN' => 'BEN – pro koho',
        'CAUS' => 'CAUS – proč',
        'AIM' => 'AIM – za jakým účelem',
        'TWHEN' => 'TWHEN – kdy',
        'DIFF' => 'DIFF – o kolik',
        'OBST' => 'OBST – o co (překážka)',
        'INTT' => 'INTT – za jakým záměrem',
        'MAT' => 'MAT – z čeho',
        'THL' => 'THL – jak dlouho',
        'EXT' => 'EXT – do jaké míry',
        'CRIT' => 'CRIT – podle čeho',
        'ACMP' => 'ACMP – s kým',
        'COMPL' => 'COMPL – jako co',
        'CPHR' => 'CPHR – jmenná část (mít zájem)',
    ],
    'obligatoriness' => [
        'Obligatory' => 'obligatorní',
        'Typical' => 'typický',
        'Optional' => 'fakultativní',
    ],
    'morph_case' => [
        'Nominative' => '1. nominativ',
        'Genitive' => '2. genitiv',
        'Dative' => '3. dativ',
        'Accusative' => '4. akuzativ',
        'Vocative' => '5. vokativ',
        'Locative' => '6. lokál',
        'Instrumental' => '7. instrumentál',
    ],
];

/**
 * The inflection patterns each word category accepts, keyed by the C# WordCategory member name.
 *
 * Not an enum, which is why it is not in LEXICON_ENUMS: the real list is the pattern JSON embedded in
 * Grammar.Czech, and nothing on the server can read it. The column has no CHECK for the same reason,
 * so before this list existed a mistyped vzor saved, pulled and validated cleanly, and only failed the
 * first time something declined the word — NotSupportedException("Noun pattern 'ucitel' not found."),
 * nowhere near the row that caused it.
 *
 * A copy is a copy, and this one is kept honest the same way the enums are: PhpSchemaParityTests
 * compares it against LexiconValidator.PatternsByCategory, which reads the JSON. A vzor added to the
 * data and not here is one nobody can enter; one added here and not to the data saves and then throws.
 * Either way the build fails rather than the data entry.
 *
 * Nouns and adjectives take their declension patterns. Verbs take both the conjugation classes and the
 * named irregular patterns, because CzechVerbConjugationService looks the pattern up in both.
 * Categories that do not inflect by pattern are absent on purpose — a pattern on one is an error, not
 * an empty choice.
 *
 * Compared case-insensitively, matching the inflection services, which all look up through ToLower().
 */
const LEXICON_PATTERNS = [
    'Noun' => [
        'hrad', 'kost', 'král', 'kuře', 'les', 'moře', 'muž', 'město', 'občan', 'pán', 'píseň',
        'předseda', 'růže', 'soudce', 'stavení', 'stroj', 'syn', 'turista', 'učitel', 'žena',
    ],
    'Adjective' => [
        'jarní', 'matčin', 'mladý', 'otcův',
    ],
    'Verb' => [
        'trida1', 'trida2', 'trida3', 'trida4', 'trida5', 'dojme',
        'bere', 'běžet', 'být', 'chtít', 'cítit', 'číst', 'dát', 'dělá', 'hrát', 'jet', 'jíst', 'jít',
        'jmout', 'klást', 'kryje', 'kupuje', 'ležet', 'maže', 'mine', 'mít', 'moci', 'nese',
        'peče', 'pomoci', 'prosí', 'psát', 'říct', 'řvát', 'sedět', 'spát', 'stát', 'téci', 'tiskne',
        'umět', 'umře', 'vědět', 'vidět', 'vzít', 'zvát',
    ],
];

/**
 * Slovesné třídy: vzor, kterým se třída časuje, a čím se pozná.
 *
 * Sloveso se ukládá vzorem, ne třídou — čtrnáct ze dvaačtyřiceti běží na pojmenovaných vzorech (být,
 * moci, psát), pro které žádná hodnota VerbClass neexistuje. Třída je zkratka k vyplnění vzoru.
 *
 * 'pattern' musí souhlasit s CzechVerbConjugationService::PatternByVerbClass, jinak by administrace
 * ukládala vzor, kterým se sloveso nečasuje. Hlídá PhpSchemaParityTests. 'ending' a 'examples' jsou
 * popisky ve formuláři.
 */
const LEXICON_VERB_CLASSES = [
    'Class1' => ['pattern' => 'trida1', 'ending' => '-e', 'examples' => ['nese', 'bere', 'maže', 'peče', 'umře']],
    'Class2' => ['pattern' => 'trida2', 'ending' => '-ne', 'examples' => ['tiskne', 'mine']],
    'Class3' => ['pattern' => 'trida3', 'ending' => '-je', 'examples' => ['kryje', 'kupuje']],
    'Class4' => ['pattern' => 'trida4', 'ending' => '-í', 'examples' => ['prosí']],
    'Class5' => ['pattern' => 'trida5', 'ending' => '-á', 'examples' => ['dělá']],
];
