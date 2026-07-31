-- Grammar.Czech — lexicon and valency schema, MySQL and MariaDB variant.
--
-- The counterpart of schema.sql for the central, editable copy of the dictionary. The tables, columns
-- and constraints are the same; what differs is only what these engines spell differently.
--
-- Target: MySQL 8.0.16+ or MariaDB 10.2.1+, which is where CHECK constraints began to be enforced
-- rather than parsed and discarded. On anything older the constraints are accepted and ignored, and an
-- enum value the C# side cannot parse gets into the data unnoticed — run the lexicon tool's validate
-- against an exported copy if you are stuck there.
--
-- The collations here are deliberately the ones both engines have. utf8mb4_0900_ai_ci is MySQL 8 only:
-- MariaDB does not know it and refuses the whole script with "Unknown collation", which is how this
-- was found. utf8mb4_unicode_ci and utf8mb4_bin exist in both and carry the same properties the
-- reasoning below depends on.
--
-- What differs from schema.sql, and why:
--
--   * AUTO_INCREMENT on every surrogate key. The server is the only assigner of ids — the local SQLite
--     copy is a replica that carries them over verbatim and never renumbers, because a renumbered
--     replica cannot be compared against the server again. Explicit ids in an INSERT are still
--     accepted and simply advance the counter, so seed.sql replays here unchanged.
--
--   * Collation, which is the one thing here that can quietly corrupt Czech data.
--     utf8mb4_unicode_ci is accent-insensitive: under it 'dát' and 'dat' are the same string, so
--     UNIQUE (lemma_key, category, homonym_index) would reject one of them as a duplicate of the
--     other, and a lookup for one would return the other. Every column that is matched rather than
--     read by a human is therefore utf8mb4_bin: the lookup key, and every column holding a C# enum
--     member name, where 'Perfective' and 'perfective' must not compare equal because Enum.TryParse is
--     called case-sensitively. Columns meant for human eyes — lemma, gloss, note, source — keep the
--     accent-insensitive default so that admin search stays forgiving.
--
--   * ENGINE=InnoDB, for foreign keys. MyISAM parses them and does not enforce them.
--
-- Not carried over: PRAGMA. Its SQLite equivalents in schema.sqlite.sql have no MySQL counterpart —
-- foreign keys are always enforced by InnoDB, and the schema version lives in lexicon_meta.

SET NAMES utf8mb4;

-- ─────────────────────────────────────────────────────────────────────────────
-- Metadata
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE lexicon_meta (
    meta_key    VARCHAR(64)  COLLATE utf8mb4_bin NOT NULL,
    meta_value  VARCHAR(255),
    CONSTRAINT pk_lexicon_meta PRIMARY KEY (meta_key)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexeme — the abstract word, holding an aspect pair together
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE lexeme (
    lexeme_id      INT          NOT NULL AUTO_INCREMENT,
    primary_lemma  VARCHAR(64)  NOT NULL,
    note           VARCHAR(500),
    CONSTRAINT pk_lexeme PRIMARY KEY (lexeme_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entry — the morphological identity of one dictionary form
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE lemma_entry (
    lemma_entry_id                     INT          NOT NULL AUTO_INCREMENT,

    lemma                              VARCHAR(64)  NOT NULL,
    lemma_key                          VARCHAR(64)  COLLATE utf8mb4_bin NOT NULL,
    homonym_index                      INT          NOT NULL DEFAULT 1,

    category                           VARCHAR(16)  COLLATE utf8mb4_bin NOT NULL,
    gender                             VARCHAR(16)  COLLATE utf8mb4_bin,
    pattern                            VARCHAR(32)  COLLATE utf8mb4_bin,
    is_animate                         SMALLINT,
    has_mobile_e                       SMALLINT,
    has_genitive_plural_shortening     SMALLINT,
    has_epenthesis_in_genitive_plural  SMALLINT,
    is_indeclinable                    SMALLINT,
    is_plural_only                     SMALLINT,
    is_countable                       SMALLINT,
    prefers_short_form                 SMALLINT,
    verb_class                         VARCHAR(16)  COLLATE utf8mb4_bin,
    aspect                             VARCHAR(16)  COLLATE utf8mb4_bin,
    aspect_counterpart                 VARCHAR(64)  COLLATE utf8mb4_bin,
    reflexive_type                     VARCHAR(32)  COLLATE utf8mb4_bin NOT NULL DEFAULT 'None',
    base_verb_lemma                    VARCHAR(64)  COLLATE utf8mb4_bin,

    lexeme_id                          INT,

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
    CONSTRAINT ck_lemma_entry_verified CHECK (is_verified IN (0, 1))
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexical unit — one sense of a lexeme
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE lexical_unit (
    lu_id         INT          NOT NULL AUTO_INCREMENT,
    lexeme_id     INT          NOT NULL,
    sense_label   VARCHAR(64)  COLLATE utf8mb4_bin,
    gloss         VARCHAR(255),
    ssc_class_id  VARCHAR(32)  COLLATE utf8mb4_bin,
    CONSTRAINT pk_lexical_unit PRIMARY KEY (lu_id),
    CONSTRAINT uq_lexical_unit_sense UNIQUE (lexeme_id, sense_label),
    CONSTRAINT fk_lexical_unit_lexeme FOREIGN KEY (lexeme_id) REFERENCES lexeme (lexeme_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- ─────────────────────────────────────────────────────────────────────────────
-- Valency frame — one per (lexical unit, diathesis)
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE valency_frame (
    frame_id    INT          NOT NULL AUTO_INCREMENT,
    lu_id       INT          NOT NULL,
    kind        VARCHAR(32)  COLLATE utf8mb4_bin NOT NULL DEFAULT 'Verbal',
    diathesis   VARCHAR(32)  COLLATE utf8mb4_bin NOT NULL DEFAULT 'Active',
    is_default  SMALLINT     NOT NULL DEFAULT 0,
    CONSTRAINT pk_valency_frame PRIMARY KEY (frame_id),
    CONSTRAINT uq_valency_frame_diathesis UNIQUE (lu_id, diathesis),
    CONSTRAINT fk_valency_frame_lu FOREIGN KEY (lu_id) REFERENCES lexical_unit (lu_id),
    CONSTRAINT ck_valency_frame_kind CHECK (kind IN (
        'Verbal', 'Copular_NominalPred', 'Copular_AdjectivalPred', 'Existential',
        'Modal', 'PhasalLightVerb', 'LightVerb')),
    CONSTRAINT ck_valency_frame_diathesis CHECK (diathesis IN (
        'Active', 'PassivePeriphrastic', 'ReflexivePassive', 'RecipientDeobjective',
        'Dispositional', 'Resultative')),
    CONSTRAINT ck_valency_frame_default CHECK (is_default IN (0, 1))
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- ─────────────────────────────────────────────────────────────────────────────
-- Valency slot — one argument position of a frame
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE valency_slot (
    slot_id              INT          NOT NULL AUTO_INCREMENT,
    frame_id             INT          NOT NULL,
    functor              VARCHAR(16)  COLLATE utf8mb4_bin NOT NULL,
    canonical_order      INT          NOT NULL,
    obligatoriness       VARCHAR(16)  COLLATE utf8mb4_bin NOT NULL DEFAULT 'Optional',
    can_drop_contextual  SMALLINT     NOT NULL DEFAULT 0,
    can_drop_generic     SMALLINT     NOT NULL DEFAULT 0,
    control_target       VARCHAR(16)  COLLATE utf8mb4_bin,

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
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- ─────────────────────────────────────────────────────────────────────────────
-- Slot realization — the surface forms one slot may take
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE slot_realization (
    realization_id    INT          NOT NULL AUTO_INCREMENT,
    slot_id           INT          NOT NULL,
    morph_case        VARCHAR(16)  COLLATE utf8mb4_bin,
    preposition       VARCHAR(16)  COLLATE utf8mb4_bin,
    clause_type       VARCHAR(16)  COLLATE utf8mb4_bin,
    takes_infinitive  SMALLINT     NOT NULL DEFAULT 0,
    preference        INT          NOT NULL DEFAULT 1,

    CONSTRAINT pk_slot_realization PRIMARY KEY (realization_id),
    CONSTRAINT fk_slot_realization_slot FOREIGN KEY (slot_id) REFERENCES valency_slot (slot_id),
    CONSTRAINT ck_slot_realization_case CHECK (morph_case IS NULL OR morph_case IN (
        'Nominative', 'Genitive', 'Dative', 'Accusative', 'Vocative', 'Locative', 'Instrumental')),
    CONSTRAINT ck_slot_realization_infinitive CHECK (takes_infinitive IN (0, 1)),
    CONSTRAINT ck_slot_realization_preference CHECK (preference >= 1),
    CONSTRAINT ck_slot_realization_shape CHECK (
        morph_case IS NOT NULL OR clause_type IS NOT NULL OR takes_infinitive = 1),
    CONSTRAINT ck_slot_realization_preposition CHECK (
        preposition IS NULL OR morph_case IS NOT NULL)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- ─────────────────────────────────────────────────────────────────────────────
-- Construction — light-verb and idiom templates
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE construction (
    construction_id   INT           NOT NULL AUTO_INCREMENT,
    pattern_name      VARCHAR(64)   COLLATE utf8mb4_bin NOT NULL,
    light_verb_lemma  VARCHAR(64)   COLLATE utf8mb4_bin NOT NULL,
    pred_noun_lemma   VARCHAR(64)   COLLATE utf8mb4_bin,
    template_json     VARCHAR(4000) NOT NULL,
    CONSTRAINT pk_construction PRIMARY KEY (construction_id),
    CONSTRAINT uq_construction_name UNIQUE (pattern_name)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- ─────────────────────────────────────────────────────────────────────────────
-- Indexes
-- ─────────────────────────────────────────────────────────────────────────────
-- MySQL indexes the leading columns of a UNIQUE constraint on its own, so lemma_key needs no separate
-- index here — uq_lemma_entry_key already covers a lookup by it. The others match schema.sql.
CREATE INDEX ix_lemma_entry_lexeme ON lemma_entry (lexeme_id);
CREATE INDEX ix_lemma_entry_base_verb ON lemma_entry (base_verb_lemma);
CREATE INDEX ix_lexical_unit_lexeme ON lexical_unit (lexeme_id);
CREATE INDEX ix_valency_frame_lu ON valency_frame (lu_id);
CREATE INDEX ix_valency_slot_frame ON valency_slot (frame_id);
CREATE INDEX ix_slot_realization_slot ON slot_realization (slot_id);
