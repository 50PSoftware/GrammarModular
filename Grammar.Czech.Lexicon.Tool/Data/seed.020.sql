-- Grammar.Czech — lexicon seed, update 21.
--
-- Continues seed.000.sql through seed.019.sql. Last ids used there: lexeme 37,
-- lemma_entry 281, lexical_unit 44, valency_frame 45, valency_slot 82,
-- slot_realization 89, construction 3; lemma_variant and lemma_sense are new here and start at 1.
-- Append after all twenty, in order.
--
-- Provenance: Internetová jazyková příručka (prirucka.ujc.cas.cz) for the headword and its doublet;
-- Nový encyklopedický slovník češtiny, heslo ZPŮSOB SLOVESNÉHO DĚJE, for the classification.
--
-- This file is what schema 6 was opened for. Both entries below are things seed.019.sql wrote down as
-- impossible and left undone, and neither was a matter of judgement — both were a missing table.
--
-- 1. mrznout AND zmrznout GET THEIR GROUPS, one per reading.
--
--    seed.019.sql left mrznout unclassified because its two senses belong to different groups and
--    aktionsart sat only on lemma_entry, and said the fix was to move the column to lexical_unit. That
--    would not have worked, and the validator is what said so: a lexical unit hangs off the lexeme, a
--    lexeme is an aspect pair, so a value written at the sense lands on the perfective too. Set
--    lu 41 to Stative and zmrznout claims to be stative — it is Perfective, and no stative verb is.
--
--    Hence lemma_sense, which pairs one heslo with one sense. The four readings:
--
--      mrzne (273 × 41)       Stativní — a state that holds, not an event that runs. The air is below
--                             zero; nothing is happening, something is the case.
--      voda mrzne (273 × 42)  Mutativní — a gradual change of state, the group of bohatnout and
--                             černat. The water is turning into ice.
--      zmrzlo (281 × 41)      Rezultativní — the change reaches its end.
--      voda zmrzla (281 × 42) Rezultativní as well, which is why zmrznout keeps its lemma-level value
--                             from seed.019.sql and gets no rows here. A verb whose readings agree
--                             says it once on the entry; this table is for the ones that disagree.
--
--    Both of mrznout's groups are imperfective, which is what they require and what the lemma is, so
--    the aspect check passes for both readings.
--
--    lemma_entry.aktionsart stays NULL for mrznout, and now says something it could not say before:
--    the word as a word belongs to no one group. Where the entry is silent and a reading speaks, the
--    reading is the answer; that is the precedence reflexive_type already has here.
--
--    The other two-sense weather verbs are not touched, and this time the reason was checked rather
--    than assumed: blýskat is frekventativní in both readings (blýská se, blýská očima — repetition of
--    one act either way) and hřmít dekurzivní in both (hřmí, hrom hřmí). Their lemma-level value covers
--    every reading, so a row here would be a second place to keep the same fact.
--
-- 2. setmět COMES BACK, as a variant of setmít. seed.019.sql renamed the headword to the spelling IJP
--    leads with and dropped the other, noting there was no column for a doublet. IJP marks setmět se
--    as "lze i": both are standard, and a dictionary that no longer recognizes one of them lost
--    something. lemma_variant is that column.
--
--    Not a second lemma_entry: the two spellings share every stem, every vzor and every frame, so a
--    copy would be two rows to keep in step and a lookup that had to pick. A lookup landing on the
--    variant returns entry 275, so setmět is understood and setmít is what comes out — the variant is
--    recognized, not preserved.
--
--    It is the only one here. Doublets like myslet/myslit are the same case and belong in this table
--    too, but they are not in the dictionary at all yet; adding them is adding verbs, not adding
--    variants, and that is its own seed.

-- Způsob slovesného děje po čteních. Vid, který každá skupina nese, hlídá validátor proti
-- AktionsartFacts — u řádku tady proti vidu toho hesla, na které ukazuje.
INSERT INTO lemma_sense (lemma_sense_id, lemma_entry_id, lu_id, aktionsart, note) VALUES
    (1, 273, 41, 'Stative',  'Mrzne — stav vzduchu, ne děj.'),
    (2, 273, 42, 'Mutative', 'Voda mrzne — postupná změna, jako bohatnout.');

-- Dubleta. lemma_key je vyhledávací klíč, ne kopie lemmatu, takže se zapisuje malými písmeny stejně
-- jako u lemma_entry.
INSERT INTO lemma_variant (variant_id, lemma_entry_id, lemma, lemma_key, note) VALUES
    (1, 275, 'setmět', 'setmět',
     'IJP vede setmít se, setmět se je „lze i“. Obě spisovné, slovník generuje to první.');
