-- Grammar.Czech — lexicon seed, update 15.
--
-- Continues seed.000.sql through seed.013.sql. Last ids used there: lexeme 30,
-- lemma_entry 267, lexical_unit 34, valency_frame 35, valency_slot 78,
-- slot_realization 85, construction 3. Append after all fourteen, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz) for the
-- morphology; the analysis follows Nový encyklopedický slovník češtiny, heslo PODMĚT. Every `source`
-- value stays 'IJP'.
--
-- What this round is FOR: the weather verbs, and with them the first frames of kind 'Impersonal'.
--
-- WHY THEY NEED A FRAME AT ALL. Until now a verb the dictionary did not hold simply took whatever the
-- caller supplied, so `pršet` accepted a participant and produced *Prší student. A frame is what says
-- there is nothing to fill: the sentence planner refuses an inner participant the frame has no slot
-- for, and these frames have no slots.
--
-- THE POINT THE SOURCES DISAGREE ON, and how these rows sit with it. NESČ (heslo PODMĚT) puts the two
-- readings side by side: the traditional one calls these věty jednočlenné / bezpodměté — sentences
-- with no subject — while "v generativních mluvnicích mají v jejich mainstreamové linii všechny věty
-- podmět", with an expletive nobody pronounces: (Ono) prší. The entry leaves it open, and so does
-- this file: an empty frame is not a claim that no subject position exists, only that no word the
-- caller supplies can occupy it. That much both readings agree on, and it is the whole of what the
-- generator needs. The expletive, if there is one, is never a word anybody passes in.
--
-- WHAT IS DELIBERATELY LEFT OUT:
--
--   * `mrznout` and `hřmít`. Both have a reading with a subject — voda mrzne, hrom hřmí — so they are
--     two senses and not one avalent verb. Writing them as impersonal would make the subject reading
--     unreachable, which is worse than leaving them out.
--   * `stmívat se`, `blýskat se`. Avalent as well, but reflexive, and the reflexive of an impersonal
--     verb wants a look at whether ReflexiveType on the frame does the right thing there. Not guessed.
--   * Free modifications — Prší od rána, Venku sněží. They attach to any verb and are never licensed
--     by a frame, so an empty frame does not stand in their way and no slot is needed for them.
--   * The perfective counterparts. `pršet` and `sněžit` have none worth the row; `svítat` pairs with
--     `rozednít se`, which is reflexive and waits with the others.

-- Lexém první: heslo na něj ukazuje cizím klíčem.
INSERT INTO lexeme (lexeme_id, primary_lemma, note) VALUES
    (31, 'pršet',  'Bez vidového protějšku, který by stál za řádek.'),
    (32, 'sněžit', 'Bez vidového protějšku, který by stál za řádek.'),
    (33, 'svítat', 'Protějšek rozednít se je zvratný a čeká s ostatními.');

INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, has_genitive_plural_shortening,
    aspect, aspect_counterpart, verb_class, lexeme_id, source, is_verified, note)
VALUES
    (268, 'pršet',  'pršet',  1, 'Verb', NULL, 'trida4', NULL, NULL, NULL,
          'Imperfective', NULL, 'Class4', 31, 'IJP', 1, 'Bezpodměťové: prší.'),
    (269, 'sněžit', 'sněžit', 1, 'Verb', NULL, 'trida4', NULL, NULL, NULL,
          'Imperfective', NULL, 'Class4', 32, 'IJP', 1, 'Bezpodměťové: sněží.'),
    (270, 'svítat', 'svítat', 1, 'Verb', NULL, 'trida5', NULL, NULL, NULL,
          'Imperfective', NULL, 'Class5', 33, 'IJP', 1, 'Bezpodměťové: svítá.');

INSERT INTO lexical_unit (lu_id, lexeme_id, sense_label, gloss) VALUES
    (35, 31, 'weather', 'Padá déšť.'),
    (36, 32, 'weather', 'Padá sníh.'),
    (37, 33, 'weather', 'Začíná být světlo.');

-- Žádné sloty. To je celý obsah těch rámců a zároveň jejich smysl.
INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default) VALUES
    (36, 35, 'Impersonal', 'Active', 1),
    (37, 36, 'Impersonal', 'Active', 1),
    (38, 37, 'Impersonal', 'Active', 1);
