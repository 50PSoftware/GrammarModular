-- Grammar.Czech — lexicon seed, update 18.
--
-- Continues seed.000.sql through seed.016.sql. Last ids used there: lexeme 37,
-- lemma_entry 277, lexical_unit 44, valency_frame 45, valency_slot 82,
-- slot_realization 89, construction 3. Append after all seventeen, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz). Every `source`
-- value stays 'IJP'.
--
-- What this round is FOR: the last two things seed.016.sql held back, and the two it did not notice.
-- Every weather verb in the dictionary now has its perfective counterpart.
--
-- 1. napršet. seed.016.sql said pršet had no counterpart worth a row "the way nasněžit is for sněžit".
--    That was wrong twice. The word it dismissed was spelled naprršet, which is not a word; the word
--    is napršet, and Napršelo is ordinary Czech. And the standard it applied was inconsistent —
--    nasněžit is the same prefixal resultative, so accepting one and refusing the other drew a line
--    where the language has none.
--
-- 2. blýsknout. Held back as "a semelfactive rather than a plain counterpart, and that distinction has
--    no column yet". The second half is true and the conclusion did not follow. A semelfactive is
--    perfective, and aspect_counterpart is the field for the other member of the pair; recording it
--    loses the difference between one flash and a stretch of them, while leaving it out loses the word.
--    The nuance goes in `note`, which is where a fact the columns cannot hold belongs.
--
-- 3. zahřmět and zmrznout, which nobody had held back — they were simply missed. hřmít and mrznout got
--    their second senses in seed.015.sql and no counterpart, so the set was uneven for no reason at
--    all. Zahřmělo and Zmrzlo are as ordinary as the rest.
--
-- No past_stem on any of them: all four were run through the conjugator before this was written and
-- come out right on their class — napršelo, blýsklo se, zahřmělo, zmrzlo. That check is here because
-- rozednít needed one and seed.016.sql claimed to have made it and had not.
--
-- blýsknout takes no reflexive_type of its own. The se belongs to the weather sense, sits on frame 40,
-- and reaches the perfective through the lexeme — which is the same inheritance the frames use and
-- wants no second copy.
--
-- STILL LEFT OUT, and this time the reason is the schema and not a judgement:
--
--   * The semelfactive/iterative distinction itself. blýsknout se against blýskat se, zahřmět against
--     hřmít — both pairs differ in more than aspect, and VerbAspect has two members. Recording it
--     needs a column, and a column needs the schema, the PHP map and a migration; it is a change of
--     its own rather than a line in a seed.

INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, has_genitive_plural_shortening,
    aspect, aspect_counterpart, verb_class, reflexive_type,
    lexeme_id, source, is_verified, note)
VALUES
    (278, 'napršet',   'napršet',   1, 'Verb', NULL, 'trida4', NULL, NULL, NULL,
          'Perfective', 'pršet',   'Class4', 'None', 31, 'IJP', 1,
          'Bezpodměťové: napršelo.'),
    (279, 'blýsknout', 'blýsknout', 1, 'Verb', NULL, 'trida2', NULL, NULL, NULL,
          'Perfective', 'blýskat', 'Class2', 'None', 35, 'IJP', 1,
          'Semelfaktivum: jeden záblesk proti trvání. VerbAspect ten rozdíl nenese, nese ho tahle věta.'),
    (280, 'zahřmět',   'zahřmět',   1, 'Verb', NULL, 'trida4', NULL, NULL, NULL,
          'Perfective', 'hřmít',   'Class4', 'None', 37, 'IJP', 1,
          'Semelfaktivum, stejně jako blýsknout. Minulý kmen netřeba: zahřmělo vychází ze třídy.'),
    (281, 'zmrznout',  'zmrznout',  1, 'Verb', NULL, 'trida2', NULL, NULL, NULL,
          'Perfective', 'mrznout', 'Class2', 'None', 36, 'IJP', 1,
          'Kryje oba významy lexému: zmrzlo i voda zmrzla.');

-- Druhá polovina každé dvojice, ze seedů 014 a 015.
UPDATE lemma_entry SET aspect_counterpart = 'napršet'   WHERE lemma_entry_id = 268;
UPDATE lemma_entry SET aspect_counterpart = 'blýsknout' WHERE lemma_entry_id = 272;
UPDATE lemma_entry SET aspect_counterpart = 'zmrznout'  WHERE lemma_entry_id = 273;
UPDATE lemma_entry SET aspect_counterpart = 'zahřmět'   WHERE lemma_entry_id = 274;

-- Poznámky u lexémů 31 a 33 tvrdily, že protějšek není nebo čeká. Obojí přestalo platit.
UPDATE lexeme SET note = 'Vidová dvojice pršet / napršet.' WHERE lexeme_id = 31;
UPDATE lexeme SET note = 'Vidová dvojice sněžit / nasněžit.' WHERE lexeme_id = 32;
UPDATE lexeme SET note = 'Vidová dvojice svítat / rozednít — suplativní, jiný kořen.' WHERE lexeme_id = 33;
