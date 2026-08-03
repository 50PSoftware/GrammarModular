-- Grammar.Czech — lexicon seed, update 3.
--
-- Continues seed.sql (1–12 / 1–3 / 1–4 / 1–4 / 1–10 / 1–10) and seed.001.sql
-- (lemma_entry 13–99, lexeme 4–18, lexical_unit 5–19, valency_frame 5–19,
-- valency_slot 11–40, slot_realization 11–41). Append after both, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz)
-- and czechency.org/slovnik, exactly like the two previous files. VALLEX/PDT-Vallex/
-- NomVallex (all CC BY-NC-SA) were consulted ONLY to sanity-check the shape of the
-- valency frames below (which functors a verb takes, obligatoriness, control
-- direction) — never as a source of text or rows to copy. Every `source` value
-- stays 'IJP' because that is where the data actually comes from.
--
-- What's new this round, and why it's worth flagging:
--
--   * Two genuinely DATIVE-governed verbs: pomáhat/pomoci and rozumět. Everything
--     in seed.sql + seed.001.sql so far took Nominative/Accusative (+ psát's
--     Dative ADDR, which is an optional slot, not the verb's core government).
--     pomáhat and rozumět make Dative the *typical* realization of PAT itself —
--     worth having in the fixture set so SlotRealization's case-per-slot design
--     actually gets exercised outside the "postava dala knihu Pavlovi" shape.
--
--   * pomáhat/pomoci's infinitive slot (COMPL) is PAT-controlled, not ACT-controlled.
--     Contrast with chtít (frame 17, seed_001): "chce jít" — the wanter is the one
--     who goes, control_target = 'ACT'. "Pomáhám mu nosit tašky" — the dative mu is
--     the one who carries, control_target = 'PAT'. Same COMPL/takes_infinitive shape,
--     opposite control direction. If ClausePlanner/Microplanner only ever saw
--     ACT-control, this is the row that would have caught the bug.
--
--   * pomoci reuses the named irregular pattern 'moci' via the same prefix
--     mechanism that already gives zahrát from hrát (seed_001): remainder after
--     stripping "po" is "moci", which matches the pattern's own infinitive, so the
--     irregular stem (můžu/moh-/může-) is inherited automatically. No new pattern
--     needed. Unlike moci's own frame (18, Modal — bare controlled infinitive,
--     no lexical object), pomoci/pomáhat get kind='Verbal': it's a full lexical
--     verb that happens to also license an infinitive, exactly like chtít.
--
-- What was deliberately left OUT, and why:
--
--   * ředitel, učitel, cestovatel and other -tel agent nouns — same reason seed_001
--     excluded them: nominative plural is -é (ředitelé), not the plain vzor "muž"
--     -i (lékaři), and NounPattern doesn't yet carry that as a variant. Not a bare
--     lemma_entry row until the pattern data grows a slot for it.
--   * sníh, nůž, oheň, déšť — each has a kmenová vowel alternation on top of the
--     mobile-e/DTN machinery already proven (sníh→sněhu í→ě, nůž→nože ů→o), which
--     is exactly what czech-alternations flags as CzechAlternationRuleEvaluator's
--     job, and that evaluator doesn't exist yet (see PROJECT memory). Same
--     "needs a rule/lexicon decision first" bucket as dům/stůl/rok in seed_001.
--   * sestra, matka, okno-style genitive-plural epenthesis (sester, matek) — okno
--     in seed_001 already accepted this gap once (has_mobile_e=0 even though real
--     gen. pl. is "oken") rather than compound it. zahrada/hodina below were
--     deliberately chosen because their gen. pl. (zahrad, hodin) needs NO inserted
--     vowel — the consonant cluster left after dropping -a is pronounceable as-is —
--     so they're safe to seed now without touching that gap at all.
--   * začít, otevřít, zavřít, najít — each is irregular in a way that is NOT just
--     "prefix + already-named pattern" (najít's present stem jd- is suppletive
--     relative to the infinitive in a way jít's own entry already carries, but
--     najít is not jít's aspectual counterpart — nacházet is — so pairing it via
--     aspect_counterpart would misstate the pair, same reasoning seed_000 already
--     gave for zajít/přijít not being jít's counterpart either). Left for a future
--     irregulars.json pass.
--   * rychlý, hluboký, tichý-type comparatives — comparative suffix choice (-ší
--     vs. -ejší, plus k/ch softening for tichý→tišší) depends on stem-final
--     consonant cluster and isn't uniformly proven across BuildComparativeStem yet;
--     memory only confirms the "n" branch (jemný→jemnější). silný below is chosen
--     *because* it exercises exactly that already-working "n" branch — it is the
--     safe, not the risky, choice. tmavý/slabý/chudý end in a single consonant,
--     the same -ší class as the already-seeded starý/mladý/nový, so those are safe
--     too. rychlý-class words (stem-final consonant cluster, needs -ejší) are
--     deferred until that branch is confirmed.
--
-- Verb pattern choice, for the reviewer:
--   * trida5 (dělat-class): volat/zavolat, pomáhat.
--   * trida4 (mluvit-class): vařit/uvařit, platit/zaplatit, rozumět.
--   * trida3 (kupovat-class, -ovat): pracovat.
--   * Named irregular + prefix reuse: pomoci (po- + moci).

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexemes — one per aspect pair (or per simplex verb with no pair)
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lexeme (lexeme_id, primary_lemma, note) VALUES
    (19, 'volat',    'Vidová dvojice volat / zavolat.'),
    (20, 'vařit',    'Vidová dvojice vařit / uvařit.'),
    (21, 'platit',   'Vidová dvojice platit / zaplatit.'),
    (22, 'pracovat', 'Bez čistého vidového protějšku — zapracovat/odpracovat posouvají význam (zapracovat = zaučit se / vpracovat něco do textu; odpracovat = dokončit směnu), stejná logika jako u mluvit/myslet v seed_001.'),
    (23, 'pomáhat',  'Vidová dvojice pomáhat / pomoci. Pomoci sdílí nepravidelný kmen se slovesem moci (viz seed_001, frame 18) přes prefixový mechanismus jako zahrát z hrát.'),
    (24, 'rozumět',  'Bez čistého vidového protějšku — porozumět je ingresivní perfektivum (pochopit najednou), ne čistý vidový pár, stejně jako uvidět u vidět v seed_000.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    -- pán (masc. anim., tvrdý, gen. -a)
    (100, 'právník',    'právník',    1, 'Noun', 'Masculine', 'pán', 1, 0, NULL, NULL, NULL, 'IJP', 1,
          'Pl. právníci — k→c měkčení v N pl. řeší SofteningRuleEvaluator, ne pattern data.'),
    (101, 'inženýr',    'inženýr',    1, 'Noun', 'Masculine', 'pán', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- muž (masc. anim., měkký, gen. -e) — mobile-e -ec words, stejná skupina jako chlapec/cizinec
    (102, 'sourozenec', 'sourozenec', 1, 'Noun', 'Masculine', 'muž', 1, 1, NULL, NULL, NULL, 'IJP', 1, NULL),
    (103, 'sportovec',  'sportovec',  1, 'Noun', 'Masculine', 'muž', 1, 1, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- hrad (masc. inanim., tvrdý, gen. -u)
    (104, 'dopis',      'dopis',      1, 'Noun', 'Masculine', 'hrad', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (105, 'internet',   'internet',   1, 'Noun', 'Masculine', 'hrad', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- stroj (masc. inanim., měkký, gen. -e)
    (106, 'olej',       'olej',       1, 'Noun', 'Masculine', 'stroj', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- žena (fem., tvrdý, gen. -y) — vybráno záměrně BEZ genitiv-pluralové epenteze (viz hlavička)
    (107, 'zahrada',    'zahrada',    1, 'Noun', 'Feminine', 'žena', 0, 0, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. "zahrad" — vysloviťelná souhlásková skupina, epentetické -e- (jako u sestra→sester) tu netřeba.'),
    (108, 'hodina',     'hodina',     1, 'Noun', 'Feminine', 'žena', 0, 0, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. "hodin" — stejný důvod jako u zahrada.'),

    -- růže (fem., měkký, gen. -e)
    (109, 'židle',      'židle',      1, 'Noun', 'Feminine', 'růže', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- kost (fem., gen. -i)
    (110, 'radost',     'radost',     1, 'Noun', 'Feminine', 'kost', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- město (neut., gen. -a)
    (111, 'jméno',      'jméno',      1, 'Noun', 'Neuter', 'město', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- moře (neut., gen. -e)
    (112, 'pole',       'pole',       1, 'Noun', 'Neuter', 'moře', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- kuře (neut., mláďata, gen. -ete)
    (113, 'zvíře',      'zvíře',      1, 'Noun', 'Neuter', 'kuře', 1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- stavení (neut., gen. -í)
    (114, 'zdraví',     'zdraví',     1, 'Noun', 'Neuter', 'stavení', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- Adjectives — tvrdý (mladý), jen slova s jednou koncovou souhláskou kmene → -ší
    (115, 'silný',      'silný',      1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
          'Komparativ silnější — kmen na -n, prokázaná funkční větev BuildComparativeStem (viz jemný v paměti projektu).'),
    (116, 'slabý',      'slabý',      1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
          'Komparativ slabší — pravidelná -ší třída jako starý→starší.'),
    (117, 'tmavý',      'tmavý',      1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
          'Komparativ tmavší — pravidelná -ší třída.'),
    (118, 'chudý',      'chudý',      1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1,
          'Komparativ chudší — pravidelná -ší třída.'),

    -- Adjectives — měkký (jarní)
    (119, 'noční',      'noční',      1, 'Adjective', NULL, 'jarní', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),
    (120, 'večerní',    'večerní',    1, 'Adjective', NULL, 'jarní', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- Verbs
    (121, 'volat',     'volat',     1, 'Verb', NULL, 'trida5', NULL, NULL, 'Imperfective', 'zavolat',  19, 'IJP', 1, NULL),
    (122, 'zavolat',   'zavolat',   1, 'Verb', NULL, 'trida5', NULL, NULL, 'Perfective',   'volat',    19, 'IJP', 1, NULL),
    (123, 'vařit',     'vařit',     1, 'Verb', NULL, 'trida4', NULL, NULL, 'Imperfective', 'uvařit',   20, 'IJP', 1, NULL),
    (124, 'uvařit',    'uvařit',    1, 'Verb', NULL, 'trida4', NULL, NULL, 'Perfective',   'vařit',    20, 'IJP', 1, NULL),
    (125, 'platit',    'platit',    1, 'Verb', NULL, 'trida4', NULL, NULL, 'Imperfective', 'zaplatit', 21, 'IJP', 1, NULL),
    (126, 'zaplatit',  'zaplatit',  1, 'Verb', NULL, 'trida4', NULL, NULL, 'Perfective',   'platit',   21, 'IJP', 1, NULL),
    (127, 'pracovat',  'pracovat',  1, 'Verb', NULL, 'trida3', NULL, NULL, 'Imperfective', NULL,       22, 'IJP', 1, NULL),
    (128, 'pomáhat',   'pomáhat',   1, 'Verb', NULL, 'trida5', NULL, NULL, 'Imperfective', 'pomoci',   23, 'IJP', 1, NULL),
    (129, 'pomoci',    'pomoci',    1, 'Verb', NULL, 'moci',   NULL, NULL, 'Perfective',   'pomáhat',  23, 'IJP', 1,
          'Kmen zděděný z pojmenovaného nepravidelného vzoru "moci" přes prefixový mechanismus (po- + moci), stejně jako zahrát z hrát.'),
    (130, 'rozumět',   'rozumět',   1, 'Verb', NULL, 'trida4', NULL, NULL, 'Imperfective', NULL,       24, 'IJP', 1, NULL);

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexical units
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lexical_unit (lu_id, lexeme_id, sense_label, gloss) VALUES
    (20, 19, 'call',       'Přivolávat někoho hlasem nebo telefonicky.'),
    (21, 20, 'cook',       'Připravovat pokrm vařením.'),
    (22, 21, 'pay',        'Odevzdávat peníze jako úhradu za něco.'),
    (23, 22, 'work',       'Vykonávat pracovní činnost.'),
    (24, 23, 'help',       'Poskytovat pomoc někomu při nějaké činnosti.'),
    (25, 24, 'understand', 'Chápat význam nebo obsah něčeho, nebo někoho ve smyslu vyjádření.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Frames
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default) VALUES
    (20, 20, 'Verbal', 'Active', 1),
    (21, 21, 'Verbal', 'Active', 1),
    (22, 22, 'Verbal', 'Active', 1),
    (23, 23, 'Verbal', 'Active', 1),
    (24, 24, 'Verbal', 'Active', 1),
    (25, 25, 'Verbal', 'Active', 1);

-- ─────────────────────────────────────────────────────────────────────────────
-- Slots
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO valency_slot (
    slot_id, frame_id, functor, canonical_order, obligatoriness,
    can_drop_contextual, can_drop_generic, control_target)
VALUES
    -- volat / zavolat
    (41, 20, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (42, 20, 'PAT', 2, 'Typical',    1, 0, NULL),

    -- vařit / uvařit
    (43, 21, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (44, 21, 'PAT', 2, 'Typical',    1, 0, NULL),

    -- platit / zaplatit — platit nájem (PAT) [komu] (ADDR, volitelný)
    (45, 22, 'ACT',  1, 'Obligatory', 1, 0, NULL),
    (46, 22, 'PAT',  2, 'Typical',    1, 0, NULL),
    (47, 22, 'ADDR', 3, 'Optional',   1, 0, NULL),

    -- pracovat — pracuje [v/na něčem] (LOC, volitelný)
    (48, 23, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (49, 23, 'LOC', 2, 'Optional',   0, 0, NULL),

    -- pomáhat / pomoci — DATIVNÍ PAT (ne akuzativ!), + volitelný infinitiv
    -- kontrolovaný PAT, ne ACT: "pomáhám mu nosit tašky" — kdo nosí, je mu (PAT),
    -- ne já (ACT). Srovnej s chtít (seed_001, slot 35), kde COMPL kontroluje ACT.
    (50, 24, 'ACT',   1, 'Obligatory', 1, 0, NULL),
    (51, 24, 'PAT',   2, 'Typical',    1, 0, NULL),
    (52, 24, 'COMPL', 3, 'Optional',   1, 0, 'PAT'),

    -- rozumět — DATIVNÍ PAT: rozumět něčemu/někomu, ne *rozumět něco
    (53, 25, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (54, 25, 'PAT', 2, 'Typical',    1, 0, NULL);

-- ─────────────────────────────────────────────────────────────────────────────
-- Realizations
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO slot_realization (
    realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
VALUES
    (42, 41, 'Nominative', NULL, NULL, 0, 1),
    (43, 42, 'Accusative', NULL, NULL, 0, 1),
    (44, 43, 'Nominative', NULL, NULL, 0, 1),
    (45, 44, 'Accusative', NULL, NULL, 0, 1),
    (46, 45, 'Nominative', NULL, NULL, 0, 1),
    (47, 46, 'Accusative', NULL, NULL, 0, 1),
    (48, 47, 'Dative',     NULL, NULL, 0, 1),
    (49, 48, 'Nominative', NULL, NULL, 0, 1),

    -- pracovat v bance (kde) vs. pracovat na projektu (na čem) — dvě reálné
    -- předložkové realizace téhož LOC slotu, preference rozlišuje generovací default.
    (50, 49, 'Locative',   'v',  NULL, 0, 1),
    (51, 49, 'Locative',   'na', NULL, 0, 2),

    (52, 50, 'Nominative', NULL, NULL, 0, 1),
    (53, 51, 'Dative',     NULL, NULL, 0, 1),
    (54, 52, NULL,         NULL, NULL, 1, 1),
    (55, 53, 'Nominative', NULL, NULL, 0, 1),
    (56, 54, 'Dative',     NULL, NULL, 0, 1);
