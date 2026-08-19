-- Per-user API tokens for the lexicon admin, MySQL/MariaDB.
--
-- Not part of Schema/schema.sql. That file is the shape of the dictionary — lexemes, entries, frames —
-- and is shared with the local SQLite replica through the paged API and LexiconSchema.cs. This table
-- holds nothing about the dictionary; it exists only so the admin app can tell which website account
-- a `lexikon.ps1 pull` bearer token belongs to. It is never pulled, never exported, and has no C#-side
-- counterpart, so it stays out of the schema files SchemaParityTests and PhpSchemaParityTests compare.
--
-- Run once by hand against the same MySQL database LEXICON_MYSQL_DSN points at (phpMyAdmin or `mysql`),
-- the same way .env.php itself is deployed by hand.
--
-- web_user_id is not a foreign key: the website's `user` table lives in a different MySQL database
-- (LEXICON_WEB_MYSQL_DSN), and a cross-database FOREIGN KEY is not something MySQL/MariaDB supports.
CREATE TABLE api_token (
    id             INT           NOT NULL AUTO_INCREMENT,
    token_hash     CHAR(64)      COLLATE utf8mb4_bin NOT NULL,
    web_user_id    INT           NOT NULL,

    -- What the person called it when they made it — "notebook", "server pull" — so a list of tokens
    -- means something to whoever is revoking one.
    label          VARCHAR(128),

    created_at     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_used_at   DATETIME,

    CONSTRAINT pk_api_token PRIMARY KEY (id),
    CONSTRAINT uq_api_token_hash UNIQUE (token_hash)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE INDEX ix_api_token_web_user ON api_token (web_user_id);
