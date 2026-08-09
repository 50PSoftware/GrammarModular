-- Grammar.Czech — lexicon seed, update 19.
--
-- Continues seed.000.sql through seed.017.sql. Last ids used there: lexeme 37,
-- lemma_entry 281, lexical_unit 44, valency_frame 45, valency_slot 82,
-- slot_realization 89, construction 3. Append after all eighteen, in order.
--
-- Provenance: Nový encyklopedický slovník češtiny, heslo ZPŮSOB SLOVESNÉHO DĚJE. Every `source` value
-- stays 'IJP'.
--
-- What this round is FOR: filling lemma_entry.aktionsart, the column schema 5 added, for the verbs
-- that have one. seed.017.sql put the semelfactive nature of blýsknout in a note because there was no
-- column; there is one now, and the note it went in was wrong about the other verb besides.
--
-- WHAT THE COLUMN IS NOT. It is not a finer grade of aspect. Aspect is grammatical, has two members
-- and every Czech verb has one; this classifies what kind of event the verb names, and most verbs are
-- in none of the twenty-six groups at all. NULL here means unclassified, not "none" — which is why
-- most of the dictionary keeps it and only the rows below are filled.
--
-- HOW THESE WERE ASSIGNED, and it is deliberately narrow: only where the prefix or suffix matches an
-- example the source itself lists. That rule fills five rows and refuses the rest, which is the point —
-- the last three seeds each shipped a judgement I had to take back, and the way to stop is to assign
-- from the source's examples rather than from a feeling about the verb.
--
--   blýsknout   Semelfactive.  -nout, one instance of a repeatable act. The source's examples are
--                              bodnout, kopnout, mávnout — the same suffix and the same relation to a
--                              frequentative partner.
--   blýskat     Frequentative. bodat, kopat, klepat: the imperfective counterpart of exactly that.
--                              NOTE this is group (v) and not group (s), whose double name is the same
--                              two words reversed; (s) is the -ívat habitual (dělávat, mívat).
--   zahřmět     Ingressive.    za-, and the source lists zakašlat and zakřičet — a single cry, a single
--                              cough. seed.017.sql called it semelfactive "like blýsknout" and that was
--                              wrong: the -nout suffix is what makes a semelfactive, and za- makes this.
--   napršet     Cumulative.    na-, against the source's nabalit, nahrabat, nakapat. Rain accumulating
--                              is the same shape as drops accumulating, which is nakapat exactly.
--   nasněžit    Cumulative.    The same, and the reason the pair with sněžit reads the way it does.
--
-- WHAT IS LEFT NULL AND WHY, rather than filled with the nearest-looking group:
--
--   * pršet, sněžit, svítat, stmívat, mrznout, hřmít. Plain imperfective weather verbs. The source has
--     two groups they could sit in — dekurzivní (psát, myslet, zpívat: an activity that runs on) and
--     stativní (sedět, milovat: a state) — and telling a weather process from a weather state is a call
--     I would be making from a feeling. Unclassified is the honest value and the column allows it.
--   * setmět, rozednít, zmrznout. Change-of-state perfectives, and the source offers rezultativní,
--     evolutivní and mutativní as candidates that each fit a different reading of the same verb.
--     Same answer: the column is nullable for exactly this.
--   * Everything else in the dictionary. Two hundred and seventy entries and no reason to touch them.

UPDATE lemma_entry SET aktionsart = 'Semelfactive'  WHERE lemma_entry_id = 279;  -- blýsknout
UPDATE lemma_entry SET aktionsart = 'Frequentative' WHERE lemma_entry_id = 272;  -- blýskat
UPDATE lemma_entry SET aktionsart = 'Ingressive'    WHERE lemma_entry_id = 280;  -- zahřmět
UPDATE lemma_entry SET aktionsart = 'Cumulative'    WHERE lemma_entry_id = 278;  -- napršet
UPDATE lemma_entry SET aktionsart = 'Cumulative'    WHERE lemma_entry_id = 276;  -- nasněžit

-- Poznámka u zahřmět tvrdila semelfaktivum. Sloupec teď říká ingresivum a poznámka má říkat totéž.
UPDATE lemma_entry
   SET note = 'Ingresivum jako zakašlat: to za- je začátek děje, ne jeho jedinost. Minulý kmen netřeba.'
 WHERE lemma_entry_id = 280;

UPDATE lemma_entry
   SET note = 'Semelfaktivum: jeden záblesk proti trvání, které nese blýskat.'
 WHERE lemma_entry_id = 279;
