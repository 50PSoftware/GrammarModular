-- Migrace centrální MySQL kopie ze schématu 8 na 9.
--
-- Přidávají se dvě tabulky pro komponenciální sémantiku: semantic_feature (rysy jednoho významu) a
-- semantic_relation (synonymie/antonymie mezi dvěma významy). Obě visí na lu_id (lexical_unit), ne na
-- lemma_entry_id — rys nebo vztah patří konkrétnímu smyslu slova, ne každému homonymu, které sdílí
-- heslo. Podrobné zdůvodnění (proč ne WordNet synsety, proč feature_value zůstává text a ne enum,
-- proč je semantic_relation doplněk k rysům a ne primární zdroj pravdy) je v komentářích u obou tabulek
-- ve schema.sql.
--
-- Pouští se ručně, přes phpMyAdmin nebo `mysql`, po migrate.007-008.

CREATE TABLE semantic_feature (
    feature_id     INT          NOT NULL AUTO_INCREMENT,
    lu_id          INT          NOT NULL,
    feature_name   VARCHAR(64)  COLLATE utf8mb4_bin NOT NULL,
    feature_value  VARCHAR(64)  NOT NULL,
    value_kind     VARCHAR(16)  COLLATE utf8mb4_bin NOT NULL,
    source         VARCHAR(64)  NOT NULL,
    note           VARCHAR(500),
    is_verified    SMALLINT     NOT NULL DEFAULT 0,
    CONSTRAINT pk_semantic_feature PRIMARY KEY (feature_id),
    CONSTRAINT uq_semantic_feature_name UNIQUE (lu_id, feature_name),
    CONSTRAINT fk_semantic_feature_lu FOREIGN KEY (lu_id) REFERENCES lexical_unit (lu_id),
    CONSTRAINT ck_semantic_feature_kind CHECK (value_kind IN ('Binary', 'Scalar', 'Categorical')),
    CONSTRAINT ck_semantic_feature_verified CHECK (is_verified IN (0, 1))
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE semantic_relation (
    relation_id      INT          NOT NULL AUTO_INCREMENT,
    lu_id_a          INT          NOT NULL,
    lu_id_b          INT          NOT NULL,
    relation_type    VARCHAR(16)  COLLATE utf8mb4_bin NOT NULL,
    antonym_subtype  VARCHAR(16)  COLLATE utf8mb4_bin,
    strength         DOUBLE,
    source           VARCHAR(64)  NOT NULL,
    note             VARCHAR(500),
    is_verified      SMALLINT     NOT NULL DEFAULT 0,
    CONSTRAINT pk_semantic_relation PRIMARY KEY (relation_id),
    CONSTRAINT uq_semantic_relation_pair UNIQUE (lu_id_a, lu_id_b, relation_type),
    CONSTRAINT fk_semantic_relation_lu_a FOREIGN KEY (lu_id_a) REFERENCES lexical_unit (lu_id),
    CONSTRAINT fk_semantic_relation_lu_b FOREIGN KEY (lu_id_b) REFERENCES lexical_unit (lu_id),
    CONSTRAINT ck_semantic_relation_type CHECK (relation_type IN ('Synonym', 'Antonym')),
    CONSTRAINT ck_semantic_relation_subtype CHECK (
        antonym_subtype IS NULL OR antonym_subtype IN ('Complementary', 'Scalar', 'Converse')),
    CONSTRAINT ck_semantic_relation_subtype_scope CHECK (
        relation_type = 'Antonym' OR antonym_subtype IS NULL),
    CONSTRAINT ck_semantic_relation_distinct CHECK (lu_id_a <> lu_id_b),
    CONSTRAINT ck_semantic_relation_verified CHECK (is_verified IN (0, 1))
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE INDEX ix_semantic_feature_name_value ON semantic_feature (feature_name, feature_value);
CREATE INDEX ix_semantic_relation_lu_b ON semantic_relation (lu_id_b);

-- Až nakonec: dokud obě tabulky nejsou hotové, databáze je pořád verze 8 a nemá se tak tvářit.
UPDATE lexicon_meta SET meta_value = '9' WHERE meta_key = 'schema_version';
