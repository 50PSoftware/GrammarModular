-- Grammar.Czech — lexicon seed, update 6.
--
-- Continues seed.sql through seed.004.sql. Last ids used there: lexeme 28,
-- lemma_entry 167, lexical_unit 29, valency_frame 29, valency_slot 62,
-- slot_realization 64. Append after all five, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz),
-- exactly like the five previous files. Every `source` value stays 'IJP'.
--
-- What this round is FOR:
--
--   * It finishes the -é nominative plural. seed_003 named the missing half itself:
--     "turista, fotbalista, šachista (-ista nouns) — LEFT OUT. They look like 'předseda'
--     … but the nominative plural is -isté (turisté), not the -ové that 'předseda' data
--     already gives … Needs the same fix: either a nominative-plural override on the
--     pattern data, or a dedicated named vzor for -ista nouns." It is the second of those,
--     and the -an group (občan → občané) hanging off vzor pán gets the same treatment.
--
--     patterns.json now has "občan" (inheritsFrom pán) and "turista" (inheritsFrom
--     předseda), each overriding nothing but the plural Nominative/Vocative, exactly like
--     "učitel" over "muž" in seed_004. That completes the matrix: three masculine animate
--     base patterns, and for each the -é variant its suffix class takes.
--
--   * -ita gets no sub-pattern of its own. husita declines exactly like turista (IJP:
--     husity, husitovi, husitu, husito … husité), so it is a member of "turista", not a
--     sibling of it — the same way ředitel is a member of "učitel".
--
-- On the doublets, since every word here has one:
--
--   IJP lists občané/občani, křesťané/křesťani, turisté/turisti, husité/husiti. The -é form
--   is the one IJP gives first and the one that is neutral in Bohemia; -i is the short
--   Moravian variant. NounPattern still carries one ending per case, so the engine produces
--   the primary and the variant is simply not expressible yet — the same limitation
--   seed_004 noted for anděl/manžel. Nothing here depends on that being fixed; it just means
--   these rows are a choice of primary, not a claim that -i is wrong.
--
-- What is still deliberately left OUT:
--
--   * The -ové class — syn → synové, král → králové, biolog → biologové. It is NOT a suffix
--     class (monosyllables, titles and kinship terms, i.e. lexical), and a pán-based
--     sub-pattern would silently lose the velar vocative singular, because the softening
--     rules match on the literal pattern name: biolog would come out *biologe instead of
--     biologu. That needs CzechSofteningRuleEvaluator to walk inheritsFrom, which is a code
--     change, not a data one. Left alone rather than half-done — same bucket as seed_001's
--     syn and král.
--   * sníh, nůž, oheň, déšť and the sestra/matka genitive-plural epenthesis — unchanged
--     since seed_002, still waiting on CzechAlternationRuleEvaluator.

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns, vzor občan (masc. anim., tvrdý, gen. -a, N/V pl. -é)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    (168, 'občan',    'občan',    1, 'Noun', 'Masculine', 'občan', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Vzorové slovo podvzoru. N/V pl. občané; IJP uvádí i krátké občani, engine dává primární tvar.'),
    (169, 'křesťan',  'křesťan',  1, 'Noun', 'Masculine', 'občan', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (170, 'měšťan',   'měšťan',   1, 'Noun', 'Masculine', 'občan', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (171, 'Moravan',  'moravan',  1, 'Noun', 'Masculine', 'občan', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Obyvatelské jméno — velké písmeno je součástí lemmatu, lemma_key je jako všude jinde malými.'),
    (172, 'Pražan',   'pražan',   1, 'Noun', 'Masculine', 'občan', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns, vzor turista (masc. anim., kmen na -a, N/V pl. -é)
-- ─────────────────────────────────────────────────────────────────────────────
    (173, 'turista',   'turista',   1, 'Noun', 'Masculine', 'turista', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Vzorové slovo podvzoru. N/V pl. turisté; IJP uvádí i krátké turisti, engine dává primární tvar.'),
    (174, 'houslista', 'houslista', 1, 'Noun', 'Masculine', 'turista', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (175, 'husita',    'husita',    1, 'Noun', 'Masculine', 'turista', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          '-ita má stejné paradigma jako -ista (husity, husitovi, husitu, husito … husité), takže je členem podvzoru turista, ne vlastním podvzorem.'),

-- ─────────────────────────────────────────────────────────────────────────────
-- Kontrolní řádek — vzor bez podvzoru
-- ─────────────────────────────────────────────────────────────────────────────
    (176, 'novinář',  'novinář',  1, 'Noun', 'Masculine', 'muž', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Sem podvzor NEpatří: -ář není -tel, N pl. je novináři jako u lékaře. Řádek je tu proto, aby se v lexikonu drželo i to, kam se -é nerozlévá.');
