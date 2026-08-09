-- Migrace centrální MySQL kopie ze schématu 3 na 4.
--
-- Rozšiřuje povolené hodnoty valency_frame.kind o 'Impersonal' — o bezpodměťová slovesa, která nemají
-- žádný slot: prší, sněží, svítá. Tabulky, sloupce ani indexy se nemění, mění se jen to, co do sloupce
-- smí, a proto je to migrace a ne oprava dat: řádek zapsaný podle nového čtení starý CHECK odmítne.
--
-- Verze se posouvá, protože nekompatibilita je jednosměrná. Starý klient nad novou databází spadne
-- na ParseEnum, jakmile takový rámec načte; nový klient nad starou databází funguje. schema_version je
-- přesně to místo, kde se tenhle rozdíl hlásí předem, místo aby se objevil uprostřed čtení slovníku.
--
-- Pouští se ručně, přes phpMyAdmin nebo `mysql`, po migrate.002-003.
--
-- MySQL neumí CHECK constraint změnit — musí se zahodit a založit znovu. Mezi tím databáze na ten
-- sloupec nic nehlídá, takže se to nepouští proti běžícímu adminu.

ALTER TABLE valency_frame
    DROP CONSTRAINT ck_valency_frame_kind;

ALTER TABLE valency_frame
    ADD CONSTRAINT ck_valency_frame_kind CHECK (kind IN (
        'Verbal', 'Copular_NominalPred', 'Copular_AdjectivalPred', 'Existential',
        'Modal', 'PhasalLightVerb', 'LightVerb', 'Impersonal'));

-- Až nakonec: dokud CHECK nepovoluje novou hodnotu, je databáze pořád verze 3 a nemá se tak tvářit.
UPDATE lexicon_meta SET meta_value = '4' WHERE meta_key = 'schema_version';
