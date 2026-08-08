-- Grammar.Czech — lexicon seed, update 14.
--
-- Continues seed.000.sql through seed.012.sql. Last ids used there: lexeme 30,
-- lemma_entry 264, lexical_unit 34, valency_frame 35, valency_slot 78,
-- slot_realization 85, construction 0. Append after all thirteen, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz) for the
-- morphology, and from the light verb literature — Kettnerová & Kolářová, Deverbal Nouns in Czech
-- Light Verb Constructions (2017) — for the analysis. Every `source` value stays 'IJP'.
--
-- What this round is FOR: the first rows of the `construction` table, which the schema has carried
-- since the beginning and which nothing has ever filled.
--
-- A light verb construction is a predicate whose meaning sits in a noun while the verb contributes
-- little beyond tense. It is here because its valency is not the verb's: `mít` governs an accusative
-- and nothing else, yet `mít zájem` governs `o` with the accusative too, and no frame of `mít` can
-- account for that without claiming that every use of the verb takes it. Reading `mít zájem o knihu`
-- through the possess frame leaves `o knihu` unaccounted for, which is exactly what happened before
-- these rows existed.
--
-- The noun is recorded as CPHR, the way the Prague Dependency Treebank annotates the nominal half of
-- such a predicate. It is not a patient: it is part of what is being predicated, and the patient is
-- the thing the interest is *in*.
--
-- WHAT IS DELIBERATELY LEFT OUT:
--
--   * `dělat starosti`, `brát ohled`, `mít radost` and the rest of the usual list. Three patterns are
--     enough to show that the mechanism carries; the inventory is a corpus job and wants doing against
--     frequency data rather than from memory.
--   * The plural-only patterns (`dělat starosti`, `mít nervy`). The number belongs to the CPHR word
--     and the caller states it, so nothing here is missing — but the ones that *must* be plural need a
--     way of saying so, and `construction` has no column for it yet.
--   * Aspectual pairs of the light verb (`dát pozor` beside `dávat pozor`). The verb of a construction
--     is a lemma, not a lexeme, so each member needs its own row; only the imperfective is written
--     here rather than half of each pair.
--   * `mít` as a phasal or modal light verb (`mít co dělat`, `mít pravdu`). Different kinds under
--     ValencyKind, and each wants its own reading before it gets a row.
--
-- The nouns come first, because a construction whose CPHR has no lemma_entry cannot be declined.

INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, has_genitive_plural_shortening,
    aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    -- Pohyblivé e: zájem → zájmu. Bez toho by z něj byl 'zájemu'.
    (265, 'zájem', 'zájem', 1, 'Noun', 'Masculine', 'hrad', 0, 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Jmenná část konstrukce mít zájem o něco.'),
    (266, 'pozor', 'pozor', 1, 'Noun', 'Masculine', 'hrad', 0, 0, 0, NULL, NULL, NULL, 'IJP', 1,
          'Jmenná část konstrukce dávat pozor na něco.'),
    (267, 'strach', 'strach', 1, 'Noun', 'Masculine', 'hrad', 0, 0, 0, NULL, NULL, NULL, 'IJP', 1,
          'Jmenná část konstrukce mít strach z něčeho.');

-- Sloty jsou v template_json ve stejném tvaru, v jakém je nesou valency_slot a slot_realization —
-- konstrukce se tím čte jako rámec, kterým se stává, a nikdo se nemusí učit druhý slovník na totéž.
-- Celý vzorec je jedna jednotka: edituje se vcelku a rozložit ho do tří tabulek by koupilo joiny
-- a ztratilo možnost přečíst si ho na jeden pohled.

INSERT INTO construction (
    construction_id, pattern_name, light_verb_lemma, pred_noun_lemma, template_json)
VALUES
    (1, 'LVC.mít.zájem', 'mít', 'zájem',
     '{"slots":[' ||
     '{"functor":"ACT","order":1,"obligatoriness":"Obligatory","forms":[{"case":"Nominative"}]},' ||
     '{"functor":"CPHR","order":2,"obligatoriness":"Obligatory","forms":[{"case":"Accusative"}]},' ||
     '{"functor":"PAT","order":3,"obligatoriness":"Typical","forms":[{"case":"Accusative","preposition":"o"}]}]}'),

    (2, 'LVC.dávat.pozor', 'dávat', 'pozor',
     '{"slots":[' ||
     '{"functor":"ACT","order":1,"obligatoriness":"Obligatory","forms":[{"case":"Nominative"}]},' ||
     '{"functor":"CPHR","order":2,"obligatoriness":"Obligatory","forms":[{"case":"Accusative"}]},' ||
     '{"functor":"PAT","order":3,"obligatoriness":"Typical","forms":[{"case":"Accusative","preposition":"na"}]}]}'),

    (3, 'LVC.mít.strach', 'mít', 'strach',
     '{"slots":[' ||
     '{"functor":"ACT","order":1,"obligatoriness":"Obligatory","forms":[{"case":"Nominative"}]},' ||
     '{"functor":"CPHR","order":2,"obligatoriness":"Obligatory","forms":[{"case":"Accusative"}]},' ||
     '{"functor":"PAT","order":3,"obligatoriness":"Typical","forms":[{"case":"Genitive","preposition":"z"}]}]}');
