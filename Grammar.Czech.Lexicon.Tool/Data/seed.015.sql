-- Grammar.Czech — lexicon seed, update 16.
--
-- Continues seed.000.sql through seed.014.sql. Last ids used there: lexeme 33,
-- lemma_entry 270, lexical_unit 37, valency_frame 38, valency_slot 78,
-- slot_realization 85, construction 3. Append after all fifteen, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz). Every `source`
-- value stays 'IJP'.
--
-- What this round is FOR: the weather verbs seed.014.sql left out, and it leaves them out no longer.
-- The reasons given there did not hold up:
--
--   * "the reflexive of an impersonal verb wants a look" was not a reason, it was not having looked.
--     It works: an impersonal frame carrying se produces Stmívá se, and the cluster lands correctly
--     with no constituent in front of the verb.
--
--   * "mrznout and hřmít have a reading with a subject, so writing them as impersonal would make that
--     reading unreachable" was true of writing ONE frame and false of the schema, which holds a frame
--     per sense and was built for exactly this. Voda mrzne and Mrzne are two senses, so they are two
--     frames, and the weather one is marked default because that is what the bare verb means.
--
-- WHERE THE VOCABULARY DOES NOT QUITE FIT, said out loud rather than papered over. `blýskat` has a
-- non-reflexive use (blýskat očima), so its se belongs to the weather sense and not to the lemma —
-- which is what valency_frame.reflexive_type is for. But the ReflexiveType members are named for
-- lemmas: ReflexivumTantum_Se says "no non-reflexive verb exists", which is false of the lemma and
-- true of this sense. It is the closest thing the enum has and the sense-level column is the right
-- home; the name is the part that is wrong, and renaming it is a bigger change than this file.
--
-- STILL LEFT OUT, with reasons that do hold:
--
--   * The instrumental of blýskat očima. It is a MEANS on the flash sense and wants deciding against
--     corpus data, not from memory; the ACT-only frame already keeps that sense reachable.
--   * `rozednít se`, `setmět se` — the perfective counterparts. They pair with the imperfectives here,
--     and aspect_counterpart wants both rows written together rather than one pointing at nothing.

INSERT INTO lexeme (lexeme_id, primary_lemma, note) VALUES
    (34, 'stmívat', 'Jen bezpodměťové; protějšek rozednít se čeká.'),
    (35, 'blýskat', 'Dva významy: bezpodměťové blýská se a blýskat něčím.'),
    (36, 'mrznout', 'Dva významy: bezpodměťové mrzne a voda mrzne.'),
    (37, 'hřmít',   'Dva významy: bezpodměťové hřmí a hrom hřmí.');

-- Zvratnost stmívat se je na hesle, ne na rámci: nezvratné 'stmívat' neexistuje, takže platí pod
-- každým významem. U blýskat je to obráceně a sedí na rámci níž.
--
-- past_stem u hřmít, protože obecná 4. třída odvodí z infinitivu 'hřmíl' a správně je hřmělo.
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, has_genitive_plural_shortening,
    aspect, aspect_counterpart, verb_class, reflexive_type, past_stem,
    lexeme_id, source, is_verified, note)
VALUES
    (271, 'stmívat', 'stmívat', 1, 'Verb', NULL, 'trida5', NULL, NULL, NULL,
          'Imperfective', NULL, 'Class5', 'ReflexivumTantum_Se', NULL, 34, 'IJP', 1,
          'Bezpodměťové: stmívá se.'),
    (272, 'blýskat', 'blýskat', 1, 'Verb', NULL, 'trida5', NULL, NULL, NULL,
          'Imperfective', NULL, 'Class5', 'None', NULL, 35, 'IJP', 1,
          'Zvratnost jen u významu o počasí, proto na rámci.'),
    (273, 'mrznout', 'mrznout', 1, 'Verb', NULL, 'trida2', NULL, NULL, NULL,
          'Imperfective', NULL, 'Class2', 'None', NULL, 36, 'IJP', 1,
          'Bezpodměťové mrzne i voda mrzne.'),
    (274, 'hřmít',   'hřmít',   1, 'Verb', NULL, 'trida4', NULL, NULL, NULL,
          'Imperfective', NULL, 'Class4', 'None', 'hřmě', 37, 'IJP', 1,
          'Minulý kmen zapsán: obecná třída by dala hřmíl.');

INSERT INTO lexical_unit (lu_id, lexeme_id, sense_label, gloss) VALUES
    (38, 34, 'weather', 'Nastává tma.'),
    (39, 35, 'weather', 'Objevují se blesky.'),
    (40, 35, 'flash',   'Vydávat záblesky.'),
    (41, 36, 'weather', 'Je mráz.'),
    (42, 36, 'freeze',  'Měnit se v led.'),
    (43, 37, 'weather', 'Ozývá se hrom.'),
    (44, 37, 'sound',   'Vydávat hřmot.');

-- Význam o počasí je u všech tří výchozí: holé 'mrzne' je o počasí, ne o vodě.
INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default, reflexive_type) VALUES
    (39, 38, 'Impersonal', 'Active', 1, 'None'),
    (40, 39, 'Impersonal', 'Active', 1, 'ReflexivumTantum_Se'),
    (41, 40, 'Verbal',     'Active', 0, 'None'),
    (42, 41, 'Impersonal', 'Active', 1, 'None'),
    (43, 42, 'Verbal',     'Active', 0, 'None'),
    (44, 43, 'Impersonal', 'Active', 1, 'None'),
    (45, 44, 'Verbal',     'Active', 0, 'None');

INSERT INTO valency_slot (slot_id, frame_id, functor, canonical_order, obligatoriness) VALUES
    (79, 41, 'ACT', 1, 'Obligatory'),
    (80, 43, 'ACT', 1, 'Obligatory'),
    (81, 45, 'ACT', 1, 'Obligatory');

INSERT INTO slot_realization (realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference) VALUES
    (86, 79, 'Nominative', NULL, NULL, 0, 1),
    (87, 80, 'Nominative', NULL, NULL, 0, 1),
    (88, 81, 'Nominative', NULL, NULL, 0, 1);
