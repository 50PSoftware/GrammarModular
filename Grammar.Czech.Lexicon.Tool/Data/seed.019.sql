-- Grammar.Czech — lexicon seed, update 20.
--
-- Continues seed.000.sql through seed.018.sql. Last ids used there: lexeme 37,
-- lemma_entry 281, lexical_unit 44, valency_frame 45, valency_slot 82,
-- slot_realization 89, construction 3. Append after all nineteen, in order. No new ids: this file
-- only corrects and classifies rows that already exist.
--
-- Provenance: Internetová jazyková příručka (prirucka.ujc.cas.cz) for the headword; Nový
-- encyklopedický slovník češtiny, heslo ZPŮSOB SLOVESNÉHO DĚJE, for the classification.
--
-- 1. setmět se → setmít se. IJP gives setmít se as the headword and marks setmět se as "lze i" — both
--    are standard written Czech and the first is the one the dictionary leads with, so that is the
--    lemma here too. The past stays setmělo se under either spelling, which is why the row gains a
--    past_stem: class 4 derives the past from the infinitive and would give setmílo se.
--
--    That is the third verb in this family to need one — hřmít gave hřmíl, rozednít gave rozednílo,
--    setmít gives setmílo. The class carries the infinitive's long í into a syllable that shortens, so
--    every new -ít verb here wants its past run before the row is written rather than after.
--
--    The variant setmět se is not recorded. There is no column for a doublet of the infinitive:
--    lemma_entry.infinitive holds the infinitive when it differs from the lemma (říct beside říci),
--    not a second equally valid spelling. Noted rather than shoehorned.
--
-- 2. THE WEATHER VERBS GET THEIR GROUP. seed.018.sql left them unclassified saying the choice between
--    dekurzivní and stativní would be a feeling. Read against the source's definitions rather than its
--    examples, most of them are not close calls at all:
--
--      pršet, sněžit, hřmít    Dekurzivní — an event that simply runs on, with no result it works
--                              towards and no state it settles into. The source's examples (psát,
--                              myslet, zpívat) have an actor and these do not, but the group is about
--                              the shape of the event and not about who causes it.
--      svítat, stmívat         Mutativní — a gradual change of state, which is the group of bohatnout
--                              and černat: getting light and getting dark are the same shape as
--                              getting rich and turning black.
--      setmít, rozednít        The completed counterparts. rozednít is roz- and the source puts roz-
--                              verbs in the evolutivní group (rozšumět se, rozplakat se); setmít has
--                              no prefix the source lists, and is rezultativní on its meaning — the
--                              change reaches its end.
--      zmrznout                Rezultativní on the same reading: the result is being frozen.
--
--    stmívat carries the -ívat of the iterativní group (dělávat, mívat) and is not one. That group is
--    the habitual — to do a thing repeatedly over time — and stmívat se is not habitual, it is the
--    imperfective half of one darkening. The suffix is the wrong witness here and the meaning decides.
--
-- 3. mrznout STAYS NULL, and this time for a reason in the schema rather than in my judgement. Its two
--    senses do not share a group: mrzne (the air is below zero) is stativní, voda mrzne (the water is
--    turning to ice) is mutativní. aktionsart sits on lemma_entry and there is one row for the lemma,
--    so the column cannot hold both. Filling it would make one sense lie about the other.
--
--    The fix is to move the column to lexical_unit, where the frames already live — způsob slovesného
--    děje is standardly described of the verb, but this lemma is the counterexample. That is schema 6
--    and a migration of its own, not a line here.

-- Přejmenování. lemma_key se mění spolu s lemmatem: je to vyhledávací klíč, ne jeho kopie.
UPDATE lemma_entry
   SET lemma = 'setmít',
       lemma_key = 'setmít',
       past_stem = 'setmě',
       note = 'IJP vede setmít se, setmět se je „lze i“. Minulý kmen zapsán: třída by dala setmílo.'
 WHERE lemma_entry_id = 275;

UPDATE lemma_entry SET aspect_counterpart = 'setmít' WHERE lemma_entry_id = 271;
UPDATE lexeme SET note = 'Vidová dvojice stmívat / setmít.' WHERE lexeme_id = 34;

-- Způsob slovesného děje. Vid, který každá skupina nese, hlídá validátor proti AktionsartFacts.
UPDATE lemma_entry SET aktionsart = 'Decursive'   WHERE lemma_entry_id = 268;  -- pršet
UPDATE lemma_entry SET aktionsart = 'Decursive'   WHERE lemma_entry_id = 269;  -- sněžit
UPDATE lemma_entry SET aktionsart = 'Decursive'   WHERE lemma_entry_id = 274;  -- hřmít
UPDATE lemma_entry SET aktionsart = 'Mutative'    WHERE lemma_entry_id = 270;  -- svítat
UPDATE lemma_entry SET aktionsart = 'Mutative'    WHERE lemma_entry_id = 271;  -- stmívat
UPDATE lemma_entry SET aktionsart = 'Resultative' WHERE lemma_entry_id = 275;  -- setmít
UPDATE lemma_entry SET aktionsart = 'Evolutive'   WHERE lemma_entry_id = 277;  -- rozednít
UPDATE lemma_entry SET aktionsart = 'Resultative' WHERE lemma_entry_id = 281;  -- zmrznout
