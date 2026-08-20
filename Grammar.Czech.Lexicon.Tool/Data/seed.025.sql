-- Grammar.Czech — lexicon seed, update 26.
--
-- Continues seed.000.sql through seed.024.sql. Last ids used there: lexeme 37, lemma_entry 564,
-- lexical_unit 44, valency_frame 62, valency_slot 113, slot_realization 120, construction 3,
-- lemma_variant 1, lemma_sense 2. Append after all twenty-five, in order.
--
-- Provenance: no new headwords. psát/napsat (lu 8) is already in the dictionary; what is added is the
-- one diathesis seed.023 named and deliberately left out.
--
-- THE RESULTATIVE, NOW THAT THE COMPOSER BUILDS IT. seed.023 held off on "mám napsáno" because mít
-- carrying a participle of another verb was not a form CzechWordFormComposer built yet. It does now
-- (CzechWordRequest.Diathesis, CzechWordFormComposer's Resultative branch, CzechAuxiliaryVerbService.
-- GetHaveForm), so the frame that names it can be written.
--
-- Rezultativní diateze (NESČ, Diateze; MSoČ 2, 2014): "mám uvařeno" — mít governs the sentence as an
-- ordinary verb, and the participle stays neuter singular no matter what is written, which is why the
-- frame is Verbal with a plain nominative ACT and no other slot. Unlike the deagentive/dispositional it
-- keeps a real subject, so it is not Impersonal and carries no reflexive particle.
--
-- WHAT IS DELIBERATELY LEFT OUT, and why:
--
--   * A PAT slot. "Mám napsáno" and "Má uklizeno" — the two examples the diathesis was named from — do
--     not express an object; where one is said, it agrees with the participle instead of staying neuter
--     ("mám napsaný dopis"), which is the attributive adjective, a different construction the composer
--     does not build under this diathesis. Adding PAT here would claim a case government this frame does
--     not have evidence for.
--
--   * Every other verb. One worked example is what this file is for, same as seed.023.
--
--   * The recipient deagentive (dostat + participle). Still needs its own composer path.
--
-- source is 'IJP' and is_verified 0, matching seed.023: the sources document the construction, not this
-- dictionary's frame for it.

INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default, reflexive_type) VALUES
    (63, 8, 'Verbal', 'Resultative', 0, 'None');

INSERT INTO valency_slot (slot_id, frame_id, functor, canonical_order, obligatoriness) VALUES
    (114, 63, 'ACT', 1, 'Obligatory');

INSERT INTO slot_realization (realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference) VALUES
    (121, 114, 'Nominative', NULL, NULL, 0, 1);
