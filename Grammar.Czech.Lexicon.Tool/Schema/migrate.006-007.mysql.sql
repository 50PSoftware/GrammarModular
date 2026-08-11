-- Migrace centrální MySQL kopie ze schématu 6 na 7.
--
-- Přidává lemma_entry.adverbial_functor — okolnost, kterou příslovce vyjadřuje samo o sobě:
-- `dnes` je TWHEN, `doma` LOC, `rychle` MANN.
--
-- Odvodit to nejde. Zakončení neříká nic a přídavné jméno, ze kterého příslovce vzniklo, taky ne —
-- `rychlý` a `rychle` je jedno slovo ve dvou slovních druzích a na otázku „jak“ odpovídá jen jedno
-- z nich. Je to fakt o jednom slově, a ten patří do slovníku, ne do kódu.
--
-- Proč do slovníku a ne k příslovcím v Data/Rules/adverbs.json, kde už 291 hesel je: adverbs.json je
-- vestavěný do knihovny, takže oprava jednoho slova by znamenala nové vydání balíčku. Je to týž důvod,
-- pro který sloupce se slovesnými kmeny nesedí ve vestavěných irregulars.json, ale tady. Nepravidelné
-- tvary v JSONu zůstávají — to je morfologie, ne fakt o významu.
--
-- Prázdno je běžný stav a znamená „nikdo neřekl“, ne „žádná okolnost“. Generátor pak roli potřebuje
-- dostat zvenčí, což potřeboval u každého příslovce i předtím, než sloupec existoval.
--
-- Výčet je celý FgdFunctor, ne jen okolnostní část. Příslovce aktantem nebývá, ale omezit to natvrdo
-- by znamenalo rozhodnout, kterou okolnost čeština příslovcem vyjádřit ještě umí a kterou už ne —
-- a to schéma rozhodovat nemá.
--
-- Verze se posouvá, protože starý klient tenhle sloupec v seznamu nemá a importér stránku s nečekaným
-- sloupcem odmítne. Nová databáze tedy potřebuje nového klienta; obráceně to funguje.
--
-- Pouští se ručně, přes phpMyAdmin nebo `mysql`, po migrate.005-006.

ALTER TABLE lemma_entry
    ADD COLUMN adverbial_functor VARCHAR(16) COLLATE utf8mb4_bin NULL AFTER base_verb_lemma;

ALTER TABLE lemma_entry
    ADD CONSTRAINT ck_lemma_entry_adverbial_functor CHECK (adverbial_functor IS NULL OR adverbial_functor IN (
        'ACT', 'PAT', 'ADDR', 'ORIG', 'EFF', 'DIR1', 'DIR2', 'DIR3', 'LOC', 'MANN',
        'MEANS', 'BEN', 'CAUS', 'AIM', 'TWHEN', 'DIFF', 'OBST', 'INTT', 'MAT', 'THL',
        'EXT', 'CRIT', 'ACMP', 'COMPL', 'CPHR'));

-- Až nakonec: dokud sloupec není, je databáze pořád verze 6 a nemá se tak tvářit.
UPDATE lexicon_meta SET meta_value = '7' WHERE meta_key = 'schema_version';
