-- Grammar.Czech — lexicon seed, update 27.
--
-- Continues seed.000.sql through seed.025.sql. Last ids used there: lexeme 37, lemma_entry 564,
-- lexical_unit 44, valency_frame 63, valency_slot 114, slot_realization 121, construction 3,
-- lemma_variant 1, lemma_sense 2. Append after all twenty-six, in order.
--
-- Provenance: no new headwords. platit/zaplatit (lu 22) is already in the dictionary with an ADDR
-- slot (the payee, dative in the active frame); what is added is the recipient deobjective diathesis
-- that promotes it.
--
-- THE RECIPIENT DEOBJECTIVE, restricted to the pattern Daneš actually documents. Kettnerová–
-- Lopatková–Panevová / MSoČ 2 (2014) name the diathesis "dostat plus the passive participle; ADDR
-- becomes the nominative subject" — the wording the enum's own doc comment already carried. Daneš
-- (Naše řeč 51, 1968, "Dostal jsem přidáno a podobné pasívní konstrukce") is the study of it, and
-- every example there is this shape: Karel dostal (od otce) vyhubováno; dostal (od závodu) přidáno —
-- neuter singular, no expressed patient, actor optional in "od" + genitive.
--
-- WHAT IS DELIBERATELY LEFT OUT, and why:
--
--   * A PAT slot, and the construction NESČ's Diateze entry also shows under the same name — Novomanželé
--     mají/dostali (od úřadu) přidělenu garsonku, participle agreeing with an expressed accusative
--     object. That agreement is jmenné (nominal) declension of the participle, and Czech Wikipedia's own
--     account of jmenné tvary states plainly that in today's language they survive only in the
--     nominative — "mají jen tvar nominativu" — so the oblique-case forms a caller would need are not a
--     rule this grammar can generate, only isolated fixed examples. Writing a PAT slot here would claim
--     a productive pattern that is not attested as one.
--
--   * mít as the alternative auxiliary. NESČ's example uses mají and dostali interchangeably for this
--     diathesis, same as it does for the resultative's je/mám — but the mechanism the composer has today
--     is per-diathesis, one auxiliary per member of the enum, matching how Resultative already commits
--     to mít alone despite je uvařeno being equally attested.
--
--   * Every other verb. One worked example, same as seed.023 and seed.025.
--
-- source is 'IJP' and is_verified 0, matching seed.023 and seed.025: the sources document the
-- construction, not this dictionary's frame for it.

INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default, reflexive_type) VALUES
    (64, 22, 'Verbal', 'RecipientDeobjective', 0, 'None');

INSERT INTO valency_slot (slot_id, frame_id, functor, canonical_order, obligatoriness) VALUES
    (115, 64, 'ACT', 2, 'Optional'),
    (116, 64, 'ADDR', 1, 'Obligatory');

INSERT INTO slot_realization (realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference) VALUES
    (122, 115, 'Genitive', 'od', NULL, 0, 1),
    (123, 116, 'Nominative', NULL, NULL, 0, 1);
