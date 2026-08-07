-- Grammar.Czech — lexicon seed, update 7.
--
-- Continues seed.sql through seed.005.sql. Last ids used there: lexeme 28,
-- lemma_entry 221, lexical_unit 29, valency_frame 29, valency_slot 62,
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
--         not *dušové. The rules are ordered, and syn's sit ahead of pán's. duch (below) is
--         the actual seeded row this was written about — it isn't just a comment example
--         anymore.
--       - the locative plural gained a g rule beside the existing k and ch ones, because
--         IJP has biolog → o biolozích. No g-stem word had reached that case before: the
--         -log borrowings all take -ové, so they only arrive here through vzor syn.
--
-- Update — why syn got wide and král stayed narrow:
--
--   syn (pán-based) covers three genuinely productive sources at once: kinship/title
--   monosyllables (syn, vnuk, strýc, kmotr, šéf, pán itself), the "duch" type worth naming
--   above, and the whole borrowed -log/-graf profession set, which IJP states as a rule
--   ("jména přejatá zakončená na -log mají koncovku -ové") rather than a per-word fact — so
--   it was safe to add in bulk the same way seed_004's -tel class was.
--
--   král (muž-based) does not have a comparable second source. Every other -e-genitive,
--   soft-stem masculine animate noun that might look like a candidate turned out, on
--   checking, to already belong somewhere else: manžel is the excluded doublet (see below),
--   vůdce/žalobce/soudce are their own -ce vzor, kníže and hrabě are separately irregular
--   titles that don't reduce to muž+ové at all, and posel stacks a mobile-e drop on top of
--   the -ové override in a way nothing in this file's -log words needed and nothing has
--   proven yet. Padding král with a shakier word just to make the two classes look
--   symmetrical would be exactly the "seeded wrong to hit a round number" mistake this
--   project keeps deciding against — so král stays the vzor word alone. Small and correct
--   beats even and guessed.
--
--   Two more control rows were added instead, one per still-live boundary: rytíř (muž, no
--   -ové — plain -i plural even though the noun denotes a title) and zpěvák (pán, k→c
--   softened plural zpěváci, but still -i not -ové — so the softening and the -ové override
--   are shown as genuinely independent, not entangled). kuchař (muž, no -ové) rounds out the
--   count the same way novinář/hasič did in seed_005.
--
-- What is still deliberately left OUT:
--
--   * anděl, manžel, soused, host — andělé/andělové, sousedé/sousedi, hosté/hosti. Genuine
--     doublets rather than a class, and NounPattern still carries one ending per case, so
--     picking one would overstate the data. Unchanged since seed_004.
--   * posel, kníže, hrabě — each would need either a mobile-e drop stacked on the -ové
--     override (posel) or a wholly separate irregular declension (kníže, hrabě) that has
--     nothing to do with vzor král beyond a surface resemblance. Left out rather than forced
--     into a pattern that doesn't actually fit — see the note above.
--   * sníh, nůž, oheň, déšť and the sestra/matka genitive-plural epenthesis — unchanged
--     since seed_002. Oprava k seed_011: ani jedno na krácení nečeká, viz seed.004.sql.

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns, vzor syn (masc. anim., tvrdý, gen. -a, N/V pl. -ové)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    (222, 'syn',       'syn',       1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Vzorové slovo podvzoru. 5. p. sg. je "synu", ne *syne — vlastnost tohohle slova, ne třídy, proto sedí v overrides vzoru (platí jen pro lemma == vzor), stejně jako "pane" u vzoru pán.'),
    (223, 'biolog',    'biolog',    1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'IJP: "Osobní jména přejatá zakončená na -log (vzor pán) mají koncovku -ové." Drží celý velární řetěz — 5. p. sg. biologu, 6. p. mn. č. biolozích (g→z), 1. p. mn. č. biologové bez měkčení.'),
    (224, 'psycholog', 'psycholog', 1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Stejná -log třída jako biolog.'),

    -- Kmenová/titulová jednoslabičná skupina — lexikální, ne odvozená příponou.
    (225, 'pán',   'pán',   1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Vzor samotný jako reálné slovo. N/V pl. má dubletu páni/pánové (formální oslovení "Vážení pánové"); engine dává primární -ové tvar, stejně jako u ostatních doublet v seed_005/006.'),
    (226, 'kmotr',  'kmotr',  1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (227, 'vnuk',   'vnuk',   1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (228, 'strýc',  'strýc',  1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (229, 'šéf',    'šéf',    1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (230, 'duch',   'duch',   1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Slovo z hlavičkové poznámky o řazení pravidel (duchové, ne *dušové) — tady už jako skutečný seedovaný řádek, ne jen komentář.'),

    -- Přejatá -log/-graf skupina — pravidlová (IJP), ne slovo od slova.
    (231, 'geolog',      'geolog',      1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (232, 'filolog',     'filolog',     1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (233, 'fotograf',    'fotograf',    1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (234, 'geograf',     'geograf',     1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (235, 'choreograf',  'choreograf',  1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (236, 'pedagog',     'pedagog',     1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (237, 'archeolog',   'archeolog',   1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (238, 'kardiolog',   'kardiolog',   1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (239, 'neurolog',    'neurolog',    1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (240, 'meteorolog',  'meteorolog',  1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (241, 'sociolog',    'sociolog',    1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (242, 'etnograf',    'etnograf',    1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (243, 'kartograf',   'kartograf',   1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (244, 'technolog',   'technolog',   1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (245, 'zoolog',      'zoolog',      1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (246, 'virolog',     'virolog',     1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (247, 'toxikolog',   'toxikolog',   1, 'Noun', 'Masculine', 'syn', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns, vzor král (masc. anim., měkký, gen. -e, N/V pl. -ové)
-- ─────────────────────────────────────────────────────────────────────────────
    (248, 'král',      'král',      1, 'Noun', 'Masculine', 'král', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Vzorové slovo podvzoru. seed_001 ho vynechal kvůli "krále, ne *krála" — to ale žádnou odchylku nepotřebuje, je to prostý vzor muž. Odchylka je jen 1. a 5. p. mn. č.: králové. Podvzor zůstává úmyslně jednoslovný — viz poznámka v hlavičce, proč přidávání dalších kandidátů (manžel, posel, kníže) bylo vždy z jiného, nekompatibilního důvodu.'),

-- ─────────────────────────────────────────────────────────────────────────────
-- Kontrolní řádky — vzor bez podvzoru
-- ─────────────────────────────────────────────────────────────────────────────
    (249, 'rytíř',  'rytíř',  1, 'Noun', 'Masculine', 'muž', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Titul, ale NE -ové: N pl. rytíři, čistý vzor muž. Ukazuje, že -ové není odvoditelné ze sémantiky "titul/postavení", jen z konkrétní lexikální třídy.'),
    (250, 'zpěvák', 'zpěvák', 1, 'Noun', 'Masculine', 'pán', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'N pl. zpěváci (k→c měkčení přes SofteningRuleEvaluator), ne *zpěvákové — měkčení a -ové override jsou na sobě nezávislé, tenhle řádek to dokazuje.'),
    (251, 'kuchař', 'kuchař', 1, 'Noun', 'Masculine', 'muž', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Třetí kontrolní řádek, stejný důvod jako novinář/hasič v seed_005 a rytíř výše: N pl. kuchaři, čistý vzor muž.');
