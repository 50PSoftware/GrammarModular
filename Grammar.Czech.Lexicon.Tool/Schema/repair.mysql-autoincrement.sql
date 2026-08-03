-- Oprava: doplní AUTO_INCREMENT na servery, kde schéma vzniklo z schema.sql místo schema.mysql.sql.
--
-- Příznak: administrace spadne při zakládání čehokoli s hláškou
--
--   SQLSTATE[HY000]: General error: 1364 Field 'slot_id' doesn't have a default value
--
-- (nebo lemma_entry_id, lu_id, frame_id, realization_id — podle toho, co se zakládalo jako první).
--
-- Proč se to stane: schema.sql je přenositelná varianta a AUTO_INCREMENT v ní schválně není, protože
-- tam identifikátory přiděluje zapisovatel — seed soubory je vkládají explicitně a lokální SQLite je
-- přebírá ze serveru beze změny. Administrace je ale nevkládá; spoléhá na to, že si je databáze
-- přidělí sama, což zařizuje jen schema.mysql.sql.
--
-- Skript je bezpečné pustit i na databázi, kde už AUTO_INCREMENT je — MODIFY na stejnou definici
-- neudělá nic. Data nemění.
--
-- Ověř si výsledek: administrace musí umět založit heslo, a `lexikon validate --server` musí projít.

SET FOREIGN_KEY_CHECKS = 0;

-- Typ ani nullability se nemění, jen se doplňuje AUTO_INCREMENT. Čítač si MariaDB nastaví sama na
-- max(sloupec) + 1, takže se nová hesla nesrazí s tím, co už z seed souborů v tabulkách je.
ALTER TABLE lexeme           MODIFY lexeme_id      INT NOT NULL AUTO_INCREMENT;
ALTER TABLE lemma_entry      MODIFY lemma_entry_id INT NOT NULL AUTO_INCREMENT;
ALTER TABLE lexical_unit     MODIFY lu_id          INT NOT NULL AUTO_INCREMENT;
ALTER TABLE valency_frame    MODIFY frame_id       INT NOT NULL AUTO_INCREMENT;
ALTER TABLE valency_slot     MODIFY slot_id        INT NOT NULL AUTO_INCREMENT;
ALTER TABLE slot_realization MODIFY realization_id INT NOT NULL AUTO_INCREMENT;
ALTER TABLE construction     MODIFY construction_id INT NOT NULL AUTO_INCREMENT;

SET FOREIGN_KEY_CHECKS = 1;

-- Kontrola: všech sedm řádků má mít v EXTRA "auto_increment".
--
--   SELECT TABLE_NAME, COLUMN_NAME, EXTRA
--     FROM information_schema.COLUMNS
--    WHERE TABLE_SCHEMA = DATABASE()
--      AND COLUMN_KEY = 'PRI'
--      AND DATA_TYPE = 'int'
--    ORDER BY TABLE_NAME;
