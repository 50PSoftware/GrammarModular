<?php

declare(strict_types=1);

namespace Lexicon\Admin\Controller;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Database\Database;
use Lexicon\Admin\Http\FileResponse;
use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Http\Response;
use Lexicon\Admin\Http\RouteMatch;
use Lexicon\Admin\Input\OldInput;
use Lexicon\Admin\Schema;
use Lexicon\Admin\View\Flash;
use Lexicon\Admin\View\Url;
use Lexicon\Admin\View\View;

/**
 * Stažení celého slovníku jako přenositelné INSERTy, přes přihlášenou session v prohlížeči.
 *
 * Existuje vedle `slovnik pull`/`validate --server` z jednoho důvodu: to jsou strojová volání proti
 * `/api/`, a WEDOS.protection u nich občas vyžádá ATP/ALTCHA výzvu, kterou konzolový klient nemá jak
 * vyřešit — je to JavaScript běžící v prohlížeči. Tahle route naopak vyřizuje přihlášený člověk ve
 * skutečném prohlížeči, takže tu výzvu buď vůbec nedostane, nebo ji prohlížeč vyřeší sám.
 *
 * Formát odpovídá LexiconDumper.cs — stejné INSERTy, stejné pořadí tabulek (LexiconSchema, rodiče
 * první), aby šel výstup přehrát proti prázdnému schématu stejně jako dump z lokálního souboru.
 */
final class ExportController extends Controller
{
    public function __construct(
        View $view,
        Url $url,
        Flash $flash,
        OldInput $old,
        Schema $schema,
        private readonly Database $database
    ) {
        parent::__construct($view, $url, $flash, $old, $schema);
    }

    /**
     * Sestaví dump a nabídne ho ke stažení.
     */
    public function download(Request $request, RouteMatch $route): Response
    {
        $body = "-- Grammar.Czech — lexicon dump.\n"
            . "-- Vyexportováno z administrace, přehraj proti prázdnému schématu.\n\n";

        foreach ($this->schema->tables() as $table) {
            $body .= $this->dumpTable($table);
        }

        $fileName = 'lexikon-' . date('Y-m-d') . '.sql';

        return new FileResponse($body, $fileName);
    }

    private function dumpTable(string $table): string
    {
        $columns = $this->schema->columns($table);
        $quotedColumns = implode(', ', array_map(static fn (string $column): string => "`$column`", $columns));

        // Řazeno podle primárního klíče — je vždy první sloupec — aby dva exporty téhož obsahu vyšly
        // bajtově stejné, ne podle pořadí, ve kterém MySQL řádky zrovna vrátí.
        $rows = $this->database->all("SELECT $quotedColumns FROM `$table` ORDER BY `{$columns[0]}`");

        if ($rows === []) {
            return '';
        }

        $lines = "-- $table\n";

        foreach ($rows as $row) {
            $values = array_map(static fn ($value): string => self::literal($value), $row);
            $lines .= "INSERT INTO $table ($quotedColumns) VALUES (" . implode(', ', $values) . ");\n";
        }

        return $lines . "\n";
    }

    /**
     * Stejná pravidla jako LexiconDumper.Literal — zdvojená apostrofa je escaping, na kterém se shodnou
     * SQLite i MySQL, na rozdíl od zpětného lomítka.
     */
    private static function literal(mixed $value): string
    {
        if ($value === null) {
            return 'NULL';
        }

        if (is_int($value) || is_float($value)) {
            return (string) $value;
        }

        return "'" . str_replace("'", "''", (string) $value) . "'";
    }
}
