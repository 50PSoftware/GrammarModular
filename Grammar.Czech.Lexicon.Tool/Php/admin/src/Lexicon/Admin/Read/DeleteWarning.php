<?php

declare(strict_types=1);

namespace Lexicon\Admin\Read;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Something that will be left broken if an entry is deleted.
 *
 * It does not block the delete — an entry created by mistake is a reason to delete it — but none of it
 * is visible from the form: the foreign key runs from the entry to the lexeme and not back, and the
 * aspect counterpart is a lemma rather than a key at all, so the database stays silent about both.
 */
final class DeleteWarning
{
    public function __construct(
        public readonly string $text,
        public readonly string $link,
        public readonly string $linkText
    ) {
    }
}
