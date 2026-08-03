-- Grammar.Czech — lexicon seed, update 7.
--
-- Continues seed.sql through seed.005.sql. Last ids used there: lexeme 28,
-- lemma_entry 176, lexical_unit 29, valency_frame 29, valency_slot 62,
-- slot_realization 64. Append after all six, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz),
-- exactly like the six previous files. Every `source` value stays 'IJP'.
--
-- What this round is FOR:
--
--   * The -ové nominative plural, which seed_005 left out and seed_001 deferred before it
--     ("syn→synové", "král→krále not krála"). Two more sub-patterns of the same shape:
--     "syn" over pán and "král" over muž, each overriding only the plural Nominative and
--     Vocative. With these the masculine animate plural is closed — every base vzor now has
--     the -é and the -ové variant its members take, and nothing is left in the "seeded wrong
--     or not at all" bucket for this reason.
--
--   * Unlike -tel/-an/-ista, -ové is NOT a suffix class. It is monosyllables, titles and
--     kinship terms, plus the borrowed -log/-graf professions — lexical, so the lexicon is
--     the only thing that can decide it, and every word below is a row rather than a rule.
--
-- The code change this needed, and why it was not needed before:
--
--   * CzechSofteningRuleEvaluator matched rules on the literal pattern name, so a sub-pattern
--     inherited endings but no rules. That was harmless for -é (občan and učitel stems end in
--     -n and -l, which none of the pán/muž rules touch) and fatal for -ové, whose members are
--     mostly velar-stemmed: biolog would have come out *biologe in the vocative singular.
--     GetMatchingRule now walks inheritsFrom, so pán's rules reach syn and občan alike.
--
--   * Two consequences worth knowing about, both covered by NounDeclensionTests:
--       - vzor syn switches the nominative/vocative plural palatalization back off. pán
--         palatalizes there because its ending is -i; -ové does not, so duch is duchové and
--         not *dušové. The rules are ordered, and syn's sit ahead of pán's.
--       - the locative plural gained a g rule beside the existing k and ch ones, because
--         IJP has biolog → o biolozích. No g-stem word had reached that case before: the
--         -log borrowings all take -ové, so they only arrive here through vzor syn.
--
-- What is still deliberately left OUT:
--
--   * anděl, manžel, soused, host — andělé/andělové, sousedé/sousedi, hosté/hosti. Genuine
--     doublets rather than a class, and NounPattern still carries one ending per case, so
--     picking one would overstate the data. Unchanged since seed_004.
--   * sníh, nůž, oheň, déšť and the sestra/matka genitive-plural epenthesis — unchanged
--     since seed_002, still waiting on CzechAlternationRuleEvaluator.

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns, vzor syn (masc. anim., tvrdý, gen. -a, N/V pl. -ové)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    (177, 'syn',       'syn',       1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Vzorové slovo podvzoru. 5. p. sg. je "synu", ne *syne — vlastnost tohohle slova, ne třídy, proto sedí v overrides vzoru (platí jen pro lemma == vzor), stejně jako "pane" u vzoru pán.'),
    (178, 'biolog',    'biolog',    1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'IJP: "Osobní jména přejatá zakončená na -log (vzor pán) mají koncovku -ové." Drží celý velární řetěz — 5. p. sg. biologu, 6. p. mn. č. biolozích (g→z), 1. p. mn. č. biologové bez měkčení.'),
    (179, 'psycholog', 'psycholog', 1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Stejná -log třída jako biolog.'),

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns, vzor král (masc. anim., měkký, gen. -e, N/V pl. -ové)
-- ─────────────────────────────────────────────────────────────────────────────
    (180, 'král',      'král',      1, 'Noun', 'Masculine', 'král', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Vzorové slovo podvzoru. seed_001 ho vynechal kvůli "krále, ne *krála" — to ale žádnou odchylku nepotřebuje, je to prostý vzor muž. Odchylka je jen 1. a 5. p. mn. č.: králové.');
