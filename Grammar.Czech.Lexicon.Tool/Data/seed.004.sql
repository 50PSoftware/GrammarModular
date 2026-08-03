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
-- The -ista/-ita group seed_003 left out for the same reason is handled in seed.005.sql,
-- which adds the other two sub-patterns of the same shape.
--
-- What is still deliberately left OUT:
--
--   * anděl, manžel — andělé / řidč. andělové, manželé / manželové. Not -tel words, and the
--     doublet is a variant-selection problem (NounPattern still carries one ending per case),
--     which is the gap this round did NOT close.
--   * sníh, nůž, oheň, déšť and the sestra/matka genitive-plural epenthesis — unchanged from
--     seed_002 and seed_003, still waiting on CzechAlternationRuleEvaluator.

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns, vzor učitel (masc. anim., měkký, gen. -e, N/V pl. -é)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    (162, 'učitel',     'učitel',     1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Vzorové slovo podvzoru. N/V pl. učitelé; 3. a 6. p. sg. mají dubletu učiteli/učitelovi, engine dává primární učiteli.'),
    (163, 'ředitel',    'ředitel',    1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (164, 'spisovatel', 'spisovatel', 1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (165, 'obyvatel',   'obyvatel',   1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (166, 'cestovatel', 'cestovatel', 1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (167, 'přítel',     'přítel',     1, 'Noun', 'Masculine', 'učitel', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Plurálový kmen přátel- a nulový 2. p. mn. č. "přátel" (relikt staršího skloňování, ne *přátelů) řeší irregulars.json, ne pattern data — stejný dělicí řez jako u právník/SofteningRuleEvaluator v seed_002. Sám podvzor učitel by dal *přítelé/*přítelů.');
