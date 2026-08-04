-- Grammar.Czech — lexicon seed, update 8.
--
-- Continues seed.000.sql through seed.006.sql. Last ids used there: lexeme 28,
-- lemma_entry 251, lexical_unit 29, valency_frame 29, valency_slot 62,
-- slot_realization 64. Append after all seven, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz),
-- like every file before it. Every `source` value stays 'IJP'.
--
-- What this round is FOR: the first lemma held under two word classes. UNIQUE is
-- (lemma_key, category, homonym_index), but until now every lemma_key was unique on its own,
-- so nothing exercised that. stát is the textbook pair — masculine inanimate noun and
-- imperfective verb — and the two rows share nothing, so a lookup that returns the wrong one
-- hands over a vzor from the other word class.
--
-- The verb needed its own vzor in irregulars.json: trida4 alone would conjugate it off the
-- infinitive and produce *státím. That entry carries stem "stoj", pastStem "stá",
-- imperativeStem "stůj".

-- Lexém před heslem, které na něj ukazuje — cizí klíč se kontroluje hned při vložení.
INSERT INTO lexeme (lexeme_id, primary_lemma)
VALUES
    (29, 'stát');

INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    (252, 'stát', 'stát', 1, 'Noun', 'Masculine', 'hrad', 0, 0, NULL, NULL, NULL, 'IJP', 1,
          'Homonymum se slovesem stát níž. Neživotné: G sg. státu, N pl. státy.'),
    (253, 'stát', 'stát', 1, 'Verb', NULL, 'stát', NULL, NULL, 'Imperfective', NULL, 29, 'IJP', 1,
          'Homonymum s podstatným jménem stát výš. Vid nemá protějšek prefixací — postát ani '
          || 'zůstat nejsou vidové dvojice, jsou to jiná slovesa.');

INSERT INTO lexical_unit (lu_id, lexeme_id, sense_label, gloss, ssc_class_id)
VALUES
    (30, 29, 'position', 'být ve svislé poloze na místě', NULL);

INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default)
VALUES
    (30, 30, 'Verbal', 'Active', 1);

INSERT INTO valency_slot (
    slot_id, frame_id, functor, canonical_order, obligatoriness,
    can_drop_contextual, can_drop_generic, control_target)
VALUES
    (63, 30, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    -- Kde se stojí, je u tohohle významu obligatorní: „stojím" bez místa je elipsa, ne úplná věta.
    (64, 30, 'LOC', 2, 'Obligatory', 1, 0, NULL);

INSERT INTO slot_realization (
    realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
VALUES
    (65, 63, 'Nominative', NULL, NULL, 0, 1),
    (66, 64, 'Locative', 'na', NULL, 0, 1),
    (67, 64, 'Locative', 'v', NULL, 0, 2),
    (68, 64, 'Genitive', 'u', NULL, 0, 3);
