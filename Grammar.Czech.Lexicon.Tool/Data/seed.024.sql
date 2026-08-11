-- Grammar.Czech — lexicon seed, update 25.
--
-- Continues seed.000.sql through seed.023.sql. Last ids used there: lexeme 37, lemma_entry 564,
-- lexical_unit 44, valency_frame 48, valency_slot 83, slot_realization 90, construction 3,
-- lemma_variant 1, lemma_sense 2. Append after all twenty-four, in order.
--
-- Provenance: no new headwords and no new senses. Every frame here is derived from an active frame the
-- dictionary already holds, by the regular Czech passive: the actor drops to the instrumental, the
-- patient rises to the nominative, and the rest of the slots are left where they were.
--
-- FILLING IN THE DIATHESIS THAT WAS ALREADY BUILT. The dictionary held 44 active frames, one
-- periphrastic passive and three from seed.023 — a mechanism ahead of its data. This adds the passive
-- to fourteen senses, so that asking for PAT as the subject answers for more than one verb.
--
-- WHY THESE FOURTEEN, and not every frame with an accusative patient:
--
--   Having a PAT in the accusative is not the same as forming a passive. mít, chtít and vědět govern
--   the accusative too, and *je mít, *je chtěn and *je věděno are not Czech. What is here are activity
--   verbs, where the passive is ordinary: vidět, dělat, kupovat, tisknout, psát, číst, hrát, volat,
--   vařit, platit, stavět, kreslit, zpívat, poslouchat — each with its aspectual counterpart, which
--   shares the lexeme and therefore the frame.
--
-- WHAT IS DELIBERATELY LEFT OUT, and why:
--
--   * The reflexive passive (kniha se čte). It is the commoner of the two in Czech and it is the next
--     thing to add, but it needs a decision first: in that diathesis the actor cannot be expressed at
--     all, so the frame would carry no ACT slot — and validate currently requires one of every frame
--     that is not Impersonal, while Impersonal is barred from having a subject. The rule has to be
--     revisited before the data can be written, and that is not a decision a seed file should make.
--
--   * The deagentive and the dispositional beyond the two verbs in seed.023. Whether the actor of a
--     given verb can go unsaid is a fact about that verb, not a rule, and fourteen more judgements do
--     not belong in the same file as a mechanical derivation.
--
--   * starat (se) and dát in the konzumace sense. Both are reflexive and their patients are not plain
--     accusative objects; deriving a passive from them by the rule above would produce a frame nobody
--     would use.
--
-- source is not restated: these frames hang off senses whose provenance the earlier seeds record.

INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default, reflexive_type) VALUES
    (49, 4, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (50, 5, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (51, 6, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (52, 7, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (53, 8, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (54, 9, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (55, 10, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (56, 20, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (57, 21, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (58, 22, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (59, 26, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (60, 27, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (61, 28, 'Verbal', 'PassivePeriphrastic', 0, 'None'),
    (62, 29, 'Verbal', 'PassivePeriphrastic', 0, 'None');

INSERT INTO valency_slot (slot_id, frame_id, functor, canonical_order, obligatoriness) VALUES
    (84, 49, 'ACT', 1, 'Obligatory'),
    (85, 49, 'PAT', 2, 'Obligatory'),
    (86, 50, 'ACT', 1, 'Obligatory'),
    (87, 50, 'PAT', 2, 'Obligatory'),
    (88, 51, 'ACT', 1, 'Obligatory'),
    (89, 51, 'PAT', 2, 'Obligatory'),
    (90, 52, 'ACT', 1, 'Obligatory'),
    (91, 52, 'PAT', 2, 'Obligatory'),
    (92, 53, 'ACT', 1, 'Obligatory'),
    (93, 53, 'PAT', 2, 'Typical'),
    (94, 53, 'ADDR', 3, 'Optional'),
    (95, 54, 'ACT', 1, 'Obligatory'),
    (96, 54, 'PAT', 2, 'Typical'),
    (97, 55, 'ACT', 1, 'Obligatory'),
    (98, 55, 'PAT', 2, 'Optional'),
    (99, 56, 'ACT', 1, 'Obligatory'),
    (100, 56, 'PAT', 2, 'Typical'),
    (101, 57, 'ACT', 1, 'Obligatory'),
    (102, 57, 'PAT', 2, 'Typical'),
    (103, 58, 'ACT', 1, 'Obligatory'),
    (104, 58, 'PAT', 2, 'Typical'),
    (105, 58, 'ADDR', 3, 'Optional'),
    (106, 59, 'ACT', 1, 'Obligatory'),
    (107, 59, 'PAT', 2, 'Typical'),
    (108, 60, 'ACT', 1, 'Obligatory'),
    (109, 60, 'PAT', 2, 'Typical'),
    (110, 61, 'ACT', 1, 'Obligatory'),
    (111, 61, 'PAT', 2, 'Optional'),
    (112, 62, 'ACT', 1, 'Obligatory'),
    (113, 62, 'PAT', 2, 'Typical');

INSERT INTO slot_realization (realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference) VALUES
    (91, 84, 'Instrumental', NULL, NULL, 0, 1),
    (92, 85, 'Nominative', NULL, NULL, 0, 1),
    (93, 86, 'Instrumental', NULL, NULL, 0, 1),
    (94, 87, 'Nominative', NULL, NULL, 0, 1),
    (95, 88, 'Instrumental', NULL, NULL, 0, 1),
    (96, 89, 'Nominative', NULL, NULL, 0, 1),
    (97, 90, 'Instrumental', NULL, NULL, 0, 1),
    (98, 91, 'Nominative', NULL, NULL, 0, 1),
    (99, 92, 'Instrumental', NULL, NULL, 0, 1),
    (100, 93, 'Nominative', NULL, NULL, 0, 1),
    (101, 94, 'Dative', NULL, NULL, 0, 1),
    (102, 95, 'Instrumental', NULL, NULL, 0, 1),
    (103, 96, 'Nominative', NULL, NULL, 0, 1),
    (104, 97, 'Instrumental', NULL, NULL, 0, 1),
    (105, 98, 'Nominative', NULL, NULL, 0, 1),
    (106, 99, 'Instrumental', NULL, NULL, 0, 1),
    (107, 100, 'Nominative', NULL, NULL, 0, 1),
    (108, 101, 'Instrumental', NULL, NULL, 0, 1),
    (109, 102, 'Nominative', NULL, NULL, 0, 1),
    (110, 103, 'Instrumental', NULL, NULL, 0, 1),
    (111, 104, 'Nominative', NULL, NULL, 0, 1),
    (112, 105, 'Dative', NULL, NULL, 0, 1),
    (113, 106, 'Instrumental', NULL, NULL, 0, 1),
    (114, 107, 'Nominative', NULL, NULL, 0, 1),
    (115, 108, 'Instrumental', NULL, NULL, 0, 1),
    (116, 109, 'Nominative', NULL, NULL, 0, 1),
    (117, 110, 'Instrumental', NULL, NULL, 0, 1),
    (118, 111, 'Nominative', NULL, NULL, 0, 1),
    (119, 112, 'Instrumental', NULL, NULL, 0, 1),
    (120, 113, 'Nominative', NULL, NULL, 0, 1);
