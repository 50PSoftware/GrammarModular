-- Grammar.Czech — lexicon seed, update 24.
--
-- Continues seed.000.sql through seed.022.sql. Last ids used there: lexeme 37, lemma_entry 564,
-- lexical_unit 44, valency_frame 45, valency_slot 82, slot_realization 89, construction 3,
-- lemma_variant 1, lemma_sense 2. Append after all twenty-three, in order.
--
-- Provenance: no new headwords. Every lemma here is already in the dictionary; what is added is a
-- frame for a diathesis those verbs have and the dictionary did not record. The diatheses themselves
-- are the five Kettnerová–Lopatková–Panevová (2014) describe for Czech, and the enum has named all
-- five since the schema was first written.
--
-- FIRST NON-PASSIVE DIATHESES. The dictionary held 44 active frames and one periphrastic passive, so
-- Diathesis had five members of which two were reachable. This adds the deagentive and the
-- dispositional to two verbs each, which is enough for the mechanism to be exercised rather than
-- merely declared.
--
--   deagentive     Pracovalo se. Mluvilo se.
--                  The actor is not demoted to an instrumental as in the passive — it is gone. What is
--                  left is a subjectless clause with the reflexive particle, which is why the frame is
--                  Impersonal and carries no ACT slot at all. Being Impersonal is also what gets the
--                  past participle into the neuter singular, the same path pršet already takes.
--
--   dispositional  Pracovalo se mi dobře.
--                  Says how the action disposes towards whoever performs it. The actor comes back, but
--                  in the dative and optionally — hence Optional on the slot — and the clause is still
--                  subjectless. A manner adverbial is what the construction is for; it is not in the
--                  frame because MANN is a free modification and free modifications are not slots.
--
-- WHAT IS DELIBERATELY LEFT OUT, and why:
--
--   * The resultative (mám napsáno) and the recipient deagentive (dostat + participle). Both need a
--     second verb — mít and dostat — carrying a participle of the first, which is a form the composer
--     does not build today. Naming them in the enum costs nothing and pretending the dictionary can
--     express them would cost the next person an afternoon.
--
--   * The other 42 verbs. A deagentive frame is not something a verb has by rule: it needs an actor
--     that can go unsaid, so pršet cannot have one and dát barely can. These four are the clear cases.
--
--   * A dispositional frame for mluvit. Mluvilo se mi dobře is fine Czech, but the frame would be a
--     copy of the one for pracovat and one worked example of each is what this file is for.
--
-- source is 'IJP' and is_verified 0: IJP documents the constructions, not this dictionary's frames.

-- Deagentiv. Bez slotů, protože konatel není odsunutý, ale žádný — Impersonal je totéž, co dělá 'prší'.
INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default, reflexive_type) VALUES
    (46, 23, 'Impersonal', 'ReflexivePassive', 0, 'DeagentivePassive_Se'),
    (47, 11, 'Impersonal', 'ReflexivePassive', 0, 'DeagentivePassive_Se');

-- Dispoziční. Konatel se vrací, ale v dativu a nepovinně; věta zůstává bez podmětu.
INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default, reflexive_type) VALUES
    (48, 23, 'Impersonal', 'Dispositional', 0, 'DeagentivePassive_Se');

INSERT INTO valency_slot (slot_id, frame_id, functor, canonical_order, obligatoriness) VALUES
    (83, 48, 'ACT', 1, 'Optional');

INSERT INTO slot_realization (realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference) VALUES
    (90, 83, 'Dative', NULL, NULL, 0, 1);
