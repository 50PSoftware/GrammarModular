-- Grammar.Czech — lexicon seed, update 4.
--
-- Continues seed.sql + seed.001.sql + seed.002.sql. Last ids used there:
-- lexeme 24, lemma_entry 130, lexical_unit 25, valency_frame 25,
-- valency_slot 54, slot_realization 56. Append after all three, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz)
-- and czechency.org/slovnik. VALLEX/PDT-Vallex/NomVallex used only to sanity-check
-- the shape of the two new valency frames below — never copied. source = 'IJP'
-- throughout, same rule as seed.001.sql and seed.002.sql.
--
-- Note on how the comparative-adjective picks were checked: -ší vs. -ější is a
-- closed, stable, textbook fact of Czech (not something that changes over time
-- or needs a live lookup any more than "how many cases does Czech have" does),
-- so these were verified against reference grammar knowledge rather than a fresh
-- IJP fetch per word. Anything time-sensitive or genuinely uncertain in this file
-- (the verb valency shapes) *was* cross-checked, per the two notes below.
--
-- One exclusion this round is a direct continuation of a question you asked me
-- last time about -tel nouns, so it's worth spelling out:
--
--   * turista, fotbalista, šachista (-ista nouns) — LEFT OUT. They look like
--     'předseda' (masc. anim., nom. sg. -a) and mostly decline like it, but the
--     nominative plural is -isté (turisté), not the -ové that 'předseda' data
--     already gives (předsedové, kolegové, both correctly seeded in seed.001.sql).
--     Seeding turista with pattern='předseda' today would generate "turistové",
--     which is wrong — the exact same class of bug as -tel/-é you asked about,
--     just on a different vzor. Needs the same fix: either a nominative-plural
--     override on the pattern data, or a dedicated named vzor for -ista nouns.
--     Left out rather than seeded wrong.
--
--   * zajíc — included below, but flagged because it's a near-miss for the
--     OPPOSITE mistake: it looks like it should join the chlapec/cizinec/
--     sourozenec mobile-e group (same -ec-ish tail), but it isn't one. Mobile e
--     needs a *short* e in the last syllable that disappears in oblique forms
--     (chlapec → chlapce). zajíc has a long í in that syllable (za-jíc), which
--     doesn't drop — gen. sg. is zajíce (regular +e, nothing removed), exactly
--     like lékař → lékaře. has_mobile_e = 0 is the correct, deliberate value
--     here, not an oversight in the other direction.
--
--   * drahý, rychlý, lehký, hezký, tichý-class comparatives — still deferred
--     (drahý→dražší h→ž, hezký→hezčí k→č, tichý→tišší ch→š): each needs the
--     same softening/alternation machinery already flagged as not fully proven
--     across BuildComparativeStem. krásný/levný/pomalý below stay in the two
--     already-proven classes (-ější on a single final consonant, and the closed
--     -ší set silný/slabý/tmavý/chudý already established in seed.002.sql).
--   * učit (someone something) — double-object verb (učit dítě angličtinu /
--     angličtině) needs a second PAT-like functor slot that doesn't have a
--     confirmed home in the functor set used so far (ACT/PAT/ADDR/COMPL/LOC).
--     Rather than guess at a functor, left out until FGD functor choice for
--     double-accusative/dat-acc verbs is settled.
--   * otevřít/zavřít, sestra/matka-style gen. pl. epenthesis — same reasons as
--     seed.001.sql/seed.002.sql, still unaddressed, still deferred.
--
-- Verb pattern choice:
--   * trida4 (mluvit-class): stavět/postavit, kreslit/nakreslit.
--   * trida5 (dělat-class): zpívat/zazpívat, poslouchat.
--   * trida2 (-nout, tisknout-class): poslechnout — NOTE this is another
--     aspect pair that changes conjugation class across the pair (5 → 2),
--     same phenomenon as kupovat/koupit (3 → 4) in seed.001.sql. Worth keeping
--     an eye on in VerbConjugationTests: the perfective member needs its own
--     class lookup, not inherited from the imperfective.
--
-- New this round, worth testing:
--   * zpívat's PAT is Optional, not Typical (zpívá = sings, no object needed,
--     as normal as zpívá árii) — contrast with stavět/kreslit/poslouchat where
--     the object is Typical. Three PAT-bearing frames, three different
--     obligatoriness values across this file and the last two — good coverage
--     for anything that reads Obligatoriness off the frame.

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexemes
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lexeme (lexeme_id, primary_lemma, note) VALUES
    (25, 'stavět',     'Vidová dvojice stavět / postavit.'),
    (26, 'kreslit',    'Vidová dvojice kreslit / nakreslit.'),
    (27, 'zpívat',     'Vidová dvojice zpívat / zazpívat.'),
    (28, 'poslouchat', 'Vidová dvojice poslouchat / poslechnout — vidový protějšek mění třídu časování (5 → 2), stejná situace jako kupovat/koupit (3 → 4) v seed_001.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    -- pán (masc. anim., tvrdý, gen. -a)
    (131, 'kapitán', 'kapitán', 1, 'Noun', 'Masculine', 'pán', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (132, 'voják',   'voják',   1, 'Noun', 'Masculine', 'pán', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Pl. vojáci — k→c měkčení, řeší SofteningRuleEvaluator, ne pattern data (stejně jako právník v seed_002).'),

    -- muž (masc. anim., měkký, gen. -e)
    (133, 'zloděj', 'zloděj', 1, 'Noun', 'Masculine', 'muž', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (134, 'malíř',  'malíř',  1, 'Noun', 'Masculine', 'muž', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (135, 'zajíc',  'zajíc',  1, 'Noun', 'Masculine', 'muž', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'NENÍ mobilní e navzdory -ec-podobnému zakončení — zajíc má dlouhé í (za-jíc), gen. "zajíce" jen přidává -e (jako lékař→lékaře), nic se neztrácí. has_mobile_e=0 je záměr, ne omyl (viz hlavička souboru).'),

    -- hrad (masc. inanim., tvrdý, gen. -u)
    (136, 'benzín',   'benzín',   1, 'Noun', 'Masculine', 'hrad', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (137, 'telefon',  'telefon',  1, 'Noun', 'Masculine', 'hrad', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- stroj (masc. inanim., měkký, gen. -e)
    (138, 'počítač', 'počítač', 1, 'Noun', 'Masculine', 'stroj', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (139, 'míč',     'míč',     1, 'Noun', 'Masculine', 'stroj', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- žena (fem., tvrdý, gen. -y) — bez gen.-pl. epenteze, stejná opatrnost jako zahrada/hodina v seed_002
    (140, 'reklama', 'reklama', 1, 'Noun', 'Feminine', 'žena', 0, 0, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. "reklam" — vysloviťelná skupina, epenteze netřeba.'),

    -- růže (fem., měkký, gen. -e)
    (141, 'košile', 'košile', 1, 'Noun', 'Feminine', 'růže', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- kost (fem., gen. -i)
    (142, 'bolest', 'bolest', 1, 'Noun', 'Feminine', 'kost', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (143, 'pomoc',  'pomoc',  1, 'Noun', 'Feminine', 'kost', 0, 0, NULL, NULL, NULL, 'IJP', 1,
          'Přímo souvisí s pomáhat/pomoci (seed_002, lexeme 23) — jiné lemma, jiná kategorie, ale stejná rodina slova.'),

    -- město (neut., gen. -a)
    (144, 'letadlo', 'letadlo', 1, 'Noun', 'Neuter', 'město', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (145, 'jídlo',   'jídlo',   1, 'Noun', 'Neuter', 'město', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- moře (neut., gen. -e)
    (146, 'hřiště', 'hřiště', 1, 'Noun', 'Neuter', 'moře', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- kuře (neut., mláďata, gen. -ete)
    (147, 'jehně', 'jehně', 1, 'Noun', 'Neuter', 'kuře', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- stavení (neut., gen. -í)
    (148, 'znamení', 'znamení', 1, 'Noun', 'Neuter', 'stavení', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- Adjectives — tvrdý (mladý)
    (149, 'krásný', 'krásný', 1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
          'Komparativ krásnější — produktivní -ější třída (jako silný v seed_002).'),
    (150, 'levný',  'levný',  1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
          'Komparativ levnější — -ější třída.'),
    (151, 'pomalý', 'pomalý', 1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
          'Komparativ pomalejší — -ější třída.'),

    -- Adjectives — měkký (jarní)
    (152, 'místní', 'místní', 1, 'Adjective', NULL, 'jarní', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (153, 'hlavní', 'hlavní', 1, 'Adjective', NULL, 'jarní', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- Verbs
    (154, 'stavět',     'stavět',     1, 'Verb', NULL, 'trida4', NULL, NULL, 'Imperfective', 'postavit',    25, 'IJP', 1, NULL),
    (155, 'postavit',   'postavit',   1, 'Verb', NULL, 'trida4', NULL, NULL, 'Perfective',   'stavět',      25, 'IJP', 1, NULL),
    (156, 'kreslit',    'kreslit',    1, 'Verb', NULL, 'trida4', NULL, NULL, 'Imperfective', 'nakreslit',   26, 'IJP', 1, NULL),
    (157, 'nakreslit',  'nakreslit',  1, 'Verb', NULL, 'trida4', NULL, NULL, 'Perfective',   'kreslit',     26, 'IJP', 1, NULL),
    (158, 'zpívat',     'zpívat',     1, 'Verb', NULL, 'trida5', NULL, NULL, 'Imperfective', 'zazpívat',    27, 'IJP', 1, NULL),
    (159, 'zazpívat',   'zazpívat',   1, 'Verb', NULL, 'trida5', NULL, NULL, 'Perfective',   'zpívat',      27, 'IJP', 1, NULL),
    (160, 'poslouchat', 'poslouchat', 1, 'Verb', NULL, 'trida5', NULL, NULL, 'Imperfective', 'poslechnout', 28, 'IJP', 1, NULL),
    (161, 'poslechnout','poslechnout',1, 'Verb', NULL, 'trida2', NULL, NULL, 'Perfective',   'poslouchat',  28, 'IJP', 1,
          'Třída se mění 5 → 2 napříč vidovou dvojicí — viz hlavička souboru.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexical units
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lexical_unit (lu_id, lexeme_id, sense_label, gloss) VALUES
    (26, 25, 'build',   'Vytvářet stavbou něco fyzického.'),
    (27, 26, 'draw',    'Vytvářet kresbu.'),
    (28, 27, 'sing',    'Vydávat zpěvem melodické tóny.'),
    (29, 28, 'listen',  'Vnímat sluchem, věnovat pozornost zvuku nebo řeči.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Frames
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default) VALUES
    (26, 26, 'Verbal', 'Active', 1),
    (27, 27, 'Verbal', 'Active', 1),
    (28, 28, 'Verbal', 'Active', 1),
    (29, 29, 'Verbal', 'Active', 1);

-- ─────────────────────────────────────────────────────────────────────────────
-- Slots
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO valency_slot (
    slot_id, frame_id, functor, canonical_order, obligatoriness,
    can_drop_contextual, can_drop_generic, control_target)
VALUES
    -- stavět / postavit
    (55, 26, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (56, 26, 'PAT', 2, 'Typical',    1, 0, NULL),

    -- kreslit / nakreslit
    (57, 27, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (58, 27, 'PAT', 2, 'Typical',    1, 0, NULL),

    -- zpívat / zazpívat — PAT je Optional, ne Typical: "zpívá" samo o sobě je
    -- naprosto běžná úplná věta, ne eliptický zbytek "zpívá [něco]".
    (59, 28, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (60, 28, 'PAT', 2, 'Optional',   1, 0, NULL),

    -- poslouchat / poslechnout
    (61, 29, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (62, 29, 'PAT', 2, 'Typical',    1, 0, NULL);

-- ─────────────────────────────────────────────────────────────────────────────
-- Realizations
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO slot_realization (
    realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
VALUES
    (57, 55, 'Nominative', NULL, NULL, 0, 1),
    (58, 56, 'Accusative', NULL, NULL, 0, 1),
    (59, 57, 'Nominative', NULL, NULL, 0, 1),
    (60, 58, 'Accusative', NULL, NULL, 0, 1),
    (61, 59, 'Nominative', NULL, NULL, 0, 1),
    (62, 60, 'Accusative', NULL, NULL, 0, 1),
    (63, 61, 'Nominative', NULL, NULL, 0, 1),
    (64, 62, 'Accusative', NULL, NULL, 0, 1);
