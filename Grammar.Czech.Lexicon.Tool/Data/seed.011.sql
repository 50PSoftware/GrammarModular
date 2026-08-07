-- Grammar.Czech — lexicon seed, update 12.
--
-- Continues seed.000.sql through seed.010.sql. Last ids used there: lexeme 30,
-- lemma_entry 255, lexical_unit 32, valency_frame 33, valency_slot 78,
-- slot_realization 85. Append after all eleven, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz). Every
-- `source` value stays 'IJP'.
--
-- What this round is FOR: dát has_genitive_plural_shortening první slova, která ho vůbec čtou.
-- Sloupec je ve schématu od začátku, ale až doteď ho nikdo nevyhodnocoval — evaluátor krácení
-- existoval jako neregistrovaná třída, kterou skloňování nevolalo. Teď volá, takže vlajka něco
-- dělá a dá se seedovat.
--
-- Krácení v gen. pl. je lexikální, ne pravidlo. Dvojice, která to říká celá: kráva → krav proti
-- káva → káv. Stejný vzor, stejná délka, stejná pozice — rozhoduje jen heslo. Káva už ve slovníku
-- je od seed.008.sql, takže ji tenhle soubor jen doplní o nulu; bez ní by nula zůstala jen
-- v komentáři a nikdo by ji netestoval.
--
-- Krátí spolehlivě jen á a í. é, ý a ú/ů si délku drží (sféra → sfér, rýha → rýh, kúra → kúr),
-- a evaluátor je odmítá bez ohledu na vlajku — kdyby se sem někdy dostalo heslo s jedničkou
-- a takovou samohláskou, veto ho zachytí. Nulu proto tato skupina nepotřebuje mít zapsanou.
--
-- What is deliberately left OUT:
--
--   * houba → hub, smlouva → smluv a spol. Je to ou→u, a ShortenVowel prochází kmen po znacích,
--     takže krátký protějšek k němu nenajde. Vlajka by byla zapsaná správně a neudělala by nic.
--     Oprava k seed_012: důvod tady původně stál na tvrzení, že `ou` je digraf, a ne jeden foném.
--     To je špatně — /ou̯/ se hodnotí monofonematicky, viz seed.012.sql, kde jsou tahle slova
--     doplněná. Omezení bylo v jednom chybějícím údaji v registru, ne ve fonologii.
--   * Polysylabika (zahrádka → zahrádek) a výpůjčky (káva už je tady jako nula). Nekrátí,
--     a tendence je taková, že novější slova nekrátí vůbec.
--   * sníh, nůž, oheň, déšť. Tři z předchozích seedů je odkládaly „na CzechAlternationRuleEvaluator“,
--     ale s ním nemají společného nic: je to dloužení v nom. sg., uzavřená množina, která jede
--     přes lemma_entry.stem. Ta cesta funguje od commitu 26a9e1e a čeká jen na vlastní seed.
--     Komentáře v seed.002.sql, seed.004.sql a seed.005/006.sql jsou tímto opravené.

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns, vzor žena, krácení kmene v gen. pl.
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, has_genitive_plural_shortening,
    aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    -- á → a
    (256, 'kráva', 'kráva', 1, 'Noun', 'Feminine', 'žena', 1, 0, 1, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. krav. Protějšek káva → káv níž: krácení je vlastnost hesla, ne vzoru.'),
    (257, 'brána', 'brána', 1, 'Noun', 'Feminine', 'žena', 0, 0, 1, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. bran.'),

    -- í → i
    (258, 'lípa',  'lípa',  1, 'Noun', 'Feminine', 'žena', 0, 0, 1, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. lip.'),
    (259, 'síla',  'síla',  1, 'Noun', 'Feminine', 'žena', 0, 0, 1, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. sil.');

-- Káva už heslo má, ze seed.008.sql — dostává jen vlajku. Nula zapsaná, ne vynechaná: teprve
-- proti ní kráva něco dokazuje, a NULL by znamenalo „nevíme“, což u výpůjčky není pravda.
UPDATE lemma_entry
   SET has_genitive_plural_shortening = 0,
       note = 'Gen. pl. káv — délka zůstává. Protějšek ke kráva → krav.'
 WHERE lemma_key = 'káva' AND category = 'Noun' AND homonym_index = 1;
