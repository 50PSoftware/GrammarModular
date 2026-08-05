-- Grammar.Czech — lexicon seed, update 10.
--
-- Continues seed.000.sql through seed.008.sql. Last ids used there: lexeme 30,
-- lemma_entry 255, lexical_unit 32, valency_frame 32, valency_slot 68,
-- slot_realization 72. Append after all nine, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz)
-- and the valency description in NESČ. Every `source` value stays 'IJP'.
--
-- What this round is FOR: the first frame that is not Active. valency_frame has carried a
-- diathesis column and a UNIQUE (lu_id, diathesis) since the schema was written, and until now
-- every one of the thirty-two rows said 'Active' — so nothing ever exercised what the column is
-- for, and nothing read it.
--
-- A diathesis remaps every slot at once, which is why it is a frame of its own and not something
-- computed from the active one:
--
--   * ACT stops being the subject and becomes an optional instrumental adjunct — dána učitelem.
--     It is the one slot the passive exists in order to demote, so it is droppable both ways.
--   * PAT stops being the accusative object and becomes the nominative subject — Kniha byla dána.
--     It is obligatory here in a way it is not in the active frame: a passive with nothing
--     promoted is the impersonal construction, which is a different frame again.
--   * ADDR and DIR3 are untouched. The passive reaches past them.
--
-- Only the transfer sense of dát gets one. That is deliberate: it is the frame every other test
-- already leans on, so the pair active/passive is visible against something known, and one row is
-- enough to prove the column is wired. The other twenty-nine senses keep answering the way they
-- did, through the licensing check in CzechSentenceBuilder.

INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default, reflexive_type)
VALUES
    (33, 1, 'Verbal', 'PassivePeriphrastic', 0, 'None');

INSERT INTO valency_slot (
    slot_id, frame_id, functor, canonical_order, obligatoriness,
    can_drop_contextual, can_drop_generic, control_target)
VALUES
    -- Konatel je tu tím, co se odsouvá — „Kniha byla dána“ je úplná věta, agens se doplňovat nemusí.
    (69, 33, 'ACT',  1, 'Optional',   1, 1, NULL),
    (70, 33, 'PAT',  2, 'Obligatory', 0, 0, NULL),
    (71, 33, 'ADDR', 3, 'Typical',    1, 0, NULL),
    (72, 33, 'DIR3', 4, 'Optional',   1, 0, NULL);

INSERT INTO slot_realization (
    realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
VALUES
    (73, 69, 'Instrumental', NULL, NULL, 0, 1),
    (74, 70, 'Nominative',   NULL, NULL, 0, 1),
    (75, 71, 'Dative',       NULL, NULL, 0, 1),
    (76, 72, 'Accusative',   'na', NULL, 0, 1);
