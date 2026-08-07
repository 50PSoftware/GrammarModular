-- Grammar.Czech — lexicon seed, update 13.
--
-- Continues seed.000.sql through seed.011.sql. Last ids used there: lexeme 30,
-- lemma_entry 259, lexical_unit 32, valency_frame 33, valency_slot 78,
-- slot_realization 85. Append after all twelve, in order.
--
-- Provenance: hand-authored from Internetová jazyková příručka (prirucka.ujc.cas.cz), heslo
-- „Skloňování ženských jmen vzoru žena — 2. p. mn. č.“ (id=250). Every `source` value stays 'IJP'.
--
-- What this round is FOR: doplnit typ ou→u, který seed.011.sql odložil s odůvodněním, že `ou` je
-- digraf, a ne jeden foném. To odůvodnění bylo špatně. Heslo DIFTONG v Novém encyklopedickém
-- slovníku češtiny (Krčmová) hodnotí české /ou̯/ monofonematicky, a opírá se přitom právě o tuhle
-- alternaci s jednoduchým vokálem (dub – doubek); bifonematické čtení „obecně přijato nebylo“.
-- Registr fonémů to ostatně měl správně celou dobu — je klíčovaný řetězcem a "ou" v něm bylo,
-- jen bez krátkého protějšku. Chyběl tedy jeden údaj, ne fonologická cesta.
--
-- IJP uvádí u tohoto vzoru krácení á→a, í→i a ou→u, a výslovně říká, že é, ó, ý a ú/ů nekrátí.
-- Říká taky, že krácení nenastává, stojí-li za dlouhou samohláskou skupina souhlásek (brázda →
-- brázd). Obojí teď drží veto v evaluátoru, takže tyhle řádky jsou data, ne výjimky.
--
-- Moucha krátí, brázda ne, a rozdíl je v tom, že ch je jeden foném a zd dva. Veto proto počítá
-- fonémy, ne písmena — jinak by moucha spadla do stejné skupiny jako brázda.
--
-- What is deliberately left OUT:
--
--   * míra → měr, díra → děr, víra → věr. Vypadá to jako krácení í→i, ale není: í se mění na ě,
--     což je jiná alternace než zkrácení kvantity, a has_genitive_plural_shortening o ní nic
--     neříká. Evaluátor by jim dal mir a dir. Patří na lemma_entry.stem, stejně jako dům a nůž.
--   * Polysylabika a novější výpůjčky. IJP k tomu píše, že „dnes se stále silněji prosazuje
--     tendence, aby k takovému krácení nedocházelo“ — proto je tenhle seed uzavřený výčet
--     doložených slov, ne pokus o třídu.

-- ─────────────────────────────────────────────────────────────────────────────
-- Lemma entries — nouns, vzor žena, krácení ou→u v gen. pl.
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
    is_animate, has_mobile_e, has_genitive_plural_shortening,
    aspect, aspect_counterpart, lexeme_id, source, is_verified, note)
VALUES
    (260, 'houba',   'houba',   1, 'Noun', 'Feminine', 'žena', 0, 0, 1, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. hub.'),
    (261, 'smlouva', 'smlouva', 1, 'Noun', 'Feminine', 'žena', 0, 0, 1, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. smluv.'),
    (262, 'touha',   'touha',   1, 'Noun', 'Feminine', 'žena', 0, 0, 1, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. tuh.'),
    (263, 'moucha',  'moucha',  1, 'Noun', 'Feminine', 'žena', 1, 0, 1, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. much — ch je jeden foném, takže za ou nestojí shluk a krácení projde.'),

    -- Nekrátí — za á stojí shluk zd. Zapsaná nula ze stejného důvodu jako káva v seed.011.sql:
    -- teprve proti ní je vidět, že veto na shluk něco dělá.
    (264, 'brázda',  'brázda',  1, 'Noun', 'Feminine', 'žena', 0, 0, 0, NULL, NULL, NULL, 'IJP', 1,
          'Gen. pl. brázd — délka zůstává, za á je souhláskový shluk.');
