-- Migrace centrální MySQL kopie ze schématu 1 na 2.
--
-- Doplňuje valency_frame.reflexive_type, který přišel s commitem f2565df. Skript se tehdy nenapsal,
-- takže databáze nasazená před ním má tabulku pořád ve tvaru schématu 1 — admin na ní hlásí
-- „Undefined array key reflexive_type“ při čtení rámce a uložení rámce spadne na neznámém sloupci.
--
-- Pouští se ručně, přes phpMyAdmin nebo `mysql`, a před migrate.002-003.
--
-- Výchozí 'None' znamená, že každý existující rámec dál říká to, co říkal: reflexivita se ke slovu
-- nepřidala, jen dostala místo, kde se dá vyslovit.

ALTER TABLE valency_frame
    ADD COLUMN reflexive_type VARCHAR(32) COLLATE utf8mb4_bin NOT NULL DEFAULT 'None'
        AFTER is_default;

ALTER TABLE valency_frame
    ADD CONSTRAINT ck_valency_frame_reflexive
        CHECK (reflexive_type IN (
            'None', 'ReflexivumTantum_Se', 'ReflexivumTantum_Si', 'DerivedReflexive_Se',
            'DerivedBenefactive_Si', 'Reciprocal_Se', 'DeagentivePassive_Se'));

-- Až nakonec: dokud sloupec není, je databáze pořád verze 1 a nemá se tak tvářit.
UPDATE lexicon_meta SET meta_value = '2' WHERE meta_key = 'schema_version';
