-- Migrace centrální MySQL kopie ze schématu 4 na 5.
--
-- Přidává lemma_entry.aktionsart — způsob slovesného děje. Není to jemnější vid: vid je gramatická
-- kategorie o dvou členech, kterou má každé české sloveso, kdežto tohle je lexikální třídění toho, jaký
-- druh děje sloveso pojmenovává, a většina sloves do žádné skupiny nepatří. Sloupec je proto nullable
-- a prázdno znamená „nezařazeno“, ne „žádný“.
--
-- Výčet je celý z Nového encyklopedického slovníku češtiny, heslo ZPŮSOB SLOVESNÉHO DĚJE — 26 skupin,
-- vzatých vcelku. Dvacet šest a ne dvacet pět: skupiny jsou značené českou abecedou, kde je ch
-- samostatné písmeno mezi h a i, takže a–y jde o jednu dál, než to vypadá.
--
-- Verze se posouvá, protože starý klient tenhle sloupec v seznamu nemá a importér stránku s nečekaným
-- sloupcem odmítne. Nová databáze tedy potřebuje nového klienta; obráceně to funguje.
--
-- Pouští se ručně, přes phpMyAdmin nebo `mysql`, po migrate.003-004.

ALTER TABLE lemma_entry
    ADD COLUMN aktionsart VARCHAR(24) COLLATE utf8mb4_bin NULL AFTER aspect_counterpart;

ALTER TABLE lemma_entry
    ADD CONSTRAINT ck_lemma_entry_aktionsart CHECK (aktionsart IS NULL OR aktionsart IN (
        'Ingressive', 'Evolutive', 'Delimitative', 'Resultative', 'Terminative',
        'Perdurative', 'Finitive', 'Egressive', 'Exhaustive', 'Total', 'Saturative',
        'Extensive', 'Cumulative', 'Intensive', 'Excessive', 'Distributive', 'Attenuative',
        'Semelfactive', 'Momentary', 'Iterative', 'Diminutive', 'Comitative', 'Frequentative',
        'Stative', 'Decursive', 'Mutative'));

-- Až nakonec: dokud sloupec není, je databáze pořád verze 4 a nemá se tak tvářit.
UPDATE lexicon_meta SET meta_value = '5' WHERE meta_key = 'schema_version';
