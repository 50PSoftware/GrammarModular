<?php

declare(strict_types=1);

namespace Lexicon\Admin\Http;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * A file to save, not a page to read.
 *
 * `Content-Disposition: attachment` is what makes the browser offer a save dialog instead of rendering
 * the body — without it a .sql file full of INSERTy would print as plain text in the tab.
 */
final class FileResponse extends Response
{
    public function __construct(string $body, string $fileName, string $contentType = 'application/sql')
    {
        parent::__construct($body, 200, [
            'Content-Type' => $contentType . '; charset=utf-8',
            'Content-Disposition' => 'attachment; filename="' . str_replace('"', '', $fileName) . '"',
            'Content-Length' => (string) strlen($body),
        ]);
    }
}
