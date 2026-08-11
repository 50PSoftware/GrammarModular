-- Migrace centrální MySQL kopie ze schématu 7 na 8.
--
-- Dvě změny, obě z jednoho zjištění: FGD funktorů bylo o pět málo.
--
-- 1) Do výčtu funktorů přibývá RHEM, MOD, ATT, PREC a PARTL.
--
--    Dosavadních 25 členů jsou účastníci děje (ACT, PAT, ADDR…) a okolnosti (LOC, TWHEN, MANN…). Pro
--    částici ani citoslovce mezi nimi nebylo nic. Věta se s nimi postavit dala jen tak, že se jim
--    vnutil MANN — tedy že se do rozboru zapsalo, že `asi` odpovídá na otázku „jak“, což neodpovídá.
--
--    Pražský závislostní korpus na to funktory má a tohle jsou ony: RHEM pro rematizátory (jen, také),
--    MOD pro modalitu (asi, prý), ATT pro postoj (bohužel), PREC pro navazování (tedy, však) a PARTL
--    pro citoslovce, které nevyplňuje slot a nic nerozvíjí — je ve větě, aniž by bylo částí toho, co
--    věta říká.
--
--    Výčet se rozšiřuje na všech třech místech, kde je: valency_slot.functor, valency_slot.control_target
--    a sloupec na hesle.
--
-- 2) lemma_entry.adverbial_functor se přejmenovává na inherent_functor.
--
--    Sloupec vznikl v schématu 7 pro příslovce a stejnou práci odvede u částic — `asi` je MOD, `jen`
--    je RHEM, a je to stejně lexikální údaj jako `dnes` = TWHEN, tedy nic, co by šlo odvodit. Jméno
--    `adverbial_functor` by na řádku částice lhalo.
--
--    `inherent_functor` ve stejném smyslu, v jakém tenhle slovník mluví o inherentní reflexivitě:
--    platí to pro slovo, ne pro rámec, ve kterém zrovna stojí.
--
-- Verze se posouvá, protože přejmenovaný sloupec starý klient v seznamu nemá a importér stránku
-- s nečekaným sloupcem odmítne. Nová databáze tedy potřebuje nového klienta; obráceně to funguje.
--
-- Pouští se ručně, přes phpMyAdmin nebo `mysql`, po migrate.006-007.

-- Nejdřív omezení pryč, pak sloupec, pak omezení zpátky — CHECK se váže na jméno sloupce a MySQL by
-- přejmenování pod ním neproneslo.
ALTER TABLE lemma_entry DROP CONSTRAINT ck_lemma_entry_adverbial_functor;

ALTER TABLE lemma_entry
    CHANGE COLUMN adverbial_functor inherent_functor VARCHAR(16) COLLATE utf8mb4_bin NULL;

ALTER TABLE lemma_entry
    ADD CONSTRAINT ck_lemma_entry_inherent_functor CHECK (inherent_functor IS NULL OR inherent_functor IN (
        'ACT', 'PAT', 'ADDR', 'ORIG', 'EFF', 'DIR1', 'DIR2', 'DIR3', 'LOC', 'MANN',
        'MEANS', 'BEN', 'CAUS', 'AIM', 'TWHEN', 'DIFF', 'OBST', 'INTT', 'MAT', 'THL',
        'EXT', 'CRIT', 'ACMP', 'COMPL', 'CPHR',
        'RHEM', 'MOD', 'ATT', 'PREC', 'PARTL'));

ALTER TABLE valency_slot DROP CONSTRAINT ck_valency_slot_functor;

ALTER TABLE valency_slot
    ADD CONSTRAINT ck_valency_slot_functor CHECK (functor IN (
        'ACT', 'PAT', 'ADDR', 'ORIG', 'EFF', 'DIR1', 'DIR2', 'DIR3', 'LOC', 'MANN',
        'MEANS', 'BEN', 'CAUS', 'AIM', 'TWHEN', 'DIFF', 'OBST', 'INTT', 'MAT', 'THL',
        'EXT', 'CRIT', 'ACMP', 'COMPL', 'CPHR',
        'RHEM', 'MOD', 'ATT', 'PREC', 'PARTL'));

ALTER TABLE valency_slot DROP CONSTRAINT ck_valency_slot_control;

ALTER TABLE valency_slot
    ADD CONSTRAINT ck_valency_slot_control CHECK (control_target IS NULL OR control_target IN (
        'ACT', 'PAT', 'ADDR', 'ORIG', 'EFF', 'DIR1', 'DIR2', 'DIR3', 'LOC', 'MANN',
        'MEANS', 'BEN', 'CAUS', 'AIM', 'TWHEN', 'DIFF', 'OBST', 'INTT', 'MAT', 'THL',
        'EXT', 'CRIT', 'ACMP', 'COMPL', 'CPHR',
        'RHEM', 'MOD', 'ATT', 'PREC', 'PARTL'));

-- Až nakonec: dokud sloupec nemá nové jméno, je databáze pořád verze 7 a nemá se tak tvářit.
UPDATE lexicon_meta SET meta_value = '8' WHERE meta_key = 'schema_version';
