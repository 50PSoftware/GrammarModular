-- Grammar.Czech — lexicon seed, update 17.
--
-- Continues seed.000.sql through seed.015.sql. Last ids used there: lexeme 37,
-- lemma_entry 274, lexical_unit 44, valency_frame 45, valency_slot 81,
-- slot_realization 88, construction 3. Append after all sixteen, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz). Every `source`
-- value stays 'IJP'.
--
-- What this round is FOR: the two things seed.015.sql said it was leaving out. Both are here now.
--
-- 1. THE INSTRUMENTAL OF blýskat očima. seed.015.sql called it a MEANS, and that was a guess made in
--    passing. It is a PAT. Czech has a whole class of verbs whose patient stands in the bare
--    instrumental — mávat rukou, kroutit hlavou, házet kamenem — and blýskat očima belongs to it: the
--    eyes are what is being moved, not what the moving is done with. A MEANS would say the flashing
--    was accomplished by means of eyes, which is a different sentence.
--
-- 2. THE PERFECTIVE COUNTERPARTS. An aspect pair is one lexeme with two lemma_entry rows, so the
--    perfective goes under the lexeme that already exists and inherits its frames through it — the
--    impersonal frame written in seed.015.sql covers both members without a second copy. That is the
--    whole reason the lexeme layer is there.
--
--    aspect_counterpart is symmetric in every pair already in the dictionary (dělat ↔ udělat), so the
--    imperfective halves need theirs filled in. They were written in seed.015.sql, hence the UPDATE at
--    the end — the same shape seed.011.sql used to flag káva from seed.008.sql. Splitting one
--    symmetric fact across two files is worse than an UPDATE that says which half it is completing.
--
-- STILL LEFT OUT:
--
--   * A perfective for `pršet`. Neither zapršet nor naprršet is the neutral counterpart the way
--     nasněžit is for sněžit — pršet is one of the verbs that simply has none worth a row, and
--     inventing one to make the table look even would be the wrong kind of tidy.
--   * `blýsknout se`, the perfective of the weather sense. It is a semelfactive rather than a plain
--     counterpart — one flash against a stretch of them — and that distinction has no column yet.
--
-- PAST STEMS. setmět needs none — trida4 gets to setmělo on its own. rozednít does: the class derives
-- the past from the infinitive and gives rozednílo, with the long í of the infinitive carried into a
-- syllable that shortens. Both were run before this was written; the first draft of this file claimed
-- they had been and only setmět had.

-- Patiens v holém instrumentálu, jako u mávat rukou.
INSERT INTO valency_slot (slot_id, frame_id, functor, canonical_order, obligatoriness) VALUES
    (82, 41, 'PAT', 2, 'Optional');

INSERT INTO slot_realization (realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference) VALUES
    (89, 82, 'Instrumental', NULL, NULL, 0, 1);

-- Dokonavé protějšky. Žádný nový lexém: jdou pod ten, který už stojí, a rámce po něm dědí.
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, has_genitive_plural_shortening,
    aspect, aspect_counterpart, verb_class, reflexive_type, past_stem,
    lexeme_id, source, is_verified, note)
VALUES
    (275, 'setmět',    'setmět',    1, 'Verb', NULL, 'trida4', NULL, NULL, NULL,
          'Perfective', 'stmívat', 'Class4', 'ReflexivumTantum_Se', NULL, 34, 'IJP', 1,
          'Bezpodměťové: setmělo se. Rámec dědí po lexému stmívat.'),
    (276, 'nasněžit',  'nasněžit',  1, 'Verb', NULL, 'trida4', NULL, NULL, NULL,
          'Perfective', 'sněžit',  'Class4', 'None', NULL, 32, 'IJP', 1,
          'Bezpodměťové: nasněžilo.'),
    (277, 'rozednít',  'rozednít',  1, 'Verb', NULL, 'trida4', NULL, NULL, NULL,
          'Perfective', 'svítat',  'Class4', 'ReflexivumTantum_Se', 'rozedni', 33, 'IJP', 1,
          'Minulý kmen zapsán: třída by dala rozednílo. Se svítat je to dvojice suplativní.');

-- Druhá polovina dvojice. Řádky ze seed.014 a seed.015 protějšek ještě neznaly, protože v tu chvíli
-- neexistoval; symetrii drží každá dvojice ve slovníku a tyhle tři nemají být výjimka.
UPDATE lemma_entry SET aspect_counterpart = 'setmět'   WHERE lemma_entry_id = 271;
UPDATE lemma_entry SET aspect_counterpart = 'nasněžit' WHERE lemma_entry_id = 269;
UPDATE lemma_entry SET aspect_counterpart = 'rozednít' WHERE lemma_entry_id = 270;
