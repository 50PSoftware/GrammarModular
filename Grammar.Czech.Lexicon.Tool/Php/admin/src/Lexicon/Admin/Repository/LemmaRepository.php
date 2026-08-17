<?php

declare(strict_types=1);

namespace Lexicon\Admin\Repository;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use InvalidArgumentException;
use Lexicon\Admin\Database\Database;
use Lexicon\Admin\Entity\LemmaEntry;
use Lexicon\Admin\Entity\LemmaVariant;
use Lexicon\Admin\Input\LemmaKey;
use Lexicon\Admin\Read\EntrySense;
use Lexicon\Admin\Read\ListRow;
use Lexicon\Admin\Read\OrphanLexeme;
use Lexicon\Admin\Read\Page;
use Lexicon\Admin\Read\Referrer;
use Lexicon\Admin\Schema;

/**
 * Hesla, jejich dublety a to, co říkají o významech svého lexému.
 */
final class LemmaRepository
{
    public const PAGE_SIZE = 40;

    public function __construct(
        private readonly Database $database,
        private readonly Schema $schema
    ) {
    }

    /**
     * Najde heslo podle identifikátoru.
     */
    public function findById(int $id): ?LemmaEntry
    {
        $row = $this->database->one('SELECT * FROM lemma_entry WHERE lemma_entry_id = ?', [$id]);

        return $row === null ? null : LemmaEntry::fromRow($row);
    }

    /**
     * Stránka seznamu hesel.
     *
     * Hledá se přes lemma_key, tedy přes tentýž sloupec, kterým se lemma vyhledává za běhu, a dotaz se
     * skládá stejně — mb_strtolower. Hledat přes lemma by při akcentově necitlivé kolaci našlo i to, co
     * se ve skutečnosti hledat nedá.
     *
     * @return Page<ListRow>
     */
    public function search(?string $query, int $page): Page
    {
        $where = '';
        $parameters = [];

        if ($query !== null) {
            $where = 'WHERE e.lemma_key LIKE ?';
            $parameters[] = LemmaKey::of($query) . '%';
        }

        $total = (int) $this->database->one("SELECT COUNT(*) AS c FROM lemma_entry e $where", $parameters)['c'];
        $count = max(1, (int) ceil($total / self::PAGE_SIZE));
        $page = min(max(1, $page), $count);
        $offset = ($page - 1) * self::PAGE_SIZE;

        $rows = $this->database->all(
            "SELECT e.lemma_entry_id, e.lemma, e.category, e.gender, e.pattern, e.aspect, e.is_verified,
                    e.lexeme_id,
                    (SELECT COUNT(*) FROM lexical_unit u
                      JOIN valency_frame f ON f.lu_id = u.lu_id
                     WHERE u.lexeme_id = e.lexeme_id) AS frames
               FROM lemma_entry e
               $where
               ORDER BY e.lemma_key
               LIMIT " . self::PAGE_SIZE . " OFFSET $offset",
            $parameters
        );

        return new Page(array_map(ListRow::fromRow(...), $rows), $total, $page, $count);
    }

    /**
     * Založí heslo a vrátí identifikátor, který mu databáze přidělila.
     */
    public function insert(LemmaEntry $entry): int
    {
        $columns = $this->writableColumns();
        $placeholders = implode(', ', array_fill(0, count($columns), '?'));

        return $this->database->insert(
            'INSERT INTO lemma_entry (' . implode(', ', $columns) . ") VALUES ($placeholders)",
            $this->valuesOf($entry, $columns)
        );
    }

    /**
     * Přepíše heslo.
     */
    public function update(int $id, LemmaEntry $entry): void
    {
        $columns = $this->writableColumns();
        $assignments = implode(' = ?, ', $columns) . ' = ?';
        $values = $this->valuesOf($entry, $columns);
        $values[] = $id;

        $this->database->run("UPDATE lemma_entry SET $assignments WHERE lemma_entry_id = ?", $values);
    }

    /**
     * Smaže heslo.
     */
    public function delete(int $id): void
    {
        $this->database->run('DELETE FROM lemma_entry WHERE lemma_entry_id = ?', [$id]);
    }

    /**
     * Další spisovné podoby hesla.
     *
     * @return list<LemmaVariant>
     */
    public function variants(int $entryId): array
    {
        return array_map(
            LemmaVariant::fromRow(...),
            $this->database->all(
                'SELECT * FROM lemma_variant WHERE lemma_entry_id = ? ORDER BY lemma_key',
                [$entryId]
            )
        );
    }

    /**
     * Přidá heslu další spisovnou podobu.
     */
    public function addVariant(int $entryId, string $lemma, ?string $note): void
    {
        $this->database->run(
            'INSERT INTO lemma_variant (lemma_entry_id, lemma, lemma_key, note) VALUES (?, ?, ?, ?)',
            [$entryId, $lemma, LemmaKey::of($lemma), $note]
        );
    }

    /**
     * Smaže podobu hesla.
     */
    public function deleteVariant(int $variantId, int $entryId): void
    {
        // lemma_entry_id v podmínce, ne jen variant_id: identifikátor přišel z cesty a bez něj by
        // podvržené číslo smazalo dubletu cizího hesla.
        $this->database->run(
            'DELETE FROM lemma_variant WHERE variant_id = ? AND lemma_entry_id = ?',
            [$variantId, $entryId]
        );
    }

    /**
     * Významy lexému spolu s tím, co o nich tohle heslo říká.
     *
     * LEFT JOIN, protože řádek je výjimka: naprostá většina dvojic heslo–význam žádný nemá a stránka
     * je má stejně ukázat.
     *
     * @return list<EntrySense>
     */
    public function sensesFor(int $entryId, int $lexemeId): array
    {
        return array_map(
            EntrySense::fromRow(...),
            $this->database->all(
                'SELECT u.lu_id, u.sense_label, u.gloss, ls.aktionsart, ls.note AS sense_note
                   FROM lexical_unit u
                   LEFT JOIN lemma_sense ls ON ls.lu_id = u.lu_id AND ls.lemma_entry_id = ?
                  WHERE u.lexeme_id = ?
                  ORDER BY u.lu_id',
                [$entryId, $lexemeId]
            )
        );
    }

    /**
     * Zapíše způsob děje, který má tohle heslo v tomhle významu.
     *
     * Prázdno neznamená „žádná skupina“, ale „tenhle význam k heslu nic nepřidává“, a to se zapisuje
     * nepřítomností řádku. Uložený NULL by vypadal stejně a znamenal jiné.
     */
    public function saveSenseAktionsart(int $entryId, int $luId, ?string $aktionsart, ?string $note): void
    {
        $this->database->transaction(function () use ($entryId, $luId, $aktionsart, $note): void {
            $this->database->run(
                'DELETE FROM lemma_sense WHERE lemma_entry_id = ? AND lu_id = ?',
                [$entryId, $luId]
            );

            if ($aktionsart !== null) {
                $this->database->run(
                    'INSERT INTO lemma_sense (lemma_entry_id, lu_id, aktionsart, note) VALUES (?, ?, ?, ?)',
                    [$entryId, $luId, $aktionsart, $note]
                );
            }
        });
    }

    /**
     * Hesla, která na tohle ukazují lemmatem místo cizím klíčem.
     *
     * Který sloupec to je, se rozhoduje v PHP: CASE s parametry v THEN i ELSE si každý ovladač otypuje
     * po svém.
     *
     * @return list<Referrer>
     */
    public function findReferrers(string $lemma, int $exceptId): array
    {
        $rows = $this->database->all(
            'SELECT lemma, lemma_entry_id, aspect_counterpart, base_verb_lemma
             FROM lemma_entry
             WHERE (aspect_counterpart = ? OR base_verb_lemma = ?) AND lemma_entry_id <> ?',
            [$lemma, $lemma, $exceptId]
        );

        return array_map(
            static fn (array $row): Referrer => new Referrer(
                (int) $row['lemma_entry_id'],
                (string) $row['lemma'],
                $row['aspect_counterpart'] === $lemma ? 'vidový protějšek' : 'odvozeno ze slovesa'
            ),
            $rows
        );
    }

    /**
     * Lexém, na který po smazání tohohle hesla neukáže žádné jiné.
     */
    public function findOrphanLexeme(int $lexemeId, int $entryId): ?OrphanLexeme
    {
        $row = $this->database->one(
            'SELECT x.lexeme_id, x.primary_lemma,
                    (SELECT COUNT(*) FROM lexical_unit u WHERE u.lexeme_id = x.lexeme_id) AS senses
             FROM lexeme x
             WHERE x.lexeme_id = ?
               AND NOT EXISTS (SELECT 1 FROM lemma_entry e
                               WHERE e.lexeme_id = x.lexeme_id AND e.lemma_entry_id <> ?)',
            [$lexemeId, $entryId]
        );

        return $row === null
            ? null
            : new OrphanLexeme((int) $row['lexeme_id'], (string) $row['primary_lemma'], (int) $row['senses']);
    }

    /**
     * Sloupce, které se zapisují — všechny až na identifikátor, který přiděluje databáze.
     *
     * Bere se ze schématu, ne z ručního seznamu vedle SQL. Ten seznam tu jednou byl a musel se držet
     * v souladu s mapou sloupců po paměti.
     *
     * @return list<string>
     */
    private function writableColumns(): array
    {
        return $this->schema->writableColumns(LemmaEntry::TABLE);
    }

    /**
     * Hodnoty hesla v pořadí sloupců.
     *
     * @param list<string> $columns
     * @return list<mixed>
     */
    private function valuesOf(LemmaEntry $entry, array $columns): array
    {
        $row = $entry->toRow();
        $values = [];

        foreach ($columns as $column) {
            if (!array_key_exists($column, $row)) {
                // Do schématu přibyl sloupec, o kterém entita neví. Selhat hlasitě je jediná možnost,
                // jak se to pozná — tichý insert bez něj by uložil heslo, kterému kus chybí.
                throw new InvalidArgumentException(
                    "LemmaEntry nemá sloupec '$column'. Přibyl do schématu a do entity ne."
                );
            }

            $values[] = $row[$column];
        }

        return $values;
    }
}
