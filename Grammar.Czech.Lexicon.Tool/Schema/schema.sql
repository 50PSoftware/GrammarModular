-- Grammar.Czech — lexicon and valency schema.
--
-- ┌───────────────────────────────────────────────────────────────────────────────────────────────┐
-- │ THIS FILE IS FOR SQLITE. For MySQL or MariaDB use schema.mysql.sql instead.                    │
-- │                                                                                                │
-- │ The two are not interchangeable, whatever the portability below suggests. This one has no      │
-- │ AUTO_INCREMENT — deliberately, because on the SQLite side identifiers come from the writer:     │
-- │ the seed files state them outright and a pull carries the server's over unchanged. The admin    │
-- │ does not state them, so on a server built from this file every insert fails with               │
-- │                                                                                                │
-- │     1364 Field 'slot_id' doesn't have a default value                                          │
-- │                                                                                                │
-- │ If that has already happened, repair.mysql-autoincrement.sql fixes it without touching data.    │
-- └───────────────────────────────────────────────────────────────────────────────────────────────┘
--
-- This file is the source of truth for the shape of the dictionary. It is written in portable SQL so
-- that a variant for another engine is a translation rather than a redesign — SQLite is the first
-- backend, not the last one. What "portable" does not mean is that the same file can be run anywhere:
-- each engine has its own variant, differing in exactly the places noted below. Everything
-- SQLite-specific lives in schema.sqlite.sql, which runs after this file.
--
-- Rules the portability rests on, so that later edits keep it:
--   * VARCHAR with an explicit length, never TEXT — MySQL cannot index TEXT without a prefix length.
--   * SMALLINT with CHECK (col IN (0, 1)) for booleans — Firebird gained BOOLEAN only in 3.0 and
--     Microsoft SQL spells it BIT.
--   * Surrogate keys are plain INTEGER and are assigned by the writer, never by AUTO_INCREMENT /
--     IDENTITY / SERIAL, which every engine spells differently.
--   * Enumerations are stored as the C# enum member name and constrained by CHECK. Storing the name
--     rather than the ordinal keeps the file readable in a database browser, which matters because
--     the .db is edited by hand, and it survives members being appended to the enum.
--   * No PRAGMA, no WITHOUT ROWID, no partial or expression indexes.
--
-- Two columns hold a lemma rather than a foreign key: lemma_entry.aspect_counterpart and
-- lemma_entry.base_verb_lemma. A real foreign key is not available for them because lemma_entry is
-- unique on (lemma_key, category, homonym_index) and neither reference carries a category. The
-- validate command of the lexicon tool checks that both resolve.

-- ─────────────────────────────────────────────────────────────────────────────
-- Metadata — guards against opening a stale or half-built database
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE lexicon_meta (
    meta_key    VARCHAR(64)  NOT NULL,
    meta_value  VARCHAR(255),
    CONSTRAINT pk_lexicon_meta PRIMARY KEY (meta_key)
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexeme — the abstract word, holding an aspect pair together
-- ─────────────────────────────────────────────────────────────────────────────
-- dát and dávat are one lexeme with one set of frames. They are separate rows in lemma_entry
-- because their morphology differs — koupit inflects as class 4, kupovat as class 3 — but the
-- arguments they take do not, and the valency they shared was previously copied out twice.
CREATE TABLE lexeme (
    lexeme_id      INTEGER      NOT NULL,
    primary_lemma  VARCHAR(64)  NOT NULL,
    note           VARCHAR(500),
    CONSTRAINT pk_lexeme PRIMARY KEY (lexeme_id)
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entry — the morphological identity of one dictionary form
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE lemma_entry (
    lemma_entry_id                     INTEGER      NOT NULL,

    -- lemma is the form as written; lemma_key is the lookup key, lower-cased by the writer with
    -- ToLowerInvariant rather than by a database collation. SQLite NOCASE folds ASCII only, so Ě and
    -- Č would not match, and a Czech culture collation is the wrong tool for orthography besides.
    lemma                              VARCHAR(64)  NOT NULL,
    lemma_key                          VARCHAR(64)  NOT NULL,

    -- Distinguishes homonyms that share a lemma and a word class — stát the building from stát the
    -- country. 1 for the ordinary case.
    homonym_index                      INTEGER      NOT NULL DEFAULT 1,

    category                           VARCHAR(16)  NOT NULL,
    gender                             VARCHAR(16),
    pattern                            VARCHAR(32),
    is_animate                         SMALLINT,
    has_mobile_e                       SMALLINT,
    has_genitive_plural_shortening     SMALLINT,
    has_epenthesis_in_genitive_plural  SMALLINT,
    is_indeclinable                    SMALLINT,
    is_plural_only                     SMALLINT,
    is_countable                       SMALLINT,
    prefers_short_form                 SMALLINT,
    verb_class                         VARCHAR(16),
    aspect                             VARCHAR(16),
    aspect_counterpart                 VARCHAR(64),
    reflexive_type                     VARCHAR(32)  NOT NULL DEFAULT 'None',
    base_verb_lemma                    VARCHAR(64),

    -- Stems the word inflects on, for verbs whose pattern does not predict them — říct conjugates by
    -- class 1 but forms its past on řek-. Empty is the ordinary case: the pattern decides. They live
    -- on the entry rather than in the embedded irregulars.json so that correcting one verb is an edit
    -- in the admin instead of a rebuild and a release of the library.
    stem                               VARCHAR(32),
    present_stem                       VARCHAR(32),
    past_stem                          VARCHAR(32),
    future_stem                        VARCHAR(32),
    imperative_stem                    VARCHAR(32),
    passive_stem                       VARCHAR(32),

    -- The infinitive when it is not the lemma, as with říct beside říci.
    infinitive                         VARCHAR(64),

    -- 0 for the few verbs that form no passive participle at all — moci has none where pomoci has
    -- pomožen. NULL leaves the answer to the pattern, which is that the verb does form one.
    forms_passive                      SMALLINT,

    -- NULL for a word that takes no arguments, which is most nouns and adjectives.
    lexeme_id                          INTEGER,

    -- Provenance. VALLEX, PDT-Vallex and NomVallex are CC BY-NC-SA and cannot be folded into a
    -- permissively licensed package, so every row has to say where it came from and stay auditable.
    source                             VARCHAR(64),
    is_verified                        SMALLINT     NOT NULL DEFAULT 0,
    note                               VARCHAR(500),

    CONSTRAINT pk_lemma_entry PRIMARY KEY (lemma_entry_id),
    CONSTRAINT uq_lemma_entry_key UNIQUE (lemma_key, category, homonym_index),
    CONSTRAINT fk_lemma_entry_lexeme FOREIGN KEY (lexeme_id) REFERENCES lexeme (lexeme_id),
    CONSTRAINT ck_lemma_entry_category CHECK (category IN (
        'Noun', 'Adjective', 'Pronoun', 'Numerale', 'Verb',
        'Adverb', 'Preposition', 'Conjunction', 'Particle', 'Interjection')),
    CONSTRAINT ck_lemma_entry_gender CHECK (gender IS NULL OR gender IN (
        'Masculine', 'Feminine', 'Neuter')),
    CONSTRAINT ck_lemma_entry_aspect CHECK (aspect IS NULL OR aspect IN (
        'Perfective', 'Imperfective')),
    CONSTRAINT ck_lemma_entry_verb_class CHECK (verb_class IS NULL OR verb_class IN (
        'Class1', 'Class2', 'Class3', 'Class4', 'Class5')),
    CONSTRAINT ck_lemma_entry_reflexive CHECK (reflexive_type IN (
        'None', 'ReflexivumTantum_Se', 'ReflexivumTantum_Si', 'DerivedReflexive_Se',
        'DerivedBenefactive_Si', 'Reciprocal_Se', 'DeagentivePassive_Se')),
    CONSTRAINT ck_lemma_entry_homonym CHECK (homonym_index >= 1),
    CONSTRAINT ck_lemma_entry_animate CHECK (is_animate IS NULL OR is_animate IN (0, 1)),
    CONSTRAINT ck_lemma_entry_mobile_e CHECK (has_mobile_e IS NULL OR has_mobile_e IN (0, 1)),
    CONSTRAINT ck_lemma_entry_gpl_short CHECK (has_genitive_plural_shortening IS NULL OR has_genitive_plural_shortening IN (0, 1)),
    CONSTRAINT ck_lemma_entry_gpl_epen CHECK (has_epenthesis_in_genitive_plural IS NULL OR has_epenthesis_in_genitive_plural IN (0, 1)),
    CONSTRAINT ck_lemma_entry_indecl CHECK (is_indeclinable IS NULL OR is_indeclinable IN (0, 1)),
    CONSTRAINT ck_lemma_entry_plural_only CHECK (is_plural_only IS NULL OR is_plural_only IN (0, 1)),
    CONSTRAINT ck_lemma_entry_countable CHECK (is_countable IS NULL OR is_countable IN (0, 1)),
    CONSTRAINT ck_lemma_entry_short_form CHECK (prefers_short_form IS NULL OR prefers_short_form IN (0, 1)),
    CONSTRAINT ck_lemma_entry_forms_passive CHECK (forms_passive IS NULL OR forms_passive IN (0, 1)),
    CONSTRAINT ck_lemma_entry_verified CHECK (is_verified IN (0, 1))
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexical unit — one sense of a lexeme
-- ─────────────────────────────────────────────────────────────────────────────
-- sense_label is what the frameLabel of the old JSON was: 'transfer', 'motion', 'perception'. It
-- now has a row of its own to be defined in, and the caller still selects a frame by naming it.
CREATE TABLE lexical_unit (
    lu_id         INTEGER      NOT NULL,
    lexeme_id     INTEGER      NOT NULL,
    sense_label   VARCHAR(64),
    gloss         VARCHAR(255),
    ssc_class_id  VARCHAR(32),
    CONSTRAINT pk_lexical_unit PRIMARY KEY (lu_id),
    CONSTRAINT uq_lexical_unit_sense UNIQUE (lexeme_id, sense_label),
    CONSTRAINT fk_lexical_unit_lexeme FOREIGN KEY (lexeme_id) REFERENCES lexeme (lexeme_id)
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Valency frame — one per (lexical unit, diathesis)
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE valency_frame (
    frame_id    INTEGER      NOT NULL,
    lu_id       INTEGER      NOT NULL,
    kind        VARCHAR(32)  NOT NULL DEFAULT 'Verbal',
    diathesis   VARCHAR(32)  NOT NULL DEFAULT 'Active',
    is_default  SMALLINT     NOT NULL DEFAULT 0,
    -- Derived reflexivity, which holds for one sense: dát si kávu needs the particle, dát knihu
    -- Pavlovi does not, and both are the same lemma. Inherent reflexivity — bát se, where no
    -- non-reflexive verb exists — stays on lemma_entry, because there it holds under every frame.
    reflexive_type  VARCHAR(32)  NOT NULL DEFAULT 'None',
    CONSTRAINT pk_valency_frame PRIMARY KEY (frame_id),
    CONSTRAINT uq_valency_frame_diathesis UNIQUE (lu_id, diathesis),
    CONSTRAINT fk_valency_frame_lu FOREIGN KEY (lu_id) REFERENCES lexical_unit (lu_id),
    CONSTRAINT ck_valency_frame_kind CHECK (kind IN (
        'Verbal', 'Copular_NominalPred', 'Copular_AdjectivalPred', 'Existential',
        'Modal', 'PhasalLightVerb', 'LightVerb')),
    CONSTRAINT ck_valency_frame_diathesis CHECK (diathesis IN (
        'Active', 'PassivePeriphrastic', 'ReflexivePassive', 'RecipientDeobjective',
        'Dispositional', 'Resultative')),
    CONSTRAINT ck_valency_frame_default CHECK (is_default IN (0, 1)),
    CONSTRAINT ck_valency_frame_reflexive CHECK (reflexive_type IN (
        'None', 'ReflexivumTantum_Se', 'ReflexivumTantum_Si', 'DerivedReflexive_Se',
        'DerivedBenefactive_Si', 'Reciprocal_Se', 'DeagentivePassive_Se'))
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Valency slot — one argument position of a frame
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE valency_slot (
    slot_id              INTEGER      NOT NULL,
    frame_id             INTEGER      NOT NULL,
    functor              VARCHAR(16)  NOT NULL,
    canonical_order      INTEGER      NOT NULL,
    obligatoriness       VARCHAR(16)  NOT NULL DEFAULT 'Optional',
    can_drop_contextual  SMALLINT     NOT NULL DEFAULT 0,
    can_drop_generic     SMALLINT     NOT NULL DEFAULT 0,

    -- The functor a controlled infinitive shares its subject with — chce přijít. NULL when the slot
    -- controls nothing. A functor rather than a slot id, so that a frame stays self-contained and can
    -- be copied without rewriting the references inside it.
    control_target       VARCHAR(16),

    CONSTRAINT pk_valency_slot PRIMARY KEY (slot_id),
    CONSTRAINT uq_valency_slot_functor UNIQUE (frame_id, functor),
    CONSTRAINT fk_valency_slot_frame FOREIGN KEY (frame_id) REFERENCES valency_frame (frame_id),
    CONSTRAINT ck_valency_slot_functor CHECK (functor IN (
        'ACT', 'PAT', 'ADDR', 'ORIG', 'EFF',
        'DIR1', 'DIR2', 'DIR3', 'LOC', 'MANN', 'MEANS', 'BEN', 'CAUS', 'AIM', 'TWHEN',
        'DIFF', 'OBST', 'INTT', 'MAT', 'THL', 'EXT', 'CRIT', 'ACMP', 'COMPL')),
    CONSTRAINT ck_valency_slot_control CHECK (control_target IS NULL OR control_target IN (
        'ACT', 'PAT', 'ADDR', 'ORIG', 'EFF',
        'DIR1', 'DIR2', 'DIR3', 'LOC', 'MANN', 'MEANS', 'BEN', 'CAUS', 'AIM', 'TWHEN',
        'DIFF', 'OBST', 'INTT', 'MAT', 'THL', 'EXT', 'CRIT', 'ACMP', 'COMPL')),
    CONSTRAINT ck_valency_slot_obligatoriness CHECK (obligatoriness IN (
        'Obligatory', 'Typical', 'Optional')),
    CONSTRAINT ck_valency_slot_order CHECK (canonical_order >= 1),
    CONSTRAINT ck_valency_slot_drop_ctx CHECK (can_drop_contextual IN (0, 1)),
    CONSTRAINT ck_valency_slot_drop_gen CHECK (can_drop_generic IN (0, 1))
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Slot realization — the surface forms one slot may take
-- ─────────────────────────────────────────────────────────────────────────────
-- morph_case is NULL exactly when the slot surfaces as a clause or an infinitive, which is why the
-- column is nullable and why clause_type and takes_infinitive sit beside it. The name avoids the
-- reserved word CASE.
CREATE TABLE slot_realization (
    realization_id    INTEGER      NOT NULL,
    slot_id           INTEGER      NOT NULL,
    morph_case        VARCHAR(16),
    preposition       VARCHAR(16),
    clause_type       VARCHAR(16),
    takes_infinitive  SMALLINT     NOT NULL DEFAULT 0,

    -- 1 is the form to generate. Analysis accepts every row; generation has to pick one, and case
    -- and preposition alone do not say which.
    preference        INTEGER      NOT NULL DEFAULT 1,

    CONSTRAINT pk_slot_realization PRIMARY KEY (realization_id),
    CONSTRAINT fk_slot_realization_slot FOREIGN KEY (slot_id) REFERENCES valency_slot (slot_id),
    CONSTRAINT ck_slot_realization_case CHECK (morph_case IS NULL OR morph_case IN (
        'Nominative', 'Genitive', 'Dative', 'Accusative', 'Vocative', 'Locative', 'Instrumental')),
    CONSTRAINT ck_slot_realization_infinitive CHECK (takes_infinitive IN (0, 1)),
    CONSTRAINT ck_slot_realization_preference CHECK (preference >= 1),

    -- A realization has to be something: a case, a clause, or an infinitive. All three empty would be
    -- a row the generator can do nothing with, and a preposition without a case governs nothing.
    CONSTRAINT ck_slot_realization_shape CHECK (
        morph_case IS NOT NULL OR clause_type IS NOT NULL OR takes_infinitive = 1),
    CONSTRAINT ck_slot_realization_preposition CHECK (
        preposition IS NULL OR morph_case IS NOT NULL)
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Construction — light-verb and idiom templates
-- ─────────────────────────────────────────────────────────────────────────────
-- The slots are held as a JSON document rather than as rows: nothing queries into them, they are read
-- and written whole, and giving them a table would buy joins nobody performs.
CREATE TABLE construction (
    construction_id   INTEGER       NOT NULL,
    pattern_name      VARCHAR(64)   NOT NULL,
    light_verb_lemma  VARCHAR(64)   NOT NULL,
    pred_noun_lemma   VARCHAR(64),
    template_json     VARCHAR(4000) NOT NULL,
    CONSTRAINT pk_construction PRIMARY KEY (construction_id),
    CONSTRAINT uq_construction_name UNIQUE (pattern_name)
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Indexes
-- ─────────────────────────────────────────────────────────────────────────────
CREATE INDEX ix_lemma_entry_lemma_key ON lemma_entry (lemma_key);
CREATE INDEX ix_lemma_entry_lexeme ON lemma_entry (lexeme_id);
CREATE INDEX ix_lemma_entry_base_verb ON lemma_entry (base_verb_lemma);
CREATE INDEX ix_lexical_unit_lexeme ON lexical_unit (lexeme_id);
CREATE INDEX ix_valency_frame_lu ON valency_frame (lu_id);
CREATE INDEX ix_valency_slot_frame ON valency_slot (frame_id);
CREATE INDEX ix_slot_realization_slot ON slot_realization (slot_id);
