-- Grammar.Czech — lexicon seed, update 22.
--
-- Continues seed.000.sql through seed.020.sql. Last ids used there: lexeme 37,
-- lemma_entry 281, lexical_unit 44, valency_frame 45, valency_slot 82, slot_realization 89,
-- construction 3, lemma_variant 1, lemma_sense 2. Append after all twenty-one, in order.
--
-- Provenance: the lemmas and their spelling from the Internetová jazyková příručka; the functor from
-- the Functional Generative Description, the annotation scheme the valency half of this dictionary
-- already uses.
--
-- FIRST ADVERBS IN THE DICTIONARY, and the reason is the new column. Until now every adverb lived in
-- the embedded Data/Rules/adverbs.json, which holds 291 of them with their irregular comparatives —
-- and that is where they stay. What could not live there is `adverbial_functor`: which circumstance
-- the adverb expresses. Correcting it has to be an edit in the admin rather than a release of the
-- library, the same reasoning that put the verb stems on lemma_entry instead of in irregulars.json.
--
-- So an adverb now has two homes and they say different things. adverbs.json says how it compares,
-- which is morphology; lemma_entry says what it means for a sentence, which is a fact about the word.
-- An adverb in the JSON and not here still works exactly as before — it is recognized, and the caller
-- states the role. One that is here gets its role without being asked.
--
-- Twenty-one entries, in three groups the source's own definitions settle without a judgement call:
--
--   TWHEN  dnes, včera, zítra, brzy, pozdě, dlouho     — answer "kdy" and "jak dlouho"
--   LOC    doma, tady, zde, venku, vpravo, vlevo,      — answer "kde"
--          nahoře, dole
--   MANN   rychle, pomalu, dobře, špatně, tiše,        — answer "jak"
--          hlasitě, pěšky
--
-- WHAT IS DELIBERATELY LEFT OUT, and why:
--
--   * The other 270 adverbs in the JSON. Most are manner adverbs derived from adjectives and would be
--     MANN, but "most" is not a rule and this file is not the place to guess two hundred times. They
--     are added when somebody has a reason to add them.
--
--   * dlouho is TWHEN and not THL, although THL is precisely "jak dlouho". The two are one question in
--     Czech for this word — "dlouho jsem čekal" is duration, "dlouho potom" is position in time — and
--     the column holds one answer. TWHEN is the wider of the two, so it is the one that does not lie
--     in the other reading. A word that is only ever duration would take THL.
--
--   * vpravo, vlevo, nahoře and dole are LOC and not DIR3, though the same words answer "kam" too:
--     "je vpravo" against "jdi vpravo". Which one holds is decided by the verb, not by the adverb, and
--     the verb is not what this column is about. LOC is the reading the word has standing alone.
--
--   * Comparatives are not copied here. lemma_entry has no column for them, and inventing one would
--     move data out of adverbs.json without any reason to — the JSON answers that question already and
--     answers it for all 291.
--
-- source stays 'IJP' for the lemma; is_verified is 0, because the functor is not something IJP states
-- and nobody has checked these against a corpus.

INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category,
    is_indeclinable, adverbial_functor, source, is_verified, note) VALUES
    (282, 'dnes',    'dnes',    1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (283, 'včera',   'včera',   1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (284, 'zítra',   'zítra',   1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (285, 'brzy',    'brzy',    1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (286, 'pozdě',   'pozdě',   1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (287, 'dlouho',  'dlouho',  1, 'Adverb', 1, 'TWHEN', 'IJP', 0,
          'TWHEN, ne THL: v jednom čtení je to trvání, ve druhém poloha v čase, a sloupec drží jednu odpověď.'),

    (288, 'doma',    'doma',    1, 'Adverb', 1, 'LOC',   'IJP', 0, NULL),
    (289, 'tady',    'tady',    1, 'Adverb', 1, 'LOC',   'IJP', 0, NULL),
    (290, 'zde',     'zde',     1, 'Adverb', 1, 'LOC',   'IJP', 0, NULL),
    (291, 'venku',   'venku',   1, 'Adverb', 1, 'LOC',   'IJP', 0, NULL),
    (292, 'vpravo',  'vpravo',  1, 'Adverb', 1, 'LOC',   'IJP', 0,
          'LOC, ne DIR3: kam proti kde rozhoduje sloveso, ne příslovce.'),
    (293, 'vlevo',   'vlevo',   1, 'Adverb', 1, 'LOC',   'IJP', 0, NULL),
    (294, 'nahoře',  'nahoře',  1, 'Adverb', 1, 'LOC',   'IJP', 0, NULL),
    (295, 'dole',    'dole',    1, 'Adverb', 1, 'LOC',   'IJP', 0, NULL),

    (296, 'rychle',  'rychle',  1, 'Adverb', 1, 'MANN',  'IJP', 0, NULL),
    (297, 'pomalu',  'pomalu',  1, 'Adverb', 1, 'MANN',  'IJP', 0, NULL),
    (298, 'dobře',   'dobře',   1, 'Adverb', 1, 'MANN',  'IJP', 0, NULL),
    (299, 'špatně',  'špatně',  1, 'Adverb', 1, 'MANN',  'IJP', 0, NULL),
    (300, 'tiše',    'tiše',    1, 'Adverb', 1, 'MANN',  'IJP', 0, NULL),
    (301, 'hlasitě', 'hlasitě', 1, 'Adverb', 1, 'MANN',  'IJP', 0, NULL),
    (302, 'pěšky',   'pěšky',   1, 'Adverb', 1, 'MANN',  'IJP', 0, NULL);
