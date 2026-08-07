-- Grammar.Czech — lexicon seed, update 5.
--
-- Continues seed.sql, seed.001.sql, seed.002.sql and seed.003.sql. Last ids used there:
-- lexeme 28, lemma_entry 161, lexical_unit 29, valency_frame 29, valency_slot 62,
-- slot_realization 64. Append after all four, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz),
-- exactly like the four previous files. Every `source` value stays 'IJP'.
--
-- What this round is FOR:
--
--   * It closes the exclusion seed.001.sql and seed.002.sql both wrote down. seed_002 put
--     it plainly: "ředitel, učitel, cestovatel and other -tel agent nouns — nominative
--     plural is -é (ředitelé), not the plain vzor muž -i (lékaři), and NounPattern doesn't
--     yet carry that as a variant. Not a bare lemma_entry row until the pattern data grows
--     a slot for it." The pattern data has now grown that slot, so the rows can land.
--
--     The slot is a sub-pattern, not a new field: patterns.json gained "učitel" with
--     "inheritsFrom": "muž" and nothing but the plural Nominative/Vocative "-é" — the same
--     shape "les" has had over "hrad" since the beginning. Everything else about these words
--     is vzor muž and stays inherited (A pl. učitele, L pl. učitelích, I pl. učiteli).
--     Per IJP the -é is predictable from the -tel suffix, but it is the lexicon that decides
--     here on purpose: neživotný "činitel" ('okolnost') is vzor stroj with N pl. činitele,
--     and a blind suffix rule would have broken it.
--
--   * přítel comes along because it is the one -tel word the sub-pattern alone cannot
--     produce. See its note below.
--
--   * Update: this file now carries the FULL -tel deverbative-agent class rather than a
--     five-word sample. Every addition below is a real, IJP-attested agent noun in -tel with
--     a productive N/V pl. -é and no further irregularity beyond what the "učitel" sub-pattern
--     already inherits from muž — no new mobile-e, no new alternation, nothing that would ask
--     anything of CzechAlternationRuleEvaluator, which at the time did nothing (zapojen až
--     v seed_011). That's precisely why this class was safe to grow wide in one pass and -ista/-an/-ové (seed.005.sql,
--     seed.006.sql) were grown separately: each is its own closed decision, not a shared risk.
--
-- The -ista/-ita group seed_003 left out for the same reason is handled in seed.005.sql,
-- which adds the other two sub-patterns of the same shape.
--
-- What is still deliberately left OUT:
--
--   * anděl, manžel — andělé / řidč. andělové, manželé / manželové. Not -tel words, and the
--     doublet is a variant-selection problem (NounPattern still carries one ending per case),
--     which is the gap this round did NOT close.
--   * činitel ('okolnost, faktor') — the inanimate homonym of the agentive -tel noun,
--     vzor stroj (N pl. činitele, no -é). It's the exact counter-example the header above
--     argues from, but it's a different lemma_entry/category pairing than this file's theme,
--     so it stays a comment here rather than a row — nothing about vzor "učitel" needs it
--     seeded to be correct.
--   * sníh, nůž, oheň, déšť and the sestra/matka genitive-plural epenthesis — unchanged from
--     seed_002 and seed_003. Oprava k seed_011: ani jedno nečeká na krácení. Ta čtyři slova
--     jsou dloužení v nom. sg. a jedou přes lemma_entry.stem; epenteze u sestra/matka se
--     vyhodnocuje z has_epenthesis_in_genitive_plural a funguje.

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns, vzor učitel (masc. anim., měkký, gen. -e, N/V pl. -é)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    (162, 'učitel',       'učitel',       1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Vzorové slovo podvzoru. N/V pl. učitelé; 3. a 6. p. sg. mají dubletu učiteli/učitelovi, engine dává primární učiteli.'),
    (163, 'ředitel',      'ředitel',      1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (164, 'spisovatel',   'spisovatel',   1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (165, 'obyvatel',     'obyvatel',     1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (166, 'cestovatel',   'cestovatel',   1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (167, 'přítel',       'přítel',       1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Plurálový kmen přátel- a nulový 2. p. mn. č. "přátel" (relikt staršího skloňování, ne *přátelů) řeší irregulars.json, ne pattern data — stejný dělicí řez jako u právník/SofteningRuleEvaluator v seed_002. Sám podvzor učitel by dal *přítelé/*přítelů.'),

    -- Zbytek třídy — pravidelné deverbativní jméno konatelské (sloveso + -tel), žádná
    -- alternace navíc nad rámec podvzoru. Notu má jen tam, kde stojí za připomenutí,
    -- odkud slovo je nebo proč je bezpečné.
    (168, 'vychovatel',   'vychovatel',   1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (169, 'majitel',      'majitel',      1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (170, 'žadatel',      'žadatel',      1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (171, 'pachatel',     'pachatel',     1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (172, 'pozorovatel',  'pozorovatel',  1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (173, 'zastupitel',   'zastupitel',   1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (174, 'provozovatel', 'provozovatel', 1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (175, 'zaměstnavatel','zaměstnavatel',1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (176, 'dodavatel',    'dodavatel',    1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (177, 'odběratel',    'odběratel',    1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (178, 'nakladatel',   'nakladatel',   1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (179, 'vydavatel',    'vydavatel',    1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (180, 'badatel',      'badatel',      1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (181, 'zpracovatel',  'zpracovatel',  1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (182, 'ošetřovatel',  'ošetřovatel',  1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (183, 'velitel',      'velitel',      1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (184, 'ctitel',       'ctitel',       1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (185, 'tazatel',      'tazatel',      1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (186, 'kazatel',      'kazatel',      1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (187, 'hostitel',     'hostitel',     1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (188, 'nositel',      'nositel',      1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (189, 'ručitel',      'ručitel',      1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (190, 'objevitel',    'objevitel',    1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (191, 'pisatel',      'pisatel',      1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL);
