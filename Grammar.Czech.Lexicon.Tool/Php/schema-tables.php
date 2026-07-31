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
        'base_verb_lemma', 'lexeme_id', 'source', 'is_verified', 'note',
    ],
    'lexical_unit' => ['lu_id', 'lexeme_id', 'sense_label', 'gloss', 'ssc_class_id'],
    'valency_frame' => ['frame_id', 'lu_id', 'kind', 'diathesis', 'is_default'],
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
