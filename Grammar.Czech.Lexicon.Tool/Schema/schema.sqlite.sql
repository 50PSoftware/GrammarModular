-- SQLite-only settings, applied after schema.sql.
--
-- Everything here is what a move to MySQL, Microsoft SQL or Firebird would replace rather than port.
-- Keeping it in its own file means that migration touches one file and leaves the DDL alone.

-- SQLite accepts foreign key clauses but does not act on them unless this is switched on, and it is
-- per connection rather than stored in the file. It is set here so that the tool's own writes are
-- checked; the provider sets it too.
PRAGMA foreign_keys = ON;

-- Written into the file header, so a build can tell a database matching the current schema from one
-- left over from an earlier shape. Raise it whenever schema.sql changes.
PRAGMA user_version = 4;

-- The lexicon is read far more often than it is written and ships as a single file, so the pages are
-- laid out for reading rather than for concurrent writers.
PRAGMA journal_mode = DELETE;
PRAGMA page_size = 4096;
