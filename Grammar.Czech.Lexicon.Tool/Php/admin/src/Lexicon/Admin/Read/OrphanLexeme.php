<?php

declare(strict_types=1);

namespace Lexicon\Admin\Read;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * A lexeme that no entry would point at any more.
 *
 * Its senses and their frames stay in the database and become unreachable through the admin, which is
 * worth saying before the delete rather than discovering at the next validate.
 */
final class OrphanLexeme
{
    public function __construct(
        public readonly int $id,
        public readonly string $primaryLemma,
        public readonly int $senses
    ) {
    }
}
