-- Grammar.Czech — lexicon seed, update 9.
--
-- Continues seed.000.sql through seed.007.sql. Last ids used there: lexeme 29,
-- lemma_entry 253, lexical_unit 30, valency_frame 30, valency_slot 64,
-- slot_realization 68. Append after all eight, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz),
-- like every file before it. Every `source` value stays 'IJP'.
--
-- What this round is FOR: the two kinds of reflexivity, one row of each, so that the new
-- valency_frame.reflexive_type has something to be read from.
--
--   * dát si kávu — the particle belongs to the sense, not to the lemma. dát already has the
--     transfer sense (dát knihu Pavlovi), which takes none. The two senses differ in exactly the
--     thing the new column states, so the pair is what makes it testable at all.
--   * starat se — the particle belongs to the lemma, because no non-reflexive *starat exists.
--     That stays on lemma_entry, where it holds under every frame the verb has.
--
-- Why starat and not bát: bát is the textbook reflexivum tantum but conjugates irregularly
-- (bojím, boj, bál) and would need its own vzor in irregulars.json. starat is trida5 like dát,
-- so it exercises the column and nothing else.
--
-- Note that lu 31 hangs off lexeme 1, which dát and dávat share, so dávat si kávu comes with it.

-- Lexém před heslem, které na něj ukazuje — cizí klíč se kontroluje hned při vložení.
INSERT INTO lexeme (lexeme_id, primary_lemma, note)
VALUES
    (30, 'starat', 'Reflexivum tantum — nereflexivní *starat neexistuje.');

INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, reflexive_type, lexeme_id,
    source, is_verified, note)
VALUES
    (254, 'káva', 'káva', 1, 'Noun', 'Feminine', 'žena', 0, 0, NULL, NULL, 'None', NULL,
          'IJP', 1, NULL),

    -- Vid nemá protějšek zapsaný: postarat se je perfektivum, ale ve slovníku zatím není a
    -- odkaz na neexistující heslo by byl horší než mlčení.
    (255, 'starat', 'starat', 1, 'Verb', NULL, 'trida5', NULL, NULL, 'Imperfective', NULL,
          'ReflexivumTantum_Se', 30, 'IJP', 1,
          'Reflexivum tantum: částice patří heslu, ne významu, proto stojí tady a ne na rámci.');

INSERT INTO lexical_unit (lu_id, lexeme_id, sense_label, gloss)
VALUES
    (31, 1,  'konzumace', 'Dát si něco k jídlu nebo pití.'),
    (32, 30, 'care',      'Pečovat o někoho nebo o něco.');

-- is_default zůstává u transferu (rámec 1). Konzumace je druhý význam téhož lexému, takže
-- volající, který nejmenuje popisek, dostane od CzechValencyService chybu — a to je správně,
-- „dát“ bez upřesnění je opravdu dvojznačné.
INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default, reflexive_type)
VALUES
    (31, 31, 'Verbal', 'Active', 0, 'DerivedBenefactive_Si'),
    (32, 32, 'Verbal', 'Active', 1, 'None');

INSERT INTO valency_slot (
    slot_id, frame_id, functor, canonical_order, obligatoriness,
    can_drop_contextual, can_drop_generic, control_target)
VALUES
    -- dát / dávat, konzumace
    (65, 31, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (66, 31, 'PAT', 2, 'Obligatory', 0, 0, NULL),

    -- starat, care
    (67, 32, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    -- O co se stará, je obligatorní: „staral se“ bez předmětu je elipsa, ne úplná věta.
    (68, 32, 'PAT', 2, 'Obligatory', 1, 0, NULL);

INSERT INTO slot_realization (
    realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
VALUES
    (69, 65, 'Nominative', NULL, NULL, 0, 1),
    (70, 66, 'Accusative', NULL, NULL, 0, 1),
    (71, 67, 'Nominative', NULL, NULL, 0, 1),
    (72, 68, 'Accusative', 'o',  NULL, 0, 1);
