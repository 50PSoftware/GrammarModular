<?php

declare(strict_types=1);

namespace Lexicon\Admin\Http;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * A rendered page.
 */
final class HtmlResponse extends Response
{
    public function __construct(string $body, int $status = 200)
    {
        parent::__construct($body, $status, ['Content-Type' => 'text/html; charset=utf-8']);
    }
}
