-- Grammar.Czech — lexicon seed.
--
-- Everything the retired Data/Lexicon/lexicon.json and Data/Lexicon/valency.json held, carried over
-- unchanged except where the new schema says something the old one could not. Written as portable
-- INSERTs with explicit primary keys, which is also the format the dump command emits, so that a
-- future review workflow can diff the dictionary as text.
--
-- Two things are stated here that the JSON left implicit:
--
--   * dát and dávat share lexeme 1 and therefore share one frame. The JSON repeated the frame under
--     both lemmas and the two copies had drifted — dát carried a directional slot, dávat did not —
--     which is exactly the divergence a shared row removes. The union is right for both: dává knihu
--     na stůl is as good Czech as dal knihu na stůl.
--   * The addressee of dát is Typical rather than optional. It is part of the meaning of the verb and
--     is understood when unsaid (dal to), which is what the old boolean could not distinguish from a
--     directional that simply is not part of the event.
--
-- Provenance: every row is hand-written from Internetová jazyková příručka and standard reference
-- grammars. Nothing is derived from VALLEX, PDT-Vallex or NomVallex, which are CC BY-NC-SA and
-- therefore cannot be redistributed by this package.

INSERT INTO lexicon_meta (meta_key, meta_value) VALUES ('schema_version', '9');
INSERT INTO lexicon_meta (meta_key, meta_value) VALUES ('license', 'Same as the package.');
INSERT INTO lexicon_meta (meta_key, meta_value) VALUES ('source', 'Hand-authored from IJP; no CC BY-NC-SA corpus material.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexemes
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lexeme (lexeme_id, primary_lemma, note) VALUES
    (1, 'dávat', 'Vidová dvojice dát / dávat.'),
    (2, 'jít', 'Bez vidového protějšku.'),
    (3, 'vidět', 'Vidová dvojice vidět / uvidět.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    (1,  'student',   'student',   1, 'Noun', 'Masculine', 'pán',   1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (2,  'studentka', 'studentka', 1, 'Noun', 'Feminine',  'žena',  1, 0, NULL, NULL, NULL, 'IJP', 1, NULL),
    (3,  'pes',       'pes',       1, 'Noun', 'Masculine', 'pán',   1, 1, NULL, NULL, NULL, 'IJP', 1, NULL),
    (4,  'den',       'den',       1, 'Noun', 'Masculine', 'hrad',  0, 1, NULL, NULL, NULL, 'IJP', 1, NULL),
    (5,  'otec',      'otec',      1, 'Noun', 'Masculine', 'muž',   1, 1, NULL, NULL, NULL, 'IJP', 1, NULL),
    (6,  'město',     'město',     1, 'Noun', 'Neuter',    'město', 0, 0, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- Vzor je 'dát', ne trida5. Obecná třída odvozuje minulý kmen z infinitivu, takže dát dostalo
    -- 'dá' a příčestí vyšlo jako *dál. Krácení á→a je u těchhle sloves lexikální — dát/dal, ale
    -- hrát/hrál — a irregulars.json ho pro dát říká. Dávat obecnou třídou projde, dával je správně.
    (7,  'dát',       'dát',       1, 'Verb', NULL, 'dát',    NULL, NULL, 'Perfective',   'dávat', 1, 'IJP', 1, NULL),
    (8,  'dávat',     'dávat',     1, 'Verb', NULL, 'trida5', NULL, NULL, 'Imperfective', 'dát',   1, 'IJP', 1, NULL),

    -- No counterpart, and the NULL is the claim rather than a gap. Verbs of motion perfectivize only by
    -- prefixation and every prefix adds meaning of its own — zajít is to drop by, přijít to arrive,
    -- odejít to leave. None of them is jít with nothing changed but the aspect, so none of them is the
    -- counterpart. (chodit is not one either: that is the indeterminate member, imperfective as well.)
    (9,  'jít',       'jít',       1, 'Verb', NULL, 'jít', NULL, NULL, 'Imperfective', NULL, 2, 'IJP', 1,
         'Imperfektivum bez vidového protějšku — prefixace u sloves pohybu mění význam, netvoří vid.'),

    (10, 'vidět',     'vidět',     1, 'Verb', NULL, 'trida4', NULL, NULL, 'Imperfective', 'uvidět', 3, 'IJP', 1, NULL),

    (11, 'mladý',     'mladý',     1, 'Adjective', NULL, 'mladý', NULL, NULL, NULL, NULL, NULL, 'IJP', 1, NULL),

    -- The other half of the vidět pair, added so the reference resolves in both directions. Standard
    -- lexicography pairs the two and the note records the reservation rather than hiding it: uvidět is
    -- read ingressively, catching sight of something rather than seeing it through, so the pair is not
    -- as pure as dát / dávat.
    (12, 'uvidět',    'uvidět',    1, 'Verb', NULL, 'trida4', NULL, NULL, 'Perfective', 'vidět', 3, 'IJP', 1,
         'Ingresivní perfektivum (spatřit), nikoli čistě vidový protějšek.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Lexical units — the sense labels the old frameLabel named
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lexical_unit (lu_id, lexeme_id, sense_label, gloss) VALUES
    (1, 1, 'transfer',   'Předat něco někomu.'),
    (2, 2, 'motion',     'Pohybovat se pěšky odněkud někam.'),
    (3, 2, 'process',    'Probíhat, dařit se — jde to.'),
    (4, 3, 'perception', 'Vnímat zrakem.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Frames
-- ─────────────────────────────────────────────────────────────────────────────
-- jít has no default frame on purpose: motion and process take different arguments, so a caller that
-- names neither is genuinely ambiguous and CzechValencyService says so rather than guessing.
INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default) VALUES
    (1, 1, 'Verbal', 'Active', 1),
    (2, 2, 'Verbal', 'Active', 0),
    (3, 3, 'Verbal', 'Active', 0),
    (4, 4, 'Verbal', 'Active', 1);

-- ─────────────────────────────────────────────────────────────────────────────
-- Slots
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO valency_slot (
    slot_id, frame_id, functor, canonical_order, obligatoriness,
    can_drop_contextual, can_drop_generic, control_target)
VALUES
    -- dát / dávat, transfer
    (1, 1, 'ACT',  1, 'Obligatory', 1, 0, NULL),
    (2, 1, 'PAT',  2, 'Obligatory', 0, 0, NULL),
    (3, 1, 'ADDR', 3, 'Typical',    1, 0, NULL),
    (4, 1, 'DIR3', 4, 'Optional',   1, 0, NULL),

    -- jít, motion
    (5, 2, 'ACT',  1, 'Obligatory', 1, 0, NULL),
    (6, 2, 'DIR3', 2, 'Optional',   1, 0, NULL),
    (7, 2, 'DIR1', 3, 'Optional',   1, 0, NULL),

    -- jít, process
    (8, 3, 'ACT',  1, 'Obligatory', 1, 0, NULL),

    -- vidět, perception
    (9,  4, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (10, 4, 'PAT', 2, 'Obligatory', 0, 0, NULL);

-- ─────────────────────────────────────────────────────────────────────────────
-- Realizations
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO slot_realization (
    realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
VALUES
    (1,  1,  'Nominative', NULL, NULL, 0, 1),
    (2,  2,  'Accusative', NULL, NULL, 0, 1),
    (3,  3,  'Dative',     NULL, NULL, 0, 1),
    (4,  4,  'Accusative', 'na', NULL, 0, 1),
    (5,  5,  'Nominative', NULL, NULL, 0, 1),
    (6,  6,  'Genitive',   'do', NULL, 0, 1),
    (7,  7,  'Genitive',   'z',  NULL, 0, 1),
    (8,  8,  'Nominative', NULL, NULL, 0, 1),
    (9,  9,  'Nominative', NULL, NULL, 0, 1),
    (10, 10, 'Accusative', NULL, NULL, 0, 1);
