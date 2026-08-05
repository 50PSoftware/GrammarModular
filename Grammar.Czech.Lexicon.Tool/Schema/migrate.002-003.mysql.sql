-- Migrace centrální MySQL kopie ze schématu 2 na 3.
--
-- schema.mysql.sql zakládá tabulky od nuly, což je k ničemu ve chvíli, kdy už na serveru data jsou.
-- Tenhle skript je proto k dispozici zvlášť a pouští se ručně jednou — přes phpMyAdmin nebo `mysql`.
-- Nová je jen sada sloupců; žádný řádek se nepřepisuje a všechny nové sloupce jsou NULL, což znamená
-- „řídí se vzorem“, takže po migraci se slovník chová přesně jako předtím.
--
-- Odpovídá lemma_entry v schema.mysql.sql. Kdyby se ty dva rozešly, pozná se to až na klientovi:
-- importér stahuje sloupce podle seznamu v schema-tables.php a sloupec, který server nemá, skončí
-- chybou uprostřed pullu.

ALTER TABLE lemma_entry
    ADD COLUMN stem            VARCHAR(32) COLLATE utf8mb4_bin NULL AFTER base_verb_lemma,
    ADD COLUMN present_stem    VARCHAR(32) COLLATE utf8mb4_bin NULL AFTER stem,
    ADD COLUMN past_stem       VARCHAR(32) COLLATE utf8mb4_bin NULL AFTER present_stem,
    ADD COLUMN future_stem     VARCHAR(32) COLLATE utf8mb4_bin NULL AFTER past_stem,
    ADD COLUMN imperative_stem VARCHAR(32) COLLATE utf8mb4_bin NULL AFTER future_stem,
    ADD COLUMN passive_stem    VARCHAR(32) COLLATE utf8mb4_bin NULL AFTER imperative_stem,
    ADD COLUMN infinitive      VARCHAR(64) COLLATE utf8mb4_bin NULL AFTER passive_stem,
    ADD COLUMN forms_passive   SMALLINT                        NULL AFTER infinitive;

ALTER TABLE lemma_entry
    ADD CONSTRAINT ck_lemma_entry_forms_passive
        CHECK (forms_passive IS NULL OR forms_passive IN (0, 1));

-- Až nakonec: dokud sloupce nejsou, je databáze pořád verze 2 a nemá se tak tvářit.
UPDATE lexicon_meta SET meta_value = '3' WHERE meta_key = 'schema_version';
