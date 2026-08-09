-- Migrace centrální MySQL kopie ze schématu 5 na 6.
--
-- Dvě nové tabulky, obě z toho, že jedno lemma není vždycky jedna věc.
--
-- 1) lemma_sense — co platí o jednom hesle v jednom významu.
--
--    Schéma 5 dalo aktionsart na lemma_entry a `mrznout` ukázalo, že to nestačí: *mrzne* je stav
--    vzduchu, *voda mrzne* postupná změna vody. Sloupec na lemmatu neumí obě, takže heslo zůstalo
--    nezařazené — ne omylem, ale protože zapsat jednu skupinu by lhalo o druhém významu.
--
--    První nápad byl dát sloupec na lexical_unit, kde už bydlí rámce. Nejde to: význam visí na lexému
--    a lexém je vidová dvojice, takže by hodnota napsaná u významu dopadla i na `zmrznout`. A tam je
--    jiná — *zmrzlo* je rezultativní v obou významech. Čtyři čtení, tři skupiny; sloupec na lemmatu si
--    musí vybrat význam, sloupec na významu si musí vybrat vid. Odsud vazební tabulka.
--
--    Řádek je jen tam, kde se význam od hesla liší. Sloveso, jehož významy se shodnou, to řekne jednou
--    na lemma_entry a tady se neobjeví — to je skoro každé.
--
-- 2) lemma_variant — druhá pravopisná podoba jednoho hesla.
--
--    `setmít se` a `setmět se` jsou jedno sloveso: IJP uvádí první a u druhého píše „lze i“, spisovné
--    jsou obě. Když se heslo přejmenovalo na spisovnější podobu, druhá tiše zmizela a slovník ji přestal
--    poznávat. Druhé lemma_entry to neřeší — obě podoby mají tytéž kmeny, týž vzor a tytéž rámce, takže
--    kopie by byla dva řádky, které je nutné držet v souladu. Varianta je jen záznam o tom, že se pod
--    tímhle klíčem hledá totéž heslo.
--
--    Vyhledání, které skončí na variantě, vrátí heslo, ke kterému patří: varianta se poznává, ale
--    negeneruje. Co slovník uvádí jako základní, to z něj taky leze.
--
-- Verze se posouvá, protože starý klient ani jednu tabulku v seznamu nemá a importér stránku s nečekanou
-- tabulkou odmítne. Nová databáze tedy potřebuje nového klienta; obráceně to funguje.
--
-- Pouští se ručně, přes phpMyAdmin nebo `mysql`, po migrate.004-005.

CREATE TABLE lemma_variant (
    variant_id      INT          NOT NULL AUTO_INCREMENT,
    lemma_entry_id  INT          NOT NULL,
    lemma           VARCHAR(64)  NOT NULL,

    -- utf8mb4_bin ze stejného důvodu jako lemma_entry.lemma_key: výchozí kolace je accent-insensitive
    -- a pod ní by 'setmět' a 'setmet' byl týž řetězec, takže by UNIQUE jeden z nich odmítl.
    lemma_key       VARCHAR(64)  COLLATE utf8mb4_bin NOT NULL,

    note            VARCHAR(500),

    CONSTRAINT pk_lemma_variant PRIMARY KEY (variant_id),
    CONSTRAINT uq_lemma_variant_key UNIQUE (lemma_key),
    CONSTRAINT fk_lemma_variant_entry FOREIGN KEY (lemma_entry_id) REFERENCES lemma_entry (lemma_entry_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE lemma_sense (
    lemma_sense_id  INT          NOT NULL AUTO_INCREMENT,
    lemma_entry_id  INT          NOT NULL,
    lu_id           INT          NOT NULL,
    aktionsart      VARCHAR(24)  COLLATE utf8mb4_bin,
    note            VARCHAR(500),
    CONSTRAINT pk_lemma_sense PRIMARY KEY (lemma_sense_id),
    CONSTRAINT uq_lemma_sense UNIQUE (lemma_entry_id, lu_id),
    CONSTRAINT fk_lemma_sense_entry FOREIGN KEY (lemma_entry_id) REFERENCES lemma_entry (lemma_entry_id),
    CONSTRAINT fk_lemma_sense_unit FOREIGN KEY (lu_id) REFERENCES lexical_unit (lu_id),
    CONSTRAINT ck_lemma_sense_aktionsart CHECK (aktionsart IS NULL OR aktionsart IN (
        'Ingressive', 'Evolutive', 'Delimitative', 'Resultative', 'Terminative',
        'Perdurative', 'Finitive', 'Egressive', 'Exhaustive', 'Total', 'Saturative',
        'Extensive', 'Cumulative', 'Intensive', 'Excessive', 'Distributive', 'Attenuative',
        'Semelfactive', 'Momentary', 'Iterative', 'Diminutive', 'Comitative', 'Frequentative',
        'Stative', 'Decursive', 'Mutative'))
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- Až nakonec: dokud tabulky nejsou, je databáze pořád verze 5 a nemá se tak tvářit.
UPDATE lexicon_meta SET meta_value = '6' WHERE meta_key = 'schema_version';
