-- Grammar.Czech — lexicon seed, update 2.
--
-- Extends the original seed.sql (lemma_entry 1–12, lexeme 1–3, lexical_unit 1–4, valency_frame 1–4,
-- valency_slot 1–10, slot_realization 1–10) with the next block of the most frequent, IJP-verified
-- vocabulary. Every id below continues that sequence — append this file after seed.sql, don't replace it.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz) and
-- czechency.org/slovnik. Nothing derived from VALLEX, PDT-Vallex or NomVallex (CC BY-NC-SA).
--
-- What was deliberately left OUT, and why — so the gaps are a decision, not an oversight:
--   * dům, stůl, rok, syn, bratr, král, učitel — each has a vowel alternation, suppletive plural, or
--     historically-soft stem (dům→domu, stůl→stolu, rok→léta, syn→synové, král→krále not "krála")
--     that the current NounPattern data does not yet model as a plain pattern. They need either a
--     stem override in irregulars.json or a pattern refinement first, not a bare lemma_entry row.
--   * kupovat/kamna-type mobile-e patterns beyond what pes/den/otec already prove — not repeated here.
--   * sníst, as the perfective of jíst — the consonant drops (jím → sním, not the regular prefix +
--     stem concatenation IsPrefixedDerivative checks for), so it needs its own irregulars.json row
--     before it can be a safe aspect_counterpart. jíst is seeded without one rather than a wrong one.
--   * mluvit, myslet, bydlet, sedět — seeded as simplex imperfectives with no aspect_counterpart.
--     Real Czech does have moves in that direction (promluvit, pomyslet, posedět) but each shifts
--     meaning (a snippet of speech, a passing thought, sitting for a while) rather than giving a
--     pure aspectual twin, so forcing one in would misstate the aspect pair the way dát/dávat is not.
--
-- Verb pattern choice, so a reviewer doesn't have to re-derive it from CzechWordStructureResolver:
--   * trida5 (-at/-át, stem = infinitive minus 2): dělat/udělat.
--   * trida3 (-ovat, stem = infinitive minus 4 + u): kupovat. Note koupit is trida4, not trida3 —
--     the aspect pair changes conjugation class, which is exactly the case the schema's separate
--     lexeme/lemma_entry split exists for (see schema.sql's own comment on this very pair).
--   * trida4 (-it/-ít/-et/-ět, stem = infinitive minus 2): koupit, mluvit, myslet, bydlet.
--   * trida2 (-nout, stem = infinitive minus 4): tisknout/vytisknout.
--   * Named irregular patterns (explicit stems in Verbs/irregulars.json, prefix stripped and
--     reattached automatically when the remainder opens with the pattern's own infinitive):
--     psát/napsat, číst/přečíst, hrát/zahrát, sedět, být, mít, chtít, moci, vědět.

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexemes — one per aspect pair (or per simplex verb with no pair)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lexeme (lexeme_id, primary_lemma, note) VALUES
    (4,  'dělat',   'Vidová dvojice dělat / udělat.'),
    (5,  'kupovat', 'Vidová dvojice kupovat / koupit — vidový protějšek mění třídu časování (3 → 4).'),
    (6,  'tisknout','Vidová dvojice tisknout / vytisknout.'),
    (7,  'psát',    'Vidová dvojice psát / napsat.'),
    (8,  'číst',    'Vidová dvojice číst / přečíst.'),
    (9,  'hrát',    'Vidová dvojice hrát / zahrát.'),
    (10, 'mluvit',  'Bez čistého vidového protějšku — promluvit posouvá význam.'),
    (11, 'myslet',  'Bez čistého vidového protějšku.'),
    (12, 'bydlet',  'Imperfektivum tantum v běžném významu trvalého bydliště.'),
    (13, 'sedět',   'Bez čistého vidového protějšku — posedět mění vid. čas (chvíli).'),
    (14, 'být',     'Sponové/existenční sloveso, bez vidového protějšku.'),
    (15, 'mít',     'Bez vidového protějšku.'),
    (16, 'chtít',   'Bez vidového protějšku.'),
    (17, 'moci',    'Modální sloveso, bez vidového protějšku.'),
    (18, 'vědět',   'Bez vidového protějšku — dozvědět se je odvozené zvratné sloveso, ne vidový pár.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    -- pán (masc. anim., tvrdý, gen. -a)
    (13, 'žák',       'žák',       1, 'Noun', 'Masculine', 'pán', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (14, 'soused',    'soused',    1, 'Noun', 'Masculine', 'pán', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (15, 'kamarád',   'kamarád',   1, 'Noun', 'Masculine', 'pán', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (16, 'bratranec', 'bratranec', 1, 'Noun', 'Masculine', 'pán', 1, 1, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- muž (masc. anim., měkký, gen. -e)
    (17, 'lékař',     'lékař',     1, 'Noun', 'Masculine', 'muž', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (18, 'cizinec',   'cizinec',   1, 'Noun', 'Masculine', 'muž', 1, 1, NULL, NULL, NULL, 'IJP', 1, NULL),
    (19, 'chlapec',   'chlapec',   1, 'Noun', 'Masculine', 'muž', 1, 1, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- soudce (masc. anim., -ce)
    (20, 'soudce',    'soudce',    1, 'Noun', 'Masculine', 'soudce', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- předseda (masc. anim., gen. -y, nom. pl. -ové)
    (21, 'předseda',  'předseda',  1, 'Noun', 'Masculine', 'předseda', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (22, 'kolega',    'kolega',    1, 'Noun', 'Masculine', 'předseda', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- hrad (masc. inanim., tvrdý, gen. -u)
    (23, 'hrad',      'hrad',      1, 'Noun', 'Masculine', 'hrad', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (24, 'strom',     'strom',     1, 'Noun', 'Masculine', 'hrad', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (25, 'obchod',    'obchod',    1, 'Noun', 'Masculine', 'hrad', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (26, 'svět',      'svět',      1, 'Noun', 'Masculine', 'hrad', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (27, 'čas',       'čas',       1, 'Noun', 'Masculine', 'hrad', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- les (masc. inanim., měkký přes hrad, gen. -a)
    (28, 'les',       'les',       1, 'Noun', 'Masculine', 'les', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- stroj (masc. inanim., měkký, gen. -e)
    (29, 'stroj',     'stroj',     1, 'Noun', 'Masculine', 'stroj', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (30, 'klíč',      'klíč',      1, 'Noun', 'Masculine', 'stroj', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (31, 'pokoj',     'pokoj',     1, 'Noun', 'Masculine', 'stroj', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (32, 'čaj',       'čaj',       1, 'Noun', 'Masculine', 'stroj', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (33, 'kraj',      'kraj',      1, 'Noun', 'Masculine', 'stroj', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- žena (fem., tvrdý, gen. -y)
    (34, 'žena',      'žena',      1, 'Noun', 'Feminine', 'žena', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (35, 'kniha',     'kniha',     1, 'Noun', 'Feminine', 'žena', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (36, 'škola',     'škola',     1, 'Noun', 'Feminine', 'žena', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (37, 'voda',      'voda',      1, 'Noun', 'Feminine', 'žena', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (38, 'ryba',      'ryba',      1, 'Noun', 'Feminine', 'žena', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- růže (fem., měkký, gen. -e)
    (39, 'růže',      'růže',      1, 'Noun', 'Feminine', 'růže', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (40, 'ulice',     'ulice',     1, 'Noun', 'Feminine', 'růže', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (41, 'práce',     'práce',     1, 'Noun', 'Feminine', 'růže', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (42, 'situace',   'situace',   1, 'Noun', 'Feminine', 'růže', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- píseň (fem., gen. -ě — the DTN/labial reversal in NormalizeEndingOrthography applies here)
    (43, 'píseň',     'píseň',     1, 'Noun', 'Feminine', 'píseň', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (44, 'větev',     'větev',     1, 'Noun', 'Feminine', 'píseň', 0, 0, NULL, NULL, NULL, 'IJP', 1,
         'Ověřovací slovo pro labiodentální větev ě→e reverze v CzechOrthographyService.'),
    (45, 'třešeň',    'třešeň',    1, 'Noun', 'Feminine', 'píseň', 0, 0, NULL, NULL, NULL, 'IJP', 1,
         'Ověřovací slovo pro DTN větev ě→e reverze v CzechOrthographyService.'),

    -- kost (fem., gen. -i)
    (46, 'kost',      'kost',      1, 'Noun', 'Feminine', 'kost', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (47, 'myš',       'myš',       1, 'Noun', 'Feminine', 'kost', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (48, 'noc',       'noc',       1, 'Noun', 'Feminine', 'kost', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (49, 'věc',       'věc',       1, 'Noun', 'Feminine', 'kost', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- město (neut., gen. -a)
    (50, 'město',     'město',     2, 'Noun', 'Neuter', 'město', 0, 0, NULL, NULL, NULL, 'IJP', 1,
         'Homonym_index 2 — id 6 už zabírá lemma_key "město" pod stejnou kategorií z původního seedu.'),
    (51, 'auto',      'auto',      1, 'Noun', 'Neuter', 'město', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (52, 'okno',      'okno',      1, 'Noun', 'Neuter', 'město', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (53, 'slovo',     'slovo',     1, 'Noun', 'Neuter', 'město', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- moře (neut., gen. -e)
    (54, 'moře',      'moře',      1, 'Noun', 'Neuter', 'moře', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (55, 'srdce',     'srdce',     1, 'Noun', 'Neuter', 'moře', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (56, 'letiště',   'letiště',   1, 'Noun', 'Neuter', 'moře', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- kuře (neut., mláďata, gen. -ete)
    (57, 'kuře',      'kuře',      1, 'Noun', 'Neuter', 'kuře', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (58, 'house',     'house',     1, 'Noun', 'Neuter', 'kuře', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (59, 'štěně',     'štěně',     1, 'Noun', 'Neuter', 'kuře', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- stavení (neut., gen. -í)
    (60, 'stavení',   'stavení',   1, 'Noun', 'Neuter', 'stavení', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (61, 'nádraží',   'nádraží',   1, 'Noun', 'Neuter', 'stavení', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (62, 'náměstí',   'náměstí',   1, 'Noun', 'Neuter', 'stavení', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- Adjectives — tvrdý (mladý)
    (63, 'starý',     'starý',     1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (64, 'nový',      'nový',      1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (65, 'velký',     'velký',     1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
         'Komparativ větší je suplativní, viz CzechAdjectiveDeclensionService._supletives.'),
    (66, 'malý',      'malý',      1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
         'Komparativ menší je suplativní.'),
    (67, 'dobrý',     'dobrý',     1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
         'Komparativ lepší je suplativní.'),
    (68, 'dlouhý',    'dlouhý',    1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
         'Komparativ delší je suplativní.'),
    (69, 'hezký',     'hezký',     1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (70, 'černý',     'černý',     1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (71, 'bílý',      'bílý',      1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (72, 'červený',   'červený',   1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (73, 'vysoký',    'vysoký',    1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- Adjectives — měkký (jarní)
    (74, 'letní',     'letní',     1, 'Adjective', NULL, 'jarní', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (75, 'zimní',     'zimní',     1, 'Adjective', NULL, 'jarní', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (76, 'denní',     'denní',     1, 'Adjective', NULL, 'jarní', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (77, 'domácí',    'domácí',    1, 'Adjective', NULL, 'jarní', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (78, 'cizí',      'cizí',      1, 'Adjective', NULL, 'jarní', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- Verbs
    (79, 'dělat',     'dělat',     1, 'Verb', NULL, 'trida5', NULL, NULL, 'Imperfective', 'udělat',  4,  'IJP', 1, NULL),
    (80, 'udělat',    'udělat',    1, 'Verb', NULL, 'trida5', NULL, NULL, 'Perfective',   'dělat',   4,  'IJP', 1, NULL),
    (81, 'kupovat',   'kupovat',   1, 'Verb', NULL, 'trida3', NULL, NULL, 'Imperfective', 'koupit',  5,  'IJP', 1, NULL),
    (82, 'koupit',    'koupit',    1, 'Verb', NULL, 'trida4', NULL, NULL, 'Perfective',   'kupovat', 5,  'IJP', 1, NULL),
    (83, 'tisknout',  'tisknout',  1, 'Verb', NULL, 'trida2', NULL, NULL, 'Imperfective', 'vytisknout', 6, 'IJP', 1, NULL),
    (84, 'vytisknout','vytisknout',1, 'Verb', NULL, 'trida2', NULL, NULL, 'Perfective',   'tisknout',  6, 'IJP', 1, NULL),
    (85, 'psát',      'psát',      1, 'Verb', NULL, 'psát',   NULL, NULL, 'Imperfective', 'napsat',  7,  'IJP', 1, NULL),
    (86, 'napsat',    'napsat',    1, 'Verb', NULL, 'psát',   NULL, NULL, 'Perfective',   'psát',    7,  'IJP', 1, NULL),
    (87, 'číst',      'číst',      1, 'Verb', NULL, 'číst',   NULL, NULL, 'Imperfective', 'přečíst', 8,  'IJP', 1, NULL),
    (88, 'přečíst',   'přečíst',   1, 'Verb', NULL, 'číst',   NULL, NULL, 'Perfective',   'číst',    8,  'IJP', 1, NULL),
    (89, 'hrát',      'hrát',      1, 'Verb', NULL, 'hrát',   NULL, NULL, 'Imperfective', 'zahrát',  9,  'IJP', 1, NULL),
    (90, 'zahrát',    'zahrát',    1, 'Verb', NULL, 'hrát',   NULL, NULL, 'Perfective',   'hrát',    9,  'IJP', 1, NULL),
    (91, 'mluvit',    'mluvit',    1, 'Verb', NULL, 'trida4', NULL, NULL, 'Imperfective', NULL, 10, 'IJP', 1, NULL),
    (92, 'myslet',    'myslet',    1, 'Verb', NULL, 'trida4', NULL, NULL, 'Imperfective', NULL, 11, 'IJP', 1, NULL),
    (93, 'bydlet',    'bydlet',    1, 'Verb', NULL, 'trida4', NULL, NULL, 'Imperfective', NULL, 12, 'IJP', 1, NULL),
    (94, 'sedět',     'sedět',     1, 'Verb', NULL, 'sedět',  NULL, NULL, 'Imperfective', NULL, 13, 'IJP', 1, NULL),
    (95, 'být',       'být',       1, 'Verb', NULL, 'být',    NULL, NULL, 'Imperfective', NULL, 14, 'IJP', 1, NULL),
    (96, 'mít',       'mít',       1, 'Verb', NULL, 'mít',    NULL, NULL, 'Imperfective', NULL, 15, 'IJP', 1, NULL),
    (97, 'chtít',     'chtít',     1, 'Verb', NULL, 'chtít',  NULL, NULL, 'Imperfective', NULL, 16, 'IJP', 1, NULL),
    (98, 'moci',      'moci',      1, 'Verb', NULL, 'moci',   NULL, NULL, 'Imperfective', NULL, 17, 'IJP', 1,
         'Modální, spisovná infinitivní varianta moci; hovorové moct je stejné lemma jinak zapsané.'),
    (99, 'vědět',     'vědět',     1, 'Verb', NULL, 'vědět',  NULL, NULL, 'Imperfective', NULL, 18, 'IJP', 1, NULL);

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexical units
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lexical_unit (lu_id, lexeme_id, sense_label, gloss) VALUES
    (5,  4,  'general',    'Vykonávat činnost, tvořit něco.'),
    (6,  5,  'purchase',   'Získávat něco za peníze.'),
    (7,  6,  'print',      'Vytvářet tiskem text nebo obraz.'),
    (8,  7,  'write',      'Vytvářet písemný text.'),
    (9,  8,  'read',       'Vnímat a rozumět psanému textu.'),
    (10, 9,  'play',       'Provozovat hru nebo hudbu.'),
    (11, 10, 'speak',      'Vyjadřovat se mluvenou řečí.'),
    (12, 11, 'think',      'Provádět myšlenkovou činnost.'),
    (13, 12, 'reside',     'Mít někde trvalé bydliště.'),
    (14, 13, 'sit',        'Být v sedě.'),
    (15, 14, 'copula',     'Existovat, nebo být v nějakém stavu či vlastnosti.'),
    (16, 15, 'possess',    'Vlastnit nebo disponovat něčím.'),
    (17, 16, 'want',       'Přát si něco, nebo si přát něco udělat.'),
    (18, 17, 'modal',      'Mít schopnost nebo možnost něco udělat.'),
    (19, 18, 'know_fact',  'Mít informaci o něčem.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Frames
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default) VALUES
    (5,  5,  'Verbal', 'Active', 1),
    (6,  6,  'Verbal', 'Active', 1),
    (7,  7,  'Verbal', 'Active', 1),
    (8,  8,  'Verbal', 'Active', 1),
    (9,  9,  'Verbal', 'Active', 1),
    (10, 10, 'Verbal', 'Active', 1),
    (11, 11, 'Verbal', 'Active', 1),
    (12, 12, 'Verbal', 'Active', 1),
    (13, 13, 'Verbal', 'Active', 1),
    (14, 14, 'Verbal', 'Active', 1),
    (15, 15, 'Copular_AdjectivalPred', 'Active', 1),
    (16, 16, 'Verbal', 'Active', 1),
    (17, 17, 'Verbal', 'Active', 1),
    (18, 18, 'Modal',  'Active', 1),
    (19, 19, 'Verbal', 'Active', 1);

-- ─────────────────────────────────────────────────────────────────────────────
-- Slots
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO valency_slot (
    slot_id, frame_id, functor, canonical_order, obligatoriness,
    can_drop_contextual, can_drop_generic, control_target)
VALUES
    -- dělat / udělat
    (11, 5,  'ACT', 1, 'Obligatory', 1, 0, NULL),
    (12, 5,  'PAT', 2, 'Obligatory', 0, 0, NULL),

    -- kupovat / koupit
    (13, 6,  'ACT', 1, 'Obligatory', 1, 0, NULL),
    (14, 6,  'PAT', 2, 'Obligatory', 0, 0, NULL),

    -- tisknout / vytisknout
    (15, 7,  'ACT', 1, 'Obligatory', 1, 0, NULL),
    (16, 7,  'PAT', 2, 'Obligatory', 0, 0, NULL),

    -- psát / napsat — addressee typical, same reasoning dát's ADDR uses
    (17, 8,  'ACT',  1, 'Obligatory', 1, 0, NULL),
    (18, 8,  'PAT',  2, 'Typical',    1, 0, NULL),
    (19, 8,  'ADDR', 3, 'Optional',   1, 0, NULL),

    -- číst / přečíst
    (20, 9,  'ACT', 1, 'Obligatory', 1, 0, NULL),
    (21, 9,  'PAT', 2, 'Typical',    1, 0, NULL),

    -- hrát / zahrát — hraje (intransitive) is as good as hraje fotbal
    (22, 10, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (23, 10, 'PAT', 2, 'Optional',   1, 0, NULL),

    -- mluvit
    (24, 11, 'ACT', 1, 'Obligatory', 1, 0, NULL),

    -- myslet
    (25, 12, 'ACT', 1, 'Obligatory', 1, 0, NULL),

    -- bydlet
    (26, 13, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (27, 13, 'LOC', 2, 'Typical',    0, 0, NULL),

    -- sedět
    (28, 14, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (29, 14, 'LOC', 2, 'Optional',   0, 0, NULL),

    -- být — copular, ACT + adjectival/nominal complement
    (30, 15, 'ACT',   1, 'Obligatory', 1, 0, NULL),
    (31, 15, 'COMPL', 2, 'Obligatory', 0, 0, NULL),

    -- mít
    (32, 16, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (33, 16, 'PAT', 2, 'Obligatory', 0, 0, NULL),

    -- chtít — chce dort (PAT) or chce jít (COMPL infinitive, control on ACT)
    (34, 17, 'ACT',   1, 'Obligatory', 1, 0, NULL),
    (35, 17, 'COMPL', 2, 'Typical',    1, 0, 'ACT'),
    (36, 17, 'PAT',   3, 'Optional',   1, 0, NULL),

    -- moci — modal, controlled infinitive only
    (37, 18, 'ACT',   1, 'Obligatory', 1, 0, NULL),
    (38, 18, 'COMPL', 2, 'Obligatory', 0, 0, 'ACT'),

    -- vědět — ví to (PAT) or ví, že... (clausal)
    (39, 19, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (40, 19, 'PAT', 2, 'Typical',    1, 0, NULL);

-- ─────────────────────────────────────────────────────────────────────────────
-- Realizations
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO slot_realization (
    realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
VALUES
    (11, 11, 'Nominative', NULL, NULL, 0, 1),
    (12, 12, 'Accusative', NULL, NULL, 0, 1),
    (13, 13, 'Nominative', NULL, NULL, 0, 1),
    (14, 14, 'Accusative', NULL, NULL, 0, 1),
    (15, 15, 'Nominative', NULL, NULL, 0, 1),
    (16, 16, 'Accusative', NULL, NULL, 0, 1),
    (17, 17, 'Nominative', NULL, NULL, 0, 1),
    (18, 18, 'Accusative', NULL, NULL, 0, 1),
    (19, 19, 'Dative',     NULL, NULL, 0, 1),
    (20, 20, 'Nominative', NULL, NULL, 0, 1),
    (21, 21, 'Accusative', NULL, NULL, 0, 1),
    (22, 22, 'Nominative', NULL, NULL, 0, 1),
    (23, 23, 'Accusative', NULL, NULL, 0, 1),
    (24, 24, 'Nominative', NULL, NULL, 0, 1),
    (25, 25, 'Nominative', NULL, NULL, 0, 1),
    (26, 26, 'Nominative', NULL, NULL, 0, 1),
    (27, 27, 'Locative',   'v',  NULL, 0, 1),
    (28, 28, 'Nominative', NULL, NULL, 0, 1),
    (29, 29, 'Locative',   'na', NULL, 0, 1),
    (30, 30, 'Nominative', NULL, NULL, 0, 1),
    (31, 31, 'Nominative', NULL, NULL, 0, 1),
    (32, 32, 'Nominative', NULL, NULL, 0, 1),
    (33, 33, 'Accusative', NULL, NULL, 0, 1),
    (34, 34, 'Nominative', NULL, NULL, 0, 1),
    (35, 35, NULL,         NULL, NULL, 1, 1),

    -- Preference řadí realizace uvnitř jednoho slotu, ne sloty proti sobě. Tohle je jediná realizace
    -- slotu PAT, takže musí být 1; s dvojkou by se PAT slovesa chtít nevygeneroval vůbec.
    --
    -- Že chtít tíhne spíš k infinitivu (chci jít) než k předmětu (chci vodu), je vztah mezi slotem 35
    -- a slotem 36 — a ten tímhle sloupcem vyjádřit nejde. Nese ho obligatornost: COMPL je Typical,
    -- PAT je Optional.
    (36, 36, 'Accusative', NULL, NULL, 0, 1),
    (37, 37, 'Nominative', NULL, NULL, 0, 1),
    (38, 38, NULL,         NULL, NULL, 1, 1),
    (39, 39, 'Nominative', NULL, NULL, 0, 1),
    (40, 40, 'Accusative', NULL, NULL, 0, 1),
    (41, 40, NULL,         NULL, 'Declarative', 0, 2);
