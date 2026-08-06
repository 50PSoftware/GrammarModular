-- Grammar.Czech — lexicon seed, update 11.
--
-- Continues seed.000.sql through seed.009.sql. Last ids used there: lexeme 30,
-- lemma_entry 255, lexical_unit 32, valency_frame 33, valency_slot 72,
-- slot_realization 77. Append after all ten, in order.
--
-- Pozor na realizace: 77 leží v seed.001.sql, ne tady. Jmenný přísudek dostal instrumentál
-- dodatečně a uvnitř toho souboru už volné číslo nebylo. Další volné číslo se proto zjišťuje
-- dotazem na max(realization_id), ne přičtením k tomu, co říká hlavička posledního seedu.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz) and the
-- tectogrammatical annotation manual of the Prague Dependency Treebank, §7.2.1. Every `source`
-- value stays 'IJP'.
--
-- What this round is FOR, část první: rozdělit sponu. Sloveso být neslo jeden význam pojmenovaný „copula“,
-- jehož popis zněl „Existovat, nebo být v nějakém stavu či vlastnosti“ — tři konstrukce v jedné
-- řádce. Rámec u nich přitom nemůže být týž, protože každá má jiný druh predikátu a ValencyKind
-- je má rozlišené:
--
--   * jmenný přísudek — Petr je učitel, lev je králem zvířat. PAT je jméno v 1. nebo 7. pádě.
--     Zůstal na původním významu, jen přejmenovaném na copula_nominal, protože instrumentál,
--     který k němu patří, tam už byl zapsaný.
--   * adjektivní přísudek — Petr je veselý. PAT je adjektivum, a to instrumentál nemá; *je veselým
--     je knižní až nepřijatelné, takže realizace je jen jedna.
--   * existence — Pět studentů bylo. Je tam problém. Žádný PAT není: co existuje, je ACT.
--
-- UNIQUE (lu_id, diathesis) drží jeden rámec na význam a diatezi, takže tři druhy predikátu
-- znamenají tři významy, ne tři rámce pod jedním.
--
-- Žádný z nich není výchozí. GetFrame u slovesa s víc rámci úmyslně hází výjimku místo hádání —
-- viz jeho vlastní komentář o jít/motion proti jít/process — takže volající musí jmenovat, kterou
-- sponu chce. Původní frame 15 proto v seed.001.sql přišel o is_default.
--
-- What this round is FOR, část druhá: doplnit slovesu mluvit aktanty, které mu chyběly. Rámec
-- licencoval jen ACT, takže „mluvit s někým“ i „mluvit o něčem“ končily hláškou, že sloveso nemá
-- slot pro funktor ADDR. VALLEX má mluvit jako ACT(1) ADDR(s+7) PAT(o+6) a tohle to dorovnává.

-- ─────────────────────────────────────────────────────────────────────────────
-- Významy
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lexical_unit (lu_id, lexeme_id, sense_label, gloss)
VALUES
    (33, 14, 'copula_adjectival', 'Být nějaký — adjektivní přísudek.'),
    (34, 14, 'existence',         'Existovat, být přítomen.');

-- ─────────────────────────────────────────────────────────────────────────────
-- Rámce
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO valency_frame (frame_id, lu_id, kind, diathesis, is_default)
VALUES
    (34, 33, 'Copular_AdjectivalPred', 'Active', 0),
    (35, 34, 'Existential',            'Active', 0);

-- ─────────────────────────────────────────────────────────────────────────────
-- Sloty
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO valency_slot (
    slot_id, frame_id, functor, canonical_order, obligatoriness,
    can_drop_contextual, can_drop_generic, control_target)
VALUES
    -- Adjektivní přísudek
    (73, 34, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (74, 34, 'PAT', 2, 'Obligatory', 0, 0, NULL),

    -- Existence. Místo je u existenční věty typické, ne povinné: „problém je“ je úplná věta,
    -- „je tam problém“ je běžnější. Obligatorní není, jinak by se bez něj nedalo nic vygenerovat.
    (75, 35, 'ACT', 1, 'Obligatory', 1, 0, NULL),
    (76, 35, 'LOC', 2, 'Typical',    1, 0, NULL);

-- ─────────────────────────────────────────────────────────────────────────────
-- Realizace
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO slot_realization (
    realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
VALUES
    (78, 73, 'Nominative', NULL, NULL, 0, 1),
    (79, 74, 'Nominative', NULL, NULL, 0, 1),
    (80, 75, 'Nominative', NULL, NULL, 0, 1),
    (81, 76, 'Locative',   'v',  NULL, 0, 1),
    (82, 76, 'Locative',   'na', NULL, 0, 2);

-- ─────────────────────────────────────────────────────────────────────────────
-- mluvit — chybějící aktanty
-- ─────────────────────────────────────────────────────────────────────────────
--
-- Frame 11 je „mluvit“ ve významu speak. Identifikátory se píšou natvrdo, i když by je SQLite
-- u INTEGER primárního klíče přidělila sama: na SQLite je zadává pisatel, seed je má vyslovit
-- a pull přenáší serverová beze změny. Dopočítané by se rozešly s tím, co má admin.
INSERT INTO valency_slot (
    slot_id, frame_id, functor, canonical_order, obligatoriness,
    can_drop_contextual, can_drop_generic, control_target)
VALUES
    -- Pořadí ADDR před PAT je pořadí ve větě: mluvil s Janou o práci.
    (77, 11, 'ADDR', 2, 'Optional', 1, 1, NULL),
    (78, 11, 'PAT',  3, 'Optional', 1, 1, NULL);

INSERT INTO slot_realization (
    realization_id, slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
VALUES
    -- ADDR: běžně „s Janou“. Řečnické „k lidem“ je varianta, kterou stačí přijímat — s dvojkou
    -- se rozpozná, ale negeneruje.
    (83, 77, 'Instrumental', 's', NULL, 0, 1),
    (84, 77, 'Dative',       'k', NULL, 0, 2),

    -- PAT: o čem se mluví, tedy obsah řeči.
    (85, 78, 'Locative',     'o', NULL, 0, 1);
