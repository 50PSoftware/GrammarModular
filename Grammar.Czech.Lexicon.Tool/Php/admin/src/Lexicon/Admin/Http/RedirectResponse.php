<?php

declare(strict_types=1);

namespace Lexicon\Admin\Http;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * The answer to a write: go and read the result.
 *
 * Every POST ends in one of these, so reloading the page that follows does not post the form again.
 */
final class RedirectResponse extends Response
{
    public function __construct(string $location, int $status = 302)
    {
        parent::__construct('', $status, ['Location' => $location]);
    }
}
