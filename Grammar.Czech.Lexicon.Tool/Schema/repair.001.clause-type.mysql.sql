-- Oprava dat v centrální MySQL kopii: clause_type nese lemma spojky, ne název druhu věty.
--
-- Není to migrace a schema_version se nemění, protože se nemění struktura — sloupec zůstává tím, čím
-- byl. Mění se to, co do něj smí, a řádek zapsaný podle staršího čtení je teď neplatný. Proto je to
-- soubor vedle migrací a ne mezi nimi: `migrate.NNN-NNN` posouvá verzi schématu a klient by pak
-- odmítl každou kopii, která jím neprošla, což by u čistě datové opravy byla škoda.
--
-- Proč lemma spojky: tak to zapisuje VALLEX, který je tady schématu předlohou, a nese to víc než
-- druh věty. 'ví, že přijde' a 'ví, zda přijde' jsou obě obsahové věty a znamenají každá něco jiného;
-- kdyby sloupec držel jen „obsahová“, generátor by mezi nimi neměl podle čeho vybrat.
--
-- Hodnota 'Declarative' byla jediná svého druhu a pocházela ze seed.001.sql, kde je od commitu
-- 0d1266c opravená. Seed ale platí jen pro `lexikon build`; server se seeduje jednou a dál se edituje
-- v administraci, takže se k němu oprava jinak než tímhle skriptem nedostane a nejbližší `pull` by ji
-- vrátil zpátky.
--
-- Pouští se ručně, jednou, přes phpMyAdmin nebo `mysql`.

UPDATE slot_realization
   SET clause_type = 'že'
 WHERE clause_type = 'Declarative';

-- Kontrola: po opravě nesmí zůstat žádná hodnota, která není podřadicí spojka. Seznam spojek žije
-- v conjunctions.json, kam SQL nedosáhne, takže se tu vyjmenují ty, které slovník používá — úplnou
-- kontrolu dělá `lexikon validate`.
SELECT realization_id, slot_id, clause_type
  FROM slot_realization
 WHERE clause_type IS NOT NULL
   AND clause_type NOT IN ('že', 'aby', 'zda', 'ať', 'jak', 'jestli', 'kdyby', 'protože');
